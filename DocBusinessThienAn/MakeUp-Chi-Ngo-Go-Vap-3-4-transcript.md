---
tier: A
read: full
source:
  - DocBusinessThienAn/MakeUp Chi Ngô Gò Vấp 3.m4a (35:36)
  - DocBusinessThienAn/MakeUp Chi Ngô Gò Vấp 4.m4a (25:41)
date: 2026-09-09
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: Review ShareData toàn diện (Cấu hình gói tin, SQL Alias Mapping, Lịch trình gửi theo giờ/data mới, Fix lỗi Hoa-Thường, Ghi log & Tinh gọn UI)
participants:
  - Anh Sơn (Tech Lead / Kiến trúc hệ thống)
  - Hiếu (Dev Backend / Service Worker / ShareData)
  - Đạt (Dev Backend / Web API / Database)
---

# Kịch bản & Biên bản Cuộc họp: Review Toàn diện Phân hệ ShareData
### (Nguồn từ: MakeUp Chi Ngô Gò Vấp 3 & MakeUp Chi Ngô Gò Vấp 4)

- **Thời lượng tổng:** 61 phút 17 giây (Tệp 3: 35:36 | Tệp 4: 25:41)
- **Người tham gia:** Anh Sơn, Hiếu, Đạt
- **Chủ đề chính:** Chuẩn hóa luồng gửi ShareData qua WebAPI, cơ chế SQL Alias Mapping, khắc phục lỗi phân biệt chữ hoa/thường (Case-Sensitivity) trong CodeSet, bổ sung lịch trình gửi dữ liệu theo giờ cố định (9h sáng), bỏ cấu hình thừa (sự kiện / gửi 1 lần) và thêm switch tự động gửi khi có dữ liệu mới.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Chuẩn hóa luồng gửi (Outbound) trực tiếp qua WebAPI
- **Vấn đề cũ:** Luồng gửi ShareData trước đây phụ thuộc vào việc ghi dữ liệu ra tệp trung gian (file), sau đó một tiến trình khác lại đọc log để quét file gửi đi. Cách này làm tăng độ trễ, cồng kềnh và khó kiểm soát trạng thái gửi.
- **Quyết định:** 
  - Sau khi Service trích xuất dữ liệu từ DB và chạy qua Mapping Engine xong, **bắn thẳng payload JSON vào WebAPI đối tác** (hoặc gửi qua SSE/Socket).
  - Tệp file chỉ đóng vai trò là bản sao lưu trữ (backup / export) nếu có yêu cầu lưu trữ cứng, không dùng file làm kênh trung chuyển bắt buộc.

### 1.2. Cơ chế SQL Alias Mapping (Tách tầng truy vấn khỏi tầng xuất bản)
- **Quy tắc phân biệt:**
  - `Tên cột DB (Column Name)`: Tên vật lý dưới bảng CSDL (ví dụ: `toc_do`, `luu_luong`, `ma_tram`).
  - `Bí danh truy vấn (SQL Alias)`: Tên định danh chuẩn trung gian trong câu lệnh SELECT (`SELECT toc_do AS Speed, ma_tram AS ZoneCode`).
  - `Tên trường đối tác (Partner Field Name)`: Tên field theo schema của từng đối tác cụ thể (ví dụ: đối tác A cần `VehicleSpeed`, đối tác B cần `Velocity`).
- **Lợi ích:** Cùng một câu truy vấn dữ liệu gốc (cho ra các alias chuẩn), hệ thống có thể cấu hình bảng ánh xạ (Mapping Table) để phân phối đến nhiều đối tác khác nhau mà không phải viết lại câu truy vấn SQL hay tạo thêm bảng phụ.

### 1.3. Lịch trình gửi dữ liệu: Thêm chạy theo giờ & Switch dữ liệu mới
- **Loại bỏ:** Bỏ chế độ gửi "theo sự kiện" (Event Trigger) và "gửi 1 lần" (One-time) vì không sát thực tế vận hành và làm rối giao diện.
- **Thống nhất 2 chế độ cốt lõi:**
  1. **Chạy định kỳ (Periodic / Interval):** Lặp lại sau mỗi chu kỳ thời gian (ví dụ: mỗi 30 giây, 1 phút, 5 phút).
  2. **Chạy theo giờ cố định hàng ngày (Daily Scheduled Time):** Bổ sung cấu hình giờ chạy cụ thể (ví dụ: đúng 9h00 sáng mỗi ngày tự động tổng hợp và gửi báo cáo ngày hôm trước).
