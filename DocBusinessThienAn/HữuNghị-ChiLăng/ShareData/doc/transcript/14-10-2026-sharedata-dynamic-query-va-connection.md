---
tier: A
read: full
source:
  - ../_source/14-10-2026.m4a (49:33)
date: 14-10-2026
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData — Kiến trúc Truy vấn động (Dynamic Query), Tham số hóa (@LastKey / Composite Key), Tầng tích hợp kết nối độc lập (Dictionary Config), Quản lý gói tin Full vs Nối đuôi và Định dạng dữ liệu (JSON, File, Byte Array, Protobuf/ASN.1)
participants:
  - Anh Sơn (Tech Lead)
  - Hiếu (Dev)
  - Đạt (Dev)
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ ShareData — Kiến trúc Truy Vấn Động, Cơ Chế Quét Nối Đuôi & Tách Tầng Kết Nối
### (Nguồn từ tệp ghi âm: 14-10-2026.m4a)

- **Ngày ghi nhận:** 14/10/2026
- **Thời lượng:** 49 phút 33 giây (toàn bộ tệp ghi âm)
- **Người tham gia:** Anh Sơn (Tech Lead), Hiếu (Dev), Đạt (Dev)
- **Chủ đề chính:**
  1. **Truy vấn động (Dynamic Query) cho gói tin:** Chuyển câu truy vấn trích xuất dữ liệu của gói tin sang lưu trữ DB/cấu hình thay vì hardcode trong Worker C#; hỗ trợ SuperAdmin chỉnh sửa có SQL Validator; cung cấp câu query mẫu mặc định (Template Query) khi tạo mới cấu hình đối tác.
  2. **Tham số hóa truy vấn & Cơ chế quét nối đuôi (`@LastKey`):** Chuẩn hóa các tham số lọc (`SugarParameter`) như `@FromDate`, `@ToDate`, `@LastKey`. Làm rõ `@LastKey` có thể là mốc thời gian hoặc khóa tổng hợp (Composite Key gồm Thời gian + Mã làn/Mã trạm/Vị trí) để đảm bảo không bị sót hoặc trùng lặp dữ liệu.
  3. **Tách tầng kết nối (`Connection Layer`) & Cấu hình linh hoạt qua `Dictionary`:** Tầng kết nối nhận 1 Object chung chứa Data chuẩn hóa (JSON), thông tin bảo mật và cấu hình kết nối (`Dictionary<string, string>`). Bóc tách riêng logic xử lý giao thức (FTP, API, Kafka, File) giúp bảo trì độc lập, dùng chung được cho cả 2 chiều gửi và nhận.
  4. **Định dạng dữ liệu, Bộ đệm lưu trữ (Buffer/File Storage) & Dự phòng mã hóa:** Dữ liệu gửi đi được lưu đệm (File/DB) để phục vụ gửi lại và kiểm toán. Thảo luận về xuất định dạng CSV, mảng byte (Byte Array), Protobuf, ASN.1 (tham khảo) và kiến trúc dự phòng mở rộng thuật toán mã hóa (Public/Private Key) trong tương lai (hiện tại chưa áp dụng).
  5. **Chính sách gói tin từ 101 đến 110 (Full vs Nối đuôi):** Phân loại gói tin gửi toàn bộ (Full/Snapshot) và gửi nối đuôi (Incremental). Gói gửi full lấy toàn bộ theo câu query, nếu cần giới hạn số lượng thì cấu hình `TOP` trực tiếp trong câu SQL. Các gói tin chưa có dữ liệu thực tế (như 110) tạm thời để trạng thái không sử dụng hoặc cấu hình SQL giả lập để kiểm thử luồng end-to-end.
  6. **Cơ chế dọn dẹp định kỳ (Auto-cleanup / Retention Policy):** Đặt vấn đề tự động xóa file tạm và log cũ sau 30-60 ngày theo cấu trúc thư mục ngày tháng (`yyyy/MM/dd`); xếp ưu tiên sau khi hoàn thiện luồng gửi nhận chính.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

> **Cập nhật yêu cầu sau cuộc họp — người dùng chốt ngày 14/10/2026:** gói **105 mặc định Snapshot** theo [mapping 105 `All/update`](../02-mapping-goi-tin-101-111.md); setting gói cho chọn Snapshot/Incremental và Worker dùng setting đã lưu. Gói **110 được tạo/cấu hình bình thường**, bổ sung bảng mô phỏng và dữ liệu giả. Viết sẵn code Kafka producer, CSV/FTP, mã hóa, cleanup để dùng khi cấu hình áp dụng; phải chạy Playwright sau sửa FE. Các quyết định bổ sung này ưu tiên khi đối chiếu yêu cầu hiện hành; lời thoại và raw notes lịch sử bên dưới được giữ nguyên, không phải bằng chứng code đã triển khai.

### 1.1. Kiến trúc Truy vấn động (Dynamic Query) & Gói tin mẫu (Template Packet)
- **Thực trạng cũ:** Câu lệnh trích xuất dữ liệu của 11 gói tin (101 - 110) bị hardcode cứng trong mã nguồn C# của Service Worker. Mỗi khi đối tác yêu cầu thêm cột, đổi bảng hoặc đổi điều kiện lọc, lập trình viên phải sửa code và build lại Worker.
- **Quyết định kỹ thuật:**
  - **Chuyển sang Truy vấn động (Dynamic Query):** Lưu trữ các câu lệnh SQL trích xuất dữ liệu trực tiếp trong CSDL (lưu chuỗi câu truy vấn dạng template theo gói tin và cấu hình ánh xạ theo đối tác; trong băng ghi âm cuộc họp hoàn toàn **KHÔNG** nhắc đến tên bảng `ShareDataPacketExtractSql`, các bên chỉ chốt lưu trong CSDL/bảng chung thay vì lưu file hay tạo bảng riêng).
  - **Cơ chế Gói tin mẫu (Template / Default Query):** Khi tạo mới cấu hình cho một đối tác, hệ thống tự động tải (load) câu SQL mẫu chuẩn mực của gói tin đó (ví dụ chọn gói 101 sẽ tự nạp SQL trích xuất trạng thái đoạn đường mặc định). Tránh để màn hình trống trơn khiến người dùng phải tự viết lại từ đầu hoặc copy thủ công từ nơi khác.
  - **Giao diện cấu hình trực quan & Thẩm định:** Giao diện cho phép người dùng (phân quyền `SuperAdmin`) xem và chỉnh sửa câu lệnh SQL. Hạn chế dùng `textarea` đơn thuần gây xấu và khó nhìn; ưu tiên tích hợp trình soạn thảo có highlight cú pháp hoặc Query Builder trực quan. Phía Backend bắt buộc có bộ kiểm tra an toàn (Validator chỉ cho phép `SELECT`, cấm DDL/DML) và ghi Audit Log đầy đủ.
  - **Ưu tiên Raw SQL lưu DB hơn Store Procedure:** Nếu viết Store Procedure trong DB thì khi cần sửa logic trên giao diện Web rất khó cập nhật; trong khi lưu chuỗi SQL trong DB thì WebAPI dễ dàng nạp, hiển thị và cho phép chỉnh sửa trực tiếp.

### 1.2. Cơ chế Tham số hóa & Quét nối đuôi (`@LastKey` / Composite Key)
- **Tham số hóa câu lệnh SQL:** Sử dụng cơ chế tham số (`SugarParameter` của SqlSugar) để truyền các biến động vào câu SQL nhằm chống lỗi SQL Injection và tối ưu execution plan:
  - `@FromDate`, `@ToDate`: Dành cho các gói quét theo khoảng thời gian cố định.
  - `@LastKey`: Dành cho các gói gửi nối đuôi (tăng dần).
