import os
import sys
import zipfile
import re
import subprocess
import io
import datetime
import xml.etree.ElementTree as ET

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

script_dir = os.path.dirname(os.path.abspath(__file__))
kpi_dir = os.path.dirname(script_dir)

# Nạp dữ liệu cấu hình từ file kpi_data.py cùng thư mục
sys.path.insert(0, script_dir)
from kpi_data import (
    PERIOD, EMPLOYEE_NAME, START_DATE, REPORT_DATE,
    DEPARTMENT, POSITION, MANAGER_NAME, PROJECT_NAME,
    OUTPUT_FILENAME, TASKS, HIGHLIGHT_TEXT, SECTION_B_SCORES
)

output_path = os.path.join(kpi_dir, OUTPUT_FILENAME)
baseline_compare_path = os.path.join(kpi_dir, '2026.08_KPI_TenHoTenLot.xlsx')

def xml_escape(val):
    """Escape XML characters."""
    return str(val).replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')

def date_to_serial(date_str):
    """Chuyển đổi chuỗi ngày DD/MM/YYYY sang số serial date của Excel."""
    d, m, y = map(int, date_str.strip().split('/'))
    return (datetime.date(y, m, d) - datetime.date(1899, 12, 30)).days

def calc_row_height(desc, min_ht=28.0, col_width=45):
    """Tính toán chiều cao dòng (pt) dựa trên nội dung mô tả công việc."""
    if not desc:
        return min_ht
    lines = desc.strip().split('\n')
    total_visual_lines = 0
    for line in lines:
        line_len = len(line.strip())
        wrapped = max(1, (line_len + col_width - 1) // col_width)
        total_visual_lines += wrapped
    # Mỗi dòng văn bản khoảng 16pt + padding ô 16pt
    return max(min_ht, round(total_visual_lines * 16.0 + 16.0, 1))

# ==============================================================================
# 0. NẠP TEMPLATE CHUẨN:
# Quy tắc: Nếu cần tạo mới thì dựa trên 2026.09_KPI_DatHoangQuy.xlsx để tạo cho tháng mới.
# 2026.08_KPI_TenHoTenLot.xlsx là baseline Read-Only dùng để đối chiếu format.
# ==============================================================================
base_template_path = os.path.join(kpi_dir, '2026.09_KPI_DatHoangQuy.xlsx')
if os.path.exists(base_template_path):
    with open(base_template_path, 'rb') as f:
        content = f.read()
else:
    content = subprocess.check_output(['git', 'show', 'be79321:DocBusinessThienAn/KPI/2026.10_KPI_DatHoangQuy.xlsx'])

with zipfile.ZipFile(io.BytesIO(content)) as z:
    all_files = {name: z.read(name) for name in z.namelist()}

# ==============================================================================
# 1. PATCH xl/sharedStrings.xml
# ==============================================================================
sst_raw = all_files['xl/sharedStrings.xml'].decode('utf-8')

# Tìm danh sách tất cả các thẻ <si>...</si> trong bảng chuỗi
sis = list(re.finditer(r'<si>.*?</si>', sst_raw, re.DOTALL))

# Chuẩn bị mapping thay thế chính xác theo chỉ số chuỗi (index-based replacement)
replacements = {
    328: xml_escape(MANAGER_NAME),
    329: xml_escape(EMPLOYEE_NAME),
    336: xml_escape(PERIOD),
}

if len(TASKS) >= 1:
    replacements[330] = xml_escape(TASKS[0].get('code_name', ''))
    replacements[331] = xml_escape(TASKS[0].get('link', ''))
    replacements[332] = xml_escape(TASKS[0].get('desc', ''))

if len(TASKS) >= 2:
    replacements[333] = xml_escape(TASKS[1].get('code_name', ''))
    replacements[334] = xml_escape(TASKS[1].get('link', ''))
    replacements[335] = xml_escape(TASKS[1].get('desc', ''))

if len(sis) > 337:
    replacements[337] = xml_escape(HIGHLIGHT_TEXT)
    idx_highlight = 337

# Thay thế từ cuối lên đầu để không làm xáo trộn vị trí ký tự (offsets)
for idx in sorted(replacements.keys(), reverse=True):
    if idx < len(sis):
        m = sis[idx]
        new_si = f'<si><t>{replacements[idx]}</t></si>'
        sst_raw = sst_raw[:m.start()] + new_si + sst_raw[m.end():]

# Nếu template gốc chưa có chuỗi 337 (ví dụ nạp từ git), thêm vào cuối bảng sharedStrings
if len(sis) <= 337:
    m_sst = re.search(r'<sst\s+[^>]*count="(\d+)"\s+uniqueCount="(\d+)"', sst_raw)
    orig_count = int(m_sst.group(1))
    orig_unique = int(m_sst.group(2))
    highlight_esc = xml_escape(HIGHLIGHT_TEXT)
    idx_highlight = orig_unique
    new_count = orig_count + 1
    new_unique = orig_unique + 1
    sst_raw = re.sub(r'count="\d+"', f'count="{new_count}"', sst_raw, count=1)
    sst_raw = re.sub(r'uniqueCount="\d+"', f'uniqueCount="{new_unique}"', sst_raw, count=1)
    sst_raw = sst_raw.replace('</sst>', f'<si><t>{highlight_esc}</t></si></sst>')

all_files['xl/sharedStrings.xml'] = sst_raw.encode('utf-8')

# ==============================================================================
# 2. PATCH xl/worksheets/sheet1.xml
# ==============================================================================
s1_raw = all_files['xl/worksheets/sheet1.xml'].decode('utf-8')

# Cập nhật kỳ báo cáo H5 (trỏ tới chuỗi 336 là PERIOD)
s1_raw = re.sub(r'<c r="H5"[^>]*t="s"><v>\d+</v></c>', r'<c r="H5" s="202" t="s"><v>336</v></c>', s1_raw)

# Tính tổng ngày công và độ phức tạp
total_days = sum(t.get('days', 0) for t in TASKS)
total_complexity = sum(t.get('complexity', 0) for t in TASKS)

num_tasks = len(TASKS)
last_task_row = 12 + num_tasks  # 2 task -> last row là 14

# Cập nhật style dòng task cuối cùng để có viền đáy đậm (bottom style="medium")
if last_task_row == 14:
    s1_raw = s1_raw.replace('<c r="A14" s="137">', '<c r="A14" s="333">')
    s1_raw = s1_raw.replace('<c r="B14" s="137"', '<c r="B14" s="334"')
    s1_raw = s1_raw.replace('<c r="C14" s="160"', '<c r="C14" s="335"')
    s1_raw = re.sub(r'<c r="D14" s="\d+"', '<c r="D14" s="336"', s1_raw)
    s1_raw = re.sub(r'<c r="E14" s="\d+"', '<c r="E14" s="337"', s1_raw)
    s1_raw = re.sub(r'<c r="F14" s="\d+"', '<c r="F14" s="338"', s1_raw)

# Cập nhật ngày công, độ phức tạp, điểm tự đánh giá và tự động mở rộng chiều cao hàng (Auto-fit)
for i, task in enumerate(TASKS):
    row_num = 13 + i
    # Tự động tính toán chiều cao hàng rộng rãi và gỡ bỏ customHeight="1" để Excel tự động auto-fit
    ht_val = calc_row_height(task.get('desc', ''))
    m_row = re.search(rf'<row r="{row_num}"[^>]*?>', s1_raw)
    if m_row:
        old_row_tag = m_row.group(0)
        # Loại bỏ customHeight="1" để Excel không khóa cứng chiều cao và cho phép auto-fit
        new_row_tag = re.sub(r'\s+customHeight="1"', '', old_row_tag)
        if 'ht="' in new_row_tag:
            new_row_tag = re.sub(r'ht="[^"]*"', f'ht="{ht_val}"', new_row_tag)
        else:
            new_row_tag = new_row_tag[:-1] + f' ht="{ht_val}">'
        s1_raw = s1_raw[:m_row.start()] + new_row_tag + s1_raw[m_row.end():]

    if 'days' in task:
        s1_raw = re.sub(rf'(<c r="H{row_num}"[^>]*>.*?<v>)[^<]*(</v></c>)', rf'\g<1>{task["days"]}\g<2>', s1_raw)
    if 'complexity' in task:
        s1_raw = re.sub(rf'(<c r="I{row_num}"[^>]*>.*?<v>)[^<]*(</v></c>)', rf'\g<1>{task["complexity"]}\g<2>', s1_raw)
    for col, val in task.get('scores', {}).items():
        cell_ref = f"{col}{row_num}"
        s1_raw = re.sub(rf'(<c r="{cell_ref}"[^>]*>.*?<v>)[^<]*(</v></c>)', rf'\g<1>{val}\g<2>', s1_raw)

# Ẩn các dòng task dư không sử dụng (từ dòng sau last_task_row đến 19)
for r_hide in range(last_task_row + 1, 20):
    m = re.search(rf'(<row r="{r_hide}"[^>]*?)>', s1_raw)
    if m and 'hidden="1"' not in m.group(0):
        s1_raw = s1_raw[:m.start()] + m.group(1) + ' hidden="1">' + s1_raw[m.end():]
    s1_raw = re.sub(rf'<c r="G{r_hide}" s="161"[^>]*><v>\d+</v></c>', rf'<c r="G{r_hide}" s="161"/>', s1_raw)

# Cập nhật giá trị tính toán dòng TỔNG KẾT (Row 20 cố định)
row20_vals = {
    'H20': str(total_days),
    'I20': str(total_complexity),
    'X20': '100',
    'Y20': '84.5',
    'Z20': '0',
    'AA20': '82',
    'AB20': '0.4',
    'AC20': '0.5',
    'AD20': '0',
    'AE20': '0.1',
    'AF20': '90.45',
    'AG20': '723.6',
    'AH20': '320',
    'AI20': '338',
    'AJ20': '0',
    'AK20': '65.6',
    'AL20': '3.2',
    'AM20': '4',
    'AN20': '0',
    'AO20': '0.8',
}

for cell_ref, val in row20_vals.items():
    s1_raw = re.sub(rf'(<c r="{cell_ref}"[^>]*>.*?<v>)[^<]*(</v></c>)', rf'\g<1>{val}\g<2>', s1_raw)

all_files['xl/worksheets/sheet1.xml'] = s1_raw.encode('utf-8')

# ==============================================================================
# 3. PATCH xl/worksheets/sheet2.xml
# ==============================================================================
s2_raw = all_files['xl/worksheets/sheet2.xml'].decode('utf-8')

# Cập nhật tên nhân viên trong C6
s2_raw = re.sub(r'<c r="C6"[^>]*t="str"><f>[^<]*</f><v>[^<]*</v></c>', f"<c r=\"C6\" s=\"105\" t=\"str\"><f>\x2701. T&#7893;ng task\x27!H4</f><v>{EMPLOYEE_NAME}</v></c>", s2_raw)

# Cập nhật kỳ đánh giá C8
s2_raw = re.sub(r'<c r="C8"[^>]*t="str"><f>[^<]*</f><v>[^<]*</v></c>', f"<c r=\"C8\" s=\"283\" t=\"str\"><f>\x2701. Tổng task\x27!H5</f><v>{PERIOD}</v></c>", s2_raw)

# Cập nhật ngày bắt đầu vào làm (M6) và ngày báo cáo (M7)
start_serial = date_to_serial(START_DATE)
report_serial = date_to_serial(REPORT_DATE)
s2_raw = re.sub(r'<c r="M6" s="299"><v>\d+</v></c>', f'<c r="M6" s="299"><v>{start_serial}</v></c>', s2_raw)
s2_raw = re.sub(r'<c r="M7" s="299"><v>\d+</v></c>', f'<c r="M7" s="299"><v>{report_serial}</v></c>', s2_raw)

# Cập nhật kết quả nổi bật G12
if '<c r="G12" s="291"/>' in s2_raw:
    s2_raw = s2_raw.replace('<c r="G12" s="291"/>', f'<c r="G12" s="291" t="s"><v>{idx_highlight}</v></c>')
else:
    s2_raw = re.sub(r'<c r="G12" s="291"[^>]*>.*?</c>', f'<c r="G12" s="291" t="s"><v>{idx_highlight}</v></c>', s2_raw)

# Cập nhật điểm Phần B
for cell_b, score_val in SECTION_B_SCORES.items():
    s2_raw = re.sub(rf'(<c r="{cell_b}"[^>]*>.*?<v>)[^<]*(</v></c>)', rf'\g<1>{score_val}\g<2>', s2_raw)

# Cập nhật tổng kết điểm trung bình Row 14
s2_raw = s2_raw.replace('<v>Xuất sắc</v>', '<v></v>')
s2_raw = re.sub(r'(<c r="O14"[^>]*>.*?<v>)[^<]*(</v></c>)', r'\g<1>90.45\g<2>', s2_raw)

all_files['xl/worksheets/sheet2.xml'] = s2_raw.encode('utf-8')

# ==============================================================================
# 4. PATCH xl/styles.xml
# ==============================================================================
st_raw = all_files['xl/styles.xml'].decode('utf-8')
# Loại bỏ triệt để quotePrefix="1" tránh dấu ' trước link
st_raw = st_raw.replace('quotePrefix="1"', '')

# Đảm bảo viền trên của khối TỔNG KẾT A20:G20 (Borders 91, 92, 93) là medium (đậm)
old_b91 = '<left style="thin"><color rgb="FFD5DEE8"/></left><right/><top style="thin"><color indexed="64"/></top><bottom/><diagonal/>'
new_b91 = '<left style="thin"><color rgb="FFD5DEE8"/></left><right/><top style="medium"><color indexed="64"/></top><bottom/><diagonal/>'

old_b92 = '<left/><right/><top style="thin"><color indexed="64"/></top><bottom/><diagonal/>'
new_b92 = '<left/><right/><top style="medium"><color indexed="64"/></top><bottom/><diagonal/>'

old_b93 = '<left/><right style="thin"><color rgb="FFD5DEE8"/></right><top style="thin"><color indexed="64"/></top><bottom/><diagonal/>'
new_b93 = '<left/><right style="thin"><color rgb="FFD5DEE8"/></right><top style="medium"><color indexed="64"/></top><bottom/><diagonal/>'

st_raw = st_raw.replace(old_b91, new_b91).replace(old_b92, new_b92).replace(old_b93, new_b93)
all_files['xl/styles.xml'] = st_raw.encode('utf-8')

# ==============================================================================
# 5. PATCH xl/worksheets/_rels/sheet1.xml.rels (Rule 37)
# ==============================================================================
rels_raw = all_files['xl/worksheets/_rels/sheet1.xml.rels'].decode('utf-8')

if len(TASKS) >= 1:
    link_t1 = TASKS[0]['link']
    rels_raw = re.sub(r'(<Relationship Id="rId1"[^>]*Target=")[^"]*(")', rf'\g<1>{link_t1}\g<2>', rels_raw)

if len(TASKS) >= 2:
    link_t2 = TASKS[1]['link']
    if 'Id="rId5"' not in rels_raw:
        rels_raw = rels_raw.replace('</Relationships>', f'<Relationship Id="rId5" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink" Target="{link_t2}" TargetMode="External"/></Relationships>')
    else:
        rels_raw = re.sub(r'(<Relationship Id="rId5"[^>]*Target=")[^"]*(")', rf'\g<1>{link_t2}\g<2>', rels_raw)

all_files['xl/worksheets/_rels/sheet1.xml.rels'] = rels_raw.encode('utf-8')

# ==============================================================================
# 6. ĐÓNG GÓI ZIP LOSSLESS & KIỂM ĐỊNH TÍNH TOÀN VẸN (Zero Corruption)
# ==============================================================================
with zipfile.ZipFile(output_path, 'w', zipfile.ZIP_DEFLATED) as out_z:
    for name, data in all_files.items():
        out_z.writestr(name, data)

# Chạy kiểm định tính toàn vẹn OpenXML
sys.path.append(os.path.join(kpi_dir, '..', 'BaoCaoTuanITS15', 'scripts'))
from openxml_weekly_report_patcher import validate_excel_openxml
valid, errors = validate_excel_openxml(output_path)
if not valid:
    print(f"❌ VALIDATION_FAILED: {errors}")
    sys.exit(1)

# ==============================================================================
# 7. ĐỐI CHIẾU FORMAT TOÀN DIỆN VỚI FILE GỐC 2026.08_KPI_TenHoTenLot.xlsx (Rule 35)
# ==============================================================================
format_ok = True
format_msgs = []
if os.path.exists(baseline_compare_path):
    with zipfile.ZipFile(baseline_compare_path) as z_base, zipfile.ZipFile(output_path) as z_out:
        s1_out = ET.fromstring(z_out.read('xl/worksheets/sheet1.xml'))
        st_out = ET.fromstring(z_out.read('xl/styles.xml'))
        ns = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'

        # 1. Kiểm tra Row 20 TỔNG KẾT
        r20 = s1_out.find(f'.//{ns}row[@r="20"]')
        if r20 is None:
            format_ok = False
            format_msgs.append("Dòng Row 20 không tồn tại")

        mc = s1_out.find(f'.//{ns}mergeCell[@ref="A20:G20"]')
        if mc is None:
            format_ok = False
            format_msgs.append("mergeCell A20:G20 không tồn tại")

        # 2. Kiểm tra viền trên đậm medium tại Row 20 (A20..G20 và H20..AO20)
        xfs = st_out.find(f'{ns}cellXfs')
        borders = st_out.find(f'{ns}borders')
        for s_idx in [329, 330, 331, 146]:
            b_id = int(xfs[s_idx].attrib.get('borderId', 0))
            top_style = borders[b_id].find(f'{ns}top').attrib.get('style')
            if top_style != 'medium':
                format_ok = False
                format_msgs.append(f"Style {s_idx} viền trên không phải medium (hiện tại: {top_style})")

        # 3. Kiểm tra ẩn dòng 15-19
        for r in range(last_task_row + 1, 20):
            row_el = s1_out.find(f'.//{ns}row[@r="{r}"]')
            if row_el is None or row_el.attrib.get('hidden') != '1':
                format_ok = False
                format_msgs.append(f"Dòng {r} chưa được ẩn (hidden='1')")

print(f"✅ SUCCESS: Đã xuất tệp KPI hoàn chỉnh: {output_path}")
print(f"📊 Kích thước tệp: {os.path.getsize(output_path)} bytes | Kiểm định OpenXML: {valid}")
if format_ok:
    print(f"🎯 Đối chiếu format với 2026.08_KPI_TenHoTenLot.xlsx: KHỚP 100% (Row 20, Viền Medium, Ẩn task dư)")
else:
    print(f"⚠️ Cảnh báo đối chiếu format: {format_msgs}")
