---
tier: A
read: full
source: ../../../Plan/_source/audio/16-09-2026-review-frontend-sharedata.m4a
date: 16-09-2026
duration: ~32 phút 40 giây
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData (Chia sẻ Dữ liệu - ESHARE) - Review & Hoàn thiện Giao diện Frontend Cấu hình Đối tác, Gói tin & Ánh xạ Dữ liệu (Mapping)
participants:
  - Anh Sơn (Tech Lead / Kiến trúc hệ thống)
  - Kiên (Dev Frontend / Trình chiếu UI)
  - Đạt (Dev Backend / Web API / Database)
  - Chị Như (QC / Tester - đối tượng định hướng giải thích)
---

# Kịch bản & Biên bản Cuộc họp: Review & Hoàn Thiện Giao Diện Frontend Phân Hệ Chia Sẻ Dữ Liệu (ShareData)

> **Ghi chú:** Bản ghi được tổng hợp và xử lý trực tiếp qua phương thức **Gemini Multimodal Native Audio Understanding** từ tệp ghi âm cuộc họp nội bộ ngày 16/09/2026 (`9.24, 16 thg 9_.m4a`). Nội dung tập trung vào buổi review trực tiếp màn hình Web Frontend phân hệ **ShareData (ESHARE)** giữa Anh Sơn (Tech Lead), Kiên (Frontend) và Đạt (Backend), rà soát chi tiết các bất cập UI/UX, chuẩn hóa luồng nghiệp vụ cấu hình đối tác, gói tin và engine ánh xạ dữ liệu (mapping).

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Chuẩn hóa Bảng danh sách & Giao diện chung (UI/UX)
- **Đồng bộ ngôn ngữ tiếng Việt:**
  - Hiện tại giao diện hiển thị lẫn lộn tiếng Anh và tiếng Việt (ví dụ: `Active`, `Disconnect`, `Connect`...).
  - **Quyết định:** Thống nhất chuyển đổi toàn bộ sang tiếng Việt chuẩn hóa (`Hoạt động`, `Ngắt kết nối`, `Kích hoạt`...).
- **Xử lý cột Cảnh báo:**
  - Logic cảnh báo trong code hiện đang bị comment lại, chưa có cơ chế thực tế khi đối tác từ chối gói tin (reject).
  - **Quyết định:** Tạm thời **ẩn cột/nút Cảnh báo** trên giao diện. Khi nào hoàn thiện đầy đủ logic xử lý cảnh báo ở Backend và quy trình tiếp nhận thì mới bật lại, tránh làm người dùng và Tester hiểu nhầm là tính năng bị lỗi.
- **Bổ sung Mã định danh đối tác (Partner Code):**
  - Danh sách đối tác hiện chỉ hiển thị Tên đối tác mà thiếu Mã đối tác (Partner Code).
  - **Quyết định:** Bổ sung hiển thị rõ ràng cả Mã đối tác và Tên đối tác (hoặc cột Mã riêng, hoặc hiển thị kết hợp `[Mã] Tên đối tác`).
- **Xử lý chuỗi văn bản dài bằng Tooltip chuẩn:**
  - Tên các trung tâm, cơ quan đối tác thường rất dài dẫn đến tràn ô hoặc bị cắt cụt chữ.
  - **Quyết định:** Sử dụng component **Tooltip chuẩn của thư viện UI (Ant Design)** khi hover vào chuỗi dài. Không sử dụng thuộc tính `title` HTML mặc định hay tự custom thủ công gây lệch màu. Style màu sắc của Tooltip (nền đen chữ trắng hoặc theo theme chung) phải đồng bộ tuyệt đối trên toàn hệ thống.
- **Tối ưu nút Hành động (Action buttons):**
  - Các nút thao tác/icon hành động đang bị thiết kế quá to, thô, chiếm dụng diện tích dòng.
  - **Quyết định:** Thu nhỏ kích thước các nút hành động cho thanh thoát, gọn gàng, đúng tỷ lệ UI hiện đại.
- **Sửa lỗi thanh cuộn ngang (Horizontal Scroll) và lệch dòng (Layout Shift):**
  - Khi cuộn ngang bảng dữ liệu, một số cột bị đè (overlap) lên khu vực phân trang hoặc đè lên các cột khác.
  - Cột Số thứ tự (STT) khi cuộn ngang bị mất hoặc lệch hàng với nội dung.
  - **Quyết định:** Cố định (fixed column) hợp lý cho cột STT hoặc điều chỉnh khoảng đệm (padding) để khi người dùng kéo thanh cuộn ngang thì STT vẫn ghim thẳng hàng, rõ ràng.

---

### 1.2. Chuẩn hóa Luồng Cấu hình Đối tác & Gói tin nghiệp vụ
Cuộc họp thống nhất phương pháp giải thích và bố trí luồng cấu hình để Tester (chị Như) và người dùng dễ dàng hiểu bản chất hệ thống mà không bị nhầm lẫn:
1. **Khai báo Đối tác (Partner):**
   - Đăng ký danh sách các đối tác/cơ quan cần kết nối, chia sẻ dữ liệu với tuyến cao tốc.
