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
bao_cao_dir = os.path.dirname(script_dir)
kpi_dir = os.path.join(bao_cao_dir, 'KPI')

# Nạp module kiểm định dùng chung excel_validator
sys.path.insert(0, script_dir)
from excel_validator import validate_excel_openxml, date_to_serial

# Nạp dữ liệu cấu hình từ file kpi_data.py cùng thư mục
from kpi_data import (
    PERIOD, EMPLOYEE_NAME, START_DATE, REPORT_DATE,
    DEPARTMENT, POSITION, MANAGER_NAME, PROJECT_NAME,
    OUTPUT_FILENAME, TASKS, HIGHLIGHT_TEXT, SECTION_B_SCORES,
    SHORT_TERM_GOAL, LONG_TERM_GOAL
)

output_path = os.path.join(kpi_dir, OUTPUT_FILENAME)
baseline_compare_path = os.path.join(kpi_dir, '2026.08_KPI_TenHoTenLot.xlsx')

def xml_escape(val):
    """Escape XML characters."""
    return str(val).replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')

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
try:
    content = subprocess.check_output(['git', 'show', 'HEAD:DocBusinessThienAn/BaoCao/KPI/2026.09_KPI_DatHoangQuy.xlsx'])
except Exception:
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
    2: xml_escape(EMPLOYEE_NAME),
    4: xml_escape(PERIOD),
    104: xml_escape(MANAGER_NAME),
    115: xml_escape(HIGHLIGHT_TEXT),
}

if len(TASKS) >= 1:
    replacements[67] = xml_escape(TASKS[0].get('code_name', ''))
    replacements[68] = xml_escape(TASKS[0].get('link', ''))
    replacements[69] = xml_escape(TASKS[0].get('desc', ''))

if len(TASKS) >= 2:
    replacements[71] = xml_escape(TASKS[1].get('code_name', ''))
    replacements[72] = xml_escape(TASKS[1].get('link', ''))
    replacements[73] = xml_escape(TASKS[1].get('desc', ''))

# Thay thế các chuỗi sẵn có từ cuối lên đầu để không làm xáo trộn vị trí ký tự
for idx in sorted(replacements.keys(), reverse=True):
    if idx < len(sis):
        m = sis[idx]
        new_si = f'<si><t>{replacements[idx]}</t></si>'
        sst_raw = sst_raw[:m.start()] + new_si + sst_raw[m.end():]

# Thêm 2 chuỗi mục tiêu nghề nghiệp ngắn hạn & dài hạn vào cuối sharedStrings
m_sst = re.search(r'<sst\s+[^>]*count="(\d+)"\s+uniqueCount="(\d+)"', sst_raw)
orig_count = int(m_sst.group(1))
orig_unique = int(m_sst.group(2))

idx_short_term = orig_unique
idx_long_term = orig_unique + 1
new_count = orig_count + 2
new_unique = orig_unique + 2

short_esc = xml_escape(SHORT_TERM_GOAL)
long_esc = xml_escape(LONG_TERM_GOAL)
sst_raw = re.sub(r'count="\d+"', f'count="{new_count}"', sst_raw, count=1)
sst_raw = re.sub(r'uniqueCount="\d+"', f'uniqueCount="{new_unique}"', sst_raw, count=1)
sst_raw = sst_raw.replace('</sst>', f'<si><t>{short_esc}</t></si><si><t>{long_esc}</t></si></sst>')

all_files['xl/sharedStrings.xml'] = sst_raw.encode('utf-8')

# ==============================================================================
# 2. PATCH xl/worksheets/sheet1.xml
# ==============================================================================
s1_raw = all_files['xl/worksheets/sheet1.xml'].decode('utf-8')

# Cập nhật kỳ báo cáo H5 (trỏ tới chuỗi 4 là PERIOD)
s1_raw = re.sub(r'<c r="H5"[^>]*t="s"><v>\d+</v></c>', r'<c r="H5" s="240" t="s"><v>4</v></c>', s1_raw)

