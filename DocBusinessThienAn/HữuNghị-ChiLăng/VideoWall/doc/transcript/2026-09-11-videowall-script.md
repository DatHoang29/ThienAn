---
tier: A
read: full
source: 
  - Sharedata-videowall.m4a
  - Sharedata-videowall-2.m4a
date: 2026-09-11
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: VideoWall (Phân hệ Tường Màn Hình)
participants:
  - Anh Sơn (Tech Lead / Kiến trúc hệ thống)
  - Hiếu (Dev Backend / Service Worker / VideoWall)
  - Đạt (Dev Backend / Web API / Database)
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ Tường Màn Hình (VideoWall)

> **Ghi chú:** Tài liệu này được tổng hợp và hợp nhất toàn diện từ 2 tệp ghi âm cuộc họp ngày 11/09/2026 (`Sharedata-videowall.m4a` và `Sharedata-videowall-2.m4a`), lọc riêng toàn bộ nội dung thảo luận, quyết định kiến trúc, cơ sở dữ liệu và kịch bản đối thoại thuộc phân hệ **VideoWall**.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Kiến trúc phân tầng Loose-Coupling qua NATS Message Broker
- **Sơ đồ chuỗi điều khiển:**
  $$\text{Web Frontend (FE)} \longrightarrow \text{Backend WebAPI} \stackrel{\text{NATS}}{\longrightarrow} \text{VideoWall Background Service} \stackrel{\text{ISAPI HTTP}}{\longrightarrow} \text{Controller (DS-C66S / DS-C30S)}$$
- **Lý do bắt buộc tách riêng VideoWall Background Service:**
  - Controller phần cứng đặt tại phòng máy và kết nối qua mạng nội bộ LAN, tiềm ẩn nguy cơ mất điện, đứt cáp, chập chờn hoặc thiết bị tự khởi động lại.
  - **Chống tắc nghẽn WebAPI (Deadlock / Thread Starvation):** Nếu Backend WebAPI kết nối trực tiếp với phần cứng, mỗi khi thiết bị phản hồi chậm hoặc treo socket, luồng HTTP của WebAPI sẽ bị khóa cứng (blocking). Hàng loạt user thao tác trên Web sẽ khiến Web server cạn kiệt tài nguyên và sập toàn bộ hệ thống.
  - **Cô lập lỗi (Fault Isolation):** Việc tách Service riêng và dùng NATS làm trung gian giao tiếp giúp WebAPI luôn phản hồi tức thì (non-blocking). Khi phần cứng gặp sự cố, chỉ có Service Worker bên dưới chịu lỗi và thực hiện retry, toàn bộ ứng dụng Web vẫn hoạt động bình thường.

---

### 1.2. Ba trụ cột chức năng của VideoWall
1. **Thiết lập (Configuration / Setup):**
   - Khai báo cấu hình controller (Master và Slave).
   - Cấu hình ma trận màn hình (ví dụ: lưới 8x5 hoặc 8x4).
   - Sơ đồ đấu nối cổng vào/ra (Input/Output ports).
   - Cấu hình các kịch bản mẫu (Scenes).
2. **Điều khiển (Control):**
   - Gửi lệnh gọi kịch bản (Switch Scene).
   - Mở cửa sổ, đóng cửa sổ, di chuyển vị trí, phóng to/thu nhỏ (Zoom / Roam window).
   - Kéo thả nguồn camera / tín hiệu cục bộ lên các ô màn hình.
3. **Giám sát & Dữ liệu (Status / Telemetry):**
   - Đọc trạng thái kết nối phần cứng (Online / Offline).
   - Trạng thái các cổng tín hiệu vào/ra.
   - Giám sát kênh giải mã (Decoding channels).
   - Gửi dữ liệu trạng thái real-time ngược về Web qua WebSocket / NATS để cập nhật giao diện.

---

### 1.3. Cơ chế Phân quyền Màn hình (Screen Partitioning & Bounding)
- **Nguyên tắc cốt lõi:**
  - **Không phân quyền trên Controller phần cứng:** Thiết bị phần cứng bên dưới không có khái niệm user/phòng ban; phân quyền hoàn toàn do phần mềm tầng trên quản lý.
  - **Phân quyền theo Màn hình (Screen ID / Display Region):** Giới hạn phạm vi thao tác của người dùng trên từng màn hình cụ thể trong ma trận tường màn hình.
- **Cấu trúc bảng CSDL phân quyền:**
  - Cột `UserId`: Định danh người dùng cụ thể.
  - Cột `OrgId`: Định danh phòng ban/tổ chức.
  - Cột `ScreenIds`: Danh sách các ID màn hình được phép thao tác (dạng mảng JSON hoặc danh sách liên kết, ví dụ: `[1, 2, 3]`).
