---
tier: A
read: full
source:
  - DocBusinessThienAn/Sharedata_26_10.m4a (29:59)
date: 26-10-2026
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData — Tái cấu trúc Form Cấu hình (Wireframe mới), Tách Tab Chiều Gửi vs Chiều Nhận, Khóa Disable Chỉnh sửa theo Quyền, Cơ chế So sánh Đối chiếu (Diff/Compare) Gói tin Mẫu, Gộp Câu lệnh Ghi Dữ liệu (Write SQL) & Tự động Nhận diện Bảng Bị Ảnh Hưởng (Affected Tables)
participants:
  - Tech Lead
  - Dev
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ ShareData — Tái cấu trúc Form Cấu hình Gửi/Nhận, Giao diện So sánh Đối chiếu Gói Mẫu & Quản lý Câu lệnh Ghi Dữ liệu
### (Nguồn từ tệp ghi âm: Sharedata_26_10.m4a)

- **Ngày ghi nhận:** 26/10/2026
- **Thời lượng:** 29 phút 59 giây (toàn văn tệp ghi âm)
- **Người tham gia:** Tech Lead, Dev
- **Chủ đề chính:**
  1. **Kiểm soát Thao tác trên UI — Phân quyền & Khóa Nút Chỉnh Sửa (Disable-by-Default):** Toàn bộ giao diện cấu hình gói tin đối tác mặc định ở chế độ khóa (`disabled`), không cho phép chỉnh sửa trực tiếp trên bảng. Chỉ khi người dùng có quyền và bấm nút "Chỉnh sửa" thì form mới mở khóa để thao tác, tránh click nhầm hoặc làm sai lệch cấu hình đang chạy.
  2. **Tái Cấu Trúc Form Giao Diện Theo Wireframe Mới (Hạn chế Scroll & Tách Tab Chiều Gửi / Chiều Nhận):** Khắc phục tình trạng form cấu hình quá dài, phải scroll ngang/dọc phức tạp. Chuyển sang bố cục gọn gàng: hiển thị tóm tắt trên bảng; khi cần sửa thì mở Modal/Dialog popup. Tách bạch hoàn toàn giữa luồng **Chiều Gửi (Outbound)** và **Chiều Nhận (Inbound)** thành 2 Tab hoặc 2 Group độc lập do nghiệp vụ hai chiều khác biệt.
  3. **Giao Diện So Sánh Đối Chiếu với Gói Tin Mẫu (Diff/Compare) & Ký Hiệu Trực Quan:** Thiết kế màn hình so sánh câu truy vấn SQL và danh sách trường giữa mẫu chuẩn và gói tin riêng của đối tác. Phân loại rõ ràng: trường có trong mẫu, trường phát sinh thêm, trường khác kiểu dữ liệu (`decimal` vs `bigint`), trường mẫu có mà đối tác không có; thể hiện bằng các ký hiệu/icon ngắn gọn trực quan kèm nút "So sánh tất cả".
  4. **Quản Lý Câu Lệnh Ghi Dữ Liệu (Write SQL) & Tự Động Nhận Diện Bảng Bị Ảnh Hưởng (Affected Tables):** Thống nhất phương án cho phép gộp các câu lệnh ghi (`INSERT`, `UPDATE`) thành một khối lệnh dùng chung trong transaction. Hệ thống tự động phân tích cú pháp để nhận diện danh sách các bảng bị ảnh hưởng (`Affected Tables`), đồng thời cho phép Admin xem xét và hiệu chỉnh danh sách bảng để đảm bảo tính an toàn toàn vẹn dữ liệu.
  5. **Xử Lý Khóa Nối Đuôi (@LastKey / Composite Key) & Chống Trùng Lặp Chiều Nhận:** Chuẩn hóa cơ chế xử lý khóa nối đuôi đối với các bản ghi trùng thời điểm bằng Composite Key nối chuỗi. Đối với chiều nhận dữ liệu, duy trì cơ chế kiểm tra trùng lặp (`Check Duplicate`) theo Unique ID trước khi thực hiện ghi nhận vào CSDL.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Khóa Chỉnh Sửa Mặc Định (Disable-by-Default) & Phân Quyền Thao Tác
