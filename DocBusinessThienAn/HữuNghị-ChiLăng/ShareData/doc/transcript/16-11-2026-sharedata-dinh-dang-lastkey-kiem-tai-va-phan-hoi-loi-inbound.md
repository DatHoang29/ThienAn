---
tier: A
read: full
source:
  - MakeUp Chi Ngô Gò Vấp.m4a (17:30)
  - MakeUp Chi Ngô Gò Vấp 2.m4a (02:14)
  - MakeUp Chi Ngô Gò Vấp 3.m4a (05:01)
date: 16-11-2026
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData — Chuẩn hóa định dạng Khóa Nối Đuôi (yyyyMMddHHmmssfff_<MãĐịnhDanh>), thao tác đặt lại mốc đang chạy trên UI, đánh giá kiểm tải Server (N+1 query, ổ cứng HDD RAID 5), 3 phương án sinh LastKey, tùy chọn phản hồi lỗi chiều Nhận (Inbound) và định dạng ASN.1
participants:
  - Tech Lead
  - Dev
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ ShareData — Chuẩn hóa Định dạng Khóa Nối Đuôi, Đặt lại Mốc trên UI, Kiểm tải Server & Phản hồi Lỗi Chiều Nhận (16/11/2026)
### (Gộp theo thứ tự thời gian từ 3 tệp ghi âm: `MakeUp Chi Ngô Gò Vấp.m4a`, `MakeUp Chi Ngô Gò Vấp 2.m4a`, `MakeUp Chi Ngô Gò Vấp 3.m4a` — Tổng thời lượng: 24 phút 45 giây)

- **Ngày họp:** 16/11/2026
- **Người tham gia:** Tech Lead, Dev
- **Danh sách tệp ghi âm nguồn (theo thứ tự):**
  1. **Phần 1:** `MakeUp Chi Ngô Gò Vấp.m4a` (17 phút 30 giây) — Định dạng hiển thị và quy tắc ghép chuỗi Khóa Nối Đuôi (`LastKey`), thao tác sửa/đặt lại mốc đang chạy trên giao diện, kết quả xử lý kiểm tải Server (`N+1 query`, index) và hạ tầng ổ cứng `HDD RAID 5`.
  2. **Phần 2:** `MakeUp Chi Ngô Gò Vấp 2.m4a` (02 phút 14 giây) — Cách nhập mốc `LastKey` khi đổi công thức câu truy vấn từ `ID` sang `Code` và đánh giá lại 3 phương án sinh khóa nối đuôi.
  3. **Phần 3:** `MakeUp Chi Ngô Gò Vấp 3.m4a` (05 phút 01 giây) — Bổ sung tùy chọn tích chọn phản hồi lỗi cho chiều Nhận dữ liệu (`Inbound`), định hướng ưu tiên giao thức `API` và yêu cầu bắt buộc triển khai định dạng `ASN.1` cho Trung tâm Quốc gia.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Chuẩn hóa Định dạng Hiển thị & Cấu trúc Khóa Nối Đuôi (`LastKey`)
- **Hiển thị nguyên bản giá trị Khóa (`LastKey`):**
  - Trên giao diện quản lý mốc đang chạy, hiển thị trực tiếp chuỗi `key` nguyên bản đang lưu trong cơ sở dữ liệu thay vì biến đổi thành câu mô tả dài dòng khó đối chiếu.
  - Khóa nối đuôi không mang thông tin nhạy cảm hay đòi hỏi bảo mật cao, và người thao tác chỉnh sửa mốc chủ yếu là đội ngũ kỹ thuật/vận hành nắm rõ cấu trúc nên để nguyên chuỗi `key` là tường minh và dễ kiểm tra nhất.
- **Định dạng phần Thời gian viết liền (`yyyyMMddHHmmssfff`):**
  - Phần thời gian ở đầu khóa nối đuôi bắt buộc viết liền, không có khoảng trắng (`space`) hay ký tự phân cách, theo đúng thứ tự từ lớn đến nhỏ: **Năm – Tháng – Ngày – Giờ – Phút – Giây – Mili-giây** (ví dụ: `20261116102001123`).
  - **Lý do kỹ thuật:** Đảm bảo khi thực hiện sắp xếp chuỗi (`ORDER BY` / sort) theo thứ tự từ điển luôn chính xác tuyệt đối (ví dụ từ ngày `20261110` sang `20261111`), đồng thời tiết kiệm dung lượng vùng ghi và tránh rối mắt.
- **Tùy biến linh hoạt phần Đuôi định danh (Thay thế `ID` GUID dài bằng Mã nghiệp vụ đặc trưng):**
  - Không đóng cứng quy tắc ghép `ID` cho mọi gói tin vì `ID` (GUID) quá dài.
  - Mục đích của phần đuôi ghép sau thời gian là để phân biệt các bản ghi có **cùng mốc thời gian**; do đó người viết câu truy vấn SQL được phép tùy biến trường ghép nối phù hợp với đặc thù từng bảng dữ liệu:
    - **Dữ liệu thu phí:** Ghép thêm mã làn (`Lane`), vì tại cùng một thời điểm không thể có 2 xe cùng đi vào 1 làn.
    - **Dữ liệu trạng thái khu vực (`ZoneStatus`):** Ghép thêm `ZoneCode` (ngắn gọn hơn `ID` mà vẫn đảm bảo duy nhất).
    - **Dữ liệu sự cố (`Incident`):** Có thể kết hợp thời gian với vị trí lý trình (`Km`, `Meter` / `Number`).
  - Hệ thống chỉ cần trích xuất đúng cột khóa nối đuôi từ câu truy vấn để lưu trữ và so sánh, không phụ thuộc vào việc đuôi đó là `ID` hay `Code`.
  - Khi chuyển đổi câu truy vấn từ ghép `ID` sang ghép `Code`, xóa dữ liệu mốc cũ đang lưu để hệ thống chạy lại sạch sẽ hoặc nhập lại mốc `LastKey` theo cấu trúc mới (chỉ đóng vai trò rào mốc thời gian quét, không ảnh hưởng tới cấu trúc dữ liệu gửi đi theo Mapping).

