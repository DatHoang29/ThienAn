"""
Cấu hình dữ liệu báo cáo KPI cá nhân.
Mỗi kỳ đánh giá mới hoặc khi cần cập nhật công việc, chỉ cần chỉnh sửa dữ liệu tại tệp này.
Tệp generate_kpi.py sẽ tự động nạp dữ liệu từ đây để xuất ra file Excel hoàn chỉnh.
"""

from datetime import datetime

# ==============================================================================
# 1. THÔNG TIN CHUNG
# ==============================================================================
PERIOD = "2026-09"                     # Kỳ đánh giá (YYYY-MM)
EMPLOYEE_NAME = "Hoàng Quý Đạt"       # Họ và tên nhân viên
START_DATE = "13/07/2026"              # Ngày bắt đầu vào làm (DD/MM/YYYY)
REPORT_DATE = datetime.now().strftime("%d/%m/%Y")  # Ngày lập báo cáo (Sửa ngày nào ghi ngày đó: hôm nay là 16/10/2026)
DEPARTMENT = "Phòng phát triển phần mềm"
POSITION = "Nhân viên phát triển phần mềm"
MANAGER_NAME = "Trần Hồng Sơn"
PROJECT_NAME = "[SW2026] ITS015 - Hữu Nghị-Chi Lăng"

# Tên tệp Excel xuất ra (lưu tại thư mục DocBusinessThienAn/BaoCao/KPI/)
OUTPUT_FILENAME = "2026.09_KPI_DatHoangQuy.xlsx"

# ==============================================================================
# 2. DANH SÁCH CÔNG VIỆC THỰC HIỆN TRONG KỲ
# Định mức: 1 tuần = 5.5 ngày công (~20.5 - 22 ngày/tháng).
# Lưu ý Rule 37: Link Base WeWork và nội dung text trong ô tự động đồng bộ 100%.
# ==============================================================================
TASKS = [
    {
        "code_name": "[2026] XD001.5.6 Service tích hợp dữ liệu 2",
        "link": "https://wework.base.vn/tasks?task=11004385",
        "desc": """- Triển khai gửi dữ liệu nối đuôi và truyền nhận tức thì khi có dữ liệu mới
- Chuyển đổi cấu trúc dữ liệu theo đúng chuẩn đối tác yêu cầu
- Tự động đóng gói và lưu trữ file dữ liệu đối soát
- Chuẩn hóa luồng log phục vụ nghiệm thu hệ thống""",
        "days": 10.5,
        "complexity": 4,
        "role": "DEV",
        "scores": {
            "X": 100,    # Điểm tiến độ
            "Y": 84,     # Điểm chất lượng
            "Z": 20,     # Điểm khối lượng
            "AA": 90,    # Điểm kỷ luật
            "AB": 0.4,   # Trọng số tiến độ
            "AC": 0.5,   # Trọng số chất lượng
            "AD": 0.0,   # Trọng số khối lượng
            "AE": 0.1,   # Trọng số kỷ luật
            "AF": 91     # Điểm quy đổi Task 1
        }
    },
    {
        "code_name": "XD001.6.3. Service tích hợp thiết bị",
        "link": "https://wework.base.vn/tasks?task=11004386",
        "desc": """- Nghiên cứu chuẩn giao tiếp thiết bị điều khiển màn hình (ISAPI Hikvision)
- Xây dựng API và phân quyền điều khiển theo người dùng và khu vực màn hình
- Phát triển Worker Service kết nối thiết bị qua NATS
- Xây dựng phần mềm kiểm thử thiết bị (WPF)
- Trực tiếp ra site kiểm thử và nghiệm thu thiết bị""",
        "days": 10.0,
        "complexity": 4,
        "role": "DEV",
        "scores": {
            "X": 0,
            "Y": 80,
            "Z": 0,
            "AA": 0,
            "AB": 0.4,
            "AC": 0.5,
            "AD": 0.0,
            "AE": 0.1,
            "AF": 40     # Điểm quy đổi Task 2
        }
    }
]

# ==============================================================================
# 3. KẾT QUẢ NỔI BẬT (Điền vào ô G12 Sheet 02. Đánh giá KPI theo Phương án 2)
# Văn phong thô, ngắn gọn, súc tích, làm gì ghi nấy (cho người phi kỹ thuật dễ hiểu)
# Plain text không chứa định dạng markdown **
# ==============================================================================
HIGHLIGHT_TEXT = """- Phân hệ VideoWall: Nghiên cứu chuẩn giao tiếp ISAPI Hikvision; xây dựng API phân quyền điều khiển theo người dùng và khu vực màn hình; phát triển Worker Service chạy nền kết nối thiết bị qua NATS; làm tool WPF kiểm thử và trực tiếp ra site kiểm thử thiết bị nghiệm thu thành công.

- Phân hệ ShareData: Xây dựng cơ chế gửi nối đuôi và truyền nhận dữ liệu tức thì, tối ưu tải hệ thống; tự động đóng gói lưu trữ file đối soát và chuẩn hóa log phục vụ nghiệm thu hệ thống."""

# ==============================================================================
# 4. ĐIỂM TỰ ĐÁNH GIÁ PHẦN B (Năng lực & Hành vi - thang điểm 10)
# ==============================================================================
SECTION_B_SCORES = {
    "H18": 8,  # Năng lực chuyên môn
    "H19": 8,  # Tinh thần trách nhiệm & chủ động
    "H20": 8,  # Kỷ luật lao động
    "H21": 8   # Phối hợp & giao tiếp
}
