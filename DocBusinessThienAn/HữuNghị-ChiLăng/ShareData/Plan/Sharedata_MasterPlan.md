# ShareData (ESHARE) — Master Plan FE · BE · Service

> 🔴 **SINGLE SOURCE OF TRUTH (SSOT):** Tài liệu quy hoạch tổng thể duy nhất cho toàn bộ phân hệ **ShareData (ESHARE)** gồm Frontend, Backend WebAPI và Service Worker.
> Được hợp nhất từ các tài liệu phân tích, kế hoạch kiểm thử, cơ chế gửi nối đuôi LastSend và kích hoạt sự kiện Change Tracking + NATS.
> 🔴 **ĐÂY LÀ SỔ THEO DÕI TASK DUY NHẤT.** Muốn biết *task nào xong, task nào chưa* thì mở đúng tệp này và đọc
> các checklist `SV-*` / `BE-*` / FE. Các báo cáo `Plan/*_Review_<ngày>*.md` chỉ là **ảnh chụp một lượt rà soát**
> tại thời điểm đó (tên có ngày ⇒ tự nó là bản chụp) — ⛔ **KHÔNG tra trạng thái ở đó**. Kết luận của mỗi lượt rà
> soát **bắt buộc được hợp nhất về đây** — kể cả bảng *quyết định đã chốt & phương án bị bác* (nay ở `SV-12 §10`),
> vì đó là phần đắt nhất. Gộp xong thì tệp báo cáo **được xoá**, để ⛔ không còn hai nguồn nói về cùng một trạng
> thái. Xem quy tắc **19.24**.
>
> 📌 **Cập nhật lần cuối: 28/09/2026** — đợt siết lưới kiểm thử & tinh gọn worker giám sát (3 đợt sửa mã + 1 đợt
> đồng bộ tài liệu), và lượt rà 33 code change của nhánh `feat/20260922-sharedata-service` đối chiếu trực tiếp
> với 4 biên bản họp trong `doc/transcript/` (21/09 ánh xạ + gửi nối đuôi · 19/09 HTTP header · 16/09 refactor
> worker · 16/09 định danh đối tác). Kết quả: **7 ràng buộc nghiệp vụ đã chốt đều khớp code**; phát hiện
> **1 task bị thiếu hẳn khỏi checklist** (đưa vào `BE-14`) và **3 điểm cần dọn ở `DataOutboundRestSender`**.
> Trước đó: 27/09/2026 — đồng bộ tài liệu với code thật sau đợt rà soát đối chiếu 31 code change của nhánh `feat/20260922-sharedata-service`: sửa 13 điểm lệch (3 điểm tự mâu thuẫn trong chính tài liệu: phễu lọc `SourceAllowList` đã bãi bỏ nhưng §4 còn mô tả, trần trang 50 vs 100, retention 2 ngày vs 1 ngày; 10 điểm lệch tên hàm/field/file test sau các đợt đổi tên), thống nhất thuật ngữ `lease` → `lock` theo rule 7, bổ sung edge case #11 (mất cơ chế vô hiệu hoá cache khi câu CHANGETABLE hỏng). Trước đó: 26/09/2026 — đồng bộ tên `Checkpoint` → `LastSend` xuyên suốt tài liệu (khớp code thật sau khi áp dụng `sharedata-doi-ten-checkpoint-thanh-lastsend-trong-dataoutboundservice-prompt.md`); trước đó: 25/09/2026 — đã gộp toàn bộ nội dung còn giá trị từ 3 báo cáo review (`Sharedata_Review_LuongNoiDuoi_20260923.md`, `Sharedata_Review_GuiKhiCoDuLieuMoi_20260923.md`, `Sharedata_Review_DoiChieuThucTe_20260925.md`) trực tiếp vào tài liệu này (chủ yếu ở SV-12 §9); 3 file review đã được xoá sau khi gộp, tài liệu này là SSOT duy nhất.

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

> 📌 **Không còn tài liệu anh em nào trong `Plan/`.** Báo cáo `Sharedata_Review_TongThe_20260927.md` đã được **gộp trọn vào đây và xoá ngày 28/09/2026**: phần đối chiếu biên bản họp 21/09 ↔ mã nguồn nằm ở `SV-12` và các mục `#### 6*`; bảng **quyết định đã chốt & phương án bị bác** nằm ở `SV-12 §10`; việc còn treo nằm ở các checklist `SV-*` / `BE-*` / FE. ⇒ Muốn tra bất cứ thứ gì về ShareData, chỉ mở **đúng tệp này**.
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
  - ✅ **Cơ chế lọc mốc nối đuôi ưu tiên thời gian mới nhất (28/09)**:
    - **5 gói nối đuôi (103, 104, 106, 107, 109)**: Lọc theo khóa phức hợp `(Thời gian, ID)`. Mốc thời gian ưu tiên thời điểm cập nhật mới nhất bằng hàm `COALESCE(UpdateTime, CreateTime, <thời gian nghiệp vụ>)`, đảm bảo bao phủ đầy đủ cả bản ghi mới tạo (`Insert`) lẫn bản ghi vừa sửa đổi (`Update`), đúng luồng nghiệp vụ.
    - **Các gói hiện trạng / danh mục (101, 102, 105, 108, 110, 111)**: Gửi nguyên vẹn bản chụp (Snapshot) toàn bộ danh mục hiện tại theo đúng yêu cầu nghiệp vụ.
  - ✅ **Siết lưới kiểm thử vòng phát/nhận tín hiệu (28/09)** — bài kiểm thử đầu-cuối nay kiểm thêm **số bản ghi
    thực sự gửi đi** (`RecordCount > 0`), ⛔ không chỉ kiểm *"có tồn tại một dòng nhật ký trạng thái thành công"*.
    - 🔴 **Vì sao phải kiểm thêm:** một **lượt chạy không có dữ liệu mới** vẫn ghi nhật ký trạng thái **thành công**,
      chỉ khác là số bản ghi bằng **0**. Trạng thái *thành công* ở đây nghĩa là *"lượt chạy kết thúc không lỗi"*,
      ⛔ **không** phải *"đã gửi được dữ liệu cho đối tác"*.
    - ⚠️ **Hệ quả đội vận hành cần biết:** mở màn hình nhật ký thấy toàn dòng thành công **vẫn chưa kết luận được**
      là dữ liệu đã sang đối tác — bắt buộc xem thêm cột số bản ghi.
  - ✅ **Tinh gọn worker giám sát (28/09)** — gộp 3 khối trùng lặp thành 2 hàm dùng chung, bỏ bản sao thứ hai của
    câu truy vấn thay đổi, và nạp cấu hình bảng nguồn **đúng 1 lượt** thay vì 4 lượt.
  - ✅ **Cả hai đường nhảy cóc mốc version đều ghi `ESH-1601` và ĐÃ CÓ TEST PHỦ 100% (28/09)** — đường *thử lại sau khi cô lập bảng hỏng*
    trước đó nhảy cóc **im lặng không để lại dấu vết**; nay cũng ghi nhật ký, phân biệt bằng cờ
    `afterTableIsolation: true`. Đã bổ sung bài test `TrackingLog_WhenRetryAfterTableIsolationHitsInvalidVersion_SelfHealsAndWritesEsh1601WithFlag_Test`
    (dùng SqlSugar AOP can thiệp tất định vào Lời gọi 1 bảng lỗi → Lời gọi 2 dính lỗi version `22114`),
    xác thực thành công việc tự phục hồi `LastVersion` và ghi nhật ký hạ tầng `ESH-1601` kèm cờ `afterTableIsolation: true`.
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
- **Cơ chế khởi tạo lấy-hoặc-tạo (GetLastSend, 28/09/2026):** Tuyệt đối không ghi đè, dựa trên khoá duy nhất `(PartnerCode, PacketCode)`. Khi gặp đua tranh (unique race), node chạy sau đọc lại bản ghi đã tạo của node chạy trước, ⛔ tuyệt đối không ghi đè mốc.
  - 🔴 **Lý do ⛔ không dùng `Saveable` / `Storageable`:** `Saveable` so theo **khoá chính** mà `ID` là GUID mới toanh ⇒ **luôn** rơi nhánh INSERT (đâm vào unique constraint); và nhánh update của nó sẽ **ghi đè `LastTime`/`LastKey`** ⇒ xoá mốc đã gửi, bắt worker gửi lại toàn bộ dữ liệu cũ cho đối tác. `Storageable` thì vẫn hở khoảng trống đua tranh giữa SELECT và INSERT nên ⛔ không bỏ được try-catch, lại tốn thêm round-trip CSDL.

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
- **Thống nhất `CreateTime`/`UpdateTime` trong `GetLastSend` (28/09/2026):** Chuyển từ `DateTime.Now` sang đồng hồ CSDL qua helper `GetDbNow(db)` — lấp chỗ sót cuối cùng của đợt chuẩn hoá 27/09, bảo đảm toàn bộ mốc thời gian và dấu vết của `ShareDataLastSend` đều lấy từ đồng hồ CSDL.
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
- Do đó, giá trị `SerialNbr` trong DB mang ngữ nghĩa *"serial của trang/lần kết xuất kế tiếp"*. Chuỗi số luôn liên tục, đơn điệu, không bị hụt hay trùng lặp. `SerialNbr` mang đúng **một** nghĩa: **số thứ tự trang/lượt kết xuất**.
- 📌 **Lịch sử ngắn (28/09/2026):** Từng có giai đoạn ngắn `SerialNbr` được dùng làm `serialGuard` phụ trợ trong OCC của `CommitSuccess` (khi chưa tách cột `ProcessingUntil`). Sau khi bổ sung cột `ProcessingUntil` ngày 28/09/2026, vế `serialGuard` đã bị **gỡ bỏ hoàn toàn** khỏi `CommitSuccess`. OCC nay dựa độc quyền vào `ProcessingUntil`.

#### 6. Cơ chế Kích hoạt tức thời (Event-Driven) qua Change Tracking + NATS

> 📌 **Cập nhật 28/09/2026 — tách service khỏi worker, chuẩn hoá lại tên class và mô tả subject NATS cho khớp code thật** (đối chiếu độc lập qua đọc trực tiếp code, xem §9): tên worker giám sát đã qua ba lần đổi — `DataChangeWatcherWorker` → `DataTrackerWorker` → **`DataChangeTrackingWorker`** (28/09/2026, bám sát thuật ngữ Microsoft "Change Tracking"); phía NATS `DataNatsWorker` → `DataNatsConsumerWorker` → **quay lại `DataNatsWorker`**. Từ 28/09/2026, **toàn bộ logic Change Tracking và NATS đã được chuyển sang `DataChangeTrackingService` và `DataNatsService` (Singleton)**; worker chỉ giữ vòng lặp và nhịp quét. Mô tả subject NATS có hậu tố `{PacketCode}` trước đây là **mô tả sai**, code thật luôn dùng 1 subject chung.

