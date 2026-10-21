# 🗄️ Khởi Tạo CSDL & Chạy Script Từ Đầu — Phân Hệ ShareData

> ⚠️ **LƯU Ý:** Chạy xong tài liệu này mới sang [06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md](06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md).  
> 📅 **Soạn & Cập nhật:** 19/10/2026 · **Người chạy:** người dùng tự thực thi script SQL trên SSMS / sqlcmd / Azure Data Studio.  
> 📌 **Phạm vi tài liệu:** Chỉ chạy các script SQL trên CSDL SQL Server, **hoàn toàn không mở bất kỳ dự án nào** (không bật WebAPI, Worker hay WebVue).  
> 📌 **Nguyên tắc dữ liệu:** Các script SQL trong tài liệu này **hoàn toàn KHÔNG khởi tạo** dữ liệu cho 3 bảng: Đối tác (`ShareDataPartner`), Ánh xạ (`ShareDataMapping`), và Đăng ký chia sẻ (`ShareDataSubscription`) — các dữ liệu nghiệp vụ này được tạo trên giao diện WebVue tại [tài liệu 06](06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md).

---

## 🗺️ Lộ Trình Thực Thi Script SQL

1. [Chọn CSDL đích & Kiểm tra dữ liệu cũ trước khi bật Worker](#1-chọn-csdl-đích--kiểm-tra-dữ-liệu-cũ-trước-khi-bật-worker) (Mục 1)
2. [Thứ tự chạy script SQL (2 tầng chuẩn)](#2-thứ-tự-chạy-script-sql-2-tầng-chuẩn) (Mục 2)
3. [Kiểm tra đối soát sau khi chạy script SQL](#3-kiểm-tra-đối-soát-sau-khi-chạy-script-sql) (Mục 3)
4. [Khôi phục và dọn dẹp bằng SQL](#4-khôi-phục-và-dọn-dẹp-bằng-sql) (Mục 4)
5. [Phụ lục A–F](#5-phụ-lục) (Mục 5)

---

## 1. Chọn CSDL đích & Kiểm tra dữ liệu cũ trước khi bật Worker

### 1.1. Chọn CSDL đích (Local DEV_ITS10 / Staging 10.10.8.30)

Trước khi thực thi các script SQL, xác định rõ môi trường CSDL mục tiêu:

| Môi trường | SQL Server | CSDL chính | CSDL nhận gói (Inbound) | Ghi chú |
|---|---|---|---|---|
| **Local / Test** | `localhost:1433` (hoặc cổng Docker tùy cấu hình) | `DEV_ITS10` | `DEV_ITS10_Inbound` | CSDL trên máy phát triển cục bộ |
| **Staging** | `10.10.8.30:1433` | `DEV_ITS10` | `DEV_ITS10_Inbound` | CSDL môi trường staging nội bộ (xem thêm tài liệu 06 mục 5.1) |

### 1.2. Kiểm tra dữ liệu cũ trước khi bật Worker

Trước khi khởi động Worker trên CSDL có sẵn dữ liệu cũ (hoặc sau khi khôi phục CSDL), cần kiểm tra các đăng ký chia sẻ (`ShareDataSubscription`) đang ở trạng thái `State = 1` (Active). Nếu các đăng ký này trỏ tới đối tác có địa chỉ thật bên ngoài (`Address` không phải `localhost` hoặc `127.0.0.1`), Worker khi bật lên sẽ lập tức quét và gửi gói tin thử nghiệm ra bên ngoài.

```sql
-- Liệt kê đăng ký chiều gửi (Direction = 0) State = 1 kèm Address/Port của đối tác:
-- (Lưu danh sách ID của câu SELECT trước khi chạy UPDATE để bật lại sau khi nghiệm thu xong)
SELECT s.ID, s.DatatypeId, s.PartnerId, p.Name AS PartnerName, p.ConnectionType, p.Address, p.Port, s.State
FROM dbo.ShareDataSubscription s
JOIN dbo.ShareDataPartner p ON s.PartnerId = p.ID
WHERE s.IsDelete IS NULL AND s.Direction = 0 AND s.State = 1;

-- Đưa State về 2 (Paused) cho đăng ký không trỏ localhost:
UPDATE s
SET s.State = 2, s.UpdateTime = GETDATE()
FROM dbo.ShareDataSubscription s
JOIN dbo.ShareDataPartner p ON s.PartnerId = p.ID
WHERE s.IsDelete IS NULL AND s.Direction = 0 AND s.State = 1
  AND (p.Address IS NULL OR (p.Address NOT LIKE '%localhost%' AND p.Address NOT LIKE '%127.0.0.1%'));
```

> 💡 **Lưu ý dữ liệu cũ:**
> - Trên CSDL đã vận hành trước đó, các gói tin có thể mang ID kiểu GUID (các ID cố định trong Phụ lục C chỉ áp dụng khi nạp mới từ đầu).
> - Nếu phát hiện gói tin dư thừa không nằm trong danh mục chuẩn (ví dụ gói thử nghiệm cũ `101_commonData1`), cần đánh dấu xóa mềm (`UPDATE dbo.ShareDataPacket SET IsDelete = GETDATE() WHERE Code = '101_commonData1';`) hoặc tính đến các gói này khi đối chiếu số lượng trong các câu truy vấn đếm.


---

## 2. Thứ tự chạy script SQL (2 tầng chuẩn)

##### Bảng phân loại nguồn nạp dữ liệu (Giao diện vs. Script vs. Tự ghi)

| Bảng | Màn hình | Cách nạp (Giao diện / Script / Hệ thống tự ghi) | Script |
|---|---|---|---|
| `ShareDataPartner` | `views/sharedata/sharing/index.vue` | **Giao diện** | *Không có script seed (tạo UI ở Bước 1)* |
| `ShareDataPacket` | `views/sharedata/dataSource/index.vue` | **Giao diện** | Script `sharedata-seed-goi-tin.sql` *(thứ tự 1)* |
| `ShareDataPacketField` | `views/sharedata/dataSource/component/fieldConfig.vue` | **Giao diện** | *Sinh khi trích xuất hoặc cấu hình trên UI* |
| `ShareDataCodeSet` | `views/sharedata/codeSet/index.vue` | **Giao diện** | *Cấu hình trên UI ở Bước 3* |
| `ShareDataMapping` | `views/sharedata/mapping/index.vue` | **Giao diện** | *Cấu hình trên UI ở Bước 3* |
| `ShareDataSubscription` | `views/sharedata/sharing/index.vue` | **Giao diện** | *Cấu hình trên UI ở Bước 4* |
| `SysOpConfig` (nhóm `sharedata_config`) | `views/cfgSystem/sysOpConfig/index.vue` *(màn Cấu hình hoạt động)* | **Giao diện** | Script `sharedata-seed-cau-hinh.sql` *(thứ tự 2, sửa trên màn Cấu hình hoạt động)* |
| `SysConfigData` (Mã lỗi ESH) | `views/cfgSystem/sysConfigType/index.vue` | **Giao diện** | Script `sharedata-seed-cau-hinh.sql` *(thứ tự 2)* |
| `SysTerminology` | `views/cfgSystem/sysTerminology/index.vue` | **Giao diện** | Script `sharedata-dich-thuat.sql` *(thứ tự 3, sau đó bấm 🔄)* |
| `ShareDataSourceTable` | *Không có màn hình* | **Bắt buộc script** | Script `sharedata-seed-cau-hinh.sql` *(thứ tự 2)* |
| `ShareDataPacketSql` | *Không có màn hình* | **Bắt buộc script** | Script `sharedata-du-lieu-gia-lap.sql` *(thứ tự 4, chỉ cho gói test; gói nhận thật nạp qua script 1)* |
| `InboundTrafficFlow` | *Không có màn hình* | **Bắt buộc script** | Script `sharedata-du-lieu-gia-lap.sql` *(thứ tự 4, chỉ local/test trên `DEV_ITS10_Inbound`)* |
| `ShareDataSession` | *Không có màn hình* | **Hệ thống tự ghi** | *Tự động ghi khi đối tác kết nối/xác thực* |
| `ShareDataLastSend` | *Không có màn hình* | **Hệ thống tự ghi** | *Worker gửi tự động cập nhật mốc* |
| `ShareDataInboundPacket` | Không có màn hình (tra bằng SQL trên DEV_ITS10_Inbound) | **Hệ thống tự ghi** | *Inbound Worker tiếp nhận gói tin thô* |
| `ShareDataActivityLog` | `views/sharedata/history/index.vue` *(tra cứu)* | **Hệ thống tự ghi** | *WebAPI & Worker tự động ghi nhật ký* |
| `ShareDataAlertLog` | `views/sharedata/errorLog/index.vue` *(tra cứu)* | **Hệ thống tự ghi** | *Worker tự động ghi cảnh báo lỗi ESH* |


### 2.1. Thứ tự chạy 2 tầng

> 📌 **Thứ tự ưu tiên chuẩn bị dữ liệu:**  
> - **Bước 1 (Bắt buộc chạy `.sql`):** CodeFirst tạo khung bảng trước $\rightarrow$ (1) `sharedata-seed-goi-tin.sql` $\rightarrow$ (2) `sharedata-seed-cau-hinh.sql` (mã lỗi ESH, `ShareDataSourceTable`, `sharedata_config`) $\rightarrow$ (3) `sharedata-dich-thuat.sql` (thuật ngữ; làm mới từ điển ở tài liệu 06) $\rightarrow$ (4) `sharedata-du-lieu-gia-lap.sql` (tùy chọn: chỉ Khối 4 khi cần chuẩn bị E2E Inbound trên Staging: `E2E_INBOUND_TEST`, `ShareDataPacketSql`, `InboundTrafficFlow`).  
> - **Bước 2 (Chọn một trong hai):** Gói tin = script 1 (`sharedata-seed-goi-tin.sql`) hoặc FE (*Cấu hình gói tin* $\rightarrow$ *Mẫu mặc định* $\rightarrow$ *Lưu* — xem [tài liệu 06 mục 3.1 Bước 2](06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md#bước-2--khởi-tạo-và-cấu-hình-gói-tin-chia-sẻ-sharedatapacket)); Tham số = sửa trên *Cấu hình hoạt động*.  
> - **Bước 3 (Chỉ FE):** Đối tác, Ánh xạ, Đăng ký (xem [tài liệu 06 mục 3.1 Bước 1, 3, 4](06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md#31-dựng-dữ-liệu-nền-trên-giao-diện-webvue)).  
> - **Ghi chú CSDL cũ:** Xóa mềm gói thừa (ví dụ `101_commonData1`) trước khi chạy script 1 (`sharedata-seed-goi-tin.sql`), vì script khớp mã theo tiền tố.

#### Tầng 1: Bắt buộc chạy script SQL (1 → 3, 4 tùy chọn)
Toàn bộ cấu trúc bảng và index do CodeFirst tạo trước (`TableSettings.EnableInitTable = true`). Các script di trú di sản (`00`, `01`, `06`) đã hoàn tất và được loại bỏ sạch sẽ theo Rule 19.85. Thứ tự thực thi gồm:
1. `sharedata-seed-goi-tin.sql` (Thứ tự 1): Khởi tạo 11 gói tin Catalog (101–111) nếu chưa tồn tại, nạp câu truy vấn SQL mẫu Catalog 101–111, nạp 18 câu ghi chiều nhận (`ShareDataPacketSql`) cho 10 gói (101–109 và 111), gán `SyncMode` mặc định (Snapshot 101/102/105/108/110, Incremental 103/104/106/107/109/111); bật gói 101–110, tắt gói 111. Cờ `@GhiDeMauGoiTin = 0` giữ nguyên các câu SQL tùy biến đã có.
2. `sharedata-seed-cau-hinh.sql` (Thứ tự 2): Khởi tạo `SysConfigType` (`sharedata_error_code`), nạp **25** mã cảnh báo lỗi ESH-xxxx vào `SysConfigData`; nạp 15 bảng nguồn vào bảng danh mục riêng `ShareDataSourceTable` (14 bảng theo dõi `IsTracked = 1`, `TmsEquipment` mặc định `IsTracked = 0`, cả 15 bảng `IsQueryable = 1`); đăng ký nhóm `sharedata_config` vào `SysConfigData` (`operation_config_type`) và nạp **48** tham số vận hành vào `SysOpConfig`.
3. `sharedata-dich-thuat.sql` (Thứ tự 3): Nạp **94** khóa bản dịch vi/en (**188** bản ghi song ngữ) vào `SysTerminology` cho các nhãn giao diện, bộ lọc, kiểm tra an toàn SQL (bao gồm khóa `SqlTableNotAllowed`), và 12 khóa lỗi kết nối đối tác (`sharedata.connectConfig*`). Sau khi chạy xong, thực hiện đồng bộ/làm mới từ điển trên giao diện ở [tài liệu 06 mục 2.2](06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md#22-làm-mới-từ-điển-đa-ngôn-ngữ-trên-giao-diện).
4. `sharedata-du-lieu-gia-lap.sql` (Thứ tự 4, tùy chọn): **Chỉ chạy Khối 4 trên Staging** (chuẩn bị hạ tầng cho kịch bản chiều nhận E0: tạo gói kiểm thử riêng `E2E_INBOUND_TEST` ID `1500000000900`, bảng đích `InboundTrafficFlow` trên `DEV_ITS10_Inbound` và cấu hình câu ghi MERGE trong `ShareDataPacketSql` ID `1500000000903`).
- ⛔ **Cảnh báo môi trường:** Script 4 `sharedata-du-lieu-gia-lap.sql` Khối 1–3, 5–7 (dữ liệu giả lập 104, 106, 110, 111) **chỉ chạy trên Local/Test, TUYỆT ĐỐI CẤM chạy trên Staging** vì staging đã có dữ liệu thật.

#### Tầng 2: Chọn một trong hai cách nạp gói tin
- **Cách A (Script SQL — Nạp nhanh):** Chạy `sharedata-seed-goi-tin.sql` (thứ tự 1: tạo mới các gói tin còn thiếu trong 11 gói chuẩn 101–111 với ID cố định; nạp câu truy vấn SQL mẫu, nạp câu ghi chiều nhận (`ShareDataPacketSql`) cho các gói đã có câu ghi; gán `SyncMode` mặc định; bật gói 110, tắt gói 111).
  *Ghi chú CSDL cũ:* Xóa mềm gói thừa (ví dụ `101_commonData1`) trước khi chạy script 1, vì script khớp mã theo tiền tố.
- **Cách B (Giao diện WebVue):** Khởi tạo gói tin và nạp SQL trích xuất trực tiếp trên màn hình *Cấu hình gói tin* $\rightarrow$ tab *Cấu hình SQL* $\rightarrow$ bấm *Mẫu mặc định* $\rightarrow$ bấm *Lưu* (xem chi tiết tại [tài liệu 06 mục 3.1 Bước 2](06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md#bước-2--khởi-tạo-và-cấu-hình-gói-tin-chia-sẻ-sharedatapacket)).
- **Tham số vận hành (`SysOpConfig`):** Đã được nạp ở script 2 (`sharedata-seed-cau-hinh.sql`), nếu muốn điều chỉnh có thể sửa trực tiếp trên màn *Cấu hình hoạt động* ([tài liệu 06 mục 3.1 Bước 8](06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md#bước-8-tùy-chọn--cấu-hình-tham-số-vận-hành-sharedata-sysopconfig-nhóm-sharedata_config)).

### 2.2. Danh mục chi tiết các script SQL (1–4)

> 💡 *Các script di sản `00-sharedata-migration-mot-lan.sql`, `01-sharedata-cau-truc.sql` và `06-sharedata-doi-ten-cot-dong-bo.sql` đã hoàn tất nhiệm vụ di trú và được loại bỏ sạch sẽ theo Rule 19.85 (CodeFirst là nguồn cấu trúc duy nhất).*

| Thứ Tự | Script | CSDL | Mục đích thực tế chính xác | Áp dụng cho DB mới | Áp dụng cho DB cũ |
|:---:|---|---|---|:---:|:---:|
| **1** | [`sharedata-seed-goi-tin.sql`](../sql/sharedata-seed-goi-tin.sql) | `DEV_ITS10` | Tạo mới các gói tin còn thiếu trong 11 gói chuẩn (101–111) với ID cố định; nạp câu truy vấn SQL mẫu Catalog 101–111 (`TemplateSql` và `ExtractSql`), nạp 18 câu ghi chiều nhận (`ShareDataPacketSql`) cho 10 gói đã có câu ghi (101–109 và 111); gán `SyncMode` mặc định (Snapshot 101/102/105/108/110, Incremental 103/104/106/107/109/111); bật gói 101–110, tắt gói 111. Cờ `@GhiDeMauGoiTin = 0` giữ nguyên các câu SQL tùy biến đã có. Gói đã xóa mềm cùng tiền tố không chặn việc tạo gói chuẩn. | **Bắt buộc** | **Bắt buộc** |
| **2** | [`sharedata-seed-cau-hinh.sql`](../sql/sharedata-seed-cau-hinh.sql) | `DEV_ITS10` | Khởi tạo `SysConfigType` (`sharedata_error_code`), nạp **25** mã cảnh báo lỗi ESH-xxxx vào `SysConfigData`; nạp 15 bảng nguồn vào bảng danh mục riêng `ShareDataSourceTable` (14 bảng theo dõi `IsTracked = 1`, `TmsEquipment` mặc định `IsTracked = 0`, cả 15 bảng `IsQueryable = 1`); đăng ký nhóm `sharedata_config` vào `SysConfigData` (`operation_config_type`) và nạp **48** tham số vận hành vào `SysOpConfig`. | **Bắt buộc** | **Bắt buộc** |
| **3** | [`sharedata-dich-thuat.sql`](../sql/sharedata-dich-thuat.sql) | `DEV_ITS10` | Nạp **94** khóa bản dịch vi/en (**188** bản ghi song ngữ) vào `SysTerminology` cho các nhãn giao diện, bộ lọc, kiểm tra an toàn SQL (bao gồm khóa `SqlTableNotAllowed`), và 12 khóa lỗi kết nối đối tác (`sharedata.connectConfig*`). **Bắt buộc chạy trước mục C**. | **Bắt buộc** | **Bắt buộc** |
| **4** | [`sharedata-du-lieu-gia-lap.sql`](../sql/sharedata-du-lieu-gia-lap.sql) | `DEV_ITS10`, `DEV_ITS10_Inbound` | Nạp dữ liệu giả lập cho gói 104 (Thời tiết), 106 (WIM), 110 (Sự cố & VMS), 111 (Tín hiệu) và Khối 4 chuẩn bị cho kịch bản E0 (tạo gói kiểm thử riêng `E2E_INBOUND_TEST` ID `1500000000900`, bảng đích `InboundTrafficFlow` trên `DEV_ITS10_Inbound` và cấu hình câu ghi MERGE trong `ShareDataPacketSql` ID `1500000000903`). Tích hợp sẵn cờ dọn dẹp `@XoaDuLieuGiaLap`. | *Tùy chọn* (cần cho E, G) | *Tùy chọn* (cần cho E, G) |



### 2.3. Khởi tạo cấu trúc bảng bằng CodeFirst (áp dụng cho mọi CSDL)

##### Bước 1: Khởi tạo khung bảng bằng CodeFirst (áp dụng cho mọi CSDL)
1. Mở file [`.vscode/launch.json`](../../../../.vscode/launch.json).
2. Sao chép cấu hình `🌐 TAC_WebAPI (Local Docker)` thành một bản ghi tạm (ví dụ `🌐 TAC_WebAPI (Init DB Schema Temporary)`).
3. Trong khối `env` của bản ghi tạm, ghi đè các cờ khởi tạo CSDL:
   ```json
   "DbConnection__ConnectionConfigs__0__DbSettings__EnableInitDb": "true",
   "DbConnection__ConnectionConfigs__0__TableSettings__EnableInitTable": "true",
   "DbConnection__ConnectionConfigs__0__TableSettings__EnableIncreTable": "false"
   ```
   Nếu CSDL chưa có CSDL `DEV_ITS10_Inbound`, bổ sung thêm:
   ```json
   "DbConnection__ConnectionConfigs__3__DbSettings__EnableInitDb": "true"
   ```
4. Khởi động WebAPI với cấu hình tạm này cho tới khi console in thông báo `"Now listening on: http://localhost:5005"`.
5. Bấm dừng WebAPI và xóa bỏ bản ghi cấu hình tạm trong `launch.json`. ⛔ **TUYỆT ĐỐI KHÔNG sửa trực tiếp và KHÔNG commit file `Database.json` hay `Database.Development.json`**.

##### Bước 2: Nạp dữ liệu seed chuẩn (theo thứ tự script)
- Chạy lần lượt các script seed chuẩn: (1) `sharedata-seed-goi-tin.sql` $\rightarrow$ (2) `sharedata-seed-cau-hinh.sql` $\rightarrow$ (3) `sharedata-dich-thuat.sql` $\rightarrow$ (4) (tùy chọn) `sharedata-du-lieu-gia-lap.sql`.
- ⚠️ **Lưu ý về danh mục bảng nguồn và câu lệnh trích xuất:** Bảng `ShareDataSourceTable` lưu trữ danh mục bảng nguồn phục vụ Change Tracking và theo dõi sự kiện NATS; câu lệnh trích xuất dữ liệu của gói tin (`ExtractSql`) được cấu hình trực tiếp bằng SQL trên tab Cấu hình SQL của màn hình Nguồn dữ liệu.

##### Truy vấn kiểm tra cấu trúc cột trên `ShareDataPacket` (chỉ đọc):
```sql
SELECT name, system_type_id, max_length, is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.ShareDataPacket')
  AND name IN ('SyncKind','SyncMode','TimeColumn','CursorColumn','CursorTimeColumn','LookbackDays','MaxLookBackDays');
```
*Kết quả mong đợi:* Phải thấy các cột mới `SyncMode` (int), `CursorTimeColumn` (nvarchar), `MaxLookBackDays` (int).


---

## 3. Kiểm tra đối soát sau khi chạy script SQL

Chạy các truy vấn SQL chỉ đọc sau trên CSDL `[DEV_ITS10]` để đối soát số lượng và cấu hình sau khi chạy script:
```sql
-- 1. Kiểm tra 11 gói tin chuẩn (10 bật, 1 tắt - gói 111):
SELECT COUNT(1) AS TotalPackets,
       SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS EnabledPackets,
       SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS DisabledPackets
FROM dbo.ShareDataPacket
WHERE IsDelete IS NULL
  AND Code IN ('101_commonData','102_cctvData','103_vdsData','104_weatherData','105_rfidData','106_wimData','107_incidentData','108_vmsInfo','109_etcData','110_wpData','111');
-- Mong đợi: TotalPackets = 11, EnabledPackets = 10, DisabledPackets = 1

-- 2. Kiểm tra 25 mã cảnh báo lỗi ESH-xxxx:
SELECT COUNT(1) AS TotalErrors
FROM dbo.SysConfigData d
JOIN dbo.SysConfigType t ON d.ConfigTypeId = t.ID
WHERE t.Code = 'sharedata_error_code' AND d.IsDelete IS NULL;
-- Mong đợi: TotalErrors = 25

-- 3. Kiểm tra 15 bảng nguồn ShareDataSourceTable (14 theo dõi IsTracked = 1, 1 tắt - TmsEquipment; cả 15 cho phép truy vấn IsQueryable = 1):
SELECT COUNT(1) AS TotalSources,
       SUM(CASE WHEN IsTracked = 1 THEN 1 ELSE 0 END) AS TrackedSources,
       SUM(CASE WHEN IsTracked = 0 THEN 1 ELSE 0 END) AS UntrackedSources,
       SUM(CASE WHEN IsQueryable = 1 THEN 1 ELSE 0 END) AS QueryableSources
FROM dbo.ShareDataSourceTable
WHERE IsDelete IS NULL;
-- Mong đợi: TotalSources = 15, TrackedSources = 14, UntrackedSources = 1, QueryableSources = 15

-- 4. Kiểm tra các khóa dịch thuật ShareData (bao gồm khóa mới SqlTableNotAllowed):
SELECT Code, Lang AS [Language], Value AS [Terminology]
FROM dbo.SysTerminology
WHERE IsDelete IS NULL
  AND Code IN ('sharedata.sqlTableNotAllowed', 'sharedata.connectConfigFileNamePatternNoToken', 'sharedata.lookbackDaysRange')
ORDER BY Code, Lang;

-- Đếm tổng số bản ghi thuật ngữ ShareData theo danh mục script 04:
SELECT COUNT(1) AS TotalTerms,
       COUNT(DISTINCT Code) AS UniqueKeys
FROM dbo.SysTerminology
WHERE Remark = N'ShareData Localization'
  AND IsDelete IS NULL;
-- Mong đợi: TotalTerms = 186 (93 khóa song ngữ vi-VN và en-US), UniqueKeys = 93

-- 5. Kiểm tra 48 tham số vận hành SysOpConfig nhóm sharedata_config:
SELECT COUNT(1) AS TotalConfigs
FROM dbo.SysOpConfig
WHERE GroupCode = 'sharedata_config' AND IsDelete IS NULL;
-- Mong đợi: TotalConfigs = 48 (19 Worker + 23 FE + 6 BE)

-- 6. Kiểm tra Đối tác, Ánh xạ và Đăng ký chia sẻ:
SELECT COUNT(1) AS TotalPartners,
       SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS EnabledPartners
FROM dbo.ShareDataPartner WHERE IsDelete IS NULL;
-- Mong đợi: TotalPartners >= 1 (có P-REST)

SELECT COUNT(1) AS TotalMappings,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveMappings,
       SUM(CASE WHEN Direction = 0 THEN 1 ELSE 0 END) AS OutboundMappings,
       SUM(CASE WHEN Direction = 1 THEN 1 ELSE 0 END) AS InboundMappings,
       SUM(CASE WHEN Direction = 2 THEN 1 ELSE 0 END) AS BothMappings
FROM dbo.ShareDataMapping WHERE IsDelete IS NULL;
-- Mong đợi: TotalMappings >= 2 (có ánh xạ gửi 101 và nhận 101)

SELECT COUNT(1) AS TotalSubscriptions,
       SUM(CASE WHEN State = 1 THEN 1 ELSE 0 END) AS ActiveSubscriptions,
       SUM(CASE WHEN Direction = 0 THEN 1 ELSE 0 END) AS OutboundSubscriptions,
       SUM(CASE WHEN Direction = 1 THEN 1 ELSE 0 END) AS InboundSubscriptions
FROM dbo.ShareDataSubscription WHERE IsDelete IS NULL;
-- Mong đợi: TotalSubscriptions >= 2 (có đăng ký gửi 101 và nhận 101 đang Active)
```


### 3.1. Truy vấn kiểm tra chỉ đọc bổ sung (chạy trên DB local)

```sql
SELECT Code, SyncMode, Status, CursorTimeColumn, MaxLookBackDays FROM dbo.ShareDataPacket ORDER BY Code;
SELECT TOP 20 * FROM dbo.ShareDataLastSend;                                   -- mốc gửi nối đuôi
SELECT TOP 30 * FROM dbo.ShareDataActivityLog ORDER BY CreateTime DESC;       -- nhật ký cha–con
SELECT TOP 30 * FROM dbo.ShareDataAlertLog    ORDER BY CreateTime DESC;       -- cảnh báo ESH-xxxx
-- Trên CSDL Inbound:
SELECT TOP 20 * FROM dbo.ShareDataInboundPacket ORDER BY CreateTime DESC;     -- gói nhận
```


---

## 4. Khôi phục và dọn dẹp bằng SQL

Sau khi thực hiện các bài nghiệm thu (hoặc khi cần dọn dẹp CSDL về trạng thái sạch ban đầu), thực hiện các câu lệnh SQL dưới đây:

### 4.1. Khôi phục tham số vận hành `SysOpConfig` (nhóm `sharedata_config`)

   - Đưa các tham số đã sửa thử nghiệm về giá trị mặc định: `outbound_check_interval_sec = '5'`, `packet_lookback_days_max = '365'`, `cleanup_interval_min = '1440'`; khôi phục dòng đã tắt/xóa mềm ở J4 về `Status = 1`, `IsDelete = NULL`.
   > 💡 **Ghi chú:** Script `sharedata-seed-cau-hinh.sql` (thứ tự 2) không ghi đè các giá trị đã tồn tại trong `SysOpConfig` (`WHERE NOT EXISTS`), nên bắt buộc chạy câu lệnh `UPDATE` dưới đây để khôi phục:
     ```sql
     USE [DEV_ITS10];
     UPDATE dbo.SysOpConfig
     SET Value = CASE Code
             WHEN 'outbound_check_interval_sec' THEN '5'
             WHEN 'packet_lookback_days_max' THEN '365'
             WHEN 'cleanup_interval_min' THEN '1440'
             ELSE Value
         END,
         Status = 1,
         IsDelete = NULL,
         UpdateTime = GETDATE()
     WHERE GroupCode = 'sharedata_config'
       AND Code IN ('outbound_check_interval_sec', 'packet_lookback_days_max', 'cleanup_interval_min');
     ```


### 4.2. Khôi phục danh mục 15 bảng nguồn (`ShareDataSourceTable`)

   - Trả lại trạng thái ban đầu của 15 dòng trong bảng `dbo.ShareDataSourceTable` theo đúng [Phụ lục B](#phụ-lục-b-danh-mục-15-bảng-nguồn-sharedata-sharedatasourcetable) (xóa dòng bảng thử nghiệm `TableName = N'BangKhongTonTai_Test'`, khôi phục `IsDelete = NULL`, đưa `TmsEquipment` về trạng thái tắt theo dõi `IsTracked = 0`, 14 bảng còn lại `IsTracked = 1`, và cả 15 bảng `IsQueryable = 1`):
   > 💡 **Lý do lọc theo `TableName` thay vì `ID`:** Tên bảng là khóa tự nhiên duy nhất (`UX_ShareDataSourceTable_TableName`), do đó lọc theo danh sách `TableName` bảo đảm độ chính xác tuyệt đối trên mọi môi trường CSDL.
     ```sql
     USE [DEV_ITS10];
     -- Xóa các dòng bảng thử nghiệm:
     DELETE FROM dbo.ShareDataSourceTable
     WHERE TableName = N'BangKhongTonTai_Test';

     -- Khôi phục 15 bảng nguồn chuẩn theo Phụ lục B:
     UPDATE dbo.ShareDataSourceTable
     SET IsDelete = NULL,
         IsTracked = CASE WHEN TableName = N'TmsEquipment' THEN 0 ELSE 1 END,
         IsQueryable = 1,
         UpdateTime = GETDATE()
     WHERE TableName IN (
         N'TmsZoneStatus', N'TmsZone', N'TmsTrafficStatistic', N'CctvDevice',
         N'TmsTrafficData', N'TmsWeather', N'TollTransactionIn', N'TollTransactionOut',
         N'TmsVehicleRegistration', N'TmsIncident', N'TmsEventType', N'VmsCurrent',
         N'TmsEquipment', N'TollLane', N'TollStation'
     );
     ```


### 4.3. Xóa dữ liệu thử nghiệm trong bảng đích chiều nhận

     ```sql
     USE [DEV_ITS10_Inbound];
     TRUNCATE TABLE [dbo].[InboundTrafficFlow];
     ```


### 4.4. Xóa cấu hình gói kiểm thử riêng `E2E_INBOUND_TEST`

     ```sql
     USE [DEV_ITS10];
     DELETE FROM dbo.ShareDataPacketSql WHERE ID = '1500000000903' OR PacketCode = 'E2E_INBOUND_TEST';
     DELETE FROM dbo.ShareDataPacket WHERE ID = '1500000000900' OR Code = 'E2E_INBOUND_TEST';
     ```


### 4.5. Bật lại các đăng ký chia sẻ (Subscription) đã tắt ở mục 1.2

```sql
USE [DEV_ITS10];
-- Bật lại các đăng ký chia sẻ theo danh sách ID đã lưu ở mục 1.2:
UPDATE dbo.ShareDataSubscription
SET State = 1, UpdateTime = GETDATE()
WHERE ID IN (<Danh sách ID đã lưu ở mục 1.2>);
```

### 4.6. Xóa đối tác kiểm thử bằng SQL

```sql
USE [DEV_ITS10];
UPDATE dbo.ShareDataPartner
SET IsDelete = GETDATE(), Status = 0
WHERE Code = 'P-REST';
```

### 4.7. DROP bảng kiểm thử `InboundTrafficFlow` trên `DEV_ITS10_Inbound` (khi dọn triệt để sau staging)

```sql
USE [DEV_ITS10_Inbound];
IF OBJECT_ID(N'[dbo].[InboundTrafficFlow]', N'U') IS NOT NULL
    DROP TABLE [dbo].[InboundTrafficFlow];
```

---

## 5. Phụ Lục

### Phụ lục A: Danh mục 25 mã cảnh báo lỗi ESH-xxxx

> 📌 *Nguồn trích xuất chuẩn:* [`sharedata-seed-cau-hinh.sql`](../sql/sharedata-seed-cau-hinh.sql) (`SysConfigType` = `'sharedata_error_code'`) và [`ShareDataAlertCode.cs`](../../../../TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Logging/ShareDataAlertCode.cs).  
> ⚠️ **Chú ý giới hạn độ dài:** Ô *Tên* trên màn hình `sysConfigType` giới hạn tối đa 64 ký tự. Với **9 mã có tên gốc dài hơn 64 ký tự**, bảng dưới cung cấp sẵn **Tên rút gọn (≤ 64 ký tự)** để sử dụng khi nhập bằng tay trên giao diện.

| Mã lỗi | Phân loại | Mức độ | Tên gốc (khi chạy Script 03) | Tên rút gọn (dùng khi nhập tay trên giao diện, ≤ 64 ký tự) | Ghi chú & Hướng xử lý |
|---|---|:---:|---|---|---|
| **ESH-1201** | Outbound | ⚠️ warning | Bộ mã khai trong metadata không tồn tại trong CSDL | *(Giữ nguyên tên gốc — 52 ký tự)* | Kiểm tra lại cấu hình CodeSet trong metadata. |
| **ESH-1202** | Outbound | 🔴 danger | Trường bắt buộc bị thiếu hoặc null — huỷ kết xuất | *(Giữ nguyên tên gốc — 51 ký tự)* | Kiểm tra dữ liệu nguồn hoặc cấu hình trường bắt buộc. |
| **ESH-1203** | Outbound | ⚠️ warning | Biểu thức không qua kiểm tra an toàn tĩnh hoặc lỗi khi tính — giữ giá trị nguồn *(79 ký tự)* | **Lỗi biểu thức biến đổi an toàn tĩnh — giữ nguyên giá trị nguồn** *(62 ký tự)* | Kiểm tra biểu thức biến đổi trong mapping. |
| **ESH-1204** | Outbound | 🔴 danger | TargetShapeJson rỗng hoặc sai cú pháp — huỷ kết xuất | *(Giữ nguyên tên gốc — 54 ký tự)* | Kiểm tra định dạng TargetShapeJson trong mapping. |
| **ESH-1301** | Outbound | 🔴 danger | Không tìm thấy cấu hình gói tin cho DatatypeId | *(Giữ nguyên tên gốc — 48 ký tự)* | Kiểm tra danh mục gói tin ShareDataPacket. |
| **ESH-1302** | Outbound | 🔴 danger | Lỗi truy vấn dữ liệu nguồn của gói tin | *(Giữ nguyên tên gốc — 39 ký tự)* | Kiểm tra câu lệnh SQL trích xuất và kết nối CSDL. |
| **ESH-1303** | Outbound | 🔴 danger | Không tìm thấy phễu lọc (ShareDataMapping) chiều gửi đang bật — huỷ kết xuất *(76 ký tự)* | **Không có phễu lọc ShareDataMapping chiều gửi bật — hủy kết xuất** *(62 ký tự)* | Kiểm tra cấu hình mapping giữa đối tác và gói tin. |
| **ESH-1304** | Outbound | 🔴 danger | Khoá bản ghi cuối trang (__rowid) dài hơn cột LastKey cho phép — huỷ kết xuất trước khi gửi *(91 ký tự)* | **Khóa cuối trang __rowid dài hơn LastKey — hủy kết xuất gửi** *(58 ký tự)* | Kiểm tra kiểu dữ liệu hoặc giá trị khóa __rowid. |
| **ESH-1305** | Outbound | 🔴 danger | Không tìm thấy câu lệnh SQL trích xuất dữ liệu ở bất kỳ tầng nào *(67 ký tự)* | **Không tìm thấy câu lệnh SQL trích xuất dữ liệu của gói tin** *(58 ký tự)* | Kiểm tra câu truy vấn SQL trong ShareDataPacket. |
| **ESH-1306** | Outbound | 🔴 danger | Thứ tự con trỏ dữ liệu không tăng dần hoặc thiếu khóa __rowid | *(Giữ nguyên tên gốc — 63 ký tự)* | Kiểm tra mệnh đề ORDER BY trong câu lệnh trích xuất dữ liệu. |
| **ESH-1307** | Outbound | ⚠️ warning | Mốc con trỏ gửi mới lùi lại so với mốc hiện tại trong CSDL | *(Giữ nguyên tên gốc — 60 ký tự)* | Kiểm tra mốc đồng bộ con trỏ và dữ liệu thời gian. |
| **ESH-1401** | Outbound | ⚠️ warning | Gửi HTTP REST trực tiếp tới đối tác thất bại — file đã ghi vẫn coi là thành công *(80 ký tự)* | **Gửi HTTP REST thất bại — file đã ghi vẫn tính thành công** *(56 ký tự)* | Kiểm tra kết nối mạng và endpoint của đối tác. |
| **ESH-1402** | Outbound | 🔴 danger | Lưu đệm payload/bằng chứng thất bại, hủy gửi và giữ nguyên mốc | *(Giữ nguyên tên gốc — 64 ký tự)* | Kiểm tra quyền ghi thư mục NasStorage:BasePath hoặc dung lượng ổ đĩa. |
| **ESH-1403** | Outbound | ⚠️ warning | Giá trị cấu hình kết nối không hợp lệ bị bỏ qua hoặc thay bằng mặc định *(71 ký tự)* | **Cấu hình kết nối không hợp lệ — đã dùng giá trị mặc định** *(56 ký tự)* | Kiểm tra các khóa cấu hình trong ConnectionConfig của đối tác. |
| **ESH-1501** | Inbound | 🔴 danger | Không đọc được nội dung gói tin nhận về | *(Giữ nguyên tên gốc — 40 ký tự)* | Kiểm tra định dạng JSON của gói tin nhận. |
| **ESH-1502** | Inbound | 🔴 danger | Lỗi ghi dữ liệu xuống CSDL nhận | *(Giữ nguyên tên gốc — 33 ký tự)* | Kiểm tra kết nối và quyền ghi CSDL Inbound. |
| **ESH-1503** | Inbound | 🔴 danger | Không tìm thấy cấu hình gói tin cho DatatypeId nhận về | *(Giữ nguyên tên gốc — 55 ký tự)* | Kiểm tra danh mục gói tin chiều nhận trong ShareDataPacket. |
| **ESH-1504** | Inbound | 🔴 danger | Đối tác của gói tin không tồn tại hoặc đang tắt | *(Giữ nguyên tên gốc — 48 ký tự)* | Kiểm tra danh mục đối tác ShareDataPartner. |
| **ESH-1505** | Inbound | 🔴 danger | Gói tin chưa có đăng ký nhận hợp lệ | *(Giữ nguyên tên gốc — 36 ký tự)* | Kiểm tra cấu hình mapping chiều nhận ShareDataMapping. |
| **ESH-1506** | Inbound | 🔴 danger | Gói tin chưa khai câu ghi chiều nhận (ShareDataPacketWrite) | *(Giữ nguyên tên gốc — 60 ký tự)* | Kiểm tra cấu hình câu ghi CSDL trong ShareDataPacketSql. |
| **ESH-1507** | Inbound | 🔴 danger | Giải mã gói tin nhận về thất bại (sai khóa, sai envelope hoặc sai tag) *(70 ký tự)* | **Giải mã gói tin nhận thất bại (sai khóa, envelope hoặc tag)** *(58 ký tự)* | Kiểm tra thuật toán mã hóa hoặc khóa bí mật khi áp dụng. |
| **ESH-1601** | Tracking | ⚠️ warning | Mốc Change Tracking không hợp lệ — đã tự nhảy cóc để hồi phục | *(Giữ nguyên tên gốc — 63 ký tự)* | Hệ thống tự động đồng bộ lại mốc Change Tracking khi vượt cleanup. |
| **ESH-1602** | Tracking | 🔴 danger | Truy vấn CHANGETABLE lỗi vì lý do KHÁC version — đã bỏ cache câu SQL và hẹn quét lại *(84 ký tự)* | **Lỗi CHANGETABLE khác version — bỏ cache SQL và hẹn quét lại** *(58 ký tự)* | Kiểm tra cú pháp hoặc quyền truy cập CHANGETABLE trên bảng nguồn. |
| **ESH-1603** | Tracking | ⚠️ warning | CSDL hoặc bảng nguồn chưa bật Change Tracking — polling tạm dừng | *(Giữ nguyên tên gốc — 64 ký tự)* | Kiểm tra cấu hình ALTER DATABASE / TABLE ENABLE CHANGE_TRACKING. |
| **ESH-1604** | Tracking | ⚠️ warning | Mốc đang giữ không còn tương thích CSDL — đã hạ mốc về -1 để đi lại đường khởi tạo *(82 ký tự)* | **Mốc không tương thích CSDL — hạ mốc về -1 để khởi tạo lại** *(57 ký tự)* | Tracking bị tắt/bật lại hoặc CSDL restore từ bản cũ hơn. |

---

### Phụ lục B: Danh mục 15 bảng nguồn ShareData (`ShareDataSourceTable`)

> 📌 *Nguồn trích xuất chuẩn:* [`sharedata-seed-cau-hinh.sql`](../sql/sharedata-seed-cau-hinh.sql) (Mục 3.4).

| STT | ID cố định (`ID`) | Tên bảng CSDL (`TableName`) | Tên hiển thị (`Name`) | Gói tin kích hoạt (`PacketCodes`) | Thứ tự (`OrderNo`) | Theo dõi CT (`IsTracked`) | Bộ dựng SQL (`IsQueryable`) | Bí danh (`DefaultAlias`) | Ghi chú vai trò (`Remark`) |
|:---:|:---:|---|---|---|:---:|:---:|:---:|:---:|---|
| 1 | `1500000000201` | `TmsZoneStatus` | Trạng thái khu vực giao thông | `101,101_commonData` | 10 | Bật (1) | Bật (1) | `zs` | Giám sát thay đổi trạng thái zone để kích hoạt gói dữ liệu dùng chung (101). |
| 2 | `1500000000202` | `TmsZone` | Danh mục khu vực giao thông | `101,101_commonData` | 20 | Bật (1) | Bật (1) | `z` | Giám sát thay đổi danh mục zone để kích hoạt gói dữ liệu dùng chung (101). |
| 3 | `1500000000203` | `TmsTrafficStatistic` | Thống kê lưu lượng giao thông | `101,101_commonData` | 30 | Bật (1) | Bật (1) | `tstat` | Giám sát dữ liệu thống kê giao thông để kích hoạt gói dữ liệu dùng chung (101). |
| 4 | `1500000000204` | `CctvDevice` | Thiết bị camera giám sát | `102,102_cctvData` | 40 | Bật (1) | Bật (1) | `c` | Giám sát dữ liệu camera để kích hoạt gói dữ liệu CCTV (102). |
| 5 | `1500000000205` | `TmsTrafficData` | Dữ liệu lưu lượng VDS và WIM | `103,103_vdsData,106,106_wimData` | 50 | Bật (1) | Bật (1) | `td` | Giám sát dữ liệu đo đếm giao thông kích hoạt gói VDS (103) và WIM cân tải trọng (106). |
| 6 | `1500000000206` | `TmsWeather` | Dữ liệu khí tượng thời tiết | `104,104_weatherData` | 60 | Bật (1) | Bật (1) | `w` | Giám sát dữ liệu trạm khí tượng để kích hoạt gói thời tiết (104). |
| 7 | `1500000000207` | `TollTransactionIn` | Giao dịch thu phí đầu vào | `105,105_rfidData,109,109_etcData` | 70 | Bật (1) | Bật (1) | `tin` | Giám sát giao dịch vào trạm kích hoạt gói RFID (105) và gói ETC thu phí (109). |
| 8 | `1500000000208` | `TollTransactionOut` | Giao dịch thu phí đầu ra | `105,105_rfidData,109,109_etcData` | 80 | Bật (1) | Bật (1) | `tout` | Giám sát giao dịch ra trạm kích hoạt gói RFID (105) và gói ETC thu phí (109). |
| 9 | `1500000000209` | `TmsVehicleRegistration` | Đăng ký phương tiện | `105,105_rfidData` | 90 | Bật (1) | Bật (1) | `vr` | Giám sát dữ liệu phương tiện đăng ký kích hoạt gói RFID (105). |
| 10 | `1500000000210` | `TmsIncident` | Sự cố giao thông | `107,107_incidentData,110,110_wpData,111` | 100 | Bật (1) | Bật (1) | `i` | Giám sát sự cố giao thông kích hoạt gói sự cố (107) và các gói tích hợp phụ trợ (110, 111). |
| 11 | `1500000000211` | `TmsEventType` | Loại sự kiện giao thông | `107,107_incidentData,110,110_wpData,111` | 110 | Bật (1) | Bật (1) | `et` | Giám sát danh mục loại sự kiện giao thông kích hoạt gói sự cố (107) và phụ trợ (110, 111). |
| 12 | `1500000000212` | `VmsCurrent` | Thông tin bản tin biển báo VMS | `108,108_vmsInfo` | 120 | Bật (1) | Bật (1) | `v` | Giám sát trạng thái hiển thị biển báo VMS kích hoạt gói tin VMS (108). |
| 13 | `1500000000213` | `TmsEquipment` | Danh mục thiết bị phần cứng | `102,102_cctvData,103,103_vdsData,108,108_vmsInfo` | 130 | **Tắt (0)** | Bật (1) | `eq` | Bảng tra cứu thiết bị, tắt giám sát Change Tracking (IsTracked = 0) để tránh trigger quét thừa cho 102/103/108 khi chỉ cập nhật thông tin thiết bị, nhưng vẫn mở cho bộ dựng truy vấn (IsQueryable = 1). |
| 14 | `1500000000214` | `TollLane` | Danh mục làn thu phí | `109,109_etcData` | 140 | Bật (1) | Bật (1) | `tl` | Giám sát danh mục làn thu phí trạm kích hoạt gói dữ liệu ETC (109). |
| 15 | `1500000000215` | `TollStation` | Danh mục trạm thu phí | `109,109_etcData` | 150 | Bật (1) | Bật (1) | `ts` | Giám sát danh mục trạm thu phí kích hoạt gói dữ liệu ETC (109). |

---

### Phụ lục C: Danh mục chuẩn 11 gói tin ShareData

> 📌 *Nguồn trích xuất chuẩn:* [`sharedata-seed-goi-tin.sql`](../sql/sharedata-seed-goi-tin.sql) và `PacketDefaultTemplate` (`Module.ShareData.Infrastructure.Services.ShareDataExtraction`).

| Gói | Số thứ tự (`OrderNo`) | Mã gói chuẩn (`Code`) | ID cố định | Tên gói tin (`Name`) | Chế độ (`SyncMode`) | Cột mốc (`CursorTimeColumn`) | Quét lùi (`MaxLookBackDays`) | Trạng thái (`Status`) |
|:---:|:---:|---|:---:|---|:---:|:---:|:---:|:---:|
| **101** | 101 | `101_commonData` | `1500000000101` | Dữ liệu thông tin chung / luồng giao thông | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **102** | 102 | `102_cctvData` | `1500000000102` | Dữ liệu hình ảnh giao thông CCTV | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **103** | 103 | `103_vdsData` | `1500000000103` | Dữ liệu đếm xe VDS | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **104** | 104 | `104_weatherData` | `1500000000104` | Dữ liệu khí tượng / thời tiết | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **105** | 105 | `105_rfidData` | `1500000000105` | Dữ liệu định danh phương tiện AVI / RFID | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **106** | 106 | `106_wimData` | `1500000000106` | Dữ liệu cân tải trọng xe tự động WIM | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **107** | 107 | `107_incidentData` | `1500000000107` | Dữ liệu thông tin sự cố / sự kiện giao thông | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **108** | 108 | `108_vmsInfo` | `1500000000108` | Dữ liệu thông tin biển báo điện tử VMS | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **109** | 109 | `109_etcData` | `1500000000109` | Dữ liệu giao dịch thu phí ETC | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **110** | 110 | `110_wpData` | `1500000000110` | Dữ liệu trao đổi thông tin với người tham gia giao thông | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **111** | 111 | `111` | `1500000000111` | Dữ liệu trao đổi thông tin giữa các TT QLĐHGT tuyến | *NULL* | *NULL* | *NULL* | **Tắt (0)** |

---

### Phụ lục D: Mẫu cấu hình chiều nhận E0 (Bảng đích, câu MERGE và TargetShapeJson)

> 📌 *Nguồn trích xuất chuẩn:* [`ShareDataSendReceiveTests.cs`](../../../../tests/BE/ITS/ShareData/Services/ShareDataSendReceiveTests.cs) và [`sharedata-du-lieu-gia-lap.sql`](../sql/sharedata-du-lieu-gia-lap.sql) Khối 4.  
> ℹ️ **Lý do dùng gói kiểm thử riêng `E2E_INBOUND_TEST`:** Gói `101_commonData` là gói tin chuẩn của chiều GỬI (Outbound). Sử dụng gói kiểm thử riêng `E2E_INBOUND_TEST` (ID `1500000000900`) giúp cô lập hoàn toàn môi trường kiểm thử E2E hai chiều, tránh xung đột cấu hình ánh xạ giữa luồng gửi D và luồng nhận E, đồng thời dễ dàng dọn dẹp sạch sẽ sau kiểm thử mà không đụng chạm dữ liệu sản xuất.

#### 1. Tạo gói kiểm thử riêng `E2E_INBOUND_TEST` trên CSDL `[DEV_ITS10]`
```sql
USE [DEV_ITS10];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ShareDataPacket WHERE Code = 'E2E_INBOUND_TEST' AND IsDelete IS NULL)
BEGIN
    INSERT INTO dbo.ShareDataPacket (
        [ID], [TenantId], [Code], [Name], [Status], [OrderNo], [SyncMode], [Remark], [CreateTime], [IsDelete]
    ) VALUES (
        '1500000000900', 1, 'E2E_INBOUND_TEST', N'Gói tin kiểm thử E2E chiều nhận', 1, 900, 1, N'Gói tin dùng cho kịch bản nghiệm thu chiều nhận E0', GETDATE(), NULL
    );
END
GO
```

#### 2. Tạo bảng đích trên CSDL `[DEV_ITS10_Inbound]`
```sql
USE [DEV_ITS10_Inbound];
GO

IF OBJECT_ID(N'[dbo].[InboundTrafficFlow]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InboundTrafficFlow] (
        [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
        [code] NVARCHAR(64) NULL,
        [recorded_time] DATETIME2 NULL,
        [val] INT NULL
    );
END
GO
```

#### 3. Cấu hình câu ghi MERGE vào bảng `[dbo].[ShareDataPacketSql]` trên CSDL `[DEV_ITS10]`
```sql
USE [DEV_ITS10];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ShareDataPacketSql WHERE PacketCode = 'E2E_INBOUND_TEST' AND TableName = 'dbo.InboundTrafficFlow' AND IsDelete IS NULL)
BEGIN
    INSERT INTO dbo.ShareDataPacketSql (
        [ID], [TenantId], [PacketCode], [OrderNo], [TableName], [WriteSql], [Status], [Remark], [CreateTime], [IsDelete]
    ) VALUES (
        '1500000000903', 1, 'E2E_INBOUND_TEST', 1, 'dbo.InboundTrafficFlow',
        N'MERGE dbo.InboundTrafficFlow AS target
USING (
    SELECT id, code, recorded_time, val
    FROM OPENJSON(@records)
    WITH (
        id NVARCHAR(64) ''$.id'',
        code NVARCHAR(64) ''$.code'',
        recorded_time DATETIME2 ''$.time'',
        val INT ''$.val''
    )
) AS src
ON target.id = src.id
WHEN MATCHED THEN
    UPDATE SET target.code = src.code, target.recorded_time = src.recorded_time, target.val = src.val
WHEN NOT MATCHED THEN
    INSERT (id, code, recorded_time, val) VALUES (src.id, src.code, src.recorded_time, src.val)
OUTPUT $action;',
        1, N'Cấu hình câu ghi MERGE chiều nhận mẫu cho gói kiểm thử E2E_INBOUND_TEST (Kịch bản E0)', GETDATE(), NULL
    );
END
GO
```

#### 4. Cấu hình `TargetShapeJson` cho Ánh xạ chiều Nhận (Inbound Mapping)
```json
{
  "data": [
    {
      "id": { "$field": "id" },
      "code": { "$field": "code" },
      "time": { "$field": "time", "$extend": { "dateFormat": "yyyy-MM-dd HH:mm:ss" } },
      "val": { "$field": "val" }
    }
  ]
}
```

---

### Phụ lục E: Danh mục thuật ngữ ShareData (SysTerminology)

> 📌 *Nguồn trích xuất chuẩn:* [`sharedata-dich-thuat.sql`](../sql/sharedata-dich-thuat.sql) và lớp hằng số [`BaseMsg.cs`](../../../../TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Exceptions/BaseMsg.cs).  
> ℹ️ Toàn bộ phân hệ ShareData bao gồm chính xác **93 mã khóa thuật ngữ**, tương ứng với **186 bản ghi song ngữ** (`vi-VN` và `en-US`) trong bảng `SysTerminology` (đã bổ sung khóa `sharedata.sqlTableNotAllowed` chặn truy vấn bảng ngoài danh mục `ShareDataSourceTable`). Khi nhập bằng tay trên giao diện màn hình *Hệ thống* → *Thuật ngữ*, người dùng tạo từng dòng với Tiền tố (`Name`), Mã (`Code`), và Giá trị (`Value`) cho từng ngôn ngữ.

| STT | Tiền tố (`Name`) | Mã khóa (`Code`) | Bản dịch tiếng Việt (`vi-VN`) | Bản dịch tiếng Anh (`en-US`) |
|:---:|---|---|---|---|
| 1 | `lz.validation` | `sharedata.lookbackDaysRange` | Số ngày quét lùi tối đa phải nằm trong khoảng từ {0} đến {1} ngày. | Maximum lookback days must be between {0} and {1} days. |
| 2 | `lz.entity` | `sharedata.intervalSeconds` | Chu kỳ gửi (giây) | Interval (seconds) |
| 3 | `lz.entity` | `sharedata.priority` | Độ ưu tiên | Priority |
| 4 | `lz.entity` | `sharedata.debounceSec` | Thời gian gom sự kiện (giây) | Debounce time (seconds) |
| 5 | `lz.validation` | `sharedata.valueOutOfRange` | {0} phải nằm trong khoảng {1}..{2}. | {0} must be in range {1}..{2}. |
| 6 | `lz.exception` | `sharedata.superAdminRequiredForSqlEdit` | Chỉ tài khoản SuperAdmin mới có quyền chỉnh sửa câu truy vấn nguồn dữ liệu. | Only SuperAdmin accounts are authorized to modify data source queries. |
| 7 | `lz.exception` | `sharedata.superAdminRequiredForSqlView` | Chỉ tài khoản SuperAdmin mới có quyền xem cấu hình câu truy vấn nguồn dữ liệu. | Only SuperAdmin accounts are authorized to view data source query configurations. |
| 8 | `lz.exception` | `sharedata.superAdminRequiredForSqlValidate` | Chỉ tài khoản SuperAdmin mới có quyền kiểm tra câu truy vấn nguồn dữ liệu. | Only SuperAdmin accounts are authorized to validate data source queries. |
| 9 | `lz.exception` | `sharedata.superAdminRequiredForSqlPreview` | Chỉ tài khoản SuperAdmin mới có quyền chạy thử câu truy vấn nguồn dữ liệu. | Only SuperAdmin accounts are authorized to preview data source query results. |
| 10 | `lz.exception` | `sharedata.superAdminRequiredForColumnSync` | Chỉ tài khoản SuperAdmin mới có quyền đồng bộ trường dữ liệu của gói tin. | Only SuperAdmin accounts are authorized to sync packet field definitions. |
| 11 | `lz.exception` | `sharedata.packetCodeNotFound` | Không tìm thấy gói tin có mã [{0}]. | Packet with code [{0}] was not found. |
| 12 | `lz.exception` | `sharedata.partnerCodeNotFound` | Không tìm thấy đối tác có mã [{0}]. | Partner with code [{0}] was not found. |
| 13 | `lz.exception` | `sharedata.partnerMappingNotFound` | Đối tác [{0}] chưa có hồ sơ ánh xạ chiều gửi đang bật cho gói tin [{1}]. | Partner [{0}] has no active outbound mapping profile for packet [{1}]. |
| 14 | `lz.validation` | `sharedata.packetCodeRequired` | Mã gói tin không được để trống. | Packet code is required. |
| 15 | `lz.validation` | `sharedata.extractSqlRequired` | Câu lệnh truy vấn SQL không được để trống. | SQL extract query is required. |
| 16 | `lz.exception` | `sharedata.superAdminRequiredForMetadata` | Chỉ tài khoản SuperAdmin mới có quyền truy cập siêu dữ liệu CSDL. | Only SuperAdmin accounts are authorized to access database metadata. |
| 17 | `lz.validation` | `sharedata.tableNameRequired` | Tên bảng nguồn không được để trống. | Source table name is required. |
| 18 | `lz.validation` | `sharedata.tableNameInvalid` | Tên bảng '{0}' không hợp lệ hoặc chứa ký tự bị cấm. | Table name '{0}' is invalid or contains forbidden characters. |
| 19 | `lz.validation` | `sharedata.tableNotAllowed` | Bảng '{0}' không thuộc danh mục được phép chia sẻ dữ liệu. | Table '{0}' is not in the allowed sharing catalog. |
| 20 | `lz.validation` | `sharedata.outputPathDeprecated` | Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern]. | Attribute [OutputPath] is deprecated. Please use [DirectoryPath] and [FileNamePattern]. |
| 21 | `lz.validation` | `sharedata.sqlScriptEmpty` | Câu lệnh truy vấn không được để trống. | Query script is required. |
| 22 | `lz.validation` | `sharedata.sqlScriptSyntaxError` | Cú pháp SQL không hợp lệ (Dòng {0}, Cột {1}): {2} | Invalid SQL syntax (Line {0}, Column {1}): {2} |
| 23 | `lz.validation` | `sharedata.sqlScriptInvalid` | Cú pháp SQL không hợp lệ hoặc không chứa script thực thi. | SQL script is invalid or contains no executable statements. |
| 24 | `lz.validation` | `sharedata.sqlMultipleBatchesNotAllowed` | Chỉ chấp nhận duy nhất một lô lệnh (batch). Không được sử dụng lệnh phân đoạn 'GO'. | Only a single batch is allowed. 'GO' delimiter is not permitted. |
| 25 | `lz.validation` | `sharedata.sqlMultipleStatementsNotAllowed` | Chỉ chấp nhận duy nhất một câu lệnh SELECT (không cho phép đa câu lệnh). | Only a single SELECT statement is allowed (multiple statements are prohibited). |
| 26 | `lz.validation` | `sharedata.sqlOnlySelectAllowed` | Chỉ chấp nhận câu lệnh đọc dữ liệu bắt đầu bằng SELECT. | Only read statements starting with SELECT are allowed. |
| 27 | `lz.validation` | `sharedata.sqlSelectIntoNotAllowed` | Không cho phép sử dụng cú pháp SELECT ... INTO tạo bảng mới. | SELECT ... INTO syntax is not permitted. |
| 28 | `lz.validation` | `sharedata.sqlFunctionCallBlocked` | Không cho phép gọi hàm hoặc thủ tục: [{0}]. | Calling function or procedure [{0}] is not allowed. |
| 29 | `lz.validation` | `sharedata.sqlExternalDataSourcesBlocked` | Không cho phép sử dụng các nguồn dữ liệu ngoài (OPENROWSET/OPENQUERY/...). | External data sources (OPENROWSET/OPENQUERY/...) are not allowed. |
| 30 | `lz.validation` | `sharedata.sqlExecuteStatementBlocked` | Không cho phép thực thi câu lệnh EXEC/EXECUTE. | Executing EXEC/EXECUTE statements is not allowed. |
| 31 | `lz.validation` | `sharedata.sqlCrossDatabaseQueryBlocked` | Không được phép truy vấn bảng vượt ra ngoài CSDL hiện tại (tên bảng 3-4 phần: [{0}]). | Cross-database queries are not allowed (3-4 part table name: [{0}]). |
| 32 | `lz.validation` | `sharedata.sqlSystemSchemaBlocked` | Không được phép truy vấn schema hệ thống: [{0}]. | Querying system schema [{0}] is not allowed. |
| 33 | `lz.validation` | `sharedata.sqlSensitiveTableBlocked` | Không được phép truy vấn bảng hệ thống nhạy cảm: [{0}]. | Querying sensitive system table [{0}] is not allowed. |
| 34 | `lz.validation` | `sharedata.sqlUnsupportedParameter` | Tham số [@{0}] không được hỗ trợ. Chỉ hỗ trợ: @lastTime, @lastKey, @pageSize, @fromDate, @toDate. | Parameter [@{0}] is not supported. Supported parameters: @lastTime, @lastKey, @pageSize, @fromDate, @toDate. |
| 35 | `lz.validation` | `sharedata.sqlDatabaseExecutionError` | Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: {0} | SQL syntax error on database: {0} |
| 36 | `lz.validation` | `sharedata.sqlIncrementalMissingTop` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề phân trang TOP (@pageSize). | Incremental queries must include a TOP (@pageSize) pagination clause. |
| 37 | `lz.validation` | `sharedata.sqlIncrementalMissingOrderBy` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề ORDER BY ở cấp ngoài cùng. | Incremental queries must include an outer ORDER BY clause. |
| 38 | `lz.validation` | `sharedata.sqlIncrementalOrderByDescNotAllowed` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc sắp xếp tăng dần (ASC); không cho phép sắp xếp giảm dần (DESC) ở mệnh đề ORDER BY ngoài cùng. | Incremental queries must sort ascending (ASC); descending (DESC) sorting is not allowed in the outer ORDER BY clause. |
| 39 | `lz.validation` | `sharedata.sqlIncrementalMissingLastTime` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có cột con trỏ thời gian [__lastTime]. | Incremental queries must include the time cursor column [__lastTime]. |
| 40 | `lz.validation` | `sharedata.sqlIncrementalMissingRowid` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có cột con trỏ khóa [__rowid]. | Incremental queries must include the key cursor column [__rowid]. |
| 41 | `lz.validation` | `sharedata.connectConfigNotJsonObject` | Cấu hình kết nối (ConnectionConfig) phải là đối tượng JSON phẳng dạng key-value. | ConnectionConfig must be a flat key-value JSON object. |
| 42 | `lz.validation` | `sharedata.connectConfigSyntaxError` | Cấu hình kết nối (ConnectionConfig) sai cú pháp JSON. | ConnectionConfig contains invalid JSON syntax. |
| 43 | `lz.validation` | `sharedata.connectConfigEmptyKey` | Cấu hình kết nối (ConnectionConfig) không được chứa khóa (Key) rỗng. | ConnectionConfig cannot contain empty keys. |
| 44 | `lz.validation` | `sharedata.connectConfigReservedKey` | Khóa cấu hình bị cấm sử dụng vì là khóa dành riêng của hệ thống. | Configuration key is reserved by the system and cannot be used. |
| 45 | `lz.validation` | `sharedata.connectConfigObsoleteKey` | Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern]. | Attribute [OutputPath] is obsolete. Please use [DirectoryPath] and [FileNamePattern]. |
| 46 | `lz.validation` | `sharedata.connectConfigDuplicateKey` | Cấu hình kết nối (ConnectionConfig) chứa khóa bị trùng lặp (không phân biệt hoa thường). | ConnectionConfig contains duplicate keys (case-insensitive). |
| 47 | `lz.validation` | `sharedata.connectConfigNestedValue` | Cấu hình kết nối (ConnectionConfig) chỉ cho phép giá trị phẳng (chuỗi/số/boolean), không cho phép lồng mảng hoặc object con. | ConnectionConfig only allows flat primitive values (string/number/boolean); nested arrays or objects are not allowed. |
| 48 | `lz.validation` | `sharedata.connectConfigInvalidTimeout` | Giá trị Timeout phải là số nguyên dương (giây). | Timeout value must be a positive integer (seconds). |
| 49 | `lz.validation` | `sharedata.connectConfigInvalidBoolean` | Giá trị cấu hình boolean chỉ chấp nhận 'true' hoặc 'false'. | Boolean configuration value only accepts 'true' or 'false'. |
| 50 | `lz.validation` | `sharedata.connectConfigUnsupportedAuthType` | Giá trị AuthType chỉ hỗ trợ: None, Bearer, ApiKey, Basic. | AuthType only supports: None, Bearer, ApiKey, Basic. |
| 51 | `lz.validation` | `sharedata.connectConfigUnsupportedEncryptor` | Giá trị mã hóa (Encryptor / InboundEncryptor) chỉ hỗ trợ: None, AesGcm. | Encryptor / InboundEncryptor only supports: None, AesGcm. |
| 52 | `lz.validation` | `sharedata.connectConfigFileNamePatternNoToken` | Thuộc tính [FileNamePattern] bắt buộc chứa ít nhất một biến động ({serial}, {timestamp}, {packet} hoặc {partner}) để tránh ghi đè tệp. | Attribute [FileNamePattern] must contain at least one dynamic token ({serial}, {timestamp}, {packet}, or {partner}) to prevent file overwriting. |
| 53 | `lz.validation` | `sharedata.sqlTableNotAllowed` | Bảng {0} chưa có trong danh mục bảng nguồn được phép chia sẻ.<br>Thêm bảng vào ShareDataSourceTable (IsQueryable = 1) rồi thử lại. | Table {0} is not in the allowed sharing source tables.<br>Add the table to ShareDataSourceTable (IsQueryable = 1) and try again. |

---

### Phụ lục F: Danh mục 48 tham số vận hành ShareData (SysOpConfig nhóm sharedata_config)

> 📌 *Nguồn trích xuất chuẩn:* [`sharedata-seed-cau-hinh.sql`](../sql/sharedata-seed-cau-hinh.sql) (Mục 4), dịch vụ cấu hình Worker `DataWorkerConfigService.cs` (`ShareDataWorker/Infrastructure/Services/DataWorkerConfig`), dịch vụ WebAPI `ShareDataConfigService.cs` (`Module.ShareData/Infrastructure/Services/ShareDataConfig`, đọc `SysOpConfig` qua `IMessageBus`), hằng số và bảng đặc tả cấu hình `ShareDataConst.cs` (`Module.ShareData.Core/Constants`), và hằng số dự phòng giao diện `SHAREDATA_FE_CONFIG_FALLBACK` (`TA-ITS015-WEBVUE-V1.0/src/src/types/constants/sharedataFeConfig.ts`).  
> ℹ️ Toàn bộ 48 tham số được phân nhóm theo tiền tố và thành phần sử dụng: 19 tham số Worker (nhóm A), 23 tham số FE/WebAPI dùng chung (nhóm B), và 6 tham số WebAPI/SQL Center (nhóm C). Người dùng có thể tra cứu và điều chỉnh trên màn hình *Cấu hình hoạt động* (lọc theo nhóm `sharedata_config`).

| STT | Mã tham số (`Code`) | Tên hiển thị (`Name`) | Mặc định (`Value`) | Thứ tự (`OrderNo`) | Khoảng hợp lệ & Đơn vị | Thành phần sử dụng | Ý nghĩa vận hành chi tiết |
|:---:|---|---|:---:|:---:|---|:---:|---|
| **A. Tham số Worker (19 mã)** | | | | | | | |
| 1 | `outbound_check_interval_sec` | Chu kỳ quét gửi gói tin (giây) | `5` | 10 | 1–300 giây | Worker | Chu kỳ vòng lặp quét gửi gói tin của Outbound Worker. |
| 2 | `inbound_check_interval_sec` | Chu kỳ quét nhận gói tin (giây) | `5` | 11 | 1–300 giây | Worker | Chu kỳ vòng lặp quét nhận gói tin của Inbound Worker. |
| 3 | `tracking_poll_interval_sec` | Chu kỳ quét Change Tracking (giây) | `1` | 12 | 1–60 giây | Worker | Chu kỳ vòng lặp quét SQL Server Change Tracking của Tracking Worker. |
| 4 | `outbound_max_pages_per_run` | Số trang trích xuất tối đa mỗi lượt gửi | `20` | 20 | 1–1000 trang | Worker | Số trang trích xuất tối đa mỗi lượt chạy của một đăng ký gửi. |
| 5 | `outbound_lock_budget_percent` | Ngân sách thời gian giữ khóa gửi (%) | `50` | 21 | 10–90 % | Worker | Tỷ lệ ngân sách thời gian giữ khóa phân tán mỗi lượt gửi. |
| 6 | `outbound_lock_min_sec` | Thời gian giữ khóa gửi tối thiểu (giây) | `300` | 22 | 30–3600 giây | Worker | Thời gian giữ khóa phân tán tối thiểu của đăng ký gửi. |
| 7 | `outbound_lock_interval_multiplier` | Hệ số nhân chu kỳ gửi tính thời gian giữ khóa | `3` | 23 | 1–10 lần | Worker | Hệ số nhân với chu kỳ gửi để tính thời gian giữ khóa phân tán. |
| 8 | `outbound_page_size` | Số dòng mỗi trang trích xuất gửi | `100` | 24 | 1–10000 dòng | Worker | Số dòng dữ liệu mặc định mỗi trang trích xuất gửi đi. |
| 9 | `outbound_default_interval_sec` | Chu kỳ gửi mặc định của Worker (giây) | `30` | 25 | 1–86400 giây | Worker | Chu kỳ gửi mặc định khi đăng ký không khai báo IntervalSeconds. |
| 10 | `inbound_max_packets_per_run` | Số gói tin nhận xử lý tối đa mỗi lượt | `50` | 30 | 1–1000 gói | Worker | Số gói tin Pending xử lý tối đa mỗi lượt của Inbound Worker. |
| 11 | `inbound_stuck_timeout_min` | Ngưỡng thu hồi gói nhận bị kẹt (phút) | `10` | 31 | 1–1440 phút | Worker | Ngưỡng thời gian thu hồi gói tin Inbound bị kẹt ở trạng thái Processing. |
| 12 | `inbound_warn_interval_min` | Chu kỳ tối thiểu ghi cảnh báo lặp Inbound (phút) | `5` | 32 | 1–1440 phút | Worker | Chu kỳ tối thiểu giữa hai lần ghi cảnh báo lặp lại ở Inbound Worker. |
| 13 | `inbound_write_timeout_sec` | Thời gian chờ ghi SQL chiều nhận (giây) | `120` | 33 | 10–3600 giây | Worker | Thời gian chờ tối đa khi thực thi câu lệnh SQL ghi dữ liệu nhận về. |
| 14 | `tracking_table_retry_min` | Chu kỳ thử lại bảng nguồn thiếu Change Tracking (phút) | `5` | 40 | 1–1440 phút | Worker | Chu kỳ thử khôi phục bảng nguồn bị cô lập do thiếu Change Tracking. |
| 15 | `tracking_log_retention_days` | Số ngày lưu giữ nhật ký hạ tầng Change Tracking | `7` | 41 | 1–365 ngày | Worker | Số ngày lưu giữ nhật ký hạ tầng Change Tracking mã ESH-16xx. |
| 16 | `tracking_log_purge_batch` | Số dòng xóa mỗi lô nhật ký Change Tracking | `1000` | 42 | 100–50000 dòng | Worker | Số dòng xóa tối đa mỗi lô khi dọn nhật ký hạ tầng Change Tracking. |
| 17 | `tracking_change_retention_days` | Số ngày giữ lịch sử CHANGE_RETENTION khi bật CT | `2` | 43 | 1–30 ngày | Worker | Số ngày giữ lịch sử thay đổi khi tự bật Change Tracking cho CSDL. |
| 18 | `cleanup_interval_min` | Chu kỳ chạy dọn dẹp định kỳ (phút) | `1440` | 46 | 1–43200 phút | Worker | Chu kỳ dọn dẹp nhật ký Change Tracking ESH-16xx cũ hơn số ngày retention. |
| 19 | `reload_interval_sec` | Chu kỳ nạp lại cấu hình và danh sách bảng nguồn (giây) | `30` | 47 | 5–3600 giây | Worker | Chu kỳ nạp lại cấu hình vận hành và danh sách bảng nguồn từ CSDL (thay thế cấu hình appsettings cũ). |
| **B. Tham số Giới hạn Giao diện FE & Dùng chung WebAPI (23 mã)** | | | | | | | |
| 20 | `subscription_interval_sec_min` | Chu kỳ gửi - tối thiểu | `5` | 101 | 1–604800 giây | FE / BE | Giới hạn dưới chu kỳ gửi khi đăng ký gói tin (chặn cùng ngưỡng ở FE và BE). |
| 21 | `subscription_interval_sec_max` | Chu kỳ gửi - tối đa | `86400` | 102 | 1–604800 giây | FE / BE | Giới hạn trên chu kỳ gửi khi đăng ký gói tin (tối đa 7 ngày). |
| 22 | `subscription_interval_sec_default` | Chu kỳ gửi - mặc định | `30` | 103 | Giây | FE | Giá trị chu kỳ gửi mặc định hiển thị trên form Đăng ký gói tin. |
| 23 | `subscription_debounce_sec_min` | Thời gian gom sự kiện - tối thiểu | `0` | 111 | 0–86400 giây | FE / BE | Giới hạn dưới thời gian gom sự kiện NATS khi đăng ký gói tin. |
| 24 | `subscription_debounce_sec_max` | Thời gian gom sự kiện - tối đa | `3600` | 112 | 0–86400 giây | FE / BE | Giới hạn trên thời gian gom sự kiện NATS khi đăng ký gói tin. |
| 25 | `subscription_debounce_sec_default` | Thời gian gom sự kiện - mặc định | `30` | 113 | Giây | FE | Giá trị thời gian gom sự kiện mặc định hiển thị trên form Đăng ký gói tin. |
| 26 | `subscription_priority_min` | Độ ưu tiên - tối thiểu | `0` | 121 | 0–100 | FE / BE | Giới hạn dưới mức độ ưu tiên hàng đợi của đăng ký gửi. |
| 27 | `subscription_priority_max` | Độ ưu tiên - tối đa | `10` | 122 | 0–100 | FE / BE | Giới hạn trên mức độ ưu tiên hàng đợi của đăng ký gửi. |
| 28 | `subscription_priority_default` | Độ ưu tiên - mặc định | `5` | 123 | 0–10 | FE | Mức độ ưu tiên mặc định khi tạo mới đăng ký gửi gói tin. |
| 29 | `partner_heartbeat_sec_min` | Heartbeat - tối thiểu | `5` | 201 | 1–300 giây | FE | Giới hạn dưới chu kỳ gửi heartbeat kiểm tra kết nối đối tác. |
| 30 | `partner_heartbeat_sec_max` | Heartbeat - tối đa | `300` | 202 | 1–300 giây | FE | Giới hạn trên chu kỳ gửi heartbeat kiểm tra kết nối đối tác. |
| 31 | `partner_heartbeat_sec_default` | Heartbeat - mặc định | `30` | 203 | Giây | FE | Chu kỳ gửi heartbeat mặc định khi tạo mới đối tác. |
| 32 | `partner_response_timeout_sec_min` | Thời gian chờ phản hồi - tối thiểu | `3` | 211 | 1–60 giây | FE | Giới hạn dưới thời gian chờ đối tác phản hồi bắt tay kết nối. |
| 33 | `partner_response_timeout_sec_max` | Thời gian chờ phản hồi - tối đa | `60` | 212 | 1–60 giây | FE | Giới hạn trên thời gian chờ đối tác phản hồi bắt tay kết nối. |
| 34 | `partner_response_timeout_sec_default` | Thời gian chờ phản hồi - mặc định | `10` | 213 | Giây | FE | Thời gian chờ phản hồi mặc định khi tạo mới đối tác. |
| 35 | `partner_datagram_size_min` | Kích thước datagram - tối thiểu | `512` | 221 | 512–65507 Byte | FE | Giới hạn dưới kích thước gói tin datagram của kênh UDP. |
| 36 | `partner_datagram_size_max` | Kích thước datagram - tối đa | `65507` | 222 | 512–65507 Byte | FE | Giới hạn trên kích thước datagram (giới hạn phần cứng UDP 65507 Byte). |
| 37 | `partner_datagram_size_default` | Kích thước datagram - mặc định | `1024` | 223 | Byte | FE | Kích thước gói tin datagram mặc định khi tạo mới đối tác UDP. |
| 38 | `mapping_sample_top_min` | Số bản ghi mẫu - tối thiểu | `1` | 301 | 1–500 bản ghi | FE / BE | Giới hạn dưới số lượng bản ghi mẫu trích xuất khi cấu hình ánh xạ. |
| 39 | `mapping_sample_top_max` | Số bản ghi mẫu - tối đa | `50` | 302 | 1–500 bản ghi | FE / BE | Giới hạn trên số lượng bản ghi mẫu trích xuất khi cấu hình ánh xạ. |
| 40 | `mapping_sample_top_default` | Số bản ghi mẫu - mặc định | `5` | 303 | 1–500 bản ghi | FE / BE | Số lượng bản ghi mẫu mặc định khi bấm xem mẫu tại màn hình Ánh xạ. |
| 41 | `history_range_default_days` | Khoảng tra cứu - mặc định | `7` | 401 | 1–31 ngày | FE | Khoảng thời gian mặc định khi mở màn hình Lịch sử chia sẻ và Nhật ký lỗi. |
| 42 | `history_range_max_days` | Khoảng tra cứu - tối đa | `31` | 402 | 1–31 ngày | FE | Khoảng thời gian tra cứu tối đa cho phép chọn trên bộ lọc ngày. |
| **C. Tham số WebAPI Backend & SQL Center (6 mã)** | | | | | | | |
| 43 | `packet_lookback_days_min` | Số ngày quét lùi tối đa - tối thiểu | `1` | 501 | 1–3650 ngày | BE / FE | Giới hạn dưới của MaxLookBackDays khi cấu hình SQL trích xuất gói tin. |
| 44 | `packet_lookback_days_max` | Số ngày quét lùi tối đa - tối đa | `365` | 502 | 1–3650 ngày | BE / FE | Giới hạn trên của MaxLookBackDays (mặc định 365, tối đa 3650). |
| 45 | `sql_sample_top_min` | Số dòng xem mẫu SQL Center - tối thiểu | `1` | 511 | 1–500 bản ghi | BE / FE | Giới hạn dưới khi chạy thử SQL trích xuất tại SQL Center. |
| 46 | `sql_sample_top_max` | Số dòng xem mẫu SQL Center - tối đa | `50` | 512 | 1–500 bản ghi | BE / FE | Giới hạn trên khi chạy thử SQL trích xuất tại SQL Center. |
| 47 | `sql_sample_top_default` | Số dòng xem mẫu SQL Center - mặc định | `10` | 513 | 1–500 bản ghi | BE / FE | Số dòng xem mẫu mặc định khi chạy thử câu SQL tại SQL Center. |
| 48 | `log_list_default_top` | Số dòng nhật ký trả về mặc định | `1000` | 521 | 1–100000 bản ghi | BE | Số dòng mặc định trả về khi truy vấn danh sách ActivityLog / AlertLog. |

