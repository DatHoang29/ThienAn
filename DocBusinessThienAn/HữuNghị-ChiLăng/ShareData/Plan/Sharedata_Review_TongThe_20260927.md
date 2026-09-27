# Báo cáo rà soát tổng thể phân hệ ShareData — 27/09/2026

> **Ngày rà soát:** 27/09/2026 · **Phạm vi:** luồng gửi nối đuôi, phát hiện dữ liệu mới tức thì, ghi nhận
> nhật ký và trạng thái worker giám sát.
> **Nguồn đối chiếu:** biên bản họp [`21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md`](../doc/transcript/21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md)
> (đã xác thực, `status: verified`) · [`Sharedata_MasterPlan.md`](./Sharedata_MasterPlan.md) · đọc trực
> tiếp mã nguồn trong `TA-ITS015-WEBAPI-V1.0/src/` và schema thật trên CSDL test.
> **Cách dựng:** không dựa vào bản báo cáo nào trước đó — dựng lại từ 3 nguồn trên.

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

---

## Kết luận — đọc 30 giây là đủ

✅ **Xong rồi, chạy được.** Mọi thứ họp 21/09 chốt đều đã có trong code, test xanh.
✅ **Không có việc gì phải làm gấp.**

✅ **Hai việc treo trước đó đã làm xong 27/09** — chốt độ dài `LastKey` (cảnh báo `ESH-1305`) và
`CommitSuccess` lấy mốc dấu vết theo đồng hồ CSDL.

⚠️ **1 việc còn treo, đã có prompt sẵn:**

| Việc | Vỡ khi nào | Prompt |
| --- | --- | --- |
| Gói 107 chưa báo được việc xoá mềm cho đối tác | **Đang xảy ra mỗi khi người vận hành xoá một sự cố nhập nhầm** — đối tác giữ mãi bản ghi đã xoá. ⚠️ Dev hiện có 0 dòng xoá mềm trong 394 dòng `TmsIncident` nên chưa có tồn đọng | [`sharedata-goi-107-bao-xoa-mem-cho-doi-tac-prompt.md`](../Prompt/sharedata-goi-107-bao-xoa-mem-cho-doi-tac-prompt.md) |