2. **Cấu hình Gói tin theo Đối tác:**
   - Mỗi đối tác được phép cấu hình nhiều gói tin khác nhau.
   - Gói tin tuân theo danh mục chuẩn được quy định từ trước (ví dụ: các gói tin từ `101` đến `111` đã được seed sẵn trong CSDL hệ thống). Tester không thể tự ý nhập bừa các gói tin ngoài quy chuẩn (như 113, 114) nếu hệ thống chưa có đặc tả nghiệp vụ.
3. **Phân định hai chiều truyền nhận (Inbound & Outbound):**
   - **Chiều gửi (Outbound):** Tuyến Hữu Nghị - Chi Lăng xuất bản dữ liệu gửi cho đối tác.
   - **Chiều nhận (Inbound):** Tiếp nhận dữ liệu đối tác gửi về lưu vào CSDL nội bộ.
4. **Thiết lập Lịch gửi (Schedule):**
   - **Gửi chu kỳ liên tục:** Thiết lập chu kỳ lặp lại theo giây (ví dụ: cứ mỗi 30 giây gửi 1 lần) và có thể giới hạn chạy trong một khung giờ cụ thể.
   - **Gửi theo ngày:** Cho phép chọn các ngày trong tuần (màu xanh thể hiện ngày được bật) kèm mốc giờ gửi cố định (ví dụ: đúng 09:00:00 sáng hàng ngày).
5. **Độ ưu tiên (Priority):**
   - Tính năng độ ưu tiên gói tin hiện chưa được lập trình ở tầng backend.
   - **Quyết định:** Tạm thời **ẩn hoặc disabled** trường này kèm ghi chú tính năng đang phát triển, không để người dùng thiết lập ảo rồi thắc mắc.

---

### 1.3. Cơ chế Ánh xạ Dữ liệu (Data Mapping Engine) & Tối ưu hóa UI
- **Khái niệm và Thuật ngữ giao diện:**
  - Từ ngữ "Mapping" mang nặng tính kỹ thuật, dễ gây bối rối cho bộ phận kiểm thử và vận hành.
  - **Quyết định:** Đổi tên hiển thị trên giao diện thành **"Ánh xạ dữ liệu"**, **"Cấu hình dữ liệu"** hoặc **"Chuyển đổi dữ liệu"**.
  - Bản chất: Là quá trình biến đổi cấu trúc dữ liệu đã thống nhất giữa hai bên thành cấu trúc JSON chuẩn mà hệ thống bên trong xử lý.
- **Bố cục khung ánh xạ:**
  - Phía bên trái: Cây cấu trúc dữ liệu / danh sách trường của gói tin nội bộ.
  - Phía bên phải: Cấu trúc ánh xạ tương ứng của đối tác.
- **Thay thế nút ba chấm (...) bằng Icon chức năng rõ ràng:**
  - Nút ba chấm (...) gây khó hiểu và khó thao tác.
  - **Quyết định:** Đổi thành icon **Cài đặt (Settings/Config)** hoặc **Chỉnh sửa (Edit)** trực quan.
- **Loại bỏ tùy chọn bất hợp lý "Bỏ khóa này khi gửi đi":**
  - Anh Sơn chỉ rõ: Khi người dùng đã chủ động đưa một trường vào mẫu cấu hình để gửi đi, việc tồn tại tùy chọn "Bỏ khóa này khi gửi đi" là mâu thuẫn và dư thừa (nếu không muốn gửi thì ngay từ đầu không thêm trường đó vào danh sách).
  - **Quyết định:** Rà soát và bỏ tùy chọn này ở chiều gửi, tránh gây nhầm lẫn logic cho Tester.
- **Trực quan hóa trạng thái sử dụng Bộ mã quy đổi (CodeSet) và Định dạng (Format):**
  - Khi một bảng mapping có hàng chục trường, người dùng không thể biết trường nào đang được cấu hình bảng mã hoặc định dạng ngày/số nếu không bấm mở từng dòng.
  - **Quyết định:**
    - Tận dụng khoảng trống ở giữa hai cột để hiển thị các biểu tượng (Badge/Icon) trạng thái.
    - Nếu trường có áp dụng bộ mã quy đổi (CodeSet): Icon Bộ mã sẽ sáng lên; hover chuột vào sẽ hiển thị chi tiết tên bộ mã đang dùng.
    - Nếu trường có cấu hình định dạng (Format date, số thập phân): Icon Định dạng sẽ sáng lên; hover chuột vào sẽ hiển thị chuỗi format mẫu.

---

### 1.4. Xử lý các trường Meta hệ thống & Tự động ánh xạ (Auto-mapping)
- **Nhóm trường Meta hệ thống:**
  - Các trường dữ liệu mang tính đặc thù hệ thống như thời gian hiện tại (`meta.now` / `DateTime.Now`), mã yêu cầu (`request_id`), thông tin chứng thực... phải được xử lý tự động trong code Backend.
  - Trên giao diện, gom các trường này vào nhóm riêng **"Trường Meta hệ thống"** để người dùng chọn nhanh thay vì phải tự cấu hình thủ công hoặc nhập tay.