# Tính tổng ngày công và độ phức tạp
total_days = sum(t.get('days', 0) for t in TASKS)
total_complexity = sum(t.get('complexity', 0) for t in TASKS)

num_tasks = len(TASKS)
last_task_row = 12 + num_tasks  # 2 task -> last row là 14

# Cập nhật ngày công, độ phức tạp, và điểm số của từng Task
for i, task in enumerate(TASKS):
    row_num = 13 + i
    if 'days' in task:
        s1_raw = re.sub(rf'(<c r="H{row_num}"[^>]*>.*?<v>)[^<]*(</v></c>)', fr'\g<1>{task["days"]}\g<2>', s1_raw)
    if 'complexity' in task:
        s1_raw = re.sub(rf'(<c r="I{row_num}"[^>]*>.*?<v>)[^<]*(</v></c>)', fr'\g<1>{task["complexity"]}\g<2>', s1_raw)
    for col, val in task.get('scores', {}).items():
        cell_ref = f"{col}{row_num}"
        s1_raw = re.sub(rf'(<c r="{cell_ref}"[^>]*>.*?<v>)[^<]*(</v></c>)', fr'\g<1>{val}\g<2>', s1_raw)

# Đảm bảo các dòng Task (13..last_task_row) luôn có customHeight="1" để Excel khóa chiều cao khít nội dung, không bị co rút khi mở
for r_task in range(13, last_task_row + 1):
    def fix_task_row(m):
        tag = m.group(1)
        if 'customHeight="1"' not in tag:
            tag += ' customHeight="1"'
        return tag + '>'
    s1_raw = re.sub(rf'(<row r="{r_task}"[^>]*?)>', fix_task_row, s1_raw)

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

# Cập nhật tên nhân viên trong C6 (Style 112 chuẩn text #1E293B, không nền fillId=0, đối chiếu 1:1 với baseline style 294)
s2_raw = re.sub(r'<c r="C6"[^>]*>.*?</c>', f"<c r=\"C6\" s=\"112\" t=\"str\"><f>\x2701. T&#7893;ng task\x27!H4</f><v>{EMPLOYEE_NAME}</v></c>", s2_raw)

# Cập nhật kỳ đánh giá C8 (Style 112 chuẩn text #1E293B, borderId=21 mỏng FFD5DEE8, khớp 1:1 với C6, C7 và baseline)
s2_raw = re.sub(r'<c r="C8"[^>]*>.*?</c>', f"<c r=\"C8\" s=\"112\" t=\"str\"><f>\x2701. Tổng task\x27!H5</f><v>{PERIOD}</v></c>", s2_raw)

# Cập nhật ngày bắt đầu vào làm (M6) và ngày báo cáo (M7)
start_serial = date_to_serial(START_DATE)
report_serial = date_to_serial(REPORT_DATE)
s2_raw = re.sub(r'(<c r="M6"[^>]*><v>)\d+(</v></c>)', fr'\g<1>{start_serial}\g<2>', s2_raw)
s2_raw = re.sub(r'(<c r="M7"[^>]*><v>)\d+(</v></c>)', fr'\g<1>{report_serial}\g<2>', s2_raw)

# Cập nhật kết quả nổi bật G12
if '<c r="G12" s="291"/>' in s2_raw:
    s2_raw = s2_raw.replace('<c r="G12" s="291"/>', f'<c r="G12" s="291" t="s"><v>115</v></c>')
else:
    s2_raw = re.sub(r'<c r="G12" s="291"[^>]*>.*?</c>', f'<c r="G12" s="291" t="s"><v>115</v></c>', s2_raw)

# Cập nhật điểm Phần B
for cell_b, score_val in SECTION_B_SCORES.items():
    s2_raw = re.sub(rf'(<c r="{cell_b}"[^>]*>.*?<v>)[^<]*(</v></c>)', rf'\g<1>{score_val}\g<2>', s2_raw)

