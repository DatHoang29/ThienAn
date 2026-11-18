# Hướng dẫn xóa sạch, seed lại và kiểm tải 10 đối tác trên staging

> Áp dụng cho CSDL staging `DEV_ITS015` (CSDL chính) và `DEV_ITS015_Inbound` (CSDL nhận). ⛔ Không chạy trên production.
> Bảng thứ tự và mô tả từng tệp nằm ở [README.md](README.md) (mục 3D, 3E, 3F); tệp này là danh sách bước để đánh dấu khi làm.
> Cập nhật: 18/11/2026.

**Kết quả sau khi làm xong:** dữ liệu và cấu hình ShareData được nạp lại từ đầu; 10 đối tác (`PC08`, `TTDHT`, `KT001`–`KT008`) gửi và nhận 8 gói (101–104, 106–109) qua chính WebAPI staging; 6 bảng nguồn đã gỡ chỉ mục mốc cũ `CursorKey`, không dựng `CursorTime` (câu mẫu một tầng không dùng).

**Không bị đụng tới:** dữ liệu nghiệp vụ của phân hệ khác (`TmsTrafficData`, `TmsZoneStatus`, `TollTransactionOut`...) trên CSDL chính, và menu (`SysMenu`).

**Sẽ mất và không khôi phục được:** mọi đối tác, đăng ký, câu SQL riêng, ánh xạ, mốc đang chạy, nhật ký, cảnh báo của ShareData; tham số Cấu hình (`SysOpConfig` nhóm `sharedata_config`) và câu dịch ShareData về bản mặc định; gói nhận và 14 bảng đích ở CSDL nhận. Cần giữ gì thì backup trước.

---

## Bước 0. Chuẩn bị