### 1.2. Cơ chế Sửa / Đặt lại Mốc (`Reset LastKey`) & Bố cục Giao diện
- **Thao tác chỉnh sửa giá trị mốc (`Edit LastKey`):**
  - Thay vì mở form bắt chọn ngày giờ rồi ghép chuỗi phức tạp, khi bấm sửa chỉ cần hiển thị một ô `Input` điền sẵn giá trị `key` cũ đang lưu để người dùng chỉnh sửa trực tiếp theo đúng cấu trúc.
  - Nếu người dùng muốn chạy lại từ một mốc thời gian (ví dụ đầu tháng 1: `20260101`) và lười nhập phần đuôi phía sau hoặc để trống phần đuôi, phép so sánh sắp xếp chuỗi (`ORDER BY`) trên SQL vẫn hoạt động đúng mà không gây lỗi.
- **Thao tác Đặt lại (`Reset`):**
  - Nút **Đặt lại** thực hiện xóa trắng giá trị mốc (`về trống`) để gói tin quét lại từ đầu (tương đương thao tác xóa mốc).
- **Vị trí hiển thị trên Giao diện:**
  - Mốc đang chạy được lưu riêng theo từng cặp **(Đối tác, Gói tin)** (chỉ lưu 1 giá trị mới nhất hiện tại cho mỗi cặp).
  - Đặt khu vực xem/đặt lại mốc ở phía **Cấu hình gói tin** (hiển thị danh sách các đối tác đang dùng mẫu chung hoặc sổ danh sách đối tác/mốc chạy bên dưới gói tin) giúp người dùng vừa nhìn thấy định nghĩa câu truy vấn/khóa của gói tin vừa sửa được mốc ngay tại chỗ, tránh việc phải chuyển qua lại giữa các màn hình để tắt gói tin rồi mới sửa được mốc.

### 1.3. Đánh giá Kiểm tải Server, Tối ưu Truy vấn `N+1` & Hạ tầng Ổ cứng `HDD RAID 5`
- **Nguyên nhân và xử lý hiện tượng nghẽn khi kiểm tải (Spam Test):**
  - Trong đợt kiểm tải trước, việc cấu hình đồng loạt nhiều đối tác (2 luồng nhận, 5 luồng gửi) cùng chạy ở chu kỳ ngắn `30 giây` khiến các tiến trình dồn cục và ghi ổ cứng liên tục làm đơ máy.
  - **Đã khắc phục trên code mới:** Bổ sung đầy đủ `Index` cho CSDL và tối ưu toàn bộ các đoạn truy vấn bị `N+1 query` về thành **1 câu truy vấn duy nhất (`1 query`)**, đồng thời giảm bớt tần suất ghi log.
- **Đặc thù hạ tầng Server ngoài Site:**
  - Server triển khai thực tế tại hiện trường sử dụng ổ cứng **`HDD`** (cấu hình **RAID 5** gồm 4 ổ cứng lấy dung lượng 3 ổ) chứ không dùng `SSD` do chi phí `SSD` dung lượng lớn rất cao. Vì vậy, phần mềm bắt buộc phải kiểm soát tốt tần suất đọc/ghi đĩa (Disk I/O).

### 1.4. Đánh giá lại 3 Phương án Sinh Khóa Nối Đuôi (`LastKey`)
- **Phương án 1 (Đang áp dụng — Nối chuỗi trực tiếp trong câu SQL lấy dữ liệu):**
  - Tương tự cách đang dùng bên hệ thống Hóa đơn điện tử.
  - *Nhược điểm:* Nếu lấy 100 dòng dữ liệu thì SQL Server phải thực hiện phép nối chuỗi trên cả 100 dòng.
- **Phương án 2 (Truy vấn thứ 2 chỉ lấy `LastKey` trên dòng cuối cùng):**
  - Câu truy vấn chính chỉ lấy dữ liệu thô, bỏ cột convert/nối chuỗi `LastKey`.
  - Khi đọc đến dòng dữ liệu cuối cùng của lô, hệ thống mới gọi câu truy vấn thứ hai (`SELECT` đúng 1 dòng cuối) để tính `LastKey`, giúp giảm tải nối chuỗi hàng loạt.
