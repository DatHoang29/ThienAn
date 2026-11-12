# 🗄️ Danh Mục Kịch Bản SQL Phân Hệ ShareData (ESHARE)

> **Căn cứ quy chuẩn:** [`.agents/rules/database-mcp_thienan.md` §2.3](../../../../.agents/rules/database-mcp_thienan.md) (Rule 19.85: Entity là SSOT, CodeFirst tạo khung trước; Rule 19.101: tệp `_local.sql` chỉ chạy trên DB local; Rule 19.103: **mỗi tệp chỉ ghi một bảng, tên tệp mang hậu tố tên bảng**).  
> **Cập nhật lần cuối:** 11/11/2026

---

## 📌 1. Hai Loại Tệp: Chạy Ở Đâu

| Loại | Vị trí / tên tệp | Chạy trên DB nào | Nội dung |
| :--- | :--- | :--- | :--- |
| **Dữ liệu chuẩn** | `sql/sharedata-…sql` (**không** có `_local`) | **Mọi DB**: local, dev, staging, production. DB local cũng phải chạy đủ các tệp còn trong bảng mục 3A (2 → 16) | Dữ liệu hệ thống cần để chạy: câu SQL mẫu gói 101–111, mã lỗi, bảng nguồn, cấu hình, bản dịch, menu, đối tác, bộ mã chuẩn hóa, hồ sơ ánh xạ |
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
| ~~1~~ | ~~`sharedata-seed-goi-tin-ShareDataPacket.sql`~~ | — | **Đã xóa tệp 06/11/2026** (commit `a9cf99ec`): CSDL đã có 11 gói Catalog thì không cần. CSDL mới chưa có gói thì lấy lại tệp bằng `git show a9cf99ec^:"DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/sharedata-seed-goi-tin-ShareDataPacket.sql"` và chạy trước tệp 2. Nội dung cũ: Thêm 11 gói Catalog (101–111) nếu chưa có gói đang sống cùng mã, chuẩn hóa `OrderNo`, chuẩn hóa `Status` (bật 110, tắt 111). |
| **2** | [`sharedata-seed-goi-tin-ShareDataSql.sql`](sharedata-seed-goi-tin-ShareDataSql.sql) | `ShareDataSql` | Chuyển câu SQL cũ (cột `ShareDataPacket.ExtractSql`, bảng `ShareDataPartnerSql`, cột `ShareDataMapping.ExtractSql`) sang `ShareDataSql`; khôi phục dòng mẫu chung đã xóa mềm của gói 101–111 (bước 0.5: chỉ mục duy nhất `PacketId, PartnerId` chặn thêm dòng mới, vd gói 105 trống câu truy vấn trên staging 11/11/2026); nạp câu mẫu chung (dòng `PartnerId` trống) khi đang trống — **nơi duy nhất chứa câu SQL mẫu chuẩn 101–111**; đổi `__rowid` → `__lastKey`; 7 câu nối đuôi (101, 103, 104, 106, 107, 109, 111) dùng một chuỗi ghép `CONVERT(varchar(23), <thời gian>, 121) + '_' + <ID> AS __lastKey` (02/11/2026); bước 1b điền bộ biến cho câu còn trống. Gói 101 lấy `COALESCE(zs.UpdateTime, zs.CreateTime)` từ 04/11/2026: DB đã có câu mẫu thì chạy tệp này với `@GhiDeMaGoi = N'101'`; đối tác dùng mẫu chung đang có mốc sẽ bị Worker bỏ mốc cũ và gửi lại (ESH-1308), muốn tránh thì tắt đăng ký và Đặt mốc trước. Cần bản ghi gói đã có trong `ShareDataPacket`. Bộ biến câu truy vấn lưu ở cột `VariablesJson` (bỏ bảng `ShareDataSqlVariable`, tự chuyển dữ liệu cũ); câu ghi chiều nhận ở `WriteSql`, `AffectedTables` của dòng mẫu chung. |
| ~~3~~ | ~~`sharedata-seed-goi-tin-ShareDataPacketSql.sql`~~ | — | **Đã gộp vào tệp 2 (27/10/2026):** câu ghi chiều nhận nằm ở cột `WriteSql`, `AffectedTables` của dòng mẫu chung `ShareDataSql`; tệp 2 tự chuyển dữ liệu cũ từ bảng `ShareDataPacketSql`. |
| **4** | [`sharedata-seed-cau-hinh-SysConfigType.sql`](sharedata-seed-cau-hinh-SysConfigType.sql) | `SysConfigType` | Khởi tạo loại danh mục `sharedata_error_code`. |
| **5** | [`sharedata-seed-cau-hinh-SysConfigData.sql`](sharedata-seed-cau-hinh-SysConfigData.sql) | `SysConfigData` | Nạp 26 mã cảnh báo `ESH-xxxx`; đăng ký nhóm `sharedata_config` vào loại `operation_config_type`. **Chạy sau tệp 4.** |
| **6** | [`sharedata-seed-cau-hinh-ShareDataSourceTable.sql`](sharedata-seed-cau-hinh-ShareDataSourceTable.sql) | `ShareDataSourceTable` | Nạp danh mục 15 bảng nguồn (`IsTracked`, `IsQueryable`, `DefaultAlias`). |
| **7** | [`sharedata-seed-cau-hinh-SysOpConfig.sql`](sharedata-seed-cau-hinh-SysOpConfig.sql) | `SysOpConfig` | Nạp 43 tham số nhóm `sharedata_config` (giữ giá trị còn ở nhóm cũ `sharedata_fe_config`); xóa mềm tham số không còn dùng (gồm `outbound_max_pages_per_run`, `outbound_lock_budget_percent` từ 09/11/2026). |
| ~~8~~ | ~~`sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql`~~ | — | **Đã xóa tệp 28/10/2026:** việc dọn nhóm cũ `sharedata_fe_config` chạy một lần, DB mới không có nhóm này. |
| ~~9~~ | ~~`sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigType.sql`~~ | — | **Đã xóa tệp 28/10/2026:** như tệp 8. |
| **10** | [`sharedata-dich-thuat-SysTerminology.sql`](sharedata-dich-thuat-SysTerminology.sql) | `SysTerminology` | Nạp 168 khóa thuật ngữ (336 dòng vi-VN / en-US) qua `MERGE` không ghi đè bản dịch đã sửa. Có sửa lại 11 câu lỗi đã nạp từ trước: câu dịch lỗi dùng cho cả máy chủ (`string.Format`) lẫn màn hình, nên không được có `{'@'}`, và lỗi nối đuôi không dùng chỗ điền `{0}` (04/11/2026). Chạy xong bấm 🔄 ở avatar góc phải trên → Ngôn ngữ để làm mới cache. |
| ~~11~~ | ~~`sharedata-chuyen-doi-moc-ShareDataLastSend.sql`~~ | — | **Đã xóa tệp 06/11/2026:** việc chuyển mốc một lần, DB mới không cần. Nội dung cũ: Gỡ unique index 2 cột cũ `index_ShareDataLastSend_Partner_Packet`; chép không trùng các dòng nhóm mốc có tên từ bảng cũ `ShareDataLastSendKey` (chỉ đọc) sang `ShareDataLastSend`. **Phải chạy sau CodeFirst; tắt Worker trước khi chạy.** |
| ~~12~~ | ~~`sharedata-xoa-bang-cu-ShareDataLastSendKey.sql`~~ | — | **Đã xóa tệp 06/11/2026:** như tệp 11. Nội dung cũ: Xóa bảng cũ `ShareDataLastSendKey` sau khi đã gộp cột `CursorKey` vào `ShareDataLastSend` (chặn bằng `SET NOEXEC ON` nếu cột `CursorKey` chưa có hoặc còn dòng chưa chuyển). **Chạy sau tệp 11.** |
| **13** | [`sharedata-seed-menu-SysMenu.sql`](sharedata-seed-menu-SysMenu.sql) | `SysMenu` | Cấu hình menu phân hệ ShareData dạng tab (`tabPage/index`) gồm 2 nhóm: **Cấu hình chia sẻ** (Cấu hình, Cấu hình gói tin, Ánh xạ dữ liệu, Bộ mã Chuẩn hóa, Nguồn sự kiện) và **Theo dõi chia sẻ** (Lịch sử chia sẻ, Cảnh báo & lỗi); chuyển các menu chức năng thành tab con (giữ nguyên ID), gỡ menu Mốc gửi (`shareDataCursor`, xóa mềm) vì mốc đang chạy nay ở khối 4 của Cấu hình gói tin (07/11/2026). Chạy lại an toàn; chạy xong đăng nhập lại để nạp menu. |
| **14** | [`sharedata-seed-doi-tac-ShareDataPartner.sql`](sharedata-seed-doi-tac-ShareDataPartner.sql) | `ShareDataPartner` | Tạo 2 đối tác `PC08` (Phòng Cảnh sát giao thông) và `TTDHT` (Trung tâm điều hành tuyến) nếu chưa có đối tác đang sống cùng mã. Chưa có địa chỉ kết nối nên để trống `Address`, `Port`, `EndPointApiUrl`; đối tác để bật vì màn hình Cấu hình chia sẻ ẩn đối tác tắt (chưa có đăng ký nên Worker chưa gửi gì). Nhập địa chỉ bằng nút sửa đối tác. Chạy lại an toàn; dòng do bản trước của tệp tạo ở trạng thái tắt, chưa ai sửa, sẽ được bật lên. |
| **15** | [`sharedata-seed-bo-ma-ShareDataCodeSet.sql`](sharedata-seed-bo-ma-ShareDataCodeSet.sql) | `ShareDataCodeSet` | Nạp 9 bộ mã chuẩn hóa (`traffic_condition`, `lane_code`, `device_state`, `vds_vehicle_class`, `toll_vehicle_type`, `incident_direction`, `incident_state`, `incident_source`, và `incident_type` đọc từ `TmsEventType`); dọn mềm dòng thử nghiệm cũ `condition` nếu không có hồ sơ ánh xạ đang sống tham chiếu. Không ghi đè bộ mã quản trị đã sửa khi `@GhiDeBoMaMau = 0`. **Chạy trước tệp 16.** |
| **16** | [`sharedata-seed-anh-xa-ShareDataMapping.sql`](sharedata-seed-anh-xa-ShareDataMapping.sql) | `ShareDataMapping` | Xóa mềm hồ sơ ánh xạ thừa của `PC08`, `TTDHT`; tạo/cập nhật 3 hồ sơ gói `101_commonData` đang bật (`PC08_101COMMONDATA_OUT`, `PC08_101COMMONDATA_IN`, `TTDHT_101COMMONDATA_IN`) áp dụng bộ mã chuẩn hóa `lane_code` (cho `laneId`) và `traffic_condition` (cho `trafficCondition`). Hồ sơ quản trị đã sửa tay trên giao diện được giữ nguyên khi `@GhiDeAnhXaMau = 0`. **Chạy sau tệp 2, 14 và 15.** |

