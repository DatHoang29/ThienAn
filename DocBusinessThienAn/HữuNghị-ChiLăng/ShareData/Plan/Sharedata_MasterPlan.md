# ShareData (ESHARE) — Master Plan FE · BE · Service

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT):** Tài liệu quy hoạch tổng thể duy nhất cho toàn bộ phân hệ **ShareData (ESHARE)** gồm Frontend, Backend WebAPI và Service Worker.
> Được hợp nhất từ các tài liệu phân tích, kế hoạch kiểm thử, cơ chế gửi nối đuôi LastSend và kích hoạt sự kiện Change Tracking + NATS.
> 📌 **Cập nhật lần cuối: 27/09/2026** — đồng bộ tài liệu với code thật sau đợt rà soát đối chiếu 31 code change của nhánh `feat/20260922-sharedata-service`: sửa 13 điểm lệch (3 điểm tự mâu thuẫn trong chính tài liệu: phễu lọc `SourceAllowList` đã bãi bỏ nhưng §4 còn mô tả, trần trang 50 vs 100, retention 2 ngày vs 1 ngày; 10 điểm lệch tên hàm/field/file test sau các đợt đổi tên), thống nhất thuật ngữ `lease` → `lock` theo rule 7, bổ sung edge case #11 (mất cơ chế vô hiệu hoá cache khi câu CHANGETABLE hỏng). Trước đó: 26/09/2026 — đồng bộ tên `Checkpoint` → `LastSend` xuyên suốt tài liệu (khớp code thật sau khi áp dụng `sharedata-doi-ten-checkpoint-thanh-lastsend-trong-dataoutboundservice-prompt.md`); trước đó: 25/09/2026 — đã gộp toàn bộ nội dung còn giá trị từ 3 báo cáo review (`Sharedata_Review_LuongNoiDuoi_20260923.md`, `Sharedata_Review_GuiKhiCoDuLieuMoi_20260923.md`, `Sharedata_Review_DoiChieuThucTe_20260925.md`) trực tiếp vào tài liệu này (chủ yếu ở SV-12 §9); 3 file review đã được xoá sau khi gộp, tài liệu này là SSOT duy nhất.

### Chú giải ký hiệu

| Ký hiệu | Ý nghĩa |
| --- | --- |
| ✅ | Đạt — khớp / đúng / đã hoàn tất |
| ⚠️ | Cần lưu ý — có vấn đề, nhưng chưa phải dừng lại để sửa; ghi nhận rồi xử lý sau |
| ❌ | Không đạt — không khớp / không đúng / không áp dụng được |
| 🔴 | Rủi ro nghiêm trọng — phải xử lý trước khi đi tiếp |
| ⛔ | Cấm tuyệt đối |
| 📌 | Ghi chú bối cảnh — nguồn dữ liệu, ngày đo, thuật ngữ |

> Ký hiệu chỉ nói **mức độ**; trục đánh giá do tiêu đề cột của từng bảng nói rõ.

> 📎 **Tài liệu anh em:** [`Sharedata_Review_TongThe_20260927.md`](./Sharedata_Review_TongThe_20260927.md) — báo cáo rà soát tại một thời điểm, đối chiếu biên bản họp 21/09 với mã nguồn thật, trả lời 4 câu: đặc tả yêu cầu gì · code làm tới đâu · **chưa làm gì** · edge case nào. Phụ lục B của nó giữ bảng **phương án bị bác và vì sao** — thứ không nằm trong tài liệu này.
> Phân vai: tài liệu này là **quy hoạch** (làm gì, thiết kế thế nào); tệp review là **đối chiếu** (đã làm tới đâu so với đặc tả). Theo tiền lệ 25/09, khi các phát hiện của một tệp review đã được hấp thụ hết vào đây thì tệp review được gộp rồi xoá.

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

### I.A · Dùng chung cả hai chiều — 🤝 Đạt + Hiếu · ⚠️ **Chưa làm**

> 📌 Cả nhóm này phụ thuộc luồng 1-1 chạy ổn trước. Đó là **lý do chưa làm**, không phải một trạng thái riêng — rule 19.15 chỉ có *đã làm* hoặc *chưa làm*.

- [ ] **SV-4** Cấu hình vai trò instance + mã định danh của mình (`ShareData:Role`, `ShareData:SelfPartnerCode`) 🔑
- [ ] **SV-5** Kịch bản test đa đối tác bằng clone service — phụ thuộc SV-1a + SV-1b
- [ ] **SV-6** Đo tải & rủi ro nghẽn CSDL — phụ thuộc luồng 1-1 chạy ổn
- [ ] **SV-9** Xác thực đối tác bằng key / token

### I.B · Chiều GỬI — Outbound — 🧑‍💻 Đạt

- [x] **SV-1a** Bỏ vỏ `httpPayload` 7 khoá khi gửi đi ✅ **xong 18/09**
- [x] **SV-2a** Bỏ phân nhánh xử lý theo Version *(chiều gửi)* ✅ **đã đúng** — `PacketVersion` chỉ xuất hiện trong log, **0 chỗ rẽ nhánh**.
- [x] **SV-3a** Đặt `partnerCode` vào trong dữ liệu gửi đi ✅ **xong 18/09** — M1 giải `$meta: PartnerCode` bằng `ctx.Partner.Code`.
- [ ] **SV-8a** Ghi log 2 bước cha–con *(chiều gửi)* 🔴 **chặn bởi BE-5** (`ParentId`/`StepNo` trên `ShareDataActivityLog`).
- [x] **SV-10** Rà soát pipeline Outbound sau refactor ✅ **xong 18/09** — Tầng gửi không logic nghiệp vụ, lỗi mapping dừng bước 2, `DataOutboundContext` xuyên suốt.
- [x] **SV-12** Cờ *"chỉ gửi khi có dữ liệu mới"* & Cơ chế Gửi nối đuôi LastSend ✅ **hoàn tất 22/09**:
  - Kiến trúc LastSend độc lập `ShareDataLastSend` theo từng cặp `(PartnerCode, PacketCode)`.
  - Phân trang nối đuôi an toàn, không lặp dữ liệu, dừng ngay khi cursor null.
  - Tích hợp SQL Server Change Tracking (1s heartbeat) + NATS Trigger (`TransportManager`).
  - Gói 106 trích xuất chuẩn 7 trường, phễu lọc nguồn allow-list (mặc định rỗng chặn gửi sai), 4 trường tải trọng null theo đặc tả.
  - > 📌 **Lưu ý đồng bộ Transcript & Plan (Việc B - Event-Driven):** Trong transcript cuộc họp ngày 21/09/2026 ghi nhận việc B chưa làm trong đợt này; tuy nhiên **chỉ đạo kiến trúc và MasterPlan là PHẢI LÀM LUÔN**, và toàn bộ cơ chế Event-Driven (Change Tracking + NATS + Lock guard + Read-only Initializer) đã được hoàn tất và kiểm thử 100% PASS.

### I.C · Chiều NHẬN — Inbound — 🧑‍💻 Hiếu