- `DataChangeTrackingService.CheckTracking`: Kiểm tra trạng thái qua DMV hệ thống (`sys.change_tracking_databases`, `sys.change_tracking_tables`). Khi Worker khởi động, hệ thống **luôn tự động kiểm tra và kích hoạt Change Tracking (DDL - Data Definition Language) ở mọi môi trường** (không còn phân biệt Dev/Staging/Production, đã bỏ hẳn cờ cấu hình `AutoEnableChangeTracking`). *(Ghi chú: Đây là quyết định có chủ đích của người dùng ngày 23/09/2026 nhằm tối ưu vận hành zero-touch, thay thế khuyến nghị mặc định ban đầu là chỉ cho phép DBA chạy script tay ở Staging/Production).*
- `DataOutboundService.ProcessSubscriptions` (Cập nhật 28/09/2026 — Tách cột `ProcessingUntil` khỏi `NextTimeRun` & Chuẩn hóa OCC):
  - 📖 **OCC là gì? (Optimistic Concurrency Control — Kiểm soát tương tranh / đồng thời lạc quan):** Là cơ chế xử lý tranh chấp tài nguyên khi nhiều tiến trình/worker chạy phân tán cùng truy cập vào 1 Subscription mà **không dùng khóa bi quan (Pessimistic Locking)** gây nghẽn hàng đợi hoặc treo CSDL. Thay vì khóa cứng bảng bằng transaction dài hay `SELECT FOR UPDATE`, OCC cho phép các worker vận hành độc lập và **kiểm soát tính toàn vẹn thông qua các mốc kiểm tra nguyên tử (Atomic Check-and-Set / CAS)**:
    1. *Lúc chiếm quyền xử lý (Acquire Lock):* Worker chạy `UPDATE ... SET ProcessingUntil = @processingUntil WHERE ID = @id AND (ProcessingUntil IS NULL OR ProcessingUntil <= @now)`. Đúng 1 worker thắng cuộc (`affected = 1`) được đi tiếp vào pipeline xử lý, các worker cùng thời điểm nhận `affected = 0` và rút lui an toàn.
    2. *Lúc cam kết kết quả (`CommitSuccess`) hoặc kết thúc dọn dẹp (`ReleaseLock`):* Đều kiểm tra lại mốc OCC: `WHERE ID = @id AND ProcessingUntil = @processingUntil`. Nếu worker chạy quá lâu làm lock hết hạn và bị worker khác chiếm quyền giữa chừng, câu lệnh `WHERE` này sẽ trượt (`affected = 0`), worker tự động dừng vòng lặp gửi trang và rollback, ngăn chặn hoàn toàn việc hai worker ghi đè đè lên tiến độ của nhau.
  - **Tách bạch triệt để hai cột:** `NextTimeRun` (DateTime?) chuyên trách duy nhất **Lịch chạy định kỳ**, `ProcessingUntil` (DateTime?) chuyên trách duy nhất **Trạng thái đang bị worker xử lý** (`null` = rảnh, có mốc tương lai = đang bị chiếm quyền).
  - **Lý do kỹ thuật bắt buộc tách cột:** Một mốc thời gian tương lai trên `NextTimeRun` có thể mang nghĩa là *lịch định kỳ* (luồng sự kiện NATS trigger ĐƯỢC PHÉP chạy) hoặc là *dấu hiệu đang xử lý* (luồng khác PHẢI lùi) — hai hành động hoàn toàn trái ngược nhau nên **một cột duy nhất không thể nào diễn đạt được**.
  - **Ba phương án một-cột đã thử nghiệm và bị bác bỏ:**
    1. *Phân biệt theo độ lớn* (mốc chiếm quyền $\ge now+300$, mốc lịch $= now+interval$): Hỏng ngay với gói `daily` vì `IntervalSeconds = 86400` khiến mốc lịch xa hơn mốc chiếm quyền.
    2. *Dùng cờ `State` làm dấu hiệu đang chạy*: `State` là trạng thái nghiệp vụ vòng đời đăng ký và cả hai luồng đều lọc `State == Active` $\rightarrow$ làm vỡ cả hai luồng.
    3. *Đảo nghĩa `NextTimeRun` và suy lịch từ `LastTimeRun + interval`*: `ComputeNextDailyRun` tính lịch theo giờ/thứ cấu hình trong `ScheduleJson` chứ không phải khoảng cách đều $\rightarrow$ đăng ký `daily` chạy sai giờ.
  - **Bản CAS trên ảnh chụp `NextTimeRun` (đã loại bỏ):** Từng dùng trong ngày 28/09/2026 chặn được N instance đồng thời nhưng **không** chặn được trigger đến muộn cướp quyền khi worker trước đang xử lý dở.
  - **Cơ chế OCC chuẩn hóa:** Cả `CommitSuccess` và `ReleaseLock` nay dùng **cùng một khuôn**: `WHERE ID == sub.ID && ProcessingUntil == processingUntil`. Hoàn toàn không còn vế `SerialNbr` nào trong `WHERE`.
  - **Vị trí chặn có ý nghĩa duy nhất:** Trong `ExportPage`, thứ tự thực thi là `restSender.Send(...)` $\rightarrow$ `CommitSuccess(...)` $\rightarrow$ `WriteActivityAsync(...)`. Mọi lưới chặn đặt tại `CommitSuccess` chỉ cứu được tính toàn vẹn CSDL (OCC) khi đối tác **đã nhận dữ liệu qua HTTP rồi**. Do đó, muốn chặn gửi trùng lặp bắt buộc **phải chặn ngay ở bước chiếm quyền** (`ProcessingUntil == null || ProcessingUntil <= now`) trước khi vào pipeline trích xuất và gửi HTTP. Thêm kiểm tra khung giờ `DataOutboundScheduler.IsWithinTimeWindow` tường minh cho luồng sự kiện.
- `DataChangeTrackingWorker` & `DataChangeTrackingService`: Worker chạy nền với heartbeat 1 giây, gọi `DataChangeTrackingService` đọc `CHANGE_TRACKING_CURRENT_VERSION()`. Bảng ánh xạ bảng nguồn → mã gói **nạp động từ cấu hình** `appsettings.json` khoá `ShareDataTracker:TablePacketMap` (`TmsTrafficData` ra cả 103 và 106), ⛔ không còn là bảng cứng trong mã và bộ lọc `ResolvePackets` chỉ chặn gói `NotReady` và `Disabled` — **mọi gói hợp lệ còn lại, cả nối đuôi lẫn bản chụp, đều kích hoạt được bằng sự kiện** nếu đăng ký bật cờ `SendOnNewData`. Khi phát hiện dữ liệu bảng nguồn thay đổi, phát sự kiện NATS qua `TransportManager` vào **1 subject chung duy nhất** `ta.its.event.sharedata.newdata` (hằng số `DEFAULT_NATS_SUBJECT`, không hậu tố) — gói tin nhận diện qua field `PacketCode` trong payload (`{PacketCode, Type, Version, TriggeredAt}`), đây là thiết kế chủ đích.
- `DataNatsService` & `DataNatsWorker`: `DataNatsService` lắng nghe đúng subject chung đó, đọc `PacketCode` từ payload rồi gọi `DataOutboundService.ProcessSubscriptions(packetCode)`, còn `DataNatsWorker` chỉ giữ vòng đời đăng ký. Không có cơ chế chống dội nào ở tầng worker/service này — chống dội từng thực hiện bằng cấu hình `DebounceSec` riêng của từng Subscription trong lệnh chiếm quyền nguyên tử; **tuy nhiên từ 28/09/2026 vế chống dội này đã bị comment lại** trong lệnh chiếm quyền nguyên tử của `ExportSubscription`, ⛔ **không còn hiệu lực kể cả khi `DebounceSec` được điền số**. Lý do: trigger gửi xong là `ReleaseLock` đẩy `NextTimeRun` đi một chu kỳ nên luồng quét định kỳ đã tự giãn nhịp. Bật lại = bỏ một dấu comment ở `ExportSubscription`, và phải bỏ comment kèm 3 bài test tương ứng.
- **Quan hệ "Trigger gửi xong ⇒ `ReleaseLock` đẩy `NextTimeRun` ⇒ luồng quét định kỳ bỏ qua" (Bổ sung 28/09/2026, đã có test khoá lại):** `ExportSubscription` luôn gọi `ReleaseLock` trong khối `finally` (dùng chung cho cả luồng sự kiện và luồng định kỳ), tính `NextTimeRun = dbNow + IntervalSeconds` (hoặc mốc daily theo `ScheduleJson`). Khi trigger NATS gửi thành công, `ReleaseLock` đẩy luôn đồng hồ của luồng quét định kỳ đi một chu kỳ, giúp luồng poll chạy ngay sau đó tự động bỏ qua đăng ký này, không gửi lặp lần hai (đã được khoá chặt chẽ bởi bài test `TriggerFlow_WhenExportSucceeds_AdvancesNextTimeRunSoScheduledSweepSkipsIt_Test`). Nêu rõ hai hệ quả:
  1. `ReleaseLock` nằm trong khối `finally` nên mốc `NextTimeRun` vẫn bị đẩy kể cả ở các lượt không có dữ liệu mới để gửi (sẽ có lúc thấy `NextTimeRun` nhảy mà `LastTimeRun` đứng yên, hoàn toàn vô hại).
  2. Khi dữ liệu nguồn chảy liên tục dẫn đến trigger phát mỗi giây, `NextTimeRun` bị đẩy liên tục khiến luồng quét định kỳ gần như không bao giờ phải chạy. Điều này an toàn và **tự phục hồi**: nếu NATS chết, trigger ngừng thì không còn ai đẩy mốc, luồng poll sẽ tự động quay lại chạy bình thường sau nhiều nhất một chu kỳ.
- 🔴 **Hai đặc điểm quyết định cách đặt lưới chặn và cách đọc nhật ký** *(gộp từ `Sharedata_Review_36CodeChange_20260928.md`, báo cáo đã xoá sau khi gộp 28/09/2026)*:
  1. **Mọi lưới chặn đặt ở `CommitSuccess` đều MUỘN.** Thứ tự thật trong `ExportPage` là `restSender.Send(...)` → `CommitSuccess(...)` → `WriteActivityAsync(...)`. Khi `CommitSuccess` trượt OCC thì **đối tác đã nhận dữ liệu rồi** — lưới ở đó chỉ ngăn được việc *ghi trùng mốc gửi*, ⛔ **không** ngăn được việc *gửi trùng*. Chỉ lưới đặt ở **bước chiếm quyền** (trước khi gọi `Send`) mới thật sự chặn gửi trùng. Đây chính là lý do cột `ProcessingUntil` phải chặn ngay ở lệnh chiếm quyền chứ không phải lúc commit.
  2. **Đếm dòng `ShareDataActivityLog` có `Success` ⛔ KHÔNG bằng đếm số lượt gửi thật.** `ExportPage` vẫn ghi một dòng `Success` cho cả lượt chạy **không có dữ liệu nào** để gửi. Bài test muốn đo số lượt gửi thật phải lọc thêm `RecordCount > 0` hoặc đo qua độ tăng của `SerialNbr` — đếm trơn số dòng `Success` cho kết quả sai lệch. 📌 Chính lỗi này khiến bài `ChangeTracking_WhenConcurrentTriggersRaceForSameSubscription_ExactlyOneExportSucceeds_Test` xanh suốt trong khi lỗ OCC vẫn hở.
- 🔴 **Hai quyết định có chủ đích ngày 28/09/2026 (Tuyệt đối không tự ý "dọn nhầm"):**
  1. Khối điều kiện nền của hai luồng (`Active`, `Outbound`, `TargetShapeJson != null`, `ApiActive`, `SendOnNewData`,...) **cố ý để hai bản chép độc lập** ở hai hàm `ProcessSubscriptions`, ⛔ **KHÔNG được gộp về helper chung**.
  2. Vùng mã lọc và chiếm quyền này **cố ý để trần**, ⛔ **KHÔNG được tự ý thêm khối comment giải thích** bên trong thân hàm.
