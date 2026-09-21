---
tier: A
read: full
source:
  - ../../../Plan/_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-1.m4a
  - ../../../Plan/_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-2.m4a
date: 2026-09-19
model: Gemini Native Audio Multimodal
status: verified
---

# Thảo luận ShareData — Bổ sung thông tin định danh vào HTTP Header luồng Gửi & Cơ chế nhận diện gói tin Inbound

**Ngôn ngữ:** Tiếng Việt  
**Thời lượng:** ~03:37 (Hợp nhất trọn vẹn từ 2 file ghi âm: File 1: 03:13, File 2: 00:23)  
**File nguồn:** `../../../Plan/_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-1.m4a`, `../../../Plan/_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-2.m4a`  
**Ngày họp:** 19/09/2026  
**Chủ đề:** Thống nhất phương án bổ sung thông tin định danh đối tác (`PartnerCode`) và ánh xạ (`Mapping`) vào HTTP Header khi gọi REST API ở luồng gửi (Outbound); phía nhận (Inbound) bóc tách Header để đối soát và tải toàn bộ danh mục Mapping lên RAM/cache để tra cứu nhanh; giữ nguyên Body HTTP là mảng payload thuần túy.

---

## 1. Tóm tắt nội dung toàn diện (Executive Summary)

### 1.1. Bối cảnh & Bài toán đặt ra (Problem Statement)
- Sau khi thống nhất loại bỏ lớp vỏ bọc phong bì PDU (`httpPayload`) ở luồng gửi để Body HTTP chỉ chứa mảng bản ghi dữ liệu thuần túy (Array of Objects), phát sinh vấn đề ở đầu nhận:
  - Phía nhận (Inbound WebAPI / Inbound Worker) khi nhận request bắn tới qua HTTP POST sẽ **không thể nhận biết được gói tin này là của đối tác nào (`PartnerCode`) và cần áp dụng quy tắc ánh xạ (`Mapping`) nào** nếu thông tin đó không nằm trong Body.
  - Cần một giải pháp truyền tải thông tin định danh vừa chuẩn mực RESTful, vừa không làm biến dạng cấu trúc dữ liệu Body đã chốt với đối tác.

### 1.2. Thống nhất giải pháp: Đưa thông tin định danh vào HTTP Header
- **Ở chiều gửi (Outbound REST Sender):**
  - Khi gửi qua HTTP API, bổ sung thông tin định danh (như `PartnerCode`, mã gói tin/mapping) vào **HTTP Header** của request.
  - Phần Body HTTP: Giữ nguyên vẹn mảng bản ghi dữ liệu nghiệp vụ chuẩn, không tự ý bọc thêm envelope hay trường phụ.
- **Ở chiều nhận (Inbound Controller / Service):**
  - Đọc trực tiếp các trường định danh từ HTTP Header (`Request.Headers`) để nhận diện đối tác và loại gói tin gửi đến.
  - Sau khi lấy được `PartnerCode` từ Header, tiến hành bóc tách và đối chiếu dữ liệu tương ứng.

### 1.3. Cơ chế tra cứu Mapping phía nhận (RAM / Cache Lookup)
- Do thông tin đối tác và cấu hình mapping có tính chất động (không fix cứng mã cố định trong code):
  - Phía nhận sẽ tải trước (load) toàn bộ danh mục Mapping lên bộ nhớ đệm (RAM/cache).
  - Khi có request gửi tới kèm Header định danh, hệ thống chỉ việc tìm kiếm (search/lookup) ngay trên RAM theo thông tin bắt được từ Header, giúp tốc độ bóc tách và xử lý đạt hiệu năng cao nhất.

---

## 2. Toàn văn nội dung đối thoại hợp nhất (Full Verbatim Transcript)

> **Quy ước định danh người nói:**
> - **Người 1 (Hiếu - Dev):** Lập trình viên phụ trách luồng bóc tách dữ liệu và Mapping của ShareData Worker.
> - **Người 2 (Anh Sơn - Tech Lead):** Trưởng nhóm kỹ thuật / Kiến trúc sư hướng dẫn giải pháp Header và quản lý Mapping trên RAM.