- **Quy tắc ưu tiên (Priority Rule):**
  1. Cấu hình theo `UserId` có độ ưu tiên cao nhất, **ghi đè (override)** cấu hình của `OrgId`.
  2. Nếu không cấu hình theo `UserId`, người dùng sẽ **thừa hưởng mặc định** cấu hình phân quyền từ `OrgId` của họ.
  3. **Mặc định (Fallback):** Nếu một User/Org hoàn toàn không có bản ghi nào trong bảng phân quyền, hệ thống mặc định cấp **Full quyền** (được phép điều khiển toàn bộ các màn hình).

---

## 2. Ma trận phân công công việc & Kế hoạch hành động

| Thành viên | Trách nhiệm | Công việc chi tiết | Deadline |
|---|---|---|---|
| **Đạt** | Backend WebAPI & DB | - Thiết kế bảng CSDL phân quyền màn hình theo `UserId` và `OrgId`.<br>- Viết API nhận lệnh điều khiển từ Frontend và publish message lên NATS.<br>- Xây dựng API quản lý cấu hình kịch bản (Scene). | Hết tuần sau |
| **Hiếu** | Core Service Worker | - Xây dựng Background Service kết nối với thiết bị Hikvision qua giao thức ISAPI HTTP.<br>- Lắng nghe lệnh từ NATS topic, biên dịch thành lệnh ISAPI tương ứng.<br>- Lấy dữ liệu trạng thái/heartbeat từ thiết bị và gửi ngược về hệ thống. | Hết tuần sau |
| **Kiên** | Frontend UI | - Móc API điều khiển VideoWall lên giao diện Web.<br>- Vẽ bố cục lưới ma trận màn hình, hiển thị rào quyền thao tác theo từng ô.<br>- Xử lý các thao tác kéo thả nguồn camera, chuyển scene mượt mà. | Hết tuần sau |

---

## 3. Kịch bản đối thoại chi tiết (Chronological Transcript)

### [Phần 1: Kiến trúc phân tầng & Tách riêng Background Service]
- **Đạt:** Xong phần ShareData rồi anh, giờ qua VideoWall nha. Cái luồng xử lý VideoWall á, cái chỗ phân quyền theo màn hình thì em hiểu rồi. Còn cái chỗ mà thiết bị á, là bây giờ sẽ build thêm một cái Service riêng để giao tiếp với thiết bị hả anh?
- **Anh Sơn:** Đúng rồi, tách riêng một cái Service độc lập.
- **Đạt:** Anh nói thử cái luồng đi anh, em chưa hình dung rõ chỗ đó.
- **Anh Sơn:** Luồng nó đi như thế này: Web (User click) $
ightarrow$ Backend WebAPI $
ightarrow$ NATS $
ightarrow$ VideoWall Service $
ightarrow$ Thiết bị Controller.
- **Đạt:** Nghĩa là theo em đang hiểu là User làm việc trên Web, gọi lên WebAPI. WebAPI bắn tín hiệu qua NATS sang Service. Rồi Service đó mới gọi ISAPI tới thiết bị Controller phần cứng?
- **Anh Sơn:** Đúng rồi.
- **Hiếu:** Tại sao không gọi trực tiếp từ WebAPI xuống thiết bị luôn mà phải đi vòng qua NATS hả anh Sơn?
- **Đạt:** Anh Sơn mới nói đó, để thiết bị nó chết thì chỉ chết cái service đó thôi, không chết cả hệ thống Web!
- **Anh Sơn:** Đúng! Thiết bị phần cứng ở phòng máy lỡ mất mạng, mất kết nối hay bị treo, nếu WebAPI gọi trực tiếp thì toàn bộ request của User trên Web sẽ bị treo theo, sập luôn cả server Web. Khi tách Service ra và đi qua NATS, WebAPI bắn lệnh xong là rảnh tay, Service bên dưới tự quản lý việc kết nối và retry với thiết bị.

---

### [Phần 2: Ba nhóm chức năng cốt lõi của VideoWall]
- **Anh Sơn:** Trong VideoWall, mình chia làm 3 nhóm chức năng rất rõ ràng:
  1. **Thiết lập (Setup/Config):** Khai báo màn hình, ma trận, sơ đồ cắm cổng, kịch bản (Scene).
  2. **Điều khiển (Control):** Chuyển scene, bật/tắt nguồn, cắt ghép cửa sổ, kéo thả nguồn.
  3. **Trạng thái / Dữ liệu (Status/Telemetry):** Lấy thông số từ thiết bị trả về (kết nối, tình trạng kênh giải mã, cảnh báo).
- **Hiếu:** Cái luồng dữ liệu trả về từ thiết bị thì sao anh?
- **Anh Sơn:** Service bên dưới định kỳ lấy trạng thái từ thiết bị, ghi log/CSDL, đồng thời bắn message qua NATS để WebAPI và giao diện Web cập nhật real-time trạng thái cho người dùng thấy.