### C. Dọn một lần trên DB cũ

| Thứ tự | Tệp | Bảng ghi | Nội dung |
| :---: | :--- | :--- | :--- |
| ~~D1~~ | ~~`sharedata-xoa-cot-thua-ShareDataPacket.sql`~~ | — | **Đã xóa tệp 06/11/2026** (commit `a9cf99ec`): DB mới tạo bằng CodeFirst không có các cột này. Nội dung cũ: Xóa 5 cột entity đã bỏ (`ExtractSql`, `TemplateSql`, `CursorTimeColumn`, `SyncMode`, `MaxLookBackDays`) trên DB tạo trước 27/10/2026. |
| ~~D2~~ | ~~`sharedata-sua-cau-rieng-moc-hai-cot-ShareDataSql.sql`~~ | — | **Đã xóa tệp 06/11/2026** (commit `a9cf99ec`): câu SQL riêng chưa theo quy chuẩn mốc sửa bằng nút **Sửa câu** trên giao diện. Nội dung cũ: Sửa câu SQL riêng của đối tác còn mốc 2 cột chép từ câu mẫu gói 101 cũ sang một chuỗi ghép `COALESCE(zs.UpdateTime, zs.CreateTime)`. |
| ~~D3~~ | ~~`sharedata-xoa-doi-tac-cu-ShareDataPartner.sql`~~ | — | **Không có tệp trong git:** việc dọn một lần trên `DEV_ITS015` cũ, DB mới không cần. Nội dung cũ: Xóa mềm 8 đối tác thử nghiệm còn sót (`ddlt`, `ddlt1`, `DDTL`, `DDTL2`, `DDTL-2`, `Partner`, `PartnerPartner`, `TTCSDL`). |
| ~~D4~~ | ~~`sharedata-xoa-dang-ky-doi-tac-da-xoa-ShareDataSubscription.sql`~~ | — | **Không có tệp trong git:** như D3. Nội dung cũ: Hủy rồi xóa mềm đăng ký của đối tác đã xóa. |
| ~~D5~~ | ~~`sharedata-xoa-anh-xa-doi-tac-da-xoa-ShareDataMapping.sql`~~ | — | **Không có tệp trong git:** như D3. Nội dung cũ: Xóa mềm cấu hình ánh xạ của đối tác đã xóa. |
| ~~D6~~ | ~~`sharedata-dong-phien-doi-tac-da-xoa-ShareDataSession.sql`~~ | — | **Không có tệp trong git:** như D3. Nội dung cũ: Đóng phiên đang mở của đối tác đã xóa. |
| ~~D7~~ | ~~`sharedata-dat-lai-mau-chung-ShareDataSql.sql`~~ | — | **Không có tệp trong git.** Muốn đưa câu mẫu chung về bản chuẩn thì chạy tệp 2 với `@GhiDeMauGoiTin = 1` hoặc `@GhiDeMaGoi` (xem mục 4, *Nạp lại câu mẫu chung*). |
| **N1** | [`sharedata-tao-bang-dich-chieu-nhan-InboundDB.sql`](sharedata-tao-bang-dich-chieu-nhan-InboundDB.sql) | 14 bảng đích trong CSDL nhận `*_Inbound` | Tạo bảng đích của câu ghi chiều nhận gói 101–109 (`TmsZone`, `TmsTrafficStatistic`, `TmsZoneStatus`, `TmsEquipment`, `CctvDevice`, `TmsTrafficData`, `TmsWeather`, `TmsVehicleRegistration`, `TollTransactionOut`, `TmsEventType`, `TmsIncident`, `VmsCurrent`, `TollStation`, `TollLane`) trong CSDL nhận riêng, cấu trúc chép từ CSDL vận hành (cột, kiểu, mặc định, khóa chính). Dữ liệu nhận chỉ ghi vào CSDL nhận (đặc tả AC-IN-06). Chạy trên CSDL không có hậu tố `_Inbound` thì tự bỏ qua. Ngoại lệ Rule 19.103: một tệp tạo nhiều bảng của một CSDL riêng. **Chạy sau CodeFirst của ConfigId `ShareDataDB`.** |
| ~~D8~~ | ~~`sharedata-xoa-cot-thua-ShareDataSql.sql`~~ | — | **Không có tệp trong git:** DB mới tạo bằng CodeFirst không có cột này. Nội dung cũ: Xóa cột `CursorTimeColumn` đã bỏ khỏi entity 03/11/2026. |
| ~~D9~~ | ~~`sharedata-xoa-cot-thua-ShareDataSubscription.sql`~~ | — | **Không có tệp trong git:** như D8. Nội dung cũ: Xóa cột `MappingId` đã bỏ khỏi entity. |

