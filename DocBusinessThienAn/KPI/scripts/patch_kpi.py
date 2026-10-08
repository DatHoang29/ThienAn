import os
import sys
import zipfile
import re
import openpyxl

sys.stdout.reconfigure(encoding='utf-8')

template_path = r'c:\ThienAn\DocBusinessThienAn\KPI\2026.08_KPI_TenHoTenLot.xlsx'
output_path = r'c:\ThienAn\DocBusinessThienAn\KPI\2026.10_KPI_DatHoangQuy.xlsx'

with zipfile.ZipFile(template_path, 'r') as z:
    all_files = {name: z.read(name) for name in z.namelist()}

# 1. Patch xl/sharedStrings.xml
sst_raw = all_files['xl/sharedStrings.xml'].decode('utf-8')

m_sst = re.search(r'<sst\s+[^>]*count="(\d+)"\s+uniqueCount="(\d+)"', sst_raw)
orig_count = int(m_sst.group(1))
orig_unique = int(m_sst.group(2))

new_strings = [
    "Hoàng Quý Đạt",
    "[2026] XD001.5.6 Service tích hợp dữ liệu 2",
    "https://wework.base.vn/tasks?task=11034326",
    "- Triển khai cơ chế gửi nối đuôi dữ liệu và gửi tức thì khi có dữ liệu mới\n- Chuẩn hóa luồng log cha - con và theo dõi fix bug kiểm thử F16",
    "XD001.6.3. Service tích hợp thiết bị",
    "https://wework.base.vn/tasks?task=11004386",
    "- Xây dựng API và cơ chế phân quyền VideoWall theo người dùng, tổ chức và ô màn hình\n- Phát triển Worker Service giao tiếp thiết bị qua ISAPI/NATS và form WPF test thiết bị",
    "2026-10"
]

idx_name = orig_unique      # 329
idx_t1_d = orig_unique + 1  # 330
idx_t1_e = orig_unique + 2  # 331
idx_t1_f = orig_unique + 3  # 332
idx_t2_d = orig_unique + 4  # 333
idx_t2_e = orig_unique + 5  # 334
idx_t2_f = orig_unique + 6  # 335
idx_period = orig_unique + 7 # 336

si_snippets = []
for s in new_strings:
    s_esc = s.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;').replace('"', '&quot;')
    si_snippets.append(f'<si><t>{s_esc}</t></si>')

new_count = orig_count + len(new_strings)
new_unique = orig_unique + len(new_strings)

sst_raw = re.sub(r'count="\d+"', f'count="{new_count}"', sst_raw, count=1)
sst_raw = re.sub(r'uniqueCount="\d+"', f'uniqueCount="{new_unique}"', sst_raw, count=1)
sst_raw = sst_raw.replace('</sst>', ''.join(si_snippets) + '</sst>')
all_files['xl/sharedStrings.xml'] = sst_raw.encode('utf-8')

# 2. Patch xl/worksheets/sheet1.xml
s1_raw = all_files['xl/worksheets/sheet1.xml'].decode('utf-8')

# Row 4 (Name)
s1_raw = s1_raw.replace('<c r="H4" s="202"/>', f'<c r="H4" s="202" t="s"><v>{idx_name}</v></c>')

# Row 5 (Period: 2026-10)
s1_raw = s1_raw.replace('<c r="H5" s="202" t="s"><v>3</v></c>', f'<c r="H5" s="202" t="s"><v>{idx_period}</v></c>')

# Row 13 (Task 1: 5 days, complexity 3)
r13_old_start = s1_raw.find('<row r="13"')
r13_old_end = s1_raw.find('</row>', r13_old_start) + len('</row>')
r13_old = s1_raw[r13_old_start:r13_old_end]

r13_new = r13_old
r13_new = r13_new.replace('<c r="D13" s="138" t="s"><v>66</v></c>', f'<c r="D13" s="138" t="s"><v>{idx_t1_d}</v></c>')
r13_new = r13_new.replace('<c r="E13" s="157" t="s"><v>67</v></c>', f'<c r="E13" s="157" t="s"><v>{idx_t1_e}</v></c>')
r13_new = r13_new.replace('<c r="F13" s="158" t="s"><v>68</v></c>', f'<c r="F13" s="158" t="s"><v>{idx_t1_f}</v></c>')
r13_new = r13_new.replace('<c r="H13" s="137"/>', '<c r="H13" s="137"><v>5</v></c>')
r13_new = r13_new.replace('<c r="I13" s="137"/>', '<c r="I13" s="137"><v>3</v></c>')
r13_new = r13_new.replace('<c r="V13" s="141"><v>5</v></c>', '<c r="V13" s="141"><v>4</v></c>')
r13_new = r13_new.replace('<c r="W13" s="141"><v>5</v></c>', '<c r="W13" s="141"><v>4</v></c>')
s1_raw = s1_raw[:r13_old_start] + r13_new + s1_raw[r13_old_end:]