- **Bổ sung Switch "Tự động gửi khi có dữ liệu mới":**
  - Khi bật switch: Hệ thống tự động phát hiện bản ghi mới trong DB và đẩy đi ngay lập tức.
  - Khi tắt switch: Chạy theo lịch trình định kỳ hoặc theo giờ cố định đã cấu hình.

### 1.4. Khắc phục lỗi Case-Sensitivity (Chữ hoa / Chữ thường)
- **Nguyên nhân lỗi:** Trong CSDL hoặc code C#, tên trường/giá trị lưu chữ thường (`status`, `normal`), nhưng trên giao diện cấu hình dev gõ chữ hoa (`STATUS`, `NORMAL`). Do so khớp chuỗi mặc định phân biệt hoa thường, Mapping Engine không tìm thấy trường và trả về giá trị rỗng/sai.
- **Giải pháp:**
  - Code Mapping Engine phải sử dụng so sánh không phân biệt hoa thường (`StringComparison.OrdinalIgnoreCase`).
  - Trên giao diện cấu hình, danh sách trường phải được load từ metadata DB dưới dạng Dropdown/Select để người dùng chọn, tránh cho nhập tay tự do gây sai lệch chính tả.

### 1.5. Tinh gọn giao diện (UI Cleanup)
- Ẩn toàn bộ các trường cấu hình rác/không cần thiết (cột nguồn DB, các tham số kết nối không dùng).
- Đưa thông tin cấu hình chi tiết vào Modal/Popup thống nhất thay vì hiển thị dàn trải toàn bộ trên bảng danh mục.

---

## 2. Ma trận phân công công việc & Tiến độ (Action Items)

| Người phụ trách | Hạng mục công việc | Chi tiết kỹ thuật |
|---|---|---|
| **Hiếu** | Service Worker & Mapping | - Sửa logic Mapping Engine: so khớp không phân biệt hoa thường (`OrdinalIgnoreCase`).<br>- Bỏ chế độ gửi theo sự kiện/gửi 1 lần; thêm switch tự động gửi khi có data mới.<br>- Bổ sung tính năng lập lịch chạy theo giờ cố định (Daily at 9:00 AM).<br>- Tinh gọn các trường thừa trong cấu hình gói tin. |
| **Đạt** | Backend WebAPI & DB | - Xây dựng endpoint WebAPI nhận/gửi dữ liệu trực tiếp.<br>- Chuẩn hóa câu truy vấn SQL có Alias phục vụ mapping.<br>- Bổ sung cột lưu trạng thái và log kết quả gửi trực tiếp vào DB. |
| **Kiên** | Frontend UI | - Cập nhật giao diện: thêm trường chọn giờ chạy hàng ngày, thêm switch tự động gửi.<br>- Chuyển danh sách trường cấu hình sang dạng Dropdown (tránh gõ tay). |

---

## 3. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

---

### PHẦN 1: Tệp MakeUp Chi Ngô Gò Vấp 3 (35 phút 36 giây)
> *Trọng tâm: Luồng gửi trực tiếp WebAPI, SQL Alias Mapping, Kiểm tra dữ liệu 291 dòng và Lỗi CodeSet.*