### B. Dữ liệu giả lập (⛔ chỉ DB local, hậu tố `_local`, thư mục con [`local/`](local/))

Chỉ giả lập bảng nghiệp vụ ShareData; không ghi vào bảng của phân hệ khác (`TmsEquipment`, `TmsWeather`, `TmsTrafficData`, `TmsIncident`, `VmsCurrent`, `TmsSignalLog`) hay bảng đích chiều nhận `InboundTrafficFlow` — các bảng đó dùng dữ liệu sẵn có.

| Thứ tự | Tệp | Bảng ghi | Nội dung |
| :---: | :--- | :--- | :--- |
| **G1** | [`sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql`](local/sharedata-du-lieu-gia-lap-ShareDataPacket_local.sql) | `ShareDataPacket` | Gói kiểm thử `E2E_INBOUND_TEST`. Chạy sau dữ liệu chuẩn (mục 3A). |
| **G2** | [`sharedata-du-lieu-gia-lap-ShareDataSql_local.sql`](local/sharedata-du-lieu-gia-lap-ShareDataSql_local.sql) | `ShareDataSql` | Câu mẫu chung của `E2E_INBOUND_TEST` theo gói 101. Chạy sau G1 và tệp 2. |
| ~~G3~~ | ~~`sharedata-du-lieu-gia-lap-ShareDataPacketSql_local.sql`~~ | — | **Đã gộp vào G2 (27/10/2026):** khối câu ghi MERGE của `E2E_INBOUND_TEST` nằm ở mục 2 của `sharedata-du-lieu-gia-lap-ShareDataSql_local.sql`. |