- **Hiện trạng:** Các trường nhập liệu, checkbox và cấu hình trên giao diện gói tin đối tác trước đây mở sẵn ở chế độ cho phép tương tác. Khi người vận hành cuộn chuột hoặc click xem thông tin dễ vô tình kích hoạt thay đổi cấu hình đang hoạt động.
- **Quyết định kỹ thuật:**
  - Mặc định khi mở màn hình, toàn bộ các input, selector và checkbox đều ở trạng thái vô hiệu hóa (`disabled` / read-only).
  - Bổ sung nút bấm **"Chỉnh sửa"** tích hợp kiểm tra phân quyền người dùng (Action Authorization). Chỉ khi bấm nút này thì form mới chuyển sang chế độ chỉnh sửa (Edit Mode).
  - Nếu người dùng không có quyền chỉnh sửa, nút này sẽ bị ẩn hoặc disable hoàn toàn.

### 1.2. Tái Cấu Trúc Form Theo Wireframe Mới — Tách Biệt Tab Chiều Gửi vs Chiều Nhận
- **Giải quyết bài toán cuộn trang (Scroll Overhead):**
  - Giao diện cũ hiển thị đồng thời cả cây cấu hình, danh sách trường và các khối xử lý khiến trang bị kéo dài, phát sinh thanh scroll ngang và dọc làm rối mắt điều hành viên.
  - Thiết kế lại bố cục: Trên trang chính chỉ hiển thị bảng danh sách thông tin tóm tắt. Khi người dùng bấm chỉnh sửa cấu hình một đối tác hoặc gói tin thì hệ thống sẽ bật **Modal/Dialog** độc lập với không gian thao tác tập trung.
- **Tách riêng 2 luồng Gửi và Nhận dữ liệu:**
  - Chiều Gửi (Outbound): Trích xuất dữ liệu từ CSDL hệ thống, đóng gói JSON/file và gửi cho đối tác (liên quan đến câu lệnh truy vấn động Dynamic Query, chu kỳ lập lịch, bộ lọc nối đuôi).
  - Chiều Nhận (Inbound): Nhận dữ liệu từ đối tác đẩy về, phân tích cú pháp và ghi vào các bảng nội bộ (liên quan đến câu lệnh Write SQL, mapping tham số, danh sách bảng ảnh hưởng).
  - Hai luồng này độc lập về bản chất, do đó giao diện chia thành **2 Tab riêng biệt: Tab Gửi và Tab Nhận** (hoặc 2 Group phân định rõ ràng), không gộp chung vào một form gây nhầm lẫn trường dữ liệu.

### 1.3. Giao Diện So Sánh Đối Chiếu Gói Tin Mẫu (Diff/Compare Tool)
- **Cơ chế so sánh trực quan:**
  - Cho phép người dùng so sánh chi tiết giữa cấu hình gói tin chuẩn (Template Packet) và cấu hình tùy biến riêng của từng đối tác (Custom Packet).
  - So sánh theo từng trường dữ liệu (Field-by-Field Diff):
    - `Trường giống nhau`: Có ở cả hai bên, cùng kiểu dữ liệu.
    - `Trường phát sinh thêm`: Trường chỉ có ở cấu hình riêng của đối tác.
    - `Trường khác biệt kiểu`: Cùng tên trường nhưng khác kiểu dữ liệu (ví dụ: mẫu dùng `decimal`, đối tác cấu hình `bigint`).
    - `Trường thiếu`: Trường có trong mẫu chuẩn nhưng bị loại bỏ ở cấu hình đối tác.
  - Dùng hệ thống icon/ký hiệu trực quan (+, -, icon cảnh báo) để người dùng nắm bắt nhanh hiện trạng khác biệt thay vì hiển thị các dòng mô tả dài dòng.
  - Bổ sung chức năng **"So sánh tất cả" (Compare All Tabs)** hỗ trợ rà soát nhanh toàn bộ các đối tác đang kế thừa mẫu này.

### 1.4. Quản Lý Câu Lệnh Ghi Dữ Liệu (Write SQL) & Tự Động Nhận Diện Bảng Bị Ảnh Hưởng
- **Phương án gộp câu lệnh ghi:**
  - Đối với các gói tin chiều nhận cần ghi vào nhiều bảng (ví dụ: 1 bảng chính và 2 bảng chi tiết liên kết): Cho phép viết gộp các câu lệnh `INSERT`/`UPDATE` thành một khối lệnh SQL hoàn chỉnh thực thi trong transaction, thay vì phải cấu hình tách rời từng câu nhỏ gây phân mảnh.