# Row 14 (Task 2: 3 days, complexity 3)
r14_old_start = s1_raw.find('<row r="14"')
r14_old_end = s1_raw.find('</row>', r14_old_start) + len('</row>')
r14_old = s1_raw[r14_old_start:r14_old_end]

r14_x_start = r14_old.find('<c r="X14"')
r14_formulas = r14_old[r14_x_start:]  # contains X14 to AO14 and </row>

r14_prefix_new = (
    '<row r="14" spans="1:41" s="122" customFormat="1" ht="108.9" customHeight="1">'
    '<c r="A14" s="137"><v>2</v></c>'
    '<c r="B14" s="137" t="s"><v>64</v></c>'
    '<c r="C14" s="160" t="s"><v>65</v></c>'
    f'<c r="D14" s="138" t="s"><v>{idx_t2_d}</v></c>'
    f'<c r="E14" s="157" t="s"><v>{idx_t2_e}</v></c>'
    f'<c r="F14" s="158" t="s"><v>{idx_t2_f}</v></c>'
    '<c r="G14" s="161" t="s"><v>69</v></c>'
    '<c r="H14" s="137"><v>3</v></c>'
    '<c r="I14" s="137"><v>3</v></c>'
    '<c r="J14" s="139"><v>1</v></c>'
    '<c r="K14" s="139"><v>1</v></c>'
    '<c r="L14" s="137"><v>1</v></c>'
    '<c r="M14" s="137"><v>1</v></c>'
    '<c r="N14" s="137"><v>0</v></c>'
    '<c r="O14" s="139"><v>0.85</v></c>'
    '<c r="P14" s="139"><v>0</v></c>'
    '<c r="Q14" s="137"><v>0</v></c>'
    '<c r="R14" s="137"><v>0</v></c>'
    '<c r="S14" s="139"><v>1</v></c>'
    '<c r="T14" s="139"><v>1</v></c>'
    '<c r="U14" s="140"><v>0</v></c>'
    '<c r="V14" s="141"><v>4</v></c>'
    '<c r="W14" s="141"><v>4</v></c>'
)
r14_new = r14_prefix_new + r14_formulas
s1_raw = s1_raw[:r14_old_start] + r14_new + s1_raw[r14_old_end:]

# Clear hidden sample rows 17, 18, 19 precisely
row17_clean_inputs = (
    '<row r="17" spans="1:41" s="122" customFormat="1" ht="108.9" hidden="1" customHeight="1">'
    '<c r="A17" s="323"/><c r="B17" s="323"/><c r="C17" s="319"/><c r="D17" s="324"/><c r="E17" s="332"/><c r="F17" s="324"/>'
    '<c r="G17" s="161"/><c r="H17" s="137"/><c r="I17" s="137"/><c r="J17" s="139"/><c r="K17" s="139"/><c r="L17" s="137"/>'
    '<c r="M17" s="137"/><c r="N17" s="137"/><c r="O17" s="139"/><c r="P17" s="139"/><c r="Q17" s="137"/><c r="R17" s="137"/>'
    '<c r="S17" s="139"/><c r="T17" s="139"/><c r="U17" s="140"/><c r="V17" s="141"/><c r="W17" s="141"/>'
)

row18_clean_inputs = (
    '<row r="18" spans="1:41" s="122" customFormat="1" ht="108.9" hidden="1" customHeight="1">'
    '<c r="A18" s="137"/><c r="B18" s="145"/><c r="C18" s="160"/><c r="D18" s="127"/><c r="E18" s="157"/><c r="F18" s="158"/>'
    '<c r="G18" s="161"/><c r="H18" s="137"/><c r="I18" s="137"/><c r="J18" s="139"/><c r="K18" s="139"/><c r="L18" s="162"/>'
    '<c r="M18" s="137"/><c r="N18" s="137"/><c r="O18" s="139"/><c r="P18" s="139"/><c r="Q18" s="137"/><c r="R18" s="137"/>'
    '<c r="S18" s="139"/><c r="T18" s="139"/><c r="U18" s="140"/><c r="V18" s="141"/><c r="W18" s="141"/>'
)

