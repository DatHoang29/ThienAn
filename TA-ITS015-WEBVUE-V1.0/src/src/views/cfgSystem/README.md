# Configuration views

Ba chức năng dùng `WorkspacePage`, `GridSearchPanel`, `GridSummary` và phân trang theo chiều rộng bảng như các trang trong `system`. Mỗi trang giữ riêng grid, truy vấn, quyền và dialog.

| Chức năng | Component menu/tab | Tên component |
| --- | --- | --- |
| Danh mục cấu hình và dữ liệu | `cfgSystem/sysConfigType/index` | `sysConfigType` |
| Cấu hình vận hành | `cfgSystem/sysOpConfig/index` | `sysOpConfig` |
| Thuật ngữ | `cfgSystem/sysTerminology/index` | `sysTerminology` |

Các đường dẫn trên chạy được dưới dạng menu độc lập hoặc tab con. Để hiển thị thành nhóm tab như System, cấu hình component của menu cha là `tabPage/index`, các menu con có loại `tab` và component theo bảng. Tiêu đề, icon, thứ tự, quyền và chính sách cache lấy từ cấu hình menu trong CSDL. Frontend không tạo menu/tab mặc định và không tự cập nhật CSDL.

Giữ nguyên đường dẫn component hiện tại nên menu cũ tiếp tục mở trực tiếp. Giữ nguyên ID bảng `sysConfigTypeGrid`, `sysConfigDataGrid`, `sysOpConfigGrid`, `sysTerminologyGrid`. Hai bảng danh mục/dữ liệu vẫn liên kết theo `configTypeId`; quay lại tab giữ bộ lọc, trang và thứ tự sắp xếp.

`old/` chứa nguyên bản 7 file Vue trước khi chuyển đổi; `old/manifest.sha256` ghi SHA-256 của từng file. `old.zip` chứa cả thư mục `old`, README và manifest. Router và bộ phân giải tab loại `cfgSystem/old/` khỏi các component có thể tải.