- **Nhận diện Bảng bị ảnh hưởng (Affected Tables):**
  - Hệ thống tích hợp bộ phân tích cú pháp để tự động nhận diện danh sách các bảng bị tác động từ câu lệnh Write SQL.
  - Để đảm bảo an toàn tuyệt đối, hệ thống cho phép Admin xem lại và trực tiếp xác nhận hoặc hiệu chỉnh danh sách bảng bị ảnh hưởng trên UI trước khi lưu cấu hình, phục vụ việc kiểm soát quyền truy cập và kiểm chứng tính toàn vẹn dữ liệu.

### 1.5. Khóa Nối Đuôi (@LastKey) & Chống Trùng Lặp Chiều Nhận
- **Xử lý mốc nối đuôi trùng thời gian:**
  - Khi dữ liệu phát sinh nhiều bản ghi tại cùng một thời điểm, sử dụng cơ chế Composite Key kết hợp giữa trường thời gian và khóa định danh phụ (ví dụ: `Time + '_' + ID`) để làm mốc `@LastKey`, ngăn ngừa nguy cơ bỏ sót hoặc lặp bản ghi khi worker quét định kỳ.
- **Chiều nhận dữ liệu:**
  - Quy trình chiều nhận duy trì cơ chế kiểm tra trùng lặp (`Check Duplicate`) dựa trên Unique ID/Message ID nhận được từ đối tác, nếu bản ghi đã tồn tại thì bỏ qua hoặc cập nhật theo kịch bản quy định.

---

## 2. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

### [00:00 – 05:00] Rà soát Form Cấu hình, Khóa Chỉnh sửa (Disable-by-Default) & Ý tưởng Tách Tab Gửi/Nhận