- **Bản chất của `@LastKey`:**
  - Với các gói tin đơn giản, `@LastKey` là mốc thời gian cập nhật/tạo mới cuối cùng (`CreateTime` / `DetectTime`).
  - Với các gói tin phát sinh theo tần suất cao và dễ bị trùng mốc thời gian (như giao dịch thu phí, xe qua trạm, đếm xe VDS), `@LastKey` có thể là **Khóa tổng hợp (Composite Key)** kết hợp giữa:
    - Mốc thời gian (`DetectTime` / `TranTime`).
    - Mã trạm / Mã làn (`StationId`, `LaneId`).
    - Vị trí lý trình (`Km`, `Location`).
  - Sau mỗi chu kỳ gửi thành công, Worker ghi nhận giá trị `@LastKey` lớn nhất vào bảng lưu trạng thái. Lần quét tiếp theo sẽ lấy `@LastKey` này truyền vào điều kiện `WHERE` để chỉ lấy các bản ghi phát sinh mới hơn.
  - **Điều kiện mở rộng (Window Constraint):** Có thể cấu hình thêm điều kiện chặn cửa sổ (ví dụ: dù lấy theo `@LastKey` nhưng không được lấy dữ liệu quá 7 ngày hoặc 30 ngày trước để tránh quét ngược dữ liệu rác quá cũ).

### 1.3. Bóc tách Tầng xử lý dữ liệu và Tầng kết nối (`Connection Handler`)
- **Kiến trúc phân tầng 3 bước độc lập:**
  ```mermaid
  flowchart LR
    Step1["1. Trích xuất dữ liệu<br/>(Dynamic SQL)"] --> Step2["2. Xử lý & Ánh xạ<br/>(Mapping sang JSON)"]
    Step2 --> Step3["3. Tầng kết nối<br/>(Connection Handler)"]
    Step3 --> OutAPI["HTTP API"]
    Step3 --> OutFTP["FTP / File"]
    Step3 --> OutKafka["Kafka (byte[])"]
  ```
  1. **Bước 1: Trích xuất dữ liệu (Data Extraction):** Chạy câu Dynamic Query lấy dữ liệu thô từ CSDL.
  2. **Bước 2: Xử lý gói tin & Ánh xạ (Packet Processor / Mapping):** Ánh xạ dữ liệu thô theo bảng ánh xạ của đối tác, chuẩn hóa đầu ra thống nhất là **Object chung chứa dữ liệu chuẩn (JSON)**.
  3. **Bước 3: Tầng kết nối (`Connection Handler`):** Nhận Object chung và thực hiện gửi đi theo đúng giao thức đã cấu hình.
- **Tính đối xứng cho chiều nhận (Inbound):** Luồng nhận dữ liệu từ đối tác vào hệ thống nội bộ cũng tái sử dụng chung module kết nối theo chiều ngược lại (Nhận dữ liệu thô -> Tầng kết nối tiếp nhận -> Parse ra Object chung -> Lưu trữ nội bộ).

### 1.4. Quản lý cấu hình kết nối đối tác bằng `Dictionary<string, string>`
- **Thực trạng:** Mỗi kênh kết nối có tập thuộc tính hoàn toàn khác nhau:
  - **FTP:** IP, Port, Username, Password, RemotePath.
  - **API:** BaseUrl, Port, Timeout, ApiKey / Bearer Token.
  - **Kafka:** BootstrapServers, Topic, ClientId, SaslConfig.
- **Quyết định kỹ thuật:**
  - Thay vì thêm hàng chục cột vào bảng Entity hoặc tạo bảng con phức tạp, cấu hình kết nối của đối tác được lưu trữ dưới dạng **`Dictionary<string, string>`** (hoặc chuỗi JSON Key-Value) bên trong trường `ConnectionConfig` của bảng đối tác (`ShareDataPartner`).
  - Khi cần thêm một thuộc tính kết nối mới, chỉ cần thêm Key vào Dictionary mà không làm xáo trộn cấu trúc bảng CSDL hay Entity mã nguồn.
  - Phía Backend khi gửi chỉ cần gọi `config.GetValueOrDefault("Ip")`, `config.GetValueOrDefault("ApiKey")` kèm kiểm tra tồn tại của Key.

### 1.5. Định dạng dữ liệu, Bộ đệm lưu trữ (File/Buffer) & Mã hóa dự phòng
- **Bộ đệm lưu trữ (Buffer / Archive Storage):**
  - Trước khi gửi đi qua kênh mạng, toàn bộ dữ liệu phải được lưu tạm thành file (hoặc bản ghi lưu trữ) theo đúng định dạng sẽ gửi (JSON / CSV).
  - Mục đích: Có bằng chứng đối soát, kiểm toán, và sẵn sàng gửi lại (Retry/Re-send) khi đối tác gặp sự cố sập mạng mà không cần truy vấn quét lại CSDL gốc.
- **Định dạng dữ liệu xuất:**
  - Hỗ trợ xuất file CSV (phân cách bằng dấu phẩy theo thứ tự cột chuẩn).
  - Hỗ trợ chuyển đổi sang mảng byte (`byte[]`) phục vụ đẩy vào Kafka hoặc socket.
- **Mã hóa và định dạng nâng cao (Protobuf / ASN.1):**
  - Đề cập đến nhu cầu mã hóa bất đối xứng (Public/Private Key) và các chuẩn giao thức ITS chuyên dụng (như ASN.1 trong thu phí VETC, Protobuf).
  - **Kết luận tại cuộc họp:** chưa áp dụng mã hóa ngay, chừa điểm mở rộng giữa xử lý dữ liệu và kết nối.
  - **Yêu cầu bổ sung đã chốt 14/10/2026:** viết sẵn cấu trúc code/provider encode–decode để dùng khi cấu hình áp dụng; việc chưa bật mã hóa không đồng nghĩa được bỏ phần code chuẩn bị. Không coi chỉ có TODO/interface là đã hoàn tất yêu cầu mới.

### 1.6. Chính sách gói tin từ 101 - 110 & Kiểm thử giả lập (Simulation)
- **Chế độ mặc định theo [mapping 101–111](../02-mapping-goi-tin-101-111.md)** (ghi chú trong ngoặc ở tiêu đề từng gói):

  | Gói | Ghi chú trong mapping | Mặc định |
  |---|---|---|
  | 101 | lấy all / DL được cập nhật | Gửi toàn bộ (Snapshot) |
  | 102 | lấy all / DL được cập nhật → nats | Gửi toàn bộ (Snapshot) |
  | 103 | lấy mới nhất từ thời điểm trước đó >= key | Gửi nối đuôi (Incremental) |
  | 104 | lấy mới nhất từ thời điểm trước đó >= key | Gửi nối đuôi (Incremental) |
  | 105 | All / update | Gửi toàn bộ (Snapshot) |
  | 106 | mới nhất từ >= key | Gửi nối đuôi (Incremental) |
  | 107 | lấy mới nhất từ thời điểm trước đó >= key; giá trị cũ sẽ update trạng thái | Gửi nối đuôi (Incremental) |
  | 108 | lấy all / DL được cập nhật → nats | Gửi toàn bộ (Snapshot) |
  | 109 | lấy mới nhất từ thời điểm trước đó >= key | Gửi nối đuôi (Incremental) |
  | 110 | all / opConfig → xóa hết trừ groupCode thuộc WP | Gửi toàn bộ (Snapshot) |
  | 111 | skip | Không làm |

- **Gói gửi toàn bộ:** không ép TOP cứng; muốn giới hạn thì thêm vào câu SQL đã cấu hình.
- **Setting chế độ đồng bộ:** cho chọn Snapshot/Incremental trên cấu hình gói và Worker dùng setting đã lưu; danh sách mặc định không phải policy bất biến theo mã gói.
- **Gói 110:** cuộc họp ban đầu đề xuất tạm khóa hoặc query giả lập khi chưa có dữ liệu. **Yêu cầu bổ sung đã chốt 14/10/2026** là cho tạo/cấu hình bình thường, triển khai bảng mô phỏng và dữ liệu giả để test cả luồng trích xuất, ánh xạ và gửi. Không tiếp tục dùng đề xuất tạm khóa như chính sách hiện hành.