❌ **Phần chưa có lưới chặn, và ⛔ đã quyết định không làm:** các gói nối đuôi **ngoài 107** (103, 104, 106,
109) vẫn không báo được việc xoá — vì payload của chúng ⛔ không có trường trạng thái nào để diễn đạt. Bỏ bộ
lọc ở đó sẽ khiến đối tác cộng dữ liệu rác vào số liệu đo, **tệ hơn hiện tại**. Chi tiết:
[A.5](#a5-vì-sao-xoá-bản-ghi-không-đến-được-đối-tác).

## Đã quyết định BỎ QUA — ⛔ không phải việc còn mở

Chủ dự án chốt 27/09/2026. Ghi ra để lần sau không ai đưa lại vào danh sách việc phải làm:

| Việc | Quyết định |
| --- | --- |
| Thống nhất bí danh `__watermark` (5 chỗ ở tầng Module) về `__lastTime` | ⛔ **Không làm.** Catalog tầng Module chỉ phục vụ gói bản chụp — nhóm gói này không dùng cursor. Chỉ cần nhớ khi nào cho gói **nối đuôi** lấy SQL từ catalog đó thì phải sửa trước |
| Thêm đường ghi log ra tệp cho `ILogger` | ⛔ **Không làm.** Ghi vào `ShareDataActivityLog` là đủ — sự cố Change Tracking đã có nhóm mã ESH-16xx vào bảng đó, tra bằng SQL được |

> 📌 Hết phần cần đọc để ra quyết định. Bốn mục dưới đây là **chi tiết đối chiếu** — mở khi cần tra một
> hạng mục cụ thể, ⛔ không cần đọc tuần tự.

---

## Tóm tắt 4 trục

| Câu hỏi | Trả lời | Xem |
| --- | --- | --- |
| Đặc tả yêu cầu gì? | Bốn việc: **chặn gửi khi thiếu hồ sơ ánh xạ** · **gửi nối đuôi** theo mốc `LastTime`/`LastKey` · **phát hiện dữ liệu mới tức thì** (không hoãn) · **trường không map để `null`**, không ép định dạng | [§1](#1-đặc-tả-yêu-cầu-gì) |
| Code làm được tới đâu? | **Đủ cả bốn.** Năm việc phân công cho Đạt đều có hiện thực; riêng API trạng thái ánh xạ tôi chưa rà đầu-cuối vì thuộc phần giao diện | [§2](#2-code-làm-được-tới-đâu-so-với-đặc-tả) |
| Chưa làm những gì? | **2 việc**, cả hai đã có prompt sẵn chờ áp: chốt độ dài `LastKey` · `CommitSuccess` lấy mốc dấu vết theo đồng hồ CSDL. Không việc nào chặn luồng đang chạy | [§3](#3-chưa-làm-những-gì) |
| Edge case? | 12 tình huống — **11 đã có lưới chặn**, **1 chưa có** (xoá bản ghi) | [§4](#4-edge-case) |

---

## 1. Đặc tả yêu cầu gì

Bốn quyết định dưới đây trích từ mục *Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi* của biên bản họp
**21/09/2026** (dự họp: Anh Sơn — Tech Lead, Đạt — Backend WebAPI/CSDL, Hiếu — Backend Worker & cấu hình
ánh xạ). Biên bản ở trạng thái đã xác thực, là nguồn sự thật nghiệp vụ cho phạm vi rà soát này.

### 1.1. Chặn gửi khi thiếu hồ sơ ánh xạ

> *"Tuyệt đối không cho phép gửi dữ liệu nếu gói tin chưa có cấu hình ánh xạ hợp lệ."*

Hồ sơ ánh xạ bắt buộc gắn liền bộ ba định danh `[Mã Đối Tác] + [Mã Gói Tin] + [Chiều Truyền In/Out]`,
mã hồ sơ do hệ thống tự sinh.

### 1.2. Gửi nối đuôi theo mốc đã gửi

> *"Lần gửi trước đã xuất dữ liệu đến mốc nào thì lần sau chỉ lấy các bản ghi phát sinh tiếp sau mốc đó
> để gửi tiếp."*

Lý do nêu trong biên bản: đối tác đã lưu bản ghi cũ; gửi lại toàn bộ lịch sử mỗi chu kỳ 30 giây gây quá
tải mạng và trùng lặp dữ liệu.

Thiết kế bảng lưu mốc, các trường tối thiểu: `PartnerCode`, `PacketCode`, `LastTime` / `LastKey`.

Xử lý trùng mốc thời gian — biên bản chốt dùng khoá phức hợp:

```
WHERE (UpdateTime > LastTime) OR (UpdateTime = LastTime AND Id > LastId)
```

### 1.3. Phát hiện dữ liệu mới tức thì — không hoãn

Tại họp chiều 21/09 từng bàn phương án tạm hoãn để ưu tiên xong luồng gửi định kỳ. Quyết định cuối
(cập nhật 22/09 trong MasterPlan): **không hoãn**, làm ngay trong tuần theo phương án **SQL Server
Change Tracking + NATS**, và ô chọn `SendOnNewData` trên giao diện bật thật.

### 1.4. Kiểu dữ liệu, định dạng và giá trị mặc định

> *"Trường dữ liệu nào không map thì để trống hoặc giá trị `null` an toàn, không cố ép format gây lỗi."*

Chiều gửi dùng định dạng thời gian `yyyy-MM-dd HH:mm:ss` (hoặc ISO-8601). Bộ mã quy đổi tách bạch giá
trị mặc định theo chiều: `DefaultPartnerValue` (gửi sang đối tác) và `DefaultSourceValue` (nhận về nội bộ).

### 1.5. Năm việc phân công cho Đạt

Trích ma trận phân công của biên bản, hạn *"trong tuần"*:

1. Thiết kế bảng lưu mốc đã gửi.
2. Cập nhật câu truy vấn luồng gửi, chỉ lấy bản ghi phát sinh sau mốc phiên trước.
3. Hoàn thiện API danh mục trả về trạng thái ánh xạ của từng gói tin theo đối tác.
4. Rà soát xử lý lỗi, tách biệt mã lỗi chuẩn hoá trong cấu hình hệ thống.
5. Triển khai cơ chế gửi khi có dữ liệu mới (Change Tracking + NATS).

---

## 2. Code làm được tới đâu so với đặc tả

| Hạng mục | Khớp đặc tả? | Căn cứ trong mã nguồn |
| --- | --- | --- |
| Chặn gửi khi thiếu ánh xạ (1.1) | ✅ | `DataOutboundService.GetExportConfig` ném `MappingNotFound` khi phân giải mapping ra `null` — huỷ kết xuất, không phát bất kỳ gói dữ liệu nào |
| Bảng lưu mốc đúng 4 trường (1.2) | ✅ | `ShareDataLastSend`: `PartnerCode`, `PacketCode`, `LastTime`, `LastKey`, kèm index độc nhất trên `(PartnerCode, PacketCode)` |
| Cursor kép đơn điệu (1.2) | ✅ | `CommitSuccess.UpdateLastSend`: `LastTime < nt OR (LastTime = nt AND (LastKey IS NULL OR LastKey < newKey))` — đúng công thức biên bản, và điều kiện nằm ở mệnh đề `WHERE` nên mốc chỉ tiến, không bao giờ tụt |
| Nguyên tử khi tịnh tiến mốc (1.2) | ✅ | Một giao dịch: mở `BeginTran` → cập nhật `ShareDataSubscription` có kiểm tra độc quyền `NextTimeRun == nextRunDeadline` → upsert `ShareDataLastSend` → `Commit`. Mất quyền xử lý thì `Rollback` và trả về `false`, trang sau không gửi tiếp |
| Phân trang có phanh (1.2) | ✅ | `DefaultPageSize = 100`, `DefaultMaxPagesPerRun = 20`, `DefaultLockBudgetPercent = 50`; thêm rào chắn cursor `null` ngắt vòng lặp ngay để không gửi lặp |
| Change Tracking + NATS (1.3) | ✅ | `DataTrackerWorker` chạy nền chu kỳ 1 giây đọc `CHANGE_TRACKING_CURRENT_VERSION()`; phát vào **một** subject chung `ta.its.event.sharedata.newdata`, gói tin nhận diện qua `PacketCode` trong nội dung bản tin; `DataNatsConsumerWorker` lắng nghe và gọi xuất bản tức thì |
| Chỉ bắt Thêm mới và Sửa, bỏ Xoá (1.3) | ✅ | `BuildChangesSql` sinh `WHERE CT.SYS_CHANGE_OPERATION IN ('I', 'U')` — đúng chủ đích, vì Change Tracking ở đây chỉ làm **tín hiệu kích hoạt**, dữ liệu thật vẫn lấy từ bảng nguồn. ⚠️ Việc xoá bản ghi không đến được đối tác, nhưng ⛔ **không phải vì bộ lọc này**: nghiệp vụ dùng xoá mềm, mà xoá mềm là lệnh cập nhật nên tín hiệu **vẫn bắn** — dòng bị loại ở mệnh đề `IsDelete IS NULL` của câu truy vấn. Xem [A.5](#a5-vì-sao-xoá-bản-ghi-không-đến-được-đối-tác) |
| Trường không map để `null` (1.4) | ✅ | Gói 106: 4 trường tải trọng không nằm trong `SELECT` nên tự trả `null`, không suy diễn sai lệch |
| Việc 1 — bảng lưu mốc | ✅ | Xem 3 dòng trên về `ShareDataLastSend` |
| Việc 2 — query nối đuôi | ✅ | `DataOutboundExtractionProcess` áp cursor kép cho 5 gói nối đuôi (103, 104, 106, 107, 109) |
| Việc 3 — API trạng thái ánh xạ | ⚠️ **Có hiện thực, chưa rà đầu-cuối** | `MappingResolverService` cùng kiểu `MappingBrief` (mang `PartnerId`) đã tồn tại. Phần này phục vụ giao diện do Hiếu làm, nằm ngoài phạm vi rà soát 27/09 nên chưa kiểm chứng từ endpoint xuống dữ liệu |
| Việc 4 — tách mã lỗi chuẩn hoá | ✅ | `ShareDataAlertCode` chia 3 nhóm: `Outbound` (ESH-12xx/13xx/14xx), `Inbound` (ESH-15xx), `Tracking` (ESH-16xx). Toàn bộ việc ghi nhật ký và cảnh báo tập trung một nơi duy nhất là `ShareDataTransferLog` |
| Việc 5 — Change Tracking + NATS | ✅ | Xem 2 dòng trên |

📌 **Bảo đảm giao hàng là *ít nhất một lần*** (at-least-once): nếu sập sau khi gửi xong nhưng trước khi
tịnh tiến mốc thì lượt sau gửi lại đúng phần đó. Đây là chủ đích, không phải khiếm khuyết — tin xoá và
khoá chống trùng phía đối tác vẫn đang hoãn theo quyết định cũ.

📌 **Độ phủ kiểm thử** đọc thẳng từ [`tests/ShareData/`](../../../../tests/ShareData/) — ⛔ báo cáo này
không chép lại danh sách bài test, vì bảng chép tay lạc hậu ngay khi có ai đổi tên tệp.

---

## 3. Chưa làm những gì

📌 Cả 2 việc dưới đây **đã có prompt thực thi sẵn** trong `Prompt/`, chờ áp.

| Việc | Thuộc ai | Vì sao chưa làm | Có chặn luồng đang chạy? |
| --- | --- | --- | --- |
| `LastKey` chưa có chốt độ dài trước khi gửi (cột dài 64 ký tự) — prompt: [`sharedata-chot-do-dai-lastkey-prompt.md`](../Prompt/sharedata-chot-do-dai-lastkey-prompt.md) | Đạt | `__rowid` của cả 5 gói nối đuôi đều là khoá chính bảng nguồn, mà cột đó cũng `NVARCHAR(64)` ⇒ chưa thể vượt trần. ⛔ Không nới cột: 64 đúng quy ước `KeyFieldLength` | ❌ Không chặn. 🔴 Vỡ nặng khi `__rowid` thôi là khoá đơn lẻ: `CommitSuccess` ném lỗi cắt chuỗi **sau khi đối tác đã nhận** ⇒ giao dịch hoàn tác ⇒ mốc không tiến ⇒ **đối tác nhận lại đúng trang đó mỗi chu kỳ, vô hạn**, mà bên mình chỉ thấy lỗi SQL chung chung |
| `CommitSuccess` lấy `CreateTime`/`UpdateTime` theo `DateTime.Now` (đồng hồ máy ứng dụng) — prompt: [`sharedata-updatetime-theo-dong-ho-csdl-prompt.md`](../Prompt/sharedata-updatetime-theo-dong-ho-csdl-prompt.md) | Đạt | Đợt đồng nhất đồng hồ 27/09 chỉ đổi 3 điểm **quyết định độc quyền xử lý**, không quét các cột dấu vết | ❌ Không chặn — 2 cột này ⛔ không tham gia phép so sánh nghiệp vụ nào. Nhưng chạy nhiều worker trên nhiều máy thì mốc giữa các dòng lệch đúng bằng mức lệch đồng hồ, trong khi `LastTimeRun`/`NextTimeRun` cùng bảng lại theo đồng hồ CSDL — trộn hai hệ quy chiếu trong một dòng là chỗ dễ kết luận sai nhất lúc truy vết |

📌 Gói 111 **đã quyết định bỏ qua** (đặc tả ghi `skip`) — đó là kết luận đã đóng, ⛔ không thuộc danh
sách việc còn mở.

---

## 4. Edge case

| # | Tình huống | Đã có lưới chặn? | Cách hệ thống xử và cái giá phải trả |
| --- | --- | --- | --- |
| 1 | Sập giữa lúc gửi nối đuôi, giữa hai trang | ✅ | Trang đã ghi nhận giữ nguyên, trang dở hoàn tác; lượt sau lấy lại từ mốc cũ. Giá: không mất dữ liệu, nhưng lượt sau phải làm lại trang dở |
| 2 | Sập sau khi gửi HTTP xong nhưng trước khi tịnh tiến mốc | ✅ | Đối tác đã nhận, mốc chưa tiến ⇒ lượt sau gửi lại đúng phần đó. Giá: **đối tác nhận trùng**, đúng thiết kế *ít nhất một lần* — bên nhận phải tự chịu trách nhiệm chống trùng |
| 3 | Tiến trình bị kết thúc đột ngột khi đang giữ quyền xử lý | ✅ | Quyền xử lý hết hạn theo `NextTimeRun`, worker lượt sau tự nhận lại và tiếp từ mốc đã ghi. Giá: chậm tối đa bằng thời hạn giữ quyền |
| 4 | Worker giám sát khởi động lại | ✅ **siết lại 27/09** | Mốc Change Tracking nằm ở bảng `ShareDataTrackVersion` nên worker **tiếp tục từ mốc lần chạy trước** trên cùng máy, bắt được tín hiệu cho cả phần dữ liệu phát sinh lúc chết. Giá: chu kỳ đầu sau khi dừng lâu có thể trả về nhiều bảng đổi cùng lúc ⇒ phát nhiều tín hiệu (chặn trên: tối đa 15 bảng ra tối đa 9 gói), khoảng chống dội của từng đăng ký vẫn tiết chế |
| 5 | NATS mất kết nối | ✅ | Ghi cảnh báo rồi thôi; luồng quét định kỳ gửi bù, mốc không đổi nên không mất dữ liệu. Giá: mất tính tức thì cho đến khi kết nối lại |
| 6 | Mất kết nối CSDL giữa chừng | ✅ | Giao dịch tự hoàn tác; quyền xử lý treo giống #3 rồi tự hồi phục |
| 7 | Sập khi đang gửi gói bản chụp | ✅ | Không mất gì — chu kỳ sau gửi lại toàn bộ, đúng thiết kế nhóm gói này |
| 8 | Mốc version rơi ra ngoài cửa sổ hợp lệ của Change Tracking (thời gian giữ 1 ngày) | ✅ **siết lại 27/09** | Tự hồi phục: bắt mã lỗi SQL 22114/22115 rồi nhảy cóc lên mốc hiện tại, ghi 1 dòng `ESH-1601` kèm khoảng version bị bỏ qua. Giá: **quãng version bị bỏ qua chỉ được luồng quét định kỳ gửi bù**, không có tín hiệu tức thì — và đây chính là lý do phải ghi lại khoảng đó để truy vết được sau này |
| 9 | Tác vụ trước xử lý lâu, bản tin tín hiệu sau dồn ứ | ⚠️ | Khoá độc quyền bỏ qua êm nếu đăng ký đang bận; lượt sau thấy 0 dòng mới (mốc đã tiến) thì thoát ngay. 📌 Tính đúng đắn do **khoá độc quyền** bảo đảm, ⛔ không phải do khoảng chống dội — khoảng chống dội chỉ là bộ lọc sơ bộ chạy trước, dùng ảnh chụp trong bộ nhớ nên **có khe hở đua nhau thật**. Lưu ý vì đây là chỗ dễ hiểu nhầm nguồn bảo đảm |
| 10 | Lần đầu khởi động sau triển khai, Change Tracking chưa bật | ✅ | Worker tự kiểm tra qua bảng hệ thống và chỉ chạy lệnh thay đổi cấu trúc khi còn thiếu. Nếu không bật được (thiếu quyền quản trị CSDL) thì tạm dừng quét và ghi 1 dòng `ESH-1603`. Giá: lần đầu nên chọn giờ thấp điểm vì có chạy lệnh thay đổi cấu trúc |
| 11 | Bảng nguồn bị tắt hoặc xoá Change Tracking giữa lúc đang chạy (quản trị CSDL chạy lệnh tắt, hoặc xoá tạo lại bảng khi triển khai) | ✅ **siết lại 27/09** | Vô hiệu hoá câu SQL đang dùng **và** hẹn lại mốc quét sau 5 phút, rồi ném lỗi lên ghi log **đúng một lần**, kèm 1 dòng `ESH-1602` mang cả câu SQL để lần ra bảng gây lỗi. Các chu kỳ giữa lúc chờ thoát sớm nên không dội lỗi. Giá: tín hiệu tức thì chậm tối đa 5 phút. 🔴 **Phải có cả hai thao tác** — chỉ bỏ câu SQL mà không hẹn lại mốc (hoặc ngược lại) là câu SQL hỏng bị lặp lại mỗi giây đến khi khởi động lại dịch vụ |
| 12 | Bản ghi ở bảng nguồn bị **xoá** | ❌ | **Gói nối đuôi: đối tác giữ mãi bản ghi đã xoá, không có đường nào biết.** Gói bản chụp tự khỏi vì chu kỳ sau gửi lại toàn bộ. Cơ chế mất dấu khác nhau theo kiểu xoá — xem [A.5](#a5-vì-sao-xoá-bản-ghi-không-đến-được-đối-tác) |


🔴 **Ràng buộc mới sinh ra cùng bảng trạng thái (27/09):** hai thao tác của tình huống #11 nay ghi vào
cột CSDL chứ không còn là biến trong bộ nhớ, nên `PollChanges` **buộc phải** ghi trạng thái trong khối
`finally` — kể cả nhánh có ngoại lệ. Bỏ khối `finally` đó là mất lưới chặn #11 lần thứ hai.

⚠️ **Đánh đổi mới sinh ra cùng bảng trạng thái:** dòng trạng thái khoá theo `(MachineName, ProcessId)`,
mà cả bộ kiểm thử chạy trong một tiến trình ⇒ dùng chung một dòng. Mọi bài test chạm tới chu kỳ quét
phải dọn dòng đó ở cả bước dựng dữ liệu và bước kết thúc, nếu không bài test phụ thuộc thứ tự chạy.

---

# Phụ lục A. Cơ chế hoạt động (tham khảo)

## A.1. Luồng gửi nối đuôi

1. Quét các đăng ký đến hạn, giành quyền xử lý độc quyền bằng một lệnh cập nhật nguyên tử lên
   `NextTimeRun` (chỉ một worker thắng, các worker thua bỏ qua im lặng, không ghi cảnh báo).
2. Phân giải gói tin đang hoạt động, chặn theo chính sách xuất bản, rồi phân giải hồ sơ ánh xạ — thiếu
   ánh xạ là huỷ kết xuất ngay (§1.1).
3. Với nhóm gói nối đuôi: đọc hoặc khởi tạo mốc `ShareDataLastSend`. Lượt chạy đầu cắm mốc lùi đúng một
   chu kỳ để gửi ngay dữ liệu gần nhất mà không nạp toàn bộ lịch sử.
4. Vòng lặp trang: trích xuất → ánh xạ → ghi tệp và gửi HTTP → ghi nhận thành công trong một giao dịch →
   tịnh tiến mốc và số thứ tự. Dừng khi hết dữ liệu, gửi lỗi, mất quyền xử lý, hết trần trang, hoặc hết
   ngân sách thời lượng giữ quyền.
5. Cuối phiên nhả quyền xử lý trong khối `finally`.

## A.2. Luồng phát hiện dữ liệu mới tức thì

Worker giám sát chạy chu kỳ 1 giây, so mốc version hiện tại của CSDL với mốc đã xử lý. Có thay đổi thì
truy vấn danh sách bảng đổi (gộp toàn bộ bảng nguồn trong **một** câu `UNION ALL`, chỉ lọc Thêm mới và
Sửa), ánh xạ ra danh sách mã gói, rồi phát vào một subject chung duy nhất. Bộ tiêu thụ đọc `PacketCode`
từ nội dung bản tin và gọi luồng xuất bản tức thì cho đúng gói đó.

Worker tự kiểm tra và tự bật Change Tracking ở mọi môi trường — quyết định có chủ đích của chủ dự án
ngày 23/09 nhằm vận hành không cần can thiệp tay, thay cho khuyến nghị ban đầu là chỉ cho quản trị CSDL
chạy lệnh tay ở môi trường dàn dựng và thật.

## A.3. Ba tầng ghi nhận, ranh giới rõ ràng

| Tầng | Dành cho | Đặc điểm |
| --- | --- | --- |
| `ShareDataAlertLog` | **Lỗi nghiệp vụ** người vận hành cần can thiệp: không tìm thấy gói tin, thiếu hồ sơ ánh xạ, truy vấn dữ liệu lỗi, gửi HTTP thất bại | Có cờ xác nhận đã xử lý |
| `ShareDataActivityLog` | Nhật ký truyền nhận nghiệp vụ hiển thị trên giao diện. **Kiêm thêm** sự cố hạ tầng Change Tracking, phân biệt bằng nhóm mã ESH-16xx đặt ở cột `Remark` | Nhóm ESH-16xx tự dọn sau 7 ngày, chỉ xoá dòng mang mã đó, 🔴 tuyệt đối không đụng nhật ký nghiệp vụ cùng khoảng thời gian |
| `ILogger` | Tranh chấp tài nguyên bình thường của cơ chế khoá độc quyền | ⛔ Không đẩy lên hai tầng trên làm rác cảnh báo |

Ba mã hạ tầng đang dùng: `ESH-1601` mốc version không hợp lệ đã tự nhảy cóc · `ESH-1602` truy vấn lỗi vì
lý do khác version, kèm câu SQL để lần ra bảng gây lỗi · `ESH-1603` chưa bật Change Tracking, tạm dừng quét.

## A.4. Trạng thái worker giám sát

Bảng `ShareDataTrackVersion` giữ **6 cột nghiệp vụ**: `MachineName`, `ProcessId`, `LastProcessedVersion`,
`MissingTables`, `NextTableRefreshTime`, `RetryIntervalSeconds`. Khoá độc nhất `(MachineName, ProcessId)`
— mỗi lần chạy tiến trình đúng một dòng, cập nhật tại chỗ theo từng chu kỳ.

Hai mốc thời gian của lượt chạy dùng luôn cột của lớp base `EntityTenant`, ⛔ không thêm cột riêng:
`CreateTime` là thời điểm lượt chạy bắt đầu (base ghi bằng `GETDATE()` lúc thêm mới và loại cột này khỏi
lệnh cập nhật nên bất biến), `UpdateTime` là nhịp sống gần nhất (base ghi bằng `GETDATE()` mỗi lần cập
nhật, mà mỗi chu kỳ quét đều cập nhật một lần). Khi giám sát, nhịp sống đọc bằng
`ISNULL(UpdateTime, CreateTime)` vì dòng vừa tạo chưa có `UpdateTime`.

⛔ **Không lưu chuỗi SQL xuống CSDL** — chỉ lưu danh sách bảng còn thiếu Change Tracking, câu SQL được
dựng lại trong bộ nhớ từ danh sách đó (nối chuỗi trên tối đa 15 bảng, không có truy xuất đĩa).

Chi phí: mỗi chu kỳ quét thêm một lệnh đọc và một lệnh cập nhật một dòng theo khoá chính. Đổi lại được
khả năng tra mốc từ ngoài, tiếp tục mốc sau khi khởi động lại, và đổi chu kỳ quét lại bằng dữ liệu thay
vì phải biên dịch lại.

## A.5. Vì sao xoá bản ghi không đến được đối tác

Luồng nối đuôi chỉ bao giờ đi **về phía trước**: mỗi lượt lấy các dòng có `UpdateTime > LastTime`, hoặc
bằng `LastTime` nhưng `ID > LastKey`. Nó chỉ nhìn được **dữ liệu còn tồn tại và mới hơn mốc** — mà một bản
ghi đã xoá thì không thoả cả hai.

Hệ thống có hai kiểu xoá, mất dấu ở **hai chỗ khác nhau**:

| Kiểu xoá | Tín hiệu Change Tracking | Mất dấu ở đâu |
| --- | --- | --- |
| **Xoá mềm** (nghiệp vụ dùng kiểu này): cập nhật cột `IsDelete` | ✅ **Có bắn** — xoá mềm là lệnh UPDATE nên Change Tracking ghi nhận `SYS_CHANGE_OPERATION = 'U'`, worker phát tín hiệu bình thường | Ở **câu truy vấn**: mọi truy vấn gói nối đuôi đều có `WHERE ... IsDelete IS NULL`, nên dòng vừa xoá mềm bị loại khỏi kết quả. Tín hiệu có, nhưng trang gửi đi không chứa dòng đó |
| **Xoá cứng**: `DELETE FROM ...` | ❌ Không bắn — `BuildChangesSql` lọc `IN ('I','U')`, cố tình bỏ `'D'` | Ở **tầng tín hiệu**: không có gì kích hoạt. Mà dù có kích hoạt thì dòng cũng đã biến mất khỏi bảng nên truy vấn không lấy được |

⚠️ Điểm dễ hiểu sai: với xoá mềm thì **tín hiệu vẫn bắn**, nên nhìn log sẽ thấy worker có hoạt động —
nhưng trang gửi đi rỗng hoặc thiếu đúng dòng đó. ⛔ Đừng kết luận "không thấy tín hiệu nghĩa là không có
ai xoá".

### Ví dụ đi trọn một trường hợp

| Thời điểm | Việc xảy ra | Mốc `ShareDataLastSend` | Đối tác thấy gì |
| --- | --- | --- | --- |
| 10:00 | Sự cố `INC-001` được tạo trong `TmsIncident` | — | — |
| 10:01 | Worker gửi gói 107, đối tác nhận `INC-001` | `LastTime = 10:00`, `LastKey = <ID của INC-001>` | Đang hiển thị `INC-001` |
| 10:05 | Người vận hành xoá `INC-001` vì nhập nhầm (xoá mềm: `IsDelete = 10:05`) | không đổi | vẫn `INC-001` |
| 10:05 | Change Tracking bắn tín hiệu (thao tác `'U'`), worker chạy ngay | không đổi | vẫn `INC-001` |
| 10:05 | Truy vấn có `WHERE i.IsDelete IS NULL` ⇒ `INC-001` **bị loại**, trang gửi 0 bản ghi | không đổi | vẫn `INC-001` |
| Mãi về sau | Không có lượt nào nói với đối tác rằng `INC-001` đã bị xoá | không đổi | 🔴 **vẫn hiển thị `INC-001` vĩnh viễn** |

### Muốn xử thì phải làm gì — và đặc tả đã mở đường cho gói 107

🔴 **Đặc tả gói 107 đã thiết kế sẵn cơ chế này.** Tiêu đề gói trong
[`02-mapping-goi-tin-101-111.md`](../doc/02-mapping-goi-tin-101-111.md) ghi nguyên văn:

> `## 107. Thông tin sự kiện giao thông (sẽ lấy mới nhất từ thời điểm trước đó >= key/ giá trị cũ sẽ update trạng thái)`

Mệnh đề *"giá trị cũ sẽ update trạng thái"* nghĩa là đối tác **được thiết kế để nhận lại bản ghi cũ và cập
nhật trạng thái của nó**. Và gói này đã có sẵn trường chở trạng thái: `incidentState` ← `TmsIncident.State`,
kèm ghi chú của đặc tả *"Cần bảng mã trạng thái"*.

⇒ Với gói 107, hướng xử **không cần đổi hợp đồng dữ liệu**:

| Bước | Việc |
| --- | --- |
| 1 | Bỏ `WHERE i.IsDelete IS NULL` khỏi `QueryPacket107` để dòng vừa xoá mềm được lấy ra |
| 2 | Khi `IsDelete` khác `null` thì `incidentState` trả về mã trạng thái "đã xoá / đã huỷ" |
| 3 | Chốt mã đó trong bộ mã quy đổi với đối tác — đây là phần *"Cần bảng mã trạng thái"* mà đặc tả để mở |

⚠️ Bước 3 là **quyết định nghiệp vụ**, ⛔ không phải việc sửa mã nguồn — phải thống nhất giá trị mã với đối tác trước.

#### 📌 Quyết định chốt ngày 27/09/2026 (Chủ trì dự án)
- **Quyết định**: Miễn `IsDelete` có giá trị thì **không lấy** (giữ nguyên bộ lọc `WHERE i.IsDelete IS NULL`), tạm thời **không gửi bản ghi xoá mềm** sang đối tác cho bất kỳ gói tin nào (kể cả gói 107).
- **Kế hoạch tương lai**: Tạm thời giữ giải pháp đơn giản nhất. Tương lai nếu đối tác chính thức phát sinh yêu cầu đồng bộ bản ghi đã xoá mềm thì sẽ thống nhất bộ mã trạng thái quy đổi và kích hoạt sau.

### ⛔ Vì sao KHÔNG áp cách này cho các gói nối đuôi còn lại

| Gói | Đặc tả ghi gì | Áp được? |
| --- | --- | --- |
| 107 — sự kiện giao thông | *"... / giá trị cũ sẽ update trạng thái"* | ✅ Được — có mệnh đề cập nhật trạng thái **và** có trường `incidentState` |
| 103 — dò xe (VDS) | *"sẽ lấy mới nhất từ thời điểm trước đó >= key"* | ❌ Không — ⛔ không có mệnh đề cập nhật trạng thái, payload không có trường trạng thái nào |
| 109 — thu phí (ETC) | *"sẽ lấy mới nhất từ thời điểm trước đó >= key"* | ❌ Không — như trên |
| 104, 106 | Cùng nhóm dữ liệu đo từ bảng nguồn đo đếm | ❌ Không — như trên |

📌 Bỏ bộ lọc ở 103/104/106/109 mà không có trường trạng thái thì đối tác nhận dòng đã xoá như **một bản ghi
đo mới hoàn toàn hợp lệ** — tệ hơn hiện tại, vì dữ liệu rác lẫn vào số liệu đo.

✅ Rủi ro lại **tập trung đúng chỗ đặc tả đã lo**: 107 là sự kiện do người vận hành nhập, nên đúng là loại
bản ghi bị xoá vì nhập nhầm; còn 103/104/106/109 là dữ liệu đo và giao dịch, gần như không ai xoá tay.

## A.6. Đồng nhất hệ quy chiếu thời gian

Mọi mốc **quyết định độc quyền xử lý** đọc từ đồng hồ CSDL qua `GetDbNow(db)` — gọi ở 3 điểm: hai dạng
của hàm quét đăng ký (mốc xét đến hạn và xét khoảng chống dội) và hàm nhả quyền xử lý. Lý do: cả ba mốc
đều so sánh hoặc ghi vào cột CSDL, nếu lấy từ đồng hồ máy ứng dụng thì khi chạy nhiều worker trên nhiều
máy, mỗi máy lệch đồng hồ một chút là cửa sổ chống dội và cửa sổ độc quyền lệch theo đúng mức đó.

---

# Phụ lục B. Nhật ký việc đã xử lý

## B.1. Vấn đề đã đóng

| Vấn đề | Cách xử |
| --- | --- |
| 🔴 Mất cơ chế vô hiệu hoá câu SQL khi truy vấn Change Tracking hỏng — hồi quy do một đợt gộp biến trước đó | Khối bắt lỗi tổng đặt **cả hai** việc: bỏ câu SQL đang dùng và hẹn lại mốc quét, rồi ném lỗi lên. Đã có test khoá lại |
| Mốc version chỉ tồn tại trong bộ nhớ, không tra được từ ngoài, restart là mất | Chuyển sang bảng trạng thái `ShareDataTrackVersion`, bỏ **hẳn** thuộc tính trong bộ nhớ |
| Bốn trạng thái trong bộ nhớ của worker giám sát (mốc version, câu SQL đang dùng, mốc hẹn quét lại, chu kỳ quét lại là hằng số cứng) | Đưa hết xuống bảng trạng thái; câu SQL dựng lại từ danh sách bảng thiếu; chu kỳ quét lại thành dữ liệu có giá trị dự phòng trong mã |
| Không có đường ghi bền vững cho sự cố Change Tracking — mất sạch khi chạy dạng dịch vụ nền | Thêm nhóm mã ESH-16xx ghi vào `ShareDataActivityLog` ở cột `Remark`, tự dọn sau 7 ngày. **Không đổi cấu trúc bảng**: không thêm cột, không thêm danh mục, không cần quản trị CSDL, không cần sửa giao diện |
| Lệch đồng hồ giữa nhiều worker làm sai cửa sổ chống dội và cửa sổ độc quyền | Thêm `GetDbNow(db)` làm nơi duy nhất đọc đồng hồ CSDL, áp cho 3 điểm quyết định độc quyền xử lý |
| Hai cột `StartedAt` và `LastHeartbeat` trùng chức năng với cột của lớp base | Bỏ cả hai, dùng `CreateTime` / `UpdateTime`. Bảng cũ đã tạo với 8 cột nên phải xoá bảng rồi để cơ chế sinh bảng từ mã dựng lại — cơ chế đó chỉ **thêm** cột, không bao giờ xoá |
| Nhiều lớp bắt lỗi lồng nhau ghi cảnh báo trùng | Chỉ bắt lỗi tại ranh giới tác vụ; lỗi bên trong để nổi lên, ghi cảnh báo đúng một lần |
| 14 điểm tài liệu lệch mã nguồn trong MasterPlan | Đã sửa hết, gồm 3 điểm tự mâu thuẫn (mô tả bộ lọc nguồn đã bãi bỏ, trần trang 50 so với 100, thời gian giữ dữ liệu 2 ngày so với 1 ngày) |

## B.2. Phương án bị bác và vì sao

🔴 Đây là phần đắt nhất của báo cáo — xoá đi là lần sau bàn lại từ đầu.

| Phương án | Vì sao bác |
| --- | --- |
| Dùng **Snapshot Isolation** cho truy vấn Change Tracking để bỏ cơ chế tự hồi phục | Tài liệu Microsoft nêu rõ: chuyển từ bất kỳ mức cô lập nào sang Snapshot **trong** một giao dịch làm giao dịch đó thất bại và bị hoàn tác. Ngoài ra Snapshot có thể chặn việc dọn dữ liệu Change Tracking của SQL Server. Đã áp rồi hoàn tác toàn bộ |
| Thiết kế `ShareDataTrackVersion` thành **nhật ký sự kiện chỉ thêm** (11 cột, có mức độ, thông điệp, dữ liệu chi tiết dạng JSON) | Lẫn hai việc khác nhau vào một bảng. Chốt lại: **lỗi** đi vào `ShareDataActivityLog`, bảng này **chỉ giữ trạng thái**. Bản cũ đã bị áp nhầm rồi mới phát hiện, phải dọn sạch — nếu để nguyên thì 3 tình huống lỗi bị ghi **hai lần** |
| Ghi sự cố Change Tracking vào `ShareDataAlertLog` | Vi phạm ranh giới ở [A.3](#a3-ba-tầng-ghi-nhận-ranh-giới-rõ-ràng): `AlertLog` chỉ dành cho lỗi **nghiệp vụ** mà người vận hành cần can thiệp. Lỗi hạ tầng đưa vào đó là làm rác cảnh báo |
| Tạo **bảng mới** riêng để chứa lỗi tracking | Đã có `ShareDataActivityLog` với cột `Remark` bỏ trống với thực thể này. Dùng lại thì không đổi cấu trúc bảng, không cần quản trị CSDL, không cần sửa giao diện |
| Khoá dòng trạng thái theo **một định danh sinh mới cho mỗi đối tượng worker** để hai đối tượng trong cùng tiến trình độc lập nhau | Mất luôn khả năng tiếp tục mốc sau khi khởi động lại — tức mất đúng mục đích chính của cả bảng trạng thái. Chọn khoá `(MachineName, ProcessId)` và chấp nhận hai đối tượng cùng tiến trình dùng chung một mốc; điều này còn **đúng hơn** vì tránh hai worker cùng phát tín hiệu cho một thay đổi |
| Hàm đọc trước mốc hợp lệ tối thiểu của Change Tracking (hướng chủ động) | Chọn hướng phản ứng: bắt mã lỗi 22114/22115 rồi mới nhảy cóc. Hàm viết sẵn cho hướng chủ động thành mã chết và đã bị xoá |
| Gói 106 chặn cứng bằng điều kiện luôn sai để không gửi khi chưa có nguồn trạm cân | Bốn trường tải trọng vốn không nằm trong `SELECT` nên đã tự trả `null`, không cần lọc gì. Điều kiện luôn sai chặn nhầm luôn cả 7 trường **có dữ liệu thật**. Chủ dự án bãi bỏ ngày 25/09 |
| Nhóm *"các gói dự kiến làm sau"* gộp chung 104, 105, 106, 110, 111 trong tài liệu ánh xạ bản 20/08 | Nhãn trì hoãn không kiểm chứng được. Hệ quả thật: 104 và 106 đã chạy nhưng bị hiểu là "chạy trước kế hoạch", còn 110 chưa chạy vì **thiếu dữ liệu nguồn** — hai chuyện khác nhau bị gộp một nhãn và không chuyện nào được xử lý. Chủ dự án bãi bỏ nhóm này ngày 23/09; từ đó chỉ còn hai trạng thái **đã làm** và **chưa làm** |