- **[00:01] Tech Lead:** Hai cái bảng giao diện này gom lại được không?
- **[00:03] Dev:** Đúng đúng rồi đó anh, tụi em đang review lại nè.
- **[00:08] Tech Lead:** Mấy cái trường dữ liệu này khi nạp lên nó sẽ tự động sinh ra và detect ra.
- **[00:13] Dev:** Thì mấy cái này sẽ có bắt buộc hoặc bỏ qua...
- **[00:17] Tech Lead:** Nhớ là có cái nút Chỉnh sửa nha!
- **[00:21] Dev:** Nút Chỉnh sửa...
- **[00:23] Tech Lead:** Bình thường nó sẽ chặn lại, không cho sửa trực tiếp.
- **[00:24] Dev:** Rồi.
- **[00:25] Dev:** Bên này đâu có nút chỉnh sửa đâu?
- **[00:26] Tech Lead:** Dưới đây kìa!
- **[00:30] Dev:** Thấy cái này anh kêu là khi nào bấm vô Chỉnh sửa thì mấy cái này mới được thao tác đúng không?
- **[00:34] Tech Lead:** Ừ, phải có phân quyền nữa chứ.
- **[00:36] Tech Lead:** Chưa bấm nút chỉnh sửa thì không được thao tác.
- **[00:38] Dev:** Bấm nút chỉnh sửa nó mới mở chức năng đó ra để chỉnh sửa.
- **[00:41] Tech Lead:** Chứ bây giờ để hớ hênh không rào lại, người ta quẹt chuột một cái là tích chọn lại được liền à.
- **[00:46] Dev:** Như vậy lúc đầu mới mở lên nếu không bấm sửa thì nó sẽ disable hết đúng không?
- **[00:49] Tech Lead:** Disable hết!
- **[00:54] Dev:** Chỗ này phải có phân quyền.
- **[00:58] Dev:** Sao lại không phân quyền? Phân quyền là không cho người dùng tự ý chỉnh sửa, còn cái này ai có quyền mới được bấm nút sửa chứ.
- **[01:03] Tech Lead:** Giờ nếu không phân cái nút chỉnh sửa thì sao nó hiện nút chỉnh sửa cho người ta bấm? Phải kiểm tra quyền trước khi hiển thị nút chứ.
- **[01:08] Dev:** Nếu phân quyền như vậy thì xử lý trong code được rồi, cái đó bỏ qua giao diện cũng được.
- **[01:27] Dev:** Mấy cái nhóm ghi chú này có cần không anh?
- **[01:30] Tech Lead:** Nhóm là để gom lại mấy cái trường dữ liệu cho nó gọn.
- **[01:34] Tech Lead:** Nhưng mà cái này nhìn nó hơi bị rối nha! Cuộn qua cuộn lại kéo ngang kéo dọc hơi rối đó.
- **[01:38] Dev:** Sẽ cân nhắc bỏ mấy cái trường không cần thiết đi, hiện tại là đang cho hiển thị chức năng trước thôi.
- **[01:44] Tech Lead:** Nó hơi bị rối đó, xem lại đi chứ để vậy người dùng nhìn vô hoa mắt.
- **[01:56] Dev:** Nói chung là hạn chế cuộn (scroll). Hay là mình thiết kế lại luôn, cái này để ở trên còn cái kia đưa xuống dưới?
- **[02:05] Dev:** Kiểu đi từ trên xuống dưới, nó giống như cái wireframe anh gửi đúng không?
- **[02:11] Tech Lead:** Ừ, cấu hình bên đây rồi nó chọn thì sẽ đi từ trên xuống dưới luôn. Cái wireframe mới anh mới đưa hồi sáng đó.
- **[02:19] Dev:** Có, sáng anh có đưa rồi.
- **[02:25] Tech Lead:** Chia làm ba phần rõ ràng ra.
- **[02:37] Dev:** Ok anh, cái này để tụi em coi lại rồi sửa cho nó gọn lại.
- **[02:44] Tech Lead:** Còn không á thì cái này chỉ để hiển thị thông tin danh sách thôi, dạng bảng bình thường nè. Khi nào bấm Chỉnh sửa thì mở Modal popup ra cho người ta sửa.
- **[02:56] Dev:** Mở Modal popup cũng được.
- **[03:05] Tech Lead:** Làm vậy nó gọn gàng hơn nhiều, chứ để cả đống trường trải dài trên trang thì người ta không biết ở phía dưới còn nội dung nữa.
- **[03:16] Tech Lead:** Thêm một cái nữa: Cái quy trình nhận này tách hẳn ra thành luồng mới, tại vì thằng gửi với thằng nhận nó đâu có liên quan gì tới nhau đâu!
- **[03:22] Dev:** Rồi.
- **[03:28] Tech Lead:** Thằng này với thằng kia không liên quan, gửi với nhận độc lập hoàn toàn.
- **[03:39] Tech Lead:** Bây giờ em để chung một form, người ta nhìn vô không biết cái trường này là của bên gửi hay của bên nhận.
- **[03:54] Dev:** Chia dạng Tab hả anh? Làm cái Tab ở đây, bên này là Gửi, bên này là Nhận.
- **[04:03] Tech Lead:** Một là chia dạng Tab, hai là chia Group rõ ràng ra. Chứ hiện tại để chung hai thằng này với nhau không có ranh giới gì hết, nhìn rất là khó chịu.

---

### [05:00 – 10:00] Tối Ưu Trải Nghiệm Nhập Liệu, Mở Rộng Modal & Giao Diện So Sánh Đối Chiếu (Diff)