- Cơ chế **Self-Healing khi mốc `LastVersion` rơi ra ngoài cửa sổ hợp lệ của Change Tracking** (bổ sung 25/09/2026, retention rút ngắn 26/09/2026): SQL Server chỉ giữ dữ liệu Change Tracking 1 ngày (`CHANGE_RETENTION = 1 DAYS, AUTO_CLEANUP = ON` — rút ngắn từ 2 ngày ban đầu để giảm overhead lưu trữ phía CSDL; đổi lại, worker chỉ được phép ngừng chạy tối đa 1 ngày trước khi bị giảm cấp xuống luồng quét định kỳ thay vì trigger tức thời); nếu worker ngừng cập nhật mốc lâu hơn khoảng đó (ví dụ mất kết nối CSDL kéo dài mà tiến trình không restart), câu `CHANGETABLE` sẽ ném lỗi SQL lặp lại vô hạn mỗi giây. `DataChangeTrackingService.QueryChangedTables` bọc `try/catch (Exception ex) when (IsVersionInvalid(ex))` quanh câu truy vấn đổi bảng — khi bắt được lỗi version không hợp lệ (mã SQL 22114/22115 hoặc message tương ứng, hàm `IsVersionInvalid`), tự động nhảy cóc mốc `LastVersion` lên version hiện tại để hồi phục ngay chu kỳ kế tiếp, không cần restart service. Chi tiết & lý do đổi hướng so với đề xuất ban đầu (proactive vs reactive): `sharedata-tu-phuc-hoi-change-tracking-min-valid-version-prompt.md` (đã thực thi và xoá theo Auto-Cleanup). Hàm `GetMinValidVersion` viết sẵn cho hướng proactive không được chọn đã trở thành dead code và bị xoá ngày 26/09/2026 (`sharedata-don-dead-code-getminvalidversion-prompt.md`, đã thực thi và xoá) — xem `Prompt/README.md`. **Từ 27/09/2026** mỗi lần nhảy cóc còn ghi 1 dòng nhật ký hạ tầng mã `ESH-1601` (xem mục 6c) kèm khoảng version bị bỏ qua, để sau này lần ra được vì sao một quãng dữ liệu chỉ đi qua luồng quét định kỳ mà không có trigger.
- Toàn bộ vùng CT + NATS được bao phủ bởi bộ test trong `tests/ShareData/Services/DataChangeTrackingServiceTests.cs` (bao gồm 3 bài cho cơ chế Self-Healing reactive: `IsTrackingVersionInvalid_WhenGivenVariousExceptions_ClassifiesCorrectly_Test`, `ChangeTracking_WhenVersionInvalid_SelfHealsAndFastForwardsToCurrentVersion_Test`, `TrackingLog_WhenRetryAfterTableIsolationHitsInvalidVersion_SelfHealsAndWritesEsh1601WithFlag_Test`, và nhóm `NatsWorker_HandleTrigger_*` cho tầng NATS). 📌 **Siết bài test đua tranh trigger (28/09/2026):** Bài `ChangeTracking_WhenConcurrentTriggersRaceForSameSubscription_ExactlyOneExportSucceeds_Test` đã được siết chặt assert: trước đó tên nói `ExactlyOne` nhưng thân bài chỉ khẳng định *"ít nhất một"*, nên xanh kể cả khi cả 5 lời gọi cùng gửi (lọt lưới lỗ OCC trên); nay đã siết đếm đúng số lượt gửi có dữ liệu và kiểm tra `SerialNbr` tăng đúng 1. 📌 Trước 27/09/2026 tài liệu ghi 2 file `DataChangeWorkerTests.cs` + `DataChangeWorkerNatsTests.cs` — cả 2 tên đều không còn đúng: bộ test đã gộp về **1 file duy nhất** `DataChangeTrackingServiceTests.cs`. Số lượng bài test cụ thể là số liệu tạm, dễ lạc hậu — chạy `dotnet test tests/test.csproj --filter "FullyQualifiedName~ShareData"` để xem số hiện tại thay vì tin số đếm cứng trong tài liệu.


#### 6b. Nơi lưu trạng thái của worker giám sát — bảng `ShareDataTrackVersion`

- **Bảng chỉ giữ đúng một thứ:** `ShareDataTrackVersion` có **1 cột nghiệp vụ duy nhất `LastVersion`**, và
  toàn hệ thống dùng **đúng 1 dòng**. ⛔ Không còn khoá theo máy hay tiến trình.
  Lý do chỉ mốc này ở CSDL: nó là **mốc tiến độ nghiệp vụ** — mất là mất khả năng tiếp tục sau khi khởi
  động lại, và nhiều instance cần dùng chung một mốc.
- **Nâng mốc nguyên tử trước khi phát tín hiệu:** khi nhiều instance cùng thấy version của CSDL tăng, chỉ
  **một** bên nâng được `LastVersion` bằng một lệnh cập nhật có điều kiện (`LastVersion < mốc mới`) và
  thắng quyền phát tín hiệu; các bên thua nhận 0 dòng bị ảnh hưởng rồi **rút lui im lặng**, ⛔ không ghi
  cảnh báo. Nhờ vậy một thay đổi chỉ sinh một loạt tín hiệu, dù chạy bao nhiêu instance.
- ⛔ **Trạng thái cô lập bảng lỗi nằm ở RAM có chủ đích, không thêm cột CSDL** — gồm 2 thứ:
  - Danh sách **bảng đang bị cô lập kèm mốc hẹn thử lại RIÊNG của từng bảng**.
  - Câu SQL truy vấn thay đổi **đang áp dụng**, chỉ gồm các bảng lành mạnh.

  Lý do đặt ở RAM: cả hai **suy ra được từ `sys.change_tracking_tables`** bất cứ lúc nào — nguồn sự thật
  vốn đã ở CSDL. Khởi động lại thì chu kỳ đầu tự phát hiện lại ⇒ ⛔ không có gì cần bảo toàn. Lưu thêm một
  cột là **nhân bản dữ liệu**, rồi phải lo hai bản lệch nhau, và lại **mất** thông tin *máy nào* phát hiện.
- 🔴 **Mốc hẹn thử lại phải tính theo TỪNG bảng, ⛔ không dùng một mốc chung.** Nếu dùng chung: bảng A hỏng
  lúc t=0 (hẹn t+5 phút), bảng B hỏng lúc t=2 phút ⇒ mốc chung bị đẩy thành t+7 phút, bảng A **mất lượt**.
  Hỏng liên tiếp thì **không bảng nào** được thử lại. Chu kỳ thử lại là hằng số 5 phút trong mã.
- **Cô lập bảng lỗi mà vẫn giữ tín hiệu cho phần còn tốt:** khi một bảng nguồn mất Change Tracking giữa lúc
  đang chạy, worker nhận diện đúng bảng hỏng, loại nó khỏi câu SQL đang áp dụng, **thử lại ngay** với các
  bảng lành và chạy tiếp bình thường. Ghi **đúng một** dòng `ESH-1602` cho mỗi lần phát hiện; các chu kỳ
  sau bảng hỏng không còn trong câu SQL nên ⛔ không dội lỗi. Đến hạn thì tự thử bật lại và đưa bảng trở
  vào giám sát.
- ⛔ **Đây là bảng TRẠNG THÁI, không phải nhật ký sự kiện.** Lỗi hạ tầng thuộc mục 6c. Bản thiết kế
  append-only ban đầu đã bị bãi bỏ và dọn sạch code ngày 27/09/2026.
- 📌 **Lịch sử thiết kế** (giữ lại để ⛔ không ai đề xuất lại): bản 27/09 buổi đầu từng để bảng này **6 cột**
  (`MachineName`, `ProcessId`, `LastProcessedVersion`, `MissingTables`, `NextTableRefreshTime`,
  `RetryIntervalSeconds`) khoá độc nhất theo `(MachineName, ProcessId)`, mỗi lần chạy tiến trình một dòng,
  resume mốc theo từng máy. Đã bỏ trong cùng ngày vì đưa xuống CSDL những thứ suy ra được, và vì mốc hẹn
  dùng chung gây lỗi mất lượt thử lại nêu trên.
- **Đọc và ghi mốc `LastVersion` xử lý lỗi NGƯỢC nhau, có chủ đích:** `GetOrCreateTrackVersion` ⛔ **không** nuốt lỗi — CSDL không đọc được thì chu kỳ quét cũng không làm gì được, để ngoại lệ văng lên vòng lặp worker xử lý. Ngược lại `SaveTrackVersion` **nuốt** lỗi và chỉ ghi Warning — mất một lượt ghi trạng thái không đáng để sập chu kỳ quét, lượt sau ghi lại. ⚠️ Cái giá: ghi hỏng liên tục thì mốc không tiến, dẫn tới **phát lại tín hiệu cho cùng một thay đổi** ở chu kỳ sau — chấp nhận được vì hệ thống vốn cam kết at-least-once, và Warning để nhìn thấy khi nó xảy ra.


#### 6c. Nhật ký sự cố hạ tầng Change Tracking — nhóm mã `ESH-16xx`

- **Ranh giới 3 tầng ghi nhận** (chốt 27/09/2026):
  - `ShareDataAlertLog` — **lỗi nghiệp vụ** mà quản trị viên cần can thiệp (`PacketNotFound`, `MappingNotFound`, `QueryFailed` của luồng gửi, `HttpSendFailed`...). Có cờ xác nhận đã xử lý.
  - `ShareDataActivityLog` — nhật ký truyền nhận nghiệp vụ, hiển thị trên giao diện. **Nay dùng thêm** cho sự cố hạ tầng Change Tracking, phân biệt bằng nhóm mã `ESH-16xx` đặt ở cột `Remark`.
  - `ILogger` — tranh chấp tài nguyên bình thường của cơ chế lock (`LockLost`), tuyệt đối không đẩy lên 2 tầng trên làm rác cảnh báo.
- **3 mã đã dùng** (`ShareDataAlertCode.Tracking`): `ESH-1601` mốc version không hợp lệ đã tự nhảy cóc · `ESH-1602` truy vấn `CHANGETABLE` lỗi vì lý do khác version, kèm câu SQL trong `DetailJson` để lần ra bảng gây lỗi · `ESH-1603` CSDL hoặc bảng nguồn chưa bật Change Tracking, polling tạm dừng.
- ⛔ **Không vào `ShareDataAlertLog`**: đây là lỗi hạ tầng, không phải lỗi nghiệp vụ — đưa vào đó là làm rác cảnh báo của người vận hành.
- **Không đổi schema**: không thêm cột, không thêm enum, không cần DBA, không cần sửa giao diện. Tận dụng cột `Remark` vốn đang bỏ trống với thực thể này.
- **Tự dọn quá hạn 7 ngày**: `ShareDataTransferLog.PurgeTrackingLogsAsync` chạy 1 lần mỗi lần worker khởi động, chỉ xoá dòng `Remark LIKE 'ESH-16%'` — 🔴 tuyệt đối không đụng nhật ký truyền nhận nghiệp vụ cùng khoảng thời gian (đã có test khoá lại lưới an toàn này).

#### 6d. Dồn ghi cảnh báo về ranh giới tác vụ ExportSubscription — 3 thay đổi hành vi & Bảng rà soát Try-Catch (28/09/2026)

- **Bối cảnh & Tái cấu trúc:** Theo yêu cầu rà soát try-catch chồng chéo trong `DataOutboundService.cs` (`sharedata-don-ghi-canh-bao-ve-ranh-gioi-tac-vu-prompt.md`), việc ghi cảnh báo và nhật ký thất bại chuyển hẳn về ranh giới tác vụ `ExportSubscription`. Các tầng bên dưới (`ExportPage`, `GetExportConfig`) nay chỉ `throw new ShareDataException(...)` thuần để phân loại lỗi, ⛔ tuyệt đối không thực hiện I/O ghi CSDL.
- **Lý do:** Khối `catch` cũ quanh `Extract` trong `ExportPage` vừa bắt nhầm `OperationCanceledException` (dẫn tới việc tắt worker bình thường cũng sinh cảnh báo giả `QueryFailed` ESH-1304) vừa thực hiện ghi CSDL không an toàn (nếu mất kết nối, ngoại lệ mới ghi đè che mất lỗi gốc).
- **Phòng vệ ranh giới:** `ExportSubscription` gọi helper `ShareDataTransferLog.WriteExportFailure(...)` tự bọc try-catch nuốt lỗi ghi, ngăn chặn lỗi I/O làm sập vòng lặp `foreach` của `ProcessSubscriptions`.
- **3 thay đổi hành vi biết trước:**
  1. `PacketNotFound`: Nay ghi nhận kèm đầy đủ tên đối tác (`PartnerName`) trong `ShareDataActivityLog` thay vì null.
  2. Lỗi không xác định: Khối `catch (Exception)` tại boundary nay sinh cảnh báo `QueryFailed` (`ShareDataAlertLog`) kèm nhật ký hoạt động `Failed` thay vì chỉ `LogWarning`.
  3. Lấp lỗ hổng im lặng `GetLastSend`: Các trường hợp ngoại lệ từ `GetLastSend` trước đây im lặng thì nay được boundary tự động ghi nhận đầy đủ.

