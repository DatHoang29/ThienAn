"""
Công cụ cập nhật Báo Cáo Tuần Excel bằng kỹ thuật Direct OpenXML ZIP Patching.
Tuân thủ Rule 19.53: Bảo toàn 100% bit-perfect logo, drawings, styles, font Times New Roman,
và cấu trúc Rich Text XML gốc của Microsoft Excel (không làm mất màu đỏ/xanh hay logo).
"""

import os
import sys
import zipfile
import xml.etree.ElementTree as ET
from datetime import datetime, date


def excel_date_serial(d: date) -> int:
    """Chuyển đổi date thành số ngày serial của Microsoft Excel (base 1899-12-30)."""
    base = date(1899, 12, 30)
    return (d - base).days


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
    this_week_height: int = 320,
    next_week_height: int = 180,
) -> None:
    """
    Cập nhật file báo cáo tuần từ template gốc mà không làm mất định dạng OpenXML.
    
    Bảo toàn:
      - Logo Thiên Ân (xl/drawings/drawing1.xml + xl/media/image1.png)
      - Rich Text màu đỏ "THIÊN ÂN" và màu xanh "OF THE DEPLOY DEPARTMENT"
      - Chú thích màu đỏ/xanh "Đỏ" / "Xanh dương"
      - Font Times New Roman, border, merge cells, background colors
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

    all_files["xl/sharedStrings.xml"] = ET.tostring(
        ss_tree, encoding="utf-8", xml_declaration=True
    )

    # 2. Cập nhật sheet1.xml (số ngày serial và chiều cao dòng)
    sheet_tree = ET.fromstring(all_files["xl/worksheets/sheet1.xml"])
    sheet_data = sheet_tree.find(f"{ns}sheetData")

    if sheet_data is not None:
        for row in sheet_data.findall(f"{ns}row"):
            r_idx = row.get("r")
            if r_idx == "32":
                row.set("ht", str(this_week_height))
                row.set("customHeight", "1")
                # J32, K32: Start date
                # L32, M32: End date
                s_serial = str(excel_date_serial(this_week_start))
                e_serial = str(excel_date_serial(this_week_end))
                for cell in row.findall(f"{ns}c"):
                    c_ref = cell.get("r")
                    if c_ref in ["J32", "K32"]:
                        v = cell.find(f"{ns}v")
                        if v is not None:
                            v.text = s_serial
                    elif c_ref in ["L32", "M32"]:
                        v = cell.find(f"{ns}v")
                        if v is not None:
                            v.text = e_serial
            elif r_idx == "33":
                row.set("ht", str(next_week_height))
                row.set("customHeight", "1")
                s_serial = str(excel_date_serial(next_week_start))
                e_serial = str(excel_date_serial(next_week_end))
                for cell in row.findall(f"{ns}c"):
                    c_ref = cell.get("r")
                    if c_ref in ["J33", "K33"]:
                        v = cell.find(f"{ns}v")
                        if v is not None:
                            v.text = s_serial
                    elif c_ref in ["L33", "M33"]:
                        v = cell.find(f"{ns}v")
                        if v is not None:
                            v.text = e_serial

    all_files["xl/worksheets/sheet1.xml"] = ET.tostring(
        sheet_tree, encoding="utf-8", xml_declaration=True
    )

    # 3. Ghi ra file đích (bảo đảm tất cả file khác trong ZIP giữ nguyên 100%)
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    with zipfile.ZipFile(output_path, "w", zipfile.ZIP_DEFLATED) as out_z:
        for name, data in all_files.items():
            out_z.writestr(name, data)


if __name__ == "__main__":
    print("Direct OpenXML Weekly Report Patcher module ready.")