#### [00:00 - 05:00] Tranh luận về luồng gửi: Bỏ trung gian ghi file
- **Hiếu:** Hiện tại luồng gửi của mình là ghi xuống file, rồi WebAPI sẽ check theo log để biết là file nào gửi thành công hay chưa, rồi sau đó nó mới lấy file đó lên để đẩy đi.
- **Anh Sơn:** Tại sao lại phải ghi xuống file rồi mới gửi? Có bỏ bớt được cái luồng của Đạt không?
- **Hiếu:** Lúc trước anh Đạt làm là ghi xuống file mà anh.
- **Anh Sơn:** Ghi xuống file nhưng bây giờ mình lấy trực tiếp dữ liệu đó gửi đi thôi. Gửi trực tiếp qua WebAPI luôn chứ?
- **Hiếu:** Gửi trực tiếp qua WebAPI luôn hả anh?
- **Anh Sơn:** Ừ! Cần thiết thì mới ghi file để backup, chứ luồng chính là lấy dữ liệu từ DB, format xong là bắn trực tiếp qua API đối tác luôn. Tự nhiên ghi ra file rồi lại đọc file lên gửi làm cái gì cho mất công?
- **Hiếu:** Dạ, cái đó để em xem lại. Trước anh kêu ghi DB rồi viết câu query trên CSDL để người ta query xuống dữ liệu đúng không? Nhưng hiện tại là đang đọc được nguyên cái cấu hình này luôn. Em test thử thêm field DB, thêm field khác vào DB là nó tự động đọc được field đó để đẩy đi.

#### [05:00 - 12:00] Phân biệt rõ: Mã trường (DB Column) vs Tên trường (Partner Field / Alias)
- **Anh Sơn:** Bây giờ cho anh hỏi lại: Cái mã trường là cái tên gì?
- **Hiếu:** Mã trường là tên trong DB, còn tên trường là cái field đối tác.
- **Anh Sơn:** Không đúng! Nhìn lại đi, cái mã trường đang ghi là cái gì kìa?
- **Hiếu:** Tên field trong DB.
- **Anh Sơn:** Tên field trong DB là tên cột. Còn cái tên trường bên cạnh là cái gì?
- **Hiếu:** Là nhãn hiển thị hoặc cái tên đối tác cần.
- **Anh Sơn:** Em phải tách bạch ra:
  1. Tên cột trong CSDL (ví dụ: `toc_do`, `luu_luong`).
  2. Tên bí danh Alias trong câu query (`SELECT toc_do AS Speed`).
  3. Tên trường gửi đi cho đối tác (`VehicleSpeed`).
  Cái phía sau mình chỉ cần quản lý tên Alias thôi. Trong câu SQL của em, em SELECT ra `colA AS FieldA`, thì tầng mapping phía sau nó chỉ cần nhìn thấy `FieldA` để map qua cho đối tác, không cần biết bên dưới DB nối mấy bảng hay viết câu lệnh phức tạp cỡ nào.

#### [12:00 - 18:00] Gói tin 101, Cấu trúc Zone và giải pháp SQL Alias
- **Đạt:** Cái này là dạng như ID hay key thôi đúng không anh?
- **Anh Sơn:** Đúng rồi, cái key đó là cái tên mình sử dụng trong code. Ở trong DB tên gì không biết, nhưng khi ra đến tầng xử lý là mình quy định một cái tên chuẩn (Alias). Vì có tình huống câu truy vấn phải JOIN nhiều bảng (bảng 1, bảng 2), nếu mà cứ lấy theo tên bảng gốc thì rất là mệt. Em cứ đặt alias cho câu truy vấn, rồi từ alias đó mình map ra cho từng đối tác:
  - Đối tác 1: `FieldA` map thành `Name`, `FieldB` map thành `Speed`.
  - Đối tác 2: `FieldA` map thành `TenTram`, `FieldB` map thành `TocDoTB`.
- **Hiếu:** Dạ em hiểu rồi, tức là phía trước giữ nguyên câu query có alias, còn phía sau chỉ map từ alias đó sang schema của đối tác.

#### [18:00 - 24:00] Kiểm tra thực tế câu truy vấn trên CSDL (291 dòng dữ liệu)
- **Anh Sơn:** Bây giờ mở CSDL ra coi thử coi. Câu truy vấn đó chạy ra bao nhiêu dòng?
- **Hiếu:** Dạ câu này chạy ra 291 dòng.
- **Anh Sơn:** Rồi, kiểm tra xem khi nạp dữ liệu lên, nó có ra đúng 291 dòng không?
- **Hiếu:** Dạ đúng 291 dòng luôn anh.
- **Anh Sơn:** Đó, câu truy vấn chạy ra 291 dòng thì khi mapping xong xuất ra kết quả cũng phải đủ 291 dòng, không được thiếu hay duplicate.