### 1.7. Cơ chế dọn dẹp dữ liệu và file log định kỳ (Retention Policy)
- Các file log gửi nhận và file dữ liệu tạm cần được tổ chức lưu trữ theo cấu trúc thư mục ngày tháng (`yyyy/MM/dd`).
- Sau này sẽ bổ sung một tác vụ chạy định kỳ (Background Job) để tự động xóa các file tạm và bản ghi log cũ hơn 30 ngày (hoặc theo cấu hình hệ thống).
- Ưu tiên tại cuộc họp: hoàn thành luồng kết nối và truy vấn động trước khi áp dụng dọn log.
- **Yêu cầu bổ sung đã chốt 14/10/2026:** viết sẵn job cleanup theo cấu hình Enable/RetentionDays/phạm vi dữ liệu; chưa bật sử dụng cho đến khi áp dụng cấu hình. Theo dõi riêng code, kiểm thử và trạng thái kích hoạt.

---

## 2. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

### Đoạn 1 (00:00 - 12:30): Bàn về Truy Vấn Động (Dynamic Query), SqlSugar & Bóc Tách Tầng Kết Nối
- **[00:00 - 01:30]**
  - **Anh Sơn (Tech Lead):** "Họp đi... Giờ em vô máy này nè..."
  - **Đạt (Dev):** "Bữa cái đó chạy đúng rồi..."
  - **Anh Sơn (Tech Lead):** "Được rồi, mở cái đó lên... Dễ nhất trong SQL Server là nó sẽ có một cái... Cái code nó chạy á, bên câu truy vấn luôn á, nó sẽ có một cái hàm để mình lắp vào. Mình đưa cái câu truy vấn đó vào là nó gen ra cho mình thôi, đúng câu truy vấn đó là xong."
  - **Đạt (Dev):** "Dạ dạ."
  - **Anh Sơn (Tech Lead):** "Ở đây nó có mấy cái hàm hỗ trợ cho mình... Để coi coi nó gen ra cái code đó, xem nó có đúng với cái câu truy vấn mình không..."
- **[01:30 - 03:00]**
  - **Hiếu (Dev):** "Mấy cái VideoWall hôm bữa em hỏi anh Sơn, anh Sơn bảo ghi tên tường màn hình..."
  - **Anh Sơn (Tech Lead):** "Này ghi từng cái nè... Lỗi là anh khai báo..."
  - **Hiếu (Dev):** "Em khai báo vô cái system của nó..."
  - **Anh Sơn (Tech Lead):** "Mà thứ hai SQL động em bây giờ... Em sẽ build cái UI cho người ta nhập đúng không?"
  - **Hiếu (Dev):** "Dạ."
  - **Anh Sơn (Tech Lead):** "Thì làm cái khung để người ta nhập thôi, nhập y chang như cái code thôi."
  - **Hiếu (Dev):** "Nhưng mà sẽ lưu dạng chuỗi vô một cái table hoặc một cái file..."
- **[03:00 - 05:00]**
  - **Anh Sơn (Tech Lead):** "Một là lưu vô bảng nếu mà nó nặng... Nếu mà nó không nặng... Thôi không thì lưu file cũng được. Nhưng mà file thì nó có những cái nè: ví dụ như không truy cập được lên server web... Hoặc là ví dụ như vô thẳng cái thư mục SQL... Có mấy công cụ quản trị nó hay dùng file... Nó vô file nó coi mấy cái file đó, xong rồi nếu mà bị gì thì nó vô file sửa. Hoặc là nó đem cái file đó ra ngoài luôn là xong."
  - **Hiếu (Dev):** "Là xong rồi cái luồng xử lý cứ đọc vô cái file đó rồi xử lý đúng không?"
  - **Anh Sơn (Tech Lead):** "Ừ, đọc file... Nhưng mà em nghĩ chắc lưu DB ok hơn."
  - **Hiếu (Dev):** "Lưu vô DB..."
  - **Anh Sơn (Tech Lead):** "Ủa hôm bữa là lưu dưới dạng file hay SQL đúng không?"
  - **Hiếu (Dev):** "Lấy dữ liệu từ câu SQL..."
  - **Anh Sơn (Tech Lead):** "Trước đây mình hardcode câu truy vấn đó nhớ không? Ban đầu là mình hardcode đó."
- **[05:00 - 07:00]**
  - **Đạt (Dev):** "Em tưởng bữa nay là anh đang muốn viết SP để lấy lên?"
  - **Anh Sơn (Tech Lead):** "Không, bây giờ mới làm nè. Bây giờ sẽ build ra UI động... Vấn đề là bữa cái code đó, cái lưu dữ liệu bên đó là nó sẽ tự động lấy lên. Ví dụ cái lấy lên của bên Đạt đúng không? Người ta sửa câu SQL đó người ta lấy thêm trường nữa mà mình đâu có lưu đâu mà khai báo."
  - **Hiếu (Dev):** "Thì quan trọng bây giờ lấy lên đúng không? Phải cập nhật... Đâu cần SQL riêng đâu... Ví dụ như hôm bữa em nói rồi đó: ví dụ như bên API nó cần cái câu gì đó, bây giờ nếu mà làm câu SQL cho từng đối tác theo từng gói tin... Ví dụ như chiều lấy đi thì nó có một câu SQL, còn chiều gửi thì nó có một câu SQL."
  - **Anh Sơn (Tech Lead):** "Ok em. Để xử lý... Thì cái đầu ra tương tự vậy, hôm bữa là có code rồi, hãy tận dụng cái hàm này của nó cho nó dễ."
- **[07:00 - 09:30]**
  - **Anh Sơn (Tech Lead):** "Nếu mà thuần về SqlSugar nha, còn nếu mà nó có link thêm hàm mà em tự viết bằng C# á là chịu á, nó không gen được đâu. Ví dụ dưới này đang query cái gì đó xong rồi xuống dưới mình đi mình xử lý `dict.ContainsKey` cái gì đó dính vào thì thua đó, nó không xử lý được đâu. Nó chỉ query thuần thôi. Còn nếu mà phức tạp quá, đóng hàm rồi nhờ chat nó hỗ trợ cho... Sau này em build cái UI... Em nhớ là có các điều kiện nha. Ví dụ như from to thì nghĩ là em để nó là cái SQL parameter đó... Parameter của SqlSugar á."
  - **Đạt (Dev):** "Nhưng mà câu SQL là để from to để lấy dữ liệu từ lúc nào đến lúc nào đấy là để trong câu SQL luôn hả?"
  - **Anh Sơn (Tech Lead):** "Đúng rồi. Nhưng mình phải truyền parameter vào... Nghĩa là ví dụ như chỗ đó mình chừa chỗ cho nó."
  - **Hiếu (Dev):** "Mình dùng bí danh."
  - **Đạt (Dev):** "Dùng Store thì trong Store nó có cái logic y chang."
  - **Anh Sơn (Tech Lead):** "Store cũng được, SQL cũng được, miễn sao chỗ nào dễ quản lý, dễ bảo trợ, dễ xử lý và dễ cập nhật. Em viết dưới Store em sửa trên giao diện được không? Em sửa được nhưng mà..."
  - **Đạt (Dev):** "À không không không..."
  - **Anh Sơn (Tech Lead):** "Nói chung là em em gõ mất... Em viết Store em sửa trên giao diện đâu có sửa Store được liền."
  - **Đạt (Dev):** "Tại vì em đang theo cái ý là bên cái chỗ quản lý dữ liệu á thì nó chỉ thuần lấy dữ liệu thôi. Còn ví dụ như lấy như thế nào như thế nào á thì qua ánh xạ để để làm. Nhưng mà nếu làm như vậy thì cái lúc lấy lên nó bị động quá."
