# 🗄️ Danh Mục Kịch Bản SQL Phân Hệ ShareData (ESHARE)

> **Căn cứ quy chuẩn:** [`.agents/rules/database-mcp_thienan.md` §2](../../../../.agents/rules/database-mcp_thienan.md) (Gom script theo vai trò, không tách theo ngày).  
> **Cập nhật lần cuối:** 16/10/2026

---

## 📌 1. Bảng Thứ Tự Chạy Kịch Bản (00 → 06)

Mọi kịch bản SQL của phân hệ ShareData được phân tách rõ ràng theo đúng vai trò nghiệp vụ với tiền tố số thứ tự chạy `NN-sharedata-<vai-tro>.sql`:

| Thứ Tự | Tên Tệp Kịch Bản | CSDL Đích | Mục Đích Nghiệp Vụ | Đặc Tính Chạy Lại | Môi Trường Cho Phép |
| :---: | :--- | :---: | :--- | :---: | :---: |
| **00** | [`00-sharedata-migration-mot-lan.sql`](00-sharedata-migration-mot-lan.sql) | `DEV_ITS10` | Bổ sung 4 cột cấu hình truy vấn vào `ShareDataPacket`/`ShareDataMapping`, dọn dẹp các dòng `ShareDataPacketSql` có `TableName = 'ROOT'` của thiết kế cũ (xóa mềm), và DROP bảng mồ côi `ShareDataTable` nếu rỗng. (Không chuyển dữ liệu). | ⚠️ **Chỉ chạy 1 lần** (khi nâng cấp/di trú) | Mọi môi trường |
| **01** | [`01-sharedata-cau-truc.sql`](01-sharedata-cau-truc.sql) | `DEV_ITS10` | Chuẩn hóa cấu hình địa chỉ/cổng/endpoint từ JSON `ConnectionConfig` sang các cột riêng (`Address`, `Port`, `EndPointApiUrl`) trên `ShareDataPartner`. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **02** | [`02-sharedata-seed-goi-tin.sql`](02-sharedata-seed-goi-tin.sql) | `DEV_ITS10` | Khởi tạo bản ghi cho 11 gói tin (101–111) nếu chưa tồn tại (idempotent), nạp câu truy vấn SQL trích xuất mẫu Catalog (gói 101–110), chuẩn hóa `SyncMode` mặc định khi còn NULL (Snapshot / Incremental) và cập nhật `Status = 1` (Enable) cho gói 110*, `Status = 0` (Disable) cho gói 111*. Có cờ `@GhiDeMauGoiTin = 0` bảo vệ dữ liệu quản trị đã sửa. | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **03** | [`03-sharedata-seed-cau-hinh.sql`](03-sharedata-seed-cau-hinh.sql) | `DEV_ITS10` | Khởi tạo `SysConfigType` `sharedata_error_code`, nạp đủ 24 mã cảnh báo lỗi `ESH-xxxx` (theo ESHARE §9) vào `SysConfigData`, nạp danh mục 15 bảng theo dõi Change Tracking `sharedata_tracker` vào `SysOpConfig`, khai nhóm `sharedata_tracker` vào `SysConfigData` loại `operation_config_type` để màn hình *Cấu hình hoạt động* hiển thị. (Cần loại cấu hình `operation_config_type` do WebAPI seed ở lần chạy đầu tiên; nếu chạy script trước khi WebAPI chạy thì cần chạy lại script sau khi WebAPI đã chạy một lần để hiển thị đầy đủ). | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **04** | [`04-sharedata-dich-thuat.sql`](04-sharedata-dich-thuat.sql) | `DEV_ITS10` | Nạp từ điển bản dịch đa ngôn ngữ (`vi-VN`, `en-US`) cho 48 mã khóa chuẩn hóa (SQL trích xuất, LookbackDays, SQL Validator, Metadata, ConnectConfig) vào bảng `SysTerminology` qua `MERGE` không ghi đè bản dịch đã sửa. (Sau khi chạy script trên DB, trên giao diện cần bấm nút 🔄 tại avatar góc phải trên → chọn mục Ngôn ngữ để làm mới cache bản dịch). | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |
| **05** | [`05-sharedata-du-lieu-gia-lap.sql`](05-sharedata-du-lieu-gia-lap.sql) | `DEV_ITS10`, `DEV_ITS10_Inbound` | Nạp dữ liệu giả lập (Mock Data) cho gói 104 (`TmsWeather`), gói 106 (`TmsTrafficData` WIM), gói 110 (`TmsIncident`, `TmsEquipment`, `VmsCurrent`) và Khối 4 chuẩn bị bảng đích `InboundTrafficFlow` (trên `DEV_ITS10_Inbound`) kèm cấu hình câu MERGE ghi chiều nhận trên `ShareDataPacketSql` (`DEV_ITS10`) phục vụ kiểm thử thông luồng E0. | Chạy lại an toàn (**Idempotent**) | ⚠️ **Chỉ Local / Test**<br/>⛔ **CẤM chạy Staging/Production** |
| **06** | [`06-sharedata-doi-ten-cot-dong-bo.sql`](06-sharedata-doi-ten-cot-dong-bo.sql) | `DEV_ITS10` | Đổi tên 3 trường cấu hình đồng bộ trên `ShareDataPacket` và `ShareDataMapping` (`SyncKind` → `SyncMode`, `TimeColumn` → `CursorTimeColumn`, `LookbackDays` → `MaxLookBackDays`). | Chạy lại an toàn (**Idempotent**) | Mọi môi trường |

