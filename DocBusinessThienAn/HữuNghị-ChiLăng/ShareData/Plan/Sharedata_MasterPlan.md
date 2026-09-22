# ShareData (ESHARE) — Master Plan FE · BE · Service

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT):** Tài liệu quy hoạch tổng thể duy nhất cho toàn bộ phân hệ **ShareData (ESHARE)** gồm Frontend, Backend WebAPI và Service Worker.
> Được hợp nhất từ các tài liệu phân tích, kế hoạch kiểm thử, cơ chế gửi nối đuôi Checkpoint và kích hoạt sự kiện Change Tracking + NATS.

---

## 🗺️ Mục lục nhanh

- [PHẦN I — SERVICE: ShareDataWorker (`SV-*`)](#phần-i--service-sharedataworker-sv-)
  - [Checklist SV](#checklist-sv--xếp-theo-scope)
  - [I.A · Dùng chung cả hai chiều](#ia--dùng-chung-cả-hai-chiều--🤝-đạt--hiếu)
  - [I.B · Chiều GỬI — Outbound & Cơ chế Nối đuôi / Event](#ib--chiều-gửi--outbound--🧑‍💻-đạt)
  - [I.C · Chiều NHẬN — Inbound](#ic--chiều-nhận--inbound--🧑‍💻-hiếu)
- [PHẦN II — BACKEND WebAPI: `Module.ShareData` (`BE-*`)](#phần-ii--backend-webapi-modulesharedata-be-)
- [PHẦN III — FRONTEND (`TA-ITS015-WEBVUE-V1.0`)](#phần-iii--frontend-ta-its015-webvue-v10)
  - [Mục A · Cấu hình Đối tác & Đăng ký](#a-cấu-hình-đối-tác--đăng-ký-chia-sẻ)
  - [Mục B · Cấu hình Gói tin](#b-cấu-hình-gói-tin--datasourceindexvue)
  - [Mục C · Ánh xạ dữ liệu](#c-ánh-xạ-dữ-liệu)
  - [Mục D · Lịch sử chia sẻ](#d-lịch-sử-chia-sẻ--historyindexvue)
  - [Mục E · Tooltip đồng bộ](#e-tooltip-đồng-bộ-toàn-module)
- [PHẦN IV — LUỒNG KIẾN TRÚC XỬ LÝ OUTBOUND & QUY CHUẨN ÁNH XẠ](#phần-iv--luồng-kiến-trúc-xử-lý-outbound--quy-chuẩn-ánh-xạ)
  - [1. Luồng chuẩn 3 bước](#1-luồng-chuẩn-3-bước-tổng-quát)
  - [2. Nguồn cấu hình: `TargetShapeJson` & 6 khoá `$extend`](#2-nguồn-cấu-hình-duy-nhất-targetshapejson)
  - [3. Quy chuẩn đối chiếu trường & Cảnh báo ESH](#3-quy-chuẩn-đối-chiếu-tên-trường--cảnh-báo)
- [PHẦN V — BẢO ĐẢM AN TOÀN & KIỂM THỬ (TESTING)](#phần-v--bảo-đảm-an-toàn--kiểm-thử-testing)

---

# PHẦN I — SERVICE: ShareDataWorker (`SV-*`)

## Checklist SV — xếp theo scope

### I.A · Dùng chung cả hai chiều — 🤝 Đạt + Hiếu · ⏸ **làm sau khi luồng 1-1 chạy ổn**

- [ ] **SV-4** Cấu hình vai trò instance + mã định danh của mình (`ShareData:Role`, `ShareData:SelfPartnerCode`) 🔑
- [ ] **SV-5** Kịch bản test đa đối tác bằng clone service *(sau khi SV-1a + SV-1b xong)*
- [ ] **SV-6** Đo tải & rủi ro nghẽn CSDL *(sau khi luồng 1-1 chuẩn)*
- [ ] **SV-9** *(Giai đoạn sau)* Xác thực đối tác bằng key / token

### I.B · Chiều GỬI — Outbound — 🧑‍💻 Đạt

- [x] **SV-1a** Bỏ vỏ `httpPayload` 7 khoá khi gửi đi 🟢 **xong 18/09**
- [x] **SV-2a** Bỏ phân nhánh xử lý theo Version *(chiều gửi)* 🟢 **đã đúng** — `PacketVersion` chỉ xuất hiện trong log, **0 chỗ rẽ nhánh**.
- [x] **SV-3a** Đặt `partnerCode` vào trong dữ liệu gửi đi 🟢 **xong 18/09** — M1 giải `$meta: PartnerCode` bằng `ctx.Partner.Code`.
- [ ] **SV-8a** Ghi log 2 bước cha–con *(chiều gửi)* 🔴 **chặn bởi BE-5** (`ParentId`/`StepNo` trên `ShareDataActivityLog`).
- [x] **SV-10** Rà soát pipeline Outbound sau refactor 🟢 **xong 18/09** — Tầng gửi không logic nghiệp vụ, lỗi mapping dừng bước 2, `DataOutboundContext` xuyên suốt.
- [x] **SV-12** Cờ *"chỉ gửi khi có dữ liệu mới"* & Cơ chế Gửi nối đuôi Checkpoint 🟢 **hoàn tất 22/09**:
  - Kiến trúc Checkpoint độc lập `ShareDataCheckpoint` theo từng cặp `(PartnerCode, PacketCode)`.
  - Phân trang nối đuôi an toàn, không lặp dữ liệu, dừng ngay khi cursor null.
  - Tích hợp SQL Server Change Tracking (1s heartbeat) + NATS Trigger (`TransportManager`).
  - Gói 106 trích xuất chuẩn 7 trường, 4 trường tải trọng null theo đặc tả.

### I.C · Chiều NHẬN — Inbound — 🧑‍💻 Hiếu

- [x] **SV-1b** Bỏ yêu cầu khoá `payload` khi parse gói đến 🟢 **xong 22/09 (PR #51)** — Hỗ trợ parse mảng dòng dữ liệu trần hoặc key `data`, không bắt buộc phong bì 7 khoá.
- [x] **SV-2b** Bỏ phân nhánh xử lý theo Version *(chiều nhận)* 🟢 **đã đúng** — 0 chỗ rẽ nhánh.
- [x] **SV-3b** Đọc `partnerCode` từ trong dữ liệu nhận về 🟢 **xong 22/09 (PR #51)** — Đọc từ header hoặc dòng đầu tiên của dữ liệu.
- [ ] **SV-7** Rà soát cắt cụt chuỗi dài (giới hạn 4000 ký tự).
- [ ] **SV-8b** Ghi log 2 bước cha–con *(chiều nhận — chặn bởi BE-5)*.
- [x] **SV-11** Tái cấu trúc luồng nhận (Inbound) 🟢 **xong 22/09 (PR #51)** — `DataInboundService.Parse.cs` chuẩn hóa, bỏ phụ thuộc `ShareDataTable`.

---

## I.A · DÙNG CHUNG CẢ HAI CHIỀU — 🤝 Đạt + Hiếu

### SV-4. Cấu hình vai trò instance + mã định danh của mình 🔑
- `appsettings.json` bổ sung:
  - `ShareData:SelfPartnerCode`: mã đối tác của chính mình khi gửi đi (ví dụ `A101`).
  - `ShareData:Role`: `SendOnly` | `ReceiveOnly` | **không khai báo = Both** (vừa gửi vừa nhận).
- Production vẫn chạy 1 service duy nhất 2 chiều; cấu hình phục vụ kiểm thử và phân tách tải.

### SV-5. Kịch bản test đa đối tác bằng clone service
- 4 instance `SendOnly` đóng vai A101–A104, mỗi bản khai bộ gói riêng + **1 instance `ReceiveOnly`** nhận cả 4.
- Dùng chung 1 CSDL (không clone DB). Tiêu chí đạt: instance nhận parse đúng và ghi đủ dữ liệu cả 4 đối tác.

### SV-6. Đo tải & rủi ro nghẽn CSDL
- Đo CPU / RAM / lock / contention trên `DEV_ITS10` khi nhiều instance cùng query gói 101.
- Nếu ảnh hưởng DB chính: tách CSDL phụ chuyên nhận `DEV_ITS10_Inbound` (`ShareDataWorkerExtensions.InboundConnectionKey`).

### SV-9. Xác thực đối tác bằng key / token (Giai đoạn sau)
- Xác thực đối tác từ thông tin đăng nhập/token thay vì tin trường `partnerCode` nằm trong dữ liệu nhằm chống giả mạo.

---

## I.B · CHIỀU GỬI — OUTBOUND — 🧑‍💻 Đạt

### SV-1a. Bỏ vỏ `httpPayload` 7 khoá khi gửi đi 🟢 *xong 18/09*
- Bỏ lớp vỏ bọc ngoài `partnerCode, datatypeId, packetVersion, serialNbr, pduType, format, rawContent`.
- Chỉ gửi nội dung mảng bản ghi JSON thuần hoặc cấu trúc `{ "header": {...}, "data": [...] }` theo đúng yêu cầu đối tác.

### SV-2a. Bỏ phân nhánh xử lý theo Version *(chiều gửi)* 🟢
- Tuyệt đối không rẽ nhánh logic theo phiên bản gói tin. `PacketVersion` chỉ dùng để ghi nhận log vận hành.

### SV-3a. Đặt `partnerCode` vào trong dữ liệu gửi đi 🟢
- M1 giải `$meta: PartnerCode` bằng `ctx.Partner?.Code`.
- Khi gửi 2 chiều nội bộ cần lưu ý: bên gửi ghi mã đối tác nhận, bên nhận đối chiếu mã đối tác gửi.

### SV-8a. Ghi log 2 bước cha–con *(chiều gửi)*
- 1 phiên gửi = 1 log cha + 2 log con: Step 1 Trích xuất CSDL $\rightarrow$ Step 2 Xử lý ánh xạ & Gửi đi.
- Chờ **BE-5** bổ sung 2 cột `ParentId` và `StepNo` vào `ShareDataActivityLog`.

### SV-10. Rà soát pipeline Outbound sau refactor 🟢
- Tầng gửi (`DataOutboundRestSender` / `DataOutboundFileSender`) làm thuần nhiệm vụ vận chuyển.
- Lỗi mapping ngắt ngay tại bước 2, ghi log lỗi và huỷ kết xuất.

### SV-12. Cờ "Chỉ gửi khi có dữ liệu mới" & Cơ chế Gửi nối đuôi Checkpoint 🟢 *hoàn tất 22/09*

#### 1. Kiến trúc Bảng Checkpoint độc lập (`ShareDataCheckpoint`)
- Tách rời hoàn toàn mốc cursor khỏi bảng `ShareDataSubscription` để tránh cạnh tranh lease.
- Cấu trúc bảng `ShareDataCheckpoint`:
  - `PartnerCode` (`string?`, `IsNullable = true`): Mã đối tác nhận.
  - `PacketCode` (`string?`, `IsNullable = true`): Mã gói tin chia sẻ.
  - `LastTime` (`DateTime?`, `IsNullable = true`): Mốc thời gian dữ liệu trích xuất thành công gần nhất.
  - `LastKey` (`string?`, `IsNullable = true`): Khóa dòng cuối của trang gần nhất (chống kẹt khi trùng mốc thời gian).
  - `LastVersion` (`long?`): Phiên bản Change Tracking gần nhất đã xử lý.
  - `CreateTime`, `UpdateTime`: Dấu vết thời gian hệ thống.
  - Index độc nhất: `UQ_ShareDataCheckpoint_Partner_Packet` trên `(PartnerCode, PacketCode)`.

#### 2. Cập nhật Checkpoint đơn điệu qua SqlSugar ORM
- Tuyệt đối không dùng raw SQL chuỗi cho update Checkpoint.
- Sử dụng method `UpdateCheckpointMonotonic` thông qua `db.Updateable<ShareDataCheckpoint>()`:
  - Điều kiện cập nhật: `LastTime < newTime OR (LastTime = newTime AND (LastKey IS NULL OR LastKey < newKey))`.
  - Đảm bảo cursor luôn tiến lên, tuyệt đối không bị tụt lùi hay ghi đè mốc cũ.

#### 3. Vòng lặp phân trang an toàn (Paging có phanh)
- Hằng số mặc định cấu hình trực tiếp trong `DataOutboundService`:
  - `DefaultPageSize = 50`: Số bản ghi tối đa mỗi trang.
  - `DefaultMaxPagesPerRun = 20`: Số trang tối đa gửi trong 1 lượt chạy lease.
  - `DefaultLeaseBudgetPercent = 50`: Tối đa 50% thời lượng lease dành cho vòng lặp trang.
- **Rào chắn cursor null (Monotonic Guard):** Nếu dữ liệu thô không rỗng nhưng trích xuất ra cursor null (không xác định được `__watermark` hoặc `__rowid`), vòng lặp ngắt ngay lập tức (`break`), bảo toàn `currentLastKey` hiện tại, không lặp lại 20 lần gây gửi trùng dữ liệu.

#### 4. Gói 106 (WIM) — Chuẩn hoá 7 trường trích xuất & 4 trường tải trọng
- Nguồn dữ liệu: bảng `TmsTrafficData`, watermark = `COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime)`.
- Trích xuất 7 trường có sẵn: `detectTime`, `lane`, `locationCode`, `speed`, `height`, `width`, `length`.
- 4 trường tải trọng (`grossWeight`, `axleWeights`, `axleCount`, `isOverweight`): Hệ thống chưa có bảng/cột WIM nên giữ nguyên giá trị `null` qua mapping hiện hữu, không suy diễn sai lệch.
- Nếu mapping profile cấu hình trường tải trọng `$extend.required: true`, trang kết xuất bị huỷ, ghi nhận log `Failed` và cảnh báo `ESH-1202`.

#### 5. Ngữ nghĩa `ShareDataSubscription.SerialNbr`
- Header HTTP, tên file kết xuất và Activity Log sử dụng giá trị `sub.SerialNbr` hiện tại trước khi tăng.
- Sau khi commit trang thành công trong CSDL, `SerialNbr` được tăng `+1` (`ISNULL(SerialNbr, 0) + 1`).
- Do đó, giá trị `SerialNbr` trong DB mang ngữ nghĩa *"serial của trang/lần kết xuất kế tiếp"*. Chuỗi số luôn liên tục, đơn điệu, không bị hụt hay trùng lặp.

#### 6. Cơ chế Kích hoạt tức thời (Event-Driven) qua Change Tracking + NATS
- `ChangeTrackingDbInitializer`: Tự động kiểm tra và kích hoạt Change Tracking trên CSDL SQL Server và các bảng nguồn khi Worker khởi động (nếu user có quyền DDL).
- `ChangeTrackingWatcherWorker`: Chạy nền với heartbeat 1 giây, đọc `CHANGE_TRACKING_CURRENT_VERSION()`. Khi phát hiện dữ liệu bảng nguồn thay đổi, phát sự kiện NATS `its.sharedata.trigger.{PacketCode}` qua `TransportManager`.
- `DataOutboundNatsWorker`: Lắng nghe subject `its.sharedata.trigger.*`, ngay lập tức kích hoạt luồng xuất bản dữ liệu cho các Subscription bật cờ `SendOnNewData`, giảm độ trễ về cận 0 mà không cần chờ nhịp polling của lịch.

#### 7. Ghi nhận cảnh báo tinh gọn (Log 1 lần tại nơi cần thiết)
- Khi bản ghi nguồn thiếu cả `UpdateTime` và `CreateTime`, hệ thống ghi log warning 1 lần cho gói tin (`AlertSource.Packet`), thông báo số dòng phải dùng thời gian nghiệp vụ thay vì throttle phức tạp.
- Khi mapping trường bị thiếu/lỗi, ghi trực tiếp `WriteAlertAsync` 1 lần cho trang/gói kết xuất (đã loại bỏ hoàn toàn hàm tiết chế `LogAlertThrottled`).

---

## I.C · CHIỀU NHẬN — INBOUND — 🧑‍💻 Hiếu

### SV-1b. Bỏ yêu cầu khoá `payload` khi parse gói đến 🟢 *xong 22/09 (PR #51)*
- **Thực tế đã hoàn thành**: Gộp triển khai cùng đợt chuẩn hoá phễu lọc chiều nhận ở commit `54e81c26` (PR #51, HiếuNV).
- **Tầng WebAPI** (`ShareDataInboundController.cs`): Đọc trực tiếp `httpRequest.Body` thô lưu vào `RawContent`. Mọi thông tin định danh chuyển lên HTTP Header (`PartnerCode`, `PacketCode`, `SerialNbr`), không ép buộc phong bì JSON ở Controller.
- **Tầng Worker parse gói** (`DataInboundService.Parse.cs`):
  - Gỡ bỏ hoàn toàn việc bắt buộc `doc.RootElement.TryGetProperty("payload", ...)`.
  - **Có phễu lọc (`TargetShapeJson`)**: Vị trí mảng bản ghi do bộ khung quyết định linh hoạt qua `EachPath` (`$each` + `$as`) hoặc mảng ngầm (`IsRecordTemplateArray`). Không giả định bất kỳ tên khoá cố định nào (kể cả `payload`).
  - **Không có phễu lọc (Fallback)**: Hàm `TryFindFallbackArray` hỗ trợ đọc trực tiếp mảng JSON trần (`root.ValueKind == Array`) hoặc mảng bọc trong key `data` / `payload` (tương thích ngược).

### SV-2b. Bỏ phân nhánh xử lý theo Version *(chiều nhận)* 🟢
- Parse trực tiếp dữ liệu theo schema định nghĩa, sai cấu trúc thì ghi lỗi, không rẽ nhánh theo version.

### SV-3b. Đọc `partnerCode` từ trong dữ liệu nhận về 🟢 *xong 22/09 (PR #51)*
- Lấy `partnerCode` từ header hoặc từ dòng đầu tiên của mảng dữ liệu nhận về để nhận diện đối tác gửi.

### SV-7. Rà soát cắt cụt chuỗi dài (giới hạn 4000 ký tự)
- Kiểm tra các tham số chuỗi trong câu lệnh SQL động và kiểu dữ liệu ở bảng đích để tránh mất dữ liệu JSON âm thầm.

### SV-8b. Ghi log 2 bước cha–con *(chiều nhận)*
- Ghi nhận 2 bước: Tiếp nhận payload $\rightarrow$ Ánh xạ & Ghi CSDL. Chờ BE-5 hỗ trợ cấu trúc cha-con.

### SV-11. Tái cấu trúc luồng nhận (Inbound) 🟢 *xong 22/09 (PR #51)*
- Chuẩn hoá cấu trúc thư mục xử lý: Tiếp nhận $\rightarrow$ Parse & Mapping $\rightarrow$ Ghi CSDL (`DataInboundService.Parse.cs`, `DataInboundService.WriteSql.cs`).
- Loại bỏ hoàn toàn sự phụ thuộc vào `ShareDataTable`.

---

# PHẦN II — BACKEND WebAPI: `Module.ShareData` (`BE-*`)

- [x] **BE-1** **CRUD Gói tin**: Bổ sung Commands/Queries/Validators cho `ShareDataPacket`. Chặn Sửa/Xoá khi gói tin có đăng ký đang `Active`.
- [x] **BE-2** **API đối tác trả đủ Mã + Tên**: `PartnerOutput` cung cấp đầy đủ `code` và `name`.
- [x] **BE-3** **Danh mục "Trường Meta hệ thống"**: Cung cấp danh mục key meta (`meta.now`, `request_id`, `partner_code`...).
- [x] **BE-4** **Seed danh mục gói tin 101–111**: Rà soát và cung cấp script `.sql` danh mục gói tin và `shareData_type`.
- [ ] **BE-5** **Log 2 bước cha–con**: Thêm 2 cột `ParentId` và `StepNo` vào thực thể `ShareDataActivityLog` và API truy vấn cây cha–con.
- [x] **BE-6** **CodeSet: Default Value + Chiều**: Bổ sung `direction` cho cấu hình giá trị bộ mã.
- [ ] **BE-7** **Bảng mã lỗi hệ thống**: Danh mục Error Code chuẩn phục vụ ghi nhận sự cố.
- [x] **BE-8** **Endpoint lấy dữ liệu mẫu thật**: 🟢 **xong 22/09 (PR #51)**
  - Cung cấp API `GET api/v1/share-data/packet/{packetCode}/sample-data`.
  - DTO `ShareDataPacketSampleDataDto` (`SampleRows`, `TotalRows`, `ColumnNames`, `GeneratedAt`).
  - Query Handler `PacketQueryHandler.Handle(GetShareDataPacketSampleDataQuery)` lấy tối đa 50 bản ghi mẫu phục vụ màn hình Gửi thử / Test Send.
- [x] **BE-9** **Trường "Tệp dữ liệu" nghiệp vụ**: `ShareDataTable` đã bỏ, không dùng bảng vật lý.
- [x] **BE-10** **Rào Port khi sửa đối tác**: Validator chặn sửa cổng khi đối tác đang kết nối.
- [x] **BE-11** **Độ ưu tiên (Priority)**: Giữ trường dữ liệu, không xử lý hàng đợi ưu tiên.
- [x] **BE-12** **Kho câu truy vấn SQL tập trung (`ShareDataPacketSqlCatalogUtil`)**: 🟢 **xong 22/09 (PR #51)**
  - Nằm tại `Module.ShareData.Core.Utils.ShareDataPacketSqlCatalog`.
  - Định nghĩa tập trung SQL template, token `{TOP}`, `{ORDER_BY}`, tham số `@lastTime` cho cả 11 gói tin ESHARE (101–111).
  - Tích hợp ánh xạ bí danh gói tin (`CodeAliases`: `101_commonData` $\rightarrow$ `101`, `104_rfidData` $\rightarrow$ `105`, `106_wimData` $\rightarrow$ `106`,...).
  - Cung cấp 2 phương thức chuẩn:
    - `GetWorkerSql(code)`: Dành cho Worker trích xuất dữ liệu snapshot.
    - `GetSampleSql(code, top)`: Dành cho WebAPI lấy dữ liệu mẫu (mặc định 5 dòng, tối đa 50 dòng).
- [x] **BE-13** **Chuẩn hóa kiến trúc & Quy ước đặt tên (Naming Refactoring)**: 🟢 **xong 22/09 (PR #51)**
  - Loại bỏ tiền tố trùng lặp `ShareData` ở các Controller Handlers & Validators trong `Module.ShareData`:
    - `MappingCommandHandler`, `MappingQueryHandler`, `MappingValidator`
    - `PacketCommandHandler`, `PacketQueryHandler`, `PacketValidator`
    - `SubscriptionCommandHandler`, `SubscriptionQueryHandler`, `SubscriptionValidator`
    - `PartnerCommandHandler`, `PartnerQueryHandler`, `PartnerValidator`
    - `CodeSetCommandHandler`, `CodeSetQueryHandler`, `CodeSetValidator`
    - `InboundCommandHandler`, `InboundValidator`
    - `ActivityLogQueryHandler`, `AlertLogCommandHandler`, `AlertLogQueryHandler`
  - Chuẩn hóa tên các Service nội bộ: `ActivityLoggerService`, `CodeSetValueReaderService`, `MappingResolverService`, `ShapeReaderService`.

---

# PHẦN III — FRONTEND (`TA-ITS015-WEBVUE-V1.0`)

## A. Cấu hình Đối tác & Đăng ký chia sẻ
- [x] Hiện **Mã đối tác** cạnh trạng thái ở danh sách đối tác (`partnerList.vue`).
- [x] Thay thế toàn bộ `:title` HTML bằng `el-tooltip effect="dark"`.
- [x] Ẩn thẻ Cảnh báo và tinh giản khối `el-descriptions` trong `sharing/index.vue`.
- [x] Bỏ cột Định dạng, thu gọn cột Hành động trong `subscriptionTable.vue`.
- [x] Thay switch "Theo sự kiện" bằng checkbox "Gửi ngay khi có dữ liệu mới" (`editSubscription.vue`).
- [x] Chiều nhận (Inbound): Ẩn toàn bộ khối cấu hình lịch chạy.
- [x] Khóa ô Cổng (`Port`) khi ở chế độ Sửa đối tác (`editPartner.vue`).

## B. Cấu hình Gói tin — `dataSource/index.vue`
- [x] Bỏ cột Bí danh, Vai trò, Kiểu nối, Điều kiện nối; đổi cột Bảng dữ liệu thành "Tệp dữ liệu".
- [x] Tối ưu cuộn ngang và độ rộng cột bảng trường con; cố định cột STT bên trái.
- [x] Dựng UI thêm/sửa gói tin; khóa thao tác Sửa/Xóa khi gói tin đang có Subscription `Active`.

## C. Ánh xạ dữ liệu — `mapping/index.vue` & `editMapping.vue`
- [x] Bỏ bộ lọc Định dạng và phiên bản gói tin; chuẩn hoá i18n "Ánh xạ dữ liệu".
- [x] Nút "..." chuyển thành icon `ele-Setting` kèm tooltip; cấu hình lá mở dạng modal chồng độc lập.
- [x] Thêm badge trạng thái CodeSet / Format; nút "Tự động ánh xạ" tách biệt.
- [x] Gom nhóm "Trường Meta hệ thống" trong dropdown chọn trường.

## D. Lịch sử chia sẻ — `history/index.vue`
- [x] Đổi nhãn: "Nhật ký cấu hình" và "Nhật ký truyền nhận".
- [x] Bộ lọc thời gian chuẩn hóa `datetimerange`; bỏ ô lọc "Nội dung".
- [x] Double-click dòng mở modal chi tiết `activityDetailDialog.vue` thay cho sidebar; sửa lỗi so sánh enum chuỗi sang số; dựng khung `el-steps` 2 bước cha-con.

## E. Tooltip đồng bộ toàn module
- [x] Toàn bộ tooltip chuyển sang component chuẩn `el-tooltip effect="dark"`.

---

# PHẦN IV — LUỒNG KIẾN TRÚC XỬ LÝ OUTBOUND & QUY CHUẨN ÁNH XẠ

## 1. Luồng chuẩn 3 bước tổng quát

```
┌─ A · CHỌN VIỆC (Selection & Lease) ───────────────────────────────────┐
│  Worker thức dậy (5s polling hoặc NATS event trigger)                 │
│    → Quét subscription hợp lệ (Outbound · Partner Active · Đến hạn)   │
│    → Claim lease (NextTimeRun = leaseExpiry)                          │
│    → Resolve Packet & Mapping profile (Thiếu → dừng, ghi ESH-1304)    │
│    → Khởi tạo DataOutboundContext xuyên suốt                          │
└───────────────────────────────────┬───────────────────────────────────┘
                                    ▼
┌─ B · BƯỚC 1: TRÍCH XUẤT (Extraction) ────────────────────────────────┐
│  DataOutboundExtractionProcess tra query handler theo PacketCode      │
│  Chạy SQL với cursor kép (__watermark, __rowid) & trần trang (50)     │
│  Lọc IsDelete IS NULL trên tất cả các nhánh                          │
│  Dữ liệu rỗng → dừng êm, không gửi                                    │
└───────────────────────────────────┬───────────────────────────────────┘
                                    ▼
┌─ C · BƯỚC 2: ÁNH XẠ (Mapping & Transformation) ──────────────────────┐
│  Nguồn duy nhất: ShareDataMapping.TargetShapeJson                     │
│  Cú pháp JSON hỏng → huỷ, ghi ESH-1206                                │
│  Xử lý từng trường theo thứ tự chuẩn:                                 │
│    Giá trị thô ($field) → CodeSet quy đổi → Giá trị mặc định →         │
│    Ép kiểu / Định dạng (dateFormat, numberFormat)                     │
│  Trường $extend.required rỗng → huỷ cả lô, ghi ESH-1202               │
│  Đóng gói JSON cuối (bỏ phong bì, không mã băm)                       │
└───────────────────────────────────┬───────────────────────────────────┘
                                    │ (Thất bại → dừng, không gửi)
                                    ▼
┌─ D · BƯỚC 3: XUẤT BẢN & COMMIT (Transport & Persistence) ────────────┐
│  Gửi HTTP REST tới đối tác (quyết định trạng thái lô)                 │
│  Ghi tệp lưu trữ local (chỉ bật ở môi trường Dev/Test/Debug)          │
│  HTTP thành công:                                                     │
│    1. Commit kết quả (OCC NextTimeRun == leaseExpiry, tăng SerialNbr) │
│    2. Update Checkpoint đơn điệu (UpdateCheckpointMonotonic)          │
│    3. Ghi ActivityLog thành công (try/catch riêng)                    │
│  Cuối cùng: Nhả lease đúng một lần trong khối finally                  │
└───────────────────────────────────────────────────────────────────────┘
```

## 2. Nguồn cấu hình duy nhất: `TargetShapeJson`

Toàn bộ cấu hình ánh xạ trường thuộc quyền điều khiển của **`ShareDataMapping.TargetShapeJson`**. Bảng `ShareDataPacketField` chỉ đóng vai trò danh mục thiết kế ban đầu.

### 6 khoá `$extend` chuẩn của phễu lọc
1. `codeSet`: Mã bộ mã quy đổi trong `ShareDataCodeSet`.
2. `defaultPartnerValue`: Giá trị mặc định trả về khi gửi dữ liệu nếu giá trị nguồn null hoặc không khớp bộ mã.
3. `defaultSourceValue`: Giá trị mặc định khi nhận dữ liệu.
4. `targetType`: Ép kiểu đích (`string`, `int`, `long`, `double`, `decimal`, `boolean`, `datetime`).
5. `dateFormat`: Định dạng ngày giờ đầu ra (ví dụ: `yyyy-MM-ddTHH:mm:ss`).
6. `numberFormat`: Định dạng làm tròn số (ví dụ: `0.00`, `N2`).

## 3. Quy chuẩn đối chiếu tên trường & Cảnh báo

- **Trường nguồn thừa / phễu lọc không gọi**: Bỏ qua im lặng, không đưa vào payload gửi đi.
- **Phễu lọc gọi / trường nguồn không có**: Giá trị ra `null` im lặng (không ghi spam log cảnh báo lặp lại).
- **Trường đánh dấu `$extend.required: true` bị null**: Lập tức huỷ toàn bộ trang kết xuất, ghi alert `ESH-1202` và log kết quả `Failed`.
- **Mã cảnh báo hệ thống chính (ESH Alert Codes)**:
  - `ESH-1201`: Không tìm thấy bộ mã CodeSet.
  - `ESH-1202`: Thiếu trường bắt buộc (`$extend.required`).
  - `ESH-1203`: Lỗi tính toán biểu thức.
  - `ESH-1206`: `TargetShapeJson` rỗng hoặc sai cú pháp JSON.
  - `ESH-1301`: Không tìm thấy cấu hình gói tin.
  - `ESH-1302`: Lỗi câu truy vấn trích xuất CSDL.
  - `ESH-1303`: Mất lease khi ghi nhận kết quả.
  - `ESH-1304`: Không tìm thấy cấu hình phễu lọc (`ShareDataMapping`).
  - `ESH-1402`: Gửi HTTP REST tới đối tác thất bại.

---

# PHẦN V — BẢO ĐẢM AN TOÀN & KIỂM THỬ (TESTING)

## 1. Các chốt chặn an toàn bắt buộc (Mandatory Safeguards)
1. **Strict Local Database for `dotnet test`**: Toàn bộ connection string dùng khi chạy test PHẢI trỏ về `local` (`localhost`, `127.0.0.1`, `(localdb)`, `.`). Nếu phát hiện IP remote (ví dụ `10.10.8.30`), HỦY test ngay lập tức.
2. **Không tự ý thực thi DDL/DML**: Mọi thay đổi schema CSDL chỉ xuất file `.sql` ra đĩa để quản trị viên review.
3. **Quản lý tài liệu tập trung**: Transcript hội thoại và file `Sharedata_MasterPlan.md` này là 2 nguồn thông tin chuẩn xác duy nhất của dự án.

## 2. Lệnh biên dịch & Kiểm thử
- **Biên dịch Worker Service**:
  ```powershell
  dotnet build src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj
  ```
- **Biên dịch WebAPI**:
  ```powershell
  dotnet build src/TAC_WebAPI/TAC_WebAPI.csproj
  ```
- **Chạy toàn bộ Unit Tests của ShareData**:
  ```powershell
  dotnet test tests/test.csproj --filter "FullyQualifiedName~ShareData"
  ```
  *(Hiện tại toàn bộ 185 unit tests bao gồm `DataOutboundServiceTests` và `ShareDataEventChangeTrackingTests` đã đạt 100% PASS, 1 test skip theo thiết kế)*.
- **Biên dịch Frontend**:
  ```powershell
  cd TA-ITS015-WEBVUE-V1.0 && npm run build
  ```