row19_clean_inputs = (
    '<row r="19" spans="1:41" s="122" customFormat="1" ht="108.9" hidden="1" customHeight="1">'
    '<c r="A19" s="325"/><c r="B19" s="325"/><c r="C19" s="321"/><c r="D19" s="322"/><c r="E19" s="326"/><c r="F19" s="327"/>'
    '<c r="G19" s="328"/><c r="H19" s="137"/><c r="I19" s="137"/><c r="J19" s="139"/><c r="K19" s="139"/><c r="L19" s="137"/>'
    '<c r="M19" s="137"/><c r="N19" s="137"/><c r="O19" s="139"/><c r="P19" s="139"/><c r="Q19" s="137"/><c r="R19" s="137"/>'
    '<c r="S19" s="139"/><c r="T19" s="139"/><c r="U19" s="140"/><c r="V19" s="141"/><c r="W19" s="141"/>'
)

for r_idx, clean_prefix in [(17, row17_clean_inputs), (18, row18_clean_inputs), (19, row19_clean_inputs)]:
    r_start = s1_raw.find(f'<row r="{r_idx}"')
    r_end = s1_raw.find('</row>', r_start) + len('</row>')
    row_str = s1_raw[r_start:r_end]
    
    x_pos = row_str.find(f'<c r="X{r_idx}"')
    row_formulas = row_str[x_pos:]
    row_formulas_clean = re.sub(r'<v>[^<]*</v>', '<v>0</v>', row_formulas)
    
    s1_raw = s1_raw[:r_start] + clean_prefix + row_formulas_clean + s1_raw[r_end:]

# Update Row 20 cached totals for Days (8) and Complexity (6)
s1_raw = s1_raw.replace('<c r="H20" s="137"><f>SUM(H13:H19)</f><v>5.5</v></c>', '<c r="H20" s="137"><f>SUM(H13:H19)</f><v>8</v></c>')
s1_raw = s1_raw.replace('<c r="I20" s="137"><f>SUM(I13:I19)</f><v>8</v></c>', '<c r="I20" s="137"><f>SUM(I13:I19)</f><v>6</v></c>')

all_files['xl/worksheets/sheet1.xml'] = s1_raw.encode('utf-8')

# 3. Patch xl/worksheets/sheet2.xml
s2_raw = all_files['xl/worksheets/sheet2.xml'].decode('utf-8')

# Set employee name cached value in C6
s2_raw = s2_raw.replace("<c r=\"C6\" s=\"105\"><f>'01. T&#7893;ng task'!H4</f><v>0</v></c>", f"<c r=\"C6\" s=\"105\" t=\"str\"><f>'01. T&#7893;ng task'!H4</f><v>Ho&#224;ng Qu&#253; &#272;&#7841;t</v></c>")

# Set Report Date = 17/10/2026 (Serial Date: 46312) in M7
s2_raw = s2_raw.replace('<c r="M7" s="299"><v>46242</v></c>', '<c r="M7" s="299"><v>46312</v></c>')

# Set Evaluation Period cached value = 2026-10 in C8
s2_raw = s2_raw.replace("<c r=\"C8\" s=\"283\" t=\"str\"><f>'01. T&#7893;ng task'!H5</f><v>2026-08</v></c>", f"<c r=\"C8\" s=\"283\" t=\"str\"><f>'01. T&#7893;ng task'!H5</f><v>2026-10</v></c>")

# Section B self-evaluation = 8
s2_raw = s2_raw.replace('<c r="H18" s="86"/>', '<c r="H18" s="86"><v>8</v></c>')
s2_raw = s2_raw.replace('<c r="H19" s="90"/>', '<c r="H19" s="90"><v>8</v></c>')
s2_raw = s2_raw.replace('<c r="H20" s="86"/>', '<c r="H20" s="86"><v>8</v></c>')
s2_raw = s2_raw.replace('<c r="H21" s="90"/>', '<c r="H21" s="90"><v>8</v></c>')
all_files['xl/worksheets/sheet2.xml'] = s2_raw.encode('utf-8')

# Write back lossless zip
with zipfile.ZipFile(output_path, 'w', zipfile.ZIP_DEFLATED) as out_z:
    for name, data in all_files.items():
        out_z.writestr(name, data)

print(f"SUCCESS_LOSSLESS_PATCH_KPI! Output file size: {os.path.getsize(output_path)}")
