---
tier: A
read: full
source: ../../../Plan/_source/audio/13.48, 16 thg 9__1.m4a
date: 2026-09-16
model: Gemini Native Audio Multimodal
status: verified
---

# Thảo luận ShareData Worker — Định danh đối tác, Clone Service Test tải & Quản lý hiệu năng CSDL

**Ngôn ngữ:** Tiếng Việt  
**Thời lượng:** 16:47  
**File nguồn:** ../../../Plan/_source/audio/13.48, 16 thg 9__1.m4a  
**Ngày họp:** 16/09/2026 (13:48)  
**Chủ đề:** Chuẩn hóa gói tin dữ liệu ShareData (Metadata / Body), loại bỏ phân nhánh version, cơ chế clone service & cấu hình cờ (flag) gửi/nhận để test song song nhiều đối tác, đánh giá rủi ro nghẽn DB và nguyên tắc phân bổ thứ tự công việc.

---

## 1. Tóm tắt nội dung toàn diện (Executive Summary)

### 1.1. Chuẩn hóa cấu trúc gói tin (Message Payload & Metadata)
- **Giữ nguyên cấu trúc dữ liệu cốt lõi:** Không tự ý bọc thêm các lớp vỏ ngoài phức tạp làm sai lệch cấu trúc dữ liệu mà các bên đã thống nhất.
- **Tách bạch thông tin định danh (Metadata) và Dữ liệu (Body):**
  - Mã đối tác (PartnerCode) hoặc mã trạm sẽ được truyền vào metadata hoặc một trường cố định trong body (ví dụ partnerCode: "A101").
  - Không truyền thông tin đối tác vào header HTTP nếu cấu trúc gói tin qua socket/broker yêu cầu dữ liệu nằm trọn vẹn trong payload.
- **Loại bỏ xử lý theo Version:**
  - Không phân biệt xử lý theo Version 1 hay Version 2 trong code bóc tách.
  - Gói tin đến theo định dạng nào thì bóc tách (parse) trực tiếp theo định dạng đó; nếu sai cấu trúc thì báo lỗi (exception), tránh việc duy trì nhiều nhánh logic version gây cồng kềnh mã nguồn.

### 1.2. Cơ chế kiểm thử đa đối tác (Multi-Partner Testing & Service Cloning)
- **Mục tiêu kiểm thử:** Giả lập tình huống nhiều đối tác độc lập (A1, A2, A3, A4...) cùng truyền nhận dữ liệu đồng thời để kiểm tra tính ổn định của luồng xử lý.
- **Giải pháp tách cờ (Flag) và Clone Service:**
  - Nhân bản (clone) Worker Service thành nhiều instance độc lập, dùng chung CSDL:
    - **Instance 1 -> 4 (Chỉ gửi - Send Only):** Bật cờ chỉ gửi, gán mã định danh đối tác tương ứng (A1, A2, A3, A4), tắt hoàn toàn luồng nhận.
    - **Instance 5 (Chỉ nhận - Receive Only):** Bật cờ chỉ nhận, đóng vai trò trung tâm tiếp nhận gói tin từ cả 4 đối tác trên, bóc tách và ghi dữ liệu vào CSDL.
  - **Môi trường Production:** Chạy 1 service duy nhất hỗ trợ song song 2 chiều gửi và nhận bình thường.

### 1.3. Đánh giá rủi ro hiệu năng CSDL & Giải pháp DB phụ
- **Rủi ro nghẽn tải (Lock / Contention):** Khi nhiều đối tác cùng gửi dữ liệu theo chu kỳ dày đặc (mỗi 29s - 30s), việc nhiều tiến trình cùng lúc truy vấn hoặc insert vào cùng bảng có thể gây quá tải CPU/RAM và làm treo hệ thống chính (ITS).
- **Cảnh báo lỗi cắt cụt chuỗi ký tự:** Lưu ý trường dữ liệu text/JSON dài (như giới hạn 4000 ký tự ở một số hệ thống) có thể bị cắt cụt đuôi, dẫn đến lỗi cú pháp khi parse JSON.
- **Giải pháp dự phòng (Secondary Database):** Nếu quá trình test tải cho thấy DB chính (DEV_ITS10 / 10.10.8.30) bị ảnh hưởng, sẽ tách riêng một CSDL phụ chuyên dùng để nhận dữ liệu ShareData, sau đó mới đồng bộ có kiểm soát sang DB chính.

