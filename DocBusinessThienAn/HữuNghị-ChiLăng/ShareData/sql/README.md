# 🗄️ Danh Mục Kịch Bản SQL Phân Hệ ShareData (ESHARE)

> **Căn cứ quy chuẩn:** [`.agents/rules/database-mcp_thienan.md` §2](../../../../.agents/rules/database-mcp_thienan.md) (Gom script theo vai trò, không tách theo ngày).  
> **Cập nhật lần cuối:** 16/10/2026

---

## 📌 1. Bảng Thứ Tự Chạy Kịch Bản (00 → 06)

Mọi kịch bản SQL của phân hệ ShareData được phân tách rõ ràng theo đúng vai trò nghiệp vụ với tiền tố số thứ tự chạy `NN-sharedata-<vai-tro>.sql`:

| Thứ Tự | Tên Tệp Kịch Bản | CSDL Đích | Mục Đích Nghiệp Vụ | Đặc Tính Chạy Lại | Môi Trường Cho Phép |
| :---: | :--- | :---: | :--- | :---: | :---: |
| **00** | [`00-sharedata-migration-mot-lan.sql`](00-sharedata-migration-mot-lan.sql) | `DEV_ITS10` | Bổ sung cột cấu hình truy vấn vào `ShareDataPacket`/`ShareDataMapping` và DROP bảng mồ côi `ShareDataTable`. | ⚠️ **Chỉ chạy 1 lần** (khi nâng cấp/di trú) | Mọi môi trường |
| **01** | [`01-sharedata-cau-truc.sql`](01-sharedata-cau-truc.sql) | `DEV_ITS10` | Chuẩn hóa cấu hình địa chỉ/cổng/endpoint từ JSON `ConnectionConfig` sang các cột riêng (`Address`, `Port`, `EndPointApiUrl`) trên `ShareDataPartner`. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **02** | [`02-sharedata-seed-goi-tin.sql`](02-sharedata-seed-goi-tin.sql) | `DEV_ITS10` | Nạp câu truy vấn SQL trích xuất mẫu Catalog (gói 101–110), chuẩn hóa `SyncMode` mặc định khi còn NULL (Snapshot / Incremental) và cập nhật `Status = 0` (Disable) cho các gói tạm hoãn (110*, 111*). Có cờ `@GhiDeMauGoiTin = 0` bảo vệ dữ liệu quản trị đã sửa. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **03** | [`03-sharedata-seed-cau-hinh.sql`](03-sharedata-seed-cau-hinh.sql) | `DEV_ITS10` | Khởi tạo `SysConfigType` `sharedata_error_code`, nạp đủ 24 mã cảnh báo lỗi `ESH-xxxx` (theo ESHARE §9) vào `SysConfigData`, nạp danh mục 15 bảng theo dõi Change Tracking `sharedata_tracker` vào `SysOpConfig`, khai nhóm `sharedata_tracker` vào `SysConfigData` loại `operation_config_type` để màn hình *Cấu hình hoạt động* hiển thị. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **04** | [`04-sharedata-dich-thuat.sql`](04-sharedata-dich-thuat.sql) | `DEV_ITS10` | Nạp từ điển bản dịch đa ngôn ngữ (`vi-VN`, `en-US`) cho 36 mã khóa chuẩn hóa (SQL trích xuất, LookbackDays, SQL Validator, Metadata) vào bảng `SysTerminology` qua `MERGE` không ghi đè bản dịch đã sửa. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **05** | [`05-sharedata-du-lieu-gia-lap.sql`](05-sharedata-du-lieu-gia-lap.sql) | `DEV_ITS10` | Nạp dữ liệu giả lập (Mock Data) cho gói 104 (`TmsWeather`), gói 106 (`TmsTrafficData` WIM), gói 110 (`TmsIncident`, `TmsEquipment`, `VmsCurrent`) phục vụ test Worker E2E. | Chạy lại an toàn (**Idempotent**) | ⚠️ **Chỉ Local / Test**<br/>⛔ **CẤM chạy Staging/Production** |
| **06** | [`06-sharedata-doi-ten-cot-dong-bo.sql`](06-sharedata-doi-ten-cot-dong-bo.sql) | `DEV_ITS10` | Đổi tên 3 trường cấu hình đồng bộ trên `ShareDataPacket` và `ShareDataMapping` (`SyncKind` → `SyncMode`, `TimeColumn` → `CursorTimeColumn`, `LookbackDays` → `MaxLookbackDays`). | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |

---

## 🚀 2. Hướng Dẫn Thực Thi

### A. Triển Khai Trên CSDL Mới Hoàn Toàn (Khởi Tạo Ban Đầu)
Chạy lần lượt theo đúng thứ tự số từ `01` đến `04`:
1. `01-sharedata-cau-truc.sql` (chuẩn hóa cột địa chỉ/cổng của đối tác trên DEV_ITS10).
2. `02-sharedata-seed-goi-tin.sql` (nạp câu truy vấn mẫu Catalog 101–110).
3. `03-sharedata-seed-cau-hinh.sql` (tự động tạo `SysConfigType` và nạp mã lỗi, cấu hình Change Tracking).
4. `04-sharedata-dich-thuat.sql` (nạp 36 khóa thuật ngữ tiếng Việt & tiếng Anh).
5. *(Tùy chọn cho môi trường Local/Test)*: Chạy `05-sharedata-du-lieu-gia-lap.sql` để có dữ liệu mẫu chạy thử.

> ℹ️ *Bỏ qua file `00-sharedata-migration-mot-lan.sql` vì CSDL mới không có các bảng tạm/cũ cần dọn.*

### B. Triển Khai Cập Nhật Trên CSDL Đang Chạy (Đã Có Dữ Liệu)
1. **Nếu chưa từng chạy dọn bảng di trú:** Chạy `00-sharedata-migration-mot-lan.sql` đúng một lần để dọn bảng thừa và chuyển dữ liệu sang `ShareDataPacket`/`ShareDataMapping`.
2. Chạy lần lượt `01-sharedata-cau-truc.sql` → `02-sharedata-seed-goi-tin.sql` → `03-sharedata-seed-cau-hinh.sql` → `04-sharedata-dich-thuat.sql`.
3. **An toàn khi chạy lại:**
   - Các file `01`, `02`, `03`, `04` đều có cơ chế Idempotent (`IF NOT EXISTS`, `WHERE NOT EXISTS`, `WHEN NOT MATCHED THEN INSERT`).
   - `02-sharedata-seed-goi-tin.sql`: Biến `@GhiDeMauGoiTin = 0` (mặc định) đảm bảo **KHÔNG ghi đè** các câu lệnh SQL trích xuất mà quản trị viên đã tùy biến. Nếu muốn reset về Catalog chuẩn thì mới đổi sang `@GhiDeMauGoiTin = 1`.
   - `03-sharedata-seed-cau-hinh.sql`: Chỉ thêm mã lỗi hoặc bảng Change Tracking chưa có trong DB, không ghi đè các cấu hình đang chạy.
   - `04-sharedata-dich-thuat.sql`: Chỉ nạp bổ sung các bản dịch còn thiếu, không làm mất bản dịch đã chỉnh sửa.

---

## ⚠️ 3. Lưu Ý Quan Trọng Về CSDL Đích (Database Target)

- Hầu hết các script thao tác trên CSDL chính **`[DEV_ITS10]`**.
- Riêng **Khối 2 của `01-sharedata-cau-truc.sql`** thao tác trên CSDL nhận **`[DEV_ITS10_Inbound]`**. Đảm bảo user thực thi có quyền truy cập trên cả hai CSDL hoặc chạy riêng từng khối theo đúng database context.
- Theo quy định **P0 Safeguards (Rule 16)**: AI tuyệt đối không tự thực thi lệnh ghi lên CSDL. Lập trình viên là người kiểm tra và thực thi script cuối cùng trên DB.