- **Dọn dẹp:** mỗi tệp giả lập có khối dọn dẹp riêng, đặt `@XoaDuLieuGiaLap = 1` trong tệp đó. Dọn theo thứ tự ngược: G2 → G1 (G2 trước G1 vì G2 tìm dòng theo gói `E2E_INBOUND_TEST`).
- Mọi dòng giả lập gắn `CreateUId = N'SHAREDATA-SEED-GIA-LAP'`.
- G1–G2 chỉ chạy trên DB local.

### D. Kiểm tải nhiều đối tác & gói tin (staging, thư mục con [`kiem-tai/`](kiem-tai/))

Khôi phục đối tác gốc (`PC08`, `TTDHT`), tạo thêm hàng loạt đối tác kiểm tải (`KT001`..`KT050`), đồng thời bật đủ hồ sơ ánh xạ và đăng ký gửi/nhận cho toàn bộ 9 gói tin đang bật trên staging (`101_commonData`, `102_cctvData`, `103_vdsData`, `104_weatherData`, `106_wimData`, `107_incidentData`, `108_vmsInfo`, `109_etcData`, `110_wpData`), để các dịch vụ `ShareDataWorker` đang chạy gửi đồng thời cho nhiều đối tác rồi theo dõi CPU, RAM và hiện tượng nghẽn trên Task Manager của máy chủ. Không sửa mã, không đổi cấu hình dịch vụ. ⛔ Không chạy trên production.