- **[05:06] Tech Lead:** Để chung nhìn nó rất là khó. Người ta nhìn vô không biết là cái này đang cấu hình cho chiều gửi hay chiều nhận, tự dưng đang thấy một đống trường rồi cuộn xuống lại thấy thêm một đống trường khác nữa. Rất là rối!
- **[05:26] Tech Lead:** Với lại chỗ này hôm bữa anh nói là cho nó nhỏ gọn lại thôi, khi nào người ta bấm vào thì nó mới mở rộng ra như cái wireframe á.
- **[05:35] Dev:** Cho nó nhỏ nhỏ vừa phải thôi đúng không anh?
- **[05:38] Tech Lead:** Ừ, khi bấm vào thì ô đó mở rộng lên giống như wireframe ấy.
- **[05:45] Dev:** Wireframe mới của anh đúng không?
- **[05:47] Tech Lead:** Đúng rồi, cái mới đó.
- **[06:15] Tech Lead:** Ví dụ chia làm ba mục nè: Tên đối tác, danh sách gói tin, rồi bên dưới là các trường dữ liệu tương ứng. Khi tạo mới thì cho phép sao chép từ cấu hình mẫu chung hoặc sao chép từ một đối tác đã có sẵn.
- **[06:42] Tech Lead:** Cái khung nhập câu truy vấn cho nó vừa phải thôi, không cần quá bự. Khi nào cần chỉnh sửa câu SQL lớn thì bấm nút mở rộng để bung toàn màn hình ra cho dễ viết.
- **[07:05] Dev:** Chỗ nút Lưu và Cập nhật nè...
- **[07:15] Tech Lead:** Chỗ này có nút Chỉnh sửa ở đây nè, bấm vô mới sửa. Đừng có mở bung hết ra, người ta nhìn không biết đang thao tác ở dòng nào vì có quá nhiều cột.
- **[07:42] Dev:** Mấy cái trạng thái có cần giữ lại không anh?
- **[07:50] Tech Lead:** Cơ bản là đã khai báo lên đây thì chắc chắn là phải có dùng rồi, cái cờ trạng thái bật tắt đó không cần thiết thì bỏ bớt đi cho gọn bảng. Chỉ cần cột Bắt buộc là được rồi.
- **[08:05] Tech Lead:** Còn bên giao diện so sánh đối chiếu: Ví dụ khi so với mẫu thì có những khóa dữ liệu này nè.
- **[08:12] Dev:** Nó khớp với mẫu hay khác với mẫu thì hiển thị ra.
- **[08:18] Tech Lead:** Đúng rồi, so sánh với mẫu: Cột này bên mẫu có, cột này bên đối tác phát sinh thêm, cột này bên mẫu có mà đối tác không có. Rồi cột này bị khác kiểu dữ liệu, ví dụ bên mẫu khai báo là `decimal` mà bên đối tác lại cấu hình là `bigint`.
- **[08:42] Tech Lead:** Đánh dấu khác biệt rõ ràng ra. Còn cái nào bên mẫu không có thì ghi nhận là trường riêng.
- **[08:48] Dev:** Dùng mấy cái ký hiệu icon cho nó ngắn gọn đúng không anh?
- **[08:52] Tech Lead:** Đúng rồi, dùng ký hiệu icon trực quan cho nó gọn cái UI của mình, chứ ghi chữ giải thích dài dòng là trường này không có, trường kia bị dư nhìn rất mất thời gian.
- **[09:05] Tech Lead:** Mục tiêu là so sánh được hai câu truy vấn và cấu trúc các trường dữ liệu giữa hai bên.
- **[09:20] Dev:** Ở ngoài trang danh sách mẫu thì có nút "So sánh tất cả" đúng không anh?
- **[09:25] Tech Lead:** Đúng, ở màn hình mẫu có nút So sánh tất cả để quản trị viên bấm một cái là kiểm tra được toàn bộ các đối tác đang dùng mẫu đó xem có đối tác nào bị lệch cấu hình không để cập nhật đồng bộ cho nhanh.

---

### [10:00 – 15:00] Xử Lý Câu Lệnh Ghi Dữ Liệu (Write SQL) & Tự Động Nhận Diện Bảng Bị Ảnh Hưởng (Affected Tables)