- **[09:30 - 12:30]**
  - **Anh Sơn (Tech Lead):** "Thì tổng hợp luôn cái phần này thiết kế làm sao để cho người ta thuận tiện nhất. Tại vì chỗ này nó sẽ có truy vấn, và truy vấn ra đang theo gói tin và đối tác. Thì nhiều tình huống là nếu mà mình làm mình thiết lập kỹ về một đối tác á, thì khi mà tạo đối tác mới hoặc là phải đi copy tay câu SQL của đối tác kia. Có cái nào nó mặc định không? Phải copy xuống. Có một cái cách nào đó thuận tiện tí: ví dụ như chọn gói 101 đi thì nó sẽ ra cái câu truy vấn mẫu."
  - **Hiếu (Dev):** "Hiện lên mặc định theo gói tin đó."
  - **Anh Sơn (Tech Lead):** "Nó sẽ ra câu truy vấn mẫu mình sẽ lưu nó ở một chỗ nào đó. Cái gói tin mẫu, dạng template á, lưu mình giữ lại. Để khi mà chọn một đối tác nó sẽ ra cấu trúc mẫu trước, rồi có thay đổi gì đó người ta sửa thêm. Chứ không phải là bụp một cái ra trống trơn hết, lại phải đi copy bằng cách khác lại mất thời gian."
  - **Hiếu (Dev):** "Đó là lấy truy vấn... Lấy và lưu đúng không? Chuyển qua truy vấn cấu trúc quản lý. Thứ hai nữa là cấu hình kết nối: mục tiêu là thứ nhất sẽ có một cái phần nó riêng lẻ á, mặc dù nó không tách ra thành một dịch vụ thôi nhưng mà ít nhất nó sẽ riêng lẻ một tí. Mục tiêu là để sau này cần thay đổi cấu hình kết nối gì đó thì em làm trong phạm vi code thôi chứ không sửa lại hết một lượt."
  - **Anh Sơn (Tech Lead):** "Hình dung là nó sẽ tách biệt ra: cái phần xử lý gói tin, xử lý gói tin đó nó ra một cái gì đó, một object chung. Rồi sau đó thằng đó mới đi nộp đi vào trong các cái phần... cái phần kết nối."

---

### Đoạn 2 (12:30 - 25:00): Cấu Hình Kết Nối Đối Tác, Dùng Dictionary Key-Value & Độc Lập Giao Thức
- **[12:30 - 15:30]**
  - **Anh Sơn (Tech Lead):** "Thì trong cái phần kết nối này sẽ nhận cái object chung đó kèm theo... Trong chung này nó sẽ có hai phần trong nè: thứ nhất là phần liên quan đến data gửi; thứ hai nữa là cái thông tin... thông tin người nhận... Đó là connection đó. Thì tùy theo connection khác nhau nó sẽ kết nối khác nhau. Và connection này thường nó sẽ nằm ở trong cái phần cấu hình á. Tại vì tùy theo kết nối, có kết nối là sẽ có username, password, IP, port; nhưng mà có một số cái là username, password và API key. API key hay gì đó không biết được, hoặc là một cái giao thức gì đó..."
  - **Anh Sơn (Tech Lead):** "Nên nó sẽ dynamic đúng không? Thì thường dynamic chỗ này mình có thể dùng một cái Dictionary gì đó... Hoặc một cái thằng gì đó để quản lý... Động. Còn cái data gửi này thì cố gắng chuyển nó về một cái cấu trúc chung, chung của mình á... Chung của thằng bên này nè. Để khi mà thằng bên này nó nhận cái object đó thì nó không cần care nữa là phía trước nội dung trong trỏng là cái gì, nó cứ thế nó phang qua luôn."
  - **Anh Sơn (Tech Lead):** "Nói chung là tổng quát, để khi mà vô trong này nó là một cái object tổng quát... Tổng quát một tí. Thì trong này lúc đó nó xử lý á, ví dụ như nó sẽ dùng switch case đi hoặc một cách nào đó không biết, switch case đi ví dụ như kết nối FTP thì nó sẽ xử lý cái data này như thế nào đó. Ví dụ như FTP nó cần gửi theo dạng CSV đi, cần chuyển thành dạng CSV. Mỗi một dữ liệu cách nhau bởi dấu phẩy theo cấu trúc gì đó... Lúc đó mình sẽ lấy cái data đó mình cứ thế mình phang cho nó đúng theo cấu trúc. Rồi tương ứng cái thằng liên quan đến thông tin kết nối, ví dụ FTP nó sẽ có IP, port, username, password thì móc trong thằng này ra... Username, password, IP, port."
- **[15:30 - 18:30]**
  - **Hiếu (Dev):** "Thì trên UI vô chỗ ShareData..."
  - **Đạt (Dev):** "Không, cấu hình... Cái cây đang nói là nằm ở đây đúng không? Cây dữ liệu nằm ở đây... Cấu hình đối tác nó sẽ nằm bên này."
  - **Hiếu (Dev):** "Sẽ có một cái drop down chọn xuống để chọn cấu hình đối tác thì có thể cái này thông tin nè, thông tin này để ở bên này, thông tin đối tác này tạo một cái tab, tốt nhất là làm cái tab thông tin kết nối... Một bên là thông tin đối tác, một bên thông tin kết nối, chia làm hai. Tại vì thông tin kết nối nó sẽ có chứa username, port các kiểu gì đó."
  - **Đạt (Dev):** "Nhưng mà có cái nguồn nào để tham khảo là sẽ có bao nhiêu cái đầu kết nối không? SSE thì cần gì, FTP thì cần gì..."
  - **Anh Sơn (Tech Lead):** "Chừa slot thôi, không cần nhất thiết... Có nghĩa là em làm cái luồng trước thôi. Ví dụ như cái chỗ này em thêm hai ba field gì đó thì nó không có gì hết, không phải sửa lại. Về cơ bản ví dụ như em quản lý qua cái Dictionary thì giờ 100 field nữa em add vô Dictionary 100 field..."
  - **Đạt (Dev):** "Nó cứ móc Dictionary ra..."
  - **Anh Sơn (Tech Lead):** "Móc ra đâu có ảnh hưởng gì đâu. Ví dụ như chỗ này mà em không đặt Dictionary đi, em tạo object đi, thì mỗi một lần em ấy em phải tạo thêm một cái trường để em hứng đúng không? Còn nếu em tạo một cái Dictionary tổng quát thì chỉ cần truyền key - value thì em đâu có cần sửa gì đâu."
- **[18:30 - 21:00]**
  - **Đạt (Dev):** "Thì vô JSON tổng quát..."
  - **Anh Sơn (Tech Lead):** "Đúng rồi, sao cho nó tổng quát một tí... Tổng quát để tình huống là có thay đổi gì đó, bên mình trước hay dùng là hay sử dụng Dictionary... Cứ bao gồm là..."
  - **Hiếu (Dev):** "Cái Dictionary anh nói là trước đó đang quản lý là đang lưu nhưng Hiếu nói là lưu ở System á."
  - **Anh Sơn (Tech Lead):** "Hệ thống... Cấu hình... Ví dụ như X, Y, Z... Ai vô cũng có IP..."
  - **Anh Sơn (Tech Lead):** "Đó, ví dụ như ở trong có port... Tất cả đều có IP, port đi, thì chắc chắn có nè. Xong rồi ví dụ như có một thằng kết nối ví dụ như thằng thứ hai đi, nhiều khi nó phát sinh thêm một cái là API key... Thì lúc đó trong này sẽ có thêm API key. Rồi nhiều khi cái liên quan đến... Nó lại phát sinh ra... Thì khi mà qua bên này nó xử lý, ví dụ như FTP, mình đã biết là FTP nó sẽ có gì rồi đúng không? Còn IP thì mình sẽ lấy ra thôi: `connection.GetValue("Ip")`... Nên anh thêm cái Dictionary này thường hiện tại là mình có luôn rồi đúng không? Kiểu có, kiểu tham khảo được..."