| Thứ tự | Tệp | Bảng ghi | Nội dung |
| :---: | :--- | :--- | :--- |
| **K1** | [`sharedata-kiem-tai-ShareDataPartner.sql`](kiem-tai/sharedata-kiem-tai-ShareDataPartner.sql) | `ShareDataPartner` | Khôi phục dòng `PC08` theo đúng `ID = 97ade67b-9e9b-44e3-ba0e-f31670c2a578` đang được các bảng staging tham chiếu, cập nhật kết nối cho `TTDHT`, và tạo `@SoDoiTac` đối tác (`KT001`..`KT050`) ở trạng thái Bật và Đã kết nối (`SessionState = 1`). Mặc định mọi đối tác gọi API nhận của chính WebAPI staging `https://10.10.12.80:12204/api/sharedata/sharedatainbound/receive` (kênh WebAPI `ConnectionType = 1`, `{"Format":"Json","BypassSsl":"true"}`); chạy lại thì đối tác kiểm tải đã có được đưa về cấu hình đang khai. |
| **K2** | [`sharedata-kiem-tai-ShareDataMapping.sql`](kiem-tai/sharedata-kiem-tai-ShareDataMapping.sql) | `ShareDataMapping` | Khôi phục các hồ sơ ánh xạ gốc của `PC08`, `TTDHT` đang bị xóa mềm (`IsDelete = NULL`, `Status = 1`) và tạo đầy đủ hồ sơ ánh xạ chiều gửi + nhận của mọi gói đang bật cho từng đối tác `KT001`..`KT050`. Chạy sau K1. |
| **K3** | [`sharedata-kiem-tai-ShareDataSubscription.sql`](kiem-tai/sharedata-kiem-tai-ShareDataSubscription.sql) | `ShareDataSubscription` | Bật lại đăng ký đang tạm dừng (`State = 2 → 1`) của `PC08`, `TTDHT` và tạo đầy đủ đăng ký gửi (`Direction = 0`, `State = 1`) + nhận (`Direction = 1`, `State = 1`) cho toàn bộ 52 đối tác × 9 gói đang bật (468 đăng ký gửi đang hoạt động), cùng chu kỳ `@ChuKyGiay` (mặc định 30 giây), chạy cả ngày. `SerialNbr` gán như khi tạo trên màn hình (số lớn nhất của đối tác + 1), nên gói gửi đầu tiên đã mang số thứ tự và WebAPI chặn được gói trùng. Chạy sau K2. Từ 12/11/2026: `@BuocGuiKhiCoDuLieuMoi` (mặc định 2) cho cứ mỗi N đối tác kiểm tải có 1 đối tác bật "Gửi khi có dữ liệu mới" ở mọi đăng ký gửi (`KT002`, `KT004`…), vẫn giữ chu kỳ; chạy lại tệp thì cờ của đăng ký kiểm tải đã có được chỉnh theo tham số; `0` = không bật. `@DanhSachChuKy` (mặc định `15,20,25,30,60`) chia chu kỳ gửi cho từng cặp đối tác kiểm tải × gói, cùng cặp luôn cùng chu kỳ; `PC08`, `TTDHT` và đăng ký nhận giữ `@ChuKyGiay`; chạy lại tệp thì chu kỳ của đăng ký gửi kiểm tải đã có được chỉnh theo. |
| **K4** | [`sharedata-kiem-tai-ShareDataSql.sql`](kiem-tai/sharedata-kiem-tai-ShareDataSql.sql) | `ShareDataSql` | Từ `KT026` trở đi (`@TuSoDoiTacRieng`), mỗi cặp đối tác × gói có đăng ký gửi được một câu SQL riêng sinh từ câu mẫu chung, mỗi câu khác nhau: thêm 0–4 cột phụ, đổi `TOP 100` thành 20/50/200/500, đổi xuống dòng, chú thích dài ngắn. Giữ nguyên công thức mốc, WHERE, ORDER BY nên câu riêng trả đúng các dòng như mẫu chung. `KT001`–`KT025` dùng mẫu chung. Chạy sau K3. |

