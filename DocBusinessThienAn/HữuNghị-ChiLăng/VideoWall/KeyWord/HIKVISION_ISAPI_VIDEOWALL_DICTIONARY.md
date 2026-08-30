# 📖 TỪ ĐIỂN THUẬT NGỮ & HƯỚNG DẪN NGHIỆP VỤ HIKVISION ISAPI VIDEO WALL
> **Dự án**: Hệ thống Quản lý & Điều khiển Video Wall Trung tâm Giám sát Điều hành Giao thông Thông minh (ITS).  
> **Tài liệu tham chiếu gốc**: Toàn bộ bộ tài liệu kỹ thuật tại `DocBusinessThienAn/HN-CL/VideoWall/ISAPI-Videowall-Controller/` (từ mục 01 đến 10).

---

## MỤC LỤC
1. [Kiến trúc Tổng thể & Giao thức Cốt lõi (Core Protocols & Framework)](#1-kiến-trúc-tổng-thể--giao-thức-cốt-lõi)
2. [Khái niệm Phần cứng & Ma trận Video Wall (Hardware & Topology)](#2-khái-niệm-phần-cứng--ma-trận-video-wall)
3. [Quản lý Kênh Tín hiệu Đầu vào & Đầu ra (Channels & Signal Routing)](#3-quản-lý-kênh-tín-hiệu-đầu-vào--đầu-ra)
4. [Quản lý Cửa sổ, Phân lớp & Cắt lát (Windowing, Layering & Slicing)](#4-quản-lý-cửa-sổ-phân-lớp--cắt-lát)
5. [Kịch bản Hiển thị, Luồng Công việc & Lập lịch (Scenes, Workflow & Scheduling)](#5-kịch-bản-hiển-thị-luồng-công-việc--lập-lịch)
6. [Bảo mật, Xác thực & Quản trị Thiết bị (Security, Auth & Device Management)](#6-bảo-mật-xác-thực--quản-trị-thiết-bị)
7. [Bảng Tra cứu Nhanh Các Từ Viết Tắt (Acronyms & Abbreviations)](#7-bảng-tra-cứu-nhanh-các-từ-viết-tắt)
8. [Cây Phụ thuộc Gộp & Map sang Code Thực tế (Dependency Tree & Code Mapping)](#8-cây-phụ-thuộc-gộp--map-sang-code-thực-tế)

---

## 1. KIẾN TRÚC TỔNG THỂ & GIAO THỨC CỐT LÕI

### 1.1. ISAPI (Intelligent Security API)
- **Định nghĩa:** Là giao thức giao tiếp lập trình ứng dụng độc quyền của hãng Hikvision, hoạt động trên nền tảng **RESTful HTTP/HTTPS**.
- **Cơ chế hoạt động:** Client (WebAPI, Ứng dụng điều khiển) gửi các yêu cầu chuẩn `GET`, `POST`, `PUT`, `DELETE` với định dạng dữ liệu truyền tải (Payload) là **XML** (hoặc JSON trên một số dòng firmware mới).
- **Đặc thù Hikvision:** Phần lớn các API điều khiển phần cứng của Video Wall bắt buộc phải định danh đúng **XML Namespace**:
  ```xml
  xmlns="http://www.isapi.org/ver20/XMLSchema"
  ```

### 1.2. SADP (Search Active Device Protocol)
- **Định nghĩa:** Giao thức dò tìm thiết bị Hikvision chạy ở tầng liên kết dữ liệu (Layer 2 - Data Link Layer / Broadcast).
- **Ý nghĩa thực tế:** Khi thiết bị mới xuất xưởng hoặc bị đổi IP không xác định được, SADP cho phép phần mềm quét thấy thiết bị trong cùng mạng LAN mà không cần biết trước địa chỉ IP.

### 1.3. Digest Authentication (Xác thực Chuỗi băm - RFC 7616)
- **Định nghĩa:** Cơ chế xác thực bảo mật tiêu chuẩn của Hikvision. Mật khẩu không bao giờ được gửi dưới dạng văn bản thô (Plain Text) qua mạng.
- **Quy trình Handshake 4 bước:**
  1. Client gửi request ban đầu không có Authorization.
  2. Thiết bị từ chối với mã `401 Unauthorized` kèm theo chuỗi ngẫu nhiên `nonce`, `realm`, và thuật toán `qop`.
  3. Client tính toán chuỗi băm `response = MD5(MD5(username:realm:password) : nonce : nc : cnonce : qop : MD5(method:uri))`.
  4. Client gửi lại request kèm Header `Authorization: Digest ...` để thiết bị xác thực thành công `200 OK`.

### 1.4. HTTP Chunked Transfer Encoding
- **Định nghĩa:** Cơ chế truyền dữ liệu theo từng khối (chunk) liên tục khi kích thước tổng của dữ liệu chưa được xác định trước.
- **Ứng dụng:** Dùng khi bắt gói tin mạng thời gian thực (**Real-time Packet Capture - Mục 5.1**) truyền stream dữ liệu trực tiếp về trình duyệt.

### 1.5. ResponseStatus (Cấu trúc Trạng thái Chuẩn của Hikvision)
Mọi phản hồi trả về từ thiết bị khi thực thi lệnh (Thêm, Sửa, Xóa, Kích hoạt) đều có cấu trúc XML chuẩn:
```xml
<ResponseStatus version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
    <requestURL>/ISAPI/DisplayDev/VideoWall/1/windows</requestURL>
    <statusCode>1</statusCode>
    <statusString>OK</statusString>
    <subStatusCode>ok</subStatusCode>
    <errorCode>0x00000000</errorCode>
    <errorMsg>Success</errorMsg>
</ResponseStatus>
```
* **Quy ước StatusCode:**
  - `statusCode = 1` hoặc `0`: **Thành công (Success)**.
  - `statusCode = 4`: **Lỗi tham số hoặc trạng thái không hợp lệ (Invalid Operation / Bad Request)**.
  - `statusCode = 6`: **Lỗi nội dung / Lỗi phần cứng (Invalid Content / Device Error)**.

---

## 2. KHÁI NIỆM PHẦN CỨNG & MA TRẬN VIDEO WALL

```text
 ┌──────────────────────────────────────────────────────────────────┐
 │                    HỆ THỐNG VIDEO WALL TỔNG THỂ                  │
 │                                                                  │
 │  ┌────────────────────────────────────────────────────────────┐  │
 │  │              TƯỜNG MÀN HÌNH GHÉP (VIDEO WALL)               │  │
 │  │  ┌──────────────┬──────────────┬──────────────┐            │  │
 │  │  │  Screen 1    │  Screen 2    │  Screen 3    │ Row 0      │  │
 │  │  │  (Output 1)  │  (Output 2)  │  (Output 3)  │            │  │
 │  │  ├──────────────┼──────────────┼──────────────┤            │  │
 │  │  │  Screen 4    │  Screen 5    │  Screen 6    │ Row 1      │  │
 │  │  │  (Output 4)  │  (Output 5)  │  (Output 6)  │            │  │
 │  │  └──────────────┴──────────────┴──────────────┘            │  │
 │  │      Column 0       Column 1       Column 2                │  │
 │  └────────────────────────────────────────────────────────────┘  │
 │                               ▲                                  │
 │                               │ Các cổng cáp HDMI / DVI Out      │
 │  ┌────────────────────────────┴───────────────────────────────┐  │
 │  │         BỘ ĐIỀU KHIỂN TRUNG TÂM (CONTROLLER / DECODER)     │  │
 │  │  - Slot 1: Card Giải mã IP (Digital Input Channels)        │  │
 │  │  - Slot 2: Card Đầu vào HDMI/DVI (Analog Input Channels)   │  │
 │  │  - Slot 3, 4: Card Đầu ra (Output Ports gắn tới màn hình)  │  │
 │  └────────────────────────────────────────────────────────────┘  │
 └──────────────────────────────────────────────────────────────────┘
```

### 2.1. Video Wall (Tường Màn hình / Màn hình ghép)
- **Định nghĩa:** Là một bức tường hiển thị cỡ lớn được tạo thành từ nhiều tấm màn hình LCD hoặc module LED ghép liền nhau.
- **WallNo / VideoWall ID:** Số thứ tự định danh tường màn hình trong bộ điều khiển (thường bắt đầu từ `1`). Một bộ điều khiển cao cấp có thể quản lý nhiều tường màn hình độc lập (`Wall 1`, `Wall 2`).

### 2.2. Controller (Bộ điều khiển Video Wall / Decoder Matrix)
- **Định nghĩa:** Thiết bị phần cứng chuyên dụng chịu trách nhiệm:
  1. Nhận tín hiệu video từ nhiều nguồn (Camera IP, Máy tính điều hành, Bản đồ GIS).
  2. Giải mã (Decode) luồng nén (H.264, H.265, 4K).
  3. Cắt lát hình ảnh và xuất ra các cổng màn hình ghép theo đúng tọa độ thời gian thực.

### 2.3. Slot & SlotPort (Khe cắm & Cổng trên Card mở rộng)
- **Slot (SlotNo):** Khung cắm card dạng phiến (blade/sub-board) trên thân Controller. Ví dụ: Slot 1 chứa card giải mã, Slot 2 chứa card xuất HDMI.
- **Port / SlotPort:** Cổng vật lý trên từng card (ví dụ: Slot 1 có 4 cổng HDMI Input, Slot 3 có 4 cổng DVI Output).

### 2.4. Screen (Tấm Màn hình Ghép Vật lý)
- **Định nghĩa:** Từng tấm màn hình đơn lẻ (ví dụ: màn LCD 55 inch viền siêu mỏng 0.88mm).
- **Tọa độ lưới:** Được định vị trong ma trận tường bằng **`Row` (Hàng)** và **`Column` (Cột)**. Ví dụ: Màn hình góc trên bên trái là `Row=0, Column=0`.

### 2.5. Topology / Wall Scale (Cấu trúc Bố cục Ma trận)
- **Định nghĩa:** Tỷ lệ ma trận ghép của tường. Ví dụ:
  - Ghép $2 \times 3$: Gồm 2 hàng và 3 cột (tổng cộng 6 màn hình).
  - Ghép $3 \times 4$: Gồm 3 hàng và 4 cột (tổng cộng 12 màn hình).
- **Pixel Grid:** Hệ tọa độ phân giải ảo toàn tường. Ví dụ mỗi tấm 1920x1080 thì tường $2 \times 3$ có tổng độ phân giải là $5760 \times 2160$ pixels.

---

## 3. QUẢN LÝ KÊNH TÍN HIỆU ĐẦU VÀO & ĐẦU RA

### 3.1. Analog Input Channels (Kênh Đầu vào Cáp trực tiếp)
- **Định nghĩa:** Các nguồn phát cắm cáp vật lý trực tiếp vào Controller (HDMI IN, DVI IN, VGA IN, DP IN) từ máy chủ máy trạm chỉ huy, máy tính hiển thị web giám sát giao thông.
- **Đánh số:** Thường mang dải ID từ `1` đến `8`.

### 3.2. Digital Input Channels / InputProxy (Kênh Đầu vào Kỹ thuật số / Kênh IP)
- **Định nghĩa:** Các kênh nhận luồng video qua đường truyền mạng IP (Camera giao thông, Đầu ghi NVR, Máy chủ streaming qua RTSP/ONVIF).
- **Đánh số:** Thường mang dải ID tiếp theo từ `9` đến `32...` hoặc `64`.

### 3.3. Output Channels / Output Ports (Cổng Xuất Tín hiệu Màn hình)
- **Định nghĩa:** Các cổng cắm cáp từ Controller kéo thẳng đến từng cổng HDMI/DVI trên mỗi tấm màn hình ghép.
- **Quy tắc ánh xạ (Linkage):** Cổng Output $N$ của Controller bắt buộc phải được cấu hình gắn đúng vào tấm màn hình `Screen (Row, Column)` tương ứng trên tường để hình ảnh không bị hiển thị đảo lộn vị trí.

---

## 4. QUẢN LÝ CỬA SỔ, PHÂN LỚP & CẮT LÁT

```text
 ┌─────────────────────────────────────────────────────────────┐
 │               MÀN HÌNH GHÉP (TỔNG THỂ 2x3)                  │
 │                                                             │
 │   ┌───────────────────────────┐    ┌────────────────────┐   │
 │   │ Cửa sổ 1 (Camera Cao tốc) │    │ Cửa sổ 2           │   │
 │   │ ┌───────────┬───────────┐ │    │ (Bản đồ GIS)       │   │
 │   │ │ Sub-Win 1 │ Sub-Win 2 │ │    │ (Cửa sổ đơn)       │   │
 │   │ ├───────────┼───────────┤ │    │                    │   │
 │   │ │ Sub-Win 3 │ Sub-Win 4 │ │    │                    │   │
 │   │ └───────────┴───────────┘ │    │                    │   │
 │   │   (Chia 4 ô / 4-Split)    │    │                    │   │
 │   └───────────────────────────┘    └────────────────────┘   │
 │                                                             │
 │          ┌──────────────────────────────────────┐           │
 │          │ Cửa sổ 3 (Phóng to đè lên trên - TOP)│           │
 │          │        (Cửa sổ Trôi - Roaming)       │           │
 │          └──────────────────────────────────────┘           │
 └─────────────────────────────────────────────────────────────┘
```

### 4.1. Window (Cửa sổ Hiển thị Chính - Main Window)
- **Định nghĩa:** Là một khung chữ nhật ảo được mở trên tường Video Wall để trình chiếu video.
- **Đặc tính Tự do (Roaming & Spanning):** Cửa sổ có thể nằm gọn trong 1 tấm màn hình, hoặc kéo giãn **phóng to đè qua 2, 4, 6 tấm màn hình cùng lúc (Spanning across screens)** mà hình ảnh vẫn liền mạch không bị đứt đoạn.

### 4.2. Sub-Window (Cửa sổ Phụ / Chia Khung hình)
- **Định nghĩa:** Là các ô chia nhỏ bên trong 1 Cửa sổ chính.
- **Chế độ chia (Multi-Split):**
  - **1-Split:** Cửa sổ hiển thị 1 luồng video duy nhất.
  - **4-Split (Quad):** Chia cửa sổ thành 4 ô vuông nhỏ, mỗi ô phát 1 Camera khác nhau.
  - **9-Split / 16-Split:** Chia thành 9 hoặc 16 ô Camera đồng thời.

### 4.3. Window Coordinates (Tọa độ Hình học Cửa sổ)
Mỗi cửa sổ trên tường được xác định bởi 4 thông số:
1. `CoordinateX`: Tọa độ điểm bắt đầu trục hoành (tính từ mép trái tường).
2. `CoordinateY`: Tọa độ điểm bắt đầu trục tung (tính từ mép trên tường).
3. `WindowWidth`: Chiều rộng của cửa sổ.
4. `WindowHeight`: Chiều cao của cửa sổ.

### 4.4. Z-Index / Layering (Thứ tự Xếp chồng Lớp)
Khi có nhiều cửa sổ nằm đè lên nhau:
- **Topmost (`BringWindowToTop`):** Đưa cửa sổ lên lớp trên cùng để không bị cửa sổ khác che khuất (thường dùng khi có sự cố khẩn cấp cần phóng to 1 camera lên trung tâm).
- **Bottommost (`SendWindowToBottom`):** Đưa cửa sổ xuống lớp dưới cùng làm nền (Background).

### 4.5. Signal Source Switching (Chuyển đổi Nguồn Tín hiệu)
- **Định nghĩa:** Hành động thay đổi luồng camera hoặc tín hiệu máy tính đang chiếu trong 1 cửa sổ mà không cần phải xóa cửa sổ đó đi tạo lại.

---

## 5. KỊCH BẢN HIỂN THỊ, LUỒNG CÔNG VIỆC & LẬP LỊCH

### 5.1. Scene (Kịch bản Hiển thị / Bố cục Mẫu)
- **Định nghĩa:** Là "bức ảnh chụp nhanh" (Snapshot) lưu lại toàn bộ trạng thái của tường màn hình tại một thời điểm: Có bao nhiêu cửa sổ, vị trí tọa độ của từng cửa sổ, và mỗi cửa sổ đang phát camera nào.
- **Ví dụ thực tế:**
  - `Scene 1 (Giờ cao điểm)`: Mở 16 camera tại các nút giao trọng điểm.
  - `Scene 2 (Ban đêm)`: Mở bản đồ sự cố toàn tuyến và 4 camera trạm thu phí.
  - `Scene 3 (Khẩn cấp)`: Phóng to toàn màn hình camera nơi xảy ra tai nạn giao thông.

### 5.2. Scene SaveData & Scene Activate
- **`SaveData`:** Lưu bố cục cửa sổ đang hiển thị trên tường vào bộ nhớ của Controller thành một mã Scene ID.
- **`Activate`:** Lệnh chuyển đổi tường màn hình sang bố cục của Scene ID mong muốn (thời gian chuyển cảnh thường diễn ra tức thì dưới 1 giây).

### 5.3. Schedule / Patrol (Lập lịch & Tuần tra Tự động)
- **Định nghĩa:** Cơ chế tự động luân phiên kích hoạt các Scene theo một thời gian biểu định sẵn (ví dụ: Tự động đổi giữa Scene 1, Scene 2, Scene 3 mỗi 30 giây).

### 5.4. Event Rule & Trigger Linkage (Quy tắc Sự kiện & Tự động hóa)
- **Định nghĩa:** Cơ chế liên kết giữa các cảm biến / hệ thống ngoại vi với Video Wall:
  - *Trigger:* Nhận tin nhắn sự kiện từ phân hệ TMS/FMS (như *"Phát hiện xe đi ngược chiều"*, *"Cháy trong hầm"*).
  - *Action:* Video Wall tự động nhảy sang Scene cảnh báo hoặc tự động mở cửa sổ camera tại vị trí sự cố lên tường màn hình trung tâm.

---

## 6. BẢO MẬT, XÁC THỰC & QUẢN TRỊ THIẾT BỊ

### 6.1. Device Activation (Kích hoạt Thiết bị Mới)
- Thiết bị mới xuất xưởng ở trạng thái `Inactive`.
- Quá trình kích hoạt sử dụng giao thức bắt tay mã hóa **RSA-1024 + AES-128 ECB**:
  1. Client gửi Public Key $\rightarrow$ Thiết bị gửi chuỗi ngẫu nhiên đã mã hóa RSA.
  2. Client giải mã lấy AES Key $\rightarrow$ Mã hóa mật khẩu `admin` gửi lại thiết bị để kích hoạt.

### 6.2. Circuit Breaker (Bộ Ngắt mạch Bảo vệ Chống Khóa Thiết bị)
- **Vấn đề an ninh của Hikvision:** Nếu một phần mềm thử sai mật khẩu 2 - 3 lần liên tiếp, thiết bị Hikvision sẽ kích hoạt cơ chế phòng vệ **khóa cứng IP đăng nhập trong 30 phút**.
- **Giải pháp Circuit Breaker trong WebAPI:**
  - Hệ thống tự đếm số lần lỗi `401 Unauthorized` hoặc `403 Forbidden`.
  - Nếu lỗi đạt $\ge 2$ lần liên tiếp $\rightarrow$ Ngắt mạch (Open Circuit) trong 5 phút, chặn không cho gửi request tiếp để bảo vệ IP của WebAPI không bị thiết bị khóa vĩnh viễn.
  - Sau 5 phút, trạng thái trong Cache (Redis) tự động hết hạn (TTL Expire) và phục hồi bình thường.

### 6.3. devIndex (Transparent Forwarding Identifier)
- **Định nghĩa:** Mã định danh do NVR/Gateway cấp cho các Camera con nằm trong mạng POE cô lập.
- Khi gửi request `GET /ISAPI/System/deviceInfo?devIndex=12345678` tới NVR, NVR sẽ đóng vai trò Proxy chuyển tiếp lệnh trực tiếp vào Camera tương ứng.

### 6.4. NTP Time Sync (Đồng bộ Thời gian)
- **NTP Client:** Controller đồng bộ giờ từ máy chủ NTP trung tâm (ví dụ máy chủ ITS).
- **NTP Server Mode:** Controller đóng vai trò máy chủ thời gian Master, tự động phát lệnh đồng bộ giờ xuống toàn bộ camera trong hệ thống (`SyncDeviceNTPInfoToCamera`).

### 6.5. Mutex Functions (Tính năng Loại trừ Lẫn nhau)
- Các tính năng AI/xử lý hình ảnh xung đột phần cứng (ví dụ: đang bật tính năng Bảo vệ chu vi thì thiết bị tự động vô hiệu hóa tính năng Đếm người do giới hạn chip xử lý).

---

## 7. BẢNG TRA CỨU NHANH CÁC TỪ VIẾT TẮT

| Thuật ngữ | Tên Tiếng Anh Đầy Đủ | Ý nghĩa trong Hệ thống Video Wall |
| :--- | :--- | :--- |
| **ISAPI** | Intelligent Security API | Giao thức RESTful HTTP/XML chuẩn điều khiển thiết bị Hikvision. |
| **SADP** | Search Active Device Protocol | Giao thức dò tìm và cấu hình IP thiết bị trong mạng Layer 2. |
| **NVR** | Network Video Recorder | Đầu ghi hình mạng (quản lý và lưu trữ luồng Camera IP). |
| **IPC** | IP Camera | Camera kỹ thuật số truyền dữ liệu qua mạng IP. |
| **VMS** | Video Management System | Hệ thống phần mềm quản lý video tổng thể. |
| **TOC** | Traffic Operations Center | Trung tâm Giám sát Điều hành Giao thông. |
| **PTZ** | Pan - Tilt - Zoom | Camera có khả năng quay quét trái phải, gật gù và phóng to hình ảnh. |
| **OSD** | On-Screen Display | Chữ, thời gian hoặc logo được gắn đè trực tiếp lên hình ảnh video. |
| **VCA** | Video Content Analysis | Phân tích nội dung video thông minh (nhận diện biển số, phát hiện sự cố). |
| **RTSP** | Real-Time Streaming Protocol | Giao thức truyền phát luồng video trực tiếp thời gian thực. |
| **CSR** | Certificate Signing Request | Đơn tạo yêu cầu cấp phát chứng chỉ số SSL/TLS gửi cơ quan CA. |
| **CA** | Certificate Authority | Tổ chức/Cơ quan cấp phát và chứng thực chữ ký số bảo mật. |
| **DST** | Daylight Saving Time | Chế độ điều chỉnh giờ mùa hè theo khu vực địa lý. |
| **TTL** | Time To Live | Thời gian tồn tại tự động của dữ liệu trong bộ nhớ Cache / Redis. |
| **PiP** | Picture in Picture | Chế độ hiển thị hình ảnh lồng trong hình ảnh (cửa sổ nhỏ nằm trong cửa sổ to). |
| **BNC / HDMI / DVI / DP** | Bayonet Neill–Concelman / High-Definition Multimedia Interface / Digital Visual Interface / DisplayPort | Các chuẩn giao tiếp cáp truyền dẫn tín hiệu hình ảnh phần cứng. |
| **RS-232 / RS-485 / RS-422** | Recommended Standard 232 / 485 / 422 | Chuẩn giao tiếp truyền thông nối tiếp (Serial Port) kết nối cảm biến/bàn phím điều khiển. |

---

## 8. CÂY PHỤ THUỘC GỘP & MAP SANG CODE THỰC TẾ

> Mục này gộp quan hệ giữa các khái niệm ở mục 1-6 thành **1 sơ đồ duy nhất**, và chỉ ra
> đúng chỗ từng khái niệm nằm trong mã nguồn `Module.VideoWall.WPF` (công cụ WPF đang dùng để
> test trực tiếp DS-C30S-S11). Đọc mục này SAU khi đã đọc qua mục 2, 4, 5 — nó không thay thế
> phần định nghĩa, chỉ nối các định nghĩa đó lại với nhau.

### 8.1. Cây phụ thuộc (từ to tới nhỏ)

```text
Controller (bộ điều khiển — 1 con DS-C30S-S11, gõ IP/Port/Account/Password để nối)
 │
 ├── Slot & SlotPort (khe cắm card + cổng vật lý trên card)
 │     ├── Input Channel (cổng vào — HDMI/DVI/DP hoặc luồng IP)
 │     └── Output Port (cổng ra — dây kéo tới 1 màn hình thật)
 │
 └── Video Wall / WallNo  ← 1 controller có TỐI ĐA 8 Wall, ĐỘC LẬP nhau
       │
       ├── Topology / Wall Scale (bố cục lưới Row×Column của Wall đó)
       │     └── Screen (từng tấm màn hình thật, gắn vào 1 Output Port,
       │                  có toạ độ Row/Column trong Wall)
       │
       └── Scene (kịch bản — LUÔN thuộc về đúng 1 Wall, không có Scene "lơ lửng")
             │
             └── Window (cửa sổ — thuộc về 1 Scene, toạ độ X/Y/W/H tuyệt đối
                          trên Wall, có thể tràn qua nhiều Screen = "Spanning")
                   │
                   ├── Signal Source (nguồn đang chiếu trong Window đó,
                   │                   lấy từ 1 Input Channel)
                   ├── Z-Index / Layering (thứ tự chồng lớp khi 2 Window đè nhau)
                   └── Sub-Window (chia nhỏ bên trong 1 Window: 1/4/9/16-split)
```

**Quy tắc quan trọng nhất rút ra từ cây này**: Scene và Window **luôn buộc chặt vào 1 Wall
cụ thể**. Mọi giới hạn (`maxWindowNums`, `maxSceneNums`) cũng tính **theo từng Wall**, không
cộng dồn toàn thiết bị — nên chọn đúng WallNo trước khi thao tác là bắt buộc, không phải tuỳ chọn.

### 8.2. Map keyword → chỗ trong code/UI

| Keyword (mục tham chiếu) | Trong code/UI `Module.VideoWall.WPF` | Ghi chú |
|---|---|---|
| **Controller** (§2.2) | Thanh kết nối trên cùng MainWindow (IP/Port/Account/Password) | Không còn `VwControllerDto`/danh sách chọn từ CSDL — đã bỏ Backend Mode |
| **Video Wall / WallNo** (§2.1) | `ConnectionViewModel.WallNo`, `ProbeResult.Walls` | Phải nhập tay, không tự chọn — tránh đẩy nhầm Wall |
| **Screen** (§2.4) | `VwScreenDto` + `VwLocalScreenStore` (file JSON local) — nút "➕ Thêm màn" / "🗑 Xoá màn" ở Tab 1 (đọc từ `ProbeResult.Outputs`) | |
| **Input Channel → Signal Source** (§3.1, §3.2) | `VwSourceDto`, lấy trực tiếp từ `ProbeResult.InputChannels` mỗi lần Probe | Tự động đồng bộ khi Probe — luôn là dữ liệu thật của thiết bị |
| **Scene** (§5.1) | `VwSceneDto` + `VwLocalSceneStore` — mục "1. Thiết lập Kịch bản (Scene)" Tab 1 | |
| **Window / Sub-Window** (§4.1, §4.2) | `VwWindowSceneDto` — mục "2. Dựng cửa sổ" Tab 1 | Chế độ "Màn KHÔNG chồng" = 1 Window/Screen ("▶ Dựng cửa sổ phủ kín"); "Màn CHỒNG" = nhập tay X/Y/W/H/ZIndex ("▶ Dựng cửa sổ xếp lớp") |
| **Z-Index / Layering** (§4.4) | `ZIndex` trên Window | Dùng ở chế độ "Màn CHỒNG" Tab 1, kịch bản "2 nguồn tranh vùng" Tab 2 |
| **SaveData / Activate** (§5.2) | Nút "🚀 ĐẨY XUỐNG THIẾT BỊ" (`PushToDevice`) | Tạo Window thật trên thiết bị; Activate = cờ `ActivateAfterPush` |
| **ResponseStatus / statusCode** (§1.5) | `VwSetupSceneStep.HttpStatus` / `.Message` | Hiện ở khung Log; "Bộ kiểm thử lỗi" Tab 2 đọc field này để in `[PASS]`/`[FAIL]` |
| **Digest Authentication** (§1.3) | `VwDirectDigestHandler` | MockServer local mặc định KHÔNG kiểm tra hash password thật (`VerifyDigestResponseHash=false`) — thiết bị thật thì có |
| **Circuit Breaker** (§6.2) | `FailedAuthLockoutThreshold` trong MockServer | Chỉ bật được qua `dotnet test`, không qua UI |
| **Event Rule & Trigger Linkage** (§5.4) | ~~Đã xoá khỏi app~~ | Thuộc tầng Backend/DB — ngoài phạm vi test Direct Mode (§A3 `videowall-record-replay.md`) |
| **Schedule / Patrol** (§5.3) | Tab "Lịch" (`ScheduleViewModel`) | Chưa gắn vào MainWindow — orphan, ngoài phạm vi hiện tại |

---
*Tài liệu được biên soạn đồng bộ với mã nguồn phân hệ `Module.VideoWall` tại WebAPI `TA-ITS015-WEBAPI-V1.0`.*