- **Bảng kết quả rà soát Try-Catch trong `DataOutboundService.cs` (28/09/2026):**

| # | Khối try-catch | Vai trò | Đánh giá |
|---|---|---|---|
| 1 | `ExportSubscription` | Ranh giới tác vụ + `finally { ReleaseLock }` | ✅ Hợp lệ — Nơi duy nhất ghi nhận cảnh báo và xử lý ngoại lệ cấp Subscription. |
| 2 | `ExportPage` quanh `Extract` | Bắt lỗi trích xuất dữ liệu | 🔴 ĐÃ GỠ — Khối thừa gây bắt nhầm `OperationCanceledException` và I/O không an toàn. |
| 3 | `ExportPage` quanh ghi log thành công | Log-write phòng vệ sau khi commit | ✅ Hợp lệ — Phòng vệ để lỗi ghi log không làm rollback dữ liệu đã gửi thành công. |
| 4 | `CommitSuccess` | Quản lý giao dịch cập nhật mốc gửi | ✅ Hợp lệ — Đảm bảo rollback khi có lỗi giao dịch CSDL. |
| 5 | `ReleaseLock` | Nuốt lỗi nhả lock trong `finally` | ✅ Hợp lệ — Không để lỗi nhả lock che mất ngoại lệ gốc của tác vụ. |
| 6 | `GetLastSend` quanh `Insertable` | Xử lý tranh chấp mốc gửi đầu tiên | ✅ Hợp lệ — Đua tranh khoá duy nhất giữa các tiến trình chạy song song. |

#### 7. Ghi nhận cảnh báo tinh gọn (Log 1 lần tại nơi cần thiết)
- Khi bản ghi nguồn thiếu cả `UpdateTime` và `CreateTime`, hệ thống ghi log warning 1 lần cho gói tin (`AlertSource.Packet`), thông báo số dòng phải dùng thời gian nghiệp vụ thay vì throttle phức tạp.
- Khi mapping trường bị thiếu/lỗi, ghi trực tiếp `WriteAlertAsync` 1 lần cho trang/gói kết xuất (đã loại bỏ hoàn toàn hàm tiết chế `LogAlertThrottled`).
- **Tập trung hoá vào `ShareDataTransferLog` (26/09/2026, cập nhật 28/09/2026):** `WriteFailureLogs`/`BuildFailExport` (đổi tên từ `WriteFailureLogsAsync`/`BuildFailExportAsync`) và `ResolvePduType` đã chuyển từ `DataOutboundService.cs` sang `ShareDataTransferLog` — đúng nguyên tắc "1 nơi duy nhất ghi log" đã chốt. Từ 28/09/2026, **việc ghi cảnh báo chuyển hẳn về ranh giới tác vụ** `ExportSubscription`. `ExportPage` / `GetExportConfig` nay chỉ `throw new ShareDataException(...)` để **phân loại lỗi**, ⛔ không ghi CSDL. Boundary gọi qua helper `ShareDataTransferLog.WriteExportFailure` tự nuốt lỗi ghi phòng vệ, bảo vệ an toàn cho vòng lặp `ProcessSubscriptions`. `WriteActivityAsync` giờ tự fallback `PduType = pduType ?? ResolvePduType(sub)` khi caller không truyền, chiều Inbound (`packet.PduType`) không bị ảnh hưởng vì vẫn truyền tường minh. Chi tiết: `sharedata-di-chuyen-writefailurelogs-buildfailexport-resolvepdutype-prompt.md` và `sharedata-don-ghi-canh-bao-ve-ranh-gioi-tac-vu-prompt.md` (xem `Prompt/README.md`).

#### 8. Trạng thái các mục sau rà soát nghiệp vụ 23/09/2026

Nhật ký các việc đã xử lý kèm lý do quyết định: xem §9.5 bên dưới (gộp từ Phụ lục B báo cáo review 23/09, đã xoá).

| Việc | Loại | Trạng thái |
|---|---|---|
| Mở kích hoạt sự kiện cho gói bản chụp (điều kiện là cờ `SendOnNewData`), bổ sung bảng `TollTransactionIn` | prompt (đã xoá) | ✅ Đã xử lý 23/09/2026 |
| Hai bộ phân giải mã gói mâu thuẫn: catalog SQL coi `104_rfidData` là gói 105, `ResolvePacketPolicy` coi là 104 | prompt (đã xoá) | ✅ Đã xử lý 23/09/2026 |
| Gói 104 và 106 là gói chính thức; nhóm "dự kiến làm sau" đã bị bãi bỏ | Chủ dự án xác nhận 23/09/2026 | ✅ Đã làm rõ |
| Lượt chạy đầu của gói nối đuôi: cắm mốc lùi một chu kỳ rồi gửi ngay, bỏ thoát sớm | prompt (đã xoá) | ✅ Đã xử lý 23/09/2026 |
| Chính sách gói tin giữ trong code; đổi mặc định sang `NotReady` + đối soát lúc khởi động (`CheckActivePackets`) | prompt (đã xoá) | ✅ Đã xử lý 23/09/2026 |
| Thống nhất phân giải mã gói ở `ResolveActivePacket` — gói RFID lưu `104_rfidData` nay khớp được tín hiệu `105_rfidData` | prompt (đã xoá) | ✅ Đã xử lý 23/09/2026 |
| Hiển thị chính sách gói tin trên giao diện ở chế độ **chỉ đọc** | Frontend, tách riêng | ⚠️ Chưa làm |
| Gói **106** — quyết định lọc `Source` (allow-list rồi `AND 1=0`) bị bãi bỏ 25/09/2026: 4 trường tải trọng vốn không nằm trong `SELECT` nên đã tự `null`, không cần lọc gì. Gói quay về gửi đủ 7 trường thật | prompt (đã xoá) (dọn 2 bài test còn khoá hành vi cũ) | ✅ Đã xử lý 25/09/2026, ⚠️ còn 2 bài test chờ gỡ |
| ✅ Gói **105** — `QueryPacket105` chuyển sang đọc trực tiếp `TollTransactionIn` kèm xử lý `NULLIF` chuỗi rỗng biển số và `exitTime` null | prompt (đã xoá) | ✅ Đã xử lý 24/09/2026 |
| Gói **105** — tạo bản ghi `105_rfidData` trong `ShareDataPacket` (`104_rfidData` đã xoá 24/09) | Script: [`doc/sql/seed-goi-105-rfiddata.sql`](../doc/sql/seed-goi-105-rfiddata.sql) | ✅ Đã xử lý 24/09/2026 — bản ghi `OrderNo = 5` tạo lúc 10:38 |
| Gói bản chụp từng chưa có giới hạn số dòng — gói 105 từng đọc nguyên bảng `TollTransactionOut`, không `TOP` không `WHERE`. Giới hạn 100 dòng áp từ **tầng Service** (bọc truy vấn con), không đụng `ShareDataPacketSqlCatalog`; sau đó gói 105 đổi hẳn sang đọc `TollTransactionIn` kèm `TOP (@snapshotTop)` riêng, không qua catalog nữa | prompt (đã xoá) × 2; 102/108 không giới hạn, 101 chờ kiểm chứng thực tế | ✅ Đã xử lý 24/09/2026 |
| Thiếu test cho 2 kịch bản sập hệ thống (nhận lại quyền xử lý sau khi worker chết; worker giám sát khởi động lại) | Việc kỹ thuật | ✅ Đã bổ sung 23/09/2026 |
| Test kịch bản mất kết nối CSDL giữa chừng | prompt (đã xoá) | ✅ Đã xử lý 23/09/2026 |
| Dọn dấu vết `104_rfidData` sau khi bản ghi bị xoá khỏi CSDL staging: giữ bí danh làm lưới chặn hồi quy, đổi tên 2 bài test | prompt (đã xoá) | ✅ Đã xử lý 24/09/2026 |
| `DataChangeTrackingService` lặp lỗi vô hạn khi mốc `LastVersion` rơi ra ngoài cửa sổ hợp lệ `CHANGE_TRACKING_MIN_VALID_VERSION` (worker/service ngừng cập nhật mốc lâu hơn 1 ngày retention) | `sharedata-tu-phuc-hoi-change-tracking-min-valid-version-prompt.md` (đã xoá) | ✅ Đã xử lý 25/09/2026, cập nhật tách service 28/09/2026 — verify độc lập tại `DataChangeTrackingService.cs` |
| `ProcessSubscriptions` (overload quét theo lịch; tài liệu từng ghi `ProcessScheduledSubscriptions`/`ProcessBatchSubscriptions` — code thật giữ tên `ProcessSubscriptions` với 2 overload) tạo mới `IServiceScopeFactory` scope + SqlSugar client riêng cho từng subscription trong vòng `foreach` thay vì dùng chung 1 scope cho cả batch | Không có file prompt tương ứng trên đĩa (đường dẫn được ghi trong báo cáo không tồn tại) | ✅ Đã xử lý 25/09/2026 — verify độc lập tại `DataOutboundService.cs:59-61,92` (1 scope duy nhất, `CopyNew()` trong loop) |

#### 9. Đặc tả nghiệp vụ đầy đủ — gộp từ 2 báo cáo review 23/09 (đã xoá sau khi gộp 25/09/2026)

##### 9.1. Hai trục quyết định độc lập: "Gửi cái gì" vs "Khi nào gửi"

| Trục ý nghĩa | Quyết định bởi | Giá trị |
|---|---|---|
| **GỬI CÁI GÌ** | `OutboundPolicy` của gói tin | `Snapshot` (101, 102, 105, 108: gửi lại toàn bộ) hoặc `AlwaysIncremental` (103, 104, 106, 107, 109: chỉ phần phát sinh sau mốc) |
| **KHI NÀO gửi** | Cờ `SendOnNewData` của **từng Subscription** + trigger NATS | Bật → gửi ngay khi Change Tracking phát hiện đổi; tắt → chỉ gửi theo lịch định kỳ |

Hai trục **độc lập hoàn toàn** — đổi trục này không kéo theo trục kia. `SendOnNewData` và `DebounceSec` là cột của `ShareDataSubscription`, không phải của `ShareDataPacket` — cơ chế kích hoạt sự kiện là **năng lực dùng chung cho cả 9 gói hợp lệ** (trừ 110 `NotReady`, 111 `Disabled`), quyền bật/tắt nằm ở từng đăng ký, không phải danh sách trắng cứng theo gói trong code.

> 📌 **Cập nhật 28/09/2026 — Gỡ NextTimeRun khỏi luồng sự kiện:** Trước 28/09/2026, câu truy vấn dùng chung áp vế `NextTimeRun <= now` khiến cờ `SendOnNewData` gần như vô nghĩa do `NextTimeRun` của luồng định kỳ luôn nằm ở tương lai sau mỗi lần chạy. Nay luồng sự kiện không lọc `NextTimeRun`, cho phép gửi ngay lập tức khi có dữ liệu mới. Đánh đổi: luồng sự kiện không còn loại trừ lẫn nhau qua `NextTimeRun` (đối tác có thể nhận trùng 1 trang theo chuẩn at-least-once nếu nhiều worker cùng nhận trigger), và cơ chế OCC lúc `CommitSuccess` bảo toàn mốc gửi `ShareDataLastSend` không bị hỏng hay lùi. Khung giờ cấu hình trong `ScheduleJson` vẫn được kiểm tra tường minh qua `DataOutboundScheduler.IsWithinTimeWindow`.

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
| 4 | Worker / Service giám sát (`DataChangeTrackingWorker` / `DataChangeTrackingService`) khởi động lại | Mốc Change Tracking nằm ở bảng `ShareDataTrackVersion` (§6b) nên worker/service **tiếp tục (resume)** từ mốc của lần chạy trước trên cùng máy, bắt được trigger cho cả phần dữ liệu phát sinh lúc chết. Mốc quá cũ ngoài retention 1 ngày thì rơi về #8 (self-heal nhảy cóc, quét định kỳ gửi bù). 📌 Trước 27/09/2026 mốc là biến trong RAM nên restart là nhảy thẳng tới mốc hiện tại, phần phát sinh lúc chết chỉ còn quét định kỳ gửi bù |
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
- **`CommitSuccess`** — điều kiện OCC `WHERE ID = @subId AND ProcessingUntil = @processingUntil` (chuẩn hóa ngày 28/09/2026, tách khỏi `NextTimeRun`); nếu quyền xử lý bị worker khác giành mất giữa chừng do quá hạn timeout thì rollback, dừng vòng lặp, không ghi đè kết quả worker kia.

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