### 1.4. Nguyên tắc tổ chức công việc (Workflow & Prioritization)
- **Quy tắc "Đáy phụ thuộc" (Bottom-up dependency):** Tìm module nằm ở tầng sâu nhất (module gốc/service xử lý dữ liệu lõi) để hoàn thiện và chốt dứt điểm trước, sau đó mới đến các tầng gọi phụ thuộc (WebAPI, Frontend).
- **Tránh làm việc vòng tròn:** Không sửa lắt nhắt từng phần rồi ghép nối chắp vá gây lãng phí thời gian và phát sinh lỗi chéo.
- **Phân định lộ trình:** Tập trung dứt điểm ShareData Worker trước để bàn giao API chuẩn cho Frontend (Kim) ghép giao diện thật, chấm dứt việc dùng mock data; sau đó toàn đội mới chuyển sang VideoWall.

---

## 2. Toàn văn nội dung đối thoại (Full Transcript)

> **Quy ước định danh người nói:**
> - **Người 1 (Anh Sơn):** Senior Dev / Lead kỹ thuật (hướng dẫn kiến trúc, thiết kế cờ test, đánh giá rủi ro DB).
> - **Người 2 (Dev):** Dev phụ trách triển khai ShareData Worker.
> - **Người 3 (Anh Đạt):** Dev tham gia trao đổi giải pháp service và API.

