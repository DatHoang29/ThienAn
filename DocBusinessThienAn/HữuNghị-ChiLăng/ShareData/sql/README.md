# 🗄️ Danh Mục Kịch Bản SQL Phân Hệ ShareData (ESHARE)

> **Căn cứ quy chuẩn:** [`.agents/rules/database-mcp_thienan.md` §2.3](../../../../.agents/rules/database-mcp_thienan.md) (Rule 19.85: Entity là SSOT, CodeFirst tạo khung trước; Rule 19.101: tệp `_local.sql` chỉ chạy trên DB local; Rule 19.103: **mỗi tệp chỉ ghi một bảng, tên tệp mang hậu tố tên bảng**).  
> **Cập nhật lần cuối:** 04/11/2026

---

## 📌 1. Hai Loại Tệp: Chạy Ở Đâu

| Loại | Vị trí / tên tệp | Chạy trên DB nào | Nội dung |
| :--- | :--- | :--- | :--- |
| **Dữ liệu chuẩn** | `sql/sharedata-…sql` (**không** có `_local`) | **Mọi DB**: local, dev, staging, production. DB local cũng phải chạy đủ tệp 1 → 13 | Dữ liệu hệ thống cần để chạy: gói 101–111, câu SQL mẫu, mã lỗi, bảng nguồn, cấu hình, bản dịch, chuyển đổi mốc |
| **Dữ liệu giả lập** | `sql/local/…_local.sql` | **Chỉ DB local** trên máy lập trình viên. ⛔ Cấm chạy trên DB ở máy chủ khác (dev chung, staging, UAT, production) | Dữ liệu giả để thử, chạy sau dữ liệu chuẩn |

> Nhớ nhanh: **có `_local` = chỉ DB local**. **Không có `_local` = dữ liệu chuẩn, DB nào cũng chạy, kể cả local.**
> Mỗi tệp `_local` có **khối 0 — Chốt DB local** ở đầu tệp: in ra máy chủ đang kết nối, và nếu địa chỉ máy chủ không phải local (`127.0.0.1` / `::1`, kết nối cục bộ, hoặc Docker trên máy dải `172.16.0.0/12`) thì báo lỗi `⛔ Tệp _local chỉ chạy trên DB local` và bỏ qua toàn bộ tệp, không ghi dữ liệu.

---

## 📌 2. Quy Ước Đặt Tên Và Tách Tệp (Rule 19.103)

- **Mỗi tệp chỉ thêm / sửa / xóa trên đúng một bảng.** Bảng khác chỉ được đọc (`SELECT`, `JOIN`, `EXISTS`). Nhờ vậy nhìn tên tệp là biết bảng nào bị đụng, chạy nhầm hay hoàn tác cũng chỉ gói trong một bảng.
- **Tên tệp:** `sharedata-<nhóm>-<TênBảng>.sql`; dữ liệu giả lập: `local/sharedata-du-lieu-gia-lap-<TênBảng>_local.sql` (hậu tố `_local` luôn đứng cuối).
- **Thao tác dọn dẹp, chuyển dữ liệu một lần** (không phải nạp dữ liệu chuẩn) ghi rõ việc ở giữa: `sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql`.
- **Đầu mỗi tệp** có dòng `BẢNG GHI` và `THỨ TỰ CHẠY`; tệp có kiểm tra bảng chưa tồn tại dùng `SET NOEXEC ON` để bỏ qua toàn bộ các batch phía sau (`RETURN` chỉ thoát batch hiện tại).

---

## 📌 3. Bảng Thứ Tự Chạy

### A. Dữ liệu chuẩn (mọi DB, kể cả local)