#### 10. Quyết định đã chốt & phương án bị bác

> 📌 Gộp từ `Phụ lục B` của báo cáo rà soát `Sharedata_Review_TongThe_20260927.md` — **báo cáo đã xoá sau khi
> gộp ngày 28/09/2026**, giống tiền lệ 25/09 với 3 báo cáo trước đó. Đây là phần **đắt nhất** của mọi lượt rà
> soát: xoá đi là lần sau bàn lại từ đầu, nên nó nằm ở sổ theo dõi chứ ⛔ không nằm ở tệp dùng-một-lần.

| Phương án | Vì sao bác |
| --- | --- |
| **Gửi bản ghi xoá mềm sang đối tác** (bỏ bộ lọc ở gói 107, `incidentState` trả mã "đã xoá") | ⛔ **Chủ dự án chốt 27/09/2026: không làm.** Miễn `IsDelete` có giá trị thì không lấy. Đã áp rồi hoàn nguyên sạch. Tương lai nếu đối tác chính thức yêu cầu thì mới thống nhất bộ mã trạng thái rồi bật lại. 📌 Chỉ gói 107 mới khả thi vì đặc tả có mệnh đề *"giá trị cũ sẽ update trạng thái"* và có trường `incidentState`; 103/104/106/109 ⛔ không có trường trạng thái nào nên bỏ lọc là đối tác cộng dữ liệu rác vào số liệu đo |
| **Đưa trạng thái cô lập bảng lỗi xuống CSDL để dùng chung** | Nguồn sự thật vốn đã ở bảng hệ thống của SQL Server ⇒ lưu thêm là **nhân bản dữ liệu**, rồi phải lo hai bản lệch nhau. Khởi động lại không mất gì vì chu kỳ đầu tự phát hiện lại. Dùng chung sẽ **mất** thông tin *máy nào* phát hiện (hữu ích khi chỉ vài máy gặp lỗi quyền hoặc kết nối riêng) |
| **Dùng một mốc hẹn thử lại chung cho mọi bảng bị cô lập** | Mốc chung bị gán lại mỗi lần cô lập thêm bảng ⇒ bảng A hỏng lúc t=0 (hẹn t+5 phút), bảng B hỏng lúc t=2 phút thì mốc bị đẩy thành t+7 phút, bảng A **mất lượt**. Hỏng liên tiếp thì **không bảng nào** được thử lại. Cần mốc hẹn **riêng từng bảng** |
| **Nới cột `LastKey` lên 128** | Khoá bản ghi hiện là khoá chính bảng nguồn, mà cột đó cũng 64 ⇒ nới là phá quy ước và che vấn đề thật. Chọn **thêm chốt chặn `ESH-1305`** tại `ExportPage` trước khi gửi |
| **Tạo bảng riêng hoặc dùng AlertLog để lưu sự cố Change Tracking** | Dùng lại cột `Remark` của `ShareDataActivityLog` (phân loại mã `ESH-16xx`) tận dụng cấu trúc có sẵn, không phải đổi schema CSDL hay giao diện, đồng thời giữ `ShareDataAlertLog` đúng vai trò dành riêng cho lỗi nghiệp vụ cần người can thiệp |
| **Giữ cách khởi tạo cũ của worker giám sát, chấp nhận đọc cấu hình lặp 4 lượt** | ⛔ **Chủ dự án chốt 28/09/2026: đổi sang cách khởi tạo tường minh.** Nguyên nhân gốc của việc lặp: C# **cấm** một khai báo khởi tạo tham chiếu tới thành viên khác của cùng đối tượng, nên 4 chỗ buộc phải tính lại từ đầu, ⛔ không chỗ nào dùng lại được kết quả của chỗ trước. Chỉ cách khởi tạo tường minh mới gán tuần tự được ⇒ còn **1 lượt**. 📌 Đánh giá ban đầu của AI là "không nên làm vì diff trải khắp tệp" — **ước lượng đó sai**, đếm thật chỉ 22 điểm đổi tên |
| **Dựng muộn câu SQL đang áp dụng (chỉ tạo khi dùng lần đầu) để tránh lặp đọc cấu hình** | Vi phạm quy tắc 19.22 (cấm tách trường đệm dựng muộn). Ngoài ra còn **sai chức năng**: một worker khởi động khi mốc trong CSDL đã hợp lệ sẽ đi thẳng vào chu kỳ thường và dùng câu SQL đó **trước khi** nhánh khởi tạo kịp chạy ⇒ buộc phải có giá trị ngay từ lúc khởi tạo |
| **Giữ một cột `NextTimeRun`, phân biệt "lịch" với "đang xử lý" bằng mẹo** *(gộp từ `Sharedata_Review_36CodeChange_20260928.md`, báo cáo đã xoá sau khi gộp 28/09/2026)* | ⛔ **Cả ba cách đều hỏng.** (1) **Phân biệt theo độ lớn** (mốc chiếm quyền ≥ `now+300`, lịch = `now+interval`): đăng ký `daily` có `IntervalSeconds = 86400` ⇒ lịch xa hơn mốc chiếm quyền, sai ngay. (2) **Dùng `State` làm cờ "đang chạy"**: `State` là trạng thái nghiệp vụ và **cả hai luồng đều lọc `State == Active`** ⇒ vỡ cả hai. (3) **Đảo nghĩa `NextTimeRun` thành dấu hiệu đang xử lý, suy lịch từ `LastTimeRun + IntervalSeconds`**: `ComputeNextDailyRun` tính lịch theo **giờ/thứ trong `ScheduleJson`** chứ ⛔ không phải khoảng cách đều ⇒ đăng ký `daily` chạy sai giờ. 🔴 **Gốc rễ:** một giá trị tương lai trong `NextTimeRun` có thể là **lịch** (luồng sự kiện phải chạy) hoặc **đang xử lý** (phải lùi) — **hai hành động ngược nhau**, một cột ⛔ không diễn đạt được. Luồng quét định kỳ sống được với một cột chỉ vì với nó cả hai nghĩa đều dẫn tới cùng hành động *bỏ qua* |
| **Đặt tên cột khoá là `LockedUntil` / `LockExpireTime`** | ⛔ **Chủ dự án chốt 28/09/2026: dùng `ProcessingUntil`.** Từ vựng `lock` quá chung chung cho một cột nằm trong bảng nghiệp vụ — người đọc schema cần hiểu nó nói gì về **đăng ký**, ⛔ không cần biết cơ chế đồng thời bên dưới. 📌 **Lệch từ vựng có chủ đích:** tên **cột** theo nghiệp vụ, còn tên **biến/hàm** của cơ chế giữ nguyên `lock` (`ReleaseLock`, `lockedRows`, `lockDurationSeconds`) theo quy tắc 7 — ⛔ lượt sau không ai được "đồng bộ lại" hai bên |
| **Giữ vế `serialGuard` trong `CommitSuccess` làm lưới thứ hai** *(sau khi đã có `ProcessingUntil`)* | ⛔ **Bác 28/09/2026.** Rà bốn kịch bản thì `ProcessingUntil` bắt hết: hai worker nhận cùng lúc · trigger đến muộn · khoá hết hạn giữa chừng · vòng lặp nhiều trang. ⛔ Không còn kịch bản nào hai worker cùng giữ **cùng một giá trị** `ProcessingUntil` vì lệnh chiếm quyền `WHERE ProcessingUntil IS NULL OR <= now` loại bỏ điều đó theo định nghĩa. 🔴 Giữ lại còn **hại**: nó khiến người đọc tưởng `SerialNbr` gánh vai trò đồng thời rồi ngại đụng vào. 📌 Đánh giá ban đầu của AI là "giữ làm lưới thứ hai" — **sai**, vì lúc đó lưới thứ nhất (CAS trên `NextTimeRun`) còn hở nên lưới thứ hai mới có việc |
| **Bỏ phép cắt `nextRunDeadline` về giây tròn** | SQL Server `DATETIME` chỉ chính xác ~3,33 ms ⇒ giá trị ghi xuống bị làm tròn, so lại với giá trị trong bộ nhớ **không bao giờ khớp** ⇒ OCC hỏng **hoàn toàn**, tệ hơn hẳn vấn đề đang muốn sửa |
| **Gỡ bớt các khối try-catch trong `DataOutboundService`** | Rà 6 khối thì **5 hợp lệ** và ⛔ không được gỡ: ranh giới tác vụ · log-write phòng vệ **sau** khi đã commit · ranh giới giao dịch · nuốt lỗi khi nhả khoá (chạy trong `finally`, văng ra là che mất lỗi gốc) · đua tranh khoá duy nhất ở `GetLastSend`. Chỉ khối quanh `Extract` là thừa và đã gỡ |
| **Gộp khối điều kiện nền của hai luồng về một helper chung** | ⛔ **Chủ dự án chốt 28/09/2026: cố ý để hai bản chép**, cùng với quyết định **cố ý để vùng mã đó không comment**. ⛔ Lượt sau không ai được "dọn cho đẹp" |
| **Dùng primary constructor cho `DataChangeTrackingService`** | ❌ **Bác 28/09/2026.** C# cấm initializer của primary constructor tham chiếu thành viên instance khác, nên cách đó buộc phải gọi `LoadTablePacketMap` **4 lượt** thay vì 1. Giữ constructor tường minh |
| **Đổi vòng lặp phân trang của `ExportSubscription` sang cursor / `GetAsyncEnumerable` / `ForEachAsync` của SqlSugar để "đỡ RAM"** | ❌ **Bác 29/09/2026.** 📌 Các API đó **có thật** trong SqlSugarCore 5.1.4.216 (đã soi assembly) nên ⛔ đừng trả lời "SqlSugar không có" — vấn đề là chúng ⛔ không áp được ở đây. (1) **RAM vốn đã chặn** ở 100 dòng mỗi vòng rồi bỏ, ⛔ không có biến tích luỹ ⇒ ⛔ không có gì để tiết kiệm. (2) Các API đó nằm trên `ISugarQueryable`, mà cả 11 hàm `QueryPacket101..111` là **SQL viết tay** chạy qua `db.Ado` ⇒ muốn dùng phải viết lại hết sang LINQ. (3) 🔴 **Gốc rễ: đây là vòng lặp GIAO DỊCH, ⛔ không phải vòng lặp ĐỌC** — mỗi vòng gửi HTTP sang đối tác rồi **commit mốc gửi** ngay. Cursor sẽ giữ kết nối CSDL mở suốt lúc chờ đối tác, ⛔ không commit mốc giữa chừng được (mất đúng khả năng *"hỏng trang 4 vẫn giữ nguyên 3 trang đã gửi"*), và đóng băng ảnh chụp lúc mở nên bỏ sót dữ liệu đến giữa chừng. **Về batch:** gộp nhiều trang vào một lần gửi là đổi hợp đồng với đối tác (mỗi gói một `SerialNbr`), ⛔ không phải cải tiến miễn phí |

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
  - 📌 **Đối chiếu 28/09**: đây là *việc 4* mà họp 21/09 giao cho Đạt (*"rà soát xử lý lỗi, tách biệt mã lỗi chuẩn
    hoá"*). Phần **tầng Worker đã xong**: `ShareDataAlertCode` (nhóm Outbound `ESH-12xx`/`13xx`/`14xx`, Inbound
    `ESH-15xx`, Tracking `ESH-16xx`) + `ShareDataException` — cả hai nằm trong 33 tệp của nhánh này. ⚠️ Phần **còn
    thiếu** đúng như tiêu đề BE-7 nói: **danh mục Error Code trong cấu hình hệ thống** (bảng CSDL + API tra cứu)
    để FE và người vận hành đọc được mã lỗi thành mô tả, ⛔ không phải tra trong mã nguồn.
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
- [ ] **BE-14** **API danh mục trạng thái ánh xạ của từng gói tin theo đối tác** ⚠️ **Chưa làm**
  - 🔴 **Task này trước 28/09 KHÔNG hề có trong checklist** — nó là *việc 3* mà biên bản họp
    [`21-09-2026`](../doc/transcript/21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md) (mục 2, dòng
    của Đạt) giao rõ: *"Hoàn thiện API danh mục: Cung cấp API trả về trạng thái ánh xạ của từng gói tin theo đối
    tác."* Nó chỉ tồn tại trong báo cáo rà soát 27/09 nên **vô hình với người tra checklist**. Bổ sung vào đây
    ngày 28/09 để không rơi mất nữa.
  - **Dùng để làm gì**: biên bản 21/09 (mục 1.1) chốt giao diện phải hiện chỉ báo *"Đã có Ánh xạ"* (xanh) /
    *"Chưa có Ánh xạ"* (xám) trên bảng gói tin của từng đối tác, để người dùng biết phải thiết lập ánh xạ trước
    khi kích hoạt. FE cần một API danh mục để vẽ chỉ báo đó.
  - **Vì sao chưa làm**: nhánh `feat/20260922-sharedata-service` chỉ chạm tầng Worker và 2 Entity — ⛔ không có
    Controller/Query nào của `Module.ShareData` trong 33 tệp thay đổi.
  - **Có chặn luồng đang chạy không**: ⛔ **Không**. Tầng Worker đã tự chặn gửi khi thiếu ánh xạ (`GetExportConfig`
    ném `MappingNotFound` mức Error), nên dữ liệu ⛔ không thể lọt ra sai. Thiếu API này chỉ làm người dùng
    **không thấy trước** gói nào chưa ánh xạ, phải chờ tới lúc có cảnh báo mới biết.