- **Cải tiến nút "Nạp lại trường gói tin" -> "Tự động ánh xạ":**
  - Tên cũ "Nạp lại trường gói tin" gây hiểu nhầm là reload lại trang hoặc reset dữ liệu.
  - **Quyết định:** Đổi tên nút thành **"Tự động ánh xạ"** (Auto Map).
  - Logic hoạt động: Tự động so sánh và khớp các trường có cùng tên kỹ thuật giữa gói tin hệ thống và cấu trúc JSON mẫu.
  - **Hiển thị thông báo kết quả rõ ràng:** Khi thực hiện xong, hệ thống phải hiển thị thông báo tiến độ trực quan (ví dụ: *"Đã tự động ánh xạ khớp 6/12 trường dữ liệu"*).

---

## 2. Ma trận phân công công việc & Kế hoạch hành động (Action Items)

| Thành viên | Trách nhiệm | Công việc chi tiết | Thời hạn |
|---|---|---|---|
| **Kiên** | Frontend UI/UX | 1. Chuyển đổi toàn bộ nhãn tiếng Anh sang tiếng Việt thống nhất.<br>2. Ẩn cột Cảnh báo và trường Độ ưu tiên (Priority) chưa sử dụng.<br>3. Bổ sung hiển thị Mã đối tác (Partner Code) bên cạnh Tên đối tác.<br>4. Tích hợp Tooltip chuẩn của Ant Design cho các trường văn bản dài, đồng bộ màu theme.<br>5. Thu nhỏ kích thước các nút hành động (Action buttons).<br>6. Sửa lỗi layout cuộn ngang (horizontal scroll), ghim cố định cột STT không bị lệch hàng.<br>7. Đổi tên các tab/nút: "Mapping" $\to$ "Ánh xạ dữ liệu", "Nạp lại trường gói tin" $\to$ "Tự động ánh xạ".<br>8. Bổ sung icon badge trực quan cho trường dùng CodeSet và Format (sáng đèn + hover tooltip).<br>9. Bỏ tùy chọn thừa "Bỏ khóa này khi gửi đi" ở chiều gửi. | Trong tuần |
| **Đạt** | Backend Web API & CSDL | 1. Cung cấp API trả về đầy đủ Mã đối tác và Tên đối tác.<br>2. Nhóm sẵn các trường Meta hệ thống (`meta.now`, `request_id`...) để Frontend hiển thị danh mục chọn riêng.<br>3. Đảm bảo Backend tự động inject giá trị thời gian hệ thống vào các trường meta khi gửi/nhận.<br>4. Rà soát danh mục các gói tin chuẩn từ 101 đến 111 để seed đầy đủ vào CSDL. | Trong tuần |
| **Chị Như** | QC / Testing | 1. Nắm rõ luồng cấu hình nghiệp vụ chuẩn: Khai báo Đối tác $\to$ Đăng ký Gói tin (101-111) $\to$ Thiết lập Lịch gửi (Chu kỳ/Theo ngày) $\to$ Cấu hình Ánh xạ dữ liệu.<br>2. Cập nhật tài liệu kiểm thử và viết test case theo đúng các quy chuẩn đã thống nhất. | Tuần tới |

---

## 3. Kịch bản đối thoại chi tiết (Chronological Transcript)

> **Các nhân sự:**
> - **Anh Sơn:** Tech Lead / Kiến trúc hệ thống (người phản biện và chỉ đạo kỹ thuật)
> - **Kiên:** Dev Frontend (người trình chiếu màn hình và demo thao tác)
> - **Đạt:** Dev Backend (người phụ trách CSDL và Web API)

---

### [Phần 1: Rà soát tổng quan Bảng danh sách & Giao diện chung]

