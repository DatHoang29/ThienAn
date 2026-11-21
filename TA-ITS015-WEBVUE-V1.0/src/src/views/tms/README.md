# Khóa dịch TMS

Ưu tiên khóa theo thứ tự **base → tms → chức năng riêng**:

- `lz.entity.base.*`, `lz.label.base.*`, `lz.button.base.*`, `lz.message.base.*`, `lz.validation.base.*`: trường phổ thông, thao tác, thời gian, placeholder và kiểm tra dữ liệu.
- `lz.entity.tms.*`, `lz.label.tms.*`, `lz.button.tms.*`: thuật ngữ giao thông dùng giữa nhiều chức năng: thiết bị, biển số, hướng, làn, lý trình, cỡ xe, loại xe, lịch và nhân viên.
- Namespace chức năng: quy trình, trạng thái nghiệp vụ, ràng buộc đổi ca, đăng kiểm, bảo trì, AID và chính sách xử lý tự động.

`vehicleSize` là **Cỡ xe** (`short`, `medium`, `long`, `other`); `vehicleType` là **Loại xe**. Chọn nhãn theo dữ liệu đang hiển thị, giữ nguyên tên trường/API và mã cấu hình.

Placeholder dùng `lz.label.base.pleaseType` / `pleaseSelect` với tham số `input`; validation dùng mẫu chung với tên trường. Đơn vị đo và chữ in hoa thuộc phần trình bày. Từ điển Việt/Anh của TMS nằm trong `src/i18n/lang/its/tms/vi-vn.json` và `en-us.json`, ở dạng khóa phẳng; key dùng chung (`base`, hệ thống) nằm ở `src/i18n/lang/vi-vn.json`.

Các khóa cũ tương đương được giữ bằng liên kết `@:khóa.chuẩn` để tương thích menu và các màn ngoài TMS. Các khóa có nghĩa khác hoặc placeholder cũ không nhận tham số giữ bản dịch riêng. Không thay mã `lz.param.*`, giá trị bộ lọc, ID cột, dữ liệu mẫu hoặc nội dung người dùng nhập bằng bản dịch.

Kiểm tra từ thư mục ứng dụng `src`:

```powershell
node --test tests/tms-i18n.test.mjs tests/traffic-monitor-api.test.mjs tests/menu-tabs.test.mjs tests/user-management-api.test.mjs
```

Các kiểm tra này xác minh từ điển, liên kết, tham số, cú pháp Vue và hợp đồng dùng API giả lập; không gọi backend thật.
