"""
Công cụ kiểm định và chuẩn hóa tính toàn vẹn file Excel OpenXML dùng chung (Single Source of Truth).
Áp dụng cho toàn bộ tệp Báo cáo tuần (BaoCaoTuan) và Báo cáo KPI (KPI) tại DocBusinessThienAn/BaoCao/.
Tuân thủ P0 Rule 28 và Rule 35:
- Zero Corruption: Bảo toàn 100% cấu trúc bit-perfect (logo, shapes, styles, font, borders).
- Zero Formula Loss: Quét và đối chiếu bảo toàn 100% công thức tính toán (<f>...</f>) so với template gốc.
"""

import os
import re
import sys
import zipfile
import datetime
from datetime import date
import xml.etree.ElementTree as ET


def excel_date_serial(d: date) -> int:
    """Chuyển đổi date thành số ngày serial của Microsoft Excel (base 1899-12-30)."""
    base = date(1899, 12, 30)
    return (d - base).days


def date_to_serial(date_str: str) -> int:
    """Chuyển đổi chuỗi ngày DD/MM/YYYY sang số serial date của Excel."""
    d, m, y = map(int, date_str.strip().split('/'))
    return (datetime.date(y, m, d) - datetime.date(1899, 12, 30)).days


def validate_excel_openxml(file_path: str, reference_template_path: str = None) -> tuple[bool, list[str]]:
    """
    Kiểm tra tính toàn vẹn (Zero Corruption & Zero Formula Loss) của file Excel OpenXML.
    Đối chiếu với file template gốc để đảm bảo không bị lỗi recovery và không bị mất công thức khi mở bằng Microsoft Excel.
    """
    errors = []
    if not os.path.exists(file_path):
        return False, [f"Tệp không tồn tại: {file_path}"]

    try:
        with zipfile.ZipFile(file_path, "r") as z:
            # 1. Kiểm tra cấu trúc ZIP
            test_res = z.testzip()
            if test_res is not None:
                errors.append(f"Lỗi cấu trúc ZIP tại: {test_res}")

            # 2. Kiểm tra cú pháp XML của tất cả các file .xml và .rels
            for name in z.namelist():
                if name.endswith(".xml") or name.endswith(".rels"):
                    try:
                        content = z.read(name)
                        ET.fromstring(content)
                    except Exception as e:
                        errors.append(f"Lỗi cú pháp XML trong {name}: {e}")

            # 3. Kiểm tra chuyên sâu xl/worksheets/sheet1.xml
            if "xl/worksheets/sheet1.xml" in z.namelist():
                sheet1 = z.read("xl/worksheets/sheet1.xml").decode("utf-8")

                # Cấm các tiền tố namespace tự sinh do ElementTree làm hỏng OpenXML
                for bad_prefix in ["ns0:", "ns1:", "ns2:", "ns3:", "ns4:"]:
                    if bad_prefix in sheet1:
                        errors.append(f"sheet1.xml chứa tiền tố namespace tự sinh không chuẩn: {bad_prefix}")

                # Bắt buộc quan hệ r:id chuẩn cho drawing
                if "drawing" in sheet1 and 'r:id="' not in sheet1:
                    errors.append('sheet1.xml thiếu thuộc tính chuẩn r:id trong thẻ drawing')

                # Bắt buộc các tiền tố trong Ignorable (mc:Ignorable) phải được khai báo xmlns:<prefix>
                ignorable_match = re.search(r'Ignorable="([^"]+)"', sheet1)
                if ignorable_match:
                    prefixes = ignorable_match.group(1).split()
                    for p in prefixes:
                        if f"xmlns:{p}=" not in sheet1:
                            errors.append(f"Tiền tố '{p}' trong Ignorable chưa được khai báo xmlns:{p}= trong sheet1.xml!")

                # Bắt buộc có khai báo namespace quan hệ chuẩn
                if 'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"' not in sheet1:
                    errors.append("sheet1.xml thiếu khai báo xmlns:r cho quan hệ OpenXML")

                # Bắt buộc các dòng task (Row 13, 14) phải có customHeight="1" để không bị Excel co rúm chiều cao dòng
                s1_root = ET.fromstring(sheet1)
                ns_main = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
                for r_elem in s1_root.findall(f'.//{ns_main}row'):
                    r_id = r_elem.attrib.get('r')
                    if r_id in ['13', '14']:
                        if r_elem.attrib.get('customHeight') != '1':
                            errors.append(f"sheet1.xml dòng {r_id} thiếu thuộc tính customHeight=\"1\" khiến chiều cao dòng bị co rút")

            # 4. Kiểm tra chuyên sâu xl/sharedStrings.xml
            if "xl/sharedStrings.xml" in z.namelist():
                ss_content = z.read("xl/sharedStrings.xml").decode("utf-8")
                if 'xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"' not in ss_content:
                    errors.append("sharedStrings.xml thiếu namespace chính của SpreadsheetML")

            # 5. Đối chiếu danh sách tệp & công thức với template gốc
            if reference_template_path and os.path.exists(reference_template_path):
                with zipfile.ZipFile(reference_template_path, "r") as z_ref:
                    # Bỏ qua các tệp cache tùy chọn của OpenXML (như calcChain.xml được Excel tự động sinh khi mở)
                    optional_parts = {"xl/calcChain.xml"}
                    missing_parts = (set(z_ref.namelist()) - set(z.namelist())) - optional_parts
                    if missing_parts:
                        errors.append(f"Thiếu các tệp thành phần so với template gốc: {missing_parts}")

                    # Đối chiếu bảo toàn công thức (Formulas) trên tất cả các sheet
                    for part in z_ref.namelist():
                        if part.startswith("xl/worksheets/sheet") and part.endswith(".xml") and part in z.namelist():
                            ref_root = ET.fromstring(z_ref.read(part))
                            cur_root = ET.fromstring(z.read(part))
                            ns = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
                            ref_f = {c.attrib.get('r'): c.find(f'{ns}f').text 
                                     for c in ref_root.findall(f'.//{ns}c') 
                                     if c.find(f'{ns}f') is not None}
                            cur_f = {c.attrib.get('r'): c.find(f'{ns}f').text 
                                     for c in cur_root.findall(f'.//{ns}c') 
                                     if c.find(f'{ns}f') is not None}
                            lost_f = set(ref_f.keys()) - set(cur_f.keys())
                            if lost_f:
                                errors.append(f"{part} bị mất công thức tại các ô: {sorted(list(lost_f))}")

                    # 6. Kiểm tra toàn vẹn định dạng màu sắc (Zero Color Corruption)
                    if "xl/styles.xml" in z.namelist() and "xl/worksheets/sheet2.xml" in z.namelist():
                        st_root = ET.fromstring(z.read("xl/styles.xml"))
                        s2_root = ET.fromstring(z.read("xl/worksheets/sheet2.xml"))
                        ns = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
                        xfs = st_root.find(f'{ns}cellXfs')
                        fonts = st_root.find(f'{ns}fonts')

                        # Kiểm tra ô Họ tên nhân viên C6 trong Sheet 2 (bắt buộc màu chữ chuẩn #1E293B, không nền)
                        c6 = s2_root.find(f'.//{ns}c[@r="C6"]')
                        if c6 is not None:
                            s_idx = int(c6.attrib.get('s', 0))
                            if s_idx < len(xfs):
                                xf = xfs[s_idx]
                                f_id = int(xf.attrib.get('fontId', 0))
                                fill_id = int(xf.attrib.get('fillId', 0))
                                font = fonts[f_id]
                                color = font.find(f'{ns}color')
                                rgb = color.attrib.get('rgb') if color is not None else ''
                                if rgb != 'FF1E293B' or fill_id != 0:
                                    errors.append(f"Sheet 2 ô C6 sai định dạng: màu chữ '{rgb}' (yêu cầu FF1E293B) hoặc fillId={fill_id} (yêu cầu 0)")

                        # Quét cấm chữ màu vàng/cam FFFFC000 trên toàn bộ các ô ngoài khu vực đánh giá sao (L36..L39)
                        for part in ["xl/worksheets/sheet1.xml", "xl/worksheets/sheet2.xml"]:
                            if part in z.namelist():
                                s_root = ET.fromstring(z.read(part))
                                for c in s_root.findall(f'.//{ns}c'):
                                    ref = c.attrib.get('r')
                                    if part.endswith("sheet2.xml") and ref in ['L36', 'L37', 'L38', 'L39']:
                                        continue
                                    s_idx = int(c.attrib.get('s', 0))
                                    if s_idx < len(xfs):
                                        f_id = int(xfs[s_idx].attrib.get('fontId', 0))
                                        font = fonts[f_id]
                                        color = font.find(f'{ns}color')
                                        if color is not None:
                                            rgb = color.attrib.get('rgb', '')
                                            if 'C000' in rgb or rgb == 'FFFFFF00':
                                                errors.append(f"{part} ô {ref} chứa font chữ màu vàng/cam sai quy chuẩn: {rgb}")

                        # 7. Kiểm tra toàn vẹn đường viền (Zero Border Corruption) so với baseline
                        if "xl/worksheets/sheet2.xml" in z_ref.namelist() and "xl/styles.xml" in z_ref.namelist():
                            st_ref = ET.fromstring(z_ref.read("xl/styles.xml"))
                            s2_ref = ET.fromstring(z_ref.read("xl/worksheets/sheet2.xml"))
                            
                            def extract_cell_borders(s_elem, st_elem):
                                cellXfs_elem = st_elem.find(f'{ns}cellXfs')
                                borders_elem = st_elem.find(f'{ns}borders')
                                res = {}
                                for c in s_elem.findall(f'.//{ns}c'):
                                    c_ref = c.attrib.get('r')
                                    s_id = int(c.attrib.get('s', 0))
                                    if s_id < len(cellXfs_elem):
                                        xf_elem = cellXfs_elem[s_id]
                                        b_id = int(xf_elem.attrib.get('borderId', 0))
                                        b_elem = borders_elem[b_id]
                                        sides = {}
                                        for side in ['left', 'right', 'top', 'bottom']:
                                            side_elem = b_elem.find(f'{ns}{side}')
                                            if side_elem is not None:
                                                style_val = side_elem.attrib.get('style', 'none')
                                                color_elem = side_elem.find(f'{ns}color')
                                                rgb_val = color_elem.attrib.get('rgb') if color_elem is not None else (color_elem.attrib.get('indexed') if color_elem is not None else '')
                                                sides[side] = (style_val, rgb_val)
                                            else:
                                                sides[side] = ('none', '')
                                        res[c_ref] = sides
                                return res

                            b_cur = extract_cell_borders(s2_root, st_root)
                            b_ref = extract_cell_borders(s2_ref, st_ref)
                            for c_ref, sides_c in b_cur.items():
                                if c_ref in b_ref:
                                    sides_r = b_ref[c_ref]
                                    for s in ['left', 'right', 'top', 'bottom']:
                                        if sides_c[s] != sides_r[s]:
                                            errors.append(f"Sheet 2 ô {c_ref} sai định dạng viền {s}: hiện tại {sides_c[s]} != baseline {sides_r[s]}")

                            # Kiểm tra nghiêm ngặt khối thông tin C6..C8: cấm đường viền màu đậm FF4E5D78 hoặc medium/thick
                            for c_ref in ['C6', 'C7', 'C8']:
                                if c_ref in b_cur:
                                    for s in ['left', 'right', 'top', 'bottom']:
                                        style_val, rgb_val = b_cur[c_ref][s]
                                        if '4E5D78' in str(rgb_val) or style_val in ['medium', 'thick']:
                                            errors.append(f"Sheet 2 ô {c_ref} viền {s} bị đậm sai quy chuẩn: style='{style_val}', color='{rgb_val}' (yêu cầu thin FFD5DEE8)")

        # 6. Kiểm tra tải thử bằng openpyxl nếu có
        try:
            import openpyxl
            wb = openpyxl.load_workbook(file_path)
            if not wb.sheetnames:
                errors.append("openpyxl không tìm thấy sheet nào trong workbook")
        except ImportError:
            pass
        except Exception as e:
            errors.append(f"openpyxl nạp workbook thất bại: {e}")

    except Exception as e:
        errors.append(f"Lỗi đọc file Excel {file_path}: {e}")

    return (len(errors) == 0, errors)