- [x] **BE-15** **Tách cột khoá riêng `ProcessingUntil` khỏi `NextTimeRun`**: ✅ **xong 28/09/2026**
  - Tách cột `ProcessingUntil` (`DateTime?`) trên entity `ShareDataSubscription` làm cờ chiếm quyền và OCC độc quyền.
  - Phân định triệt để: `NextTimeRun` chỉ mang nghĩa **lịch chạy định kỳ**, `ProcessingUntil` chỉ mang nghĩa **đang xử lý tới mốc này** (`null` = rảnh).
  - Gỡ bỏ hoàn toàn `serialGuard` khỏi `CommitSuccess`. OCC của `CommitSuccess` và `ReleaseLock` nay cùng một khuôn: `WHERE ID == sub.ID && ProcessingUntil == processingUntil`.

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
- [x] **Thông báo khi xóa đăng ký đang chạy đã đọc được.** Mã `lz.exception.sharedata.subscriptionMustPauseBeforeDelete` trước đây hiện nguyên key vì backend chưa có bản dịch; nay ra "Đăng ký đang chạy, vui lòng tắt trước khi xóa." Xong 30/09/2026 (bug TuyenHTN mục 19).
- [x] **Đặt trần 86400 giây cho ô Chu kỳ** (`editSubscription.vue`). Ô `el-input-number` trước đây chỉ có `:min="5"`, thiếu `:max` nên Element Plus kẹp giá trị quá lớn về `Number.MAX_SAFE_INTEGER` (9007199254740991) — vượt `Int32` ⇒ backend ném nguyên văn exception .NET `The JSON value could not be converted to System.Nullable'1[System.Int32]`. Trần 86400 = 1 ngày, vì lịch Liên tục luôn kèm khung giờ nên chu kỳ dài hơn một ngày là vô nghĩa. Xong 01/10/2026 (bug TuyenHTN mục 1 & 2).
- [x] **Khoá ô Mã đối tác khi Sửa** (`editPartner.vue`) — bọc `el-tooltip` + `:disabled="props.operateType === 'edit'"`, bê đúng mẫu của ô Cổng ngay bên dưới; key mới `lz.tooltip.sharedataPartner.codeLocked`. Xong 01/10/2026 (bug TuyenHTN mục 4).
- [x] **Sao chép đối tác: xoá `code` của bản nguồn trong `openDialog`** (`editPartner.vue`). Kèm 2 vá trong `submit`: bỏ `ElMessage.error(e?.message)` gây **toast hiện 2 lần** (interceptor `axios-utils.ts` đã hiện message backend rồi — cùng loại lỗi đã xử ở `dataSource/index.vue`), và chuyển `closeDialog()` ra khỏi `finally` để **lưu hỏng không đóng mất hộp thoại**. Xong 01/10/2026 (bug TuyenHTN mục 3).

## B. Cấu hình Gói tin — `dataSource/index.vue`
- [x] Bỏ cột Bí danh, Vai trò, Kiểu nối, Điều kiện nối; đổi cột Bảng dữ liệu thành "Tệp dữ liệu".
- [x] Tối ưu cuộn ngang và độ rộng cột bảng trường con; cố định cột STT bên trái.
- [x] Dựng UI thêm/sửa gói tin.
- [x] **Bỏ toast generic "Thực hiện thất bại"** ở `handleDeletePacket` và `handleDeleteField` — interceptor `axios-utils.ts` đã hiển thị message thật của backend, toast cục bộ chỉ đè lấp nó. Xong 30/09/2026 (bug TuyenHTN mục 4 & 22).
- [ ] **Làm mờ nút Xóa + tooltip giải thích khi gói tin đang được dùng.** Chưa làm: `ShareDataPagePacketOutput` là class rỗng, chưa trả cờ usage; điều kiện chặn nằm ở `private IsDatatypeInUseAsync()` (`PacketCommandHandler.cs:156` — có hồ sơ ánh xạ **hoặc** có đăng ký còn Alive). Làm được thì phải thêm `IsInUse` vào DTO rồi chạy lại `pnpm build-api`, nên tách Pha 2 chờ đồng bộ với người giữ nhịp regen `api-services/`. Mẫu để bê nguyên: `subscriptionTable.vue` dùng `canToggle()` + `toggleTitle()` → `:disabled` + `el-tooltip`.
  - 🔴 **Cảnh báo nghiệp vụ, cần phản hồi TuyenHTN**: backend ⛔ **không** chặn toàn bộ Sửa. `PacketCodeNameLocked` (`PacketCommandHandler.cs:85-91`) chỉ khoá đổi **Mã/Tên**; `PacketVersion`, `Description`, `OrderNo`, `Status`, `Remark` vẫn sửa được. Nên "ẩn/mờ nút Sửa" như bug mục 22 đề nghị là **sai nghiệp vụ** — đúng là để nút Sửa mở bình thường và disable riêng 2 ô Mã/Tên trong `editPacket.vue`.
- [x] **Nhãn ô tìm kiếm của lưới Trường gói tin khớp tiêu đề cột**: `lz.entity.base.code` ("Mã") → `lz.entity.sharedataDataSource.aliasFieldKey` ("Khóa field"), `lz.entity.base.type` ("Loại") → `lz.entity.sharedataDataSource.fieldType` ("Kiểu"), đổi cả `:label` lẫn tham số của `:placeholder`. Dùng lại đúng key của cột, ⛔ không tạo key mới. Thanh tìm của lưới Gói tin bên trái giữ nguyên Mã/Tên. Xong 01/10/2026 (bug TuyenHTN mục 6).
- [ ] **Danh mục `sharedata_value_type` bổ sung 5 kiểu**: `long`, `float`, `double`, `decimal`, `guid` (trước chỉ có `string`, `int`, `dateTime`, `bool`). 🔴 Đây là **dữ liệu danh mục trong CSDL**, ⛔ không phải lỗi mã nguồn — cả dropdown "Loại" của `editPacketField.vue:58` lẫn "Kiểu dữ liệu phía đối tác" của `editMapping.vue:375` đều đọc chung `getConfigDataByCode(BaseConfigTypeEnum.SharedataValueType)` (bug TuyenHTN mục 7 & 25).
  - ✅ **Phần mã nguồn xong 01/10/2026**: nhánh `guid` đã thêm vào `previewCoerce` của `editMapping.vue` (`long`/`decimal`/`double`/`float` vốn đã có sẵn).
  - ❌ **Phần dữ liệu CHƯA xong**: script [`../sql/20261001-bo-sung-sharedata-value-type.sql`](../sql/20261001-bo-sung-sharedata-value-type.sql) (idempotent, chạy riêng từng môi trường) **chưa được chạy trên staging `10.10.8.30`** — đo 02/10/2026: danh mục vẫn **đúng 4 dòng**. ⇒ Dropdown **vẫn chỉ 4 lựa chọn**, người dùng chưa thấy khác gì.
    - ⚠️ **Số đo này chỉ đúng cho staging.** Các bản triển khai khác (vd `115.78.1.139`) dùng CSDL riêng và **chưa kiểm được** — `mssql_dev` / `mssql_test` đang `CONNECT_TIMEOUT`.
    - 📌 **Tự kiểm không cần đụng CSDL**: mở **Cấu hình gói tin → Thêm Trường gói tin** → đếm dropdown **Loại**. **4** = chưa chạy · **9** = đã chạy.
  - 🔴 **Vì sao để `[ ]`**: sửa mã nguồn xong nhưng triệu chứng tester báo **vẫn còn nguyên**. Chỉ đánh `[x]` sau khi script đã chạy và dropdown hiện đủ **9 lựa chọn**.

## C. Ánh xạ dữ liệu — `mapping/index.vue` & `editMapping.vue`
- [x] Bỏ bộ lọc Định dạng và phiên bản gói tin; chuẩn hoá i18n "Ánh xạ dữ liệu".
- [x] Nút "..." chuyển thành icon `ele-Setting` kèm tooltip; cấu hình lá mở dạng modal chồng độc lập.
- [x] Thêm badge trạng thái CodeSet / Format; nút "Tự động ánh xạ" tách biệt.
- [x] Gom nhóm "Trường Meta hệ thống" trong dropdown chọn trường.
- [x] **Hiện `[mã] tên` ở ô chọn Đối tác và Gói tin** trong modal (helper `codeNameLabel`) — phân biệt bản ghi trùng tên khác mã; `filterable` lọc được cả mã. Xong 30/09/2026 (bug TuyenHTN mục 2 & 18).
- [x] **Ô Mã: nhãn riêng `lz.entity.sharedataMapping.code` ("Mã hồ sơ ánh xạ") + `disabled` cho xám hẳn.** Gỡ `:disabled="true"` đặt sai trên `el-form-item` (component này không có prop đó) và gỡ `rules required` vì ô này để trống cho backend tự sinh. Xong 30/09/2026 (bug TuyenHTN mục 15).
- [x] **Sao chép hồ sơ ánh xạ: xoá `id`, tắt `isActive`, và DỰNG LẠI `code` bằng `syncMappingCode()`** trong `openDialog`, sau khi `GetById` ghi đè. Xong 01/10/2026 (bug TuyenHTN mục 13, 14, 16).
  - 🔴 **Bản sửa ngày 30/09 đặt `code = ''` là SAI và đã gây lỗi nặng hơn — ⛔ lượt sau đừng khôi phục lại.** `MappingCommandHandler.cs:92` bỏ qua kiểm tra trùng khi `Code` rỗng và backend ⛔ **không tự sinh mã**; ô Mã lại đang `disabled` nên người dùng ⛔ không gõ vào được ⇒ sao chép lưu thành công nhưng **ra bản ghi có Mã RỖNG**.
  - **Cách đúng**: mã hồ sơ là **khoá tự nhiên** `{MÃ_ĐỐI_TÁC}_{MÃ_GÓI_TIN}_{CHIỀU}` (`buildMappingCode`). Gọi `syncMappingCode()` để dựng lại theo đối tác/gói tin/chiều hiện tại của form. Sao chép mà ⛔ không đổi một trong ba thứ đó là mâu thuẫn tự thân — backend trả "Mã đã tồn tại trong hệ thống!" là thông điệp **đúng**; đổi Gói tin hoặc Chiều thì `watch` tự dựng mã mới.
  - Thêm dòng gợi ý dưới ô Mã khi `operateType === 'copy'` (key `lz.label.sharedataMapping.codeAutoHint`) để người dùng biết phải đổi cái gì. ⛔ Không mở `disabled` của ô Mã.