#### [24:00 - 30:00] Phát hiện lỗi cấu hình CodeSet và Mapping giá trị
- **Hiếu:** Chỗ này nè anh Sơn, em đang test cái phần CodeSet để map trạng thái của Zone. Trong DB nó đang là giá trị nội bộ `0`, `1`, `2`. Em muốn ra cho đối tác là `0` thành `"Bình thường"`, `1` thành `"Cảnh báo"`.
- **Anh Sơn:** Sao nó không ra?
- **Hiếu:** Nó không ăn cái CodeSet anh, nó cứ ra thẳng giá trị `0`, `1` gốc của DB.
- **Anh Sơn:** Để coi lại... Đó, thấy chưa! Cái giá trị trong DB em đang để kiểu int, mà trên CodeSet em lại cấu hình kiểu khác, hoặc do tên trường em gõ sai chính tả hoa thường. Hai bên không khớp nhau thì làm sao nó tìm thấy để map? Bắt buộc phải chuẩn hóa kiểu dữ liệu và kiểm tra logic so sánh chuỗi trong code.

#### [30:00 - 35:36] Loại bỏ các trường thừa, dọn rác giao diện
- **Anh Sơn:** Mấy cái trường này có cần thiết không? Nhìn nó rối như một đống rác vậy đó.
- **Hiếu:** Dạ mấy cái này là do lúc trước gen tự động từ bảng cũ nên nó bị lặp lại.
- **Anh Sơn:** Bỏ hết mấy cái không cần thiết đi. Chỉ giữ lại những trường thật sự có trong gói tin và cần map cho đối tác thôi. Càng để nhiều trường rác thì người dùng càng khó cấu hình mà code chạy càng dễ phát sinh lỗi.

---

### PHẦN 2: Tệp MakeUp Chi Ngô Gò Vấp 4 (25 phút 41 giây)
> *Trọng tâm: Lịch trình gửi dữ liệu, Bỏ gửi sự kiện/1 lần, Switch tự động gửi, Fix lỗi hoa/thường và Thu phí.*

#### [00:00 - 05:00] Thiết lập Lịch trình gửi dữ liệu: Định kỳ vs Theo giờ cố định
- **Anh Sơn:** Bây giờ xem tiếp phần cấu hình thời gian gửi. Hiện tại hệ thống đang cho cấu hình gửi kiểu gì?
- **Hiếu:** Hiện tại đang cho cấu hình định kỳ theo chu kỳ giây/phút, với lại có một cái theo giờ.
- **Anh Sơn:** Theo giờ là chạy như thế nào? Ví dụ đúng 9h sáng hàng ngày chạy một lần gửi báo cáo đúng không?
- **Hiếu:** Dạ, nhưng mà cái chỗ theo giờ đó nó đang chưa rõ là chạy 1 lần lúc 9h hay là lặp lại trong ngày.
- **Anh Sơn:** Đối tác họ cần là: **Đúng 9h sáng mỗi ngày gửi một lần dữ liệu tổng hợp của ngày hôm trước**. Em phải tách rõ:
  - Một là chạy theo định kỳ lặp lại (Interval: 30s, 60s, 5 phút...).
  - Hai là chạy vào một thời điểm cố định trong ngày (Daily at specific time: ví dụ 09:00:00).

#### [05:00 - 11:00] Bỏ chế độ gửi "Theo sự kiện" & "Gửi 1 lần" — Thêm Switch dữ liệu mới
- **Hiếu:** Trong tài liệu cũ có ghi cái chế độ gửi theo 'sự kiện' (ví dụ phát hiện xe quá khổ hay tai nạn thì gửi), với lại chế độ 'gửi 1 lần duy nhất'.
- **Anh Sơn:** Cái gửi theo 'sự kiện' với 'gửi 1 lần' đó bỏ đi! Bây giờ không có làm kiểu đó nữa.
- **Đạt:** Bỏ luôn hả anh?
- **Anh Sơn:** Bỏ! Gửi một lần để làm cái gì? Để test hả? Test thì bấm nút test trên giao diện là xong, cần gì phải cấu hình chế độ gửi 1 lần chi cho phức tạp.
- **Anh Sơn:** Thay vào đó, em thêm một cái **Switch (hoặc Checkbox)**: `Tự động gửi khi có dữ liệu mới` (Auto-trigger on new data).
  - Nếu bật switch này: Cứ khi nào DB phát sinh bản ghi mới là hệ thống tự động đẩy gói tin đi ngay.
  - Nếu tắt switch này: Hệ thống chỉ gửi theo đúng lịch trình định kỳ hoặc giờ cố định đã cài đặt.
