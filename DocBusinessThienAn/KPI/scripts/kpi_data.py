"""
Cấu hình dữ liệu báo cáo KPI cá nhân.
Mỗi kỳ đánh giá mới hoặc khi cần cập nhật công việc, chỉ cần chỉnh sửa dữ liệu tại tệp này.
Tệp generate_kpi.py sẽ tự động nạp dữ liệu từ đây để xuất ra file Excel hoàn chỉnh.
"""

# ==============================================================================
# 1. THÔNG TIN CHUNG
# ==============================================================================
PERIOD = "2026-09"                     # Kỳ đánh giá (YYYY-MM)
EMPLOYEE_NAME = "Hoàng Quý Đạt"       # Họ và tên nhân viên
START_DATE = "13/07/2026"              # Ngày bắt đầu vào làm (DD/MM/YYYY)
REPORT_DATE = "30/09/2026"             # Ngày lập báo cáo (Bắt buộc trong tháng đánh giá)
DEPARTMENT = "Phòng phát triển phần mềm"
POSITION = "Nhân viên phát triển phần mềm"
MANAGER_NAME = "Trần Hồng Sơn"
PROJECT_NAME = "[SW2026] ITS015 - Hữu Nghị-Chi Lăng"

# Tên tệp Excel xuất ra (lưu tại thư mục DocBusinessThienAn/KPI/)
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
        "desc": """- Triển khai cơ chế gửi nối đuôi dữ liệu và gửi tức thì khi có dữ liệu mới
- Xử lý luồng gửi dữ liệu: lấy dữ liệu từ CSDL và chuyển đổi đúng cấu trúc đối tác yêu cầu
- Tự động đóng gói và ghi file dữ liệu gửi đi để lưu trữ làm bằng chứng đối soát
- Chuẩn hóa luồng log cha - con và theo dõi fix bug kiểm thử F16""",
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
        "desc": """- Nghiên cứu tài liệu chuẩn giao tiếp ISAPI của thiết bị điều khiển màn hình
- Xây dựng API và cơ chế phân quyền VideoWall theo người dùng, tổ chức và ô màn hình
- Phát triển Worker Service giao tiếp thiết bị qua ISAPI/NATS
- Xây dựng form WPF test thiết bị
- Trực tiếp ra site kiểm thử thiết bị (2 ngày): kiểm tra kết nối phần cứng và điều khiển màn hình tại hiện trường""",
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
# Plain text không chứa định dạng markdown **
# ==============================================================================
HIGHLIGHT_TEXT = """- Phân hệ VideoWall (Tìm hiểu & Kiểm thử site): Nghiên cứu tài liệu chuẩn giao tiếp ISAPI (Hikvision) nắm rõ cơ chế điều khiển màn hình; phát triển Worker Service qua NATS điều khiển thiết bị không nghẽn WebAPI, tự làm form WPF test thiết bị và trực tiếp ra site kiểm thử 2 ngày nghiệm thu thành công.
- Phân hệ ShareData (Thực hiện & Tối ưu): Triển khai cơ chế gửi nối đuôi kết hợp Change Tracking & NATS giúp đẩy dữ liệu tức thì và tối ưu tải CSDL nhanh hơn."""

# ==============================================================================
# 4. ĐIỂM TỰ ĐÁNH GIÁ PHẦN B (Năng lực & Hành vi - thang điểm 10)
# ==============================================================================
SECTION_B_SCORES = {
    "H18": 8,  # Năng lực chuyên môn
    "H19": 8,  # Tinh thần trách nhiệm & chủ động
    "H20": 8,  # Kỷ luật lao động
    "H21": 8   # Phối hợp & giao tiếp
}
