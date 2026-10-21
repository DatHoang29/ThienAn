# 🗄️ Danh Mục Kịch Bản SQL Phân Hệ ShareData (ESHARE)

> **Căn cứ quy chuẩn:** [`.agents/rules/database-mcp_thienan.md` §2.3](../../../../.agents/rules/database-mcp_thienan.md) (Rule 19.85: Entity là SSOT, CodeFirst tạo khung trước, script SQL gom theo vai trò cố định, không tạo/sửa bảng có entity).  
> **Cập nhật lần cuối:** 21/10/2026

---

## 📌 1. Bảng Thứ Tự Chạy Kịch Bản (1 → 4)

Mọi kịch bản SQL của phân hệ ShareData được chuẩn hóa tên file theo vai trò nghiệp vụ (bỏ tiền tố số thứ tự để dễ dàng mở rộng và bổ sung script) và quy định rõ ràng thứ tự thực thi:

> 💡 **Ghi chú về việc dọn dẹp các script di sản:**  
> Các script trước đây gồm `00-sharedata-migration-mot-lan.sql`, `01-sharedata-cau-truc.sql` và `06-sharedata-doi-ten-cot-dong-bo.sql` là các kịch bản di trú di sản từ trước ngày 18/10/2026. Mọi cơ sở dữ liệu hiện tại đã hoàn tất di trú, các cột và bảng đều do CodeFirst đảm nhiệm 100% theo Rule 19.85 (không dùng script DDL ngoài CSDL). Toàn bộ 3 kịch bản di sản này đã được loại bỏ sạch sẽ, giữ lại đúng bộ 4 kịch bản chuẩn mực duy nhất dưới đây:

| Thứ Tự | Tên Tệp Kịch Bản | CSDL Đích | Mục Đích Nghiệp Vụ | Đặc Tính Chạy Lại | Môi Trường Cho Phép |
| :---: | :--- | :---: | :--- | :---: | :---: |
| **1** | [`sharedata-seed-goi-tin.sql`](sharedata-seed-goi-tin.sql) | `DEV_ITS10` | Khởi tạo bản ghi cho 11 gói tin Catalog (101–111) nếu chưa tồn tại (idempotent), nạp câu truy vấn SQL trích xuất mẫu Catalog (`TemplateSql` và `ExtractSql` cho gói 101–111), nạp 18 câu ghi chiều nhận (`ShareDataPacketSql`) cho 10 gói (101–109 và 111), chuẩn hóa `SyncMode` mặc định (Snapshot 101/102/105/108/110, Incremental 103/104/106/107/109/111) và cập nhật `Status = 1` (Enable) cho gói 101–110, `Status = 0` (Disable) cho gói 111*. Có cờ `@GhiDeMauGoiTin = 0` bảo vệ dữ liệu quản trị đã sửa. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **2** | [`sharedata-seed-cau-hinh.sql`](sharedata-seed-cau-hinh.sql) | `DEV_ITS10` | Khởi tạo `SysConfigType` `sharedata_error_code`, nạp đủ 25 mã cảnh báo lỗi `ESH-xxxx` (bổ sung `ESH-1403` theo ESHARE §9) vào `SysConfigData`, nạp danh mục 15 bảng nguồn vào `ShareDataSourceTable` (CodeFirst tạo khung trước, nạp hai công tắc `IsTracked`, `IsQueryable`, `DefaultAlias` — đã loại bỏ cột `Category`), và nạp 48 tham số vận hành vào `SysOpConfig` nhóm `sharedata_config` (19 Worker, 23 FE/WebAPI, 6 BE WebAPI) kèm khai nhóm vào `SysConfigData` loại `operation_config_type` để màn hình *Cấu hình hoạt động* hiển thị. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **3** | [`sharedata-dich-thuat.sql`](sharedata-dich-thuat.sql) | `DEV_ITS10` | Nạp từ điển bản dịch đa ngôn ngữ (`vi-VN`, `en-US`) cho 94 mã khóa chuẩn hóa (188 bản ghi song ngữ gồm SQL trích xuất, LookbackDays, SQL Validator, Metadata, ConnectConfig, SqlTableNotAllowed) vào bảng `SysTerminology` qua `MERGE` không ghi đè bản dịch đã sửa. (Sau khi chạy script trên DB, trên giao diện cần bấm nút 🔄 tại avatar góc phải trên → chọn mục Ngôn ngữ để làm mới cache bản dịch). | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **4** | [`sharedata-du-lieu-gia-lap.sql`](sharedata-du-lieu-gia-lap.sql) | `DEV_ITS10`, `DEV_ITS10_Inbound` | Nạp dữ liệu giả lập (Mock Data) cho gói 104 (`TmsWeather`), gói 106 (`TmsTrafficData` WIM), gói 110 (`TmsIncident`, `TmsEquipment`, `VmsCurrent`), gói 111 (`TmsSignalLog`), kiểm thử con trỏ trang và Khối 4 chuẩn bị gói kiểm thử riêng `E2E_INBOUND_TEST` (`ShareDataPacket` mã `E2E_INBOUND_TEST`, `ShareDataPacketSql` bảng `InboundTrafficFlow`) kèm bảng đích `InboundTrafficFlow` (trên `DEV_ITS10_Inbound` - ngoại lệ duy nhất không có entity) phục vụ kiểm thử thông luồng hai chiều E0. Tích hợp sẵn khối dọn dẹp qua cờ `@XoaDuLieuGiaLap`. | Chạy lại an toàn (**Idempotent**) | Khối 1–3, 5–7: chỉ Local/Test ⛔ cấm Staging; Khối 4 (E2E_INBOUND_TEST): được chạy trên Staging, dọn theo [doc 05 mục 4](../doc/05-chay-sql-tu-dau-sharedata.md#4-khôi-phục-và-dọn-dẹp-bằng-sql) |

---

## 🚀 2. Hướng Dẫn Thực Thi

> 🔴 **Nguyên tắc cốt lõi (Rule 19.85):**
> Entity là **Single Source of Truth (SSOT)** cho toàn bộ cấu trúc bảng và index.
> CodeFirst tạo khung trước (`TableSettings.EnableInitTable = true`), script SQL chỉ làm nhiệm vụ seed dữ liệu danh mục ban đầu và dữ liệu giả lập kiểm thử.
> Tuyệt đối **KHÔNG** `CREATE TABLE`, `ALTER TABLE ... ADD`, `CREATE INDEX` cho các bảng đã có entity trong script `.sql` (ngoại lệ duy nhất: `InboundTrafficFlow` không có entity trong `sharedata-du-lieu-gia-lap.sql`).

### A. Trình Tự Triển Khai Chuẩn
Thứ tự thực thi đơn giản, nhất quán cho mọi môi trường (Local / Staging / Production):
```
CodeFirst tạo khung (WebAPI EnableInitTable = true) ➔ (1) sharedata-seed-goi-tin.sql ➔ (2) sharedata-seed-cau-hinh.sql ➔ (3) sharedata-dich-thuat.sql ➔ (4) sharedata-du-lieu-gia-lap.sql (tùy chọn)
```

1. **Bước 0 — CodeFirst tạo khung bảng CSDL:**
   Khởi động WebAPI (bật tạm `TableSettings.EnableInitTable = true` trong cấu hình debug local) để CodeFirst tự động tạo đầy đủ bảng, cột (`SyncMode`, `CursorTimeColumn`, `MaxLookBackDays` trên `ShareDataPacket`; bảng `ShareDataSourceTable` với unique index `UX_ShareDataSourceTable_TableName`; các cột `ConnectionType`, `ConnectionConfig` trên `ShareDataPartner`) và index từ các Entity ShareData.
2. **Bước 1 — `sharedata-seed-goi-tin.sql`:**
   Khởi tạo 11 gói tin chuẩn Catalog (101–111) nếu chưa có, nạp câu truy vấn mẫu Catalog 101–111 và nạp 18 câu ghi chiều nhận (`ShareDataPacketSql`) cho các gói đã có câu ghi. Cờ `@GhiDeMauGoiTin = 0` bảo vệ câu lệnh trích xuất tùy biến đã lưu.
3. **Bước 2 — `sharedata-seed-cau-hinh.sql`:**
   Tự động tạo `SysConfigType` `sharedata_error_code`, nạp 25 mã lỗi ESH-xxxx, 15 bảng nguồn `ShareDataSourceTable` và 48 tham số vận hành `SysOpConfig` nhóm `sharedata_config`.
4. **Bước 3 — `sharedata-dich-thuat.sql`:**
   Nạp 94 khóa thuật ngữ tiếng Việt & tiếng Anh (188 bản ghi song ngữ); sau khi chạy bấm 🔄 ở avatar → Ngôn ngữ để làm mới cache.
5. **Bước 4 (Tùy chọn cho Local/Test) — `sharedata-du-lieu-gia-lap.sql`:**
   Nạp dữ liệu mẫu thời tiết, tải trọng WIM, sự cố/VMS, tín hiệu, kiểm thử phân trang con trỏ và gói kiểm thử riêng `E2E_INBOUND_TEST` kèm bảng đích chiều nhận chạy thử.

> 📌 *Lưu ý về môi trường Local:* Cấu hình `.vscode/launch.json` ghim cứng các cờ CodeFirst của ConfigId 0 (`DEV_ITS10`) về `false` bằng biến môi trường để bảo vệ CSDL. Khi cần init bảng thì bật tạm thời.

### B. Tính An Toàn Khi Chạy Lại (Idempotent)
- Cả 4 file `sharedata-seed-goi-tin.sql`, `sharedata-seed-cau-hinh.sql`, `sharedata-dich-thuat.sql`, `sharedata-du-lieu-gia-lap.sql` đều có cơ chế Idempotent (`IF NOT EXISTS`, `WHERE NOT EXISTS`, `WHEN NOT MATCHED THEN INSERT`).
- `sharedata-seed-goi-tin.sql`: Nạp SQL mẫu cho 11 gói (101–111) chỉ khi trống, chuẩn hóa `OrderNo` các gói chuẩn về đúng dải 101–111. Biến `@GhiDeMauGoiTin = 0` (mặc định) đảm bảo **KHÔNG ghi đè** các câu lệnh SQL trích xuất mà quản trị viên đã tùy biến. Biến `@GhiDeMauGoiTin = 1` là **cách duy nhất** để làm mới lại `TemplateSql` và `ExtractSql` về mẫu Catalog chuẩn.
- `sharedata-seed-cau-hinh.sql`: Chỉ thêm mã lỗi, bảng nguồn `ShareDataSourceTable` hoặc tham số `sharedata_config` chưa có trong DB, không ghi đè các cấu hình đang chạy.
- `sharedata-dich-thuat.sql`: Chỉ nạp bổ sung các bản dịch còn thiếu trong 94 khóa, không làm mất bản dịch đã chỉnh sửa.
- `sharedata-du-lieu-gia-lap.sql`: Kiểm tra dữ liệu tồn tại trước khi chèn, có thể chạy dọn dẹp sạch sẽ bằng cách gán `@XoaDuLieuGiaLap = 1`.

### C. Khởi Tạo DB Mới Bằng Cờ CodeFirst (Thử Nghiệm)

> ⚠️ **LƯU Ý QUAN TRỌNG:**
> - Theo Rule 19.85, các lớp Seed class trong mã nguồn C# đã được loại bỏ hoàn toàn để Entity là Single Source of Truth duy nhất cho cấu trúc, và script SQL (`sharedata-seed-goi-tin.sql`, `sharedata-seed-cau-hinh.sql`) là nguồn duy nhất cho dữ liệu seed ban đầu. Toàn bộ câu SQL mẫu của 11 gói tin Catalog được lưu trữ và nạp qua script `sharedata-seed-goi-tin.sql` (bỏ hoàn toàn mã cứng `PacketDefaultTemplate.cs` trong C#).
> - **CẢNH BÁO:** Tuyệt đối **KHÔNG** bật cờ `EnableInitSeed` trên CSDL đã có dữ liệu hoặc đã có các gói tin do giao diện/script tạo ra với ID khác (có thể sinh dòng trùng mã `Code`). Triển khai CSDL luôn dùng CodeFirst tạo khung và script `sharedata-seed-goi-tin.sql` → `sharedata-dich-thuat.sql` (thứ tự 1 → 3) nạp dữ liệu seed.

#### Các bước thử nghiệm:
1. **Dùng CSDL thử nghiệm tách biệt:** Ví dụ `DEV_ITS10_SeedSpike` bằng biến môi trường ghi đè chuỗi kết nối `ConfigId` `Default` trên cấu hình debug local (theo kiểu `.vscode/launch.json` đang dùng), ⛔ **KHÔNG sửa trực tiếp vào file `Database.json`**.
2. **Cờ cần bật tạm thời cho ConfigId Default:**
   - `DbSettings.EnableInitDb = true`
   - `TableSettings.EnableInitTable = true` (`EnableIncreTable = false`)
   - *(Nhớ trả về `false` ngay sau khi chạy thử nghiệm).*
3. **Danh sách kiểm nghiệm (Checklist nghiệm thu):**
   - ☐ **(1) Lần 1 trên DB trống:** Sau khi ứng dụng WebAPI khởi động với CodeFirst, CSDL có đầy đủ các bảng và index từ các Entity ShareData. Chạy script `sharedata-seed-goi-tin.sql` (thứ tự 1) để có đúng 11 dòng `ShareDataPacket` với ID dạng GUID tuần tự chữ thường (chống trùng theo `Code`), câu truy vấn `ExtractSql` và `SyncMode` chuẩn xác.
   - ☐ **(2) Chạy lại lần 2 script 1:** Chạy lại `sharedata-seed-goi-tin.sql` → CSDL vẫn duy trì đúng 11 dòng, không bị nhân đôi bản ghi (idempotent).
   - ☐ **(3) Sửa câu lệnh ExtractSql qua UI rồi chạy lại script 1:** Sửa câu lệnh `ExtractSql` của một gói qua giao diện SQL Center, sau đó chạy lại script 1 với `@GhiDeMauGoiTin = 0` → Bản ghi đã sửa vẫn còn nguyên vẹn.
   - ☐ **(4) Tắt cờ, chạy lại:** Đặt lại các cờ về `false`, khởi động lại WebAPI → Dữ liệu gói tin không thay đổi.
   - ☐ **(5) Ghi nhận kết luận:** Cấu trúc bảng do CodeFirst tạo; dữ liệu seed do script SQL nạp có kiểm tra idempotent (`IF NOT EXISTS`).

#### Truy vấn đối soát chỉ đọc:
```sql
SELECT
    [ID],
    [Code],
    [Name],
    [SyncMode],
    [CursorTimeColumn],
    [MaxLookBackDays],
    [Status],
    CASE WHEN [ExtractSql] IS NOT NULL THEN LEN([ExtractSql]) ELSE 0 END AS [SqlLength]
FROM [dbo].[ShareDataPacket]
ORDER BY [OrderNo];
```

---

## ⚠️ 3. Lưu Ý Quan Trọng Về CSDL Đích (Database Target)

- Hầu hết các script thao tác trên CSDL chính **`[DEV_ITS10]`**.
- Riêng **Khối 4.1 của `sharedata-du-lieu-gia-lap.sql`** thao tác trên CSDL nhận **`[DEV_ITS10_Inbound]`** (tạo bảng đích `InboundTrafficFlow`). Đảm bảo user thực thi có quyền truy cập trên cả hai CSDL hoặc chạy riêng từng khối theo đúng database context.
- Theo quy định **P0 Safeguards (Rule 16)**: AI tuyệt đối không tự thực thi lệnh ghi lên CSDL. Lập trình viên là người kiểm tra và thực thi script cuối cùng trên DB.