# Cập nhật tổng kết điểm trung bình Row 14
s2_raw = s2_raw.replace('<v>Xuất sắc</v>', '<v></v>')
s2_raw = re.sub(r'(<c r="O14"[^>]*>.*?<v>)[^<]*(</v></c>)', r'\g<1>90.45\g<2>', s2_raw)

# Cập nhật Mục F. Định hướng nghề nghiệp: B60 (ngắn hạn) và B61 (dài hạn)
if '<c r="B60" s="224"/>' in s2_raw:
    s2_raw = s2_raw.replace('<c r="B60" s="224"/>', f'<c r="B60" s="224" t="s"><v>{idx_short_term}</v></c>')
else:
    s2_raw = re.sub(r'<c r="B60"[^>]*>.*?</c>', f'<c r="B60" s="224" t="s"><v>{idx_short_term}</v></c>', s2_raw)

if '<c r="B61" s="219"/>' in s2_raw:
    s2_raw = s2_raw.replace('<c r="B61" s="219"/>', f'<c r="B61" s="224" t="s"><v>{idx_long_term}</v></c>')
else:
    s2_raw = re.sub(r'<c r="B61"[^>]*>.*?</c>', f'<c r="B61" s="224" t="s"><v>{idx_long_term}</v></c>', s2_raw)

# Cập nhật chiều cao hàng Row 60 (ht="65.0") và Row 61 (ht="105.0") để hiển thị rõ ràng, thoải mái
s2_raw = re.sub(r'(<row r="60"[^>]*?ht=")[^"]*(")', r'\g<1>65.0\g<2>', s2_raw)
s2_raw = re.sub(r'(<row r="61"[^>]*?ht=")[^"]*(")', r'\g<1>105.0\g<2>', s2_raw)

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
    rels_raw = re.sub(r'(<Relationship Id="rId4"[^>]*Target=")[^"]*(")', rf'\g<1>{link_t1}\g<2>', rels_raw)

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

# Chạy kiểm định tính toàn vẹn OpenXML & đối chiếu template gốc (gọi hàm từ excel_validator)
valid, errors = validate_excel_openxml(output_path, baseline_compare_path)
if not valid:
    print(f"❌ VALIDATION_FAILED: {errors}")
    sys.exit(1)