- **Hiếu:** Dạ, vậy thì gọn và rõ ràng hơn nhiều.

#### [11:00 - 16:00] Lỗi hoa - thường (Case Sensitivity) làm hỏng Mapping
- **Hiếu:** Anh Sơn coi giùm em chỗ này, sao cái trường `Status` nó không map được qua bên kia?
- **Anh Sơn:** Đâu, mở cái DB với cái màn hình cấu hình lên xem.
- **Anh Sơn:** Trời đất ơi! Nhìn đi: Trong DB người ta viết là `status` (chữ thường), mà trên cái cấu hình mapping em lại gõ là `Status` (chữ hoa đầu)!
- **Hiếu:** Ủa, viết hoa viết thường nó cũng không nhận hả anh?
- **Anh Sơn:** C# so sánh chuỗi mặc định là phân biệt chữ hoa chữ thường! Em gõ `Status` với `status` là hai chuỗi hoàn toàn khác nhau, làm sao nó tìm thấy để map?
- **Anh Sơn:** Cái này sửa lại 2 việc:
  1. Trong code C#: Bắt buộc dùng `StringComparison.OrdinalIgnoreCase` khi so sánh tên trường hoặc giá trị CodeSet.
  2. Trên giao diện Frontend: Không cho người dùng gõ tay tên trường nữa! Load danh sách các trường từ CSDL lên cho người ta chọn bằng Dropdown, vừa chuẩn xác vừa không sợ sai hoa thường hay sai chính tả.

#### [16:00 - 20:00] Bàn về cơ chế Heartbeat và Giao thức truyền nhận
- **Hiếu:** Còn cái vụ Heartbeat (kiểm tra sống/chết) với đối tác thì mình có cần làm không anh?
- **Đạt:** Heartbeat để bên kia biết server mình còn sống hay bên mình biết server đối tác còn sống?
- **Anh Sơn:** Mình đang gửi dữ liệu qua WebAPI (HTTP POST). HTTP là giao thức request-response: em bắn request qua, nếu server bên kia sống thì nó trả về HTTP 200, nếu nó chết hoặc lỗi thì trả về timeout hoặc 500. Bản thân mỗi lần gửi là đã biết sống chết rồi, không cần phải làm thêm một luồng ping heartbeat riêng làm gì cho nặng hệ thống.
- **Anh Sơn:** Trừ khi nào dùng kết nối Socket TCP giữ liên tục (persistent connection) thì mới cần heartbeat ping-pong. Còn HTTP REST API thì cứ gửi và ghi log mã lỗi phản hồi là đủ.

#### [20:00 - 25:41] Rà soát tổng kết công việc & Thu phí ETC
- **Anh Sơn:** Nhắc lại các việc cần sửa ngay cho ShareData:
  1. Bỏ chế độ gửi theo sự kiện và gửi 1 lần.
  2. Thêm switch 'Tự động gửi khi có dữ liệu mới'.
  3. Bổ sung cấu hình giờ chạy cố định hàng ngày (ví dụ 9h sáng).
  4. Fix triệt để lỗi phân biệt hoa/thường trong Mapping Engine và CodeSet.
  5. Đưa danh sách trường cấu hình thành Dropdown chọn, không cho gõ tự do.
- **Đạt:** Còn bên phân hệ Thu phí (ETC) thì sao anh Sơn?
- **Anh Sơn:** Thu phí thì bên đó họ đang chạy thử nghiệm rồi, dữ liệu thu phí cũng sẽ đi theo luồng chuẩn hóa này luôn. Các em tập trung xử lý xong các điểm chốt của ShareData để tuần sau mình ghép nối kiểm thử toàn diện.
- **Hiếu / Đạt:** Dạ, tụi em rõ rồi anh!\n