# Báo cáo rà soát tổng thể phân hệ ShareData — 27/09/2026

> **Phạm vi:** luồng gửi nối đuôi, phát hiện dữ liệu mới tức thì, ghi nhận nhật ký, trạng thái worker giám sát.
> **Nguồn:** biên bản họp [`21-09-2026`](../doc/transcript/21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md)
> (đã xác thực) · đọc trực tiếp mã nguồn `TA-ITS015-WEBAPI-V1.0/src/`.
> **Rà lại lần 2 cuối ngày 27/09** sau đợt refactor worker giám sát.

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

✅ **Ba việc bạn chỉ định đều đã làm đúng:**

| Việc | Kết quả đối chiếu code |
| --- | --- |
| Chốt độ dài `LastKey` | ✅ Chốt nằm trong `ExportPage`, chặn **trước khi gửi HTTP**, ghi cảnh báo `ESH-1305`. Giữ độ dài 64 theo chuẩn, ⛔ không nới cột |
| Mốc dấu vết theo đồng hồ CSDL | ✅ `GetDbNow(db)` là nơi duy nhất đọc đồng hồ CSDL, gọi ở **4 điểm**: 2 dạng quét đăng ký, `CommitSuccess`, `ReleaseLock` |
| Không gửi bản ghi xoá mềm | ✅ Đã hoàn nguyên **sạch**: `QueryPacket107` giữ `IsDelete IS NULL`, `incidentState` trả về `State` gốc. ⛔ Không còn mảnh vụn nào (mã trạng thái `Deleted` và bài test tương ứng đều đã gỡ) |

🔴 **Một hồi quy MỚI phát sinh từ đợt refactor worker giám sát — chưa có ai xử:**

| Việc | Vỡ khi nào |
| --- | --- |
| Mất cơ chế chặn khi một bảng nguồn bị tắt Change Tracking giữa lúc đang chạy | Ngay khi quản trị CSDL tắt Change Tracking một bảng, hoặc xoá-tạo lại bảng lúc triển khai. Câu SQL sinh **một lần lúc khởi tạo** nên không tự loại bảng hỏng ⇒ lỗi lặp **mỗi giây vô hạn**, mỗi lần ghi 1 dòng `ESH-1602` ⇒ rác nhật ký, luồng tín hiệu tức thì **chết hoàn toàn**. ⚠️ Khởi động lại dịch vụ **không** khắc phục được |

> 📌 Hết phần cần đọc để ra quyết định. Bốn mục dưới là chi tiết đối chiếu, ⛔ không cần đọc tuần tự.

---

## Tóm tắt các trục