| Thứ tự | Tệp | Bảng ghi | Nội dung |
| :---: | :--- | :--- | :--- |
| **1** | [`sharedata-seed-goi-tin-ShareDataPacket.sql`](sharedata-seed-goi-tin-ShareDataPacket.sql) | `ShareDataPacket` | Thêm 11 gói Catalog (101–111) nếu chưa có gói đang sống cùng mã, chuẩn hóa `OrderNo`, chuẩn hóa `Status` (bật 110, tắt 111). Không chứa câu SQL. Viết lại 03/11/2026 (tệp bị mất ngày 27/10/2026); dòng mới để `IsDelete` trống và có `TenantId`. |
| **2** | [`sharedata-seed-goi-tin-ShareDataSql.sql`](sharedata-seed-goi-tin-ShareDataSql.sql) | `ShareDataSql` | Chuyển câu SQL cũ (cột `ShareDataPacket.ExtractSql`, bảng `ShareDataPartnerSql`, cột `ShareDataMapping.ExtractSql`) sang `ShareDataSql`; nạp câu mẫu chung (dòng `PartnerId` trống) khi đang trống — **nơi duy nhất chứa câu SQL mẫu chuẩn 101–111**; đổi `__rowid` → `__lastKey`; 7 câu nối đuôi (101, 103, 104, 106, 107, 109, 111) dùng một chuỗi ghép `CONVERT(varchar(23), <thời gian>, 121) + '_' + <ID> AS __lastKey` (02/11/2026); bước 1b điền bộ biến cho câu còn trống. Gói 101 lấy `COALESCE(zs.UpdateTime, zs.CreateTime)` từ 04/11/2026: DB đã có câu mẫu thì chạy tệp này với `@GhiDeMaGoi = N'101'`; đối tác dùng mẫu chung đang có mốc sẽ bị Worker bỏ mốc cũ và gửi lại (ESH-1308), muốn tránh thì tắt đăng ký và Đặt mốc trước. **Chạy sau tệp 1** vì cần bản ghi gói. Bộ biến câu truy vấn lưu ở cột `VariablesJson` (bỏ bảng `ShareDataSqlVariable`, tự chuyển dữ liệu cũ); câu ghi chiều nhận ở `WriteSql`, `AffectedTables` của dòng mẫu chung. |
| ~~3~~ | ~~`sharedata-seed-goi-tin-ShareDataPacketSql.sql`~~ | — | **Đã gộp vào tệp 2 (27/10/2026):** câu ghi chiều nhận nằm ở cột `WriteSql`, `AffectedTables` của dòng mẫu chung `ShareDataSql`; tệp 2 tự chuyển dữ liệu cũ từ bảng `ShareDataPacketSql`. |
| **4** | [`sharedata-seed-cau-hinh-SysConfigType.sql`](sharedata-seed-cau-hinh-SysConfigType.sql) | `SysConfigType` | Khởi tạo loại danh mục `sharedata_error_code`. |
| **5** | [`sharedata-seed-cau-hinh-SysConfigData.sql`](sharedata-seed-cau-hinh-SysConfigData.sql) | `SysConfigData` | Nạp 26 mã cảnh báo `ESH-xxxx`; đăng ký nhóm `sharedata_config` vào loại `operation_config_type`. **Chạy sau tệp 4.** |
| **6** | [`sharedata-seed-cau-hinh-ShareDataSourceTable.sql`](sharedata-seed-cau-hinh-ShareDataSourceTable.sql) | `ShareDataSourceTable` | Nạp danh mục 15 bảng nguồn (`IsTracked`, `IsQueryable`, `DefaultAlias`). |
| **7** | [`sharedata-seed-cau-hinh-SysOpConfig.sql`](sharedata-seed-cau-hinh-SysOpConfig.sql) | `SysOpConfig` | Nạp 45 tham số nhóm `sharedata_config` (giữ giá trị còn ở nhóm cũ `sharedata_fe_config`); xóa mềm tham số không còn dùng. |
| ~~8~~ | ~~`sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql`~~ | — | **Đã xóa tệp 28/10/2026:** việc dọn nhóm cũ `sharedata_fe_config` chạy một lần, DB mới không có nhóm này. |
| ~~9~~ | ~~`sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigType.sql`~~ | — | **Đã xóa tệp 28/10/2026:** như tệp 8. |
| **10** | [`sharedata-dich-thuat-SysTerminology.sql`](sharedata-dich-thuat-SysTerminology.sql) | `SysTerminology` | Nạp 168 khóa thuật ngữ (336 dòng vi-VN / en-US) qua `MERGE` không ghi đè bản dịch đã sửa. Có sửa lại 11 câu lỗi đã nạp từ trước: câu dịch lỗi dùng cho cả máy chủ (`string.Format`) lẫn màn hình, nên không được có `{'@'}`, và lỗi nối đuôi không dùng chỗ điền `{0}` (04/11/2026). Chạy xong bấm 🔄 ở avatar góc phải trên → Ngôn ngữ để làm mới cache. |
| **11** | [`sharedata-chuyen-doi-moc-ShareDataLastSend.sql`](sharedata-chuyen-doi-moc-ShareDataLastSend.sql) | `ShareDataLastSend` | Gỡ unique index 2 cột cũ `index_ShareDataLastSend_Partner_Packet`; chép không trùng các dòng nhóm mốc có tên từ bảng cũ `ShareDataLastSendKey` (chỉ đọc) sang `ShareDataLastSend`. **Phải chạy sau CodeFirst; tắt Worker trước khi chạy.** |
| **12** | [`sharedata-xoa-bang-cu-ShareDataLastSendKey.sql`](sharedata-xoa-bang-cu-ShareDataLastSendKey.sql) | `ShareDataLastSendKey` | Xóa bảng cũ `ShareDataLastSendKey` sau khi đã gộp cột `CursorKey` vào `ShareDataLastSend` (chặn bằng `SET NOEXEC ON` nếu cột `CursorKey` chưa có hoặc còn dòng chưa chuyển). **Chạy sau tệp 11.** |
| **13** | [`sharedata-seed-menu-SysMenu.sql`](sharedata-seed-menu-SysMenu.sql) | `SysMenu` | Thêm menu **Mốc gửi** (`views/sharedata/cursor`) dưới nhóm ShareData nếu chưa có. Không gán vai trò vì API của trang chỉ nhận SuperAdmin. Chạy lại an toàn. |