- **[10:14] Tech Lead:** Nhìn chung làm lại thì phải có hai điểm cốt lõi: Thứ nhất là cơ chế so sánh các trường với gói mẫu, thứ hai là nút tạo nhanh đối tác.
- **[10:33] Tech Lead:** Còn phần gói gửi với gói nhận: Chia thành hai nhóm hoặc hai Tab riêng biệt cho rõ ràng.
- **[10:47] Dev:** Còn đối với chiều nhận dữ liệu từ đối tác về...
- **[11:00] Dev:** Ví dụ nhận dữ liệu về mà ghi vào 3 bảng thì bên dưới này sẽ hiển thị 3 bảng đúng không anh?
- **[11:05] Tech Lead:** Đúng rồi, ghi vào bảng nào thì phải nhận diện ra được bảng đó.
- **[11:15] Dev:** Ý là hệ thống sẽ tự động detect ra câu lệnh ghi đó tác động vào những bảng nào đúng không?
- **[11:25] Tech Lead:** Cái câu lệnh ghi dữ liệu của mình đã có trong cấu hình rồi thì phải biết nó ghi vào bảng nào chứ.
- **[11:32] Dev:** Nhưng ví dụ cái câu lệnh đó người ta chỉ ghi vào 2 bảng, người ta bỏ bớt 1 bảng đi thì sao?
- **[11:42] Dev:** Em đang hỏi là câu lệnh ghi dữ liệu này mình xử lý tự động nhận diện hay cho người ta nhập?
- **[11:46] Tech Lead:** Một bảng tương ứng với một lệnh ghi, hoặc trong câu lệnh ghi đó nó ảnh hưởng đến những bảng nào.
- **[11:53] Dev:** Nhưng khai báo danh sách bảng này để làm gì trên UI anh?
- **[12:02] Tech Lead:** Khai báo để người ta biết phạm vi dữ liệu bị ảnh hưởng, để kiểm soát an toàn và phân quyền ghi.
- **[12:20] Dev:** Nghĩa là mình hiển thị danh sách bảng bị ảnh hưởng ra để người ta kiểm chứng?
- **[12:25] Tech Lead:** Đúng rồi, hiển thị danh sách bảng ảnh hưởng ra cho người ta nhìn thấy. Ví dụ trích xuất được câu lệnh này ảnh hưởng đến 3 bảng A, B, C thì liệt kê 3 bảng đó ra.
- **[12:35] Dev:** Để kiểm chứng lại xem câu lệnh tác động đúng những bảng nào.
- **[12:40] Tech Lead:** Câu lệnh ghi này Admin có thể tự chỉnh sửa được, sau đó hệ thống sẽ detect xem đang ảnh hưởng bảng nào.
- **[12:48] Dev:** Admin được chỉnh sửa câu lệnh ghi?
- **[12:50] Tech Lead:** Ừ, Admin có quyền sửa câu lệnh ghi. Còn nếu câu lệnh phức tạp quá mà parser không tự trích xuất được thì cho phép Admin tự chọn hoặc điền danh sách bảng ảnh hưởng vào luôn cũng được, không sao hết.

---

### [15:00 – 20:00] Gộp Câu Lệnh Ghi (Write SQL Script) & Quản Lý Tham Số Truyền Vào

- **[15:06] Tech Lead:** Chỗ này hiện tại em đang chia làm 3 câu lệnh ghi riêng biệt đúng không?
- **[15:10] Dev:** Dạ, cái đó gộp chung thành một câu lệnh được không anh?
- **[15:13] Tech Lead:** Gộp chung được chứ! Bản chất là ghép chung các câu lệnh lại chạy trong một transaction thôi mà.
- **[15:20] Dev:** Tại vì hôm bữa em thấy bên quy chuẩn yêu cầu là ghi xuống bảng nào thì ghi rõ một bảng, nên em tách ra từng câu một bảng.
- **[15:27] Dev:** Nếu gộp chung được thì quản lý trên UI nó đơn giản hơn nhiều, đỡ phải cấu hình phân mảnh nhiều dòng.
- **[15:36] Tech Lead:** Gộp chung được, mà tách riêng cũng được. Nhưng gộp chung thì quản lý trên một form nó khỏe hơn cho người cấu hình.
- **[15:46] Tech Lead:** Cơ bản là mấy câu lệnh SQL này cách nhau bởi dấu chấm phẩy hoặc từ khóa kết thúc lệnh là chạy được.
- **[15:54] Tech Lead:** Quan trọng nhất là lúc truyền tham số vào câu lệnh Insert/Update kìa. Khi insert thì nó sinh ra các tham số `@param`, danh sách tham số này phải được mapping chuẩn xác từ gói tin nhận về.
- **[16:15] Tech Lead:** Ví dụ câu lệnh 1 cần 10 tham số, câu lệnh 2 cần 10 tham số... nếu hai câu dùng chung 9 tham số thì chỉ cần định nghĩa các tham số đặc thù riêng biệt, tránh khai báo lặp lại gây dư thừa.
- **[16:38] Dev:** Như vậy sẽ đỡ số lượng trường khai báo trên giao diện rất nhiều.
- **[17:05] Dev:** Để em cập nhật trước phần này, rồi cái phần giao diện sửa lại sau.
- **[17:23] Tech Lead:** Gộp câu lệnh lại thì vừa gọn UI mà vừa tận dụng được transaction khi thực thi, dữ liệu ghi xuống đảm bảo đồng bộ hoàn toàn giữa các bảng liên quan.

---

### [20:00 – 25:00] Cơ Chế Khóa Nối Đuôi (@LastKey) Khi Trùng Mốc Thời Gian & Composite Key