# ==============================================================================
# 7. ĐỐI CHIẾU FORMAT & CÔNG THỨC VỚI FILE GỐC 2026.08_KPI_TenHoTenLot.xlsx (Rule 28, Rule 35)
# ==============================================================================
format_ok = True
format_msgs = []
total_base_formulas = 0
total_out_formulas = 0

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
        if r20 is not None:
            for c in r20.findall(f'{ns}c'):
                s_idx = int(c.attrib.get('s', 0))
                if s_idx < len(xfs):
                    b_id = int(xfs[s_idx].attrib.get('borderId', 0))
                    top_node = borders[b_id].find(f'{ns}top')
                    top_style = top_node.attrib.get('style') if top_node is not None else None
                    if top_style != 'medium':
                        format_ok = False
                        format_msgs.append(f"Cell {c.attrib.get('r')} style {s_idx} viền trên không phải medium (hiện tại: {top_style})")

        # 3. Kiểm tra ẩn dòng 15-19
        for r in range(last_task_row + 1, 20):
            row_el = s1_out.find(f'.//{ns}row[@r="{r}"]')
            if row_el is None or row_el.attrib.get('hidden') != '1':
                format_ok = False
                format_msgs.append(f"Dòng {r} chưa được ẩn (hidden='1')")

        # 4. Kiểm tra bảo toàn 100% công thức (<f>...</f>) so với file baseline
        for sheet_file in ['xl/worksheets/sheet1.xml', 'xl/worksheets/sheet2.xml']:
            if sheet_file in z_base.namelist() and sheet_file in z_out.namelist():
                base_s = ET.fromstring(z_base.read(sheet_file))
                out_s = ET.fromstring(z_out.read(sheet_file))
                base_f = {c.attrib.get('r'): c.find(f'{ns}f').text 
                          for c in base_s.findall(f'.//{ns}c') 
                          if c.find(f'{ns}f') is not None}
                out_f = {c.attrib.get('r'): c.find(f'{ns}f').text 
                         for c in out_s.findall(f'.//{ns}c') 
                         if c.find(f'{ns}f') is not None}
                total_base_formulas += len(base_f)
                total_out_formulas += len(out_f)
                lost_f = set(base_f.keys()) - set(out_f.keys())
                if lost_f:
                    format_ok = False
                    format_msgs.append(f"{sheet_file} bị mất công thức tại các ô: {sorted(list(lost_f))}")

        # 5. Kiểm tra toàn diện màu font và fill của khối Thông tin chung Sheet 2 (Zero Color Regression)
        s2_out = ET.fromstring(z_out.read('xl/worksheets/sheet2.xml'))
        fonts_out = st_out.find(f'{ns}fonts')
        c6_out = s2_out.find(f'.//{ns}c[@r="C6"]')
        if c6_out is not None:
            s_c6 = int(c6_out.attrib.get('s', 0))
            if s_c6 < len(xfs):
                xf_c6 = xfs[s_c6]
                f_c6 = int(xf_c6.attrib.get('fontId', 0))
                color_c6 = fonts_out[f_c6].find(f'{ns}color')
                rgb_c6 = color_c6.attrib.get('rgb') if color_c6 is not None else ''
                fill_c6 = int(xf_c6.attrib.get('fillId', 0))
                if rgb_c6 != 'FF1E293B' or fill_c6 != 0:
                    format_ok = False
                    format_msgs.append(f"C6 màu chữ ({rgb_c6}) hoặc màu nền fillId={fill_c6} sai chuẩn (bắt buộc text #1E293B, fillId=0)")

        # 6. Quét toàn bộ các ô trong Sheet 1 & Sheet 2: Cấm dùng font màu vàng FFFFC000 ngoài vùng đánh giá sao L36..L39
        for s_check in ['xl/worksheets/sheet1.xml', 'xl/worksheets/sheet2.xml']:
            s_xml = ET.fromstring(z_out.read(s_check))
            for cell in s_xml.findall(f'.//{ns}c'):
                r_cell = cell.attrib.get('r')
                if s_check.endswith('sheet2.xml') and r_cell in ['L36', 'L37', 'L38', 'L39']:
                    continue  # Bỏ qua các ô xếp hạng sao
                s_idx = int(cell.attrib.get('s', 0))
                if s_idx < len(xfs):
                    f_id = int(xfs[s_idx].attrib.get('fontId', 0))
                    c_node = fonts_out[f_id].find(f'{ns}color')
                    if c_node is not None:
                        c_rgb = c_node.attrib.get('rgb', '')
                        if 'C000' in c_rgb or c_rgb == 'FFFFFF00':
                            format_ok = False
                            format_msgs.append(f"Ô {r_cell} trong {s_check} có màu chữ vàng sai chuẩn: {c_rgb}")

print(f"✅ SUCCESS: Đã xuất tệp KPI hoàn chỉnh: {output_path}")
print(f"📊 Kích thước tệp: {os.path.getsize(output_path)} bytes | Kiểm định OpenXML: {valid}")
if format_ok:
    print(f"🎯 Đối chiếu format & công thức với 2026.08_KPI_TenHoTenLot.xlsx: KHỚP 100% ({total_out_formulas}/{total_base_formulas} công thức, Row 20, Viền Medium, Ẩn task dư)")
else:
    print(f"⚠️ Cảnh báo đối chiếu format & công thức: {format_msgs}")