- **Anh Sơn:** Rồi, trong bảng kìa, tiếng Anh tiếng Việt kìa, đổi nó lại. `Active` kìa.
- **Kiên:** Dạ, cái này là một cái...
- **Anh Sơn:** Sửa nó lại cho nó... Rồi cái Cảnh báo kìa có chạy được không?
- **Kiên:** Không, Cảnh báo hiện tại là bỏ. Cảnh báo là khi nào mà thật á thì nó mới raise cảnh báo lên.
- **Anh Sơn:** Nhưng mà nó có test được không? Nó có dữ liệu thật không?
- **Kiên:** Trong code hiện tại các bạn đang comment hết rồi.
- **Anh Sơn:** Vậy thôi ẩn luôn đi!
- **Kiên:** Ẩn luôn ha anh?
- **Anh Sơn:** Ẩn luôn! Tại vì đang không biết là cái trường nó kiểu... nó sẽ trả về những gì khi mà bên đối tác người ta reject cái gói của mình á. Ẩn đi. Chưa có xử lý thì ẩn đi.
- **Kiên:** Dạ rồi, ẩn Cảnh báo.
- **Anh Sơn:** Mốt làm xong thì bật lên lại. Rồi tiếng Anh nữa kìa, `Disconnect`, `Connect` kìa.
- **Kiên:** Cái này do cái bảng cấu hình nãy... Để sửa lại.
- **Anh Sơn:** Đúng rồi, vậy đi. Rồi để cái mã nó vào, để tên rồi kiếm chỗ nào để cái mã nó vào.
- **Kiên:** Mã gì anh?
- **Anh Sơn:** Cái mã, cái mã định danh á!
- **Kiên:** Ý là mã của bên partner hả?
- **Anh Sơn:** Ừ!
- **Kiên:** Rồi ok anh. Để em để dưới, trước có mà em xóa đi á.
- **Anh Sơn:** Với lại xem nếu mà cái tên nó dài quá thì có cách nào để hover vào nó hiện đủ cái tên đó không? Dùng Tooltip hay dùng cái gì á?
- **Kiên:** Có có có, cái đó thoải mái anh.
- **Anh Sơn:** Tại vì chắc chắn là tên sẽ rất là dài. Ví dụ như: "Trung tâm quản lý điều hành giao thông gì gì đó bla bla bla...", chắc chắn là cái chữ trên bảng sẽ không đủ rồi.
- **Kiên:** Dùng cái component Tooltip mặc định của HTML có... cái title gì đó...
- **Anh Sơn:** Dùng Tooltip hả?
- **Kiên:** Dạ, Ant Design chắc chắn có component Tooltip mà anh. Tốt, cái validator hoạt động ổn.
- **Anh Sơn:** Có nè.
- **Kiên:** Quá đỉnh!
- **Anh Sơn:** Không mấy ai... nhưng mà sao màu nó không đúng giống như mấy thằng khác? Mấy thằng khác là màu đen mà?
- **Kiên:** Cái này màu gì anh?
- **Anh Sơn:** Mấy thằng khác màu đen mà? Lại hôm qua hổng để ý thằng khác rồi! Qua mấy giao diện khác coi thử coi.
- **Kiên:** Giao diện khác... để em coi...
- **Anh Sơn:** Hover thử coi... Bên giao diện lưới á. Nền trắng hay đen?
- **Kiên:** Không có chữ ra sao mà hover được anh.
- **Anh Sơn:** Hover vô mấy cái nút kìa!
- **Kiên:** À à, đúng rồi đúng rồi...
- **Anh Sơn:** Hình như là không xài...
- **Kiên:** Không xài cái thư viện có sẵn của UI hả anh?
- **Anh Sơn:** Không xài Tooltip của Ant Design á.
- **Kiên:** Dạ để em sửa lại, dùng component Tooltip chuẩn.
- **Anh Sơn:** Dùng component Tooltip! Đã nói là chắc người ta không nghe được hết... Rồi, còn cái gì ở đây nữa không? Ẩn cảnh báo nè, dùng component Tooltip nè... Rồi cái hành động, hành động thu nhỏ lại! Hành động để bự quá, thô quá!
- **Kiên:** Ok anh.
- **Anh Sơn:** Chỉnh lại, chỉnh kỹ kỹ lại. Rồi giao diện bị lỗi kìa! Đó, thấy chưa? Cái trang nãy đó... Bị gì? Cái đuôi này...
- **Kiên:** Không đủ chiều rộng...
- **Anh Sơn:** Scroll màn hình sao scroll qua? Chắc màn hình bị trơ à?
- **Kiên:** Không, ở đây nè, nó bị cái gì nè...
- **Anh Sơn:** Tính lại, tính lại cho nó ngon. Quan trọng nó đang bị cái gì á, sửa nó đi. Qua thử cái màn hình cấu hình, quản lý cấu hình coi. Coi nó có bị không?
- **Kiên:** Cấu hình hệ thống...
- **Anh Sơn:** Nó bị đâu? Nhiều khi của em sửa gì bây giờ kéo qua thử coi...
- **Kiên:** Đâu có bị đâu anh!
- **Anh Sơn:** Nó bị cái chỗ phân trang đó, nó bị đè lên đó! Cái này chỉnh lại nó bị cái gì rồi.
- **Kiên:** Đây nè, nó đang bị cái này đè lên nè.
- **Anh Sơn:** Không kéo nó rộng ra được hả?
- **Kiên:** Không kéo được anh ơi, fix cứng luôn rồi anh ơi. `Placeholder` là phải chọn trường.
- **Anh Sơn:** Đâu?
- **Kiên:** Đúng luôn! Chỗ trường đó phải chọn trường chứ!
- **Anh Sơn:** Thôi rảnh quá! Viết nhiều quá không làm được cái gì hết á! Cái tên label có sẵn rồi.

---

### [Phần 2: Chuẩn hóa Luồng Cấu hình Đối tác & Gói tin nghiệp vụ]