- [x] **SV-1b** Bỏ yêu cầu khoá `payload` khi parse gói đến ✅ **xong 22/09 (PR #51)** — Hỗ trợ parse mảng dòng dữ liệu trần hoặc key `data`, không bắt buộc phong bì 7 khoá.
- [x] **SV-2b** Bỏ phân nhánh xử lý theo Version *(chiều nhận)* ✅ **đã đúng** — 0 chỗ rẽ nhánh.
- [x] **SV-3b** Đọc `partnerCode` từ trong dữ liệu nhận về ✅ **xong 22/09 (PR #51)** — Đọc từ header hoặc dòng đầu tiên của dữ liệu.
- [ ] **SV-7** Rà soát cắt cụt chuỗi dài (giới hạn 4000 ký tự).
- [ ] **SV-8b** Ghi log 2 bước cha–con *(chiều nhận — chặn bởi BE-5)*.
- [x] **SV-11** Tái cấu trúc luồng nhận (Inbound) ✅ **xong 22/09 (PR #51)** — `DataInboundService.Parse.cs` chuẩn hóa, bỏ phụ thuộc `ShareDataTable`.

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

### SV-9. Xác thực đối tác bằng key / token — ⚠️ Chưa làm
- Xác thực đối tác từ thông tin đăng nhập/token thay vì tin trường `partnerCode` nằm trong dữ liệu nhằm chống giả mạo.

---

## I.B · CHIỀU GỬI — OUTBOUND — 🧑‍💻 Đạt

### SV-1a. Bỏ vỏ `httpPayload` 7 khoá khi gửi đi ✅ *xong 18/09*
- Bỏ lớp vỏ bọc ngoài `partnerCode, datatypeId, packetVersion, serialNbr, pduType, format, rawContent`.
- Chỉ gửi nội dung mảng bản ghi JSON thuần hoặc cấu trúc `{ "header": {...}, "data": [...] }` theo đúng yêu cầu đối tác.

### SV-2a. Bỏ phân nhánh xử lý theo Version *(chiều gửi)* ✅
- Tuyệt đối không rẽ nhánh logic theo phiên bản gói tin. `PacketVersion` chỉ dùng để ghi nhận log vận hành.

### SV-3a. Đặt `partnerCode` vào trong dữ liệu gửi đi ✅
- M1 giải `$meta: PartnerCode` bằng `ctx.Partner?.Code`.
- Khi gửi 2 chiều nội bộ cần lưu ý: bên gửi ghi mã đối tác nhận, bên nhận đối chiếu mã đối tác gửi.

### SV-8a. Ghi log 2 bước cha–con *(chiều gửi)*
- 1 phiên gửi = 1 log cha + 2 log con: Step 1 Trích xuất CSDL $\rightarrow$ Step 2 Xử lý ánh xạ & Gửi đi.
- Chờ **BE-5** bổ sung 2 cột `ParentId` và `StepNo` vào `ShareDataActivityLog`.

### SV-10. Rà soát pipeline Outbound sau refactor ✅
- Tầng gửi (`DataOutboundRestSender` / `DataOutboundFileSender`) làm thuần nhiệm vụ vận chuyển.
- Lỗi mapping ngắt ngay tại bước 2, ghi log lỗi và huỷ kết xuất.

### SV-12. Cờ "Chỉ gửi khi có dữ liệu mới" & Cơ chế Gửi nối đuôi LastSend ✅ *hoàn tất 22/09*

#### 1. Kiến trúc Bảng LastSend độc lập (`ShareDataLastSend`)
- Tách rời hoàn toàn mốc cursor khỏi bảng `ShareDataSubscription` để tránh cạnh tranh lock.
- Cấu trúc bảng `ShareDataLastSend`:
  - `PartnerCode` (`string?`, `IsNullable = true`): Mã đối tác nhận.
  - `PacketCode` (`string?`, `IsNullable = true`): Mã gói tin chia sẻ.
  - `LastTime` (`DateTime?`, `IsNullable = true`): Mốc thời gian dữ liệu trích xuất thành công gần nhất.
  - `LastKey` (`string?`, `IsNullable = true`): Khóa dòng cuối của trang gần nhất (chống kẹt khi trùng mốc thời gian).
  - *(Đã loại bỏ `LastVersion` ngày 26/09/2026: Change Tracking được theo dõi tập trung ở cấp worker, không lưu trên từng gói tin/đối tác nữa — xem mục 6 và 6b về nơi lưu mốc đó).*
  - `CreateTime`, `UpdateTime`: Dấu vết thời gian hệ thống.
  - Index độc nhất: `index_{table}_Partner_Packet` trên `(PartnerCode, PacketCode)` (khai bằng `[SugarIndex(..., isUnique: true)]` trong entity, SqlSugar tự thay `{table}` bằng tên bảng).

#### 2. Cập nhật LastSend đơn điệu qua SqlSugar ORM
- Tuyệt đối không dùng raw SQL chuỗi cho update LastSend.
- Sử dụng method `UpdateLastSend` thông qua `db.Updateable<ShareDataLastSend>()`:
  - Điều kiện cập nhật: `LastTime < newTime OR (LastTime = newTime AND (LastKey IS NULL OR LastKey < newKey))`.
  - Đảm bảo cursor luôn tiến lên (đơn điệu), tuyệt đối không bị tụt lùi hay ghi đè mốc cũ.

#### 2b. Khởi tạo LastSend lần đầu cho Đăng ký mới (`GetLastSend`)
- Khi đăng ký chưa từng chạy (chưa có `lastTimeRun`), worker cắm mốc lùi đúng một chu kỳ an toàn để gửi ngay dữ liệu phát sinh gần nhất mà không nạp toàn bộ lịch sử CSDL gây nghẽn:
```csharp
if (lastTimeRun.HasValue)
{
    initialTime = lastTimeRun.Value;
}
else
{
    var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
    var safeInterval = intervalSeconds > 0 ? intervalSeconds : DataOutboundScheduler.DefaultIntervalSeconds;
    initialTime = dbNow.AddSeconds(-safeInterval);
}
```
- **Bắt buộc dùng `var dbNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");`**: Bảo đảm 100% đồng nhất hệ quy chiếu thời gian với SQL Server. Tránh lỗi clock skew giữa máy chủ ứng dụng và CSDL khiến mốc `initialTime` vượt trước thời gian bản ghi trong CSDL, gây rớt bản ghi ở lượt quét đầu tiên.
- **Mở rộng ra cả luồng lock (27/09/2026):** nguyên tắc trên nay áp cho **toàn bộ** mốc thời gian quyết định lock, không riêng `GetLastSend`. `DataOutboundService.GetDbNow(db)` là nơi duy nhất đọc đồng hồ CSDL, được gọi ở 3 điểm: 2 overload `ProcessSubscriptions` (mốc `now` để xét `NextTimeRun <= now` và `DebounceSec`) và `ReleaseLock` (tính `NextTimeRun` kế tiếp). Lý do: 3 mốc này đều **so sánh hoặc ghi vào cột CSDL**, nên nếu lấy từ đồng hồ máy ứng dụng thì khi chạy nhiều instance worker trên nhiều máy, mỗi máy lệch đồng hồ một chút là cửa sổ debounce và cửa sổ lock lệch theo đúng mức lệch đó. ⛔ Không dùng `DateTime.Now` cho các mốc này nữa.
- **Phòng vệ `safeInterval`**: `var safeInterval = intervalSeconds > 0 ? intervalSeconds : DataOutboundScheduler.DefaultIntervalSeconds;` bảo đảm luôn có chu kỳ hợp lệ kể cả khi `IntervalSeconds` cấu hình <= 0.

#### 3. Vòng lặp phân trang an toàn (Paging có phanh)
- Hằng số mặc định cấu hình trực tiếp trong `DataOutboundService`:
  - `DefaultPageSize = 100`: Số bản ghi tối đa mỗi trang (chuẩn hóa nâng lên 100 ngày 25/09/2026).
  - `DefaultMaxPagesPerRun = 20`: Số trang tối đa gửi trong 1 lượt chạy lock (tối đa 2.000 bản ghi/lần chạy).
  - `DefaultLockBudgetPercent = 50`: Tối đa 50% thời lượng lock dành cho vòng lặp trang.
- **Rào chắn cursor null (Monotonic Guard):** Nếu dữ liệu thô không rỗng nhưng trích xuất ra cursor null (không xác định được `__lastTime` hoặc `__rowid`), vòng lặp ngắt ngay lập tức (`break`), bảo toàn `currentLastKey` hiện tại, không lặp lại 20 lần gây gửi trùng dữ liệu.
- 📌 **Hai tầng dùng 2 bí danh khác nhau cho cùng khái niệm mốc thời gian** (đo 27/09/2026): tầng Worker (`DataOutboundExtractionProcess`, 5 gói nối đuôi 103/104/106/107/109) dùng `AS __lastTime` và đọc lại đúng bằng `dict.TryGetValue("__lastTime")`; tầng Module (`ShareDataPacketSqlCatalogUtil`, 5 chỗ) vẫn dùng `AS __watermark`. Hiện **vô hại** vì catalog tầng Module chỉ phục vụ gói bản chụp — vốn không dùng cursor. ⚠️ Nhưng là cái bẫy: nếu sau này cho một gói **nối đuôi** lấy SQL từ catalog tầng Module, Worker sẽ không đọc được cursor (`__lastTime` không tồn tại) → cursor null → Monotonic Guard ngắt vòng lặp, dữ liệu chỉ còn chảy qua luồng quét định kỳ. ⛔ **Chủ dự án chốt 27/09/2026: KHÔNG thống nhất bí danh lúc này** — đây không phải việc còn mở, chỉ là điều kiện tiên quyết phải xử **trước** khi cho một gói nối đuôi lấy SQL từ catalog tầng Module.

#### 4. Gói 106 (WIM) — Chuẩn hoá 7 trường trích xuất & phễu lọc nguồn allow-list
- Nguồn dữ liệu: bảng `TmsTrafficData`, watermark = `COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime)`.
- 📌 **Lịch sử phễu lọc nguồn (đã bãi bỏ):** Từng có cấu hình `ShareData:Packet106:SourceAllowList` (mặc định rỗng `[]`) để chặn gửi khi chưa khai nguồn trạm cân. **Quyết định này đã bị bãi bỏ 25/09/2026 và code đã gỡ sạch** — rà toàn repo ngày 27/09/2026: `SourceAllowList` còn **0 chỗ** trong cả code lẫn cấu hình. Lý do bãi bỏ: 4 trường tải trọng vốn không nằm trong `SELECT` nên đã tự `null`, không cần lọc nguồn. Gói 106 gửi đủ 7 trường thật. Xem mục 8 bên dưới.
- Trích xuất 7 trường có sẵn: `detectTime`, `lane`, `locationCode`, `speed`, `height`, `width`, `length`.
- 4 trường tải trọng (`grossWeight`, `axleWeights`, `axleCount`, `isOverweight`): Hệ thống chưa có bảng/cột WIM nên giữ nguyên giá trị `null` qua mapping hiện hữu, không suy diễn sai lệch (chờ trạm cân hoạt động và có bảng/cột riêng).
- Nếu mapping profile cấu hình trường tải trọng `$extend.required: true`, trang kết xuất bị huỷ, ghi nhận log `Failed` và cảnh báo `ESH-1202`.

#### 5. Ngữ nghĩa `ShareDataSubscription.SerialNbr`
- Header HTTP, tên file kết xuất và Activity Log sử dụng giá trị `sub.SerialNbr` hiện tại trước khi tăng.
- Sau khi commit trang thành công trong CSDL, `SerialNbr` được tăng `+1` (`ISNULL(SerialNbr, 0) + 1`).
- Do đó, giá trị `SerialNbr` trong DB mang ngữ nghĩa *"serial của trang/lần kết xuất kế tiếp"*. Chuỗi số luôn liên tục, đơn điệu, không bị hụt hay trùng lặp.

#### 6. Cơ chế Kích hoạt tức thời (Event-Driven) qua Change Tracking + NATS

> 📌 **Cập nhật 25/09/2026 — chuẩn hoá lại tên class và mô tả subject NATS cho khớp code thật** (đối chiếu độc lập qua đọc trực tiếp code, xem §9): tên worker trước đây ghi `DataChangeWatcherWorker`/`DataNatsWorker` đã đổi thành `DataTrackerWorker`/`DataNatsConsumerWorker`; mô tả subject NATS có hậu tố `{PacketCode}` trước đây là **mô tả sai**, code thật luôn dùng 1 subject chung.

- > 📌 **Đồng bộ Transcript & Plan (Việc B - Event-Driven):** Trong transcript cuộc họp ngày 21/09/2026 (`16:56–17:03`), việc B (gửi ngay khi có dữ liệu mới qua Event-Driven Change Tracking + NATS) từng được ghi nhận là không làm ở đợt này và khóa checkbox trên FE. Tuy nhiên, theo quyết định chính thức của **MasterPlan là PHẢI LÀM LUÔN**, và toàn bộ cơ chế đã được triển khai hoàn tất đợt này theo chuẩn an toàn cao nhất.
- `DataTrackerWorker.CheckTracking`: Kiểm tra trạng thái qua DMV hệ thống (`sys.change_tracking_databases`, `sys.change_tracking_tables`). Khi Worker khởi động, hệ thống **luôn tự động kiểm tra và kích hoạt Change Tracking (DDL - Data Definition Language) ở mọi môi trường** (không còn phân biệt Dev/Staging/Production, đã bỏ hẳn cờ cấu hình `AutoEnableChangeTracking`). *(Ghi chú: Đây là quyết định có chủ đích của người dùng ngày 23/09/2026 nhằm tối ưu vận hành zero-touch, thay thế khuyến nghị mặc định ban đầu là chỉ cho phép DBA chạy script tay ở Staging/Production).*
- `DataOutboundService.ProcessSubscriptions` (Lock Protection): Khôi phục điều kiện claim lock `Where(s => s.NextTimeRun == null || s.NextTimeRun <= now)`. Khi một worker theo lịch đang gửi dở các trang, trigger NATS không thể cướp lock, loại bỏ hoàn toàn nguy cơ xung đột OCC và cảnh báo giả `ESH-1303`.
- `DataTrackerWorker`: Chạy nền với heartbeat 1 giây, đọc `CHANGE_TRACKING_CURRENT_VERSION()`. Tích hợp trực tiếp bảng ánh xạ bảng nguồn `RawTableToPacketMap` (`TmsTrafficData` ra cả 103 và 106) và bộ lọc `ResolvePackets` chỉ chặn gói `NotReady` và `Disabled` — **mọi gói hợp lệ còn lại, cả nối đuôi lẫn bản chụp, đều kích hoạt được bằng sự kiện** nếu đăng ký bật cờ `SendOnNewData`. Khi phát hiện dữ liệu bảng nguồn thay đổi, phát sự kiện NATS qua `TransportManager` vào **1 subject chung duy nhất** `ta.its.event.sharedata.newdata` (hằng số `DEFAULT_NATS_SUBJECT`, không hậu tố) — gói tin nhận diện qua field `PacketCode` trong payload (`{PacketCode, Type, Version, TriggeredAt}`), đây là thiết kế chủ đích.
- `DataNatsConsumerWorker`: Lắng nghe đúng subject chung đó, đọc `PacketCode` từ payload rồi gọi `DataOutboundService.ProcessSubscriptions(packetCode)`. Không có cơ chế debounce nào ở tầng worker này — chống dội thực hiện bằng cấu hình `DebounceSec` riêng của từng Subscription, kiểm tra trong `ProcessSubscriptions`.
- Cơ chế **Self-Healing khi `LastProcessedVersion` rơi ra ngoài cửa sổ hợp lệ của Change Tracking** (bổ sung 25/09/2026, retention rút ngắn 26/09/2026): SQL Server chỉ giữ dữ liệu Change Tracking 1 ngày (`CHANGE_RETENTION = 1 DAYS, AUTO_CLEANUP = ON` — rút ngắn từ 2 ngày ban đầu để giảm overhead lưu trữ phía CSDL; đổi lại, worker chỉ được phép ngừng chạy tối đa 1 ngày trước khi bị giảm cấp xuống luồng quét định kỳ thay vì trigger tức thời); nếu worker ngừng cập nhật mốc lâu hơn khoảng đó (ví dụ mất kết nối CSDL kéo dài mà tiến trình không restart), câu `CHANGETABLE` sẽ ném lỗi SQL lặp lại vô hạn mỗi giây. `DataTrackerWorker.QueryChangedTables` bọc `try/catch (Exception ex) when (IsVersionInvalid(ex))` quanh câu truy vấn đổi bảng — khi bắt được lỗi version không hợp lệ (mã SQL 22114/22115 hoặc message tương ứng, hàm `IsVersionInvalid`), tự động nhảy cóc `LastProcessedVersion = currentVersion` để hồi phục ngay chu kỳ kế tiếp, không cần restart service. Chi tiết & lý do đổi hướng so với đề xuất ban đầu (proactive vs reactive): `sharedata-tu-phuc-hoi-change-tracking-min-valid-version-prompt.md` (đã thực thi và xoá theo Auto-Cleanup). Hàm `GetMinValidVersion` viết sẵn cho hướng proactive không được chọn đã trở thành dead code và bị xoá ngày 26/09/2026 (`sharedata-don-dead-code-getminvalidversion-prompt.md`, đã thực thi và xoá) — xem `Prompt/README.md`. **Từ 27/09/2026** mỗi lần nhảy cóc còn ghi 1 dòng nhật ký hạ tầng mã `ESH-1601` (xem mục 6c) kèm khoảng version bị bỏ qua, để sau này lần ra được vì sao một quãng dữ liệu chỉ đi qua luồng quét định kỳ mà không có trigger.
- Toàn bộ vùng CT + NATS được bao phủ bởi bộ test trong `tests/ShareData/Services/DataTrackerWorkerTests.cs` (bao gồm 2 bài cho cơ chế Self-Healing reactive: `IsTrackingVersionInvalid_WhenGivenVariousExceptions_ClassifiesCorrectly_Test`, `ChangeTracking_WhenVersionInvalid_SelfHealsAndFastForwardsToCurrentVersion_Test`, và nhóm `NatsWorker_HandleTrigger_*` cho tầng NATS). 📌 Trước 27/09/2026 tài liệu ghi 2 file `DataChangeWorkerTests.cs` + `DataChangeWorkerNatsTests.cs` — cả 2 tên đều không còn đúng: bộ test đã gộp về **1 file duy nhất** `DataTrackerWorkerTests.cs`. Số lượng bài test cụ thể là số liệu tạm, dễ lạc hậu — chạy `dotnet test tests/test.csproj --filter "FullyQualifiedName~ShareData"` để xem số hiện tại thay vì tin số đếm cứng trong tài liệu.


#### 6b. Nơi lưu trạng thái của worker giám sát — bảng `ShareDataTrackVersion`

- **Vấn đề của bản cũ (đã xử lý):** `DataTrackerWorker` từng giữ 4 trạng thái **trong bộ nhớ** — mốc version đã xử lý, câu SQL truy vấn thay đổi đang dùng, mốc hẹn quét lại danh sách bảng, và chu kỳ quét lại (hằng số cứng 5 phút). Hệ quả: không tra được mốc Change Tracking từ ngoài, và restart là mất mốc nên phải nhảy thẳng tới mốc hiện tại (khoảng dữ liệu phát sinh lúc worker chết chỉ còn luồng quét định kỳ gửi bù).
- **Hướng đã chốt:** đưa cả 4 trạng thái xuống bảng `ShareDataTrackVersion` (**6 cột nghiệp vụ**: `MachineName`, `ProcessId`, `LastProcessedVersion`, `MissingTables`, `NextTableRefreshTime`, `RetryIntervalSeconds`), khoá độc nhất `(MachineName, ProcessId)` — mỗi lần chạy tiến trình đúng 1 dòng, cập nhật tại chỗ theo từng chu kỳ polling. Khi khởi động, worker **tiếp tục (resume)** mốc của lần chạy gần nhất trên cùng máy thay vì nhảy tới mốc hiện tại.
- ⛔ **Không thêm cột riêng cho 2 mốc thời gian của lượt chạy** — tận dụng đúng 2 cột của base `EntityTenant`, vì base đã gắn sẵn cờ SqlSugar làm đúng việc cần:
  - `CreateTime` = **lượt chạy này bắt đầu khi nào**. Base có `InsertServerTime` (INSERT tự ghi `GETDATE()` — đồng hồ CSDL) và bị loại khỏi UPDATE nên **bất biến**. Dòng chỉ được tạo đúng lúc lượt chạy bắt đầu, nên đây cùng là một sự kiện. Cũng là cột dùng để `ORDER BY ... DESC` khi tìm lần chạy gần nhất.
  - `UpdateTime` = **nhịp sống gần nhất**. Base có `UpdateServerTime` (UPDATE tự ghi `GETDATE()`), mà mỗi chu kỳ polling đều UPDATE 1 lần ⇒ nhịp sống có sẵn, không cần dòng code nào. Bản ghi mới có `UpdateTime = null` (base loại cột này khỏi INSERT) cho tới lần ghi đầu tiên — cách nhau vài milli giây, nên khi giám sát dùng `ISNULL(UpdateTime, CreateTime)`.
  - 📌 Cách này còn khép đúng nguyên tắc ở mục 2b: mốc ghi vào cột CSDL thì lấy từ đồng hồ CSDL, không lấy `DateTime.Now` của máy ứng dụng. Hai cột `StartedAt` / `LastHeartbeat` từng thêm ở bản thiết kế đầu đã bị bỏ ngày 27/09/2026 vì dư và lấy sai đồng hồ.
  - ⚠️ Đánh đổi khi viết test: `CreateTime` **không đặt được từ code** (bị `GETDATE()` ghi đè lúc INSERT, bị loại khỏi UPDATE) nên không dựng được nhiều dòng "lần chạy trước" với thời điểm tuỳ ý. Bài test resume vì thế chỉ dựng đúng 1 dòng.
- ⛔ **Không lưu chuỗi SQL xuống CSDL**: chỉ lưu danh sách bảng còn thiếu Change Tracking (`MissingTables`), câu SQL được dựng lại trong bộ nhớ từ danh sách đó. Nối chuỗi trên tối đa 15 bảng, không có I/O.
- ⛔ **Đây là bảng TRẠNG THÁI, không phải nhật ký sự kiện.** Lỗi hạ tầng thuộc mục 6c. Bản thiết kế append-only ban đầu đã bị bãi bỏ và dọn sạch code ngày 27/09/2026.
- **Trạng thái thi công: ✅ ĐÃ HOÀN TẤT 27/09/2026** (verify độc lập bằng cách đọc trực tiếp code + đọc schema thật trên CSDL test):
  - `DataTrackerWorker` **không còn trạng thái thay đổi theo thời gian nào trong bộ nhớ** — rà toàn bộ solution: `_cachedChangesSql`, `_nextTableRefreshTime`, `MissingTablesRetryInterval` và property `LastProcessedVersion` đều **0 kết quả**. Class chỉ còn hằng số (`PollingInterval`, nhóm `Sql*`, `RawTableToPacketMap`) và `_natsSubject`.
  - `PollChanges` bọc `try/finally`, ghi trạng thái **đúng 1 lần cho mọi đường ra kể cả nhánh ngoại lệ** — bắt buộc, vì phần vô hiệu hoá cache ở edge case #11 nằm trong khối `catch` rồi ném lại lỗi; không ghi ở `finally` thì thay đổi đó mất.
  - Câu SQL truy vấn thay đổi được dựng lại trong bộ nhớ từ `MissingTables` qua `BuildChangesSqlFromState`, không lưu chuỗi SQL xuống CSDL.
  - Schema trên CSDL test đã đọc trực tiếp: đúng 6 cột nghiệp vụ + nhóm cột base, ⛔ không còn `StartedAt` / `LastHeartbeat`. Cờ CodeFirst đã tắt lại `false`.
  - Kiểm thử: **146/146 bài test ShareData PASS**. Chi tiết việc đã làm ở `Prompt/sharedata-bang-state-trackversion-bo-4-field-ram-prompt.md` (file được giữ lại để review theo quy định).
- ⚠️ **Đánh đổi đã biết trước:** dòng trạng thái khoá theo `(MachineName, ProcessId)` nên 2 đối tượng worker trong **cùng một tiến trình** dùng chung một mốc cursor. Đây là chủ đích (tránh 2 tracker cùng bắn trigger cho một thay đổi), nhưng khiến bộ test phụ thuộc thứ tự chạy nếu không dọn dòng trạng thái giữa các bài.

#### 6c. Nhật ký sự cố hạ tầng Change Tracking — nhóm mã `ESH-16xx`

- **Ranh giới 3 tầng ghi nhận** (chốt 27/09/2026):
  - `ShareDataAlertLog` — **lỗi nghiệp vụ** mà quản trị viên cần can thiệp (`PacketNotFound`, `MappingNotFound`, `QueryFailed` của luồng gửi, `HttpSendFailed`...). Có cờ xác nhận đã xử lý.
  - `ShareDataActivityLog` — nhật ký truyền nhận nghiệp vụ, hiển thị trên giao diện. **Nay dùng thêm** cho sự cố hạ tầng Change Tracking, phân biệt bằng nhóm mã `ESH-16xx` đặt ở cột `Remark`.
  - `ILogger` — tranh chấp tài nguyên bình thường của cơ chế lock (`LockLost`), tuyệt đối không đẩy lên 2 tầng trên làm rác cảnh báo.
- **3 mã đã dùng** (`ShareDataAlertCode.Tracking`): `ESH-1601` mốc version không hợp lệ đã tự nhảy cóc · `ESH-1602` truy vấn `CHANGETABLE` lỗi vì lý do khác version, kèm câu SQL trong `DetailJson` để lần ra bảng gây lỗi · `ESH-1603` CSDL hoặc bảng nguồn chưa bật Change Tracking, polling tạm dừng.
- ⛔ **Không vào `ShareDataAlertLog`**: đây là lỗi hạ tầng, không phải lỗi nghiệp vụ — đưa vào đó là làm rác cảnh báo của người vận hành.
- **Không đổi schema**: không thêm cột, không thêm enum, không cần DBA, không cần sửa giao diện. Tận dụng cột `Remark` vốn đang bỏ trống với thực thể này.
- **Tự dọn quá hạn 7 ngày**: `ShareDataTransferLog.PurgeTrackingLogsAsync` chạy 1 lần mỗi lần worker khởi động, chỉ xoá dòng `Remark LIKE 'ESH-16%'` — 🔴 tuyệt đối không đụng nhật ký truyền nhận nghiệp vụ cùng khoảng thời gian (đã có test khoá lại lưới an toàn này).

#### 7. Ghi nhận cảnh báo tinh gọn (Log 1 lần tại nơi cần thiết)
- Khi bản ghi nguồn thiếu cả `UpdateTime` và `CreateTime`, hệ thống ghi log warning 1 lần cho gói tin (`AlertSource.Packet`), thông báo số dòng phải dùng thời gian nghiệp vụ thay vì throttle phức tạp.
- Khi mapping trường bị thiếu/lỗi, ghi trực tiếp `WriteAlertAsync` 1 lần cho trang/gói kết xuất (đã loại bỏ hoàn toàn hàm tiết chế `LogAlertThrottled`).
- **Tập trung hoá vào `ShareDataTransferLog` (26/09/2026):** `WriteFailureLogs`/`BuildFailExport` (đổi tên từ `WriteFailureLogsAsync`/`BuildFailExportAsync`) và `ResolvePduType` đã chuyển từ `DataOutboundService.cs` sang `ShareDataTransferLog` — đúng nguyên tắc "1 nơi duy nhất ghi log" đã chốt. `WriteActivityAsync` giờ tự fallback `PduType = pduType ?? ResolvePduType(sub)` khi caller không truyền, chiều Inbound (`packet.PduType`) không bị ảnh hưởng vì vẫn truyền tường minh. Chi tiết: `sharedata-di-chuyen-writefailurelogs-buildfailexport-resolvepdutype-prompt.md` (đã thực thi và xoá theo Auto-Cleanup, xem `Prompt/README.md`).

#### 8. Trạng thái các mục sau rà soát nghiệp vụ 23/09/2026

Nhật ký các việc đã xử lý kèm lý do quyết định: xem §9.5 bên dưới (gộp từ Phụ lục B báo cáo review 23/09, đã xoá).

| Việc | Loại | Trạng thái |
|---|---|---|
| Mở kích hoạt sự kiện cho gói bản chụp (điều kiện là cờ `SendOnNewData`), bổ sung bảng `TollTransactionIn` | [prompt](../Prompt/mo-trigger-cho-goi-ban-chup-prompt.md) | ✅ Đã xử lý 23/09/2026 |
| Hai bộ phân giải mã gói mâu thuẫn: catalog SQL coi `104_rfidData` là gói 105, `ResolvePacketPolicy` coi là 104 | [prompt](../Prompt/thong-nhat-phan-giai-ma-goi-va-mac-dinh-an-toan-prompt.md) | ✅ Đã xử lý 23/09/2026 |
| Gói 104 và 106 là gói chính thức; nhóm "dự kiến làm sau" đã bị bãi bỏ | Chủ dự án xác nhận 23/09/2026 | ✅ Đã làm rõ |
| Lượt chạy đầu của gói nối đuôi: cắm mốc lùi một chu kỳ rồi gửi ngay, bỏ thoát sớm | [prompt](../Prompt/gui-ngay-o-luot-chay-dau-goi-noi-duoi-prompt.md) | ✅ Đã xử lý 23/09/2026 |
| Chính sách gói tin giữ trong code; đổi mặc định sang `NotReady` + đối soát lúc khởi động (`CheckActivePackets`) | [prompt](../Prompt/thong-nhat-phan-giai-ma-goi-va-mac-dinh-an-toan-prompt.md) | ✅ Đã xử lý 23/09/2026 |
| Thống nhất phân giải mã gói ở `ResolveActivePacket` — gói RFID lưu `104_rfidData` nay khớp được tín hiệu `105_rfidData` | [prompt](../Prompt/thong-nhat-phan-giai-ma-goi-o-resolveactivepacket-prompt.md) | ✅ Đã xử lý 23/09/2026 |
| Hiển thị chính sách gói tin trên giao diện ở chế độ **chỉ đọc** | Frontend, tách riêng | ⚠️ Chưa làm |
| Gói **106** — quyết định lọc `Source` (allow-list rồi `AND 1=0`) bị bãi bỏ 25/09/2026: 4 trường tải trọng vốn không nằm trong `SELECT` nên đã tự `null`, không cần lọc gì. Gói quay về gửi đủ 7 trường thật | [prompt](../Prompt/goi-106-go-test-khoa-chan-prompt.md) (dọn 2 bài test còn khoá hành vi cũ) | ✅ Đã xử lý 25/09/2026, ⚠️ còn 2 bài test chờ gỡ |
| ✅ Gói **105** — `QueryPacket105` chuyển sang đọc trực tiếp `TollTransactionIn` kèm xử lý `NULLIF` chuỗi rỗng biển số và `exitTime` null | [prompt](../Prompt/goi-105-doc-tolltransactionin-prompt.md) | ✅ Đã xử lý 24/09/2026 |
| Gói **105** — tạo bản ghi `105_rfidData` trong `ShareDataPacket` (`104_rfidData` đã xoá 24/09) | Script: [`doc/sql/seed-goi-105-rfiddata.sql`](../doc/sql/seed-goi-105-rfiddata.sql) | ✅ Đã xử lý 24/09/2026 — bản ghi `OrderNo = 5` tạo lúc 10:38 |
| Gói bản chụp từng chưa có giới hạn số dòng — gói 105 từng đọc nguyên bảng `TollTransactionOut`, không `TOP` không `WHERE`. Giới hạn 100 dòng áp từ **tầng Service** (bọc truy vấn con), không đụng `ShareDataPacketSqlCatalog`; sau đó gói 105 đổi hẳn sang đọc `TollTransactionIn` kèm `TOP (@snapshotTop)` riêng, không qua catalog nữa | [prompt](../Prompt/gioi-han-so-dong-goi-ban-chup-prompt.md) + [prompt](../Prompt/goi-105-doc-tolltransactionin-prompt.md); 102/108 không giới hạn, 101 chờ kiểm chứng thực tế | ✅ Đã xử lý 24/09/2026 |
| Thiếu test cho 2 kịch bản sập hệ thống (nhận lại quyền xử lý sau khi worker chết; worker giám sát khởi động lại) | Việc kỹ thuật | ✅ Đã bổ sung 23/09/2026 |
| Test kịch bản mất kết nối CSDL giữa chừng | [prompt](../Prompt/test-mat-ket-noi-csdl-giua-chung-prompt.md) | ✅ Đã xử lý 23/09/2026 |
| Dọn dấu vết `104_rfidData` sau khi bản ghi bị xoá khỏi CSDL staging: giữ bí danh làm lưới chặn hồi quy, đổi tên 2 bài test | [prompt](../Prompt/don-dau-vet-104-rfiddata-prompt.md) | ✅ Đã xử lý 24/09/2026 |
| `DataTrackerWorker` lặp lỗi vô hạn khi `LastProcessedVersion` rơi ra ngoài cửa sổ hợp lệ `CHANGE_TRACKING_MIN_VALID_VERSION` (worker ngừng cập nhật mốc lâu hơn 1 ngày retention) | `sharedata-tu-phuc-hoi-change-tracking-min-valid-version-prompt.md` (đã xoá) | ✅ Đã xử lý 25/09/2026 — verify độc lập tại `DataTrackerWorker.cs` |
| `ProcessSubscriptions` (overload quét theo lịch; tài liệu từng ghi `ProcessScheduledSubscriptions`/`ProcessBatchSubscriptions` — code thật giữ tên `ProcessSubscriptions` với 2 overload) tạo mới `IServiceScopeFactory` scope + SqlSugar client riêng cho từng subscription trong vòng `foreach` thay vì dùng chung 1 scope cho cả batch | Không có file prompt tương ứng trên đĩa (đường dẫn được ghi trong báo cáo không tồn tại) | ✅ Đã xử lý 25/09/2026 — verify độc lập tại `DataOutboundService.cs:59-61,92` (1 scope duy nhất, `CopyNew()` trong loop) |

#### 9. Đặc tả nghiệp vụ đầy đủ — gộp từ 2 báo cáo review 23/09 (đã xoá sau khi gộp 25/09/2026)

##### 9.1. Hai trục quyết định độc lập: "Gửi cái gì" vs "Khi nào gửi"

| Trục ý nghĩa | Quyết định bởi | Giá trị |
|---|---|---|
| **GỬI CÁI GÌ** | `OutboundPolicy` của gói tin | `Snapshot` (101, 102, 105, 108: gửi lại toàn bộ) hoặc `AlwaysIncremental` (103, 104, 106, 107, 109: chỉ phần phát sinh sau mốc) |
| **KHI NÀO gửi** | Cờ `SendOnNewData` của **từng Subscription** + trigger NATS | Bật → gửi ngay khi Change Tracking phát hiện đổi; tắt → chỉ gửi theo lịch định kỳ |

Hai trục **độc lập hoàn toàn** — đổi trục này không kéo theo trục kia. `SendOnNewData` và `DebounceSec` là cột của `ShareDataSubscription`, không phải của `ShareDataPacket` — cơ chế kích hoạt sự kiện là **năng lực dùng chung cho cả 9 gói hợp lệ** (trừ 110 `NotReady`, 111 `Disabled`), quyền bật/tắt nằm ở từng đăng ký, không phải danh sách trắng cứng theo gói trong code.

##### 9.2. Vì sao chọn Change Tracking, không chọn CDC hay NATS thuần

| Tiêu chí | **Change Tracking (đang dùng)** | CDC | NATS thuần (tầng ứng dụng tự bắn) |
|---|---|---|---|
| Bắt được thay đổi từ nguồn nào | Mọi đường ghi vào bảng, kể cả script tay/import/hệ thống ngoài ghi thẳng CSDL | Mọi đường ghi (đọc nhật ký giao dịch) | Chỉ đường ghi đi qua code có gọi hàm phát tín hiệu — dễ sót |
| Nội dung trả về | Chỉ "có đổi + số hiệu version" | Đầy đủ ảnh trước/sau | Tuỳ code tự đóng gói |
| Chi phí hạ tầng | Nhẹ, không cần tiến trình phụ | Nặng hơn — cần tác vụ đọc nhật ký riêng | Không tốn CSDL nhưng tốn công sửa mọi nơi ghi dữ liệu |
| Phù hợp bài toán này | ✅ Đủ dùng — dữ liệu thật vẫn lấy qua truy vấn nối đuôi | Thừa — không cần ảnh trước/sau | Rủi ro cao — dễ sót nguồn ghi ngoài tầm kiểm soát |

##### 9.3. Đối chiếu đầy đủ 11 gói: đặc tả ↔ CSDL staging ↔ code

> 📌 Cột "Bản ghi staging" đo trên `mssql_staging` (`dev_its10` @ `10.10.8.30`) ngày 24/09/2026.

| Gói | Đặc tả (nguyên văn) | Policy trong code | Khớp? |
|---|---|---|---|
| 101 | `lấy all/DL được cập nhật` | `Snapshot` | ✅ |
| 102 | `lấy all /DL được cập nhật -> nats` | `Snapshot` | ✅ |
| 103 | `>= key` | `AlwaysIncremental` | ✅ |
| 104 | `>= key` | `AlwaysIncremental` | ✅ |
| 105 | `All/ update` | `Snapshot` | ✅ đọc trực tiếp `TollTransactionIn TOP 100` |
| 106 | `>= key` | `AlwaysIncremental` | ✅ gửi đủ 7 trường thật, 4 trường tải trọng `null` |
| 107 | `>= key / giá trị cũ update trạng thái` | `AlwaysIncremental` | ✅ |
| 108 | `lấy all /DL được cập nhật -> nats` | `Snapshot` | ✅ |
| 109 | `>= key` | `AlwaysIncremental` | ✅ |
| 110 | 3 trường `messageId`/`channel`/`deliveryState` — "chưa có, cần bảng notification/outbox" | `NotReady` → chặn | ⚠️ thiếu 3 trường, không phải thiếu tất cả — xem §9.6 |
| 111 | "Trao đổi với TT QLĐHGT tuyến (skip)" | `Disabled` → chặn | ✅ đặc tả chốt bỏ qua |

##### 9.4. Bảng nguồn → gói tin (15 bảng, đã lọc theo policy)

| Bảng nguồn | Gói được ánh xạ | Gói thực sự bắn tín hiệu (sau lọc NotReady/Disabled) |
|---|---|---|
| `TmsZoneStatus`, `TmsZone`, `TmsTrafficStatistic` | 101 | 101 |
| `CctvDevice` | 102 | 102 |
| `TmsTrafficData` | 103, 106 | 103, 106 |
| `TmsWeather` | 104 | 104 |
| `TollTransactionIn`, `TollTransactionOut` | 105, 109 | 105, 109 |
| `TmsVehicleRegistration` | 105 | 105 |
| `TmsIncident`, `TmsEventType` | 107, 110, 111 | **chỉ 107** (110/111 bị lọc) |
| `VmsCurrent` | 108 | 108 |
| `TmsEquipment` | 102, 103, 108 | 102, 103, 108 |
| `TollLane`, `TollStation` | 109 | 109 |

15/15 bảng nguồn đều sinh được tín hiệu — kể cả `TmsIncident`/`TmsEventType` (nuôi 2 gói bị chặn) vẫn còn gói 107 gánh tín hiệu qua.

##### 9.5. Vì sao gói bản chụp gửi lại toàn bộ mỗi chu kỳ — trích biên bản họp 21/09

> `09:59` — **Anh Sơn**: *"Một số dữ liệu là mình sẽ gửi lại hết. Nhưng một số dữ liệu... như cái bảng Incident... chỉ gửi tiếp mới mới thôi... Nhưng một số bảng là nó cần cập nhật hết tất cả thông tin thì bắt buộc phải gửi lại hết."*
> `10:50` — **Anh Sơn**: *"Đúng rồi! Trong cái **gói tin** đó, chứ không phải một bảng nữa."*

Hai điều chốt: (1) đúng 2 chế độ, không có chế độ thứ ba kiểu "lần đầu gửi hết rồi sau chỉ gửi phần đổi"; (2) trục phân loại là **gói tin**, khớp đúng `ResolvePacketPolicy` khoá theo mã gói. Gửi trùng vô hại — bên nhận ghi đè theo khoá, không cộng dồn; cái giá phải trả chỉ là băng thông.

📌 Nhật ký các việc đã xử lý 23-25/09/2026 (mở khoá gói bản chụp, thống nhất phân giải mã gói, lượt chạy đầu gói nối đuôi, mặc định an toàn NotReady...): xem bảng "Trạng thái các mục sau rà soát nghiệp vụ" ở mục 8 phía trên.

##### 9.6. Chưa làm những gì

> 📌 Đo lại trên `mssql_staging` ngày 24/09/2026.

| Việc | Vì sao chưa làm | Có chặn luồng đang chạy không? |
|---|---|---|
| Gói **106** — 4 trường tải trọng `grossWeight`, `axleWeights`, `axleCount`, `isOverweight` | Thiếu ở cả 3 tầng: cả 3 thiết bị `WOS` (trạm cân) đã xoá mềm, `Source` không có giá trị từ trạm cân, `TmsTrafficData` không có cột nào cho 4 trường này | ❌ Không — gói vẫn gửi đủ 7 trường thật, 4 trường này để trống |
| Gói **110** — dựng bảng outbox cho `messageId`, `channel`, `deliveryState` | Quét cả 154 entity trên staging, không có bảng `*Outbox`/`*Inbox`/`*Notification` nào | ❌ Không — gói đang `NotReady`, bị chặn đúng chủ đích |

##### 9.7. Edge case đầy đủ (gộp, khử trùng lặp)

| # | Tình huống | Cách hệ thống xử |
|---|---|---|
| 1 | Sập giữa lúc gửi nối đuôi, giữa 2 trang | Trang đã commit giữ nguyên, trang dở rollback; lần chạy sau lấy lại từ mốc cũ — không mất |
| 2 | Sập sau khi HTTP gửi xong nhưng trước khi commit mốc | Đối tác đã nhận, mốc chưa tiến ⇒ lần sau gửi lại đúng phần đó — đúng thiết kế *at-least-once* |
| 3 | Tiến trình bị kill / mất điện khi đang giữ quyền xử lý | Lock `NextTimeRun` tự hết hạn, worker lần sau tự nhận lại và tiếp tục từ checkpoint — không sai lệch dữ liệu |
| 4 | Worker giám sát (`DataTrackerWorker`) khởi động lại | Mốc Change Tracking nằm ở bảng `ShareDataTrackVersion` (§6b) nên worker **tiếp tục (resume)** từ mốc của lần chạy trước trên cùng máy, bắt được trigger cho cả phần dữ liệu phát sinh lúc chết. Mốc quá cũ ngoài retention 1 ngày thì rơi về #8 (self-heal nhảy cóc, quét định kỳ gửi bù). 📌 Trước 27/09/2026 mốc là biến trong RAM nên restart là nhảy thẳng tới mốc hiện tại, phần phát sinh lúc chết chỉ còn quét định kỳ gửi bù |
| 5 | NATS mất kết nối | Ghi cảnh báo rồi thôi; quét định kỳ quét bù, mốc checkpoint không đổi nên không mất dữ liệu |
| 6 | Mất kết nối CSDL giữa chừng | Transaction tự rollback; quyền xử lý treo giống #3, tự hồi phục |
| 7 | Sập khi đang gửi gói bản chụp | Không mất gì — chu kỳ sau gửi lại toàn bộ, đúng thiết kế nhóm gói này |
| 8 | Mốc version đã xử lý rơi ra ngoài cửa sổ hợp lệ Change Tracking (retention 1 ngày) | Self-Healing: bắt lỗi SQL 22114/22115, tự nhảy cóc lên mốc hiện tại — xem §6 phía trên. Ghi 1 dòng nhật ký hạ tầng `ESH-1601` kèm khoảng version bị bỏ qua |
| 9 | Tác vụ trước xử lý lâu, bản tin trigger sau dồn ứ | NATS Client xếp hàng tuần tự; `LockedSubscription` khoá OCC bỏ qua êm dịu nếu subscription đang bận; lượt sau thấy 0 dòng (checkpoint đã tiến) thì thoát ngay |
| 10 | Lần đầu khởi động sau triển khai (Change Tracking chưa bật) | Worker tự kiểm tra qua DMV hệ thống, chỉ `ALTER` khi còn thiếu; nên chọn giờ thấp điểm cho lần đầu. Nếu vẫn chưa bật được (thiếu quyền DBA), polling tạm dừng và ghi 1 dòng `ESH-1603` |
| 11 | Bảng nguồn trong câu SQL Change Tracking đang dùng bị tắt/xoá Change Tracking (DBA chạy `DISABLE CHANGE_TRACKING`, hoặc drop/tạo lại bảng khi deploy) | `QueryChangedTables` vô hiệu hoá câu SQL đang dùng **và** hẹn lại mốc quét (+5 phút) rồi ném lỗi lên `ExecuteAsync` ghi log **đúng 1 lần**, đồng thời ghi 1 dòng `ESH-1602` kèm câu SQL để lần ra bảng gây lỗi; trong lúc chờ, polling thoát sớm nên không dội lỗi; sau ≤5 phút tự quét lại và loại bảng hỏng khỏi câu SQL. Trigger tức thì chậm tối đa 5 phút, luồng quét định kỳ vẫn gửi đủ — chậm chứ không mất. 🔴 Phải có **cả hai** thao tác: chỉ bỏ câu SQL mà không hẹn lại mốc (hoặc ngược lại) là câu SQL hỏng bị lặp lại mỗi giây. 📌 Bổ sung 27/09/2026 sau khi phát hiện lưới chặn này từng bị mất do refactor |

##### 9.8. Cơ chế tương tranh khi nhiều worker chạy song song (OCC 3 lớp)

- **`GetLastSend`** — 2 worker cùng tạo bản ghi LastSend 1 lúc: bên thua ràng buộc unique tự nạp lại dòng đã có (`catch { reload; if found return reloaded; throw; }`), không ghi đè.
- **`UpdateLastSend`** — chốt chặn chỉ tiến không lùi: `WHERE LastTime < @newTime OR (LastTime = @newTime AND LastKey < @newKey)` — worker chạy trễ cố ghi mốc cũ hơn thì ảnh hưởng 0 dòng.
- **`CommitSuccess`** — điều kiện OCC `WHERE ID = @subId AND NextTimeRun = @nextRunDeadline`; nếu quyền xử lý bị worker khác giành mất giữa chừng thì rollback, dừng vòng lặp, không ghi đè kết quả worker kia.

Nhờ commit theo từng trang, khi trang thứ N gửi lỗi thì các trang trước đã gửi thành công vẫn giữ nguyên — không phải làm lại từ đầu.

##### 9.9. Vì sao lần chạy đầu tiên lùi đúng 1 chu kỳ (không lấy hết lịch sử, không lấy đúng hiện tại)

| Phương án | Hậu quả |
|---|---|
| ❌ Lấy toàn bộ lịch sử từ trước đến nay | Bảng dò xe/thu phí khổng lồ — kéo hàng triệu bản ghi, treo DB, ngập máy đối tác |
| ❌ Lấy mốc đúng thời điểm hiện tại (`GETDATE()`) | `WHERE UpdateTime >= now` không có dòng nào thoả — lần chạy đầu chạy không công |
| ✅ Lùi mốc về đúng 1 chu kỳ (`GETDATE() - IntervalSeconds`) | Quét được lượng nhỏ dữ liệu vừa sinh ra — đủ chứng minh kết nối hoạt động, không nặng hệ thống |

⚠️ Hệ quả: đối tác mới **vẫn không nhận được dữ liệu cũ hơn 1 chu kỳ**. Nếu cần nạp lịch sử cho đối tác mới, đó phải là 1 thao tác riêng có chủ đích, không gắn vào việc tạo đăng ký. Code cụ thể (`GetLastSend`, dùng `SELECT GETDATE()` của DB thay vì `DateTime.Now`) đã có ở §2b phía trên.

##### 9.10. Bảng LastSend có phình to không

**Không.** `ShareDataLastSend` là bảng trạng thái hiện tại, không phải nhật ký — mỗi cặp (Đối tác × Gói tin) chỉ sinh đúng 1 dòng, các lần chạy sau chỉ cập nhật tại chỗ. Gói `Snapshot` không sinh dòng nào. Tổng số dòng bị chặn trên bởi (số đối tác × 5 gói nối đuôi) — vài chục đến vài trăm dòng, không tăng theo lượng dữ liệu gửi đi. Không cần cơ chế tự xoá.

---

## I.C · CHIỀU NHẬN — INBOUND — 🧑‍💻 Hiếu

### SV-1b. Bỏ yêu cầu khoá `payload` khi parse gói đến ✅ *xong 22/09 (PR #51)*
- **Thực tế đã hoàn thành**: Gộp triển khai cùng đợt chuẩn hoá phễu lọc chiều nhận ở commit `54e81c26` (PR #51, HiếuNV).
- **Tầng WebAPI** (`ShareDataInboundController.cs`): Đọc trực tiếp `httpRequest.Body` thô lưu vào `RawContent`. Mọi thông tin định danh chuyển lên HTTP Header (`PartnerCode`, `PacketCode`, `SerialNbr`), không ép buộc phong bì JSON ở Controller.
- **Tầng Worker parse gói** (`DataInboundService.Parse.cs`):
  - Gỡ bỏ hoàn toàn việc bắt buộc `doc.RootElement.TryGetProperty("payload", ...)`.
  - **Có phễu lọc (`TargetShapeJson`)**: Vị trí mảng bản ghi do bộ khung quyết định linh hoạt qua `EachPath` (`$each` + `$as`) hoặc mảng ngầm (`IsRecordTemplateArray`). Không giả định bất kỳ tên khoá cố định nào (kể cả `payload`).
  - **Không có phễu lọc (Fallback)**: Hàm `TryFindFallbackArray` hỗ trợ đọc trực tiếp mảng JSON trần (`root.ValueKind == Array`) hoặc mảng bọc trong key `data` / `payload` (tương thích ngược).

### SV-2b. Bỏ phân nhánh xử lý theo Version *(chiều nhận)* ✅
- Parse trực tiếp dữ liệu theo schema định nghĩa, sai cấu trúc thì ghi lỗi, không rẽ nhánh theo version.

### SV-3b. Đọc `partnerCode` từ trong dữ liệu nhận về ✅ *xong 22/09 (PR #51)*
- Lấy `partnerCode` từ header hoặc từ dòng đầu tiên của mảng dữ liệu nhận về để nhận diện đối tác gửi.

### SV-7. Rà soát cắt cụt chuỗi dài (giới hạn 4000 ký tự)
- Kiểm tra các tham số chuỗi trong câu lệnh SQL động và kiểu dữ liệu ở bảng đích để tránh mất dữ liệu JSON âm thầm.

### SV-8b. Ghi log 2 bước cha–con *(chiều nhận)*
- Ghi nhận 2 bước: Tiếp nhận payload $\rightarrow$ Ánh xạ & Ghi CSDL. Chờ BE-5 hỗ trợ cấu trúc cha-con.

### SV-11. Tái cấu trúc luồng nhận (Inbound) ✅ *xong 22/09 (PR #51)*
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
- [x] **BE-8** **Endpoint lấy dữ liệu mẫu thật**: ✅ **xong 22/09 (PR #51)**
  - Cung cấp API `GET api/v1/share-data/packet/{packetCode}/sample-data`.
  - DTO `ShareDataPacketSampleDataDto` (`SampleRows`, `TotalRows`, `ColumnNames`, `GeneratedAt`).
  - Query Handler `PacketQueryHandler.Handle(GetShareDataPacketSampleDataQuery)` lấy tối đa 50 bản ghi mẫu phục vụ màn hình Gửi thử / Test Send.
- [x] **BE-9** **Trường "Tệp dữ liệu" nghiệp vụ**: `ShareDataTable` đã bỏ, không dùng bảng vật lý.
- [x] **BE-10** **Rào Port khi sửa đối tác**: Validator chặn sửa cổng khi đối tác đang kết nối.
- [x] **BE-11** **Độ ưu tiên (Priority)**: Giữ trường dữ liệu, không xử lý hàng đợi ưu tiên.
- [x] **BE-12** **Kho câu truy vấn SQL tập trung (`ShareDataPacketSqlCatalogUtil`)**: ✅ **xong 22/09 (PR #51)**
  - Nằm tại `Module.ShareData.Core.Utils.ShareDataPacketSqlCatalog`.
  - Định nghĩa tập trung SQL template, token `{TOP}`, `{ORDER_BY}`, tham số `@lastTime` cho cả 11 gói tin ESHARE (101–111).
  - Tích hợp ánh xạ bí danh gói tin (`CodeAliases`: `101_commonData` $\rightarrow$ `101`, `104_rfidData` $\rightarrow$ `105`, `106_wimData` $\rightarrow$ `106`,...).
  - 📌 **Cập nhật 25/09/2026**: Tầng Service (`PacketMetadataResolver.cs`) đã dọn rỗng `CodeAliases` vì staging đã sạch (chỉ còn `105_rfidData`, bóc tiền tố số tự nhiên, không che mã lệch nữa). Tầng Module vẫn tạm giữ alias (lệch tạm có chủ đích theo scope lock).
  - Cung cấp 2 phương thức chuẩn:
    - `GetWorkerSql(code)`: Dành cho Worker trích xuất dữ liệu snapshot.
    - `GetSampleSql(code, top)`: Dành cho WebAPI lấy dữ liệu mẫu (mặc định 5 dòng, tối đa 50 dòng).
- [x] **BE-13** **Chuẩn hóa kiến trúc & Quy ước đặt tên (Naming Refactoring)**: ✅ **xong 22/09 (PR #51)**
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
- [ ] Chặn gửi khi thiếu hồ sơ ánh xạ + badge trạng thái "Đã có/Chưa có Ánh xạ" (xanh/xám) trên bảng gói tin của đối tác — theo chốt họp 21/09 Phiên 3 mốc 02:35-02:51 ("bắt buộc phải có mapping, không có thì báo lỗi, không cho gửi"). Hiện `editSubscription.vue` chỉ hiện cảnh báo rồi vẫn cho gửi theo mặc định gói tin, `subscriptionTable.vue` chưa có badge. Chưa làm vì đang ưu tiên luồng Backend nối đuôi/event trước.

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
┌─ A · CHỌN VIỆC (Selection & Lock) ────────────────────────────────────┐
│  Worker thức dậy (5s polling hoặc NATS event trigger)                 │
│    → Quét subscription hợp lệ (Outbound · Partner Active · Đến hạn)   │
│    → Claim lock (NextTimeRun = nextRunDeadline)                       │
│    → Resolve Packet & Mapping profile (Thiếu → dừng, ghi ESH-1304)    │
│    → Khởi tạo DataOutboundContext xuyên suốt                          │
└───────────────────────────────────┬───────────────────────────────────┘
                                    ▼
┌─ B · BƯỚC 1: TRÍCH XUẤT (Extraction) ────────────────────────────────┐
│  DataOutboundExtractionProcess tra query handler theo PacketCode      │
│  Chạy SQL với cursor kép (__lastTime, __rowid) & trần trang (100)     │
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
│    1. Commit (OCC NextTimeRun == nextRunDeadline, tăng SerialNbr)     │
│    2. Update LastSend đơn điệu (UpdateLastSend)                       │
│    3. Ghi ActivityLog thành công (try/catch riêng)                    │
│  Cuối cùng: Nhả lock đúng một lần trong khối finally                   │
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
  - `ESH-1303`: Mất lock khi ghi nhận kết quả.
  - `ESH-1304`: Không tìm thấy cấu hình phễu lọc (`ShareDataMapping`).
  - `ESH-1402`: Gửi HTTP REST tới đối tác thất bại.

---

# PHẦN V — BẢO ĐẢM AN TOÀN & KIỂM THỬ (TESTING)

## 1. Các chốt chặn an toàn bắt buộc (Mandatory Safeguards)
1. **Strict Local Database for `dotnet test`**: Toàn bộ connection string dùng khi chạy test PHẢI trỏ về `local` (`localhost`, `127.0.0.1`, `(localdb)`, `.`). Nếu phát hiện IP remote (ví dụ `10.10.8.30`), HỦY test ngay lập tức.
2. **Không tự ý thực thi DDL/DML**: Mọi thay đổi schema CSDL (các lệnh DDL - Data Definition Language, hoặc DML) chỉ xuất file `.sql` ra đĩa để quản trị viên review.
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
  *(Bao gồm `DataOutboundServiceTests`, `DataTrackerWorkerTests` — đúng 2 file. Số lượng test là số liệu tạm, dễ lạc hậu theo từng lần thêm — chạy lệnh trên để xem số hiện tại và tình trạng PASS thay vì tin số đếm cứng trong tài liệu)*.
- **Biên dịch Frontend**:
  ```powershell
  cd TA-ITS015-WEBVUE-V1.0 && npm run build
  ```