- **[21:00 - 23:30]**
  - **Anh Sơn (Tech Lead):** "Nó có trong cái VMS Connection á. Từng bảng rồi đó. Ví dụ như FTP..."
  - **Anh Sơn (Tech Lead):** "Đó, ví dụ như hồi nãy anh nói nè, cái kết nối thì nó ở trên rồi nè. Mình không biết nó sẽ có thêm cái thông tin gì liên quan đến cái connection type... Data đây ví dụ như 95%."
  - **Hiếu (Dev):** "Ok anh."
  - **Anh Sơn (Tech Lead):** "Đó, ví dụ như là thằng khác thì nó sẽ có cái tay của nó... Thì về cơ bản là có hai cái. Lấy cái dữ liệu này ra... Thì đảm bảo là tất cả các thằng... Ngoài ra trong này nó sẽ có nhiều thằng... Nó sẽ đỡ cái phần chuyển ra. Mình thêm cấu hình gì đó nó đỡ hơn là có cái object. Tại vì nếu mà mình dùng tạo object đi, ví dụ thằng này username, password, IP, port đi; rồi thằng thứ hai có username lại tạo một trường username nữa; rồi trường thứ ba là API key lại tạo thêm API key nữa... Tự nhiên cái object của mình nó phình lên rất là nhiều. Mà lúc đó mình lại phải đánh dấu là trường này là của kết nối A, trường này kết nối A và B, trường này kết nối A và C... Rất là mất công. Thà chuyển qua Dictionary dễ hơn."
- **[23:30 - 25:00]**
  - **Anh Sơn (Tech Lead):** "Dictionary thì cẩn thận cái chỗ này là chưa có check kỹ nè: chỗ này là phải check key có tồn tại hay không, không sẽ lấy null. Lấy cái này là nó lỗi... Cho nó logic cơ bản... Đó thì mục tiêu là nó sẽ tổng quát chung mấy cái đó. Để khi mình nhập vào, ví dụ như thằng này nó sẽ chung... Nó sẽ chung trong này. Thì khi nhập qua thì nó chỉ care trong này... Trong này nó sẽ có cái cấu hình này nó sẽ có mấy cái thông số khác... Ví dụ như data gửi gì đó... Thì ví dụ như đây là dữ liệu data chung nè, còn mấy thông tin khác ví dụ như thông tin lấy ra... Cấu hình nó sẽ chung và cái data nó cũng sẽ chung. Thì ở phía dưới đó nó kết nối bao nhiêu thằng nó sẽ biết bấy nhiêu thằng đó và mỗi thằng sẽ xử lý. Tầng ngoài nó sẽ nhận nó biết là có cái data... Nó sẽ tổng quát."

---

### Đoạn 3 (25:00 - 37:30): Xử Lý Binary/Byte[], Buffer File Lưu Trữ & Phân Loại Gói Tin 101 - 110
- **[25:00 - 27:30]**
  - **Anh Sơn (Tech Lead):** "Còn thằng này muốn tổng quát được thì thằng phía trước này phải xử lý làm sao cho nó tổng quát. Tại vì ở phía trước này sẽ ra rất là nhiều đúng không? Gói tin của mình 101, 102, 103, 104 gì đó... Đóng gì đó và mỗi cái nó sẽ cấu trúc nó khác nhau. Nhưng mà qua bên này là phải chung... Chung nó phải có một cái gì đó nó bọc lại. Nó giống như là cái thằng... Cái thằng response của API đó, em thấy là nó sẽ chung hết. Nó đều có cái gì? Code, status, rồi cái thời gian... Và dữ liệu nó sẽ để ở trong `record`. Đúng không?"
  - **Anh Sơn (Tech Lead):** "Đó, nó sẽ có những cái cấu trúc như vậy. Còn ở trong `record` này chứa cái gì trong đó thì... Không quan tâm. Thì mình sẽ đi theo từng cái. Ví dụ ở trong `record` nó sẽ có cái mã F... Có cái mã phân biệt là ví dụ gói 101, 102, 103 gì đó. Đó thì qua bên này bắt đầu mình sẽ... Đó là cái cấu hình kết nối. Mục tiêu là quản lý được, và tương tự cái hướng ngược lại cũng vậy luôn. Hướng ngược lại luôn là cái dữ liệu nhận được..."
  - **Hiếu (Dev):** "Là xử lý bóc tách nó như thế nào..."
  - **Anh Sơn (Tech Lead):** "Đúng rồi. Nó đi dây ngược... Nó chưa có luồng thôi... Scan ra... Scan ra nhưng trước cái luồng đó thì có nghĩa là cái phần xử lý connection đó là sẽ dùng chung. Nó sẽ dùng chung cho cả hai đó, nhưng nhận và gửi... Chủ yếu là nó sẽ bị ngược hướng thôi đúng không? Ngược hướng là..."
- **[27:30 - 30:00]**
  - **Anh Sơn (Tech Lead):** "Cố gắng làm sao để có những cái phần nó chung được. Ví dụ như là cái phần liên quan đến mã hóa đi, cái này nói trước thôi... Nói trước thôi để mấy đứa hình dung thôi. Ví dụ như là lấy dữ liệu đi... Hôm qua tính là hôm bữa có ra được cái tập dữ liệu FTP đi, cần chuyển CSV... Cần có chút dạng này... Nhưng mà khi gửi thì nó còn cần tạo ra một cái tên, truyền mã hóa, chìa khóa một cái số... Số đó số public key... Đó. Thì làm sao để khi mà nhận hoặc gửi gì đó... Cái hàm ra được cái key này nó cũng phải ra đúng... Lên được. Hoặc là tương tự ví dụ như là cái... Cái cấu trúc liên quan đến mảng byte đi... Mảng byte ví dụ như là Protobuf... Hoặc là dạng byte đó... Gửi bằng byte, quy chuẩn byte..."
  - **Anh Sơn (Tech Lead):** "Quy chuẩn byte ví dụ như là A, B, C, D, E... Tất cả đều là string đi, hoặc tất cả đều là int đi, thì là 4 byte, 4 byte, 4 byte... Đó. Thì làm sao để convert 4 byte đầu là của A, 4 byte tiếp theo là của B, 4 byte tiếp theo của C... D, E... Ngược lại, khi nhận 4 byte đó làm sao chuyển đổi ngược lại thành A, B, C, D, E."
  - **Hiếu (Dev):** "Mảng này sinh ra làm cái gì anh?"
  - **Anh Sơn (Tech Lead):** "VETC á em."
  - **Hiếu (Dev):** "À..."
- **[30:00 - 32:30]**
  - **Anh Sơn (Tech Lead):** "VETC nó sẽ có những cái mảng mảng byte cho mình để chuyển đổi... Ví dụ như lúc trước là nó sẽ có một cái thằng convert chung... Convert chung thì nó sẽ có... Một là theo kiểu string thì bình thường; hai nữa là nó sẽ có cái... Ví dụ như là cái gói mảng byte đi nó sẽ có kích thước nữa. Nó tổng... Không biết sao nhưng kích thước nó sẽ tổng kích thước lại... Kích thước lại để nó biết được cấu trúc này. Thì lúc đó nó sẽ convert lại: 4 byte đầu là... Thằng này chuỗi nè, thằng này 4 byte là int nè... Tiếp theo là cái gì... Thì cái này là mã hóa truyền đi."
  - **Anh Sơn (Tech Lead):** "Mã hóa truyền đi thì nó sẽ có mã hóa chiều gửi nè... Cái kia chiều gửi á... Thường hay chiều gửi... Đó, thì gần như là nó đảo ngược lại. Này là nhận về nè, là biến đổi cái dữ liệu... Ra lại cái cấu trúc này."
  - **Hiếu (Dev):** "Ok."
  - **Anh Sơn (Tech Lead):** "Ví dụ như là 16 byte đầu... Tính từ 16 nè, 16 4 byte là cái pin á, là 16 đến 20 á... Đó là cái mảng này nè. Để mình biết được cái cấu trúc của nó. 4 byte tiếp theo là cái mảng này... Nhưng mà nó là cái chuỗi."