### C. Dọn một lần trên DB cũ

| Thứ tự | Tệp | Bảng ghi | Nội dung |
| :---: | :--- | :--- | :--- |
| **D1** | [`sharedata-xoa-cot-thua-ShareDataPacket.sql`](sharedata-xoa-cot-thua-ShareDataPacket.sql) | `ShareDataPacket` | Xóa 5 cột entity đã bỏ (`ExtractSql`, `TemplateSql`, `CursorTimeColumn`, `SyncMode`, `MaxLookBackDays`) trên DB tạo trước 27/10/2026. **Chạy sau tệp 2**, vì tệp 2 chuyển `ExtractSql`, `CursorTimeColumn` cũ sang `ShareDataSql`; DB mới tạo bằng CodeFirst không có các cột này nên bỏ qua. |
| **D2** | [`sharedata-sua-cau-rieng-moc-hai-cot-ShareDataSql.sql`](sharedata-sua-cau-rieng-moc-hai-cot-ShareDataSql.sql) | `ShareDataSql` | Sửa câu SQL riêng của đối tác còn mốc 2 cột (`__lastTime`, `@lastTime`) chép từ câu mẫu gói 101 cũ sang một chuỗi ghép `COALESCE(zs.UpdateTime, zs.CreateTime)`, giống nút **Sửa câu**; bỏ biến `lastTime`. Câu khác khuôn hoặc đối tác đã có mốc thì chỉ in tên, sửa bằng nút **Sửa câu** trên giao diện. Chạy lại không sao. **DB dev (đọc 04/11/2026):** sửa được câu riêng gói 101 của TTCSDL và Kiên. |

### B. Dữ liệu giả lập (⛔ chỉ DB local, hậu tố `_local`, thư mục con [`local/`](local/))