- [ ] Có bản build mới, cả ba phần cùng mã ShareData (nhánh `fix/20261117-sharedata-chuan-hoa-moc-noi-duoi`):
  - Worker: zip `ShareDataWorker-clone-<ngày>.zip` mới nhất trong `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/` (6 thư mục dịch vụ, cách cài ở `DOC-TRUOC-KHI-CHAY.md` trong zip).
  - WebAPI và FE: publish từ cùng nhánh, có bản sửa ô mốc chiều 17/11 (đầu nhánh WebAPI `74812dc9`, FE `16cd792f`, đã merge vào `dev` sáng 18/11 qua PR #20, #28; publish từ `dev` thì kèm thêm 1 commit WP, 1 commit TMS bản đồ không đụng ShareData; dựng từ bản lấy sạch từ git, không publish từ thư mục làm việc còn DLL core cũ và tệp cấu hình local). Ô Đặt mốc và Mốc bắt đầu nhận ngày giờ viết liền 8–17 chữ số (ví dụ `20260101`), hoặc đủ 17 chữ số kèm `_<đuôi>`, đuôi được để trống. Phần sửa này không đụng Worker.
  - Zip Worker sửa lại lúc 14:44 ngày 17/11 vì máy chủ staging đổi địa chỉ (chuỗi CSDL, NATS trong 6 tệp `appsettings.json` và `DOC-TRUOC-KHI-CHAY.md`), rồi dựng lại lần cuối lúc 18:09 cùng ngày (commit `74812dc9`, giảm truy vấn lặp ở luồng gửi): lần này chỉ đổi `ShareDataWorker.dll`, `ShareDataWorker.Core.dll` ở 6 thư mục dịch vụ, giữ nguyên `appsettings.json`. Đã chép zip lên máy chủ trước 18:09 ngày 17/11 thì chép lại.
  - **Bắt buộc lên cùng lúc.** Worker và WebAPI cùng tính chữ ký công thức mốc; lệch phiên bản thì Worker coi mốc là không hợp lệ (cảnh báo ESH-1308) và gửi lại từ đầu. Câu mẫu mới dùng mốc dạng `yyyyMMddHHmmssfff_<đuôi>`; WebAPI, FE bản cũ không nhận dạng này ở ô Đặt mốc.
  - WebAPI publish từ HEAD sẽ mang core 16/11; hỏi anh Sơn tệp khóa `TAKeyData.key` trên máy chủ còn dùng được với core mới không. Worker không dùng tệp khóa.
- [ ] (Nên làm) Backup `DEV_ITS015` và `DEV_ITS015_Inbound`.
- [ ] Mỗi tệp `.sql` đều có `@ChiXem = 1` (hoặc tham số tương đương) để xem trước, riêng I2 ở bước 4 không có (chạy là gỡ luôn). Chạy xong đọc phần Messages: dòng `✅` là xong, dòng `❌` hoặc `⚠️` thì dừng lại xem.

## Bước 1. Dừng dịch vụ

- [ ] Dừng 6 dịch vụ Worker (PowerShell quyền Administrator):
  ```powershell
  net stop ShareDataWorker
  net stop ShareDataWorker_Inbound_1
  net stop ShareDataWorker_Outbound_1
  net stop ShareDataWorker_Outbound_2
  net stop ShareDataWorker_Outbound_3
  net stop ShareDataWorker_Outbound_4
  ```
- [ ] Dừng WebAPI (site, app pool `TAC_WebAPI` trên IIS).
- [ ] Kiểm không còn tiến trình `ShareDataWorker.exe` nào chạy (Task Manager).

## Bước 2. Triển khai bản mới (chưa bật)

- [ ] Worker: giải nén zip mới, trỏ 6 dịch vụ sang thư mục mới (`sc.exe config ... binPath=`), làm theo `DOC-TRUOC-KHI-CHAY.md` trong zip. **Chưa `net start`.**
- [ ] WebAPI, FE: chép bản publish mới. **Chưa bật** WebAPI.

## Bước 3. Xóa dữ liệu (X1 → X17)

Chạy đúng thứ tự, từng tệp một.

| Thứ tự | Tệp | Chạy trên | Ghi chú |
| :---: | :--- | :--- | :--- |
| X1 | [sharedata-xoa-du-lieu-ShareDataLastSend.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataLastSend.sql) | `DEV_ITS015` | Mốc đang chạy |
| X2 | [sharedata-xoa-du-lieu-ShareDataActivityLog.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataActivityLog.sql) | `DEV_ITS015` | Khoảng 2,4 triệu dòng, `TRUNCATE` nên xong ngay |
| X3 | [sharedata-xoa-du-lieu-ShareDataAlertLog.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataAlertLog.sql) | `DEV_ITS015` | |
| X4 | [sharedata-xoa-du-lieu-ShareDataSession.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataSession.sql) | `DEV_ITS015` | |
| X5 | [sharedata-xoa-du-lieu-ShareDataSubscription.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataSubscription.sql) | `DEV_ITS015` | Phải chạy kèm X16 |
| X6 | [sharedata-xoa-du-lieu-ShareDataMapping.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataMapping.sql) | `DEV_ITS015` | |
| X7 | [sharedata-xoa-du-lieu-ShareDataSql.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataSql.sql) | `DEV_ITS015` | Cả câu mẫu cũ dạng `121` |
| X8 | [sharedata-xoa-du-lieu-ShareDataCodeSet.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataCodeSet.sql) | `DEV_ITS015` | |
| X9 | [sharedata-xoa-du-lieu-ShareDataPartner.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataPartner.sql) | `DEV_ITS015` | |
| X10 | [sharedata-xoa-du-lieu-ShareDataSourceTable.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataSourceTable.sql) | `DEV_ITS015` | |
| X11 | [sharedata-xoa-du-lieu-ShareDataPacket.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataPacket.sql) | `DEV_ITS015` | ⚠️ **Sửa `@XoaCaGoiCatalog = 0`** (mặc định trong tệp là 1). Bằng 0: giữ gói 101–111 với ID, trạng thái cũ (105 vẫn tắt), chỉ xóa gói thử như `1611_test01`. Để bằng 1 thì phải chạy lại tệp 1 lấy từ git, tệp đó bật gói 105 và đổi ID gói |
| X12 | [sharedata-xoa-du-lieu-SysOpConfig.sql](xoa-du-lieu/sharedata-xoa-du-lieu-SysOpConfig.sql) | `DEV_ITS015` | Chỉ nhóm `sharedata_config` |
| X13 | [sharedata-xoa-du-lieu-SysConfigData.sql](xoa-du-lieu/sharedata-xoa-du-lieu-SysConfigData.sql) | `DEV_ITS015` | Chỉ mã `ESH-*` |
| X14 | [sharedata-xoa-du-lieu-SysConfigType.sql](xoa-du-lieu/sharedata-xoa-du-lieu-SysConfigType.sql) | `DEV_ITS015` | Chạy sau X13 |
| X15 | [sharedata-xoa-du-lieu-SysTerminology.sql](xoa-du-lieu/sharedata-xoa-du-lieu-SysTerminology.sql) | `DEV_ITS015` | Chỉ khóa `sharedata.%` |
| X16 | [sharedata-xoa-du-lieu-ShareDataInboundPacket.sql](xoa-du-lieu/sharedata-xoa-du-lieu-ShareDataInboundPacket.sql) | `DEV_ITS015_Inbound` | Gói nhận thô, tránh trùng số thứ tự |
| X17 | [sharedata-xoa-du-lieu-BangDichInboundDB.sql](xoa-du-lieu/sharedata-xoa-du-lieu-BangDichInboundDB.sql) | `DEV_ITS015_Inbound` | 14 bảng đích, giữ cấu trúc và chỉ mục |

## Bước 4. Gỡ chỉ mục mốc cũ trên 6 bảng nguồn

- [ ] Chạy I2 [sharedata-chi-muc-go-BangNguonNoiDuoi.sql](chi-muc/sharedata-chi-muc-go-BangNguonNoiDuoi.sql) trên `DEV_ITS015`, giữ tham số mặc định (`@ChiBang = NULL` là cả 6 bảng).
  - Gỡ chỉ mục `IX_<Bảng>_CursorKey` rồi cột tính toán `CursorKey` (dạng `121 + '_' + ID`, đánh ngày 12/11) trên `TmsTrafficData`, `TmsIncident`, `TollTransactionOut`, `TmsSignalLog`, `TmsZoneStatus`, `TmsWeather`. Seed lại xong không câu nào dùng dạng `121`; để lại thì mỗi lần phân hệ khác ghi vào 6 bảng này vẫn phải cập nhật chỉ mục. Gỡ xong bảng trở về đúng cấu trúc trước 12/11.
  - Tệp không có chế độ xem trước: chạy là gỡ luôn.
  - Chỉ gỡ, không dựng gì nên xong trong vài giây, không cần giờ thấp điểm. Mỗi lệnh chờ khóa tối đa `@ChoKhoaMs` (5 giây), bảng có thể khựng chừng đó. Báo lỗi 1222 thì chạy lại tệp, bảng đã gỡ được bỏ qua.
  - Kết quả cuối tệp: mỗi bảng `CotMoc = đã gỡ`, `ChiMuc = đã gỡ`.
- [ ] **Không chạy I1** (dựng `CursorTime`).
- [ ] Không cần chạy I4, I7 (CSDL nhận vẫn giữ chỉ mục sau X16, X17).

> ⚠️ **Đã chốt 17/11: 7 câu mẫu giữ một tầng, chấp nhận quét bảng, không dựng `CursorTime`.** Câu một tầng lọc và sắp theo cả chuỗi giờ + đuôi nên không dùng được chỉ mục nào: mỗi lượt gửi quét và sắp cả bảng nguồn, theo dõi sát tempdb và đĩa ở bước 8.

## Bước 5. Seed lại dữ liệu chuẩn (tệp 2 → 16)

Chạy trên `DEV_ITS015`, giữ tham số mặc định.

| Thứ tự | Tệp | Nạp |
| :---: | :--- | :--- |
| 2 | [sharedata-seed-goi-tin-ShareDataSql.sql](sharedata-seed-goi-tin-ShareDataSql.sql) | Câu mẫu chung 101–111, câu ghi chiều nhận |
| 4 | [sharedata-seed-cau-hinh-SysConfigType.sql](sharedata-seed-cau-hinh-SysConfigType.sql) | Loại danh mục `sharedata_error_code` |
| 5 | [sharedata-seed-cau-hinh-SysConfigData.sql](sharedata-seed-cau-hinh-SysConfigData.sql) | 26 mã cảnh báo `ESH-*` |
| 6 | [sharedata-seed-cau-hinh-ShareDataSourceTable.sql](sharedata-seed-cau-hinh-ShareDataSourceTable.sql) | Danh mục bảng nguồn |
| 7 | [sharedata-seed-cau-hinh-SysOpConfig.sql](sharedata-seed-cau-hinh-SysOpConfig.sql) | Tham số nhóm `sharedata_config` |
| 10 | [sharedata-dich-thuat-SysTerminology.sql](sharedata-dich-thuat-SysTerminology.sql) | Câu dịch ShareData |
| 13 | [sharedata-seed-menu-SysMenu.sql](sharedata-seed-menu-SysMenu.sql) | Menu (chỉ cập nhật menu có sẵn) |
| 14 | [sharedata-seed-doi-tac-ShareDataPartner.sql](sharedata-seed-doi-tac-ShareDataPartner.sql) | Đối tác `PC08`, `TTDHT` |
| 15 | [sharedata-seed-bo-ma-ShareDataCodeSet.sql](sharedata-seed-bo-ma-ShareDataCodeSet.sql) | 9 bộ mã chuẩn hóa |
| 16 | [sharedata-seed-anh-xa-ShareDataMapping.sql](sharedata-seed-anh-xa-ShareDataMapping.sql) | Hồ sơ ánh xạ gói 101 của `PC08`, `TTDHT` |

Không cần chạy N1, D10, D11, D12, K7 sau khi đã xóa sạch.

## Bước 6. Tạo dữ liệu kiểm tải (K1 → K4)

Chạy trên `DEV_ITS015`, giữ tham số mặc định.

| Thứ tự | Tệp | Tạo |
| :---: | :--- | :--- |
| K1 | [sharedata-kiem-tai-ShareDataPartner.sql](kiem-tai/sharedata-kiem-tai-ShareDataPartner.sql) | `KT001`–`KT008` (`@SoDoiTac = 8`, cộng `PC08`, `TTDHT` là 10 đối tác), bật kết nối cho cả `PC08`, `TTDHT`; mọi đối tác gửi vào API nhận của chính WebAPI staging. `PC08` đã có từ tệp 14 nên K1 không tạo trùng |
| K2 | [sharedata-kiem-tai-ShareDataMapping.sql](kiem-tai/sharedata-kiem-tai-ShareDataMapping.sql) | Hồ sơ ánh xạ gửi và nhận cho mọi gói đang bật |
| K3 | [sharedata-kiem-tai-ShareDataSubscription.sql](kiem-tai/sharedata-kiem-tai-ShareDataSubscription.sql) | Đăng ký gửi và nhận. Chu kỳ gửi của đối tác kiểm tải chia theo `30,40,50,60,120` giây (giãn gấp đôi ngày 17/11 để bớt lượt quét `TmsTrafficData`), `PC08`, `TTDHT` và đăng ký nhận 30 giây; đối tác số chẵn (`KT002`, `KT004`...) bật "Gửi khi có dữ liệu mới"; gói 110 chưa có câu ghi chiều nhận nên tự bỏ |
| K4 | [sharedata-kiem-tai-ShareDataSql.sql](kiem-tai/sharedata-kiem-tai-ShareDataSql.sql) | Câu SQL riêng cho `KT005`–`KT008` (`KT001`–`KT004` dùng câu mẫu chung), mỗi cặp đối tác × gói một biến thể (thêm cột phụ, đổi `TOP 100` thành 20/50/200/500, đổi đuôi `__lastKey` sang cột nghiệp vụ như `ZoneId`/`RefId`/`LaneId+TransactionId`/`Code+ID`/`Lane+ID` trên `101/104/109/107` và `103/106` kiểu `1,3`; chạy lại tự cập nhật câu đổi ở bước 2a và dò `FieldsJson` ở bước 2b) |

- [ ] Kiểm số lượng (chỉ đọc):
  ```sql
  SELECT SessionState, COUNT(*) AS SoDoiTac FROM dbo.ShareDataPartner WHERE IsDelete IS NULL
  GROUP BY SessionState;                                                                   -- SessionState 1: 10
  SELECT Direction, State, COUNT(*) AS SoDangKy
  FROM dbo.ShareDataSubscription WHERE IsDelete IS NULL
  GROUP BY Direction, State;                                                               -- Direction 0, State 1: 80; Direction 1, State 1: 80
  SELECT COUNT(*) AS SoCauRieng FROM dbo.ShareDataSql WHERE PartnerId IS NOT NULL AND IsDelete IS NULL; -- 32 (4 đối tác × 8 gói)
  ```

## Bước 7. Bật dịch vụ

- [ ] Sau K1–K4 mà đã ngắt đối tác (K5 hoặc nút Ngắt kết nối trên màn hình) thì chạy K6 [sharedata-kiem-tai-ket-noi-lai.sql](kiem-tai/sharedata-kiem-tai-ket-noi-lai.sql) trên `DEV_ITS015`, giữ mặc định (`@TienToMa = NULL` là toàn bộ đối tác), rồi chạy lại khối kiểm số lượng ở bước 6. Chạy lúc WebAPI còn tắt vì K6 không xóa bộ nhớ đệm WebAPI. Bỏ qua bước này thì Worker không gửi gì: chiều gửi chỉ quét đối tác đã kết nối (`SessionState = 1`) và đăng ký đang chạy (`State = 1`). Nút Kết nối trên màn hình không tự bật lại đăng ký, K6 thì bật. K6 mặc định kết nối mọi đối tác đang bật, kể cả đối tác thử thêm trên màn hình (staging 18/11 có `Test_01` với 2 đăng ký gửi): không muốn gửi cho đối tác đó thì Tắt hoặc xóa nó trên màn hình trước, xem trước danh sách bằng `@ChiXem = 1`.
- [ ] Bật WebAPI trước.
- [ ] Bật 6 Worker:
  ```powershell
  net start ShareDataWorker
  net start ShareDataWorker_Inbound_1
  net start ShareDataWorker_Outbound_1
  net start ShareDataWorker_Outbound_2
  net start ShareDataWorker_Outbound_3
  net start ShareDataWorker_Outbound_4
  ```
- [ ] Trên web: bấm 🔄 ở avatar góc phải trên → Ngôn ngữ để làm mới câu dịch; đăng xuất, đăng nhập lại để nạp menu.
- [ ] Trang Giám sát dịch vụ có 6 thẻ ShareData ở trạng thái Đang chạy.
- [ ] Màn Cấu hình chia sẻ thấy đủ 10 đối tác; màn Cấu hình gói tin: gói 101 ghi "10 đối tác đang dùng gói".

## Bước 8. Theo dõi khi kiểm tải

- Mốc đang chạy đã bị xóa nên **cả 10 đối tác đọc lại toàn bộ lịch sử**: gói 103, 106 đọc `TmsTrafficData` khoảng 342 nghìn dòng, mỗi đối tác khoảng 3.400 trang. Giai đoạn đuổi kịp này nặng nhất.
- Câu mẫu một tầng không dùng chỉ mục nên **mỗi lượt gửi đều đọc cả bảng nguồn, kể cả khi đã có mốc**: mốc chỉ giảm số dòng gửi đi, không giảm số dòng phải đọc. Lúc đuổi kịp lịch sử còn phải sắp xếp gần cả bảng nên tràn tempdb (staging 12/11 trước khi có chỉ mục: gói 103 tràn khoảng 120 MB mỗi lượt); đuổi kịp rồi thì hết tràn nhưng vẫn quét liên tục theo chu kỳ của 10 đối tác (đã chốt giữ PA1 một tầng và giảm từ 52 xuống 10 đối tác để bớt tải). Nếu tempdb, đĩa lên cao kéo dài thì dừng bớt dịch vụ gửi (`ShareDataWorker_Outbound_*`) hoặc ngắt bớt đối tác bằng K5 [sharedata-kiem-tai-ngat-ket-noi.sql](kiem-tai/sharedata-kiem-tai-ngat-ket-noi.sql) (mặc định `@TienToMa = NULL` ngắt toàn bộ đối tác; muốn ngắt một dải KT thì đặt `@TienToMa = N'KT'`, `@TuSo`, `@DenSo`).
- Theo dõi trên máy chủ: CPU, RAM của 6 Worker và SQL Server; đĩa (HDD RAID 5) và kích thước tempdb.
- CSDL nhận để FULL recovery: mỗi gói nhận được ghi lại vào bảng đích nên log tăng nhanh trong giai đoạn đuổi kịp. Xem bằng `DBCC SQLPERF(LOGSPACE);`, backup log định kỳ nếu tăng gần đầy đĩa.
- Trên web: Lịch sử chia sẻ, Cảnh báo & lỗi (ESH-xxxx), khối "Mốc đang chạy" ở Cấu hình gói tin (mốc phải tiến dần).
- Dọn dữ liệu kiểm tải khi xong (nếu cần): đặt `@XoaDuLieuKiemTai = 1` rồi chạy ngược K4 → K3 → K2 → K1.

---

## Xử lý nhanh

| Hiện tượng | Cách xử lý |
| :--- | :--- |
| Tệp X, K, I báo lỗi 1222 | Bảng đang bị câu khác giữ khóa; dừng dịch vụ liên quan rồi chạy lại tệp |
| Worker không lên | Event Viewer → Windows Logs → Application |
| Cảnh báo ESH-1308 ngay sau khi bật | Worker và WebAPI lệch phiên bản; kiểm lại bước 2 |
| Bật dịch vụ mà không có gói nào gửi đi, Lịch sử chia sẻ trống | Đối tác đang ngắt kết nối, đăng ký đang tạm dừng (sau K5): chạy K6 [sharedata-kiem-tai-ket-noi-lai.sql](kiem-tai/sharedata-kiem-tai-ket-noi-lai.sql), rồi khởi động lại WebAPI để màn hình thấy trạng thái mới |
| Màn hình không thấy đối tác mới | Khởi động lại WebAPI (danh sách đối tác đọc qua cache) |