| Mốc thời gian | Người nói | Lời thoại chi tiết |
| :---: | :---: | :--- |
| **00:00** | **Người 1 (Hiếu)** | ...Cái chỗ mapping là em sẽ biết được là trong cái đống này, partner này đang làm mapping nào đúng không anh? |
| **00:07** | **Người 2 (Sơn)** | Nhưng mà em nhận cái đó làm gì? Bỏ cái mapping lên đó làm cái gì? |
| **00:11** | **Người 1 (Hiếu)** | Đúng rồi. Giờ làm sao biết được là thằng này nó mapping nào để nó bốc? |
| **00:16** | **Người 2 (Sơn)** | *(Suy nghĩ)* |
| **00:18** | **Người 1 (Hiếu)** | Làm sao mình biết được thằng này nó trúng cái mapping nào? |
| **00:21** | **Người 2 (Sơn)** | Quan trọng là bây giờ làm sao để mà lấy nó ra? |
| **00:24** | **Người 1 (Hiếu)** | Lấy nó ra làm sao? |
| **00:26** | **Người 2 (Sơn)** | Ví dụ gửi qua API... |
| **00:28** | **Người 1 (Hiếu)** | Ừ, gửi qua API... |
| **00:30** | **Người 2 (Sơn)** | ...gửi qua API, ví dụ như thằng này gửi qua hệ thống khác nè. |
| **00:33** | **Người 1 (Hiếu)** | Hệ thống khác á? Ừ, gửi qua hệ thống khác... |
| **00:35** | **Người 2 (Sơn)** | ...thì làm sao mà biết được thằng này nó đi chung mapping nào? |
| **00:37** | **Người 1 (Hiếu)** | Thì mới nói là quay lại bài toán trước đã nói rồi, phải nhận biết được thông qua cái gì? |
| **00:43** | **Người 2 (Sơn)** | Định danh thực. |
| **00:47** | **Người 1 (Hiếu)** | Thật ra hôm bữa mới đặt cái tình huống... thật mới đúng. |
| **00:53** | **Người 2 (Sơn)** | Có thể là header, hoặc là gắn một cái key vô trong request body. |
| **00:59** | **Người 1 (Hiếu)** | Phải có header. |
| **01:01** | **Người 2 (Sơn)** | ...là partner nào. |
| **01:07** | **Người 1 (Hiếu)** | Ừ của nó đó. Bữa có đặt trường hợp đó. Là nếu mà đúng chuẩn á, thằng này đang là thằng nào... |
| **01:13** | **Người 2 (Sơn)** | Đúng rồi, là thằng nào... |
| **01:16** | **Người 1 (Hiếu)** | ...thì phải dựa vô thông tin của header. |
| **01:19** | **Người 2 (Sơn)** | Còn không thì gửi vô trong... header... |
| **01:26** | **Người 1 (Hiếu)** | Thì phải lúc nó bắn... nó không có vào trong phần body thì làm sao nó biết được là thằng nào? |
| **01:32** | **Người 2 (Sơn)** | Hiện tại bây giờ vào cái body này còn chưa biết nó là thằng nào... |
| **01:39** | **Người 1 (Hiếu)** | Bây giờ thống nhất là gửi qua cái gì? |
| **01:45** | **Người 2 (Sơn)** | Gửi qua HTTP thì gửi kèm theo header... gửi kèm theo cái trường... |
| **01:52** | **Người 1 (Hiếu)** | Nếu mà người ta chưa có đăng nhập là xác thực... |
| **01:55** | **Người 2 (Sơn)** | Chưa có thì... |
| **01:58** | **Người 1 (Hiếu)** | ...thì mình sẽ gửi qua... thì gửi qua header. |
| **02:00** | **Người 2 (Sơn)** | Gửi qua header cái này là do... bên phía bên kia đúng không? |
| **02:04** | **Người 1 (Hiếu)** | Đúng rồi, đúng rồi. Ví dụ như thằng... bên nhận hay bên gửi theo cái chiều của nó... ví dụ như... header có cái gì đó. |
| **02:12** | **Người 2 (Sơn)** | Đến đó thì parse nó ra. |
| **02:14** | **Người 1 (Hiếu)** | Là có rồi đúng không, đâu cần thêm cái gì nữa đâu đúng không? |
| **02:17** | **Người 2 (Sơn)** | Thì giờ thêm cái header. |
| **02:18** | **Người 1 (Hiếu)** | Thêm header cho thằng Hiếu nó bắt cái đó để nó xử lý. |
| **02:22** | **Người 2 (Sơn)** | Đúng rồi, nhận được rồi thì bắt vô. |
| **02:25** | **Người 1 (Hiếu)** | Còn cái em cần ví dụ cái gì đưa em không cần nữa... |
| **02:28** | **Người 2 (Sơn)** | Còn cái anh lo là nó không có fix cố định đâu. |
| **02:32** | **Người 1 (Hiếu)** | Cái này là anh không fix cố định này, nhưng mà bên em đang làm là khi mà một cái partner... |
| **02:39** | **Người 2 (Sơn)** | Nó bắt được code partner rồi... |
| **02:40** | **Người 1 (Hiếu)** | ...là nó bắt được code partner rồi, nhưng mà quan trọng là bây giờ... |
| **02:45** | **Người 2 (Sơn)** | ...nó cũng không cố định nữa. |
| **02:50** | **Người 1 (Hiếu)** | Thế hôm qua hỏi chat GPT kêu làm cái gì? Lấy hết mapping lên trên RAM rồi search. |
| **02:55** | **Người 2 (Sơn)** | Nó cũng là một cách mà, nó cũng là một cách. |
| **03:00** | **Người 1 (Hiếu)** | Coi cho nó thoát cái này nè. |
| **03:04** | **Người 2 (Sơn)** | Nói chung là chuẩn á, sẽ dựa theo thông tin... |
| **03:06** | **Người 1 (Hiếu)** | Dựa theo thông tin thì quá chuẩn rồi. |
| **03:08** | **Người 2 (Sơn)** | Thì cứ gửi qua header cũng được. |
| **03:13** | **Người 1 (Hiếu)** | *(Chuyển tiếp mạch đối thoại sang file ghi âm 2)* |
| **03:14** | **Người 1 (Hiếu)** | Giờ phần header sẽ gửi những thông tin này đúng không? Ý là header của API... |
| **03:18** | **Người 2 (Sơn)** | Ừ biết, cho nó biết. |
| **03:20** | **Người 1 (Hiếu)** | Sợ anh lộn với lại cái... |
| **03:22** | **Người 2 (Sơn)** | Hỏi rồi, ai đâu biết. |
| **03:25** | **Người 1 (Hiếu)** | Thì thí dụ cái nào thì để để vô API. |
| **03:27** | **Người 2 (Sơn)** | Ừ vậy đi. Còn body thì sao? |
| **03:29** | **Người 1 (Hiếu)** | Body thì gửi giống như... payload bình thường. |
| **03:37** | **Người 2 (Sơn)** | *(Kết thúc cuộc trao đổi)* |