---

### [Phần 3: Thiết kế Phân quyền Màn hình theo User & Org]
- **Đạt:** Còn cái chỗ phân quyền theo màn hình (Screen) á anh, bữa bàn là tạo thêm một bảng mới để lưu đúng không?
- **Anh Sơn:** Đúng, sẽ có bảng lưu phân quyền màn hình. Trong đó có cột `OrgId` và cột `UserId`.
  - Có 2 cột, cột nào điền thì ưu tiên cột đó. Ví dụ lưu theo `UserId` thì `UserId` có giá trị, lưu theo `OrgId` thì `OrgId` có giá trị.
  - Ví dụ User A được điều khiển màn hình 1, 2, 3. User B được điều khiển màn hình 4, 5, 6.
- **Đạt:** Nếu một User không được khai báo trong bảng này thì sao anh?
- **Anh Sơn:** Mặc định nếu không khai báo thì là **Full quyền**! Được điều khiển tất cả màn hình. Còn nếu có khai báo thì ăn theo cấu hình, và **ưu tiên quyền của User hơn quyền của Org**.
- **Hiếu:** Tức là một record cấu hình chỉ chứa User hoặc Org thôi đúng không anh?
- **Anh Sơn:** Đúng, hoặc là phân quyền cho User cụ thể, hoặc là cho cả Org. Phân quyền User sẽ ghi đè quyền Org.

---

### [Phần 4: Phân công nhiệm vụ & Tiến độ hoàn thành]
- **Hiếu:** Bây giờ hai cái source FE, VideoWall thì kêu Kiên sửa, Kiên ghép lại.
- **Đạt:** Kiên làm phần nào?
- **Hiếu:** Đợt trước Kiên móc hai cái giao diện VideoWall, thì bây giờ kêu Kiên vô sửa theo API mới của tụi mình.
- **Anh Sơn:** Đúng rồi, Kiên làm giao diện thì cứ đẩy cho Kiên làm. Đạt lo phần Backend WebAPI và Database, Hiếu lo phần Core Service và kết nối NATS.
- **Hiếu:** Deadline qua tuần là tới đâu anh Sơn?
- **Anh Sơn:** Hết tuần sau, tụi em phải có một bản chạy được trọn vẹn: VideoWall điều khiển được chuyển scene và phân quyền màn hình cơ bản.
- **Đạt:** Dạ ok anh. Giờ hai đứa em nắm rõ hết luồng rồi, chiều nay anh cứ đi họp, tụi em bắt tay vô triển khai luôn.\n

====================================
Note của sếp: 
VideoWalll



FE                 BE                 Service                   Thiet bi
 		API                CSDL                  API/SDK/Protocol                 
						   MQ (Nats)
						   API


FE                 BE                 Service                   Thiet bi
 		API                Nats                  API/SDK/Protocol         


Subjects (= topic/channel/kenh truyen/group)



Thiết bị:
Huong di 
   Thiết lập
   Điều khiển

Huong nhan
	Dữ liệu (Thống số, dữ liệu xử lý, kết quả thực thi)

Service <-> Thiết bị 



FE <-> BE

hướng đi:
   Thiết lập
   Điều khiển
Huong nhan
	Dữ liệu (Thống số, dữ liệu xử lý, kết quả thực thi)

BE <=> Service
hướng đi:
   Thiết lập
   Điều khiển
Huong nhan
	Dữ liệu (Thống số, dữ liệu xử lý, kết quả thực thi)




Thiet lap
Dieu khien

Du lieu

=> 3 subject

ta.its.control.videowall
>> 2 gói tin riêng biệt
	PackageType: control: gửi đi
	PackageType: control-response: nhân ve




ta.its.status.videowall: 
ta.its.data.videowall: dữ liệu nhận dc, xử lý







VideoWalll
Thiết lập cấu hình
Điều khiển thiết bị
Đọc thông số định kỳ (hoạt động bật/tắt/....)

Dich vu -> CSDL
Nat -> gửi qua  -> FE -> realtime




user phan quyen
123456
123456
123456
OrgId  UserId		Config
A					[{1,1},{1,2},{1,3}]
				
		1			[{2,1},{2,2}]


CHECK DUPLICATE

========================================================
Note của tôi
1. User phân quyền theo screen 
2. FE => BE => NATS => SERVERVICE => GỌI HTTP Thiết bị => 
3. Thiết bị: Hướng đi Thiết lập , điều khiển
4. Thiết bị: Hướng nhận dữ liệu thông số, dữ liệu xử lý, kết quả thực thi. Service <-> Thiết bị, FE <-> BE:
5. Service này điều khiển tính toán thiết lập, điều khiển thiết bị, đọc thông tin định kỳ (hoạt động bật/tắt)
6. Nếu không có khai bao trong này mặc định là full ưu tiên user hơn là orgID, check duplicate