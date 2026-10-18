# 🗄️ Danh Mục Kịch Bản SQL Phân Hệ ShareData (ESHARE)

> **Căn cứ quy chuẩn:** [`.agents/rules/database-mcp_thienan.md` §2.3](../../../../.agents/rules/database-mcp_thienan.md) (Rule 19.85: Entity là SSOT, CodeFirst tạo khung trước, script SQL gom theo vai trò cố định, không tạo/sửa bảng có entity).  
> **Cập nhật lần cuối:** 18/10/2026

---

## 📌 1. Bảng Thứ Tự Chạy Kịch Bản (00 → 06)

Mọi kịch bản SQL của phân hệ ShareData được phân tách rõ ràng theo đúng vai trò nghiệp vụ với tiền tố số thứ tự chạy `NN-sharedata-<vai-tro>.sql`:

| Thứ Tự | Tên Tệp Kịch Bản | CSDL Đích | Mục Đích Nghiệp Vụ | Đặc Tính Chạy Lại | Môi Trường Cho Phép |
| :---: | :--- | :---: | :--- | :---: | :---: |
| **00** | [`00-sharedata-migration-mot-lan.sql`](00-sharedata-migration-mot-lan.sql) | `DEV_ITS10` | Di trú dữ liệu bảng nguồn sang `ShareDataSourceTable` (CodeFirst tạo khung trước): chép dữ liệu từ `ShareDataTrackTable` sang nếu có và DROP `ShareDataTrackTable`, xóa cột `Category` thừa nếu còn tồn tại, di trú mốc từ `SysOpConfig` (`sharedata_tracker`) + `ShareDataTrackVersion` cũ sang nếu bảng rỗng, xóa mềm các dòng cấu hình cũ trong `SysOpConfig`, dọn dòng `ShareDataPacketSql` có `TableName = 'ROOT'` cũ (xóa mềm), DROP bảng mồ côi (`ShareDataTable`, `ShareDataTrackVersion`), và di trú cột `ConnectionType` trên `ShareDataPartner` từ `NVARCHAR` sang `INT` (enum 1..5). | ⚠️ **Chỉ chạy 1 lần** (khi nâng cấp/di trú) | Mọi môi trường |
| **01** | [`01-sharedata-cau-truc.sql`](01-sharedata-cau-truc.sql) | `DEV_ITS10` | Chuẩn hóa cấu hình đối tác `ShareDataPartner` (CodeFirst tạo khung trước gồm `ConnectionType`, `ConnectionConfig`): kiểm tra xung đột và chuyển đổi cấu hình địa chỉ/cổng/endpoint từ JSON `ConnectionConfig` sang các cột riêng (`Address`, `Port`, `EndPointApiUrl`), dọn dẹp các key địa chỉ/cổng khỏi `ConnectionConfig` bằng `JSON_MODIFY`. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **02** | [`02-sharedata-seed-goi-tin.sql`](02-sharedata-seed-goi-tin.sql) | `DEV_ITS10` | Khởi tạo bản ghi cho 11 gói tin (101–111) nếu chưa tồn tại (idempotent), nạp câu truy vấn SQL trích xuất mẫu Catalog (gói 101–110), chuẩn hóa `SyncMode` mặc định khi còn NULL (Snapshot / Incremental) và cập nhật `Status = 1` (Enable) cho gói 110*, `Status = 0` (Disable) cho gói 111*. Có cờ `@GhiDeMauGoiTin = 0` bảo vệ dữ liệu quản trị đã sửa. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **03** | [`03-sharedata-seed-cau-hinh.sql`](03-sharedata-seed-cau-hinh.sql) | `DEV_ITS10` | Khởi tạo `SysConfigType` `sharedata_error_code`, nạp đủ 25 mã cảnh báo lỗi `ESH-xxxx` (bổ sung `ESH-1403` theo ESHARE §9) vào `SysConfigData`, nạp danh mục 15 bảng nguồn vào `ShareDataSourceTable` (CodeFirst tạo khung trước, nạp hai công tắc `IsTracked`, `IsQueryable`, `DefaultAlias` — đã loại bỏ cột `Category`), và nạp 47 tham số vận hành vào `SysOpConfig` nhóm `sharedata_config` (18 Worker, 23 FE/WebAPI, 6 BE WebAPI) kèm khai nhóm vào `SysConfigData` loại `operation_config_type` để màn hình *Cấu hình hoạt động* hiển thị. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **04** | [`04-sharedata-dich-thuat.sql`](04-sharedata-dich-thuat.sql) | `DEV_ITS10` | Nạp từ điển bản dịch đa ngôn ngữ (`vi-VN`, `en-US`) cho 49 mã khóa chuẩn hóa (98 bản ghi song ngữ gồm SQL trích xuất, LookbackDays, SQL Validator, Metadata, ConnectConfig, SqlTableNotAllowed) vào bảng `SysTerminology` qua `MERGE` không ghi đè bản dịch đã sửa. (Sau khi chạy script trên DB, trên giao diện cần bấm nút 🔄 tại avatar góc phải trên → chọn mục Ngôn ngữ để làm mới cache bản dịch). | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **05** | [`05-sharedata-du-lieu-gia-lap.sql`](05-sharedata-du-lieu-gia-lap.sql) | `DEV_ITS10`, `DEV_ITS10_Inbound` | Nạp dữ liệu giả lập (Mock Data) cho gói 104 (`TmsWeather`), gói 106 (`TmsTrafficData` WIM), gói 110 (`TmsIncident`, `TmsEquipment`, `VmsCurrent`) và Khối 4 chuẩn bị gói kiểm thử riêng `E2E_INBOUND_TEST` (`ShareDataPacket` ID `1500000000900`, `ShareDataPacketSql` ID `1500000000903`) kèm bảng đích `InboundTrafficFlow` (trên `DEV_ITS10_Inbound` - ngoại lệ duy nhất không có entity) phục vụ kiểm thử thông luồng hai chiều E0 mà không ảnh hưởng gói 101. | Chạy lại an toàn (**Idempotent**) | ⚠️ **Chỉ Local / Test**<br/>⛔ **CẤM chạy Staging/Production** |
| **06** | [`06-sharedata-doi-ten-cot-dong-bo.sql`](06-sharedata-doi-ten-cot-dong-bo.sql) | `DEV_ITS10` | Đồng bộ 3 trường cấu hình đồng bộ trên `ShareDataPacket` (`SyncKind` → `SyncMode`, `TimeColumn`/`CursorColumn` → `CursorTimeColumn`, `LookbackDays` → `MaxLookBackDays`): chạy SAU CodeFirst, sao chép dữ liệu sang cột mới do CodeFirst tạo sẵn và DROP cột cũ; dọn cột di sản trên `ShareDataMapping`. Tự động bỏ qua nếu chưa chạy CodeFirst. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |

---

## 🚀 2. Hướng Dẫn Thực Thi

> 🔴 **Nguyên tắc cốt lõi (Rule 19.85):**
> Entity là **Single Source of Truth (SSOT)** cho toàn bộ cấu trúc bảng và index.
> CodeFirst tạo khung trước (`TableSettings.EnableInitTable = true`), script SQL chỉ làm nhiệm vụ di trú/seed dữ liệu và dọn dẹp di sản.
> Tuyệt đối **KHÔNG** `CREATE TABLE`, `ALTER TABLE ... ADD`, `CREATE INDEX` cho các bảng đã có entity trong script `.sql` (ngoại lệ duy nhất: `InboundTrafficFlow` không có entity trong `05`).

### A. Triển Khai Trên CSDL Mới Hoàn Toàn
1. Khởi động WebAPI (bật tạm `TableSettings.EnableInitTable = true` trong cấu hình debug local) để CodeFirst tự động tạo đầy đủ bảng và index từ các Entity ShareData.
2. Chạy lần lượt các kịch bản seed:
   - `02-sharedata-seed-goi-tin.sql` (tự động tạo 11 gói tin nếu chưa có và nạp câu truy vấn mẫu Catalog 101–110).
   - `03-sharedata-seed-cau-hinh.sql` (tự động tạo `SysConfigType`, nạp 25 mã lỗi ESH, 15 bảng nguồn `ShareDataSourceTable` và 47 tham số vận hành `SysOpConfig` nhóm `sharedata_config`).
   - `04-sharedata-dich-thuat.sql` (nạp 49 khóa thuật ngữ tiếng Việt & tiếng Anh; sau khi chạy bấm 🔄 ở avatar → Ngôn ngữ).
   - *(Tùy chọn cho Local/Test)*: Chạy `05-sharedata-du-lieu-gia-lap.sql` để có dữ liệu mẫu và gói kiểm thử riêng `E2E_INBOUND_TEST` kèm bảng đích chiều nhận chạy thử.

