# System views

Các trang chức năng nằm trực tiếp dưới `system/`, dùng tên thư mục số ít:

| Trang | Component menu/tab | Tên component |
| --- | --- | --- |
| Người dùng | `system/user/index` | `sysUser` |
| Tài khoản | `system/account/index` | `sysAccount` |
| Ứng dụng | `system/app/index` | `sysApp` |
| Plugin | `system/plugin/index` | `sysPlugin` |
| Đơn vị | `system/org/index` | `sysOrg` |
| Chức vụ | `system/pos/index` | `sysPos` |
| Menu | `system/menu/index` | `sysMenu` |
| Vai trò | `system/role/index` | `sysRole` |

Các nhóm dùng khung chung `views/tabPage/index.vue`, nằm ngoài System để dùng cho mọi phân hệ. Component của cả bốn menu nhóm trong CSDL là `tabPage/index`; giữ nguyên ID và tên route riêng (`sysUserMngt`, `sysAppMngt`, `sysOrgMngt`, `sysPermMngt`). Router tách instance/cache theo menu, khung lấy tiêu đề/icon/tab từ CSDL. Các trang độc lập trong bảng tự có header khi mở như menu riêng. ID bảng và ID thanh chia giữ nguyên để không mất cấu hình đã lưu.

`SysMenu.Component` phải chứa đúng đường dẫn hiện tại trong bảng và đoạn mô tả nhóm. Frontend phân giải trực tiếp component từ CSDL, không ánh xạ đường dẫn cũ. Script SQL Server `docs/database/20261027_system_menu_component_paths.sql` tại gốc repository cập nhật các đường dẫn menu/tab cũ (hỗ trợ dấu `/` đầu), giữ nguyên ID, quan hệ cha/con, quyền và vai trò. Script cần chạy trên CSDL đang phục vụ frontend; sau khi cập nhật, đăng nhập lại để tải cây menu mới.

`user/component/userCenter.vue` và `user/component/orgTree.vue` được giữ tại đường dẫn hiện tại để bảo toàn các trang phụ.

`old/` lưu nguyên bản 19 file nguồn của tám chức năng trước khi thay thế; `old/manifest.sha256` chứa hash của từng file. `old.zip` nén toàn bộ thư mục này, bao gồm README và manifest. Router loại `old/` khỏi danh sách module được tải. Các thư mục `file`, `infoSetting` giữ nguyên.