- **Phương án 3 (Đẩy việc ghép chuỗi về code C#):**
  - Khó khả thi vì câu truy vấn SQL đang được cấu hình động trong CSDL, nếu đẩy thêm quy tắc ghép chuỗi về C# sẽ phức tạp.
- **Quyết định:** Trước mắt giữ nguyên **Phương án 1** cho chạy ổn định và đúng nghiệp vụ; việc chuyển sang Phương án 2 khi cần tối ưu chỉ cần chỉnh lại câu truy vấn.

### 1.5. Tùy chọn Phản hồi Lỗi Chiều Nhận (`Inbound`) & Định dạng `ASN.1`
- **Cấu hình tích chọn "Có phản hồi lỗi / Không phản hồi lỗi" cho chiều Nhận (`Inbound`):**
  - Ở chiều Nhận dữ liệu (đối tác gọi API gửi dữ liệu sang hệ thống mình), nếu dữ liệu thiếu giá trị khai báo hoặc bị lỗi mà hệ thống chỉ ghi log âm thầm thì phía đối tác không biết dữ liệu của họ bị lỗi.
  - **Giải pháp chốt:** Bổ sung một ô tích chọn (checkbox) trong cấu hình đối tác / gói tin chiều Nhận: **"Có phản hồi lỗi"** (hoặc không phản hồi).
    - **Khi tích chọn:** Nếu kiểm tra dữ liệu nhận vào thấy lỗi, hệ thống vừa ghi log nội bộ (`Activity` / log hệ thống) vừa trả thông tin lỗi trực tiếp trong kết quả phản hồi HTTP Response (JSON) về cho đối tác.
    - **Khi không tích chọn:** Xử lý âm thầm, chỉ ghi nhận lỗi vào log bên mình, không trả lỗi chi tiết về phía đối tác.
  - **Phạm vi áp dụng:** Chỉ áp dụng cho chiều Nhận dữ liệu (khi đối tác gọi vào mình). Không làm cơ chế gọi ngược (callback) sang URL/WebSocket của đối tác khi mình tự đi kéo dữ liệu về, và chiều Gửi đi (`Outbound`) thì do mình chủ động gửi nên phía đối tác tự trả kết quả về.
- **Giao thức Kết nối & Định dạng `ASN.1`:**
  - Phần cấu hình chọn loại giao thức hiện tại chỉ để mức tạm thời; định hướng triển khai thực tế là thuyết phục tất cả đối tác sử dụng **`API` (HTTP REST)**.
  - Riêng chuẩn định dạng **`ASN.1`** bắt buộc phải triển khai để phục vụ kết nối và chia sẻ dữ liệu với **Trung tâm Quốc gia** theo quy định trong tài liệu yêu cầu kỹ thuật.

---

## 2. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

### PHẦN 1: ĐỊNH DẠNG KHÓA NỐI ĐUÔI (`LASTKEY`), ĐẶT LẠI MỐC TRÊN UI & ĐÁNH GIÁ KIỂM TẢI SERVER
*(Trích xuất từ tệp ghi âm: `MakeUp Chi Ngô Gò Vấp.m4a` — Thời lượng: 17 phút 30 giây)*

#### 1.1. Hiển thị nguyên bản `LastKey` và chuẩn hóa định dạng thời gian `yyyyMMddHHmmssfff`
- `[00:00 - 00:10]` **Tech Lead:** ...cái `LastKey` cuối cùng vô. Là cái mốc đang chạy nếu đúng là nên hiện cái `key` luôn, chứ đừng có hiện cái nội dung như vầy, cái nội dung như vầy rất là khó.
- `[00:11 - 00:17]` **Dev:** Nếu mà hiện... từ từ cho em hỏi cái: nếu mà hiện nguyên cái `key` á là có phải là hiện như vầy không, hay là sao?
- `[00:18 - 00:38]` **Tech Lead:** Ừ, đúng rồi. Là cái `key`... không, cái `key` đó cũng chưa chuẩn nữa. `Key` tốt nhất là mình có cái thời gian: thứ nhất là thời gian đừng có cách, viết liền luôn. Ví dụ: `20261116102001123`.
- `[00:39 - 00:42]` **Dev:** Ví dụ là `20261116`...
- `[00:43 - 01:12]` **Tech Lead:** Đúng rồi, tại vì em viết làm sao, nói chung là một cái thứ tự như thế nào để khi nó `sort` là nó sẽ `sort` được. Ví dụ như ở `20261110` đi, ví dụ như đến `20261111` đi, thì nếu mình `order` thì nó vẫn `sort` đúng, có nghĩa là cái rule này mình vẫn `sort` đúng. Miễn sao là khi mà áp vào cái rule sắp xếp, cái thứ tự của mình nó `sort` được chính xác là được.
- `[01:13 - 01:20]` **Dev:** Cái `key` mà nó `sort` thì nó cũng đề xuất là kiểu cái thứ tự đi như anh là năm – tháng – ngày, còn chỉ là chưa xử lý liền thôi.
- `[01:21 - 01:36]` **Tech Lead:** Tại vì mình ở trên mình đảo ngược lại đi, ví dụ ngày – tháng thì nhiều khi nó lại lệch, nó không khớp hay gì đó. Đó thì mình sẽ xét theo cái rule này. Cái rule trước giờ của mình ở đây là: năm – tháng – ngày – giờ – phút – giây, rồi mili-giây.
- `[01:37 - 01:40]` **Dev:** Nó cũng đang có đủ á anh, chỉ là cái này nó không viết liền kề thôi.
- `[01:41 - 01:58]` **Tech Lead:** Đúng rồi, thì viết nó lại thành một cái cấu trúc để nó dễ check hơn, với lại nó tiết kiệm cái vùng ghi. Ví dụ như mình viết liền thì nó sẽ đỡ mấy cái ký tự gạch, cách gì đó.
- `[01:59 - 02:00]` **Dev:** `Space` đồ vậy đúng không anh?
- `[02:01 - 02:37]` **Tech Lead:** Đúng rồi, nó sẽ đỡ hơn, thì `key` nó cũng đỡ rối nữa. Thì tốt nhất là nên để cái `key`, tại vì cơ bản là những cái này thường là những người họ biết cách chỉnh thì họ mới dám vô chỉnh, chứ những người không biết chỉnh cũng không cho vô chỉnh đâu. Nên là cái này thường là tụi mình sẽ sử dụng là chính, tụi mình sẽ vô mình kiểm tra. Đó, thì thuận tiện nhất thì cứ để cái `key`, tại vì chưa chắc là lúc nào cơ sở dữ liệu cũng cho phép mình gán cái ngày tháng.
- `[02:38 - 03:12]` **Tech Lead:** Thì có những cái trường nào đó mà trong cái cơ sở dữ liệu nó đẩy lên được thì mình cứ đẩy lên trước cho nó dễ, ví dụ như những cái khóa đang lưu nè. Tại vì cơ bản cái khóa đó nó cũng không mang thông tin quan trọng gì hết, nó không phải là thông tin bắt buộc, cũng không phải là thông tin mà có cái độ bảo mật cao. Nó chỉ là cái trường nó chạy đến mốc nào thôi, thì mình cứ hiện sao thì mình cứ hiện lên như vậy, không cần phải biến đổi đâu. Tại biến đổi lên nó cũng hơi khó hiểu. Thì hiện cái `key` cho nó dễ.
- `[03:13 - 03:19]` **Dev:** Rồi, vậy thôi anh coi giùm mấy cái chốt này là mình sẽ theo format này luôn không để em đổi. Ok, để em note lại.

#### 1.2. Tùy biến phần đuôi của Khóa Nối Đuôi (`Code`, `Lane`, `ZoneCode`, vị trí lý trình) thay cho `ID`
- `[03:20 - 03:28]` **Tech Lead:** Còn cái đuôi ở sau thì xem cái khóa nào nó phù hợp hơn. `ID` thì nó hơi dài á.
- `[03:29 - 03:36]` **Dev:** Đuôi này hả anh? Tại vì em có thử mấy cái mã như đối tác nè, rồi mã gói tin thì nó...
- `[03:37 - 04:30]` **Tech Lead:** Tại cái rule hôm bữa mà nói với hai đứa là tại sao phải nối chuỗi á, là để tránh trường hợp là cùng một cái dữ liệu nó bị trùng thời gian thôi. Thì ví dụ như cái dữ liệu này, mình xem coi khả năng xác suất cao nó sẽ trùng và nó khác nhau ở đâu, thì mình chỉ cần kiếm cái chỗ nào chắc chắn nó sẽ khác nhau á để mình cộng vô. Ví dụ như ở lúc trước cái dữ liệu liên quan đến thu phí đi, thì 100% là chắc chắn sẽ khác làn. Không thể nào mà một cái dữ liệu thông tin mà nó cùng một làn, ủa lộn cùng hai làn một thời điểm được, nó bắt buộc phải khác làn. Không thể nào một xe vô một làn này và với lại một làn khác được, thì chắc chắn là làn nó phải khác, thì mình sẽ cộng làn vô.
- `[04:30 - 04:39]` **Tech Lead:** Thì tương tự như dữ liệu này, ví dụ như là nó sẽ có theo cái gì...
- `[04:39 - 04:40]` **Dev:** Chỉ có mã đối tác, mã gói tin thôi.
- `[04:41 - 04:55]` **Tech Lead:** Ví dụ như cái này là dữ liệu về cái `ZoneStatus` đúng không? `ZoneStatus` thì có thể sẽ lấy cái ví dụ như là `ZoneName`, `ZoneCode` gì đó.
- `[04:56 - 05:03]` **Dev:** Cái `ID` hiện tại em đang lấy là của `ID` này, `ID` này em lấy của cái bảng `Zone`, `ZoneStatus` này.
- `[05:04 - 05:09]` **Dev:** Vậy là thành ra là mỗi cái `LastKey` là mỗi cái đều khác nhau hết đúng không anh? Cái phía sau là do mình quy ước đúng không?
- `[05:10 - 05:22]` **Tech Lead:** Đúng rồi! Thì có thể là lấy dựa trên ví dụ như là cái `ZoneCode` đi. Nó lấy dựa trên `ZoneCode` thì nó sẽ ngắn lại, mà đảm bảo là nó cũng khác nhau.
- `[05:23 - 05:27]` **Dev:** Đa số các dữ liệu của mình là đều có `Code` đó, trừ dữ liệu mà thu thập...
- `[05:28 - 05:44]` **Tech Lead:** Tại `ID` nhiều khi nó dài quá, `ID` nó bị dài, thì mình có thể chuyển nó qua `ZoneCode` như vầy. Không nhất thiết là phải cái nào cũng `ID` hết. Tương tự mấy cái khác cũng vậy, ví dụ như là dữ liệu liên quan đến giao thông đi, hay là mấy cái khác.
- `[05:45 - 05:56]` **Dev:** Nhưng mà mấy dữ liệu khác á là cái `Code` nó vẫn có dữ liệu đúng không anh? Theo như em đang hiểu là cái chỗ này thay vì `ID` là giờ là đổi `Code` nè, đúng không?
- `[05:57 - 06:20]` **Tech Lead:** Thì nó sẽ có tùy, nói chung là tùy theo dữ liệu. Ví dụ như `Zone` này thì lấy `Code`, còn ví dụ như là thằng `Incident` thì có thể kết hợp cái thời gian với lại cái vị trí lý trình, `Km` với `Number` chuyển nó qua. Đấy.
- `[06:21 - 06:46]` **Dev:** Vậy theo như anh nói cái chỗ mà cái mốc `LastKey` này nè, là cái người viết nó có thể tùy biến luôn?
- `[06:26 - 06:27]` **Tech Lead:** Đúng rồi.
- `[06:28 - 06:46]` **Dev:** Thay vì là hiện tại mình đang rule đơn giản thôi là `ID`, giờ nó có thể tùy biến và mình sẽ detect cái đó mình lưu. Và ngày tháng thay vì mình đang format như vậy thì bây giờ mình sẽ viết liền vô luôn, kèm theo cái `LastKey` mà cái thằng viết nó muốn. Ok. Thêm tuần nữa rồi! *(cười)*
- `[06:47 - 07:09]` **Tech Lead:** Cái này mình cho nó tự chỉnh bằng cái một mà.
- `[06:55 - 07:09]` **Dev:** Nhưng mà cái này nói chung chỉ cần sửa câu query thôi là nó ra. Rồi xong rồi mấy cái dữ liệu cũ lưu này thì để coi mai mốt là em xóa hết cho nó chạy lại từ đầu, cho nó sạch giống bữa đó.
- `[07:05 - 07:09]` **Tech Lead:** Ừ, xóa hết cho nó chạy lại thôi.

#### 1.3. Thao tác Đặt lại mốc (`Reset`) và Chỉnh sửa trực tiếp chuỗi `LastKey` trên UI
- `[07:10 - 07:20]` **Dev:** Ủa cái này không có liên quan, ví dụ như anh đặt cái `key` mới rồi anh reset `key` lại không được hả? Đặt cái `key` mới đúng không, rồi xong xóa cái cũ thì nó vẫn lưu theo cái mới thôi.
- `[07:21 - 07:38]` **Tech Lead:** Thì mới nói là cái chỗ mà đặt lại á, thì tốt nhất là cho người ta nhập, người ta nhập lại đúng cái cấu trúc đó luôn. Ví dụ như là bây giờ muốn nó chạy lại từ tháng 1, thì điền lại cái này y chang vậy là được: ví dụ `20260101` bla bla gì đó.
- `[07:39 - 07:45]` **Dev:** Tại cái chỗ này á thì em chỉ cho nó ngày tháng thôi, xong cái phần đằng sau nè trên hệ thống nó tự detect nó tự nối vô.
- `[07:46 - 08:04]` **Tech Lead:** Tại nhiều tình huống là muốn nó chạy từ cái nào luôn. Thì ví dụ như giờ muốn chạy từ tháng 1 đi, mà người ta lười nhập phía sau thì nó vẫn chạy đúng mà, đâu có sai đâu. Nếu mà nó sắp xếp thì nó vẫn đúng, đúng không? Gặp phía sau để trống thì nó vẫn `order` đúng mà, nó đâu có bị lỗi gì đâu.
- `[08:05 - 08:12]` **Dev:** Vậy là cái đống ở dưới đây anh muốn bỏ hết cái sửa ở dưới đây hay cho sửa ở trên thôi hả anh? Cho sửa cái chỗ theo mốc thời gian chạy á?
- `[08:13 - 08:20]` **Dev:** Cái mẫu chung này nè là nó đang hiển thị tất cả những đối tác đang dùng mẫu chung luôn, và những cái đối tác này nó đang có một cái mốc riêng.
- `[08:21 - 08:38]` **Tech Lead:** Ừ, thì cái này ok không sao hết. Có nghĩa là cái phần liên quan đến thiết lập lại cái giá trị đó, thì sẽ cho người ta thiết lập theo cái giá trị đó luôn, không cần phải làm mấy cái bước chọn chọn này đâu.
- `[08:39 - 08:44]` **Dev:** Anh nói cái chỗ thiết lập giá trị là chỗ nào, cái chữ `LastKey` màu vàng theo mốc thời gian chạy đúng không? Hay là có nút...
- `[08:45 - 09:15]` **Tech Lead:** Bấm vô `Edit` kìa, xong rồi cho chọn thời gian kìa, đó, mấy đó khỏi, cái đó để một cái `Input` này được rồi. Điền lại cái giá trị cũ. Ở trên em có thể em bớt, em kéo thời gian gì đó. Người ta muốn sửa gì người ta sửa.
- `[09:16 - 09:35]` **Dev:** Còn cái nút Đặt lại kia là gì? `Reset` á, `Reset` lại về trống luôn á, là đi lại từ đầu á.
- `[09:28 - 09:38]` **Tech Lead:** Cho về trống là chạy từ đầu. Thì nó bằng thao tác là xóa cái đó đó. Cái hồi nãy là xóa cái `key` đi, hai cái này như nhau.

#### 1.4. Thảo luận vị trí đặt bảng quản lý Mốc đang chạy giữa các Tab cấu hình
- `[09:42 - 09:56]` **Dev:** Anh còn review cái UI gì không, để em làm nốt luôn. Vậy cái mốc này hay là tách hẳn để riêng qua luôn, để riêng qua một cái tab riêng á, để mình control nó luôn?
- `[09:57 - 10:05]` **Tech Lead:** Riêng thì giờ hơi rối, nhiều quá rồi. Để ở dưới thôi, tại vì nó cũng ít thông tin.
- `[10:06 - 10:23]` **Dev:** Cái mốc đang chạy hôm bữa em tính là để qua bên chỗ cấu hình á, anh qua cái cấu hình đi. Là để dưới mấy cái thằng `Packet` này nè, em tính dời qua bên đó, đang xem có khả thi không.
- `[10:24 - 10:45]` **Dev:** Thì họ thao tác được luôn, tại vì ví dụ cứ mỗi lần mà kiểu muốn đặt lại mốc là phải dừng cái gói tin xong rồi bật tắt lại. Ý là kiểu mà gói đang chạy đó, để thao tác được ở đây thì mình phải qua đây mình tắt cái, xong qua lại bên cấu hình gói tin để mình `Edit`, nên muốn để ở đây luôn cho nó tiện.
- `[10:46 - 11:05]` **Dev:** Do giờ mình cấu hình, ví dụ như mình đặt lại cái đó là mình phải đặt theo đối tác mà, vừa đối tác vừa gói tin luôn mà đúng không? Thì để bên cấu hình cũng hợp lý. Ví dụ như bấm vô hành động của cái gói tin đó.
- `[11:06 - 11:25]` **Dev:** Như gói tin gửi đi `101` á, ý là bấm này sổ ra đây hả?
- `[11:13 - 11:25]` **Tech Lead:** Ừ đúng rồi, có nghĩa là mỗi lần bấm gói tin nó sẽ có một cái `children` ở dưới sổ ra là chạy đang theo mốc bao nhiêu, có thể đặt lại, bật tắt ở trên.
- `[11:26 - 11:36]` **Dev:** Mọi người cứ review UI đi, để em nhớ logic em sửa logic trước nha. Đúng rồi, họ xem được, có nghĩa là để đó thì nó cũng hoàn toàn... ví dụ mà anh mới tạo chưa chạy thì nó không hiển thị.
- `[11:45 - 12:04]` **Dev:** Nó chỉ lưu cái mới nhất thôi mà đúng không? Đang lưu đến chạy hiện tại, cái mới nhất á, đi theo đối tác, gói tin. Là nó chỉ lưu có một thôi đó.
- `[12:05 - 12:25]` **Dev:** Trong cái tab chỉnh sửa cái gói tin đó nó có cái gì không? Cái gói tin để chọn đối tác á, ngay chỗ sát bên `Delete` á, chỉnh sửa. Nó chỉ có thời gian thôi, chu kỳ gửi dữ liệu mới.
- `[12:26 - 13:15]` **Tech Lead:** Để đây thì nó lại có một cái vấn đề là cái trường đó là cái gì, từ đâu đến đâu thì hơi khó, lại phải chép luôn cả cái khóa qua. Ví dụ như là em bấm lại cái cấu hình gói tin đi... Đúng rồi, cái khóa đó nó kết hợp từ trường nào trường nào là mình phải đi mình chép nguyên cái đoạn đó qua nữa, cũng hơi ấy. Tại giờ hiện cái mã vậy không thì không hình dung được cái mã đó đang kết hợp từ thằng nào, thì nó lại hơi khó.
- `[13:10 - 13:17]` **Dev:** Còn nếu mà để ở đây lúc em sửa thì em nhìn được ở đây luôn là em biết gì để em sửa, ý của anh là vậy.
- `[13:34 - 14:29]` **Dev:** Ví dụ anh chọn đối tác, cái chỗ chọn đối tác mẫu chung á, ở bên `Edit`, chọn đối tác khác coi... À đó đúng rồi, nó vẫn hiển thị riêng. Ok không? Rồi mấy cái này vẫn như cũ nha, không có sửa gì hết á.

#### 1.5. Kết quả xử lý Kiểm tải Server (`N+1 Query`, `Index`) & Lưu ý Hạ tầng Ổ cứng `HDD RAID 5`
- `[14:30 - 14:33]` **Dev:** Ủa bữa giờ anh chạy cái con Server nó ok không?
- `[14:34 - 15:09]` **Dev:** Thì ổn rồi đó, hôm bữa là điều tra này nọ rồi thêm `Index` vô thôi, thì một phần anh cũng spam dữ quá mà! Với lại mấy cái chỗ nào mà `N+1 query` á thì cũng đang giảm lại xuống `1 query` hết rồi. Mà do cái hôm qua một phần là anh spam nhiều đối tác quá, thực tế chắc cũng không đến nỗi đâu. Anh đã làm trong những khả năng anh có thể rồi, em đừng lo! *(cười)*
- `[15:10 - 15:15]` **Dev:** Em nghĩ căng lắm tầm 20 cái gói gửi đi thôi.
- `[15:16 - 15:35]` **Dev:** Hôm bữa anh spam tính ra là 2 nhận 5 gửi. Cái cục `ShareData` này nè là vừa nhận gửi nè, còn này là nhận nè, gửi, gửi, gửi, gửi.
- `[15:36 - 15:53]` **Dev:** Hôm bữa bật lên đứng máy luôn. Tại nó ghi nhiều quá, nó ghi ổ cứng á, ghi mà sợ luôn. Tại lúc anh cấu hình anh spam anh để cùng lúc là 30 giây hết, nó dồn vô một lần luôn.
- `[15:54 - 16:03]` **Dev:** Ủa vậy giờ anh có giảm bớt cái độ ghi log lại chưa?
- `[15:57 - 16:03]` **Dev:** Rồi, giảm hết rồi. Cái code mới push lên á là kiểm tra sửa hết rồi đó.
- `[16:04 - 16:15]` **Tech Lead:** Tưởng là `SSD` chứ không phải, `HDD` hả?
- `[16:06 - 16:15]` **Dev:** Ủa nhưng mà mình ra site mình làm cho người ta là dùng `HDD` hay `SSD` anh?
- `[16:11 - 16:41]` **Tech Lead:** `HDD`. Lưu dữ liệu nhiều lắm, `SSD` giá không chịu nổi, không có tiền. Tại vì nó phải cài `RAID`, `RAID 5` nữa. `RAID 5` là phải ít nhất 4 ổ, 4 ổ là lấy được 3 đó. `HDD` rẻ, chứa nhiều không sao.
- `[16:45 - 17:20]` **Dev:** Rồi em sẽ sửa lại mấy cái phần này nha, về cũ với lại xử lý lại cái phần lưu... Tại vì em đang fix cứng rule rồi, giờ mình đang theo hướng linh động thì để em sửa lại xíu. Dữ liệu `ID` em xóa hết luôn rồi em chạy lại thử, là đẹp.
- `[17:21 - 17:30]` **Dev:** Ủa, vậy lỡ sau này mình chuyển cái đó thì sao? Ví dụ như cái convert này sau này mình chạy `ID` rồi mai mốt mình chuyển qua `Code`...

---

### PHẦN 2: CÁCH NHẬP MỐC `LASTKEY` KHI ĐỔI CẤU TRÚC QUERY & ĐÁNH GIÁ LẠI 3 PHƯƠNG ÁN SINH KHÓA
*(Trích xuất từ tệp ghi âm: `MakeUp Chi Ngô Gò Vấp 2.m4a` — Thời lượng: 02 phút 14 giây)*

- `[00:00 - 00:11]` **Tech Lead:** Bây giờ mình chuyển cái câu truy vấn lại thành cái thời gian kiểu này và cái `Code` đi, thì bắt buộc là mình phải nhập cái `LastKey` theo đúng cái cấu trúc mới của mình.
- `[00:11 - 00:15]` **Dev:** Thì cũng là `10`, `11`, `10` rồi ở dưới đây sẽ là cái `Code`, sẽ là ghi cái tên `Zone` của mình?
- `[00:16 - 00:35]` **Tech Lead:** Đúng rồi, thì lúc đó nó sẽ chạy theo cái mới, mấy dữ liệu cũ nó đâu ảnh hưởng. Do chủ yếu cái này là cái để rào cái mốc chạy dữ liệu đến cái thời điểm nào thôi, chứ nó không ảnh hưởng dữ liệu. Dữ liệu là nó dựa theo cái cấu trúc `Mapping` ở những cái khác rồi.
- `[00:36 - 00:40]` **Dev:** Thì hôm bữa là có 3 phương án, mà phương án thứ ba là dùng C# anh đâu có lấy đâu.
- `[00:41 - 01:02]` **Tech Lead:** Thì có một cái phương án là nó còn một cái phương án nữa là: cái `LastKey` đó không có lưu, mà sẽ dựa trên cái `Code` chạy đến cái dòng cuối cùng á, `SELECT` cái gì đó ra là viết thêm một cái câu truy vấn thứ hai.
- `[01:02 - 01:07]` **Dev:** Cái kiểu mà anh nói là đang có 2 lần truy vấn á, hôm bữa lên có nói vậy.
- `[01:08 - 01:31]` **Tech Lead:** Thì cái dữ liệu thô là mình không có convert nó nữa, bỏ cái dòng đó luôn. Mình sẽ đẩy nó thành cái câu truy vấn thứ hai, thì đến cái dữ liệu cuối cùng nó mới gọi cái đó. Thì lúc đó là nó chỉ `SELECT` có 1 dòng thôi, nó chỉ `JOIN` đúng 1 dòng thôi, thì nó đỡ ảnh hưởng nhiều. Đó là cái cách thứ hai.
- `[01:32 - 01:44]` **Tech Lead:** Còn cách thứ ba là đẩy hết về C#, mà đẩy hết C# thì nó hơi khó.
- `[01:39 - 01:44]` **Dev:** Ở đây thì đang để SQL sẵn trong đây, để vô C# nữa thì khó dữ.
- `[01:45 - 01:51]` **Dev:** Thì hôm bữa anh bàn 3 phương án anh nói thôi cứ làm một cái trước đi cho nó chạy đúng đi, rồi sửa lại nó cũng dễ không sao.
- `[01:52 - 02:14]` **Tech Lead:** Cách này thì bên Hóa đơn điện tử vẫn đang xài nè. Nó có khuyết điểm là cột đó nó phải xem hết thôi. Ví dụ 100 dữ liệu thì phải xem hết 100 dòng dữ liệu. Nó phải nối chuỗi hết 100 dòng đó, thì nó sẽ nặng.

---

### PHẦN 3: TÙY CHỌN PHẢN HỒI LỖI CHIỀU NHẬN DỮ LIỆU (`INBOUND`) & ĐỊNH DẠNG `ASN.1`
*(Trích xuất từ tệp ghi âm: `MakeUp Chi Ngô Gò Vấp 3.m4a` — Thời lượng: 05 phút 01 giây)*

#### 3.1. Cơ chế tích chọn "Có phản hồi lỗi / Không phản hồi lỗi" cho chiều Nhận (`Inbound`)
- `[00:00 - 00:06]` **Tech Lead:** ...thì báo lỗi lại ngược về cho nó luôn, để nó biết là nó bị lỗi.
- `[00:16 - 00:26]` **Tech Lead:** Thêm một cái tích ở đây nữa: khi mà không có giá trị khai báo...
- `[00:26 - 00:33]` **Dev:** Là lỗi hay là cho phép ghi? Rồi, một là cho phép ghi, hai là báo lỗi nó không ghi khi nó chạy.
- `[00:34 - 00:49]` **Dev:** Hôm bữa thằng đó nó cũng nói vậy á, nó có nói nhưng mà anh không nghĩ tới nhiều, anh tập trung cái kia trước. Kiểu giờ cái trên đó hay bị âm thầm á, ở ngoài nó bị âm thầm nó không có báo gì hết á.
- `[00:52 - 00:57]` **Dev:** Ủa báo lỗi vậy ghi log thôi đúng không anh? Ghi vô cái `Activity` nhưng mà...
- `[00:58 - 01:07]` **Tech Lead:** Thứ nhất là ghi log của mình, thứ hai nữa là phản hồi về cho bên họ là bị lỗi.
- `[01:03 - 01:08]` **Dev:** Ok, giống như cái mã lỗi á hả? Báo về cái JSON trả về cho bên họ á.
- `[01:09 - 01:22]` **Dev:** Nhưng mà lỡ nó cấu hình khác thì sao thấy được? Ủa, của họ nha, ví dụ như chiều nhận về đúng không, thì mình gọi API của họ mà mình đâu có thằng nào mà mình bắn ngược lại được? Chỉ có họ gọi API mình, mình mới trả lại lỗi được thôi.
- `[01:23 - 01:35]` **Tech Lead:** Thì mới nói là cái đó là cái tích đó! Cái tích là như vậy. Tùy ăn thua, ví dụ như là mình gửi về được thì mình bật cái tích lên, còn ví dụ như là giờ không gửi được thì mình chỉ ghi nhận lại thôi.
- `[01:36 - 02:00]` **Dev:** Không, làm vậy khó lắm anh, thôi để tự ấy đi, để tự ghi nhận thông báo đi, chứ làm cái kiểu đó nha là nó còn phải liên quan tới: à cái phương thức là `API` thì phải gửi về cái `URL` này, hay là phương thức `WebSocket` là phải gửi trực tiếp, hay là phương thức gì đó là phải... nó mở rộng nhiều lắm luôn á!
- `[02:14 - 02:34]` **Tech Lead:** Còn không thì check ở ngay chỗ lập cấu hình luôn, thiết lập đối tác, chỗ gói nhận gói gửi đó: có phản hồi lỗi hoặc là không phản hồi.
- `[02:35 - 02:49]` **Dev:** Nếu mà có một cái chung thì ok nha, em thấy nếu mà có cái cấu hình chung thì nó sẽ ok.
- `[02:44 - 02:49]` **Tech Lead:** Có phản hồi, không phản hồi thôi, vậy cho nó dễ.
- `[02:50 - 03:09]` **Dev:** Bên nhận về á, chủ yếu là bên nhận về, nhận về đang gọi API trên đó cho đối tác. Từ từ, có phản hồi rồi suy ra bên nhận về, vậy là tích vậy xong rồi bên đối tác nó tự cầm cái tích này nó xử lý hả?
- `[03:10 - 03:29]` **Dev:** Không, nếu mà tích vậy là phải cấu hình: nếu mà có lỗi là phải gọi vô cái đó, rồi gửi cái nội dung đó vô cho họ. Nếu mà `API` thì nó dễ, chỉ có đưa `URL` với cái `Token` vô là cấu hình cái đó, nhưng mà nếu mà `WebSocket` thì em phải cấu hình theo sao?
- `[03:30 - 03:52]` **Tech Lead:** Có nghĩa là đang về một cái hướng là cái hướng nhận về thôi! Có nghĩa là bây giờ họ gửi dữ liệu cho mình, mình check mình thấy nó bị lỗi, thì nếu mà có cái tích phản hồi đó thì trả cái lỗi về cho họ. Còn nếu mà không tích thì xử lý âm thầm thôi, là bên mình chỉ ghi nhận là dữ liệu bị gì đó. Đấy, là được.
- `[03:53 - 04:10]` **Tech Lead:** Còn cái chiều gửi đi chắc chắn là phải đúng rồi, tại vì chiều gửi đi là mình gửi mà. Thì bị lỗi hay không thì phải do bên họ trả về chứ đâu phải mình tự mình check mình tự mình phản hồi được. Nên là chỉ rào ở bên nhận thôi, rào bên nhận là chủ yếu.

#### 3.2. Giao thức kết nối (`API`) và định dạng `ASN.1` cho Trung tâm Quốc gia
- `[04:11 - 04:18]` **Dev:** Ủa, bây giờ cái chỗ cấu hình đây mình có cấu hình được cái loại giao thức chưa?
- `[04:19 - 04:31]` **Tech Lead:** Có đó, bữa có yêu cầu đó, nhưng mà đang tạm thôi chứ chưa có xài được đâu. Đợi khi nào mà chính thức thì mình làm sao.
- `[04:32 - 04:40]` **Dev:** Chắc cơ bản `API` hết thôi.
- `[04:35 - 04:40]` **Tech Lead:** Thì cố gắng là đẩy tất cả qua `API` đó, cố gắng là thuyết phục.
- `[04:41 - 04:56]` **Dev:** Còn một cái chắc chắn phải làm là cái `ASN.1` nha! Cái đó phải làm cho bên Trung tâm Quốc gia, chứ nó có ghi trong tài liệu á.
- `[04:57 - 05:01]` **Dev:** Ok, em hết câu hỏi rồi.