> ℹ️ *Bỏ qua các file `06`, `00`, `01` vì CSDL mới đã có cấu trúc chuẩn từ CodeFirst và không có dữ liệu cũ cần di trú.*  
> 📌 *Lưu ý về môi trường Local:* Cấu hình `.vscode/launch.json` ghim cứng các cờ CodeFirst của ConfigId 0 (`DEV_ITS10`) về `false` bằng biến môi trường để bảo vệ CSDL. Khi cần init bảng thì bật tạm thời.

### B. Triển Khai Cập Nhật Trên CSDL Đang Chạy (Đã Có Dữ Liệu Cũ)
Thứ tự thực thi chuẩn xác theo Rule 19.85:
```
CodeFirst (bật tạm EnableInitTable qua cấu hình debug) → [DB cũ: 06 → 00 → 01] → 02 → 03 → 04 → (05)
```

1. **Bước 0 — CodeFirst tạo khung mới:**
   Chạy WebAPI với `TableSettings.EnableInitTable = true` để khung tạo mới các cột đích (`SyncMode`, `CursorTimeColumn`, `MaxLookBackDays` trên `ShareDataPacket`; bảng `ShareDataSourceTable` với unique index `UX_ShareDataSourceTable_TableName`; các cột `ConnectionType`, `ConnectionConfig` trên `ShareDataPartner`).
2. **Bước 1 — `06-sharedata-doi-ten-cot-dong-bo.sql` (chạy SAU CodeFirst):**
   Chạy SAU KHI CodeFirst đã sinh các cột mới (`SyncMode`, `CursorTimeColumn`, `MaxLookBackDays`). Đồng bộ dữ liệu từ các cột cũ (`SyncKind`, `TimeColumn`, `CursorColumn`, `LookbackDays`) sang các cột mới đã được CodeFirst tạo sẵn (`UPDATE cột mới = cột cũ WHERE cột mới IS NULL`), sau đó dọn dẹp các cột cũ và default constraint. Nếu chưa chạy CodeFirst, script tự động kiểm tra và bỏ qua an toàn.
3. **Bước 2 — `00-sharedata-migration-mot-lan.sql`:**
   Di trú dữ liệu từ `ShareDataTrackTable` (nếu có) sang `ShareDataSourceTable` rồi DROP `ShareDataTrackTable`, xóa cột `Category` thừa nếu còn tồn tại, di trú từ `SysOpConfig` + `ShareDataTrackVersion` nếu bảng rỗng, dọn dòng `ROOT` cũ trong `ShareDataPacketSql`, DROP bảng mồ côi `ShareDataTable` & `ShareDataTrackVersion`, và chuyển đổi kiểu `ConnectionType` sang `INT`.
4. **Bước 3 — `01-sharedata-cau-truc.sql`:**
   Kiểm tra xung đột và bóc tách địa chỉ/cổng/endpoint từ JSON `ConnectionConfig` sang các cột riêng trên `ShareDataPartner`, dọn sạch key trong JSON bằng `JSON_MODIFY`.
5. **Bước 4 — `02-sharedata-seed-goi-tin.sql`:**
   Bổ sung gói tin thiếu, nạp câu truy vấn mẫu; `@GhiDeMauGoiTin = 0` bảo vệ SQL đã tùy biến.
6. **Bước 5 — `03-sharedata-seed-cau-hinh.sql`:**
   Nạp 25 mã lỗi ESH, 15 bảng nguồn `ShareDataSourceTable` và 47 tham số vận hành `SysOpConfig` nhóm `sharedata_config`.
7. **Bước 6 — `04-sharedata-dich-thuat.sql`:**
   Nạp 49 khóa thuật ngữ đa ngôn ngữ; sau khi chạy bấm 🔄 ở avatar → Ngôn ngữ.