- **[20:04] Tech Lead:** Bữa trước anh nói chỗ khóa `@LastKey` rồi đó.
- **[20:09] Tech Lead:** Ví dụ trong trường hợp `@LastKey` này mình xếp theo chuỗi thời gian...
- **[20:18] Tech Lead:** Nếu mà thời gian nó bị trùng nhau thì sao? Ví dụ lúc 10:00 có bản ghi A, 10:00 có thêm bản ghi B, 10:00 có bản ghi C...
- **[20:30] Tech Lead:** Khi select lấy dữ liệu lên, nếu chỉ dựa vào mỗi trường thời gian thì lần quét tiếp theo nó sẽ không biết lấy tiếp từ bản ghi nào để không bị sót hoặc không bị lặp lại dữ liệu.
- **[20:41] Tech Lead:** Cho nên bắt buộc phải có cơ chế tạo **Composite Key (Khóa kết hợp)**.
- **[20:47] Dev:** Khóa kết hợp này trên giao diện mình cũng phải khai báo đúng không anh?
- **[20:53] Tech Lead:** Đúng rồi, trong bảng cấu hình tham số có trường để khai báo khóa.
- **[21:05] Tech Lead:** Quan trọng là cái key đó phải là duy nhất (Unique). Ví dụ kết hợp giữa mốc thời gian và ID tự tăng hoặc mã định danh của bản ghi.
- **[21:28] Tech Lead:** Có hai cách để xử lý:
  - Cách 1: Nối chuỗi trực tiếp dưới câu lệnh SQL, ví dụ dùng hàm `CONCAT` để sinh ra một cột khóa ảo kết hợp giữa thời gian và ID, rồi gán vào biến `@LastKey`.
  - Cách 2: Truyền riêng các trường thời gian và ID lên tầng xử lý C#, sau đó code logic tự động ghép chuỗi và lưu vết lại giá trị mốc cho lần chạy kế tiếp.
- **[22:04] Dev:** Giao diện cho phép người dùng chọn trường làm khóa rồi hệ thống tự sinh ra biểu thức kết hợp?
- **[22:20] Tech Lead:** Khi chạy worker, nó sẽ căn cứ vào giá trị mốc khóa này để so sánh trong mệnh đề `WHERE Key > @LastKey` để lấy đúng phần dữ liệu nối tiếp.

---

### [25:00 – 29:59] Kiểm Tra Trùng Lặp Chiều Nhận (Check Duplicate) & Thống Nhất Kế Hoạch Triển Khai

- **[25:05] Dev:** Nếu chọn cách nối chuỗi dưới SQL thì không cần phải điền biểu thức phức tạp trên UI, nhưng bù lại câu lệnh SQL phải xử lý format chuỗi.
- **[25:20] Tech Lead:** Đúng, mỗi cách có ưu nhược điểm riêng. Nhưng bản chất là khi đã có giá trị mốc thì phải lưu trữ lại được để lần quét sau worker đọc lên và truyền ngược lại vào câu truy vấn.
- **[26:18] Dev:** Em nghĩ là cái phần tạo biểu thức phức tạp này để làm sau cùng đi.
- **[26:26] Dev:** Bây giờ hoàn thiện xong cái form cấu hình này, rồi em qua làm tiếp cái Service bên chiều nhận dữ liệu.
- **[26:33] Tech Lead:** Chiều nhận dữ liệu thì đơn giản hơn chiều gửi rất nhiều. Chiều nhận đối tác gửi qua bao nhiêu thì mình nhận bấy nhiêu.
- **[26:40] Dev:** Chiều nhận về không sợ bị sót, chỉ cần kiểm tra trùng lặp (`Check Duplicate`) thôi.
- **[26:46] Tech Lead:** Đúng, chỉ cần kiểm tra trùng lặp theo mã định danh hoặc Unique Key, tránh trường hợp đối tác gửi lặp bản tin dẫn đến insert trùng dữ liệu vào hệ thống.
- **[27:05] Dev:** Tóm lại về mặt giao diện là em cần hoàn thiện: Khóa form mặc định (chỉ mở khi bấm Chỉnh sửa), chuyển sang dùng Modal popup khi sửa để tránh cuộn trang, tách riêng Tab Gửi và Tab Nhận, và bổ sung các icon trực quan cho màn hình so sánh Diff với gói mẫu.
- **[27:40] Tech Lead:** Đúng rồi. Tái cấu trúc lại UI cho nó gọn gàng và dễ sử dụng. Làm xong phần form này rồi kiểm tra thông suốt luồng gửi và nhận dữ liệu nhé.