| Mốc thời gian | Người nói | Lời thoại chi tiết |
| :---: | :---: | :--- |
| **00:00** | **Người 1** | ...hợp cái header đó là chỗ khác nữa, nó đâu phải là bạt từ trong data này đâu. |
| **00:06** | **Người 2** | Ừ, tiếp. |
| **00:08** | **Người 1** | Rồi. Thì nó, cái dữ liệu cấu trúc mình như thế nào thì mình nhận y chang vậy, không có tự sửa thêm. Sửa thêm là coi như là không khớp cấu trúc mà tự nhiên làm cấu trúc cho đã rồi... |
| **00:19** | **Người 2** | Vậy là bây giờ... |
| **00:20** | **Người 1** | ...xong rồi gửi lại cấu trúc đặc quyền hoàn toàn. |
| **00:22** | **Người 2** | Không, vậy từ từ để cho em chốt lại này nha: Là bây giờ anh Đạt... anh vẫn sẽ nhận cái này... |
| **00:30** | **Người 1** | Không có gửi, chỉ gửi cái nội dung bên trong thôi. Cái đó bỏ hết! Không có care cái gì ở trên đó hết nữa. Bỏ hết. Còn nếu mà em muốn gửi á, em gửi ở trong header á. |
| **00:43** | **Người 2** | Không, ý là lúc mà gửi mấy cái đó anh có cần những cái thông tin mà em đang xử lý không vậy? |
| **00:47** | **Người 3 (Đạt)** | Có, có cần. |
| **00:49** | **Người 1** | Tại sao cần? |
| **00:50** | **Người 3 (Đạt)** | Cần để lưu DB nè. |
| **00:52** | **Người 2** | Để mở cái đó nằm rõ ra để coi anh Sơn nói muốn cái gì đây... Này partner code nè, data ID nè... |
| **00:58** | **Người 1** | Để làm cái gì á? |
| **01:00** | **Người 2** | Để cái job của em nó scan cái bảng này nó lấy để nó xử lý. |
| **01:03** | **Người 1** | Đâu liên quan! Mấy dữ liệu đó mình lưu về đâu liên quan, cũng không làm cái gì hết. Tự nhiên care nó version 1 làm cái gì? |
| **01:10** | **Người 2** | Thì không care version 1, nhưng mà care cái thằng này chứ, hai thằng này chứ. |
| **01:14** | **Người 1** | Ủa mấy thằng đó em em em chuyển đổi nó qua thằng nào được? |
| **01:17** | **Người 2** | Chuyển đổi qua là gửi đi được. |
| **01:19** | **Người 1** | Một là em bắt buộc truyền trong gì? |
| **01:21** | **Người 2** | Truyền trong cái thằng payload đúng không? |
| **01:23** | **Người 1** | Truyền trong thằng dữ liệu. Đúng không? |
| **01:27** | **Người 2** | Ờ ok. Rồi ok chịu. |
| **01:30** | **Người 1** | Hai là nâng cao hơn là sẽ truyền khi mà gọi... Ví dụ gọi API thì nó sẽ có cái thằng header đó. Gán code vào trong header. Đúng không? Nhưng mà cách dễ nhất là đẩy hết vào trong thằng dữ liệu, cấu trúc dữ liệu. |
| **01:46** | **Người 2** | Nhưng mà bỏ thằng payload luôn nha. Chỉ lấy cái ở trong đó thôi. |
| **01:48** | **Người 1** | Bỏ cái vỏ, chỉ lấy ở trong thôi. Nói chung là cái thằng receiver bên kia như thế nào là mình sẽ bắt buộc biến đổi y chang vậy, không có bỏ, không có thay đổi gì hết. Mục tiêu là đang đang cấu trúc theo dữ liệu trả về mà. Chứ cấu trúc dữ liệu trả về cho đã rồi tự nhiên ngồi làm thêm cấu trúc ở ngoài, xong rồi lại phải đi thống nhất ở bên ngoài nữa. Rồi thằng này thống nhất kiểu này, thằng kia thống nhất kiểu kia rồi lại sinh ra chức năng thống nhất cấu trúc bên ngoài nữa. |
| **02:18** | **Người 2** | Ừm. |
| **02:20** | **Người 1** | Dữ liệu cấu trúc như thế nào thì trả về như vậy, cho gọn. Rồi còn mấy cái thông tin kia không có liên quan, mấy thông tin version mấy thông tin gì không có liên quan gì hết. Mình nhận là mình nhận thôi. Mình parse ra nó sai là nó ghi nhận lỗi, chứ không cần care là thằng đó version 1, phải map cái version 1 của mình không có. |
| **02:44** | **Người 2** | Ok anh. Bỏ luôn. |
| **02:46** | **Người 1** | Tại vì giờ quan trọng là giờ em hình dung là giờ dữ liệu họ gửi đi, tại hiện tại đang là cấu trúc thống nhất như thế nào thì y chang thống nhất như vậy. Parse ra không được thì báo lỗi thôi. Chứ không phải là cố gắng thằng này chạy version 1 phải parse version 1, thằng kia chạy version 2 phải parse version 2. Mất công. |
| **03:07** | **Người 2** | Ừ. Ok. |
| **03:10** | **Người 1** | Rồi. Bỏ version luôn. Chuẩn bị họp nè, trả phòng anh Nghi nè. 14:50. |
| **03:22** | **Người 2** | Xong rồi cái lúc mà nó nhận rồi á thì nó sẽ qua bên parse này để nó... nó bóc tách cái gói tin ra rồi nó gọi lên mấy cái mapping, mấy cái process để nó xử lý. |
| **03:34** | **Người 1** | Ừ. |
| **03:35** | **Người 2** | Tiếp sau đó thì nó sẽ xuống câu SQL. Gói tin nào phụ thuộc... à ở đây có bảng SQL này... |
| **03:42** | **Người 3 (Đạt)** | Câu thực thi SQL đang để ở đâu? |
| **03:44** | **Người 2** | Đang lưu ở trong DB. |
| **03:45** | **Người 3 (Đạt)** | Lưu trong DB tổng hợp đó hả? |
| **03:46** | **Người 2** | Ừ. Lưu từng bảng, từng gói tin từng bảng. Câu nào thuộc bảng nào, của gói tin nào... |
| **03:59** | **Người 1** | Ừ, cũng được. |
| **04:00** | **Người 2** | Rồi nó sẽ bốc lên nó lấy để nó ghi data ngược xuống lại. Thì cái này là chuẩn của anh luôn á. |
| **04:12** | **Người 1** | Gì vậy? Chuẩn của anh gì? |
| **04:15** | **Người 2** | Chuẩn của anh Sơn, chuẩn cái viết qua SQL nè. |
| **04:19** | **Người 1** | Cái gì for for XML? Đâu. SQL for XML? Không biết, không biết đâu, của ai á, chịu. |
| **04:30** | **Người 1** | Từ từ, review lại... Gói... bảng... bảng... |
| **04:54** | **Người 1** | Ừ cũng... cũng ổn á. Nhưng mà thấy nó kỳ kỳ á. Nhưng mà cái tầm test coi nó ổn không? |
| **04:59** | **Người 2** | Test nó ổn. Ví dụ mà có khai báo đầy đủ gói tin á thì là test ổn. |
| **05:03** | **Người 1** | Coi chừng nó va chạm mắt kìa. Nó chỉ lưu được 4000 ký tự. |
| **05:07** | **Người 2** | Đâu? Đâu đâu đâu? |
| **05:10** | **Người 1** | Nhiều khi lưu dữ liệu DB không ổn, nó dài quá nó bị cắt chữ á. Cái chỗ cái bảng hải quan á, cẩn thận bị cắt á, tại vì có trường hợp bị cắt rồi. Nó cắt là nó mất dữ liệu phía sau á. |
| **05:24** | **Người 2** | Ừm. Nhưng tụi nó cắt là nó lỗi luôn chứ? |
| **05:27** | **Người 1** | Nó truyền đúng cái dữ liệu đó, còn lại nó cắt bỏ. Thì cũng có thể báo lỗi. Nhiều khi sợ nó báo lỗi vượt quá ký tự cho phép. Nhưng mà ví dụ như có các hệ chỉ lưu được 4000 ký tự thôi. |
| **05:43** | **Người 2** | 2004 hả? |
| **05:45** | **Người 1** | 2004. Mới nói! Chắc gì mấy thằng kia... Nhiều thứ lắm chứ bộ. |
| **05:54** | **Người 1** | Giờ thằng này đúng không? Giờ thằng này nãy nó bị dính cái gì? Cấu trúc đi vào nè, cấu trúc đi vào sửa nó lại. Sửa lại theo cấu trúc vậy thôi, làm đơn giản thôi. Không có tự thêm tiền kiệu vô nó rối lắm. Cấu hình như thế nào thì trả về như vậy. Xong rồi lấy cái dữ liệu đó xử lý. Thì trong cái dữ liệu của nó nó sẽ chứa thông tin, nếu mà đúng á... nếu mà đúng thực ra cái thông tin liên quan đến đối tác nha, thường là nó phải đúng là nó phải đi theo dựa trên thông tin đăng nhập. Nó mới đúng. Ví dụ như thằng đối tác A này mình sẽ cấp cái key cho nó. Key hay mật key á, hoặc token á. Mình phải xác định được là cái thằng key token đó đang thuộc của thằng đối tác nào. Nó mới đúng. Chứ không phải dựa trên dữ liệu đâu. Giờ nếu mà dựa trên dữ liệu... Anh nè, anh đi anh... anh hack rượt qua anh. Anh biết thằng khác cái mã đó là cái mã dài, anh đi anh sửa lại dữ liệu của anh, sửa lại mã khác, gửi dữ liệu phá tới. Giờ em cấp cho thằng này đối tác là XYZ, thằng kia ABC, thằng XYZ này biết được mã kia ABC, ok gửi hàng loạt ABC một đống dữ liệu spam á, trong khi chết dí á. Nên đang dựa theo dữ liệu là thấy cũng tạm thời. Nếu mà đúng là mình phải dựa trên cái thông tin liên quan đến thằng đó. Thì nó sẽ dính đến phần bảo mật. Nó cấp key cho nó lock in chẳng hạn. Thì sẽ biết được thằng đó nó sẽ thuộc thằng nào. Còn cái key á, key đó biết được nó là thằng nào, từ đó mình mới xác định được liên ngành đến thằng nào. |
| **07:54** | **Người 2** | Nhưng thôi cái đó anh để qua sau đi. Hiện tại trước mắt để cho nó đúng cái luồng cho nó nhanh trước. |
| **08:00** | **Người 1** | Ok, tạm thời lưu cái mã nó vào trong cái dữ liệu, trong body á. Để trong body luôn, tạo hai trường gì đó. Hôm bữa mới nói là cái metadata đó, thiết lập cho nó, để biết được trong code truyền cái mã này qua. Ví dụ chỗ Đạt lúc gửi cái dữ liệu này nè, truyền metadata này cái mã của mình, cái mã đối tác của mình. Đúng không? Để mình gửi đi. Thì mới biết được là trong code là mới lấy cái mã đang định danh trong mình nè. Ví dụ như cái site mình đang chạy mình set cho nó là mã 101, à lộn cái mã đối tác của mình là mã A101 đi. Thì mình gán cái mã A101 đó vào trong cái trường define cái đối tác, để lúc em nhận em mới biết được là ờ cái gói này là của 101, A101. |
| **09:00** | **Người 2** | Là gán nó vô đây đúng không? |
| **09:03** | **Người 1** | Chứ không phải là đi set cứng ở đâu đó. Đúng không? Rồi ví dụ như giờ muốn test thằng khác đi, lấy cái dịch vụ đó clone nó ra thành một cái thằng khác, sửa nó lại A102. Thì lúc đó hai cái dịch vụ nó chạy á, thì thằng 101 A101 á nó xử lý như thế nào? Đúng không? Ví dụ như mình chỉ bật cái luồng gửi đi, ví dụ A102 chỉ bật cái gói gửi đi thôi, đúng không? Thì lúc đó là bắt đầu mới xử lý cái dịch vụ lấy gói gửi, gửi đi. Rồi lúc em nhận được là nó sẽ có thông tin liên quan đến A102. |
| **09:47** | **Người 2** | Ừ. |
| **09:50** | **Người 1** | Đó, chuyện như vậy. Làm sao mà tí nữa một cái dịch vụ đi, một cái dịch vụ đi clone nó ra đó, cũng có thể clone nó ra sửa config nó lại. Thằng này đóng vai trò là A101, thằng kia đóng vai trò A102, thằng này đóng vai trò A103, thằng kia đóng vai trò A104. Bật nó ở chế độ gửi hết. Đúng không? Thì lúc đó là có một cái dịch vụ nó giữ vai trò gửi tự động đi. Đóng vai trò gửi tự động đi, thì lúc đó nó sẽ nhận được bốn cái gói tin. Em xử lý được bốn cái đó ok là coi như là cái luồng chuẩn á. |
| **10:30** | **Người 2** | Khoan từ từ... nói lại nói lại... Em clone thằng này ra... |
| **10:38** | **Người 1** | Không phải clone thằng này ra, mà là clone cái dịch vụ! Có nghĩa bây giờ ở đây bốn cái đối tác đúng không? Bốn đối tác: A1, A2, A3, A4 đi. Được chưa? A1, A2, A3, A4. Rồi, gói gửi: 101, 102, 103; thằng hai là 104, 105, 106 gì đó; thằng kia là 101, 105, 103 gì đó, nói chung là sáu lung tung gói. Thì nhận về cũng vậy luôn, y chang. Nhận sao gửi y chang vậy. Được chưa? Rồi bắt đầu là cái dịch vụ xử lý đúng không? Dịch vụ xử lý là hiện tại đang hai chiều: vừa nhận vừa gửi đúng không? Thì mình sẽ thiết lập một cái config gì đó để biết được là cái dịch vụ này đang đóng vai trò là gửi thôi, nó không có chạy luồng nhận nữa. Thì lúc đó là sẽ lấy cái dịch vụ đó copy thành bốn bản. Có một cái chỗ khai báo cái mã. |
| **11:32** | **Người 2** | Bốn bản, chỗ khai báo mã? |
| **11:35** | **Người 1** | Thì đúng rồi. Thì bây giờ ví dụ như đối với mình đi, mình gửi đi cho người ta thì mình phải định danh cho mình là cái mã trạm gì đúng không? Phải tự định danh mình là cái gì chứ? |
| **11:47** | **Người 2** | À em hiểu rồi, có nghĩa là mỗi một server này đang đóng vai trò là một đối tác? |
| **11:51** | **Người 1** | Đúng rồi! Người ta đối tác của mình thì mình là đối tác của người ta. |
| **11:54** | **Người 2** | Rồi tụi em sẽ tự khai port khai IP của từng cái ra rồi bắn qua đúng không? Là clone ra mấy chục cái DB nữa? |
| **12:00** | **Người 1** | Cần gì clone trong DB trời! Vẫn xài chung DB hết! Ý là clone các thằng dịch vụ chạy ra. Em sẽ có một cái chỗ em đi khai báo mấy cái mã trạm đó, mấy cái mã đối tác đó. Thì một thằng dịch vụ là sẽ đóng vai trò đối tác A1, một thằng dịch vụ đóng vai trò đối tác A2, một thằng dịch vụ đóng vai trò đối tác A3, một thằng đối tác A4. Nhưng mà chỉ bật cái hướng gửi đi thôi, không xử lý nhận về. Rồi một cái thằng dịch vụ thứ năm bật cái vai trò là nhận. Được chưa? Thì lúc đó là bốn thằng kia nó gửi, thằng còn lại nó sẽ nhận bốn thằng để nó xử lý. |
| **12:47** | **Người 2** | Rồi hiểu. |
| **12:49** | **Người 1** | Thì lúc đó nó sẽ parse đúng về nó lưu được bốn dữ liệu của bốn thằng là ok. |
| **12:55** | **Người 2** | Ok hiểu. |
| **12:56** | **Người 1** | Đó, hiện tại là đang 1-1 thôi đó. Nhưng mà thực tế là nó chạy đâu có 1-1 như vậy. Mình gửi nhiều thằng và mình nhận nhiều thằng mà. Đó, thì cái thằng hồi nãy đóng vai trò ví dụ A1, A2, A3, A4 đúng không? Thằng thứ năm đóng vai trò nhận nó sẽ nhận hết. Bắn cái gì vô xử lý hết. |
| **13:30** | **Người 2** | Rồi hiểu hiểu hiểu. |
| **13:33** | **Người 1** | Nên là phải test được bao nhiêu đó thì nó mới đúng, mới đúng cái hướng của mình. |
| **13:37** | **Người 2** | Anh Đạt clear không? |
| **13:39** | **Người 3 (Đạt)** | Cái này cũng hiểu. Thì service nhận của em là có thể clone ra nhiều đúng không? |
| **13:43** | **Người 2** | Service gửi của anh là clone ra nhiều. |
| **13:46** | **Người 1** | Gửi nhiều, nhận một thôi! Gửi nhiều, nhận một xử lý thôi. |
| **13:51** | **Người 2** | Đúng không, đây nè, ví dụ ở đây đang là 401 đúng không? Anh sẽ clone ra thành 402, 403, 404, 405. Clone ra từng đó cái xong rồi anh gửi đi. Anh làm mấy gói tin gửi rồi sẽ có một cái thằng đối tác thứ năm nó sẽ là... nó sẽ chỉ khai báo gói tin nhận về thôi. Tất cả các thằng còn lại, tất cả các thằng từ 1 tới 4 nó sẽ gửi về cái thằng thứ năm ở đây. |
| **14:09** | **Người 1** | Nhưng mà cái luồng nó phải chuẩn trước đã, thì lúc đó mới thêm... Mới thêm cái phần check nó vào để tình huống là mình bật cái cờ đó lên thì nó chỉ đóng vai trò nhận, bật cái cờ đó lên chỉ đóng vai trò gửi thôi. Còn nếu mà mình không có khai báo gì hết thì mặc định là vừa gửi vừa nhận. Đúng không? Ấy, thì vậy thôi. Mục tiêu là để mình dùng mình kiểm thử lại cái luồng của mình coi nó chạy đúng hay chưa. Làm sao khai báo... giờ muốn khai báo được nhiều thì giờ chỉ còn cách là mình đi clone nó ra thôi, tại vì nó cùng một luồng xử lý mà. Clone nó ra thành A, B, C, D... rồi cùng vô dữ liệu lấy ra xử lý. Ví dụ thằng A này xử lý gói 101, 102, 103 gửi đi; thằng B xử lý 101, 105, 106; C là 108, 109 gì đó... Xử lý như vậy xem nó có ảnh hưởng gì không. Thì nó đâu đó nó cũng sẽ test luôn trường hợp là khi mà cùng một thằng xử lý cùng ba đối tác đi, một mình ba đối tác. Thì giờ cơ bản nó chính là ba cái dịch vụ đang chạy đúng không? Thì mình phải tách vậy thôi chứ giờ cơ bản là chạy chung một lúc thì lúc ba thằng chạy cùng lúc vậy giờ tình huống: Ba thằng đều xử lý cái gói 101 thì sao? Ba thằng đều trỏ vào 101 để lấy thì sao, có ảnh hưởng không? Cái phần lấy dữ liệu nó cũng là vấn đề nữa. Giờ thằng đối tác một này đến giờ đó nó chạy, nó lấy dữ liệu 101 thì theo quy tắc là lấy truy vấn vô đúng không, lấy dữ liệu lên một cục. Thì trong quá trình đang lấy như vậy thằng partner thứ hai nó cũng tới giờ nó chạy cũng 101 luôn, nó lại truy vấn vào trong bảng đó lấy một cục nữa. Đúng không? Thằng 103 cũng vậy luôn, truy vấn vô trong đó lấy một cục. Thì ba cục đó có thể dữ liệu giống nhau có thể dữ liệu khác nhau. Tại sao? Tại vì phụ thuộc vào trong cái... cái gì? Cái điều kiện tìm kiếm. Đúng không? |
| **16:04** | **Người 2** | Đúng đúng đúng. |
| **16:06** | **Người 1** | Đó! |
| **16:08** | **Người 2** | Không, từ từ... Nếu mà nói như anh á thì nó hơi không được ấy nha, tại vì cả ba thằng nó đều join vào lấy... |
| **16:15** | **Người 1** | VPN vô máy 30 cái. Nó còn nó còn nhiều thứ để nó... |
| **16:20** | **Người 2** | Quá tải. |
| **16:21** | **Người 1** | ...nó tối ưu nữa. Cho nên là hiện tại là mình mới đem cái luồng... cái luồng căn bản thôi đó. Tạm là 1-1 chạy được thôi đó, nên là tranh thủ làm nó còn... |
| **16:32** | **Người 2** | Cái lúc mình lấy data lên có cho nó ghi xuống DB không? |
| **16:35** | **Người 1** | Không có, lấy ra xử lý thôi. Chỗ gửi hiện tại là chỉ có lấy ra xử lý và ghi log thôi, ghi log cái chỗ mà xử lý rõ ràng thôi, không lỗi á. Thành công là cũng ghi log. |
| **16:47** | **Người 1** | *(Hết phần thảo luận kỹ thuật — các thành viên chuẩn bị trả phòng họp)* |