- **Chạy ngay không cần điền tay:** mặc định `@MaDoiTacMau = NULL`, các đối tác gửi vào API nhận của chính WebAPI staging (cổng `12204`; màn hình web là cổng `12205`). Gói gửi đi mang header `PartnerCode` = mã đối tác nên WebAPI nhận dưới chính đối tác đó, lưu thô vào `ShareDataInboundPacket` của `DEV_ITS015_Inbound`, rồi dịch vụ chỉ nhận ghi bằng câu ghi của gói vào bảng đích trong `DEV_ITS015_Inbound` (bảng đích tạo bằng tệp N1). Nhờ vậy một lượt kiểm tải đo được cả hai chiều. Muốn chép kênh kết nối từ một đối tác có sẵn thì điền `@MaDoiTacMau` ở K1; muốn quay về kênh xuất tệp thì đặt `@KieuKetNoiMacDinh = 2`, `@EndPointMacDinh = NULL`.
- **Vì sao bật `BypassSsl`:** cổng `12204` dùng chứng chỉ cấp cho tên miền, gọi bằng IP thì tên không khớp; bỏ `BypassSsl` thì mọi lượt gửi báo lỗi chứng chỉ SSL/TLS.
- **Gói chưa có câu ghi chiều nhận (hiện là `110_wpData`) K2, K3 tự bỏ:** không tạo hồ sơ, đăng ký, nên bên nhận không báo lỗi hàng loạt; staging còn 8 gói (101–104, 106–109). Muốn bỏ thêm gói nào thì đặt `@GoiBoQua` giống nhau ở K2 và K3; K4 tự bỏ theo vì chỉ sinh câu riêng cho gói có đăng ký gửi.
- **Màn hình chưa hiện đối tác mới sau K1:** danh sách đối tác đọc qua cache của WebAPI; sửa rồi Lưu một đối tác bất kỳ (không đổi gì) hoặc khởi động lại WebAPI.
- **Phạm vi gói:** chỉ gói đang bật trên CSDL. Staging 11/11/2026 có 9 gói bật (101–104, 106–110); gói 105 đang tắt và câu mẫu chung đã xóa mềm, gói 111 chưa có bản ghi, nên hai gói này không được tạo cấu hình.
- **Dọn dẹp:** đặt `@XoaDuLieuKiemTai = 1` rồi chạy ngược K4 → K3 → K2 → K1. Mọi dòng tạo mới gắn `CreateUId = N'SHAREDATA-KIEM-TAI'` được xóa sạch; các dòng gốc của `PC08`, `TTDHT` được giữ lại.

### E. Chỉ mục cột mốc nối đuôi (thư mục con [`chi-muc/`](chi-muc/))

Câu nối đuôi so mốc trên chuỗi ghép `CONVERT(varchar(23), COALESCE(<thời gian>), 121) + '_' + ID` nên không chỉ mục thường nào dùng được: mỗi lượt quét và sắp xếp cả bảng nguồn, bảng lớn thì tràn xuống `tempdb` (staging 12/11/2026: gói 103 tràn 120 MB mỗi lượt). Tệp I1, I2 thêm / gỡ cột tính toán `CursorKey` và chỉ mục `IX_<TênBảng>_CursorKey` trên 6 bảng nguồn của 7 gói nối đuôi. Câu mẫu không phải đổi.