- **Kiên:** Ủa, "Nhập mã họ" là sao anh?
- **Anh Sơn:** Mã gì? Từ khóa nhập mã họ? Chắc xóa nhầm á. Ngó lại, chỉnh kỹ kỹ tí. Xem lại Frontend. Còn mấy cái tên ở trong á, viết chung chung thôi. Không cần phải viết nhập tên bảng, tên hay mã trường gì đâu.
- **Kiên:** Để em cắt nguyên màn hình đó luôn, em đánh dấu lại.
- **Anh Sơn:** Cứ viết chung chung cũng được. Ví dụ cái nào tổng quát quá thì viết dài dài được. Chứ không ghi chọn trường, mốt chọn trường mệt chữ nữa. Ủa giao diện này nhìn kỳ quá! Nó bị lệch sao á?
- **Kiên:** Thì đang làm nên nó bị lệch mà anh.
- **Anh Sơn:** Không, ý là em thấy cái cột số kìa, thấy cái đường nó bị lệch lên một tí không? Tự nhiên nó bị cái gì phía trước đó...
- **Kiên:** Cái thằng này là nó đang fix cái column này nè...
- **Anh Sơn:** Thì nó đang bị lệch á, không chịu nổi! Một là em đẩy cái cột số 1 ra luôn, đẩy nó ra thì lúc mà scroll kéo qua thì vẫn giữ lại cái số. Chứ đẩy qua không thấy số này là cái gì thì cũng như không. Nhìn kỳ quá! Rồi, tiếp: Cấu hình. Thì cái này thêm chức năng Thêm / Xóa vô nè.
- **Kiên:** Đang thêm vô đúng không anh?
- **Anh Sơn:** Rồi, Mapping... Trong màn hình em giải thích coi cái cấu hình làm cái gì? Giải thích lại lần nữa coi cấu hình làm cái gì?
- **Kiên:** Cấu hình này là sẽ khai báo đối tác. Xong rồi khai báo cái gói tin mà đối tác sẽ gửi. Khai báo đối tác nè, khai báo những cái đăng ký gửi...
- **Anh Sơn:** Đối tác là cái gì?
- **Kiên:** Đối tác là những cái partner mà mình sẽ gửi gói tin, sẽ chia sẻ dữ liệu cho mình hoặc mình chia sẻ dữ liệu cho người ta.
- **Anh Sơn:** Tạm ổn. Rồi, ở phần đăng ký gửi này là sao?
- **Kiên:** Ở phần đăng ký gửi này là mình sẽ đăng ký những cái... đăng ký mà mình muốn gửi đi cho họ hoặc mình nhận về, theo từng cái gói tin cho từng đối tác. Thì nó sẽ đăng ký theo... có kiểu lịch, ví dụ mình gửi đi á...
- **Anh Sơn:** Trời đất ơi! Từ từ, em giải thích tiếp: Trong loại gói tin nó sẽ có cái gì?
- **Kiên:** Trong loại gói tin nó sẽ có những cái tin... những cái dữ liệu của gói tin đó.
- **Anh Sơn:** Trời ơi! Sẽ có những cái danh sách gói tin theo quy định! Từ gói `101` đến gói bao nhiêu đó... Em giải thích để em giải thích cho Tester đó!
- **Kiên:** Dạ dạ rồi rồi.
- **Anh Sơn:** Lúc mà bàn giao việc là em phải giải thích vậy, chứ em không giải thích rồi Tester không hiểu rồi họ thêm `101`, `113`, `114` vô rồi thắc mắc ủa `113`, `114` lấy ở đâu ra nữa! Phải nói là nó fix từ `101` đến một lẻ mấy đó, `111`.
- **Kiên:** Dữ liệu gói tin này là những dữ liệu gói tin được quy định ở trong phần cấu hình gói tin, được seed DB sẵn.
- **Anh Sơn:** Rồi, thứ hai nữa là mỗi partner thì sẽ có cái gì? Nhận hoặc gửi đúng không? Đúng rồi, chiều đi và chiều về. Tương ứng là ở chiều đi thì mình sẽ có thông tin để mình thiết lập:
  1. Liên quan đến kiểu lịch để gửi: Có 2 mục là liên quan đến chu kỳ liên tục, hoặc là theo từng ngày ở mốc thời gian cụ thể. Gửi liên tục thì mình có thể thiết lập theo chu kỳ theo giây và giới hạn trong khung giờ cụ thể. Giải thích chứ không phải là đi ngồi nói từng cái chỗ ba ơi!
  2. Còn cái Ưu tiên làm cái gì?
- **Kiên:** Cái ưu tiên này sẽ là cái để hệ thống biết là gói tin nào được ưu tiên gửi trước nếu như nó đang trong một cái hàng đợi...
- **Anh Sơn:** Nhưng mà có làm chưa?
- **Kiên:** Không có làm!
- **Anh Sơn:** Không có làm thì nói nó là tạm thời cái này để lại thôi! Sau này dùng cái gì đó, dùng thiết lập sự ưu tiên gói tin hiện tại không có. Nói trước để Tester biết, chứ không set ưu tiên cho đã rồi không biết nó làm cái gì! Còn hàng ngày thì nó sẽ cho phép mình chọn: Chọn theo ngày nào sẽ được gửi, giải thích rõ ràng!
- **Kiên:** Dạ, chọn theo ngày là chọn ngày nào được gửi, màu xanh là sẽ chọn ngày gửi, giờ gửi là sẽ cấu hình là vào ngày đó gửi lúc mấy giờ.
- **Anh Sơn:** Rồi, tiếp! Còn cái cuối, một câu chốt lại: Mỗi một đối tác có thể cấu hình nhiều gói tin khác nhau, đúng không? Cả chiều gửi và chiều nhận. Nhớ chỉnh mấy cái đó lại nha, tranh thủ trong tuần. Để gửi Tester chứ không cái task nó cứ treo hoài, task test bữa giờ cứ treo hoài sáu mấy phần trăm á!

---

