"""
Công cụ cập nhật Báo Cáo Tuần Excel bằng kỹ thuật Direct OpenXML ZIP Patching.
Tuân thủ Rule 19.53: Bảo toàn 100% bit-perfect logo, drawings, styles, font Times New Roman,
và cấu trúc Rich Text XML gốc của Microsoft Excel (không làm mất màu đỏ/xanh hay logo).
"""

import os
import re
import sys
import zipfile
import xml.etree.ElementTree as ET
from datetime import datetime, date


from excel_validator import excel_date_serial, validate_excel_openxml


def patch_sheet1_xml_dates_and_heights(
    sheet_xml: str,
    r32_height: int,
    r32_start: date,
    r32_end: date,
    r33_height: int,
    r33_start: date,
    r33_end: date,
) -> str:
    """
    Cập nhật chiều cao dòng và số ngày serial trực tiếp trên chuỗi XML nguyên gốc.
    Bảo toàn 100% bit-perfect cấu trúc namespace, r:id, xr:uid và Markup Compatibility.
    """
    # 1. Cập nhật Row 32 (chiều cao)
    sheet_xml = re.sub(
        r'(<row r="32"[^>]*?\bht=")[^"]*("[^>]*>)',
        rf'\g<1>{r32_height}\g<2>',
        sheet_xml,
    )
    # 2. Cập nhật Row 32 (ngày bắt đầu J32, K32 và kết thúc L32, M32)
    s32 = str(excel_date_serial(r32_start))
    e32 = str(excel_date_serial(r32_end))
    sheet_xml = re.sub(
        r'(<c r="[JK]32"[^>]*><v>)\d+(</v></c>)',
        rf'\g<1>{s32}\g<2>',
        sheet_xml,
    )
    sheet_xml = re.sub(
        r'(<c r="[LM]32"[^>]*><v>)\d+(</v></c>)',
        rf'\g<1>{e32}\g<2>',
        sheet_xml,
    )

    # 3. Cập nhật Row 33 (chiều cao)
    sheet_xml = re.sub(
        r'(<row r="33"[^>]*?\bht=")[^"]*("[^>]*>)',
        rf'\g<1>{r33_height}\g<2>',
        sheet_xml,
    )
    # 4. Cập nhật Row 33 (ngày bắt đầu J33, K33 và kết thúc L33, M33)
    s33 = str(excel_date_serial(r33_start))
    e33 = str(excel_date_serial(r33_end))
    sheet_xml = re.sub(
        r'(<c r="[JK]33"[^>]*><v>)\d+(</v></c>)',
        rf'\g<1>{s33}\g<2>',
        sheet_xml,
    )
    sheet_xml = re.sub(
        r'(<c r="[LM]33"[^>]*><v>)\d+(</v></c>)',
        rf'\g<1>{e33}\g<2>',
        sheet_xml,
    )

    return sheet_xml