Chỉ giả lập bảng nghiệp vụ ShareData; không ghi vào bảng của phân hệ khác (`TmsEquipment`, `TmsWeather`, `TmsTrafficData`, `TmsIncident`, `VmsCurrent`, `TmsSignalLog`) hay bảng đích chiều nhận `InboundTrafficFlow` — các bảng đó dùng dữ liệu sẵn có.

| Thứ tự | Tệp | Bảng ghi | Nội dung |
| :---: | :--- | :--- | :--- |
| **G1** | [`sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql`](local/sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql) | `ShareDataPacket` | Gói kiểm thử `E2E_INBOUND_TEST`. Chạy sau tệp 1. |
| **G2** | [`sharedata-du-lieu-gia-lap-ShareDataSql_local.sql`](local/sharedata-du-lieu-gia-lap-ShareDataSql_local.sql) | `ShareDataSql` | Câu mẫu chung của `E2E_INBOUND_TEST` theo gói 101. Chạy sau G1 và tệp 2. |
| ~~G3~~ | ~~`sharedata-du-lieu-gia-lap-ShareDataPacketSql_local.sql`~~ | — | **Đã gộp vào G2 (27/10/2026):** khối câu ghi MERGE của `E2E_INBOUND_TEST` nằm ở mục 2 của `sharedata-du-lieu-gia-lap-ShareDataSql_local.sql`. |

- **Dọn dẹp:** mỗi tệp giả lập có khối dọn dẹp riêng, đặt `@XoaDuLieuGiaLap = 1` trong tệp đó. Dọn theo thứ tự ngược: G2 → G1 (G2 trước G1 vì G2 tìm dòng theo gói `E2E_INBOUND_TEST`).
- Mọi dòng giả lập gắn `CreateUId = N'SHAREDATA-SEED-GIA-LAP'`.
- G1–G2 chỉ chạy trên DB local.

---

## 🚀 4. Hướng Dẫn Thực Thi

> 🔴 **Nguyên tắc cốt lõi (Rule 19.85):** Entity là SSOT cho cấu trúc bảng và index. CodeFirst tạo khung trước (`TableSettings.EnableInitTable = true`), script SQL chỉ seed dữ liệu. Tuyệt đối **KHÔNG** `CREATE TABLE`, `ALTER TABLE ... ADD`, `CREATE INDEX` cho bảng đã có entity.

```
CodeFirst tạo khung ➔ 1 ➔ 2 ➔ 4 ➔ 5 ➔ 6 ➔ 7 ➔ 10 ➔ 11 ➔ 12 ➔ 13 ➔ (tùy chọn) G1 ➔ G2
```

1. **Bước 0 — CodeFirst:** khởi động WebAPI (bật tạm `TableSettings.EnableInitTable = true` trong cấu hình debug local) để tạo đủ bảng và index, gồm `ShareDataSql` (câu SQL, bộ biến `VariablesJson`, danh sách trường `FieldsJson` và câu ghi `WriteSql`, unique theo gói + đối tác), `ShareDataLastSend` (mốc nối đuôi nhóm mặc định và nhóm có tên theo `CursorKey`, unique `index_ShareDataLastSend_Partner_Packet_Key` trên `(PartnerCode, PacketCode, CursorKey)`), `ShareDataSourceTable` (unique `UX_ShareDataSourceTable_TableName`).
2. **Bước 1 — chạy tệp 1 → 13** theo bảng mục 3A, bỏ các dòng gạch ngang (mọi DB, kể cả local). DB cũ còn cột thừa ở `ShareDataPacket` thì chạy thêm tệp D1 ở mục 3C.
3. **Bước 2 (tùy chọn, chỉ DB local) — chạy G1 → G2** theo bảng mục 3B.

> 📌 *Lưu ý môi trường Local:* `.vscode/launch.json` ghim các cờ CodeFirst của ConfigId 0 (`DEV_ITS10`) về `false` bằng biến môi trường để bảo vệ CSDL. Khi cần init bảng thì bật tạm thời.