- **[32:30 - 35:00]**
  - **Đạt (Dev):** "Vậy là nó nó chia ra từng mảng từng mảng..."
  - **Anh Sơn (Tech Lead):** "Đúng rồi. Mảng byte mà. Mảng byte thì nó phải... Nó phải cắt đúng. Cắt lố... Đó thì nói là có một số cái phần liên quan nó sẽ làm trong số mã hóa nếu có sau này. Thì cũng là chừa cái chỗ, ví dụ chừa cái chỗ giữa giữa này nè. Chỗ này chuyển đổi đúng không? Xử lý qua dữ liệu... Thì nó sẽ có một cái phần To-do sau này nữa: mã hóa dữ liệu gì đó nếu có. Để sau này mình có thực hiện thì mình sẽ gắn vào."
  - **Hiếu (Dev):** "Cái phần xử lý này nó đang nằm ở chỗ Worker đúng không?"
  - **Anh Sơn (Tech Lead):** "Nó đang nằm ở chỗ xử lý kết nối á."
  - **Hiếu (Dev):** "Kết nối luôn hả?"
  - **Anh Sơn (Tech Lead):** "Xử lý kết nối... Hoặc em để bên chỗ xử lý data cũng được. Nhưng mà xử lý data thì đó... Mục tiêu là sau này nó cái này nó đi chung luôn... Đi chung là ví dụ như lại dữ liệu mảng byte thì nó có phù hợp không."
  - **Hiếu (Dev):** "Mấy thằng kia nó đang kiểu JSON... Ví dụ như quy định chung là gửi JSON đi... Thì qua bên này nó convert hay gì đó... Thì cái phần kết nối này nó sẽ hỗ trợ phần xử lý kết nối này nè. Thực ra nếu mà đúng á thì cái phần xử lý giống như nó phải nằm ở giữa giữa này nè... Xong nó ra một cái chung này luôn... Xong nó mới ra xử lý... Tách thành ba lớp thì hơi rắc rối."
- **[35:00 - 37:30]**
  - **Anh Sơn (Tech Lead):** "Này lắm lớp quá: xử lý gói tin một lớp rồi, ra cái object chung... Này là một, này hai cái chung một... Ở trên này nó còn cái nữa là... Là lấy dữ liệu đúng không? Lấy dữ liệu là một, xử lý gói tin là hai..."
  - **Hiếu (Dev):** "Lấy dữ liệu... Lấy dữ liệu là cái phần trên á... Một trên á. Xong rồi xử lý gói tin thì hôm bữa là làm rồi, nó ra cái chung rồi. Và lúc lúc làm chung đó là mình chưa có tách thằng này ra... Lúc mình lấy gói chung chung là mình gửi qua hàm này luôn đúng không? Qua luôn bây giờ phải tách nó ra luôn là nó sẽ riêng hết. Nó tách riêng chứ bản chất của nó là sẽ cố gắng tách thành trong code, quản lý trong code thôi, chứ không cần phải nhất thiết tách thành lắm project đâu."
  - **Hiếu (Dev):** "Theo em đang hiểu kiểu lấy dữ liệu, xử lý gói tin thì cái object chung này hình như là đang chỗ là cái mapping gì của..."
  - **Anh Sơn (Tech Lead):** "Đúng rồi, ra cái mapping luôn rồi đó."
  - **Hiếu (Dev):** "Khúc này là mình đang gửi file với lại... phải lòng xuất file với lại gửi HTTP. Thì giờ sắp tới là mình sẽ xử lý tách nó ra..."
  - **Anh Sơn (Tech Lead):** "Đúng rồi, tách nó ra thì cái thằng cái thằng lưu trữ cái file gì á là một... Cái nào nó phù hợp hơn? Để đảm bảo là lỡ thằng này nó tèo... Nó gửi không được đi nhưng mà đảm bảo là cái file nó vẫn lưu."
  - **Đạt (Dev):** "Vậy khi muốn lưu trữ đó là mình sẽ lưu trữ nó... Lưu log hả? Chứ không có lưu file nữa hả?"
  - **Anh Sơn (Tech Lead):** "Lưu log hoặc lưu file đó..."
  - **Đạt (Dev):** "Nếu mà lưu file đó thì mình sẽ lưu theo cái định dạng gửi đi đúng không?"
  - **Anh Sơn (Tech Lead):** "Đúng rồi."
  - **Đạt (Dev):** "Coi định dạng lưu gửi đi đi chứ JSON làm cái gì."
  - **Anh Sơn (Tech Lead):** "Nói chung là có cái để chứng minh là thời điểm đó mình có gửi dữ liệu này nè. Nhưng mà nó bị gì đó... Bị chết hay gì đó không gửi được, nhưng mà thực sự là mình vẫn có xuất."

---

### Đoạn 4 (37:30 - 49:33): Mức Trần Gói Full (LIMIT/TOP), Auto-cleanup Log, UI Query Builder & Khóa @LastKey
- **[37:30 - 40:00]**
  - **Đạt (Dev):** "Cho em xin cái thông tin gói nào là nối đuôi, gói nào là lấy hết? Và trường hợp mà lấy hết á mình có mức chừng cho nó không? Kiểu 100 hay 50 gì đó, hay là cho setting luôn?"
  - **Anh Sơn (Tech Lead):** "Lấy hết thì cứ lấy hết thôi. Tại thường mấy bạn lấy hết mình cũng... Cũng hình dung dữ liệu nhưng mà..."
  - **Hiếu (Dev):** "Nhiều quá full hết luôn không cần."
  - **Anh Sơn (Tech Lead):** "Tại vì nếu mà muốn chặn thì mình add trong câu truy vấn là được rồi."
  - **Hiếu (Dev):** "Thêm `TOP` vô thôi."
  - **Anh Sơn (Tech Lead):** "Đúng rồi, không có sợ đâu. Cứ mặc định là lấy all đi. Coi coi nó xử lý như thế nào. Nếu nó ra cái câu... Cái gói tin nó nặng quá không gửi được hay không... Chứ thực tế là mình rào lại đi được, tại vì câu truy vấn mình đã động rồi, không sao. Mình cũng đang lưu trong DB mà, có gì mình sửa lại thôi nó cũng ăn được, không sao."
- **[40:00 - 43:00]**
  - **Hiếu (Dev):** "Mấy cái log á, mình có xử lý auto xóa không?"
  - **Anh Sơn (Tech Lead):** "Log hả? Gửi tin... Mấy cái log gửi tin gửi thất bại hay gì đó nó đang ghi lại, lưu lại á... Thì log thì để sau đi. Log thì đang tính làm cái cái xóa định kỳ á. Nhưng mà thôi cứ ưu tiên cái kia trước đi. Xóa định kỳ thì thường làm sao cũng được. Xóa định kỳ thực ra làm cái hàm file rồi cho nó chạy định kỳ rồi trong trỏng mình set thiết lập thôi. Nó cũng dễ nó không khó."
  - **Anh Sơn (Tech Lead):** "Chủ yếu là trong trỏng nó sẽ có hai chỗ xóa thôi: một là xóa file đúng không; hai là xóa dữ liệu trong DB. Đó thì nó có hai cái... Thì bố trí nói chung là bố trí cái cái cấu trúc... Cái cấu trúc và thứ hai nữa là các trường... Trường nào mình sẽ dùng để mình xóa đó. Để khi mà xóa thì nó sẽ xóa nhanh là được. Ví dụ như cấu trúc theo thư mục đi, thì ví dụ mình xóa dữ liệu... Mình chơi mình xóa theo từng ngày thì cứ đè cái thư mục đó xóa khỏi phải quét từng file. Ví dụ mình cấu hình một đống thư mục rồi mình xóa thư mục hoặc cấu hình theo tháng gì đó."
  - **Hiếu (Dev):** "Ý em nói kiểu cái tên thư mục cấu hình theo ngày hay gì để dễ biết đường đúng không?"
  - **Anh Sơn (Tech Lead):** "Cấu trúc rõ ràng để nó dễ xóa... Vậy thôi."
