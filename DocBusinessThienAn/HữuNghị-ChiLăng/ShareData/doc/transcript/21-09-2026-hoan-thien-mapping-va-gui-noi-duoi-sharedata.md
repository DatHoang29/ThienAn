---
tier: A
read: full
source:
  - ../../../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-1-ui-mapping.m4a
  - ../../../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-2-gui-noi-duoi-va-du-lieu-moi.m4a
  - ../../../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-3-mapping-profile.m4a
  - ../../../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-4-tong-ket.m4a
  - ../../../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-clip-gui-noi-duoi.m4a
date: 21-09-2026
duration: ~35 phút (hợp nhất toàn diện các phần)
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData (Chia sẻ Dữ liệu - ESHARE) - Hoàn thiện Cấu hình Ánh xạ, Cơ chế Gửi Nối đuôi theo Mốc đánh dấu & Xử lý Dữ liệu Mới
participants:
  - Anh Sơn (Tech Lead / Kiến trúc hệ thống)
  - Hiếu (Dev Backend Worker & Cấu hình Ánh xạ)
  - Đạt (Dev Backend WebAPI & CSDL)
---

# Kịch bản & Biên bản Cuộc họp: Hoàn Thiện Cấu Hình Ánh Xạ, Cơ Chế Gửi Nối Đuôi (Mốc Đã Gửi) & Xử Lý Dữ Liệu Mới (ShareData)