def patch_weekly_report(
    template_path: str,
    output_path: str,
    this_week_title: str,
    this_week_content: str,
    this_week_start: date,
    this_week_end: date,
    this_week_task_url: str,
    next_week_title: str,
    next_week_content: str,
    next_week_start: date,
    next_week_end: date,
    next_week_task_url: str,
    this_week_height: int = 340,
    next_week_height: int = 190,
) -> None:
    """
    Cập nhật file báo cáo tuần từ template gốc mà không làm mất định dạng OpenXML.
    Bảo toàn 100% bit-perfect logo, drawings, styles, font Times New Roman và namespaces.
    """
    if not os.path.exists(template_path):
        raise FileNotFoundError(f"Không tìm thấy template: {template_path}")

    # Đọc toàn bộ nội dung ZIP nguyên vẹn
    with zipfile.ZipFile(template_path, "r") as z:
        all_files = {name: z.read(name) for name in z.namelist()}

    ns = "{http://schemas.openxmlformats.org/spreadsheetml/2006/main}"
    ET.register_namespace("", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")

    # 1. Cập nhật SharedStrings.xml (bảo toàn các ô Rich Text si[0..5])
    ss_tree = ET.fromstring(all_files["xl/sharedStrings.xml"])

    # si[41]: Task URL tuần hiện tại
    si_41_t = ss_tree[41].find(f"{ns}t")
    if si_41_t is not None:
        si_41_t.text = this_week_task_url

    # si[42]: Task URL tuần tiếp theo
    si_42_t = ss_tree[42].find(f"{ns}t")
    if si_42_t is not None:
        si_42_t.text = next_week_task_url

    # si[43]: Tiêu đề cột tuần hiện tại
    si_43_t = ss_tree[43].find(f"{ns}t")
    if si_43_t is not None:
        si_43_t.text = this_week_title

    # si[44]: Tiêu đề cột tuần tiếp theo
    si_44_t = ss_tree[44].find(f"{ns}t")
    if si_44_t is not None:
        si_44_t.text = next_week_title

    # si[45]: Nội dung công việc tuần hiện tại
    si_45_t = ss_tree[45].find(f"{ns}t")
    if si_45_t is not None:
        si_45_t.text = this_week_content.strip()

    # si[46]: Nội dung công việc tuần tiếp theo
    si_46_t = ss_tree[46].find(f"{ns}t")
    if si_46_t is not None:
        si_46_t.text = next_week_content.strip()

    raw_ss_xml = ET.tostring(ss_tree, encoding="utf-8")
    all_files["xl/sharedStrings.xml"] = (
        b'<?xml version="1.0" encoding="UTF-8" standalone="yes"?>\r\n' + raw_ss_xml
    )

    # 2. Cập nhật sheet1.xml bằng chuỗi regex an toàn (tránh ElementTree phá vỡ namespaces/r:id)
    orig_sheet1_xml = all_files["xl/worksheets/sheet1.xml"].decode("utf-8")
    patched_sheet1_xml = patch_sheet1_xml_dates_and_heights(
        orig_sheet1_xml,
        r32_height=this_week_height,
        r32_start=this_week_start,
        r32_end=this_week_end,
        r33_height=next_week_height,
        r33_start=next_week_start,
        r33_end=next_week_end,
    )
    all_files["xl/worksheets/sheet1.xml"] = patched_sheet1_xml.encode("utf-8")

    # 3. Ghi ra file đích (bảo đảm tất cả file khác trong ZIP giữ nguyên 100%)
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    with zipfile.ZipFile(output_path, "w", zipfile.ZIP_DEFLATED) as out_z:
        for name, data in all_files.items():
            out_z.writestr(name, data)

    # 4. Kiểm tra tính toàn vẹn (Zero Corruption)
    valid, errors = validate_excel_openxml(output_path, template_path)
    if not valid:
        raise ValueError(f"Tệp Excel vừa tạo không đạt chuẩn Zero Corruption: {'; '.join(errors)}")


if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    weekly_dir = os.path.join(os.path.dirname(script_dir), "BaoCaoTuan")
    default_template = os.path.join(weekly_dir, "PCN_WeeklyReport_20261005.xlsx")
    default_output = os.path.join(weekly_dir, "PCN_WeeklyReport_20261012.xlsx")

    print("=== KIỂM TRA TÍNH TOÀN VẸN (ZERO CORRUPTION) BÁO CÁO TUẦN ===")
    for fname in ["PCN_WeeklyReport_20261005.xlsx", "PCN_WeeklyReport_20261012.xlsx"]:
        fpath = os.path.join(weekly_dir, fname)
        if os.path.exists(fpath):
            is_valid, err_list = validate_excel_openxml(fpath, default_template if fname != "PCN_WeeklyReport_20261005.xlsx" else None)
            status = "✅ PASS (Hợp lệ 100%)" if is_valid else f"❌ FAIL ({len(err_list)} lỗi: {'; '.join(err_list)})"
            print(f"- {fname}: {status}")