- **[43:00 - 46:00]**
  - **Hiếu (Dev):** "Nay 14/10... Có hai người mở à... Build UI thấy khó..."
  - **Anh Sơn (Tech Lead):** "Cho xin lại thông tin những gói nào mà nối đuôi với lại... Hôm phái... Hôm phái bữa có ghi đó."
  - **Hiếu (Dev):** "Đọc cái video đó đúng không?"
  - **Anh Sơn (Tech Lead):** "Ừ, cứ theo cái video đó đi rồi mốt chỉnh sau. Thật ra bản chất chỉnh cái đó mình chỉnh động được mà."
  - **Hiếu (Dev):** "Chưa cấu hình á, giờ tính làm luôn nè."
  - **Anh Sơn (Tech Lead):** "Thật ra mình sẽ có hành động chỗ đó. Ví dụ như trong cái mà cái giao diện mà quản lý đúng không? Phía dưới nó có một cái input... Textarea hay input... Textarea làm cái gì mà biết kiểu nào để nó đẹp đẹp á. Có thể dùng cái dạng JSON cũng được. Chứ làm textarea kiểu kia kiểu cũ nó cùi lắm. Nhìn cái kiểu giống như mình để mình quản lý á, người ta nhìn vào là: À, cái textarea này có liền... Đừng có làm cái textarea giống như là cái cái note... Cái note khó nhìn lắm, cái này khó nhìn lắm. Nó vừa nó vừa khó mà nhìn nó vừa xấu nữa. Nó không đẹp, nó không được wow. Kiếm cái cái UI nào nó đẹp hơn... Cái này chắc một chút... Kiếm cái component nào nó xịn xò tí."
- **[46:00 - 49:33]**
  - **Anh Sơn (Tech Lead):** "Thứ hai nữa là bên dưới nó sẽ có cái phần add parameter... Để mình biết được là trong đối tác đó mình mình sẽ cần cái gì. Trong input cái câu truy vấn này mình cần truyền hai cái parameter này vào..."
  - **Hiếu (Dev):** "Tạo truy vấn động đúng không?"
  - **Anh Sơn (Tech Lead):** "Ừ, mình phải tạo mình phải add ở đây hai cái parameter. Để mình biết được à trong cái... Cái này nó sẽ có hai parameter đó."
  - **Hiếu (Dev):** "Cái này anh đang nói về UI đúng không? Query... Hình như nó có cái Query Builder anh coi thử được không? Hoặc tìm thư viện coi tham khảo cái UI Query Builder..."
  - **Anh Sơn (Tech Lead):** "Nó có mấy cái dạng đó... Nhưng mà làm sao để cái chỗ mà... Cái chỗ này nè... Cái chỗ này là cái chỗ điều kiện nè đúng không? Điều kiện giá trị điều kiện nè... Thì làm sao để nó tự... Theo cái giá trị của nó. Chính là cái hàm giống như cái hàm get ra để mình biết là ở trong code mình truyền nó vô trong đó là ví dụ `meta.LastKey`... Thì khi mình đọc cái `LastKey` mình sẽ biết được. Ví dụ dữ liệu đằng trước nó lưu cái key là cái gì đó, mình phải định ra cái key nó chính là... Cái `LastKey`... Ví dụ anh nói cái `LastKey` nó sẽ bằng... Nó sẽ bằng với cái cột `CreateTime`... Thì khi mà nó xử lý cái cuối nó sẽ lấy cái `CreateTime` nó lưu vào cái `LastKey`... Xử lý nối đuôi á... Lưu trong cái bảng nào đó, ví dụ một cái bảng nó định dạng là đối tác A, gói tin 101, cái key... Cái `LastKey`... Nó sẽ lưu là ngày 12 tháng 10... Thì lúc đó xử lý tiếp theo nó sẽ lấy thằng này bóc bỏ vô đây."
  - **Anh Sơn (Tech Lead):** "Kiểu như vậy... Nhưng mà ví dụ như là... Nó bén hơn... Ví dụ `Date`... `Date.Now` trừ 30... trừ đi... Nó sẽ tự động nó kiêng... Tính từ hiện tại trừ đi 30 ngày, hoặc trừ đi 7 ngày... Nó rào lại ví dụ như là thằng này `LastKey`... Lấy từ `LastKey` nè, nhưng mà nó chỉ giới hạn trong vòng 7 ngày thôi. 7 ngày gần nhất, chứ nó không lấy full vì nhiều tình huống cái `LastKey` nó xa quá... Dữ liệu cũ quá..."
  - **Hiếu (Dev):** "Nếu mà `LastKey` mà xa quá thì mình chỉ lấy trong vòng 7 ngày thôi hả?"
  - **Anh Sơn (Tech Lead):** "Thì anh ví dụ thôi... Để cái điều kiện mình set động được. Nhưng mà cơ bản là mình đang care thằng `LastKey` trước thôi. Còn mấy cái này là phát sinh thêm thôi, nhưng mà hình dung là nó sẽ có động như vậy. Thì mấy cái này từ từ bổ sung thêm không cần thiết đâu. Mình chỉ check đúng cái trường hợp mình đang yêu cầu là: Lấy dữ liệu mới thì sẽ có một cái `LastKey` mình lưu lại. `LastKey` này nói `LastKey` nhưng mà à nó có thể kết hợp nhiều, ví dụ như kết hợp giữa ngày, giữa detect time đi, nó kết hợp với lại một cái mã ví dụ như mã làn hay gì đó... Tại nhiều tình huống là dữ liệu đó thời gian có thể bị trùng, nhưng mà một cái chỗ nào đó ví dụ như vị trí... Location chắc chắn không thể trùng được. Không thể nào một một cái xe nó dịch chuyển tốc biến từ chỗ này qua chỗ kia được."
  - **Anh Sơn (Tech Lead):** "Nên là nó sẽ có kết hợp thêm một cái thông tin gì đó... Thường là lấy vị trí... Đa số là lấy vị trí là ok nhất. Thời gian ví dụ bên thu phí là hay lấy thời gian với mã làn á... Tại không thể nào một xe A này vào làn này mà lại lần khác cũng vào được... Thì thường là sẽ có thêm mã làn. Còn chắc ăn nữa thì có mã làn, mã trạm luôn: trạm, làn, ngày... Còn bên này có thể là lấy vị trí, ví dụ kilomet mét gì đó... Mình thấy vừa rồi... Thôi được rồi... Làm một bản trước đi."

==================================================================

# Ghi Chú Kỹ Thuật ShareData (14/10/2026)

> **Phân hệ:** Chia sẻ dữ liệu (ShareData) — Tuyến cao tốc Hữu Nghị - Chi Lăng  
> **Nguồn gốc:** Bản ghi chú trao đổi kỹ thuật ngày 14/10/2026  
> **Mục tiêu:** Định hướng kiến trúc Truy vấn động SQL (Dynamic Query), Cấu hình kết nối linh hoạt (Dictionary) và Cơ chế mở rộng tương lai.

---

## 1. Lưu Trữ & Quản Lý Truy Vấn Động SQL (Dynamic Query Extraction)