### [Phần 3: Cơ chế Ánh xạ Dữ liệu (Mapping) & Thiết kế Giao diện]

- **Kiên:** Tiếp tục là phần Mapping...
- **Anh Sơn:** Đi theo đúng cấu trúc, đừng có đi nhảy lung tung rồi tí nữa rối!
- **Kiên:** Dạ, Mapping nè. Đầu tiên là vô Cấu hình trước đúng không? Xong cấu hình gói tin. Thì sau khi có 2 thông tin này rồi, bước tiếp theo là mình sẽ tiến hành đi cấu hình mapping dữ liệu.
- **Anh Sơn:** Nhưng mà sửa cái tên Mapping lại! Gọi là cái gì đó... Cấu hình dữ liệu, phễu lọc... Làm sao để người ta hình dung được cái nó là cái dữ liệu sẽ trả về á, trả ra hoặc nhận về á. Hoặc Ánh xạ dữ liệu gửi/nhận hay là cái gì đó... Tiếp nè!
- **Kiên:** Tiếp, thì đầu tiên vô phần Ánh xạ dữ liệu, tổng quan phần này nó sẽ là một cái phần ánh xạ để nó cấu hình cái dữ liệu bên mình qua dữ liệu đối tác, thay đổi những cái trường dữ liệu ở trong một gói tin nhất định.
- **Anh Sơn:** Là sao? Không hiểu! Giải thích lại từ từ coi!
- **Kiên:** Phần ánh xạ này nó là một phần để mình cấu hình dữ liệu gửi đi hoặc nhận về theo... theo cái dữ liệu mà đối tác mong muốn.
- **Anh Sơn:** Dùng từ biến đổi nè, chuyển đổi nè... Chuyển đổi theo cấu trúc mà đối tác mong muốn. Cấu trúc thống nhất giữa hai bên, giữa mình và đối tác, với lại cái dữ liệu ở trong CSDL mình lưu. Thì nó sẽ có nhiệm vụ là ở hai chiều:
  - Chiều nhận là mình sẽ biến đổi từ cái dữ liệu đối tác gửi về cho mình, chuyển về CSDL.
  - Còn chiều đi là biến đổi từ CSDL của mình sang dữ liệu đối tác... Không phải sang dữ liệu đối tác, mà là sang cấu trúc dữ liệu thống nhất giữa mình với đối tác! Phải giải thích vậy Tester mới hiểu ba ơi!