- [x] **Câu cảnh báo trường bắt buộc nói rõ "giá trị nguồn rỗng"**: `lz.message.sharedataMapping.requiredFieldEmptyWillBlock` đổi từ *"Trường bắt buộc {field} đang rỗng"* thành *"…đã được ánh xạ nhưng giá trị nguồn trong cơ sở dữ liệu đang rỗng"*. Ảnh issue 20b chứng minh hai trường **đã map**, ảnh 20a cho thấy `weatherId`/`status` NULL trong dữ liệu thật ⇒ ⛔ không phải lỗi ánh xạ. Chỉ sửa chuỗi, ⛔ không đụng `previewLeafValue`. Xong 01/10/2026 (bug TuyenHTN mục 20).

## D. Lịch sử chia sẻ — `history/index.vue`
- [x] Đổi nhãn: "Nhật ký cấu hình" và "Nhật ký truyền nhận".
- [x] Bộ lọc thời gian chuẩn hóa `datetimerange`; bỏ ô lọc "Nội dung". ⚠️ Dòng này từng ghi `[x]` **sai** từ trước: tệp thực tế dùng 2 ô `type="datetime"` rời, và **bộ lọc bị vô hiệu hoàn toàn** — template bind `state.query.fromDate`/`toDate` còn `handleQueryApi()` lại đọc `toIsoRange(state.dateRange)`, nên người dùng chọn ngày nào kết quả cũng không đổi. Gộp về một nguồn `state.dateRange` với một ô `datetimerange` ngày 30/09/2026; đồng thời sửa 3 phím tắt 24h/7 ngày/30 ngày (trả `[Date, Date]` — sai kiểu cho picker đơn) và dọn lệch mặc định 7 ngày vs 3 ngày.
- [x] Double-click dòng mở modal chi tiết `activityDetailDialog.vue` thay cho sidebar; sửa lỗi so sánh enum chuỗi sang số; dựng khung `el-steps` 2 bước cha-con.
- [x] **Chặn chọn thời gian ở tương lai** — thêm `:disabled-date="disableFutureDate"` (cắt tại `dayjs().endOf('day')` để hôm nay vẫn chọn được). Ảnh issue 28a: tester đặt bắt đầu `2030-01-01`, kết thúc `2028-09-30` mà vẫn ra đủ 178 bản ghi. Hai triệu chứng còn lại của issue 28 đã đóng từ 30/09: thứ tự bắt đầu/kết thúc do `datetimerange` tự ràng buộc, và kết quả ngoài phạm vi là do bộ lọc cũ không tới được truy vấn. 📌 Backend ⛔ **không có lỗi** — `ActivityLogQueryHandler.cs` lọc `OccurredAt` đúng ở cả 3 truy vấn (dòng 52-53, 83-84, 116-117). Xong 01/10/2026 (bug TuyenHTN mục 28).

## E. Tooltip đồng bộ toàn module
- [x] Toàn bộ tooltip chuyển sang component chuẩn `el-tooltip effect="dark"`.
- [x] **Tooltip cho nút "Làm mới"** trong `components/advanced/table-header-operation.vue` (dùng chung toàn hệ thống): key `lz.tooltip.base.refreshKeepFilter` — *"Nạp lại dữ liệu, giữ nguyên điều kiện lọc hiện tại. Muốn bỏ lọc thì bấm Đặt lại."* Hai nút cùng icon `ele-Refresh` nhưng khác việc: **Làm mới** nối `@refresh` → `handleQuery` (giữ lọc), **Đặt lại** nối `@reset` (xoá lọc). Thay đổi thuần cộng thêm, ⛔ không đụng hành vi hay nhãn. Xong 01/10/2026 (bug TuyenHTN mục 11).

## F. Câu hỏi chờ phản hồi TuyenHTN — đợt F16 ngày 29/09/2026

🔴 Năm issue dưới đây **⛔ không sửa được bằng mã nguồn** cho tới khi có quyết định nghiệp vụ. Đã rà mã
nguồn đầy đủ ngày 01/10/2026; mỗi mục ghi đúng ba thứ: *hiện trạng · vì sao chưa làm được · cần quyết gì*.
Nguồn gốc: [`../KiemThu/F16-nhat-ky-loi-issue-20260929.md`](../KiemThu/F16-nhat-ky-loi-issue-20260929.md).

- [ ] **Mục 9 — nhãn "Quản lý dự án TCP - V2.0" trên thanh đầu trang.**
  - 🔴 **Bản ghi ngày 01/10 là SAI, ⛔ đừng đi lại đường đó.** Bản đó nói nhãn này là
    `themeConfig.globalTitle` lấy từ `sysTitle`. Sai: `globalTitle` chính là **dòng chữ to ở giữa**
    (`topBar/index.vue:9` → `<h1 class="top-bar-title">`), mà ảnh cho thấy dòng đó **đang đúng**
    (`HỆ THỐNG GIÁM SÁT GIAO THÔNG`). Chuỗi cần xử nằm trong **ô có viền, bo góc, bên TRÁI** — thành
    phần khác hẳn. Sửa `sys_web_title` là làm hỏng đúng dòng đang chạy tốt.
  - **Đã loại trừ chắc chắn** (rà 02/10/2026):

    | Ứng viên | Căn cứ loại trừ |
    | --- | --- |
    | Mã nguồn frontend | Gortex `search text` với `"V2.0"` và `"Quản lý dự án"` → **0 hit** trong `TA-ITS015-WEBVUE-V1.0` |
    | `sys_web_title` (`SysConfig` / `WebConfig`) | Staging = *"Hệ thống giám sát giao thông"* — **chính là dòng chữ to đã đúng** |
    | `VITE_APP_NAME` trong repo | `src/.env.production:5` = `TMS`; `.env` và `.env.development` ⛔ không khai |
    | `SysMenu` cấp 1 trên staging | 48 dòng `Pid = '0'`, ⛔ không dòng nào chứa "TCP" |
    | `incidentNotification.vue` | Chỉ render khi có sự cố; nội dung là loại sự cố + lý trình |
    | `logo/index.vue` | Chỉ còn `<img>`; dòng `<span>{{ globalTitle }}</span>` đã bị comment ở `:4` |

  - **Hai ứng viên còn lại**, cả hai đều là **dữ liệu/cấu hình của môi trường tester**:
    - **(a) Tiêu đề tab trình duyệt** — `utils/other.ts:49-51` dựng
      `document.title = ${appName} | ${webTitle} - ${globalTitle}`, với
      `appName = window.__env__?.VITE_APP_NAME` (sinh vào `public/config.js` **theo từng lần triển khai**).
      Tab trình duyệt đúng là một ô bo góc có viền, chữ nhỏ hơn `<h1>` — khớp hệt ảnh.
    - **(b) Breadcrumb** — `breadcrumb.vue:9` render `$t(v.meta.title)`, `meta.title` đến từ **cây menu
      backend** (`SysMenu.Title`). ⚠️ Điểm trừ: `<style scoped>` của tệp đó ⛔ không khai `border` nào.
  - **Cần quyết**: chạy chẩn đoán theo mục **3.1** của
    [`../Prompt/sharedata-chot-so-phien-ra-soat-0210-prompt.md`](../Prompt/sharedata-chot-so-phien-ra-soat-0210-prompt.md)
    (2 lệnh F12 Console + 4 câu `SELECT`, chạy trên **đúng môi trường tester**): F12 → Console →
    `document.title` và `window.__env__?.VITE_APP_NAME`; nếu không chứa chuỗi đó thì chạy 4 truy vấn,
    hoặc Inspect ô có viền → đọc `class`. Rồi TuyenHTN quyết **đổi thành chữ gì** hay
    **ẩn hẳn** (issue ghi *"Đổi **hoặc ẩn**"*).
  - **Có chặn luồng đang chạy không**: ⛔ Không — chỉ là nhãn hiển thị.
- [ ] **Mục 17 — sửa hồ sơ ánh xạ đang được đăng ký dùng.**
  - **Hiện trạng**: tiêu đề issue mô tả **sai nguyên nhân**. Ảnh b chứng minh *lấy dữ liệu mẫu THÀNH CÔNG*
    (dựng đủ `header` + `data` từ 2 bản ghi thật của `101_commonData`); lỗi
    `lz.exception.sharedata.mappingInUse` chỉ bắn ra lúc **bấm Xác nhận để lưu**.
  - **Cần quyết**: có cho sửa hồ sơ đang được đăng ký dùng không? Nếu **có**, chặn riêng những trường nào
    (đối tác / gói tin / chiều là khoá tự nhiên, đổi là đổi luôn danh tính hồ sơ)? Nếu **không**, giao
    diện nên làm mờ nút Sửa thay vì để người dùng điền xong rồi mới báo lỗi.
  - **Có chặn luồng đang chạy không**: ⚠️ Có — người dùng hiện ⛔ không sửa được hồ sơ đang chạy.
- [ ] **Mục 23 — ẩn nhóm "Trường gói tin" khi đang ở khoá header.**
  - **Hiện trạng**: `editMapping.vue` **⛔ không có khái niệm "nhóm header"**. `header` và `data` là tên
    khoá trong **JSON riêng của từng đối tác**, ⛔ không phải khái niệm hệ thống — lọc theo chữ `header`
    là hardcode magic string (rule 7 cấm). Nặng hơn: `buildPreviewAggregate` (`:1598`) **cố ý** cho lá
    nằm **ngoài** mảng khuôn lấy giá trị của **bản ghi đầu tiên**, kèm chú thích *"đúng như nhánh gộp của
    service"* ⇒ ẩn nhóm đó ở header sẽ làm **frontend lệch khỏi hành vi của service**.
  - **Cần quyết**: (a) chấp nhận để nguyên như hiện nay, hay (b) định nghĩa "header" thành một khái niệm
    hệ thống áp dụng được cho **mọi** đối tác — và nếu chọn (b) thì **service phải đổi cùng lúc**.
  - **Có chặn luồng đang chạy không**: ⛔ Không — chỉ là dropdown hiện nhiều lựa chọn hơn cần thiết.
- [ ] **Mục 24 — kiểu dữ liệu và giá trị mặc định không có tác dụng khi lấy dữ liệu mẫu.**
  - **Hiện trạng**: tìm ra đúng dòng. `buildPreviewNode` (`:1558-1560`) — lá **chưa gắn `fieldKey`** thì
    trả thẳng `parseConst(cfg?.constValue ?? '')`, **⛔ bỏ qua hoàn toàn `targetType` và cả hai giá trị
    mặc định**. Đúng tình huống của ảnh: tester chỉ đặt `Kiểu dữ liệu = string` và
    `Giá trị nội bộ mặc định = 123456` cho `header.sessionId` mà ⛔ không gắn trường gói tin nào.
    Thêm nữa, `previewLeafValue` (`:1502`) **cố ý** chỉ áp `defaultPartnerValue` cho payload **gửi đi**.
  - **Cần quyết**: (1) với lá **chưa gắn trường gói tin**, service có áp `Kiểu dữ liệu phía đối tác` và
    giá trị mặc định không? (2) payload **gửi đi** thì phải lấy *"Giá trị đối tác mặc định"* hay
    *"Giá trị nội bộ mặc định"*? Màn chạy thử phải mô phỏng **đúng** service, nên ⛔ không tự quyết được.
  - **Có chặn luồng đang chạy không**: ⚠️ Có — màn chạy thử đang cho kết quả khác với lúc gửi thật.