### C. Tính An Toàn Khi Chạy Lại (Idempotent)
- Các file `01`, `02`, `03`, `04`, `06` đều có cơ chế Idempotent (`IF NOT EXISTS`, `WHERE NOT EXISTS`, `WHEN NOT MATCHED THEN INSERT`).
- `02-sharedata-seed-goi-tin.sql`: Biến `@GhiDeMauGoiTin = 0` (mặc định) đảm bảo **KHÔNG ghi đè** các câu lệnh SQL trích xuất mà quản trị viên đã tùy biến. Nếu muốn reset về Catalog chuẩn thì mới đổi sang `@GhiDeMauGoiTin = 1`.
- `03-sharedata-seed-cau-hinh.sql`: Chỉ thêm mã lỗi, bảng nguồn `ShareDataSourceTable` hoặc tham số `sharedata_config` chưa có trong DB, không ghi đè các cấu hình đang chạy.
- `04-sharedata-dich-thuat.sql`: Chỉ nạp bổ sung các bản dịch còn thiếu trong 49 khóa, không làm mất bản dịch đã chỉnh sửa.

### C. Khởi Tạo DB Mới Bằng Cờ CodeFirst (Thử Nghiệm)

> ⚠️ **LƯU Ý QUAN TRỌNG:**
> - Theo Rule 19.85, các lớp Seed class trong mã nguồn C# đã được loại bỏ hoàn toàn để Entity là Single Source of Truth duy nhất cho cấu trúc, và script SQL (`02`, `03`) là nguồn duy nhất cho dữ liệu seed ban đầu. Mẫu câu SQL trích xuất của 11 gói tin Catalog được cung cấp qua bộ nhớ đệm tĩnh `PacketDefaultTemplate` trong WebAPI.
> - **CẢNH BÁO:** Tuyệt đối **KHÔNG** bật cờ `EnableInitSeed` trên CSDL đã có dữ liệu hoặc đã có các gói tin do giao diện/script tạo ra với ID khác (có thể sinh dòng trùng mã `Code`). CSDL đang chạy bắt buộc dùng script `00` → `06`.

#### Các bước thử nghiệm:
1. **Dùng CSDL thử nghiệm tách biệt:** Ví dụ `DEV_ITS10_SeedSpike` bằng biến môi trường ghi đè chuỗi kết nối `ConfigId` `Default` trên cấu hình debug local (theo kiểu `.vscode/launch.json` đang dùng), ⛔ **KHÔNG sửa trực tiếp vào file `Database.json`**.
2. **Cờ cần bật tạm thời cho ConfigId Default:**
   - `DbSettings.EnableInitDb = true`
   - `TableSettings.EnableInitTable = true` (`EnableIncreTable = false`)
   - *(Nhớ trả về `false` ngay sau khi chạy thử nghiệm).*
3. **Danh sách kiểm nghiệm (Checklist nghiệm thu):**
   - ☐ **(1) Lần 1 trên DB trống:** Sau khi ứng dụng WebAPI khởi động với CodeFirst, CSDL có đầy đủ các bảng và index từ các Entity ShareData. Chạy script `02-sharedata-seed-goi-tin.sql` để có đúng 11 dòng `ShareDataPacket` với ID cố định `1500000000101`..`1500000000111`, câu truy vấn `ExtractSql` và `SyncMode` chuẩn xác.
   - ☐ **(2) Chạy lại lần 2 script 02:** Chạy lại `02-sharedata-seed-goi-tin.sql` → CSDL vẫn duy trì đúng 11 dòng, không bị nhân đôi bản ghi (idempotent).
   - ☐ **(3) Sửa câu lệnh ExtractSql qua UI rồi chạy lại script 02:** Sửa câu lệnh `ExtractSql` của một gói qua giao diện SQL Center, sau đó chạy lại script 02 với `@GhiDeMauGoiTin = 0` → Bản ghi đã sửa vẫn còn nguyên vẹn.
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
- Riêng **Khối 4.1 của `05-sharedata-du-lieu-gia-lap.sql`** thao tác trên CSDL nhận **`[DEV_ITS10_Inbound]`** (tạo bảng đích `InboundTrafficFlow`). Đảm bảo user thực thi có quyền truy cập trên cả hai CSDL hoặc chạy riêng từng khối theo đúng database context.
- Theo quy định **P0 Safeguards (Rule 16)**: AI tuyệt đối không tự thực thi lệnh ghi lên CSDL. Lập trình viên là người kiểm tra và thực thi script cuối cùng trên DB.