- **Nguyên lý chuyển đổi:** Chuyển toàn bộ câu lệnh trích xuất dữ liệu từ việc hardcode trong mã nguồn C# sang lưu trữ, cấu hình và quản lý động trực tiếp trong CSDL (lưu chuỗi câu truy vấn tại bảng gói tin mẫu và bảng cấu hình ánh xạ đối tác, không tạo bảng riêng `ShareDataPacketExtractSql`).
- **Phân loại quản lý:**
  - **Đối tác** (`PartnerCode`): Cho phép tuỳ biến câu lệnh theo từng đối tác cụ thể.
  - **Gói tin** (`PacketCode`): 101 – 111.
  - **Chiều dữ liệu** (`Direction`): Chiều gửi (Outbound) hoặc chiều nhận (Inbound).
- **Cơ chế tham số hóa SQL (SQL Parameters):**
  - Trích xuất dữ liệu có điều kiện lọc thời gian động: từ ngày (`FROM`) đến ngày (`TO`).
  - Hỗ trợ các biến ngữ cảnh hệ thống (Metadata Parameters):
    - `meta.last_key`: Con trỏ mốc trích xuất dữ liệu cuối cùng (Ví dụ: `CreateTime`, ngày ghi nhận `2026-10-12` đối với gói 101).
    - `meta.now - 7d`: Cửa sổ trượt giới hạn lùi thời gian (Rolling window 7 ngày) để tránh quét lại toàn bộ dữ liệu lịch sử quá cũ.
- **Yêu cầu giao diện cấu hình (Frontend UI):**
  - **Cấm dùng `textarea` đơn thuần:** Tránh việc nhập liệu thô sơ không có kiểm tra cú pháp, khó bảo trì và dễ gây lỗi runtime.
  - **Giải pháp:** Sử dụng bộ dựng truy vấn trực quan (Visual Query Builder) hoặc trình soạn thảo code chuyên dụng (có gợi ý bảng/cột, highlight cú pháp, format SQL và kiểm tra validation `SELECT` an toàn).

---

## 2. Kiến Trúc Cấu Hình Kết Nối Đa Kênh (Connection Configuration)

- **Sơ đồ phân tầng xử lý:**
  ```text
  [Lấy dữ liệu (Dynamic SQL)] 
           │
           ▼
  [Xử lý & Ánh xạ gói tin] 
           │
           ▼
  [Đối tượng dữ liệu chung (Common JSON Object)] 
           │
           ▼
  [Tầng Giao vận / Kết nối (Transport Connection)]
    ├── HTTPS / REST API
    ├── FTP / SFTP
    ├── Kafka / Message Queue
    └── File Export / Khác
  ```
- **Chuẩn hóa cấu hình bằng `Dictionary<string, string>` (C#):**
  - Tham khảo kiến trúc đã chạy ổn định tại: `IntgVmsManager.cs`.
  - Thay vì cố định các cột thuộc tính cứng trong CSDL, cấu hình kết nối của đối tác được lưu trữ động dạng Key-Value trong Dictionary / JSON:
    - **Kênh REST API:**
      - `dic.Add("ip", "...")`
      - `dic.Add("port", "...")`
      - `dic.Add("apiKey", "...")`
      - `dic.Add("endpoint", "...")`
    - **Kênh FTP / SFTP:**
      - `dic.Add("ip", "...")`
      - `dic.Add("port", "...")`
      - `dic.Add("username", "...")`
      - `dic.Add("password", "...")`
- **Đặc điểm:** Tách bạch hoàn toàn thông tin cấu hình người nhận/giao vận với dữ liệu nghiệp vụ của gói tin.

---

## 3. Cơ Chế Mã Hóa & Định Dạng Mở Rộng (Làm Sau)

> ⚠️ **Trạng thái:** Tạm thời chưa triển khai ở pha hiện tại, lưu lại làm định hướng tích hợp cho các giai đoạn nâng cấp sau.

- **Kênh truyền FTP & Xuất tệp phẳng (CSV / Text):**
  - Xuất dữ liệu theo định dạng ngăn cách dấu phẩy (CSV) tương ứng danh sách trường của gói tin:
    - Gói 101: `a,b,c,d,e`
    - Gói 102: `1,2,3,4,5`
  - Đảm bảo đồng bộ cấu trúc với hệ thống Backend đối tác (như hệ thống thu phí VETC).
- **Bảo mật & Mã hóa:**
  - Cơ chế cặp khóa mã hóa: Private Key / Public Key.
- **Định dạng tuần tự hóa nâng cao:**
  - **Protocol Buffers (Protobuf):** Tuần tự hóa dữ liệu nhị phân tốc độ cao, tối ưu băng thông.
  - **ASN.1:** Chuẩn mã hóa truyền thông ngành giao thông / viễn thông (tham khảo nghiên cứu thêm).

---

## 4. Nguyên Văn Bản Ghi Chú Gốc (Raw Notes)

```text
1. lay va luu => sql lưu trữ va quản lý
truy van dong SQL 
code => truy vấn

chat sql sugar -> 

doi tac , goi tin, chieu

lay du lieu: code -> sql   dieu kien  from to  sql paramters

luu du lieu: sql

input textarea không dùng dùng cách nào dễ maintain hơn
sql  

add paramter
+ A       meta.last_key       
+ B			meta.now - 7d	
last_key  =   createTime


A 			101				last_key = 2026-10-12

====================
cấu hình kết nối 

lay du lieu -> xủ lý gói tin  >>>>>  obj chung   			    >>>> phan ket nói                         
					   data gui  => cau truc no chung				switch    ftp:
 					   thong tin nguoi nhan (cau hình)								
                                                                         //ma hoa
dic.Add ('ip','')														ftp
 																		duc['Ip'].,dic['username'],
dic.Add {'port','')

api
dic.Add('apiKey','')

ftp
dic.Add('username','')


=====================
Mã hoá: làm sau
ftp
csv
101:    a,b,c,d,e
102:    1,2,3,4,5

private public key

protobuf/ 

a,b,c,d,e    44444  dong bo voi Backend (thu phi vetc)

ASN.1 (tham khảo thêm thôi)
```

==============================================
# Note Cá nhân ShareData 14/10/2026

- Build SQL UI, động tham khảo query build mà mình có làm sẵn rồi giờ cần build API lấy table, danh sách field dựa trên 02-mapping-goi-tin-101-111.md.
- Khi chọn gói tin sẽ có SQL cấu trúc sẵn, SQL động đi theo gói tin vì mình không muốn khi tạo đối tác mới lại tạo lại SQL từ đầu.
- Cấu hình kết nối dùng Dictionary trong C#, tham khảo cách truyền input và điều phối integration trong `IntgVmsManager.cs`; ghi chú kỹ thuật/raw notes đã được hợp nhất trong tệp transcript này.
- Dữ liệu nhận parse lại sao cho luồng gửi và nhận giống nhau (hãy tham khảo luồng inbound đang xử lý như nào đối chiếu lại).
- Viết sẵn cấu trúc mã hóa encode/decode, provider và cấu hình để dùng khi áp dụng; trạng thái bật sử dụng được theo dõi riêng.
- Các gói snapshot gửi full hết không cần tốp mức trần gì hết.
- Viết sẵn code cleanup theo cấu hình; chưa kích hoạt xóa ở môi trường sử dụng cho đến khi áp dụng cấu hình.
- Gói 110 cho tạo/cấu hình bình thường; bổ sung bảng mô phỏng và dữ liệu giả, chuẩn bị script cấu trúc/seed để test toàn trình.
- Gói 105 mặc định Snapshot theo `02-mapping-goi-tin-101-111.md` mục `All/update`; setting cấu hình gói cho chọn Snapshot/Incremental và Worker dùng setting đã lưu. Đây là quyết định đã chốt, không còn chờ xác nhận.
- Viết sẵn code Kafka producer, CSV/FTP và pipeline payload chung để dùng khi cấu hình áp dụng; phải phân biệt có code thực thi với đã bật kênh sử dụng.
- Sau sửa FE phải chạy lại Playwright, ghi rõ số pass/fail/skip hoặc lỗi môi trường cụ thể.