| Thứ tự | Tệp | Bảng đổi cấu trúc | Nội dung |
| :---: | :--- | :--- | :--- |
| **I1** | [`sharedata-chi-muc-them-BangNguonNoiDuoi.sql`](chi-muc/sharedata-chi-muc-them-BangNguonNoiDuoi.sql) | `TmsTrafficData`, `TmsIncident`, `TollTransactionOut`, `TmsSignalLog`, `TmsZoneStatus`, `TmsWeather` | Thêm cột tính toán bằng đúng biểu thức mốc của câu mẫu và chỉ mục trên cột đó, `INCLUDE (IsDelete)`. Chạy lại an toàn; `@ChiBang` để chạy riêng một bảng; mỗi lệnh chờ khóa tối đa `@ChoKhoaMs` (5 giây), hết giờ báo lỗi 1222 thì chạy lại. Bản SQL Server hỗ trợ thì dựng `ONLINE = ON`, bản khác thì bảng bị chặn ghi trong lúc dựng. Nên dừng `ShareDataWorker` trước khi chạy. |
| **I2** | [`sharedata-chi-muc-go-BangNguonNoiDuoi.sql`](chi-muc/sharedata-chi-muc-go-BangNguonNoiDuoi.sql) | như trên | Gỡ chỉ mục rồi gỡ cột (chỉ gỡ khi là cột tính toán). Dùng khi dịch vụ ghi báo lỗi 1934, CodeFirst báo lỗi vì cột ngoài entity, hoặc cần đổi biểu thức mốc. |
| **I3** | [`sharedata-chi-muc-ShareDataActivityLog.sql`](chi-muc/sharedata-chi-muc-ShareDataActivityLog.sql) | `ShareDataActivityLog` | Đồng bộ chỉ mục `index_ShareDataActivityLog_OA` từ `CreateTime` sang `OccurredAt DESC` (khớp Entity) và thêm chỉ mục `IX_ShareDataActivityLog_LogType_ParentId_OccurredAt` (khóa `LogType, ParentId, OccurredAt`, `INCLUDE (IsDelete, Action, Success, TransferDirection)`) cho màn hình Lịch sử chia sẻ: 3 câu của màn hình lọc theo `OccurredAt` nhưng bảng chưa có chỉ mục nào trên cột này, staging 12/11/2026 mỗi câu quét cả bảng 750.955 dòng, 9–10 giây. Gỡ: đặt `@GoChiMuc = 1`. Thử local trên bản sao 463.680 dòng: đọc 11–48 trang thay cho 62.503 (lấy trang), 11–2.953 thay cho 62.503 (đếm thống kê), kết quả giống hệt. Chạy độc lập với I1, I2. |
| **I4** | [`sharedata-chi-muc-ShareDataInboundPacket.sql`](chi-muc/sharedata-chi-muc-ShareDataInboundPacket.sql) | `ShareDataInboundPacket` (`DEV_ITS015_Inbound`) | Thêm chỉ mục `index_ShareDataInboundPacket_Partner_Packet_Serial` (khóa `PartnerCode, PacketCode, SerialNbr`, `INCLUDE (IsDelete)`, cùng tên với `[SugarIndex]` trên Entity) trên CSDL nhận `DEV_ITS015_Inbound` (`ConfigId = ShareDataDB`) cho câu kiểm tra trùng `SerialNbr` khi WebAPI nhận gói tin (staging 12/11/2026: chỉ mục cũ đặt `ProcessState` ở đầu nên mỗi lượt nhận quét 3.552 trang, 294 ms). Gỡ: đặt `@GoChiMuc = 1`. |

- **Ngoại lệ Rule 19.85 và 19.103:** SqlSugar CodeFirst (5.1.4.214) không tạo được cột tính toán và chỉ mục có `INCLUDE`, nên cột và chỉ mục này tạo bằng script; hai tệp gộp 6 bảng theo yêu cầu ngày 12/11/2026.
- **Đã thử 12/11/2026 trên CSDL local** (Azure SQL Edge 15.0, bản sao 342.036 dòng `TmsTrafficData`): 4 dạng câu (`TOP 100` một bảng, `TOP 100` có nối, `TOP 500` có nối, `TOP (@pageSize)`) ở 3 vị trí mốc đều trả đúng trang như khi chưa có chỉ mục; kế hoạch dùng `Index Seek` trên chỉ mục cột mốc, không còn phép sắp xếp; đọc 357 trang thay cho 23.876 (`TOP 100`), 1.240 thay cho 31.416 (`TOP 500`). Chưa chạy trên staging.
- **Phiên ghi vào 6 bảng** phải bật `QUOTED_IDENTIFIER`, `ANSI_NULLS` (mặc định của SqlClient, SqlSugar); phiên không đạt bị lỗi 1934 khi thêm dòng. `TmsTrafficData`, `TmsIncident` đã có chỉ mục có lọc đòi cùng điều kiện; 4 bảng còn lại phải thử ghi một dòng sau khi chạy I1.
- **Câu ghép mốc bằng biểu thức khác** (đổi cột thời gian, đổi định dạng) không dùng được chỉ mục này.
- **CodeFirst với cột ngoài entity (thử 12/11/2026 trên CSDL `test` local, `EnableInitTable = true`):** chạy I1 rồi chạy toàn bộ test ShareData, 903 ca đạt, không ca nào lỗi; cột `CursorKey` và chỉ mục còn nguyên trên cả 6 bảng.