---

## 🚀 2. Hướng Dẫn Thực Thi

### A. Triển Khai Trên CSDL Mới Hoàn Toàn (Khởi Tạo Ban Đầu Bằng Script)
Chạy lần lượt theo đúng thứ tự số từ `01` đến `04`:
1. `01-sharedata-cau-truc.sql` (chuẩn hóa cột địa chỉ/cổng của đối tác trên DEV_ITS10).
2. `02-sharedata-seed-goi-tin.sql` (tự động tạo 11 gói tin nếu chưa có và nạp câu truy vấn mẫu Catalog 101–110).
3. `03-sharedata-seed-cau-hinh.sql` (tự động tạo `SysConfigType` và nạp mã lỗi, cấu hình Change Tracking; chạy lại nếu lần đầu WebAPI chưa seed loại `operation_config_type`).
4. `04-sharedata-dich-thuat.sql` (nạp 48 khóa thuật ngữ tiếng Việt & tiếng Anh; sau khi chạy bấm 🔄 ở avatar → Ngôn ngữ).
5. *(Tùy chọn cho môi trường Local/Test)*: Chạy `05-sharedata-du-lieu-gia-lap.sql` để có dữ liệu mẫu và bảng đích chiều nhận chạy thử.

> ℹ️ *Bỏ qua file `00-sharedata-migration-mot-lan.sql` vì CSDL mới không có các bảng tạm/cũ cần dọn.*  
> 📌 *Lưu ý về môi trường Local:* Cấu hình `.vscode/launch.json` ghim cứng các cờ CodeFirst của ConfigId 0 (`DEV_ITS10`) về `false` bằng biến môi trường để bảo vệ CSDL.

### B. Triển Khai Cập Nhật Trên CSDL Đang Chạy (Đã Có Dữ Liệu)
1. **BẪY THỨ TỰ CẦN LƯU Ý:** Nếu CSDL còn tên cột cũ (`SyncKind`, `TimeColumn`, `LookbackDays`), BẮT BUỘC chạy script `06-sharedata-doi-ten-cot-dong-bo.sql` **TRƯỚC `00` và TRƯỚC mọi lần chạy CodeFirst**. Nếu CodeFirst hoặc `00` chạy trước thì các cột mới đã tồn tại, khiến `06` bỏ qua việc đổi tên và dữ liệu cũ sẽ nằm lại ở cột cũ.
2. **Thứ tự thực thi đầy đủ cho DB cũ:**
   - Bước 1: `06-sharedata-doi-ten-cot-dong-bo.sql` (đổi tên các cột đồng bộ sang `SyncMode`, `CursorTimeColumn`, `MaxLookBackDays`).
   - Bước 2: `00-sharedata-migration-mot-lan.sql` (bổ sung cột cấu hình còn thiếu, dọn dòng `ROOT` cũ và DROP bảng `ShareDataTable` nếu rỗng; không chuyển dữ liệu).
   - Bước 3: `01-sharedata-cau-truc.sql` (chuẩn hóa địa chỉ/cổng sang cột riêng trên `DEV_ITS10`).
   - Bước 4: `02-sharedata-seed-goi-tin.sql` (bổ sung gói tin thiếu, nạp câu truy vấn mẫu; `@GhiDeMauGoiTin = 0` bảo vệ SQL đã tùy biến).
   - Bước 5: `03-sharedata-seed-cau-hinh.sql` (nạp mã lỗi ESH và cấu hình Change Tracking).
   - Bước 6: `04-sharedata-dich-thuat.sql` (nạp thuật ngữ đa ngôn ngữ; sau khi chạy bấm 🔄 ở avatar → Ngôn ngữ).
