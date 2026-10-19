# 🎙️ BẢN GỠ BĂNG HỘI THOẠI KỸ THUẬT: STREET NO. 8
> **Tệp ghi âm gốc:** [`DocBusinessThienAn/Street No. 8.m4a`](file:///Users/hoangquydat/ThienAn/DocBusinessThienAn/Street%20No.%208.m4a)  
> **Thời lượng:** 13 phút 08 giây (787.9 giây)  
> **Thời điểm trao đổi:** Tháng 10/2026  
> **Thành phần tham gia:**
> - **Speaker 1 (Lead / Senior Architect):** Định hướng kiến trúc hệ thống, quy chuẩn cấu trúc mã nguồn, triệt tiêu interface thừa, hướng dẫn dùng Furion DI và giải thích bộ rule AI.
> - **Speaker 2 (Developer):** Đặt câu hỏi, thảo luận về cấu trúc module, interface, entity và xin bộ rule cho AI.

---

## 📌 TÓM TẮT CÁC QUYẾT ĐỊNH KIẾN TRÚC CỐT LÕI

1. **Triệt tiêu Interface 1:1 — Tận dụng Furion Dependency Injection:**
   - CẤM tạo interface 1:1 cho từng service nghiệp vụ (ví dụ `ITmsEquipmentService` chỉ để map với `TmsEquipmentService`).
   - Lý do: Tốn công khai báo và mapping, sửa một phương thức phải sửa cả hai nơi, luồng debug bị rối (F12 nhảy vào interface rỗng thay vì code thực thi), test và refactor rất mệt mỏi.
   - Thay thế bằng cách kế thừa trực tiếp `IScoped`, `ITransient`, hoặc `ISingleton` của Furion trong Service. Framework tự động scan và đăng ký theo vòng đời phù hợp.
   - Chỉ dùng Interface khi có từ 2 implementations trở lên (Factory Pattern, Strategy Pattern), hoặc làm hợp đồng API Refit / Options.
2. **Cấu trúc Phân cấp Base & Exception:**
   - Không để từng tính năng tự tạo Base Exception / Base Service riêng lẻ làm phân mảnh và mất tính đồng nhất.
   - Phân cấp chuẩn: Base dùng chung toàn hệ thống (`CommonBaseController`, `SqlSugarRepository<T>`, `BaseLocaleManager`) $\rightarrow$ Base cấp module (`BaseController`, `BaseRepository<T>`) $\rightarrow$ Feature.
   - Exception 100% dùng qua `Oops.Oh(BaseMsg...)`, cấm hardcode chuỗi string.
3. **Quy chuẩn Entity & Dữ liệu dùng chung:**
   - Tên Entity bắt đầu bằng tiền tố phân hệ (`Tms...`, `Vms...`, `Fms...`), không cần thêm hậu tố `Entity`.
   - Dữ liệu dùng chung giữa các module bắt buộc chuyển xuống `Shared/Core.Entities` hoặc `Shared.DTO`.
4. **Tầng Base Helper trung gian:**
   - Xây dựng các Base Helper trung gian (`BaseHelper.FileHp`, `BaseHelper.NetworkHp`, `BaseHelper.ImageHp`, `BaseHelper.ExcelHp`, `BaseHelper.RetryHp`, `BaseHelper.EChartHp`) để lập trình viên dễ dàng gọi lại mà không cần nhớ chi tiết hàm nội bộ.
5. **Bộ Quy Chuẩn Cho AI (AI Rules):**
   - Đã có bộ rule chuẩn (từng dùng cho Kiro, nay mở rộng cho các AI agent): chuẩn Entity, CSS 5 biến màu Element Plus (cấm mã hex, cấm `!important`), VxeTable grid pattern, phân cấp i18n không hardcode text.

---

## 📜 NỘI DUNG CHI TIẾT (FULL TRANSCRIPT WITH TIMESTAMPS)

### [00:00 - 02:45] Chuẩn hóa thư mục Base, phân cấp Exception và Service

- **[00:00] Speaker 1:** Ờ, có một... có một... chia cho đúng thôi chứ đừng có mỗi cái tạo một cái riêng. Tạo cái riêng như mốt người khác tạo cái riêng này nọ nó nhiều, nó lại không có đồng nhất. Thì mình sẽ cố gắng mình fit theo cái cấu trúc...
- **[00:19] Speaker 1:** Thứ hai, cái service với lại cái base này nọ... viết... *(cười)*
- **[00:39] Speaker 1:** Hiểu không? Mở sổ... sai nè ba.
- **[01:04] Speaker 1:** Alo... Nè, cái base thì trong nó chia ra từng... ví dụ này chức năng tổng đi. Mình sẽ có base exception, base validate... một cái là cái gì quên tiêu rồi...
- **[01:36] Speaker 1:** Nó có đủ cả ba cái...
- **[01:39] Speaker 2:** Ủa cái của em đang bị như vầy nè...
- **[01:43] Speaker 1:** Này là riêng rồi. Ờ ok, không có...
- **[01:52] Speaker 1:** Rồi trong này vô đây sẽ base exception. Nó là chung cho tất cả các project dùng, thì nó sẽ kế thừa cái thằng này. Rồi trong đó nó sẽ chia thành từng cấp: một cái chung cho cái thằng bự đó nếu có, còn cái nào mà chưa có... Rồi nếu mà nó riêng cho chức năng đó thì mình sẽ bỏ riêng chức năng. Giống như của em một trang chức năng nè, rồi tạo file gì đó... VIP nè... Xài cái gì...
- **[02:35] Speaker 1:** Mấy cái VIP này nọ... Nó sẽ xa cái gì đó...

---

### [02:46 - 06:17] Hạn chế lạm dụng Interface – Sử dụng Dependency Injection của Furion

- **[02:46] Speaker 1:** Rồi tiếp theo nữa là interface nhiều quá. Interface hơi nhiều thì xem xét interface có cần thiết hay không. Còn hiện tại á là cái source của mình đã hỗ trợ sử dụng dạng dependency injection không cần interface, mà mình sẽ khai báo những cái thằng của Furion á, thằng Furion nó có hỗ trợ đó.
- **[03:15] Speaker 1:** Cái gì dữ vậy? Mấy thằng như `IScoped` nè, `ITransient`, với lại `ISingleton`... Chỉ cần khai báo vô là sử dụng thằng này thẳng luôn, không cần phải interface gì hết. Mình tự tạo cái interface nó rất là rối. Em sửa hàm em phải đi bổ sung hàm trong interface nữa, và nó rất là khó debug. Bấm trỏ vô interface cái trỏ qua bên service... ở nhiều tùm lum chỗ nó rối. Nên là tốt nhất là cứ xài thẳng từ service luôn. Mình sử dụng cái này để nó hỗ trợ nó đăng ký sự kiện... khỏi phải đăng ký interface.
- **[04:05] Speaker 2:** Nó này hình như nó chỉ hỗ trợ ngay chỗ API đúng không?
- **[04:09] Speaker 1:** API chỗ nào cũng được. Tại vì cái Furion nó đang xuyên suốt dự án. Cứ khai báo chỗ nào cần đăng ký để nó xài dạng dependency injection á thì cứ đăng ký vào, gọi thẳng nó luôn, không cần phải đi đăng ký riêng. Em đăng ký interface là em phải đăng ký riêng đúng không? Em phải ghi ở trong cái map cái thằng interface tương ứng với thằng thực thi chính đúng không? Đó, thì nó lại mất công vừa mất công đi khai báo cái đó, vừa phải mất công đi tạo interface, rồi bổ sung thêm hàm lại phải tạo thêm hàm ở đây rất là mất công. Thứ hai nữa là test rất là khó.
- **[04:54] Speaker 1:** Thì làm theo cái dạng thứ nhất thì nó có hỗ trợ cái thư viện này nọ, xài thư viện cho nó dễ. Lúc sau chỉ cần khai báo gán nó vào là xong. `IScoped`, `ITransient` với lại `ISingleton`... nó hỗ trợ ba cái, đủ luôn ba cái, không cần phải khai báo gì thêm. Nếu không cần thiết thì chuyển hết nó về.
- **[05:22] Speaker 1:** Interface chỉ dùng trong trường hợp là muốn nó dạng kiểu dạng Factory... một thằng viết để tổng quát chung mà có thể sử dụng nhiều thằng nhau đó, thì mới dùng chỗ đó. Còn tình huống mà mình chỉ sử dụng nó bình thường thì tốt nhất hạn chế xài interface. Tại lúc trước dự án trước xài interface rất là cực, rất là khó debug luôn. Đi kiếm cái hàm đó nằm ở đâu... Cho nên tốt nhất nên hạn chế interface. Kiếm cái thư viện mới để mọi người xài, hạn chế interface lại.

---

### [06:18 - 08:50] Xử lý luồng kết nối, quy chuẩn Entity và Helper trung gian

- **[06:18] Speaker 1:** Rồi cái nữa...
- **[06:20] Speaker 2:** Kiểu qua nó đang xử lý cái luồng gửi mấy cái dạng kết nối á, kiểu HTTP nè, Kafka... tương tác...
- **[06:31] Speaker 1:** Nó sẽ nhận dạng JSON vô rồi nó xử lý thôi.
- **[06:40] Speaker 1:** Exception dưới này thì đúng nè, exception trên này thì sai.
- **[06:51] Speaker 1:** Entity thì cố gắng nó chỉ cứ đuôi Entity một tí để cho nó dễ xử lý, dễ sử dụng, dễ biết được nó nằm ở khu vực nào.
- **[07:04] Speaker 2:** Mấy cái Entity á, có cần thêm "Entity" đằng sau không?
- **[07:07] Speaker 1:** Không. Tại Entity là bắt đầu bởi cái tiền tố rồi. Mình search tiền tố là nó sẽ ra. Còn mấy cái này là phải có cái đuôi: một là cái đuôi cho nó... Entity...
- **[07:35] Speaker 1:** Thứ hai nữa là xem xét những cái này có xài chung không, cái số nào có xài chung không...
- **[07:41] Speaker 2:** Xài chung để vô Core?
- **[07:42] Speaker 1:** Thì xả xuống dưới thằng Share, Share chỗ Entity á, Core Entity đó. Xả xuống dưới đó thì sẽ không... Ở dưới này sẽ có một cái thằng Base dùng chung. Tại vì do là hình dung mọi người sẽ không có nhớ cái tên, cái tên hàm thì sẽ để trong Base. Mọi người cứ gõ vô cái Base, trong cái Base nó sẽ có cái gì đó, mình sẽ làm tầng lớp trung gian ở giữa này. Kéo qua thay vì gọi thẳng tới service Entity thì nhiều khi không nhớ, chẳng hạn vậy... Sẽ có một cái Base helper, hỗ trợ... Cái đuôi thường sẽ là helper cho thằng này xài. Một thằng tiền hàng chữa nè... Đó thì làm mấy cái helper sau.

---

### [08:51 - 10:54] Thảo luận về Rule AI (Kiro, Cursor/Cline, chuẩn CSS, i18n, VxeTable)

- **[08:53] Speaker 1:** Nhiều lắm... Bắt được con nào thì nắn con đó. Ủa em biết mấy cái này em không biết cái rule cho AI hả?
- **[09:03] Speaker 2:** Biết rồi, nhưng mà nó không đúng. Lúc được lúc không à.
- **[09:07] Speaker 2:** Vậy anh cho em cái rule đó đi.
- **[09:09] Speaker 1:** Trong cái rule đợt share trên nhóm á.
- **[09:12] Speaker 2:** Em chưa có.
- **[09:13] Speaker 1:** Ủa hôm nọ mình nhắn call đó, mình nhớ là ghi nhận lại ta?
- **[09:18] Speaker 2:** Cái này chắc anh có chứ tụi em không có đâu. Có thì đưa em cái link đó đi.
- **[09:22] Speaker 1:** Đợt share lên lâu lắm rồi, đây nè... Nhưng mà nó không đúng đâu. Chạy hình như nó chưa đúng. Cái này phải cải thiện thêm. Tại vì thứ nhất là cái này trước làm cho Kiro, thứ hai nữa là giờ chuyển qua con mới thì có thể nó đọc không đúng.
- **[09:44] Speaker 2:** Không sao, tại em để chung một nguồn rồi xong rồi AI nào dùng cũng được á.
- **[09:48] Speaker 1:** Còn cái rule trong này nó sẽ có luôn cái rule dịch thuật đồ đó. Mà thấy giờ nó đang trật quớt nữa nè. Hình như cái này chạy không đúng...
- **[09:56] Speaker 2:** Vậy anh cho em xin mới đi.
- **[09:59] Speaker 1:** Mới chưa xong, xài tạm cái cũ đi, có cái gì xài đó. Trong đó nó có một số rule nó có sẵn rồi nè: về những cái chuẩn chuẩn vậy nè... Entity nè... Rồi mấy cái liên quan đến xác thực, CSS nè, màu nè (4 màu, 5 màu gì nãy...), CSS font name, font size... Mấy biến thể nếu có... Rồi dịch thuật... Hình như có trong này luôn nè... VxeTable nè, rồi dịch thuật nè... Lúc trước nó vẫn chạy đúng á, mà không biết giờ sao...
- **[10:48] Speaker 2:** Không sao, để em cứ bám theo cái này trước nha.

---

### [10:55 - 13:08] Cấu trúc Service trong Module & Chi phí Refactor

- **[10:55] Speaker 1:** Rồi, helper đó. Ta thấy nó ngắn quá không? Mở vô... Cái GitLab chắc nó bị lock rồi, giờ mình Google book fork cái review cũng được. Nè, nó ra nguồn nè... Rồi tiếp, rồi module... rồi ok...
- **[11:53] Speaker 1:** Nó đang để như vậy đúng không? Thì này những cái hàm nào... Đây là tầng service nè. Service này đúng không? Service thì vô đây là một... đó là một chức năng. Còn mà vô nó bay vô trong này nó sẽ lấy service không.
- **[12:19] Speaker 1:** Còn interface này là cái gì? Mình cũng cố gắng tạo ra một cái thư mục nằm ở trong, trong này tương ứng. Ai vô mà thấy service không... để cho nó đồng nhất, dòm lại nó dễ tìm. Mình vô mình kiếm service gì đó thì có service không. Còn vô đó liên quan tới file gì đó...
- **[12:47] Speaker 2:** Nghĩa là service trong cái module?
- **[12:51] Speaker 1:** Ừ. Còn cái service kia chưa có thống nhất cái cấu trúc, chưa có chuẩn. Cứ viết cho nó lấy trước đi rồi mốt dọn lại sau, tùy ai sửa.
- **[13:03] Speaker 2:** Nhiều file mệt hả anh?
- **[13:04] Speaker 1:** Đúng rồi, nó refactor mệt.