---

## 🚀 4. Hướng Dẫn Thực Thi

> 🔴 **Nguyên tắc cốt lõi (Rule 19.85):** Entity là SSOT cho cấu trúc bảng và index. CodeFirst tạo khung trước (`TableSettings.EnableInitTable = true`), script SQL chỉ seed dữ liệu. Tuyệt đối **KHÔNG** `CREATE TABLE`, `ALTER TABLE ... ADD`, `CREATE INDEX` cho bảng đã có entity.

```
CodeFirst tạo khung ➔ 2 ➔ 4 ➔ 5 ➔ 6 ➔ 7 ➔ 10 ➔ 13 ➔ 14 ➔ 15 ➔ 16 ➔ (tùy chọn) G1 ➔ G2
```

1. **Bước 0 — CodeFirst:** khởi động WebAPI (bật tạm `TableSettings.EnableInitTable = true` trong cấu hình debug local) để tạo đủ bảng và index, gồm `ShareDataSql` (câu SQL, bộ biến `VariablesJson`, danh sách trường `FieldsJson` và câu ghi `WriteSql`, unique theo gói + đối tác), `ShareDataLastSend` (mốc nối đuôi nhóm mặc định và nhóm có tên theo `CursorKey`, unique `index_ShareDataLastSend_Partner_Packet_Key` trên `(PartnerCode, PacketCode, CursorKey)`), `ShareDataSourceTable` (unique `UX_ShareDataSourceTable_TableName`).
2. **Bước 1 — chạy tệp 2 → 16** theo bảng mục 3A, bỏ các dòng gạch ngang (mọi DB, kể cả local). CSDL phải có sẵn 11 gói Catalog; CSDL mới chưa có gói thì lấy lại tệp 1 từ git như ghi ở dòng tệp 1 rồi chạy trước tệp 2.
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
3. Khởi động lại WebAPI, Worker. Câu mẫu mới đổi công thức cột mốc thì Worker bỏ mốc cũ của đối tác dùng câu mẫu chung, ghi ESH-1308 và gửi lại từ Mốc bắt đầu.
4. Câu SQL riêng của đối tác chưa theo quy chuẩn mốc (Worker dừng gói, ESH-1306): mở tab Cấu hình gói tin, tab đối tác, bấm **Sửa câu** rồi lưu.

### Khởi tạo DB mới bằng cờ CodeFirst (thử nghiệm)

> ⚠️ **KHÔNG** bật `EnableInitSeed` trên CSDL đã có dữ liệu (có thể sinh dòng trùng mã `Code`). Seed class C# đã bỏ hoàn toàn (Rule 19.85); bản ghi 11 gói Catalog nằm ở tệp 1 (đã xóa 06/11/2026, lấy lại từ git như ghi ở dòng tệp 1 trước khi làm checklist dưới), câu SQL mẫu chỉ nằm ở tệp 2.

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

- Các tệp chạy trên CSDL chính **`[DEV_ITS10]`**. Câu ghi của G2 `MERGE` vào bảng đích `dbo.InboundTrafficFlow` trên `[DEV_ITS10_Inbound]`; thư mục không tạo bảng này, cần thì tạo tay khi kiểm thử chiều nhận.
- **P0 Safeguards (Rule 16):** AI không tự thực thi lệnh ghi lên CSDL. Lập trình viên kiểm tra và thực thi script trên DB.
- **Ghi chú lịch sử:** trước 24/10/2026 thư mục có 4 tệp gộp (`sharedata-seed-goi-tin.sql`, `sharedata-seed-cau-hinh.sql`, `sharedata-dich-thuat.sql`, `sharedata-du-lieu-gia-lap_local.sql`), mỗi tệp ghi nhiều bảng. Nội dung đã tách nguyên vẹn sang bộ tệp trên. Riêng mục 5 cũ của `sharedata-seed-cau-hinh.sql` (ghi chú bộ mã sự cố gói 107 đã gỡ ngày 22/10/2026) chỉ là chú thích, không còn câu lệnh, nên không chép sang.