- [ ] **Mục 26 — tự sinh GUID cho trường bắt buộc đang NULL.**
  - **Hiện trạng**: cùng họ với mục 20 — dữ liệu nguồn `message`/`status`/`sessionid` của gói
    `104_weatherData` đang NULL, ⛔ không phải lỗi ánh xạ. Mục 20 đã sửa câu cảnh báo cho rõ nghĩa.
  - **Cần quyết**: có thêm **khoá dựng sẵn thứ 5** (`NewGuid`) bên cạnh `Now` / `Serial` / `PacketCode` /
    `PartnerCode` (`editMapping.vue:654-659`) không? Đây là **tính năng mới**: frontend và service phải
    sinh **y hệt** nhau, ⛔ không làm riêng một bên được.
  - **Có chặn luồng đang chạy không**: ⚠️ Có — gói có trường bắt buộc mà nguồn NULL thì bị service chặn.

### ✅ Thuật ngữ "hồ sơ ánh xạ" — đã chốt, chờ chạy script

🔴 **Căn cứ: rule 19.26** (bản 01/10/2026) — Backend ⛔ **không dùng** `TAC_WebAPI/Resources/*.json`;
mọi bản dịch của Backend nằm trên CSDL `SysTerminology`. ⇒ Bảng đó là **nguồn duy nhất**, ⛔ không có
"tệp JSON để đối chiếu" nữa.

✅ **Gỡ key khỏi `Resources/*.json` ⛔ KHÔNG làm mất bản dịch** — kiểm chứng staging 02/10/2026:

| Nhóm key `sharedata.*` | `vi-VN` | `en-US` |
| --- | --- | --- |
| `lz.exception` | **27** ✅ | **27** ✅ |
| `lz.validation` | **7** ✅ | ⚠️ chưa kiểm |
| `lz.message` | **2** ✅ | ⚠️ chưa kiểm |
| `lz.entity` | **23** ✅ | ⚠️ chưa kiểm |

📌 Hai tệp `Resources/vi-VN.json` và `en-US.json` hiện **129 / 69 dòng, 0 key `sharedata`**, `git` sạch —
59 key đưa vào ngày 30/09 đã được gỡ, đúng rule mới.

**Khuyết tật còn lại: 9 dòng `vi-VN` sai chữ.** Bản `en-US` đã sạch (*"mapping profile"*), ⛔ không đụng.

| Nhóm | Key | Vấn đề |
| --- | --- | --- |
| `lz.exception` | `codeSetInUse` · `mappingActiveDuplicated` · `packetCodeNameLocked` · `packetFieldInUse` · `packetFieldLocked` · `packetInUse` | còn chữ **"phễu lọc"** |
| `lz.validation` | `mappingFieldKeyRequired` | 🔴 lượt rà 01/10 chỉ quét `lz.exception` nên **bỏ sót** — dòng này cũng ghi "phễu lọc" |
| `lz.exception` | `mappingConflictInUse` · `mappingInUse` | đã "hồ sơ ánh xạ" nhưng câu cũ lặp chữ *"đang … đang"*, lệch với bản `en-US` trong cùng bảng |

**Căn cứ chọn "hồ sơ ánh xạ"** — nhật ký lỗi F16, tester ⛔ **không một lần nào** viết "phễu lọc":

| Issue | Nguyên văn của tester |
| --- | --- |
| 13 | "Sao chép **ánh xạ dữ liệu** báo lỗi khó hiểu" |
| 14 | "Sao chép **ánh xạ** không cho nhập lại Mã" |
| 17 | "Chỉnh sửa **ánh xạ**, lấy dữ liệu mẫu không thành công" |
| 21 | "Đang có **ánh xạ dữ liệu** thì không cho sửa ⇒ chỉnh câu thông báo" |
| 22 | "Gói tin đã có **hồ sơ ánh xạ** thì không sửa/xóa được" |

Giao diện cũng gọi màn đó là *"Ánh xạ dữ liệu"* ⇒ **chốt "hồ sơ ánh xạ"**, ⛔ không còn là câu hỏi treo.

**Việc còn lại**: chủ dự án chạy
[`../sql/20261002-dong-bo-thuat-ngu-systerminology.sql`](../sql/20261002-dong-bo-thuat-ngu-systerminology.sql)
— seed idempotent cho 9 key (`IF EXISTS → UPDATE ELSE → INSERT`, nên chạy được cả trên môi trường chưa
có dòng). Khối kiểm chứng cuối phải trả **0 dòng** còn chữ "phễu lọc" trên **mọi** nhóm, và in bảng độ
phủ `Name × Lang` để lộ nhóm nào còn thiếu bản `en-US`. ⛔ AI không chạy (rule 9).

📌 Nợ kỹ thuật kèm theo (đã ghi 30/09): bản dịch của `cctvDevice` / `wp` / `vmsTemplate` hiện **chỉ tồn
tại trong `SysTerminology`, ⛔ không có commit nào** — dựng môi trường mới là mất sạch.

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
│    1. Commit (OCC ProcessingUntil, tăng SerialNbr)                    │
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
- **Trạng thái `BuildFailExport` (28/09/2026)**: Sau khi dồn ghi cảnh báo về ranh giới `ExportSubscription`, hàm `ShareDataTransferLog.BuildFailExport` hiện là **dead code (không còn call site nào trong toàn codebase)**, chờ quyết định chính thức từ chủ dự án để dọn sạch. Hàm `WriteFailureLogs` vẫn được giữ nguyên để phục vụ helper `WriteExportFailure` và nhánh `HttpSendFailed`.
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

## 0. Ngữ nghĩa đã khoá bằng test — *số bản tin NATS ≠ số lần gửi HTTP*

Tiêu chí nghiệm thu của cơ chế gửi tức thì là **đối tác nhận đủ dữ liệu**, ⛔ không phải "đếm số bản tin
NATS". Bốn bài test trong `tests/ITS/ShareData/Services/DataChangeTrackingServiceTests.cs` khoá lại
ngữ nghĩa đó (căn cứ: biên bản họp 21/09/2026, mốc `07:08` · `13:36` · `14:31`):

| Ngữ nghĩa được khoá | Bài test |
| --- | --- |
| N thay đổi trên 2 bảng ⇒ đúng **1 bản tin / mã gói** | `Tracker_WhenManyChangesAcrossTwoTables_PublishesOneMessagePerPacketCode_Test` |
| 10 dòng đổi cùng lúc ⇒ đúng **1 request** mang đủ 10 bản ghi | `TriggerFlow_WhenTenRowsChangeAtOnce_SendsOneBatchWithAllRows_Test` |
| Trigger lặp 5 lần ⇒ **gửi 1 lần**, các lần sau báo không có dữ liệu mới | `TriggerFlow_WhenSameTriggerArrivesFiveTimes_SendsDataOnceThenReportsNoNewData_Test` |
| Sửa bản ghi đã gửi ⇒ đối tác chỉ nhận bản mới **nếu mốc gửi tiến lên** | `TriggerFlow_WhenSentRowIsUpdated_PartnerReceivesNewVersionOnlyIfWatermarkAdvances_Test` |

🔴 **Giới hạn đã biết, do bài thứ 4 khoá lại**: sửa một bản ghi **đã gửi** mà **quên nâng `UpdateTime`**
thì mốc gửi ⛔ không tiến, và đối tác **không bao giờ nhận được** bản sửa đó. Đây là hành vi **cố ý** của
cơ chế gửi nối đuôi, ⛔ không phải lỗi — nhưng phải biết khi sửa dữ liệu bằng tay hoặc bằng script.

📌 Ghi nhận 02/10/2026 sau khi rà lại: 4 bài này **đã có và đang bật** từ trước, chỉ là MasterPlan chưa
bao giờ ghi lại.

## 1. Các chốt chặn an toàn bắt buộc (Mandatory Safeguards)
1. **Strict Local Database for `dotnet test`**: Toàn bộ connection string dùng khi chạy test PHẢI trỏ về `local` (`localhost`, `127.0.0.1`, `(localdb)`, `.`). Nếu phát hiện IP remote (ví dụ `10.10.8.30`), HỦY test ngay lập tức.
2. **Không tự ý thực thi DDL/DML**: Mọi thay đổi schema CSDL (các lệnh DDL - Data Definition Language, hoặc DML) chỉ xuất file `.sql` ra đĩa để quản trị viên review.
3. **Quản lý tài liệu tập trung**: Transcript hội thoại và file `Sharedata_MasterPlan.md` này là 2 nguồn thông tin chuẩn xác duy nhất của dự án.
4. 🔴 **Năm tệp TUYỆT ĐỐI KHÔNG được stage** *(gộp từ báo cáo rà soát 28/09/2026, báo cáo đã xoá sau khi gộp)*. Đây là các tệp chỉnh tay cho máy local, lượt commit nào cũng hiện ra trong `git status` và ⛔ không được lẫn vào — đặc biệt nguy hiểm nếu lỡ gõ `git add .`:

   | Repo | Tệp | Vì sao |
   | --- | --- | --- |
   | `TA-ITS015-WEBAPI-V1.0` | `src/TAC_WebAPI/Configuration/Database.json` | 🔴 **Chứa mật khẩu CSDL**, và thường đang bật cờ CodeFirst (`EnableInitDb`/`EnableInitTable`) — phải trả hai cờ này về `false` |
   | `TA-ITS015-WEBAPI-V1.0` | `src/TAC_WebAPI/TAKeyData.key` | 🔴 Tệp khoá |
   | `TA-ITS015-WEBAPI-V1.0` | `src/DLLs/Shared.Core.Security.dll` | Nhị phân dựng tại máy local |
   | `TA-ITS015-WEBVUE-V1.0` | `src/.env` · `src/.env.development` | Cấu hình điểm cuối của máy local |

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
  *(Bao gồm `DataOutboundServiceTests`, `DataChangeTrackingServiceTests` — đúng 2 file. Số lượng test là số liệu tạm, dễ lạc hậu theo từng lần thêm — chạy lệnh trên để xem số hiện tại và tình trạng PASS thay vì tin số đếm cứng trong tài liệu)*.
- **Biên dịch Frontend**:
  ```powershell
  cd TA-ITS015-WEBVUE-V1.0 && npm run build
  ```

### 🔴 Hai cái bẫy khi chạy build/test (gộp từ báo cáo rà soát 28/09/2026, báo cáo đã xoá sau khi gộp)

1. **Phiên gỡ lỗi đang chạy làm vỡ build.** Đang mở phiên debug `TAC_WebAPI` thì build ném `MSB3021` / `MSB3027` vì tệp DLL bị khoá bởi tiến trình `.NET Host` và `Visual Studio Debug Adapter for .NET`. **Cách xử đúng: dừng phiên gỡ lỗi rồi build lại.** ⛔ TUYỆT ĐỐI KHÔNG `Stop-Process -Name dotnet` hay `taskkill /f /im dotnet.exe` — sẽ tắt nhầm WebAPI / Worker / MockServer khác đang chạy (quy tắc 12).
2. **Rà `tests/appsettings.Test.json` TRƯỚC mỗi lượt `dotnet test`.** Chuỗi kết nối phải là `127.0.0.1` / `localhost` / `(localdb)` / `.`; thấy IP remote (ví dụ `10.10.8.30`) thì **huỷ ngay và báo lại**. 📌 Kèm theo: hai cờ `EnableInitDb` / `EnableInitTable` trong tệp đó chỉ bật khi cần đồng bộ schema local, xong phải trả về `false`.