- **Kiên:** Dạ rồi, để xíu em viết tóm tắt lại.
- **Anh Sơn:** Tóm lại là sao? Sure kèo luôn, Như nó sẽ hỏi tóm lại là sao?
- **Kiên:** Hôm qua em có gửi trước cho chị Như một bản rồi á anh.
- **Anh Sơn:** Mình phải giải thích, phải hình dung... Mỗi chức năng phải hình dung cái luồng nó chạy như thế nào. Hai cái đầu tiên là cấu hình đối tác, trong đối tác cấu hình từng gói tin gửi/nhận. Rồi bắt đầu đến cái bước ánh xạ này là từng gói tin gửi/nhận đó sẽ được chuyển đổi theo cấu trúc như thế nào theo từng đối tác. Rồi bắt đầu vô đây mới giải thích kỹ là: Mình sẽ thiết lập cấu hình theo từng gói tin và theo đối tác cụ thể, cộng thêm hướng (chiều gửi / chiều nhận). Thì mặc định nếu mà không có gì xảy ra thì thường cái cấu trúc đó là sẽ đồng bộ. Hai cái kia em có thử chưa, thử ok không?
- **Kiên:** Chiều gửi với chiều nhận hôm qua em thử ok anh. Gửi đi nhận về ok, hai chiều cũng ok. Nó map với thằng CodeSet cũng ok luôn. Nhận về nó sẽ revert lại cái cấu trúc.
- **Anh Sơn:** Rồi!
- **Kiên:** Đầu tiên là một cái tab thông tin chung là để mình khai báo hồ sơ ánh xạ... Ở đây sẽ có danh sách chọn gói tin.
- **Anh Sơn:** Thiếu cái mã kìa! Thiếu cái mã gói tin kìa. Tại vì trong bên này nó quy định theo mã, `101`, `102` đó.
- **Kiên:** Tại vì nó đang dùng hai cái bảng khác nhau á, thì sau sẽ chuyển đổi qua hết một bảng. Trường mã hồ sơ ánh xạ ở đây nó sẽ tự động sinh theo...
- **Anh Sơn:** Cho mình chọn, theo mã kết hợp với mã. Cái partner kia đặt cái mã test đúng không? Nó làm đảo lộn hết trơn...
- **Kiên:** Cái mã này sẽ tự sinh theo mã đối tác, mã gói tin và mã chiều (In/Out).
- **Anh Sơn:** Rồi, tại sao chỗ đó bị trống?
- **Kiên:** Tại vì nó đang thêm mới anh. Giờ em qua bên này em sửa có dữ liệu sẵn nè.
- **Anh Sơn:** Khung đầu ra là cái gì?
- **Kiên:** Bên phía bên phải là bảng ánh xạ theo cấu trúc của đối tác.
- **Anh Sơn:** Ba chấm (...) đó là cái gì?
- **Kiên:** Ba chấm là thêm mấy cái... Đối với những cái mà hệ thống nhận diện nó là Header thì sẽ chỉ có 2 option đó là: Bỏ khóa này khi gửi đi và Giá trị mặc định khi gửi đi.
- **Anh Sơn:** Điên vậy? Bỏ cái chữ... để hình khác. Để ba chấm nhìn khó nhìn quá, nhìn không hiểu mẹ gì hết! Kiếm nó thành cái hình Config hay là hình Edit hay gì đó... Nhưng mà ví dụ như tình huống mà có chọn cái gì đó, thì thêm một cái biểu tượng vào để người ta biết là trường đó nó đang có sử dụng bảng mã hay gì á! Chứ giờ anh nói ví dụ giờ nó đến 20 trường đi, rồi em nhìn vô em sao em biết được thằng nào đang dùng bảng mã, cái nào đang chuyển đổi?
- **Kiên:** Thì em đang tính thêm nó vô đây nè.
- **Anh Sơn:** Đó, mình tận dụng cái khoảng trống ở giữa đó! Có thể là đẩy cái thằng map qua đây, nguyên khoảng trống phía sau mình sẽ hiển thị cho thằng đó. Ví dụ như cái thằng này nó có biến đổi về format đi, thì mình sẽ sáng cái icon format lên!
- **Kiên:** Giống như các anh làm á...
- **Anh Sơn:** Đúng rồi, sáng icon format lên. Ví dụ như người ta muốn biết format cái gì, người ta rê chuột vào cái icon format đó, nó sẽ hiện lên là sẽ format qua định dạng gì đó. Rồi ví dụ như nó có sử dụng CodeSet bảng mã đi, thì cái icon liên quan đến bộ mã nó sẽ sáng lên. Mình rê vào nó sẽ ghi là đang sử dụng bộ mã gì đó. Là hình dung được là thằng nào đang sử dụng icon nào, thằng nào đang sử dụng bộ mã. Để tìm cho nó nhanh!
- **Kiên:** Dạ hiểu.
- **Anh Sơn:** Rồi mấy cái dữ liệu... Ví dụ nó dài quá thì có cách nào cho nó hiện đủ không? Một là có Info gì đó... Rồi mấy cái trường nhớ chia thêm một số trường của bên mình á, của hệ thống á, để người ta chọn. Ví dụ như giờ muốn cái trường đó là thời gian hiện tại thì truyền làm sao? `DateTime.Now` cái gì á?
- **Kiên:** Giống như bên kia demo là có nguyên một cái cụm Meta đó...
- **Anh Sơn:** Đúng rồi, làm cho nó từng cụm như vậy để quản lý. Ví dụ như cái liên quan đến dữ liệu của bên kia đi, thì nguyên cái cụm này nó sẽ thuộc về cái dữ liệu gì đó.
- **Kiên:** Không, nếu mà vậy á thì từ từ nha anh. Tại vì hiện tại là anh Đạt ảnh sẽ gửi nguyên cái cụm này nó là một cái raw content luôn. Tụi em sẽ nhận về như thế này. Còn những cái thằng ở ngoài á, nó sẽ là cái Meta, cần thì em hiển thị những cái trường này lên để cho người ta cấu hình trên này.
- **Anh Sơn:** Bậy rồi! Bậy lộ ý con ba ơi! Ba nói ý khác mà ngộ ba hiểu ý khác ba ơi! Đang nói cái mapping này mà... Đâu em mở cái trang hôm bữa anh demo lên coi.
- **Kiên:** Em biết cái Meta của anh rồi.
- **Anh Sơn:** Biết thì làm đúng đi chứ! Kéo xuống... Đây, ở trên... Ví dụ như đây em chia làm 2 bảng nè: Thì bây giờ bên này nó ra đây, đúng không? Trình bày sao không biết, nhưng mà hình dung được là ví dụ như ở trong thằng này khi chọn đi... Ví dụ 4 cái trường, 6 cái trường này nó thuộc về `TimeZoneStatus`, thì phải biết được 6 cái trường này của nó. Rồi 4 cái trường này của cái `TimeZone` gì đó, nếu mình chia theo nhóm. Rồi sau đó sẽ có một số cụm là của hệ thống, coi như là thằng nào cũng sẽ có. Dù cái dữ liệu ngoài kia không có nhưng mà thằng nào trong này cũng sẽ có. Nó chính là cái thằng liên quan đến mapping, là thằng Meta đó! Là hệ thống mình sẽ xử lý, là những cái này mình trong code mình sẽ xử lý cứng luôn! Ví dụ như nó truyền vào `now` đi, thì mình sẽ mặc định là trong code mình sẽ lấy `DateTime.Now` mình truyền vào.
- **Kiên:** Dạ, vậy thôi.
- **Anh Sơn:** Đúng không? Anh sẽ muốn em làm như thế này đúng không? Gom hộp sao cho nó dễ xử lý á. Chứ cái này hiện tại đang nhiều quá, mốt mình nó nhiều lên là tìm không biết thằng nào đang dùng dữ liệu nào luôn á!
- **Kiên:** Có thể là drop-down, hoặc có thể là bấm vào nó sẽ hiện nguyên một cái Modal để mình chọn.
- **Anh Sơn:** Thì có thể là bấm vào nó sẽ hiện luôn một cái cửa sổ Modal gì đó để mình chọn.
- **Kiên:** Rồi...
- **Anh Sơn:** Rồi sao `RequestID` lại bị chặn? Kéo lên... Làm sao? Tại sao nó chặn?
- **Kiên:** Khóa này cấu hình nó đang là không gửi đi nên là nó không cho chọn.
- **Anh Sơn:** Không hiểu! Tự dưng đã nhập cái nội dung theo mẫu mong muốn được gửi đi xong rồi lại vô bỏ chọn nó đi? Vậy thôi lúc đầu không nhập cho rồi!
- **Kiên:** Hiểu hiểu, ok anh.
- **Anh Sơn:** Tự dưng đưa qua thành một cái nghĩa khác á!
- **Kiên:** Dạ dạ, ok ok... Vậy thì em bỏ cái option đó đi.
- **Anh Sơn:** Rồi, tiếp phần data...
- **Kiên:** Ở phần data này nó sẽ có một cái chức năng nữa là "Nạp lại trường gói tin". Là nó sẽ tự động mapping những cái field của gói tin hệ thống đã được khai báo với lại cái cấu trúc JSON mà mình đã tạo lên đây.
- **Anh Sơn:** Tự nạp lại là cái gì? Đổi tên nó lại! Tự nhiên nạp lại trường dữ liệu nghe nó kỳ quá.
- **Kiên:** Nếu mà JSON không có là nó sẽ không tự động map. Bấm một cái là phân tích...
- **Anh Sơn:** Bà đổi, bà đổi hết mấy thằng kia lại xong rồi bà bấm... Á đù! Bà đổi bên kia kìa, bà đổi bên này nè, xong rồi bà bấm lại thử coi coi nó có map lại không? Mà sao tự dưng nó bị thụt thò vô vậy ba? Thôi đừng có thụt ra thụt vô! Bấm cái nút bên kia mà...
- **Kiên:** Nút của em là em mới nói á.
- **Anh Sơn:** Nếu vậy chữ đó không phải là tự nạp lại trường gói tin, mà là "Mapping tự động", "Ánh xạ tự động" hay "Thiết lập tự động" hay cái gì đó!
- **Kiên:** Ok anh. Tại vì khi mà ấn phân tích này là nó đã tự động ánh xạ so với cái gói tin rồi, so với gói tin đã chọn bên này nè.
- **Anh Sơn:** Rồi sao không ghi là: "Đã khớp 6/12 trường"?
- **Kiên:** Dạ...
- **Anh Sơn:** Chứ ghi "Đã khớp" nghe kỳ quá!
- **Kiên:** Đảo sao nãy giờ nó ghi sai data... Đau bụng quá! Thôi đi vệ sinh nha.
- **Anh Sơn:** Thôi ta đi vệ sinh đây.

