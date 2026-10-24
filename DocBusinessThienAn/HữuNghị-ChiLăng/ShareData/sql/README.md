# 🗄️ Danh Mục Kịch Bản SQL Phân Hệ ShareData (ESHARE)

> **Căn cứ quy chuẩn:** [`.agents/rules/database-mcp_thienan.md` §2.3](../../../../.agents/rules/database-mcp_thienan.md) (Rule 19.85: Entity là SSOT, CodeFirst tạo khung trước; Rule 19.101: dữ liệu giả lập mang hậu tố `_local.sql`; Rule 19.103: **mỗi tệp chỉ ghi một bảng, tên tệp mang hậu tố tên bảng**).  
> **Cập nhật lần cuối:** 24/10/2026

---

## 📌 1. Quy Ước Đặt Tên Và Tách Tệp (Rule 19.103)

- **Mỗi tệp chỉ thêm / sửa / xóa trên đúng một bảng.** Bảng khác chỉ được đọc (`SELECT`, `JOIN`, `EXISTS`). Nhờ vậy nhìn tên tệp là biết bảng nào bị đụng, chạy nhầm hay hoàn tác cũng chỉ gói trong một bảng.
- **Tên tệp:** `sharedata-<nhóm>-<TênBảng>.sql`; dữ liệu giả lập: `local/sharedata-du-lieu-gia-lap-<TênBảng>_local.sql` (hậu tố `_local` luôn đứng cuối).
- **Thao tác dọn dẹp, chuyển dữ liệu một lần** (không phải nạp dữ liệu chuẩn) ghi rõ việc ở giữa: `sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql`.
- **Đầu mỗi tệp** có dòng `BẢNG GHI` và `THỨ TỰ CHẠY`; tệp có kiểm tra bảng chưa tồn tại dùng `SET NOEXEC ON` để bỏ qua toàn bộ các batch phía sau (`RETURN` chỉ thoát batch hiện tại).

---

## 📌 2. Bảng Thứ Tự Chạy

### A. Dữ liệu chuẩn (mọi môi trường)

| Thứ tự | Tệp | Bảng ghi | Nội dung |
| :---: | :--- | :--- | :--- |
| **1** | [`sharedata-seed-goi-tin-ShareDataPacket.sql`](sharedata-seed-goi-tin-ShareDataPacket.sql) | `ShareDataPacket` | Thêm 11 gói Catalog (101–111) nếu chưa có, chuẩn hóa `OrderNo`, nạp `TemplateSql` (câu mẫu gốc), chuẩn hóa `Status` (bật 101–110, tắt 111), đổi `__rowid` → `__lastKey` trong `TemplateSql`. |
| **2** | [`sharedata-seed-goi-tin-ShareDataSql.sql`](sharedata-seed-goi-tin-ShareDataSql.sql) | `ShareDataSql` | Chuyển câu SQL cũ (cột `ShareDataPacket.ExtractSql/CursorTimeColumn/MaxLookBackDays`, bảng `ShareDataPartnerSql`, cột `ShareDataMapping.ExtractSql`) sang `ShareDataSql`; nạp câu mẫu chung (dòng `PartnerId` trống) theo `TemplateSql` khi đang trống; đổi `__rowid` → `__lastKey`. **Chạy sau tệp 1** vì đọc `TemplateSql`. |
| **3** | [`sharedata-seed-goi-tin-ShareDataPacketSql.sql`](sharedata-seed-goi-tin-ShareDataPacketSql.sql) | `ShareDataPacketSql` | Nạp 18 câu ghi chiều nhận cho 10 gói (101–109 và 111). |
| **4** | [`sharedata-seed-cau-hinh-SysConfigType.sql`](sharedata-seed-cau-hinh-SysConfigType.sql) | `SysConfigType` | Khởi tạo loại danh mục `sharedata_error_code`. |
| **5** | [`sharedata-seed-cau-hinh-SysConfigData.sql`](sharedata-seed-cau-hinh-SysConfigData.sql) | `SysConfigData` | Nạp 25 mã cảnh báo `ESH-xxxx`; đăng ký nhóm `sharedata_config` vào loại `operation_config_type`. **Chạy sau tệp 4.** |
| **6** | [`sharedata-seed-cau-hinh-ShareDataSourceTable.sql`](sharedata-seed-cau-hinh-ShareDataSourceTable.sql) | `ShareDataSourceTable` | Nạp danh mục 15 bảng nguồn (`IsTracked`, `IsQueryable`, `DefaultAlias`). |
| **7** | [`sharedata-seed-cau-hinh-SysOpConfig.sql`](sharedata-seed-cau-hinh-SysOpConfig.sql) | `SysOpConfig` | Nạp 48 tham số nhóm `sharedata_config` (giữ giá trị còn ở nhóm cũ `sharedata_fe_config`); xóa mềm tham số không còn dùng. |
| **8** | [`sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql`](sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql) | `SysConfigData` | Xóa mềm dữ liệu nhóm cũ `sharedata_fe_config`. **Chạy sau tệp 7** (tệp 7 đọc dữ liệu nhóm cũ). |
| **9** | [`sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigType.sql`](sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigType.sql) | `SysConfigType` | Xóa mềm loại danh mục cũ `sharedata_fe_config`. **Chạy sau tệp 8.** |
| **10** | [`sharedata-dich-thuat-SysTerminology.sql`](sharedata-dich-thuat-SysTerminology.sql) | `SysTerminology` | Nạp 112 khóa thuật ngữ vi-VN / en-US qua `MERGE` không ghi đè bản dịch đã sửa. Chạy xong bấm 🔄 ở avatar góc phải trên → Ngôn ngữ để làm mới cache. |

