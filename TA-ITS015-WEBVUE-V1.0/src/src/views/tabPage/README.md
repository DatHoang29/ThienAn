# Khung hiển thị tab dùng chung

| Nhóm | Component menu | Tên component | Trang chức năng |
| --- | --- | --- | --- |
| Người dùng | `tabPage/index` | `sysUserMngt` | `system/user/index`, `system/account/index` |
| Ứng dụng | `tabPage/index` | `sysAppMngt` | `system/app/index`, `system/plugin/index` |
| Tổ chức | `tabPage/index` | `sysOrgMngt` | `system/org/index`, `system/pos/index` |
| Phân quyền | `tabPage/index` | `sysPermMngt` | `system/menu/index`, `system/role/index` |

`views/tabPage/index.vue` dùng chung cho mọi phân hệ, không thuộc System. Tiêu đề, icon và tab lấy từ SysMenu. Router truyền `menuId` cố định vào từng instance và tạo lớp bọc có tên theo `SysMenu.Name` để tương thích KeepAlive/đóng/làm mới tag. Bảng trên là ví dụ cấu hình System; tên component dùng chung là `tabPageView`. Tab đang chọn và cache của từng menu được tách riêng; không có ánh xạ đường dẫn cũ trong frontend.

Chức năng, form, VxeGrid và API nằm trong các thư mục chức năng trực tiếp dưới `system/`. Các trang dùng được cả như menu độc lập và tab trong nhóm. Tab có `isHide = true` hoặc không được cấp quyền sẽ không hiển thị.

`SysMenu.Component` của các menu nhóm là `tabPage/index` (có thể có dấu `/` ở đầu). Mỗi menu có ID và tên route riêng; các node con `type = tab`, `pid` trỏ đến menu đó và `component` trỏ đến trang chức năng của phân hệ. Giữ nguyên quan hệ cha/con và quyền. Script SQL Server `docs/database/20261027_system_menu_component_paths.sql` ở gốc repository cập nhật các đường dẫn System cũ, hỗ trợ cả dạng có và không có `/` đầu. Script chưa được chạy lên CSDL. Sau khi cập nhật CSDL, đăng nhập lại để tải cây menu mới.