3. **An toàn khi chạy lại:**
   - Các file `01`, `02`, `03`, `04`, `06` đều có cơ chế Idempotent (`IF NOT EXISTS`, `WHERE NOT EXISTS`, `WHEN NOT MATCHED THEN INSERT`).
   - `02-sharedata-seed-goi-tin.sql`: Biến `@GhiDeMauGoiTin = 0` (mặc định) đảm bảo **KHÔNG ghi đè** các câu lệnh SQL trích xuất mà quản trị viên đã tùy biến. Nếu muốn reset về Catalog chuẩn thì mới đổi sang `@GhiDeMauGoiTin = 1`.
   - `03-sharedata-seed-cau-hinh.sql`: Chỉ thêm mã lỗi hoặc bảng Change Tracking chưa có trong DB, không ghi đè các cấu hình đang chạy.
   - `04-sharedata-dich-thuat.sql`: Chỉ nạp bổ sung các bản dịch còn thiếu, không làm mất bản dịch đã chỉnh sửa.

### C. Khởi Tạo DB Mới Bằng Cờ CodeFirst + Seed (Thử Nghiệm)

> ⚠️ **LƯU Ý QUAN TRỌNG:**
> - Cơ chế Seed của khung (`ISqlSugarEntitySeedData<ShareDataPacket>`) qua lớp `ShareDataPacketSeedData` phục vụ tự động tạo dữ liệu nền trên CSDL mới mà không cần chạy script thủ công.
> - **CẢNH BÁO:** Tuyệt đối **KHÔNG** bật cờ seed trên CSDL đã có dữ liệu hoặc đã có các gói tin do giao diện/script tạo ra với ID khác (có thể sinh dòng trùng mã `Code`). CSDL đang chạy bắt buộc dùng script `00` → `06`.

#### Các bước thử nghiệm:
1. **Dùng CSDL thử nghiệm tách biệt:** Ví dụ `DEV_ITS10_SeedSpike` bằng biến môi trường ghi đè chuỗi kết nối `ConfigId` `Default` trên cấu hình debug local (theo kiểu `.vscode/launch.json` đang dùng), ⛔ **KHÔNG sửa trực tiếp vào file `Database.json`**.
2. **Cờ cần bật tạm thời cho ConfigId Default:**
   - `DbSettings.EnableInitDb = true`
   - `TableSettings.EnableInitTable = true` (`EnableIncreTable = false`)
   - `SeedSettings.EnableInitSeed = true` (`EnableIncreSeed = false`)
   - *(Nhớ trả về `false` ngay sau khi chạy thử nghiệm).*
3. **Danh sách kiểm nghiệm (Checklist nghiệm thu):**
   - ☐ **(1) Lần 1 trên DB trống:** Sau khi ứng dụng WebAPI khởi động, CSDL có đúng 11 dòng `ShareDataPacket` với ID cố định `1500000000101`..`1500000000111`, câu truy vấn `ExtractSql` và `SyncMode` chuẩn xác.
   - ☐ **(2) Chạy lại lần 2 cùng cờ:** Dừng và khởi động lại WebAPI với cùng cờ seed → CSDL vẫn duy trì đúng 11 dòng, không bị nhân đôi bản ghi.
   - ☐ **(3) Sửa câu lệnh ExtractSql qua UI rồi chạy lại cùng cờ:** Sửa câu lệnh `ExtractSql` của một gói qua giao diện SQL Center, sau đó khởi động lại WebAPI với cờ seed → Bản ghi đã sửa vẫn còn nguyên vẹn *(nếu bị ghi đè: ghi nhận "seed ghi đè" và tuyệt đối KHÔNG dùng cờ trên DB đã có dữ liệu)*.
   - ☐ **(4) Tắt cờ, chạy lại:** Đặt lại các cờ về `false`, khởi động lại WebAPI → Dữ liệu gói tin không thay đổi.
   - ☐ **(5) Ghi nhận kết luận ngữ nghĩa:** Xác định rõ hành vi của khung hạ tầng là `Chèn thiếu` (chỉ insert khi chưa có) hay `Ghi đè` (override).

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