### B. Dữ liệu giả lập (⚠️ chỉ Local / Test, hậu tố `_local`, thư mục con [`local/`](local/))

Chỉ giả lập bảng nghiệp vụ ShareData; không ghi vào bảng của phân hệ khác (`TmsEquipment`, `TmsWeather`, `TmsTrafficData`, `TmsIncident`, `VmsCurrent`, `TmsSignalLog`) hay bảng đích chiều nhận `InboundTrafficFlow` — các bảng đó dùng dữ liệu sẵn có.

| Thứ tự | Tệp | Bảng ghi | Nội dung |
| :---: | :--- | :--- | :--- |
| **G1** | [`sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql`](local/sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql) | `ShareDataPacket` | Gói kiểm thử `E2E_INBOUND_TEST`. Chạy sau tệp 1. |
| **G2** | [`sharedata-du-lieu-gia-lap-ShareDataSql_local.sql`](local/sharedata-du-lieu-gia-lap-ShareDataSql_local.sql) | `ShareDataSql` | Câu mẫu chung của `E2E_INBOUND_TEST` theo gói 101. Chạy sau G1 và tệp 2. |
| **G3** | [`sharedata-du-lieu-gia-lap-ShareDataPacketSql_local.sql`](local/sharedata-du-lieu-gia-lap-ShareDataPacketSql_local.sql) | `ShareDataPacketSql` | Xóa mềm dòng cũ của bản script trước; nạp câu ghi MERGE của `E2E_INBOUND_TEST`. |

