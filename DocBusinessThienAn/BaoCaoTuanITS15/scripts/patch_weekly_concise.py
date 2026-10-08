import os
import sys
import zipfile
import xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding='utf-8')

report_path = r'c:\ThienAn\DocBusinessThienAn\HữuNghị-ChiLăng\BaoCaoTuanITS15\PCN_WeeklyReport_20261005.xlsx'

if not os.path.exists(report_path):
    print(f"Error: {report_path} not found")
    sys.exit(1)

# 1. Đọc toàn bộ các file trong zip nguyên vẹn
with zipfile.ZipFile(report_path, 'r') as z:
    all_files = {name: z.read(name) for name in z.namelist()}

ns = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
ET.register_namespace('', 'http://schemas.openxmlformats.org/spreadsheetml/2006/main')

# 2. Parse sharedStrings.xml
ss_tree = ET.fromstring(all_files['xl/sharedStrings.xml'])

# Nội dung G32 ngắn gọn, bỏ liệt kê issue, bỏ test, bỏ kiến trúc/WOS
new_g32 = '''Phân hệ Chia sẻ Dữ liệu (ShareData - ESHARE):
- Xử lý các lỗi kiểm thử F16 trên hệ thống CSDL và WebAPI.
- Hoàn thiện tích hợp luồng nhật ký tiến trình cha - con (ParentId, StepNo) giữa Backend Worker và WebAPI; xây dựng endpoint GetSteps phục vụ hiển thị chi tiết tiến trình.
- Đồng bộ múi giờ hệ thống Scheduler (Hangfire / Quartz) và chuẩn hóa cấu hình dịch vụ.

Phân hệ Tường màn hình (VideoWall):
- Hoàn thành Backend Service giao tiếp thiết bị điều khiển phần cứng Hikvision (ISAPI Controller) qua NATS, chống blocking WebAPI.
- Hoàn thiện tính năng phân quyền màn hình (User/Org), cấu trúc cây Zone, quản lý Scene và bố cục ma trận hiển thị.
- Xây dựng ứng dụng WPF kết nối trực quan ma trận tường màn hình và điều khiển thiết bị.'''

# Nội dung G33 ngắn gọn, bỏ test hiện trường, bỏ trạm thời tiết WOS
new_g33 = '''Phân hệ Chia sẻ Dữ liệu (ShareData):
- Tiếp tục theo dõi, giám sát độ ổn định và hiệu năng của ShareDataWorker trên môi trường Staging khi vận hành đa đối tác với tải dữ liệu lớn; kiểm soát cơ chế gửi nối đuôi và gửi khi có dữ liệu mới.
- Hỗ trợ xử lý các vấn đề phát sinh sau đợt cập nhật F16.

Phân hệ Tường màn hình (VideoWall):
- Tiếp tục tinh chỉnh các thao tác điều khiển chuyển Scene, phân chia cửa sổ và tối ưu độ trễ phản hồi trạng thái thiết bị qua NATS trên môi trường nội bộ.
- Hoàn thiện các luồng điều khiển và ứng dụng WPF kết nối thiết bị.'''

# Tìm đúng vị trí si chứa nội dung của G32 và G33
# Trong sharedStrings: si[45] là G32, si[46] là G33
si_45_t = ss_tree[45].find(f'{ns}t')
if si_45_t is not None:
    si_45_t.text = new_g32.strip()

si_46_t = ss_tree[46].find(f'{ns}t')
if si_46_t is not None:
    si_46_t.text = new_g33.strip()

all_files['xl/sharedStrings.xml'] = ET.tostring(ss_tree, encoding='utf-8', xml_declaration=True)

# 3. Ghi lại file zip (bảo toàn 100% bit-perfect styles, theme, logo, rich text)
with zipfile.ZipFile(report_path, 'w', zipfile.ZIP_DEFLATED) as out_z:
    for name, data in all_files.items():
        out_z.writestr(name, data)

print(f"SUCCESS_PATCHED_WEEKLY_REPORT! Size: {os.path.getsize(report_path)}")