| Câu hỏi | Trả lời | Xem |
| --- | --- | --- |
| Đặc tả yêu cầu gì? | Bốn việc: **chặn gửi khi thiếu hồ sơ ánh xạ** · **gửi nối đuôi** theo mốc `LastTime`/`LastKey` · **phát hiện dữ liệu mới tức thì** (không hoãn) · **trường không map để `null`** | [§1](#1-đặc-tả-yêu-cầu-gì) |
| Code làm được tới đâu? | **Đủ cả bốn**, cộng ba việc bạn chỉ định trong ngày 27/09 | [§2](#2-code-làm-được-tới-đâu) |
| Chưa làm những gì? | **1 việc**: khôi phục cơ chế chặn bảng mất Change Tracking giữa lúc chạy | [§3](#3-chưa-làm-những-gì) |
| Edge case? | 12 tình huống — **11 có lưới chặn**, **1 vừa mất lưới** (đúng việc ở §3) | [§4](#4-edge-case) |

---

## 1. Đặc tả yêu cầu gì

Trích mục *Quyết định kỹ thuật cốt lõi* của biên bản **21/09/2026** (Anh Sơn — Tech Lead, Đạt — Backend
WebAPI/CSDL, Hiếu — Backend Worker). Biên bản đã xác thực, là nguồn sự thật nghiệp vụ.

**1.1. Chặn gửi khi thiếu hồ sơ ánh xạ** — *"Tuyệt đối không cho phép gửi dữ liệu nếu gói tin chưa có cấu
hình ánh xạ hợp lệ."* Hồ sơ ánh xạ gắn bộ ba `[Đối tác] + [Gói tin] + [Chiều In/Out]`, mã tự sinh.

**1.2. Gửi nối đuôi** — *"Lần gửi trước đã xuất dữ liệu đến mốc nào thì lần sau chỉ lấy các bản ghi phát
sinh tiếp sau mốc đó để gửi tiếp."* Bảng lưu mốc tối thiểu `PartnerCode`, `PacketCode`, `LastTime`,
`LastKey`; xử lý trùng mốc bằng khoá phức hợp:

```
WHERE (UpdateTime > LastTime) OR (UpdateTime = LastTime AND Id > LastId)
```

**1.3. Phát hiện dữ liệu mới tức thì — không hoãn.** Chốt cuối: làm ngay trong tuần bằng SQL Server
Change Tracking + NATS, ô chọn `SendOnNewData` bật thật trên giao diện.

**1.4. Kiểu dữ liệu** — *"Trường dữ liệu nào không map thì để trống hoặc giá trị `null` an toàn, không cố
ép format gây lỗi."*

---

## 2. Code làm được tới đâu

| Hạng mục | Khớp đặc tả? | Căn cứ |
| --- | --- | --- |
| Chặn gửi khi thiếu ánh xạ (1.1) | ✅ | Phân giải ra `null` là huỷ kết xuất, không phát gói nào |
| Bảng lưu mốc đúng 4 trường (1.2) | ✅ | `ShareDataLastSend` + index độc nhất `(PartnerCode, PacketCode)` |
| Cursor kép chỉ tiến, không tụt (1.2) | ✅ | Điều kiện đơn điệu nằm ở mệnh đề `WHERE` của lệnh cập nhật mốc |
| Nguyên tử khi tịnh tiến mốc (1.2) | ✅ | Một giao dịch: cập nhật đăng ký có kiểm tra độc quyền → ghi mốc → xác nhận. Mất quyền thì hoàn tác, trang sau không gửi |
| Phân trang có phanh (1.2) | ✅ | 100 dòng/trang, tối đa 20 trang/lượt, 50% thời lượng giữ quyền; có rào chắn cursor `null` |
| Change Tracking + NATS (1.3) | ✅ | Worker quét 1 giây/lần, phát vào **một** subject chung, gói tin nhận diện qua mã trong nội dung bản tin |
| Chỉ bắt Thêm mới và Sửa, bỏ Xoá (1.3) | ✅ | Đúng chủ đích — Change Tracking chỉ làm **tín hiệu kích hoạt**, dữ liệu thật vẫn lấy từ bảng nguồn |
| Trường không map để `null` (1.4) | ✅ | Gói 106: 4 trường tải trọng không nằm trong câu truy vấn nên tự trả `null` |
| **Chốt độ dài `LastKey`** | ✅ | Chặn trước khi gửi, cảnh báo `ESH-1305` |
| **Mốc dấu vết theo đồng hồ CSDL** | ✅ | `GetDbNow(db)`, 4 điểm gọi |
| **Không gửi bản ghi xoá mềm** | ✅ | Giữ bộ lọc ở cả 5 gói nối đuôi, hoàn nguyên sạch |
| API danh mục trạng thái ánh xạ | ⚠️ Có hiện thực, **chưa rà đầu-cuối** | Thuộc phần giao diện do Hiếu làm, ngoài phạm vi rà soát này |

📌 Bảo đảm giao hàng là **ít nhất một lần**: sập sau khi gửi xong nhưng trước khi tịnh tiến mốc thì lượt
sau gửi lại phần đó. Chủ đích, không phải khiếm khuyết.

📌 Độ phủ kiểm thử đọc thẳng từ [`tests/ShareData/`](../../../../tests/ShareData/) — ⛔ báo cáo không chép
lại danh sách bài test.

---

## 3. Chưa làm những gì

| Việc | Thuộc ai | Vì sao chưa làm | Có chặn luồng đang chạy? |
| --- | --- | --- | --- |
| Khôi phục cơ chế chặn khi bảng nguồn mất Change Tracking giữa lúc đang chạy | Đạt | Đợt refactor worker giám sát gộp hết trạng thái về **một cột `LastVersion`**, bỏ luôn hai thứ từng tạo nên lưới chặn: danh sách bảng thiếu và mốc hẹn quét lại | ❌ Chưa chặn **lúc bình thường**. 🔴 Nhưng khi tình huống xảy ra thì **chặn toàn bộ luồng tín hiệu tức thì**, và ⚠️ khởi động lại dịch vụ không khắc phục được — xem [§4](#4-edge-case) mục 11 |

---

## 4. Edge case

| # | Tình huống | Có lưới chặn? | Cách xử và cái giá phải trả |
| --- | --- | --- | --- |
| 1 | Sập giữa hai trang khi gửi nối đuôi | ✅ | Trang đã ghi nhận giữ nguyên, trang dở hoàn tác. Giá: lượt sau làm lại trang dở |
| 2 | Sập sau khi gửi xong nhưng trước khi tịnh tiến mốc | ✅ | Lượt sau gửi lại. Giá: **đối tác nhận trùng** — bên nhận tự chống trùng |
| 3 | Tiến trình bị kết thúc đột ngột khi đang giữ quyền xử lý | ✅ | Quyền tự hết hạn, worker sau nhận lại. Giá: chậm tối đa bằng thời hạn giữ quyền |
| 4 | Worker giám sát khởi động lại | ✅ | Mốc nằm ở bảng `ShareDataTrackVersion` (**đúng 1 dòng toàn hệ thống**) nên tiếp tục từ mốc cũ. Giá: chu kỳ đầu sau khi dừng lâu có thể bắn nhiều tín hiệu; khoảng chống dội của từng đăng ký vẫn tiết chế |
| 5 | NATS mất kết nối | ✅ | Ghi cảnh báo rồi thôi, quét định kỳ gửi bù. Giá: mất tính tức thì tới khi nối lại |
| 6 | Mất kết nối CSDL giữa chừng | ✅ | Giao dịch tự hoàn tác, quyền xử lý treo giống #3 rồi tự hồi phục |
| 7 | Sập khi đang gửi gói bản chụp | ✅ | Không mất gì — chu kỳ sau gửi lại toàn bộ |
| 8 | Mốc version rơi ngoài cửa sổ hợp lệ (giữ 1 ngày) | ✅ | Tự nhảy cóc lên mốc hiện tại, ghi `ESH-1601` kèm khoảng bị bỏ qua. Giá: **quãng đó chỉ được quét định kỳ gửi bù**, không có tín hiệu tức thì |
| 9 | Tác vụ trước chạy lâu, bản tin tín hiệu sau dồn ứ | ⚠️ | Khoá độc quyền bỏ qua êm nếu đăng ký đang bận. 📌 Tính đúng đắn do **khoá độc quyền** bảo đảm, ⛔ không phải do khoảng chống dội — cái đó dùng ảnh chụp bộ nhớ nên có khe hở đua nhau thật |
| 10 | Lần đầu khởi động sau triển khai, chưa bật Change Tracking | ✅ | Worker tự kiểm tra và tự bật; không bật được thì tạm dừng quét, ghi `ESH-1603`. Giá: nên chọn giờ thấp điểm cho lần đầu |
| 11 | **Bảng nguồn bị tắt hoặc xoá Change Tracking giữa lúc đang chạy** | ❌ **vừa mất lưới 27/09** | Câu SQL truy vấn thay đổi được sinh **một lần duy nhất lúc khởi tạo worker** từ cấu hình, nên ⛔ không có đường tự loại bảng hỏng. Việc kiểm tra và tự bật lại Change Tracking **chỉ chạy ở nhánh khởi tạo** (khi mốc còn âm) ⇒ sau khi đã chạy thì không bao giờ kiểm lại. Hệ quả: lỗi lặp **mỗi giây vô hạn**, mỗi lần ghi 1 dòng `ESH-1602`. 🔴 Luồng tín hiệu tức thì chết hoàn toàn cho tới khi quản trị CSDL bật lại Change Tracking cho bảng đó, hoặc gỡ bảng đó khỏi cấu hình. ⚠️ Khởi động lại dịch vụ **không** khắc phục |
| 12 | Bản ghi nguồn bị **xoá mềm** | ✅ *(theo quyết định)* | Bộ lọc `IsDelete IS NULL` giữ nguyên ⇒ ⛔ không gửi bản ghi đã xoá. Giá: **đối tác giữ bản ghi đã xoá** — đã chấp nhận, xem [Phụ lục B](#phụ-lục-b-quyết-định-đã-chốt-và-phương-án-bị-bác) |

---

# Phụ lục A. Cơ chế hoạt động (tham khảo)

**A.1. Luồng gửi nối đuôi.** Quét đăng ký đến hạn → giành quyền xử lý độc quyền bằng một lệnh cập nhật
nguyên tử → phân giải gói tin và hồ sơ ánh xạ (thiếu ánh xạ là huỷ ngay) → với nhóm nối đuôi thì đọc mốc
đã gửi → vòng lặp trang: trích xuất, ánh xạ, ghi tệp và gửi, ghi nhận trong một giao dịch, tịnh tiến mốc
→ cuối phiên nhả quyền trong khối `finally`.

**A.2. Luồng phát hiện dữ liệu mới.** Worker quét 1 giây/lần, so mốc version của CSDL với mốc đã xử lý.
Version không tăng thì thoát ngay, ⛔ không tốn thêm truy vấn nào. Có tăng thì truy vấn danh sách bảng đổi
(gộp toàn bộ bảng nguồn trong **một** câu `UNION ALL`, chỉ lọc Thêm mới và Sửa), ánh xạ ra danh sách mã
gói, phát vào một subject chung.

📌 **Danh sách bảng nguồn nạp động từ cấu hình** `appsettings.json` khoá `ShareDataTracker:TablePacketMap`
— ⛔ không còn là bảng ánh xạ cứng trong mã. Câu SQL truy vấn thay đổi được sinh **một lần lúc khởi tạo**
worker từ danh sách đó (⚠️ đây chính là gốc của tình huống 11 ở §4).

**A.3. Ba tầng ghi nhận.**

| Tầng | Dành cho |
| --- | --- |
| `ShareDataAlertLog` | **Lỗi nghiệp vụ** người vận hành cần can thiệp. Có cờ xác nhận đã xử lý |
| `ShareDataActivityLog` | Nhật ký truyền nhận nghiệp vụ. **Kiêm** sự cố hạ tầng Change Tracking, phân biệt bằng nhóm mã `ESH-16xx` ở cột `Remark`, tự dọn sau 7 ngày |
| `ILogger` | Tranh chấp tài nguyên bình thường của khoá độc quyền. ⛔ Không đẩy lên hai tầng trên làm rác cảnh báo |

Ba mã hạ tầng: `ESH-1601` mốc version không hợp lệ đã tự nhảy cóc · `ESH-1602` truy vấn lỗi vì lý do khác
version · `ESH-1603` chưa bật Change Tracking, tạm dừng quét.

**A.4. Trạng thái worker giám sát.** Bảng `ShareDataTrackVersion` nay chỉ còn **một cột nghiệp vụ duy nhất
`LastVersion`**, và **đúng một dòng cho toàn hệ thống**. Không còn khoá theo máy hay tiến trình, không còn
danh sách bảng thiếu, không còn mốc hẹn quét lại.

📌 Nhờ rút gọn này, ràng buộc *"phải ghi trạng thái trong khối `finally`"* của bản trước **không còn cần
thiết** — chu kỳ quét chỉ ghi mốc ở đường thành công và ở nhánh tự nhảy cóc, không còn trạng thái nào
buộc phải lưu trên đường lỗi.

**A.5. Đồng nhất hệ quy chiếu thời gian.** Mọi mốc ghi vào hoặc so sánh với cột CSDL đều đọc từ đồng hồ
CSDL qua `GetDbNow(db)` — 4 điểm gọi. Lý do: chạy nhiều worker trên nhiều máy, mỗi máy lệch đồng hồ một
chút là cửa sổ chống dội và cửa sổ độc quyền lệch theo đúng mức đó. ⛔ Không dùng đồng hồ máy ứng dụng.

---

# Phụ lục B. Quyết định đã chốt và phương án bị bác

🔴 Phần đắt nhất của báo cáo — xoá đi là lần sau bàn lại từ đầu.

| Phương án | Vì sao bác |
| --- | --- |
| **Gửi bản ghi xoá mềm sang đối tác** (bỏ bộ lọc ở gói 107, `incidentState` trả mã "đã xoá") | ⛔ **Chủ dự án chốt 27/09/2026: không làm.** Miễn `IsDelete` có giá trị thì không lấy. Đã áp rồi hoàn nguyên sạch. Tương lai nếu đối tác chính thức yêu cầu thì mới thống nhất bộ mã trạng thái rồi bật lại. 📌 Chỉ gói 107 mới khả thi vì đặc tả có mệnh đề *"giá trị cũ sẽ update trạng thái"* và có trường `incidentState`; 103/104/106/109 ⛔ không có trường trạng thái nào nên bỏ lọc là đối tác cộng dữ liệu rác vào số liệu đo |
| **Nới cột `LastKey` lên 128** | Khoá bản ghi hiện là khoá chính bảng nguồn, mà cột đó cũng 64 ⇒ nới là phá quy ước và che vấn đề thật. Chọn **thêm chốt** chặn trước khi gửi |
| Bỏ hẳn lệnh gán mốc dấu vết, phó thác cho lớp base tự ghi | Chưa kiểm chứng được lớp base có áp cho lệnh cập nhật theo cột chỉ định hay không; nếu không thì mốc **không bao giờ cập nhật** — tệ hơn lệch đồng hồ. Chọn đọc đồng hồ CSDL tường minh |
| Dùng **Snapshot Isolation** cho truy vấn Change Tracking để bỏ cơ chế tự hồi phục | Tài liệu Microsoft nêu rõ: chuyển sang Snapshot **trong** một giao dịch làm giao dịch đó thất bại và bị hoàn tác. Ngoài ra Snapshot có thể chặn việc dọn dữ liệu Change Tracking. Đã áp rồi hoàn tác toàn bộ |
| Thiết kế bảng trạng thái worker thành **nhật ký sự kiện chỉ thêm** | Lẫn hai việc vào một bảng. Chốt lại: **lỗi** đi vào `ShareDataActivityLog`, bảng kia **chỉ giữ trạng thái** |
| Ghi sự cố Change Tracking vào `ShareDataAlertLog` | Vi phạm ranh giới ở [A.3](#phụ-lục-a-cơ-chế-hoạt-động-tham-khảo): `AlertLog` chỉ dành cho lỗi **nghiệp vụ** cần người can thiệp |
| Tạo **bảng mới** riêng để chứa lỗi tracking | Cột `Remark` của `ShareDataActivityLog` đang bỏ trống với thực thể này. Dùng lại thì không đổi cấu trúc bảng, không cần quản trị CSDL, không cần sửa giao diện |
| Hàm đọc trước mốc hợp lệ tối thiểu (hướng chủ động) | Chọn hướng phản ứng: bắt mã lỗi rồi mới nhảy cóc. Hàm viết sẵn thành mã chết và đã bị xoá |
| Gói 106 chặn cứng bằng điều kiện luôn sai | 4 trường tải trọng vốn không nằm trong câu truy vấn nên đã tự trả `null`. Điều kiện luôn sai chặn nhầm cả 7 trường **có dữ liệu thật**. Bãi bỏ 25/09 |
| Nhóm *"các gói dự kiến làm sau"* trong tài liệu ánh xạ bản 20/08 | Nhãn trì hoãn không kiểm chứng được: 104 và 106 đã chạy thật lại bị hiểu là chạy trước kế hoạch, còn 110 chưa chạy vì **thiếu dữ liệu nguồn** — hai chuyện khác nhau gộp một nhãn, không chuyện nào được xử lý. Bãi bỏ 23/09 |