- **Dọn dẹp:** mỗi tệp giả lập có khối dọn dẹp riêng, đặt `@XoaDuLieuGiaLap = 1` trong tệp đó. Dọn theo thứ tự ngược: G3 → G2 → G1 (G2 trước G1 vì G2 tìm dòng theo gói `E2E_INBOUND_TEST`).
- Mọi dòng giả lập gắn `CreateUId = N'SHAREDATA-SEED-GIA-LAP'`.
- G1–G3 (gói `E2E_INBOUND_TEST`) được chạy trên Staging để kiểm thử chiều nhận E0, dọn theo [doc 05 mục 4](../doc/05-chay-sql-tu-dau-sharedata.md#4-khôi-phục-và-dọn-dẹp-bằng-sql).

---

## 🚀 3. Hướng Dẫn Thực Thi

> 🔴 **Nguyên tắc cốt lõi (Rule 19.85):** Entity là SSOT cho cấu trúc bảng và index. CodeFirst tạo khung trước (`TableSettings.EnableInitTable = true`), script SQL chỉ seed dữ liệu. Tuyệt đối **KHÔNG** `CREATE TABLE`, `ALTER TABLE ... ADD`, `CREATE INDEX` cho bảng đã có entity (ngoại lệ duy nhất: `InboundTrafficFlow` trong tệp G7).

```
CodeFirst tạo khung ➔ 1 ➔ 2 ➔ 3 ➔ 4 ➔ 5 ➔ 6 ➔ 7 ➔ 8 ➔ 9 ➔ 10 ➔ (tùy chọn) G1 … G3
```

1. **Bước 0 — CodeFirst:** khởi động WebAPI (bật tạm `TableSettings.EnableInitTable = true` trong cấu hình debug local) để tạo đủ bảng và index, gồm `ShareDataSql` (câu SQL và thiết lập SQL, unique theo gói + đối tác), `ShareDataSqlVariable` (biến SQL như `@pageSize`, unique theo gói + đối tác + tên biến), `ShareDataSourceTable` (unique `UX_ShareDataSourceTable_TableName`).
2. **Bước 1 — chạy tệp 1 → 10** theo bảng mục 2A.
3. **Bước 2 (tùy chọn) — chạy G1 → G3** theo bảng mục 2B trên Local / Test.

> 📌 *Lưu ý môi trường Local:* `.vscode/launch.json` ghim các cờ CodeFirst của ConfigId 0 (`DEV_ITS10`) về `false` bằng biến môi trường để bảo vệ CSDL. Khi cần init bảng thì bật tạm thời.

### Tính an toàn khi chạy lại (idempotent)

- Mọi tệp kiểm tra dữ liệu đã có trước khi ghi (`IF NOT EXISTS`, `WHERE NOT EXISTS`, `WHEN NOT MATCHED THEN INSERT`).
- **Cờ `@GhiDeMauGoiTin`** (mặc định `0`, không ghi đè dữ liệu quản trị đã sửa) có ở hai tệp:
  - tệp 1 làm mới `TemplateSql`;
  - tệp 2 làm mới câu mẫu chung trong `ShareDataSql`.
  Muốn đưa cả hai về mẫu Catalog thì đặt `= 1` ở cả hai tệp.
- **Tệp 7** chỉ thêm tham số `sharedata_config` chưa có, không ghi đè giá trị đang chạy.
- **Tệp 10** chỉ thêm bản dịch còn thiếu, không làm mất bản dịch đã chỉnh sửa.

### Khởi tạo DB mới bằng cờ CodeFirst (thử nghiệm)

> ⚠️ **KHÔNG** bật `EnableInitSeed` trên CSDL đã có dữ liệu (có thể sinh dòng trùng mã `Code`). Seed class C# đã bỏ hoàn toàn (Rule 19.85); câu SQL mẫu của 11 gói Catalog chỉ nằm ở tệp 1.

1. Dùng CSDL thử nghiệm tách biệt (ví dụ `DEV_ITS10_SeedSpike`) bằng biến môi trường ghi đè chuỗi kết nối `ConfigId` `Default` trên cấu hình debug local, ⛔ **KHÔNG sửa trực tiếp `Database.json`**.
2. Bật tạm `DbSettings.EnableInitDb = true`, `TableSettings.EnableInitTable = true` (`EnableIncreTable = false`), nhớ trả về `false` sau khi chạy.
3. Checklist:
   - ☐ **(1) Lần 1 trên DB trống:** sau CodeFirst, chạy tệp 1 và 2 → đúng 11 dòng `ShareDataPacket`, 11 dòng câu mẫu chung trong `ShareDataSql`.
   - ☐ **(2) Chạy lại tệp 1 và 2:** vẫn đúng 11 dòng mỗi bảng, không nhân đôi.
   - ☐ **(3) Sửa câu SQL qua giao diện rồi chạy lại tệp 2** với `@GhiDeMauGoiTin = 0` → câu đã sửa còn nguyên.
   - ☐ **(4) Tắt cờ, khởi động lại WebAPI** → dữ liệu không đổi.

#### Truy vấn đối soát chỉ đọc

```sql
SELECT
    p.[ID],
    p.[Code],
    p.[Name],
    s.[CursorTimeColumn],
    s.[MaxLookBackDays],
    p.[Status],
    CASE WHEN s.[ExtractSql] IS NOT NULL THEN LEN(s.[ExtractSql]) ELSE 0 END AS [SqlLength]
FROM [dbo].[ShareDataPacket] p
LEFT JOIN [dbo].[ShareDataSql] s ON s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL
ORDER BY p.[OrderNo];
```

---

## ⚠️ 4. Lưu Ý Về CSDL Đích

- Các tệp chạy trên CSDL chính **`[DEV_ITS10]`**. Câu ghi của G3 `MERGE` vào bảng đích `dbo.InboundTrafficFlow` trên `[DEV_ITS10_Inbound]`; thư mục không tạo bảng này (xem doc 05 nếu cần tạo tay khi kiểm thử chiều nhận).
- **P0 Safeguards (Rule 16):** AI không tự thực thi lệnh ghi lên CSDL. Lập trình viên kiểm tra và thực thi script trên DB.
- **Ghi chú lịch sử:** trước 24/10/2026 thư mục có 4 tệp gộp (`sharedata-seed-goi-tin.sql`, `sharedata-seed-cau-hinh.sql`, `sharedata-dich-thuat.sql`, `sharedata-du-lieu-gia-lap_local.sql`), mỗi tệp ghi nhiều bảng. Nội dung đã tách nguyên vẹn sang bộ tệp trên. Riêng mục 5 cũ của `sharedata-seed-cau-hinh.sql` (ghi chú bộ mã sự cố gói 107 đã gỡ ngày 22/10/2026) chỉ là chú thích, không còn câu lệnh, nên không chép sang.