==============================================

1. Port nhớ rào lại khi setting chỉnh sửa tên đối tác.
2. Hiện tại setting chưa có smtp, sse
3. Cái thứ 2 chưa có liên quan đến điều chỉnh gói tiên hiện tại đang hard code. thêm xóa sửa
4. Cấu hình đăng ký chia sẽ dữ liệu sửa lại checkbox sẽ đẩy khi có dữ liệu mới thay thế radion Theo sự kiện. dựa vô field updatetime, createtime gì đó để biết có dữ liệu mới.
5. Setting "radio chia sẽ dữ liệu" bỏ định dạng ẩn đi.
6. Setting luồng nhận  bỏ những field không liên quan như kiểu lịch, hằng ngày các kiểu.
7. Giao diện lỗi ở phần cấu hình gói tin  ở bảng gói tin.
8. Mapping để cấu hình gửi đi nhận về, chuyển đổi theo cấu trúc thống nhất giữa mình và đối tác.
9. Thiếu mã gói tin ở phần tạo mới hồ sơ ánh xạ.
10. Cái mã ở phần hồ sơ ánh xạ, tên mã tự sinh.
11. phần cấu hình chỉnh sửa hồ sơ ánh xạ bỏ ... đổi qua icon config.
12. Sửa lại UI hồ sơ ánh xạ build 1 một model chồng lên ở hồ sơ ánh sạ khi chỉnh sửa tên field.
13. Khi cấu hình gói tin đang chạy thì sẽ không được chỉnh sữa
14. Ở tab lịch sử chia sẽ sẽ sửa lại "cấu hình" => kiểu ở phần header.
15. Phân nội hành động chỉ cần nội dung bật tắt thôi.
16. Chuyển qua model không dung sidebar nữa ở phần tab lịch sử chia sẽ khi bấm double các row trong table
17. Thời gian tab lịch sửa chỉnh đúng format datetime đổi sang component rambo gì đó.
18. Bỏ search nội dung ở phần tab lịch sử chia sẽ
19. Xử lý luồng nhận ghi log lại là xử lý xong ghi nhận + cầm cục là gửi đi sẽ ghi 1 log nữa (sẽ có quan hệ cha con) nghĩa là 2 step này sẽ nằm trong 1 log.