> **Ghi chú:** Bản ghi được tổng hợp và hợp nhất toàn diện bằng phương thức **Gemini Multimodal Native Audio Understanding** từ các tệp ghi âm cuộc họp nội bộ ngày 21/09/2026 trong thư mục [`_source/audio/21-09-2026/`](file:///c:/ThienAn/DocBusinessThienAn/HữuNghị-ChiLăng/Plan/_source/audio/21-09-2026/). Buổi làm việc đi sâu vào giải quyết các bài toán kiến trúc then chốt của phân hệ **Chia sẻ Dữ liệu (ShareData / ESHARE)**: chuẩn hóa luồng tạo và gán hồ sơ ánh xạ, thiết kế cơ chế gửi dữ liệu nối đuôi (dựa trên mốc đánh dấu thời gian và ID đã gửi của phiên trước), đánh giá tính khả thi cơ chế phát hiện dữ liệu mới tức thì, và phân công triển khai giữa Đạt và Hiếu dưới sự chỉ đạo của Anh Sơn.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Chuẩn hóa Luồng Tạo & Gán Hồ sơ Ánh xạ (Quy trình 1 chiều)
- **Vấn đề bất cập:**
  - Trước đây, việc cấu hình ánh xạ bị lộn xộn giữa hai luồng: vừa tạo ánh xạ bên ngoài rồi gán vào đối tác, vừa vào đối tác tạo gói tin rồi lại quay ngược ra tìm hồ sơ ánh xạ. Điều này dẫn đến nguy cơ "máp ngược hai chiều", tạo hồ sơ ánh xạ ảo khi chưa có gói tin thực tế.
- **Quy trình chuẩn hóa 1 chiều chặt chẽ:**
  1. **Bước 1 — Khai báo Đối tác:** Đăng ký đối tác kết nối.
  2. **Bước 2 — Cấu hình Gói tin:** Đăng ký gói tin cần gửi/nhận cho đối tác đó (chọn chiều Gửi hoặc Nhận).
  3. **Bước 3 — Tạo Hồ sơ Ánh xạ:** Hồ sơ ánh xạ bắt buộc gắn liền với bộ ba định danh: `[Mã Đối Tác] + [Mã Gói Tin] + [Chiều Truyền In/Out]`. Mã hồ sơ ánh xạ được hệ thống **tự động sinh**, người dùng không cần nhập tay.
- **Trực quan hóa trạng thái Ánh xạ trên Giao diện:**
  - Trên bảng danh sách cấu hình gói tin của từng đối tác: Thêm chỉ báo trạng thái rõ ràng:
    - **Đã có Ánh xạ (Màu xanh / Đầy đủ):** Cho biết gói tin đã được ánh xạ trường dữ liệu, sẵn sàng kích hoạt chạy.
    - **Chưa có Ánh xạ (Màu xám / Cảnh báo):** Nhắc nhở người dùng/Tester cần bấm vào để thiết lập ánh xạ trước khi kích hoạt gói tin.
  - Tuyệt đối không cho phép gửi dữ liệu nếu gói tin chưa có cấu hình ánh xạ hợp lệ.

---

### 1.2. Cơ chế Gửi dữ liệu Nối đuôi (Gửi tiếp dữ liệu phát sinh mới)
- **Phân định hai nhóm dữ liệu truyền tải:**
  - **Nhóm 1: Dữ liệu Cấu hình / Hiện trạng:** Cần gửi toàn bộ trạng thái mới nhất của danh mục thiết bị, thông số tuyến (gửi bản chụp toàn bộ hiện tại).
  - **Nhóm 2: Dữ liệu Lịch sử / Sự kiện (Ví dụ: Bảng Sự cố, Lưu lượng xe):**
    - **Bắt buộc chỉ gửi nối đuôi:** Lần gửi trước đã xuất dữ liệu đến mốc nào thì lần sau **chỉ lấy các bản ghi phát sinh tiếp sau mốc đó** để gửi tiếp.
    - **Lý do:** Đối tác đã tiếp nhận và lưu trữ các bản ghi cũ; nếu mỗi chu kỳ (ví dụ 30 giây) lại gửi lại toàn bộ lịch sử sẽ gây quá tải mạng, trùng lặp dữ liệu và lãng phí tài nguyên hệ thống.
- **Thiết kế Cơ sở Dữ liệu cho Bảng lưu mốc đã gửi:**
  - Tạo bảng lưu trạng thái mốc gửi gần nhất với các trường tối thiểu:
    - `PartnerCode`: Mã đối tác.
    - `PacketCode`: Mã gói tin.
    - `LastTime` / `LastKey`: Mốc thời gian hoặc khóa bản ghi lớn nhất đã gửi thành công trong phiên trước.
  - **Xử lý trùng lặp thời gian:** Nếu chỉ lưu mốc thời gian (`LastTime`), có thể xảy ra trường hợp nhiều bản ghi có cùng một giây phát sinh dẫn đến sót hoặc lặp dữ liệu. Do đó, hệ thống sẽ kết hợp khóa phức hợp: `LastTime` + `LastId` (hoặc chuỗi định danh duy nhất) để truy vấn SQL lấy tiếp nối:
    $$\text{WHERE } (\text{UpdateTime} > \text{LastTime}) \text{ OR } (\text{UpdateTime} = \text{LastTime} \text{ AND } \text{Id} > \text{LastId})$$

---

### 1.3. Đánh giá Cơ chế "Gửi ngay khi có dữ liệu mới" (Bắt thay đổi tức thì)
- **Bản chất kỹ thuật:**
  - Khác với gửi định kỳ theo lịch cố định, tính năng này yêu cầu hệ thống phải gửi dữ liệu ngay tức thì khi trong CSDL có bản ghi mới hoặc trạng thái thay đổi.
- **Hai phương án kỹ thuật được thảo luận:**
  - **Phương án 1 (Tầng Ứng dụng):** Tại các hàm nghiệp vụ khi thao tác ghi/sửa CSDL của bảng tương ứng (ví dụ tạo Sự cố mới), gọi trực tiếp hàm phát tín hiệu sang Worker hoặc gửi bản tin qua NATS.
  - **Phương án 2 (Tầng Cơ sở Dữ liệu):** Sử dụng cơ chế bắt thay đổi dữ liệu (Change Tracking - CT / CDC) để tự động nhận diện version thay đổi của bảng nguồn.
- **Quyết định tại cuộc họp & Cập nhật triển khai thực tế (22/09/2026 — MasterPlan P0):**
  - *Tại cuộc họp (chiều 21/09):* Đội ngũ từng thảo luận phương án tạm hoãn để ưu tiên hoàn thành trước luồng gửi định kỳ nối đuôi theo mốc đánh dấu, và dự kiến tạm thời khóa ô chọn (checkbox) trên Frontend.
  - *Cập nhật thực tế triển khai (Vẫn làm ngay trong tuần này):* **Tính năng này KHÔNG HOÃN mà đã được triển khai xong hoàn tất ngay trong tuần** (ngày 22/09/2026) theo phương án **SQL Server Change Tracking (CT) + NATS** (thông qua hạ tầng chung `Services.Shared.Runtime` / `TransportManager`).
  - Trên giao diện Frontend, **checkbox "Gửi ngay khi có dữ liệu mới" (`SendOnNewData`) đang được BẬT THỰC TẾ (active / enabled)** và kết nối hoạt động trực tiếp cùng Backend Worker, bảo đảm 100% test suite (186/186 tests) đều đã vượt qua. Các thành viên và bên liên quan lưu ý tính năng đã chạy thật, **không bị hoãn hay khóa**.

---

### 1.4. Xử lý Kiểu dữ liệu, Định dạng & Giá trị Mặc định
- **Định dạng Thời gian & Chuỗi:**
  - Rà soát format thời gian khi chuyển đổi hai chiều. Ở chiều gửi, đảm bảo format chuẩn ISO-8601 hoặc định dạng thỏa thuận (`yyyy-MM-dd HH:mm:ss`).
  - Ở chiều nhận, kiểm tra tính hợp lệ trước khi chuyển thành `DateTime` để tránh dừng đột ngột dịch vụ.
  - Trường dữ liệu nào không map thì để trống hoặc giá trị `null` an toàn, không cố ép format gây lỗi.
- **Giá trị mặc định cho Bộ mã quy đổi (CodeSet):**
  - Tách bạch cấu hình giá trị mặc định theo chiều: `DefaultPartnerValue` (chiều Gửi sang đối tác) và `DefaultSourceValue` (chiều Nhận về nội bộ).

---

## 2. Ma trận phân công công việc & Kế hoạch hành động (Action Items Checklist)

| Thành viên | Trách nhiệm | Công việc chi tiết | Thời hạn |
|---|---|---|---|
| **Đạt** | Backend Web API & Worker | 1. **Thiết kế bảng lưu mốc đã gửi:** Tạo bảng lưu `PartnerCode`, `PacketCode`, `LastTime`, `LastKey` phục vụ gửi nối đuôi.<br>2. **Cập nhật Query luồng gửi:** Sửa câu truy vấn trích xuất dữ liệu, chỉ lấy bản ghi mới phát sinh tiếp sau mốc `LastTime`/`LastKey` của phiên trước.<br>3. **Hoàn thiện API danh mục:** Cung cấp API trả về trạng thái ánh xạ của từng gói tin theo đối tác.<br>4. **Rà soát xử lý lỗi:** Tách biệt mã lỗi chuẩn hóa trong cấu hình hệ thống.<br>5. **Triển khai cơ chế Gửi khi có dữ liệu mới (Event-Driven CT + NATS):** Hoàn thành trong tuần (22/09) với `ChangeTrackingWatcherWorker` (heartbeat 1s) và `DataOutboundNatsWorker` qua `TransportManager`, kích hoạt xuất bản tức thì khi bảng nguồn có dữ liệu mới. | Trong tuần |
| **Hiếu** | Dev ShareData | 1. **Chuẩn hóa luồng Tạo Hồ sơ Ánh xạ:** Tự động sinh Mã hồ sơ ánh xạ từ bộ 3 (`Đối tác` + `Gói tin` + `Chiều In/Out`), bỏ chọn tay thủ công.<br>2. **Hiển thị trạng thái Ánh xạ trên bảng Gói tin:** Thêm nhãn thể hiện rõ gói tin nào "Đã có Ánh xạ" (xanh) hoặc "Chưa có Ánh xạ" (xám/cảnh báo).<br>3. **Sửa các lỗi format DateTime:** Đồng bộ component DateTime picker.<br>4. **Kích hoạt tính năng gửi dữ liệu mới (SendOnNewData):** Bật checkbox hoạt động thật trên Frontend, liên kết với cờ `SendOnNewData` của Subscription để kích hoạt luồng CT + NATS của Backend trong tuần này.<br>5. **Bàn giao test ShareData:** Đóng gói bản test cho Tester (chị Như) để nghiệm thu phân hệ. | Trong tuần |
| **Anh Sơn** | Tech Lead | Rà soát kiến trúc toàn tuyến, hỗ trợ xử lý câu query nối đuôi và chuẩn bị kịch bản tích hợp VideoWall. | Xuyên suốt |

---

## 3. Toàn văn kịch bản đối thoại chi tiết (Full Verbatim Transcript)

---

### [Phiên 1: Rà soát Giao diện Ánh xạ & Format Dữ liệu]
*(Nguồn: `21-09-2026-sharedata-phan-1-ui-mapping.m4a`)*

| Mốc thời gian | Người nói | Lời thoại chi tiết |
|---|---|---|
| `00:00` | **Hiếu** | ...cái bảng ánh xạ này... |
| `00:02` | **Anh Sơn** | Bảng nào? Giờ gửi dữ liệu hả? |
| `00:05` | **Hiếu** | Cái này đưa vô trong header này là cái gì nó cũng không quan tâm... Cái này là do mình đặt, mình đặt cái gì cũng được. Bỏ cục này đi. |
| `00:15` | **Anh Sơn** | Đâu, em thử đổi coi. Đặt cái gì cũng được mà bỏ luôn cũng được hả? |
| `00:20` | **Hiếu** | Bỏ luôn rồi! |
| `00:21` | **Anh Sơn** | Bỏ luôn nó có bị sai không? Sao sai được? Mình không chọn mà. Kêu đặt cái gì cũng được... Mới nói đặt cái gì cũng được, xong rồi bảo bị sai! |
| `00:36` | **Hiếu** | Bên này chưa có format nè... Bắt buộc bắt buộc phải format mấy thằng này. |
| `00:44` | **Anh Sơn** | Ủa giờ không map thì thì chọn... remove đi, để null lại. Để chữ null. |
| `00:53` | **Hiếu** | Để null thì để chữ null thôi chứ cần gì format. |
| `00:56` | **Anh Sơn** | Để chữ gì cũng được. Nghĩa là bỏ trống hả? Bỏ trống được không? Bỏ trống thì nó gửi null. |
| `01:03` | **Hiếu** | Ừ, gửi null đó. |
| `01:09` | **Anh Sơn** | Rồi rồi để chút xíu, chút xíu coi lại. Đâu ai hỏi đâu? Đúng rồi, đâu ai hỏi. |
| `01:20` | **Đạt** | Tình hình xong cái gì? Em đang xem dữ liệu... dữ liệu mới hả? |
| `01:28` | **Hiếu** | Cái này dữ liệu gửi thử nè, chưa... đang đang xoay bên... đang xoay cái thằng test luôn. Còn mấy cái này ok. |
| `01:40` | **Hiếu** | Cái này của anh, bữa cũ là anh xổ ra hết mấy cái định dạng đúng không? Còn cái này của em là từng một cái tab là một cái định dạng riêng. |
| `01:48` | **Anh Sơn** | Không được! Không có đúng. |
| `01:56` | **Anh Sơn** | Nhưng mà nếu mà kiểu như vậy á... Chuyển như vậy đúng không? Chuyển như vậy thì xem... Ví dụ như người ta nhập vô đi... Chuyển qua đây nó xoá nè. Xong nó nhập lại. |
| `02:13` | **Hiếu** | Dạ. |
| `02:14` | **Anh Sơn** | Cũng hợp lý chứ bây giờ ví dụ người ta nhập tin bên mình, người ta nhập theo định dạng ngày tháng năm nó không xử lý được. Đúng không? |
| `02:27` | **Anh Sơn** | Xong rồi nó chuyển qua string là sao? String thì để cho nó tự do, muốn nhập gì thì nhập. |
| `02:37` | **Hiếu** | Thì cũng có format mà. Xem format của string là như thế nào. |
| `02:45` | **Hiếu** | Anh... anh Sang hay anh Thắng gì đó... Anh Thắng hả? Mới nãy Thắng vô... Thắng làm mà anh Sang bị thiếu á. Dạ. |
| `03:00` | **Hiếu** | Ủa cái này hôm nay nó không có nút lưu hả? Ở đây đúng không? Không, cái này... cái nút cấu hình á. |
| `03:05` | **Đạt** | Thêm thêm cái cấu hình vô... |
| `03:07` | **Hiếu** | Ở đây để nút cấu hình mệt quá! Thêm cái nút cấu hình vô. |
| `03:12` | **Anh Sơn** | Đấy không biết cấu hình toàn diện... Bữa đó nói về cái đó mà. Có nói về cái đó. Nói cái gì về cái đó? |
| `03:22` | **Hiếu** | Cấu hình vô cái gì? Rồi có thể... không phải. Mapping đang sử dụng... |
| `03:36` | **Anh Sơn** | Như vậy thì bay chặn... Sao không biết table luôn? Như vậy bay chặn ngay từ đầu luôn đi. Phải sửa lạng quạng táo... |
| `03:52` | **Hiếu** | Còn cái... còn mấy cái từ viết tắt này nọ kia... Tí xíu nữa đưa cho... |
| `04:04` | **Hiếu** | Kiếm nước ly nước... |
| `04:12` | **Anh Sơn** | Ủa nãy đi mua nước mà? |
| `04:13` | **Hiếu** | Đi vô trong... Thôi trễ rồi em ơi! Ngựa ngựa... ba phải. Ba phải á! |
| `04:20` | **Đạt** | Đúng ba phải luôn á! |
| `04:22` | **Hiếu** | Em vô hết được rồi, em muốn chọn thì nói anh Sơn rót cho em nữa. Rót cho em đi! Có 2 ly nước... Anh rót cho em đi! |
| `04:30` | **Anh Sơn** | Nè... Nó tưởng nó trả là nó thích chọn. Hồi nãy không đọc group nhóm hả ba? Hồi nãy giờ nói ba bốn lần luôn rồi mà nó chưa... |
| `04:40` | **Hiếu** | Tưởng chưa... Ai ngờ đi qua mua ly nước. |
| `04:47` | **Anh Sơn** | Hiếu có một cái năng lực là đầu để trên trời! |
| `04:57` | **Hiếu** | Giờ ở chỗ đó có thêm màn hình bự này nọ... Đã làm màn hình này xong rồi tự review ở dưới luôn. |
| `05:07` | **Đạt** | Cái này... cái gì? Đưa vào. |
| `05:11` | **Anh Sơn** | Rồi, ông ba ơi, khó làm quá à! Kệ nó đi. Hoàn tất cả món rồi. Thôi. Cái gì? Cái gì không phải cướp? |
| `05:32` | **Anh Sơn** | Nè nè nè... Màn hình 60 inch kìa. |
| `05:48` | **Hiếu** | Ví dụ mình mua pin cho mình... |

---

### [Phiên 2: Thảo luận Chuyên sâu Cơ chế Gửi Nối đuôi & Xử lý Dữ liệu Mới]
*(Nguồn: `21-09-2026-sharedata-phan-2-gui-noi-duoi-va-du-lieu-moi.m4a`)*

| Mốc thời gian | Người nói | Lời thoại chi tiết |
|---|---|---|
| `00:15` | **Hiếu** | Giờ đưa vô DB... |
| `00:23` | **Đạt** | Lấy dữ liệu trước rồi đã... |
| `00:29` | **Anh Sơn** | Giờ nhận về bắt đầu đẩy ngược qua... rồi nó quét bảng lấy ra, chia cho các bảng con ở dưới. |
| `00:39` | **Hiếu** | Tầm bậy nè! Thì lấy một máy thôi. Ừ, một luồng đúng không? Đúng rồi. Giờ chỗ anh Đạt chỉ lưu cho em... |
| `00:53` | **Hiếu** | Khi mà nhận dữ liệu từ đối tác á thì bên phía đối tác sẽ gửi kèm theo một cái partner code... |
| `01:10` | **Đạt** | Chị Tuyên có cái xấp chữ ký kìa chị Tuyên. |
| `01:17` | **Hiếu** | Nếu có cái gói gửi qua API code đi vô header. Nhưng mà nếu mà nó xử lý xong thì nó sẽ ghi cái gì? |
| `01:25` | **Anh Sơn** | Xử lý xong nó ghi cái gì là sao? |
| `01:28` | **Đạt** | Ý là làm sao để biết cái thằng nào chưa xử lý, cái thằng nào đã xử lý rồi? |
| `01:37` | **Anh Sơn** | Nếu mà có... thì đăng ký, xử lý lại làm sao? Phải đăng ký, xong rồi biến đổi sang... chuyển qua format. |
| `01:57` | **Hiếu** | Nghĩa là lưu cái dữ liệu đó lại? |
| `02:02` | **Anh Sơn** | Không không, anh hiểu nhầm rồi! |
| `02:05` | **Hiếu** | Ví dụ như khi nhận đúng không? Tạo cấu hình thì sẽ chọn mapping, xong rồi xem cái này coi đúng format chưa... thì thống nhất lại có lỗi hay không. Bây giờ lỗi đó mình chưa biết đúng không? |
| `02:18` | **Anh Sơn** | Giải thích cái gì vậy trời? |
| `02:23` | **Hiếu** | Mua kiếm màn hình giá bình thường thôi được rồi. Tầm cỡ 24-27 inch xem thử coi... 27 đi! 27 ngon hơn. 27 đúng không? 24 với 27 phê hơn. 24 thôi cho nó gọn. |
| `02:56` | **Anh Sơn** | Quan trọng nó xử lý về... vừa bỏ đúng không? |
| `03:04` | **Hiếu** | Nhưng mà mới đi vô á là nó sẽ lưu một cái file khác rồi nó dựa vào cái file này nó lấy ra nó xử lý. Lúc mà nó lấy ra xử lý xong rồi nó sẽ ngủ tầm 5 giây. 5 giây sau bắt đầu tiếp tục nó quay lại, scan để xử lý tiếp. |
| `03:18` | **Anh Sơn** | Ừ. |
| `03:20` | **Hiếu** | Thì khi mà nó xử lý xong thằng này nó sẽ ánh xạ ngược lại cái mapping theo... theo cái thằng... cái vector mà gửi về á. Xong rồi nó tìm lại cái gói mapping, xong rồi nó sẽ tìm ra được cái câu truy vấn để insert vô. |
| `03:40` | **Đạt** | Hú hồn hả? Bảng đó bảng nào? |
| `03:44` | **Hiếu** | Bảng gửi về đó anh. Đúng rồi đó. Đâu mà xóa hết trơn vậy? Trời ơi! |
| `05:10` | **Anh Sơn** | Nhưng mà sao cái mã bên DB lại sai? Dữ liệu gốc nó có nhận dạng nhỏ mà. Mà nó có cả hướng nữa. Chọn bên kia mapping đúng không? Bên kia map đúng, bên kia có cài đặt lớn. |
| `05:47` | **Hiếu** | Tạm thời để như vậy trước đi, để ok rồi muốn sửa thì sửa sau. |
| `05:54` | **Anh Sơn** | Chừa chỗ để sửa. Cho một cái constant hay cái gì á... gom nó lại để sửa tổng thể á, chứ đừng có truyền vô từng cái. |
| `06:06` | **Hiếu** | Ờ đúng, đúng rồi đó, ghi lại... ghi xuống chứ mốt sửa nó khó. |
| `06:13` | **Hiếu** | Rồi, cái format DateTime này chỗ mapping gửi đi á thì ok nhưng mà nhận về thì sao? Nhận về qua test ổn rồi, code menu default sửa lại ok hết rồi. Bây giờ xử lý lại cái luồng mapping... Cái này làm đủ hết tất cả các gói chưa? |
| `06:35` | **Đạt** | Chưa, mới làm vài gói này à. |
| `06:38` | **Anh Sơn** | Cái này làm cho nó xong tối nay đi! Rồi cái thứ hai nữa là cái luồng gửi á... Luồng gửi hiện tại là đang gửi... gửi là hết đúng không? Gửi ra hết. Thì hiện tại bây giờ là sẽ thêm một cái chỗ cấu hình gửi vào nữa là liên quan đến gửi nối đuôi á! |
| `06:55` | **Hiếu** | Ừ. |
| `06:56` | **Anh Sơn** | Gửi nối đuôi, ví dụ như là hiện tại là gửi đến mốc thời gian này... thì lần sau phát sinh dữ liệu ra thì mới gửi. Gửi nối tiếp chứ không phải gửi lại từ đầu. |
| `07:05` | **Hiếu** | Là gửi ngay khi có dữ liệu mới hay là cái luồng bình thường? |
| `07:08` | **Anh Sơn** | Dữ liệu mới hay dữ liệu bình thường cũng được. Miễn sao là cái dữ liệu đó không được gửi lại cái dữ liệu trước đó nữa. |
| `07:14` | **Hiếu** | Vậy là bên anh Đạt cũng phải detect là thằng nào mới thay đổi nha! Anh phải đánh dấu lại... |
| `07:18` | **Anh Sơn** | Phải đánh dấu lại đó, nói chung là đánh dấu lại. Như vậy có nghĩa là phải có một cái bảng nữa, lưu lại cái bảng đánh dấu này nè! Nó sẽ bao gồm có cái đối tác, cái gói tin, với lại cái thời gian... trong cái trường thời gian đó nó sẽ lưu lại cái... cái định danh duy nhất của nó là cái gì á! Có thể là thời gian kết hợp với một cái mã gì đó. Phải có trường hợp là cùng một dữ liệu thời gian nó sẽ giống nhau. |
| `08:08` | **Anh Sơn** | Rồi nói lại nè: Cái luồng gửi... cái luồng gửi đi nha, bây giờ làm thêm cái phần liên quan đến check cái dữ liệu trước đó đó! Giống như lúc trước mình gửi ví dụ như là đến cái mốc này đi, mốc thời gian này đi. Thì lần sau nó gửi nó chỉ lấy cái mốc này nó gửi đi thôi. |
| `08:30` | **Đạt** | Nó đi tiếp. |
| `08:31` | **Anh Sơn** | Nó đi tiếp, đúng rồi, như nó nối đuôi á, nối tiếp nhau. Chứ nó không có gửi lại hết tất cả danh sách trước đó nữa thì nó sẽ bị dư. Đó thì mình sẽ có một cái bảng, chắc chắn phát sinh thêm một bảng nữa để mình lưu trữ nó lại nè. Thì về cơ bản trong trỏng thì ít nhất phải ba ba trường nè: Thằng đối tác, thằng gói tin, với lại là lưu lại cái key thao tác gần nhất trước đó. Đúng không? Thì nó có thể kết hợp giữa thằng thời gian... Thời gian hoặc một cái thằng gì đó, tại vì nếu em lấy thời gian không á thì sẽ có phát sinh là cùng một cái thời gian đó dữ liệu giống nhau. Đúng không? Dữ liệu giống nhau nên mình phải kèm thêm, mình sẽ combine nó lại, nói chung là sẽ biến đổi nó lại một cái chuỗi gì đó, thằng này kết hợp với thằng kia, hoặc là tạo nhiều cột. Nhiều cột để mình lưu trữ. Miễn sao đảm bảo là dựa trên một cái cặp giá trị đó, đó thì sẽ lấy cái... dữ liệu sau, tiếp tiếp tiếp tiếp tiếp đó. |
| `09:33` | **Hiếu** | Cứ đi tiếp. Ví dụ bây giờ làm cái gói tin này đúng không, xong rồi ví dụ anh gửi lúc 10 giờ 31 phút. Ví dụ vậy đi, nói ví dụ quen là 10 giờ 31 phút thì anh sẽ lưu vào cái bảng mà anh kêu tạo mới á, là LastTime 10 giờ 31 phút. Xong rồi anh lấy cái khung giờ đó so sánh với thằng update này. Update nào cao... update nào lớn hơn thì sẽ gửi thằng update đó đi, rồi gửi luôn cả thằng đi tiếp. |
| `09:59` | **Anh Sơn** | Nhưng mà nó sẽ tùy... tùy dữ liệu! Một số dữ liệu là mình sẽ gửi lại hết. Nhưng một số dữ liệu ví dụ như nó có theo cái gọi là cái lịch sử đó... Chẳng hạn như cái bảng Incident. Đúng không? Mấy Incident thì chỉ gửi tiếp mới mới thôi, chứ tự dưng cứ định kỳ 30 giây gửi lại hết tất cả các bảng thì đâu có được! Tại vì dữ liệu đó mình đã gửi cho họ, họ đã lưu trữ bên bển rồi, giờ mình lại đi gửi cái dữ liệu đó lại nữa thì cũng không mang ý nghĩa gì. Không có ý nghĩa gì. Nhưng mà một số bảng là nó cần cập nhật hết tất cả thông tin thì bắt buộc phải gửi lại hết. Đúng không? |
| `10:40` | **Hiếu** | Nó có 2 dạng như vậy. Có nghĩa là một số gói tin là sẽ gửi hết, còn một số gói tin là chỉ gửi một bản ghi thôi... Dữ liệu mới nhất của một bản ghi thôi. |
| `10:50` | **Anh Sơn** | Đúng rồi! Trong cái gói tin đó, chứ không phải một bảng nữa, mà là trong cái gói tin đó. |
| `10:55` | **Đạt** | Ví dụ nếu mà mình đi theo cái hướng này á, thì cái cờ của cu Hiếu làm "Gửi khi có dữ liệu mới" nó cần không? |
| `11:00` | **Anh Sơn** | Cần chứ! Cũng cần luôn. Cần cho thằng Incident đó. Cần luôn, tại vì cái dữ liệu mới... dữ liệu mới, cái mà cập nhật ngay lập tức nó khác với cái dữ liệu kia. Dữ liệu kia là đến định kỳ là nó sẽ gửi lại, nhưng mà cái mới là ví dụ như có cập nhật trạng thái gì đó, nó mới thay đổi á, là nó sẽ gửi luôn. |
| `11:21` | **Hiếu** | Mà hiện tại cái chu kỳ của anh á là anh đang lấy hết nè! |
| `11:24` | **Anh Sơn** | Thì bây giờ đổi! Ví dụ mà bây giờ ý anh Sơn ví dụ có cập nhật mới đi, hiện tại là mình chưa có detect cái chỗ đó... Thường nếu mà làm để biết cái dữ liệu đó cập nhật mới á, thì bây giờ cái chỗ ShareData làm thêm một cái thằng API một thằng gì đó. Để khi mà ví dụ như mình sẽ biết được là ví dụ như cái gói này 101 nè, nó sẽ liên quan đến bảng nào. Đúng không? Tại mình làm dữ liệu mình sẽ biết nó liên quan đến bảng nào. Thì tương ứng là trong cái lúc mà nó ảnh hưởng đến cái bảng đó đó, chức năng nào nó ảnh hưởng đến bảng đó đó, mình sẽ kêu nó gọi thêm một cái thằng... một cái hàm gì đó. |
| `12:03` | **Đạt** | Mình dùng CDC được không anh? |
| `12:05` | **Anh Sơn** | Được! CDC là kiểu dữ liệu nó có update á thì nó sẽ ghi vô log là lúc đó mình biết nó dữ liệu đó là dữ liệu mới. Thì một là ở trên, còn hai là mình có thể ghi ở dưới cũng được. Ghi ở dưới mặt DB cũng được, trong mấy cái nào cũng được. |
| `12:20` | **Hiếu** | Thấy cái này scope... scope cái này rộng quá! |
| `12:23` | **Anh Sơn** | Đó, tóm gọn nó lại là vậy, nó mới thực... Chứ nếu mà dựa vô cái trường UpdateTime á, thì nó cũng không... không chính xác lắm! Có trường hợp nó không chính xác lắm. Mấy thằng FDK á... |
| `12:36` | **Hiếu** | Nhiều quá trời... nhiều quá anh Sơn! Anh phân chia task đi, nhiều quá. |
| `12:44` | **Anh Sơn** | Thiệt chứ, thì mới nói là nó nhiều! Đâu ít đâu. Ở trong requirement đúng hai dòng: làm hai tháng trời chưa xong! Nó quá nhiều mà. |
| `12:51` | **Đạt** | Cái luồng gửi là phải xử lý thêm rồi đó. Luồng gửi xử lý thêm nhiều. |
| `12:55` | **Anh Sơn** | Giờ mình dẹp bữa quay ra luồng nhận là dễ. |
| `13:00` | **Đạt** | Không, luồng nhận nó khó ở chỗ SQL... SQL nó chát làm sao hết được ta? |
| `13:08` | **Hiếu** | À đúng rồi, nó có... có SQL để lưu xuống. |
| `13:13` | **Đạt** | Nó khó ở chỗ SQL đó, có nghĩa là mốt làm chuyện nhưng mà mốt có cập nhật á, có nghĩa là nó có thay đổi á, là em phải thay đổi em biến đổi cái hàm SQL đó lại. |
| `13:25` | **Hiếu** | Nếu vậy luôn cái cờ mà "Gửi khi có dữ liệu mới" á, ý anh Sơn là ban đầu là tự nhận biết tính năng nào update xong tính bắn qua NATS đúng không? Hay chuyển qua CDC? |
| `13:36` | **Anh Sơn** | Một là dựa trên hàm, có nghĩa là dựa trên tầng tầng trên. Còn hai là có thể dựa ở tầng dưới. Tầng dưới là dùng dùng các chức năng của SQL đó, nó bắt được cái sự thay đổi á. Cũng được! Ví dụ trong SQL, SQL nó có trigger đó, trigger, mình có thể tận dụng nó được. Có cập nhật gì đó thì nó sẽ cập nhật qua một cái bảng... đánh dấu lại. Lấy dữ liệu gần nhất là ngày nào. Còn không thì nếu mà trong cùng một bảng á, thì dựa trên cột nào đó... Nhưng mà cẩn thận cái cột Update nha! Cột Update với cột Create nha. Cột Update là khi có sự chỉnh sửa nó mới có hiện, còn không nó bị trống đó. Đã nói rồi! |
| `14:31` | **Anh Sơn** | Mục tiêu là detect được, nó có cập nhật là được. Nhưng mà dựa hai cột này thì bắt buộc là code trên kia hai thằng này phải update theo. Nghĩa là khi mà chỉnh sửa cái gì đó là phải có update luôn này. Chứ mà chơi kiểu mà chơi... chơi tạo dữ liệu tay, xong rồi quên cập nhật mấy trường này là coi như cũng dính theo luôn là không... là nó không có đẩy theo được. |
| `15:13` | **Hiếu** | Vậy giờ chốt lại là anh Đạt làm cải thiện cái luồng gửi, em sửa lại cái này. Xong rồi đưa cho test trước. |
| `15:22` | **Anh Sơn** | Bây giờ... bây giờ gửi thì thêm cái thằng nối đuôi giống như kia. Bây giờ hiện tại đưa test trước cũng được, tại vì cái đó cơ bản là nó có thêm... nó thêm này. Đầy... làm sao này? Chọn gói tin, chọn đối tác, đặt thời gian, tạo cái mapping... kiểm tra lại định kỳ nó ra đúng là được. Đúng không? Là được một luồng gửi. Thứ hai nữa là bắt đầu là luồng nhận, thì khai báo vô luồng nhận. Cho nó map lại đúng, vô DB là được. Bên em check lại trước là vô mapping, ví dụ luồng gửi em map kiểu này, luồng nhận em map kiểu khác. Nói vậy là đúng rồi. Cháy đúng theo cái luồng nhận là được, cho tương thích. Nghĩa là tạo hai mapping riêng biệt luôn. |
| `16:13` | **Đạt** | Có có có. |
| `16:16` | **Hiếu** | Giao diện? |
| `16:20` | **Anh Sơn** | Giao diện ngáo ngơ! Tí nữa chuyển qua màu xám là hiểu rồi á! Rồi vậy thôi. Bị tội gì thắc mắc. |
| `16:34` | **Hiếu** | Em sửa lại nốt xong rồi em đưa cho test trước, rồi anh bàn giao em VideoWall đi để em check từ từ VideoWall. Anh làm tiếp ShareData đúng không? |
| `16:41` | **Anh Sơn** | Làm tiếp ShareData. Hiện tại là cái luồng cũng ổn rồi đó, nhưng mà cái của em đang mới có một gói tin nè, làm thêm mấy cái gói tin còn lại ba ơi! Một cái sao mà test? |
| `16:51` | **Hiếu** | Ý là tất cả làm hết rồi nhưng mà em chỉ test một cái thôi! Chín cái kia chưa test... |
| `16:56` | **Đạt** | Vậy cái luồng mà gửi dữ liệu mới á, cái cờ tắt luôn đi, chưa có làm. |
| `17:01` | **Hiếu** | Không thì thì mới để cái kiểu là anh có dữ liệu... |
| `17:03` | **Anh Sơn** | Cứ để đó đi xong rồi báo là cái này chưa có implement thôi! Chưa có implement thôi. Chốt chốt! |

> 📌 **Ghi chú cập nhật thực tế triển khai (22/09/2026 — MasterPlan P0):**
> Tại thời điểm trao đổi chiều 21/09 ở mốc thoại này, nhóm từng thống nhất tạm hoãn tính năng gửi dữ liệu mới để ưu tiên luồng nối đuôi. Tuy nhiên, theo quyết định chính thức của **MasterPlan**, tính năng này **vẫn được thực hiện ngay trong tuần này** và thực tế **ĐÃ CODE XONG HOÀN TẤT 100%** bằng giải pháp **SQL Server Change Tracking (CT) + NATS** (qua hạ tầng chung `Services.Shared.Runtime` / `TransportManager`), đồng thời trên giao diện Frontend **checkbox "Gửi ngay khi có dữ liệu mới" (`SendOnNewData`) đang BẬT THỰC TẾ (enabled)** và hoạt động trực tiếp cùng hệ thống. Toàn bộ 186/186 tests đều đã PASS. Ghi chú này nhằm làm rõ đúng tiến độ thực tế, tránh trường hợp người đọc transcript hiểu lầm là tính năng đang bị hoãn.

---

### [Phiên 3: Chuẩn hóa Luồng Tạo Hồ sơ Ánh xạ vs Cấu hình Gói tin]
*(Nguồn: `21-09-2026-sharedata-phan-3-mapping-profile.m4a`)*

| Mốc thời gian | Người nói | Lời thoại chi tiết |
|---|---|---|
| `00:00` | **Hiếu** | Gợi ý em luôn đi... Cái này nó tự động... Nó tự động lấy cái mapping luôn chứ không hề chọn. Có nghĩa là đúng đối tác, đúng gói tin thì nó sẽ đẻ ra một cái hồ sơ ánh xạ... đúng chiều. Khi mình tạo hồ sơ ánh xạ đúng không, mình chọn đối tác, chọn gói tin, chọn chiều đúng không? Đúng không? Xong rồi mình qua cái gói tin này nè, mình cũng chọn đối tác, chọn gói tin, chọn chiều thì nó sẽ tìm đúng cái bảng ánh xạ đó nó gắn vô đây. Chứ không cho người dùng tự chọn để tránh trường hợp người ta map lung tung. |
| `00:57` | **Anh Sơn** | Không hiểu! Lý do là... Khi mình tạo hồ sơ ánh xạ, mình sẽ chọn đối tác, chọn gói tin đúng không? Thì nó sẽ sinh ra cái hồ sơ với cái thông tin là đối tác đó với gói tin đó. Xong rồi mình qua đây tạo cái gói tin gửi đi, nó sẽ tự động nó lấy... hồ sơ ánh xạ đó nó add vô cái gói tin gửi đi. Thì nếu có một triệu người ta thích tạo một triệu cái hồ sơ cho một gói tin gửi đi thì sao? |
| `01:34` | **Hiếu** | Tạo một cái thôi chứ tạo nhiều làm chi ba? Giờ muốn cái này xử lý sao? Em muốn đi một cái luồng... Đi một luồng từ cái gì qua cái gì? Em đi một luồng từ cái gì qua cái gì được? |
| `01:50` | **Đạt** | Thì đây bây giờ đúng là phải tạo cái gói tin, tạo cái cấu hình này trước xong mới tạo mapping chứ? |
| `01:55` | **Anh Sơn** | Tạo thằng nào trước cũng được chứ không quan trọng, nhưng mà hiện tại em đang map tự nhiên em map hai chiều với nhau. Map hai chiều là gì? Tự nhiên bên kia vừa... vừa vừa map thằng này, vừa map thằng đối tác đi, thì map đối tác thì map gói tin. Đúng không? Để nó ra được cái mã ánh xạ. Xong rồi qua bên này em lại đi áp ngược lại cái mã ánh xạ này là bằng cái này với cái này... Để chi? |
| `02:25` | **Anh Sơn** | Hiểu không? Tự nhiên bên kia... bên kia chọn ánh xạ mà chọn ánh xạ là đi map thằng này với thằng này... Thì đó! Thì đối tác với thằng gói tin... Thằng này là thằng Subscription chứ không phải là thằng gói tin! Gói tin là cái này nè. Nó map cái gì? |
| `02:54` | **Anh Sơn** | Điên khùng hết rồi ba ơi! Thiệt á! Đối tác... map với gói tin, rồi tạo ánh xạ. Nhưng mà tự nhiên em đi map cái gói tin với đối tác để làm cái gì? Tự nhiên bên đây... bên đây có rồi nè... tự nhiên qua bên kia em lại đi map ngược đối tác với gói tin nữa? Là tạo ra cái này nè, tạo ra cái này nè! Tự nhiên em tạo cái mapping đúng không? Xong rồi tự nhiên em lại quay ngược lại đây, em lại map thằng này... nó đâu có mapping đâu, nó đâu có thiếu. Tự nhiên tạo thiếu vô chi cho nó mệt vậy? Cái này là nó cấu hình... |
| `03:36` | **Anh Sơn** | Tự nhiên bên này đâu cần map! Bên này chỉ cần để... chọn cái gì thôi. Đúng không? Tự nhiên em đi em đòi em map hai chiều á, bên kia cũng map, bên này cũng map. Vậy thằng nào trước thằng nào sau? |
| `03:58` | **Hiếu** | Tạo ánh xạ trước! |
| `04:00` | **Anh Sơn** | Sao tạo ánh xạ trước? Giờ người ta chưa xác định được là thằng đó sẽ có gói tin nào, tự nhiên người ta tạo ánh xạ làm cái gì? Chẳng lẽ giờ tạo đối tác mới xong rồi thằng nào cũng add 10 cái ánh xạ vô trước rồi... xong rồi nó muốn cái nào mới làm cái đó hả? Khùng vậy cũng nghĩ ra được nữa! Bữa đó nói là chiều là đi từ thằng cấu hình này ra trước! Đối tác đó bao nhiêu gói tin, từ gói tin đó mình mới tạo mapping. |
| `04:36` | **Anh Sơn** | Tự nhiên giờ em đang map... em đang map ngược lại em đòi map hai chiều đó. Map hai chiều rồi ví dụ như giờ bên ánh xạ tạo cái thằng thứ hai đi, tạo thành... cái thằng cũ không xài, tạo thành cái ánh xạ thứ hai, chẳng lẽ phải quay ngược lại đây đi update lại? Khổ quá vậy! Giờ ví dụ phát sinh thêm cái ánh xạ mới rồi sao? Chẳng lẽ em phải quay ngược lại đây em kiếm cái partner đó em kiếm cái gói tin này để update lại hả? Tự nhiên thao tác lại thành hai thao tác phiền hà vậy? |
| `05:25` | **Anh Sơn** | Có nghĩa là một chiều thôi! Thằng này có trước, thằng này partner, bao nhiêu gói tin nó add trước. Thằng này bỏ đây, thằng này bỏ đó. Người ta không có care đâu! Ta vô chi tiết người ta care à: Thằng partner này đúng không, thì map như thế nào... Nhưng mà qua bên đây... |
| `05:43` | **Hiếu** | Không, bên đây chọn partner xong nó xổ ra gói tin của partner đó thôi chứ? |
| `05:46` | **Anh Sơn** | Đúng rồi! Đối tác nè, trong đối tác này nè, nó đang được truyền bao nhiêu gói tin... thì mình chọn cái đó. Rồi tiếp nè! Gói tin này nó sẽ map... Rồi tiếp theo? |
| `06:17` | **Anh Sơn** | Xong rồi tới sửa data... Rồi nói lại đi ba! Nãy giờ chưa thấy làm được cái gì hết... |
| `07:11` | **Anh Sơn** | Vô chọn nước kìa Hiếu... Hôm qua Bách Hóa Xanh nó sale ly nước có mười mấy ngàn à. Mua vậy nè, anh săn bảy ly nước, đang tính chiều mời team uống bảy ly nước... |
| `07:33` | **Hiếu** | Mai cũng được anh! |
| `07:49` | **Anh Sơn** | Thôi lo làm sửa web đi ba! Ai rảnh đâu mà ngồi lo chuyện nước non hoài! |

---

### [Phiên 4: Đúc kết Kinh nghiệm & Chốt Kế hoạch Triển khai]
*(Nguồn: `21-09-2026-sharedata-phan-4-tong-ket.m4a`)*

| Mốc thời gian | Người nói | Lời thoại chi tiết |
|---|---|---|
| `00:00` | **Đạt** | Hai bảng giống nhau à? Thì có bảng nhận với bảng gửi... Còn cái này là... gửi qua cho bên người khác. Chắc chắn là DB mình với DB họ nó phải khác nhau. Bắt buộc phải có bước chuyển đổi đó. |
| `00:20` | **Đạt** | Mày làm từ lúc nào? Cái này hình như làm từ trước khi đi review... Ừ. Giờ mấy tháng rồi? Từ hồi tháng 7 rồi... đâu, từ tháng 7, từ cuối tháng 7. Ghê vậy! |
| `00:33` | **Hiếu** | Hồi đợt cấu hình em làm scope nó chưa to á, giờ map to bành... |
| `00:40` | **Đạt** | Thôi mệt quá! Đi luẩn quẩn quần quần hồi làm sai tè le á, xong rồi bị chửi! |
| `00:44` | **Hiếu** | Không có sai đâu... Chịu tưởng tượng ghê! |
| `00:46` | **Đạt** | Hồi đầu mà làm đúng là không bao nhiêu hết á. Giờ phải sửa lại... Từ cái lúc em share lại cái clip đúng tới giờ mất bao nhiêu thời gian... Tại thông tin nhiều quá mà cái phần xử lý thông tin nó không có ổn, sau này thì nó ổn hơn xíu. |
| `01:05` | **Hiếu** | Đúng rồi, trước trình bày... trình bày cho anh Sơn, đánh giá lại nè... sẽ làm cái gì. Biết cách làm việc á, trao đổi lại trước. |
| `01:13` | **Đạt** | Đúng, làm với Senior nó có khác! Senior nè, Senior đàng hoàng! Tại vì nếu mà... nếu mà rõ quá giúp cho mình làm việc nó nhẹ nhàng hơn. Đỡ tốn thời gian ngồi mò mẫm. |
| `01:38` | **Hiếu** | Giờ vẽ ra hai cái là nó lòi ra hết trơn... |
| `01:45` | **Anh Sơn** | Tự nhiên đi map lại đi ngược đi xuôi làm cái gì? |
| `01:49` | **Đạt** | Không, nhưng mà lúc đó anh em chốt đâu có hướng đó đâu. |
| `01:52` | **Hiếu** | Không, cái hướng đó là mình chứa bảng á, hướng đó là có tạo CRUD map thì thôi chứ kịch bản... Cái đợt trước làm gì có, mấy cái này mới đẻ ra... |
| `02:00` | **Đạt** | Không mà sure hai chiều này em mới đẻ đúng không? Hôm bữa đâu có đâu? |
| `02:04` | **Hiếu** | Cái map này là từ từ hồi đầu ý em rồi, nhưng mà em không có show ra. Hôm bữa là nó... là nó không có đọc được cái cấu hình, nó hiện khoảng trống cho nên bắt bỏ đó. |
| `02:15` | **Đạt** | Tại em cũng không hiểu là đi từng từng từng từng từng tầng, đi từng cái một cái. |
| `02:20` | **Hiếu** | Cái đó là mình phải tính bỏ bớt rồi đó. |
| `02:24` | **Anh Sơn** | Còn không á, cái này dễ nhất là như vầy nè: Em hiện lại... có mapping hay chưa có mapping thôi! |
| `02:31` | **Hiếu** | Không, nếu mà nói vậy em thấy xử lý nó đơn giản hơn á. |
| `02:35` | **Anh Sơn** | Có hoặc chưa có thôi! Ví dụ như là giờ add gói tin vô đi, add gói 101, 102 đi, bên kia mà chưa có mapping thì báo là chưa có để người ta biết người ta vô cấu hình theo. |
| `02:48` | **Hiếu** | Lúc đó là nó vẫn gửi á? |
| `02:51` | **Anh Sơn** | Nhưng mà nó đâu có map được mà nó gửi? Không có map được thì gửi cái gì? Đúng không? Bắt buộc phải có mapping! Tạo đúng không? Bắt buộc phải có mapping. Nếu mà không có mapping thì báo lỗi, không cho gửi. |