### Tính an toàn khi chạy lại (idempotent)

- Mọi tệp kiểm tra dữ liệu đã có trước khi ghi (`IF NOT EXISTS`, `WHERE NOT EXISTS`, `WHEN NOT MATCHED THEN INSERT`).
- **Cờ `@GhiDeMauGoiTin`** (mặc định `0`, không ghi đè dữ liệu quản trị đã sửa) chỉ còn ở tệp 2: đặt `= 1` để đưa câu mẫu chung trong `ShareDataSql` về mẫu Catalog. Riêng một gói thì đặt `@GhiDeMaGoi` = mã gói.
- **Tệp 7** chỉ thêm tham số `sharedata_config` chưa có, không ghi đè giá trị đang chạy.
- **Tệp 10** chỉ thêm bản dịch còn thiếu, không làm mất bản dịch đã chỉnh sửa.

### Nạp lại câu mẫu chung trên DB đang có dữ liệu (03/11/2026)

Lập trình viên chạy theo thứ tự; AI không tự chạy script ghi DB.

1. Tắt Worker.
2. Chạy tệp 2 với `@GhiDeMauGoiTin = 1`, hoặc `@GhiDeMaGoi` theo từng gói. ⚠️ Cờ này ghi đè câu mẫu chung đã sửa trên màn hình, xem lại trước khi chạy.
3. Chạy tệp 11, rồi tệp 12 nếu chưa chạy.
4. Khởi động lại WebAPI, Worker. Câu mẫu mới đổi công thức cột mốc thì Worker bỏ mốc cũ của đối tác dùng câu mẫu chung, ghi ESH-1308 và gửi lại từ mốc bắt đầu.
5. Câu SQL riêng của đối tác chưa theo quy chuẩn mốc (Worker dừng gói, ESH-1306): mở trang Trường gói tin, tab đối tác, bấm **Sửa câu** rồi lưu.

### Khởi tạo DB mới bằng cờ CodeFirst (thử nghiệm)

> ⚠️ **KHÔNG** bật `EnableInitSeed` trên CSDL đã có dữ liệu (có thể sinh dòng trùng mã `Code`). Seed class C# đã bỏ hoàn toàn (Rule 19.85); bản ghi 11 gói Catalog nằm ở tệp 1, câu SQL mẫu chỉ nằm ở tệp 2.

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
    p.[Status],
    CASE WHEN s.[ExtractSql] IS NOT NULL THEN LEN(s.[ExtractSql]) ELSE 0 END AS [SqlLength]
FROM [dbo].[ShareDataPacket] p
LEFT JOIN [dbo].[ShareDataSql] s ON s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL
ORDER BY p.[OrderNo];
```

---

## ⚠️ 5. Lưu Ý Về CSDL Đích

- Các tệp chạy trên CSDL chính **`[DEV_ITS10]`**. Câu ghi của G3 `MERGE` vào bảng đích `dbo.InboundTrafficFlow` trên `[DEV_ITS10_Inbound]`; thư mục không tạo bảng này, cần thì tạo tay khi kiểm thử chiều nhận.
- **P0 Safeguards (Rule 16):** AI không tự thực thi lệnh ghi lên CSDL. Lập trình viên kiểm tra và thực thi script trên DB.
- **Ghi chú lịch sử:** trước 24/10/2026 thư mục có 4 tệp gộp (`sharedata-seed-goi-tin.sql`, `sharedata-seed-cau-hinh.sql`, `sharedata-dich-thuat.sql`, `sharedata-du-lieu-gia-lap_local.sql`), mỗi tệp ghi nhiều bảng. Nội dung đã tách nguyên vẹn sang bộ tệp trên. Riêng mục 5 cũ của `sharedata-seed-cau-hinh.sql` (ghi chú bộ mã sự cố gói 107 đã gỡ ngày 22/10/2026) chỉ là chú thích, không còn câu lệnh, nên không chép sang.
