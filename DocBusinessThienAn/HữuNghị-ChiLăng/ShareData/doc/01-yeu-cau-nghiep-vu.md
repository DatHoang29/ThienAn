# NGHIÊN CỨU: PHẦN MỀM CHIA SẺ DỮ LIỆU (ITS)

**Dự án:** Cao tốc Hữu Nghị – Chi Lăng - Này là yêu cầu hệ thống
**Tài liệu gốc:** III.2. CHỈ DẪN KỸ THUẬT - HỆ THỐNG GIAO THÔNG THÔNG MINH_HN-CL.pdf  
**Mã phần mềm:** TMC-PM-ITS-ESHARE | Model: TA-ShareData | Hãng: Thiên Ân  
**Ngày nghiên cứu:** 2025

---

## MỤC LỤC

1. [Tổng quan phần mềm chia sẻ dữ liệu](#1-tổng-quan-phần-mềm-chia-sẻ-dữ-liệu)
2. [Chức năng phần mềm](#2-chức-năng-phần-mềm)
3. [Khả năng chia sẻ dữ liệu](#3-khả-năng-chia-sẻ-dữ-liệu)
4. [Nguyên tắc đặt tên](#4-nguyên-tắc-đặt-tên)
5. [Nguyên tắc đường truyền](#5-nguyên-tắc-đường-truyền)
6. [Nguyên tắc bảo mật](#6-nguyên-tắc-bảo-mật)
7. [Tiêu chuẩn áp dụng](#7-tiêu-chuẩn-áp-dụng)
8. [Kiến trúc hệ thống](#8-kiến-trúc-hệ-thống)
9. [Cấu trúc dữ liệu DATEX-ASN](#9-cấu-trúc-dữ-liệu-datex-asn)

---



## 1. Tổng quan phần mềm chia sẻ dữ liệu

### 1.1. Vị trí trong kiến trúc phần mềm ITS

Phần mềm chia sẻ dữ liệu (Data Sharing Software) là một trong các phân hệ phần mềm thuộc lớp **ITS Core** trong kiến trúc phần mềm hệ thống giao thông thông minh (Hình 20, trang 203). Phần mềm nằm trong nhóm ứng dụng TMC (Traffic Management Center) và có nhiệm vụ:

- Kết nối và trao đổi dữ liệu giữa **Trung tâm QLĐHGT tuyến (TMC tuyến)** và **Trung tâm QLĐHGT Quốc gia**
- Chia sẻ dữ liệu với các đối tác bên ngoài (cảnh sát giao thông, công chúng qua Internet)
- Đảm bảo tính liên thông (interoperability) giữa các hệ thống ITS khác nhau

### 1.2. Đặc điểm chung (trang 204)

- **Thiết kế module hóa** (modular design)
- Hỗ trợ **truyền file hai chiều** (bidirectional file transfer)
- Cung cấp **dịch vụ Internet**
- Hỗ trợ **đa ngôn ngữ** (Tiếng Anh/Tiếng Việt)
- Bảo mật dữ liệu **3 cấp độ** (nhân viên vận hành / trưởng ca / quản lý)
- Kiến trúc mở cho chia sẻ dữ liệu với trung tâm quốc gia

### 1.3. Tham chiếu trong tài liệu

| Nguồn | Vị trí | Nội dung |
|--------|--------|----------|
| Chỉ dẫn kỹ thuật | Trang 171 | Yêu cầu thông số kỹ thuật tối thiểu |
| Mục 4.4.8 | Trang 193-201 | Yêu cầu phương án kết nối TMC – Quốc gia |
| Mục 4.4.9 | Trang 202-216 | Hệ thống phần mềm (kiến trúc) |
| Mục 1419 | Trang 543-572 | Chuẩn kết nối trung tâm tuyến quốc gia |
| Excel YCKT | Row 181-183 | Mã TMC-PM-ITS-ESHARE, 2 chức năng |

---



## 2. Chức năng phần mềm

Theo yêu cầu thông số kỹ thuật tối thiểu (trang 171) và xác nhận từ file Excel YCKT (Row 182-183), phần mềm chia sẻ dữ liệu có **2 chức năng chính**:

### 2.1. Quản lý giao diện chia sẻ dữ liệu tại trung tâm điều hành

Chức năng này cung cấp giao diện quản lý tại TMC để:
- Thiết lập và giám sát các kết nối chia sẻ dữ liệu
- Hiển thị trạng thái kết nối (phiên làm việc đang hoạt động, bị ngắt, v.v.)
- Quản lý đăng ký (subscription) nhận/gửi dữ liệu
- Cấu hình các tham số kết nối (heartbeat, datagram size, encoding rules)
- Giám sát luồng dữ liệu giữa TMC tuyến và Trung tâm Quốc gia

### 2.2. Quản lý lịch sử chia sẻ

Chức năng này lưu trữ và quản lý nhật ký (log) các hoạt động chia sẻ:
- Lịch sử các phiên kết nối (session) — thời gian bắt đầu/kết thúc, lý do ngắt
- Lịch sử các gói dữ liệu đã gửi/nhận (FrED confirmation numbers)
- Lịch sử đăng ký/hủy đăng ký (subscription/cancellation)
- Lịch sử xuất bản dữ liệu (publication) — đơn lẻ và định kỳ
- Tra cứu và báo cáo theo thời gian, loại dữ liệu, đối tác

> **Tham chiếu:** Mục 1404D. Trung tâm quản lý điều hành giao thông (TMC), tiểu mục 4.7.7 Hệ thống phần mềm

---



## 3. Khả năng chia sẻ dữ liệu

### 3.1. Các loại dữ liệu chia sẻ (11 loại — Bảng 1, trang 548-549)

> **LƯU Ý QUAN TRỌNG (Cập nhật theo giao diện thực tế dự án):**
> Dải ID từ 101-111 trong tài liệu gốc ISO 14827 đã được dự án **chủ động tùy biến** lại để hỗ trợ các nghiệp vụ đặc thù (như Thu phí, Định danh xe, Tải trọng) mà không phá vỡ cấu trúc tổng thể. Dưới đây là bảng ánh xạ thực tế đang được sử dụng trong hệ thống (cả UI và Backend Code):

| ID | Loại dữ liệu thực tế dự án | Mã Code Backend (`DatatypeIdEnum`) | Ghi chú so với chuẩn gốc |
|----|----------------------------------------|-----------------------|--------------------------|
| 101 | Thông tin chung / luồng giao thông | `TrafficFlow` | Giữ nguyên chuẩn |
| 102 | Dữ liệu hình ảnh giao thông (CCTV) | `CctvImage` | Tùy biến (Gốc: VMS) |
| 103 | Dữ liệu dò xe (VDS) | `VehicleDetection` | Tùy biến (Gốc: Sự cố) |
| 104 | Dữ liệu thời tiết | `Weather` | Tùy biến (Gốc: Sự cố bổ sung) |
| 105 | Dữ liệu định danh phương tiện (AVI) | `VehicleIdentification` | Tùy biến (Gốc: Tình trạng đường) |
| 106 | Dữ liệu kiểm tra tải trọng xe (WIM) | `WeighInMotion` | Tùy biến (Gốc: Thời tiết) |
| 107 | Thông tin sự kiện giao thông | `TrafficIncident` | Tùy biến (Gốc: Quản lý đường) |
| 108 | Thông tin hiển thị trên biển báo điện tử | `VmsDisplay` | Tùy biến (Gốc: Đỗ xe) |
| 109 | Thông tin thu phí (ETC) | `TollCollection` | Tùy biến (Gốc: Phát hiện xe) |
| 110 | Trao đổi thông tin với người TGGT | `PublicMessaging` | Tùy biến (Gốc: Hình ảnh/video) |
| 111 | Trao đổi thông tin TT QLĐHGT tuyến | `InterCenterExchange` | Tùy biến (Gốc: Khác) |

### 3.2. Luồng chia sẻ dữ liệu (Hình 2, trang 548)

Hệ thống hỗ trợ **3 kiểu luồng dữ liệu**:

1. **TMC tuyến → Trung tâm Quốc gia** (báo cáo lên — upward)
2. **Trung tâm Quốc gia → TMC tuyến** (điều khiển xuống — downward)
3. **TMC tuyến ↔ TMC tuyến** (ngang hàng — peer-to-peer, qua Trung tâm Quốc gia)

### 3.3. Đối tác chia sẻ (trang 204, 217)

- **Trung tâm QLĐHGT Quốc gia** — đối tác chính, kết nối qua giao thức chuẩn
- **Các TMC tuyến khác** — chia sẻ qua trung tâm quốc gia
- **Cảnh sát giao thông** — chia sẻ hình ảnh VDS, thông tin sự cố
- **Nhà điều hành (operators)** — truy cập dữ liệu theo phân quyền
- **Công chúng** — chia sẻ qua Internet (thông tin giao thông, tình trạng đường)

### 3.4. Chế độ xuất bản dữ liệu (Publication Mode)

| Chế độ | Mô tả |
|--------|--------|
| Single | Gửi một lần duy nhất theo yêu cầu |
| Event-driven | Gửi khi có sự kiện xảy ra (registered) |
| Periodic | Gửi định kỳ theo lịch đăng ký (registered) — hỗ trợ cấu hình ngày trong tuần, thời gian bắt đầu/kết thúc |

---



## 4. Nguyên tắc đặt tên

### 4.1. Quy ước đặt tên trường dữ liệu DATEX-ASN

Dựa trên cấu trúc dữ liệu DATEX-ASN (Section 4, trang 562-572), các trường dữ liệu tuân theo quy tắc đặt tên thống nhất:

**Tiền tố `datex-` + Chức năng + Hậu tố kiểu dữ liệu**

| Hậu tố | Ý nghĩa | Ví dụ |
|--------|---------|-------|
| `-txt` | Chuỗi văn bản (text) | `datex-Sender-txt`, `datex-Destination-txt`, `datex-Origin-txt` |
| `-nbr` | Số thứ tự (number) | `datexDataPacket-nbr`, `datexPublish-Serial-nbr` |
| `-cd` | Mã phân loại (code) | `datexDataPacketPriority-cd`, `datexPublish-Management-cd` |
| `-qty` | Số lượng (quantity) | `datexLogin-HeartbeatDurationMax-qty`, `datexRegistered-UpdateDelay-qty` |
| `-bool` | Giá trị logic (boolean) | `datexSubscribe-Persistent-bool`, `datexPublish-Guaranteed-bool` |
| `-id` | Định danh (identifier) | `datex-Crc-id`, `datexAccept-Login-id` |
| `-loc` | Địa chỉ logic (location) | `datex-OriginAddress-loc`, `datex-DestinationAddress-loc` |

### 4.2. Quy ước đặt tên cấu trúc (Type names)

Tên các kiểu dữ liệu sử dụng **PascalCase** theo chuẩn ASN.1:

| Cấu trúc | Mô tả |
|-----------|--------|
| `DatexDataPacket` | Gói dữ liệu tổng quát |
| `C2CAuthenticatedMessage` | Thông điệp xác thực C2C |
| `HeaderOptions` | Tùy chọn header |
| `SubscriptionData` | Dữ liệu đăng ký |
| `SubscriptionMode` | Chế độ đăng ký |
| `PublicationData` | Dữ liệu xuất bản |
| `PublicationType` | Loại xuất bản |
| `EndApplicationMessage` | Thông điệp kết thúc ứng dụng |

### 4.3. Quy ước đặt tên PDU (Protocol Data Unit)

Các đơn vị giao thức sử dụng tên ngắn gọn theo chức năng:

```
PDUs ::= CHOICE {
    initiate       Initiate,
    login          Login,
    fred           FrED,
    terminate      Terminate,
    logout         Logout,
    subscription   Subscription,
    publication    Publication,
    transfer-done  TransferDone,
    accept         Accept,
    reject         Reject
}
```

### 4.4. Nguyên tắc xây dựng thông điệp (Section 3.3, trang 560-561)

Theo TCVN 12192:2018, mỗi thông điệp ITS phải tuân thủ:

1. **Cấu trúc bắt buộc**: gồm các thuộc tính siêu dữ liệu (metadata) + thuộc tính thông điệp + phần chính (body)
2. **Thuộc tính bắt buộc** (mandatory attributes): phải có trong mọi thông điệp
3. **Thuộc tính tùy chọn** (optional attributes): mở rộng theo nhu cầu
4. **Thuộc tính mở rộng** (extension attributes): cho khả năng mở rộng tương lai
5. **Phần chính** gồm: Khung dữ liệu (Data Frame) + Phần tử dữ liệu (Data Element)

> **Hình 10 (trang 561):** Cấu trúc Thông điệp ITS
> ```
> Thông điệp ITS
> ├── Các thuộc tính siêu dữ liệu
> │   ├── Thuộc tính bắt buộc
> │   ├── Thuộc tính tùy chọn
> │   └── Thuộc tính mở rộng
> ├── Các thuộc tính thông điệp
> └── Phần chính của thông điệp
>     ├── Khung dữ liệu
>     └── Phần tử dữ liệu
> ```

### 4.5. Nguyên tắc mã hóa (Section 3.3.2, trang 561)

- Các thông điệp được mã hóa sử dụng cú pháp **ASN.1** (Abstract Syntax Notation One)
- Đặc tả đối tượng thông tin **ASN.1 (IOS)** được sử dụng để kết hợp đặc tả các thuộc tính và phần chính thành khuôn dạng hoàn thiện của thông điệp logic
- Mỗi thông điệp bao gồm đặc tả siêu dữ liệu về thông điệp, các yêu cầu trao đổi dữ liệu, và đặc tả phần chính

---



## 5. Nguyên tắc đường truyền

### 5.1. Yêu cầu môi trường kết nối (Section 2.2, trang 550)

- Hạ tầng truyền dẫn số (digital backbone) với **băng thông cao**
- Đường truyền chuyên dụng (dedicated) hoặc qua Internet
- Đảm bảo an ninh mạng (network security)
- Thống nhất chuẩn mạng truyền thông
- Giao diện mở theo tiêu chuẩn (open interface standards)

### 5.2. Giao thức truyền thông (Section 3.1, trang 554-556)

**Giao thức chính:** ISO 14827 (Parts 1, 2, 3) trên nền **TCP/IP**

**Mô hình Client-Server:**
- **Server**: Khởi động ứng dụng từ yêu cầu của Client
- **Client**: Khởi tạo kết nối và chấp nhận dữ liệu

**Luồng trao đổi dữ liệu (Hình 8, trang 556):**
```
Client                          Server
  │                               │
  ├── Start Request ────────────► │
  │                               │
  ├── Login ────────────────────► │
  │                               │
  │ ◄──────────── Accept/Reject ──┤
  │                               │
  │ ◄──── FrED Response Data ─────┤
  │                               │
  ├── End Request ──────────────► │
  │                               │
  ├── LogOut ───────────────────► │
  │                               │
  │ ◄──── FrED Response Data ─────┤
  │                               │
```

### 5.3. Vòng đời phiên kết nối (Session Lifecycle)

#### 5.3.1. Khởi tạo phiên (Session Initialization — trang 557)

1. Client gửi gói **Initiate** chứa `datex-Sender-txt` và `datex-Destination-txt`
2. Xử lý firewall: mở cổng kết nối
3. Client gửi gói **Login** chứa:
   - `datexLogin-UserName-txt` — tên đăng nhập
   - `datexLogin-Password-txt` — mật khẩu
   - `datexLogin-EncodingRules-id` — quy tắc mã hóa
   - `datexLogin-HeartbeatDurationMax-qty` — khoảng heartbeat tối đa
   - `datexLogin-DatagramSize-qty` — kích thước datagram tối đa
   - `datexLogin-ResponseTimeOut-qty` — thời gian chờ phản hồi
   - `datexLogin-Initiator-cd` — bên khởi tạo (serverInitiated/clientInitiated)
4. Server xác thực và trả về **Accept** hoặc **Reject**

#### 5.3.2. Duy trì phiên (Session Maintenance — trang 558)

- Cơ chế **FrED Heartbeat**: gửi gói FrED định kỳ để xác nhận kết nối còn sống
- Xác nhận gói: `FrED_ConfirmPacket_number-ulong` (INTEGER 0..4294967295)
- Gói **DATEX-FrED** được gửi xác nhận cho mỗi datagram nhận được

#### 5.3.3. Kết thúc phiên (Session Termination — trang 558-559)

- Gửi **end-request** sau 2 lần thất bại liên tiếp
- Gửi **Logout** với lý do (`datexLogout-Reason-cd`):
  - `other`, `serverRequested`, `clientRequested`
  - `serverShutdown`, `clientShutdown`
  - `serverCommProblems`, `clientCommProblems`
- Hoặc **Terminate** với lý do (`datexTerminate-Reason-cd`) — cùng danh sách

### 5.4. Quy trình đăng ký và xuất bản dữ liệu (trang 559-560)

#### Đăng ký (Subscription):
1. Client gửi gói **Subscription** ("đăng ký")
2. Server phản hồi **Accept** ("chấp nhận") hoặc **Reject** ("từ chối")
3. Hủy đăng ký qua `datexSubscribe-CancelReason-cd`

#### Xuất bản (Publication):
- **Đơn lẻ (single)**: gửi một lần
- **Định kỳ (periodic/event-driven)**: gửi tự động theo lịch/sự kiện
- Dữ liệu nhỏ hơn datagram tối đa → gửi nguyên gói
- Dữ liệu lớn hơn → chia nhỏ (split) thành nhiều datagram

### 5.5. Định dạng truyền (Publish Format)

Hỗ trợ các định dạng xuất bản:
- **data** — gửi trực tiếp qua gói dữ liệu (SEQUENCE OF PublicationData)
- **datexPublish-FileName-txt** — gửi qua file (UTF8String, tối đa 2000 ký tự tên file)
- Các giao thức truyền file: **FTP**, **TFTP**, **dataPacket**

---



## 6. Nguyên tắc bảo mật

### 6.1. Bảo mật 3 cấp độ truy cập (trang 204)

Hệ thống áp dụng mô hình phân quyền **3 cấp độ**:

| Cấp độ | Vai trò | Quyền hạn |
|--------|---------|-----------|
| Cấp 1 | Nhân viên vận hành (Operator) | Giám sát, xem dữ liệu cơ bản |
| Cấp 2 | Trưởng ca (Shift Leader) | Cấu hình, phê duyệt chia sẻ |
| Cấp 3 | Quản lý (Management) | Toàn quyền, quản trị hệ thống |

### 6.2. Xác thực kết nối (Authentication — trang 557, 563)

Khi thiết lập phiên, bắt buộc xác thực qua gói **Login**:

- **Username** (`datexLogin-UserName-txt`): OCTET STRING
- **Password** (`datexLogin-Password-txt`): OCTET STRING
- **Encoding Rules** (`datexLogin-EncodingRules-id`): SEQUENCE OF OBJECT IDENTIFIER — xác định quy tắc mã hóa được chấp nhận

### 6.3. Mã hóa và xác thực gói tin

- Gói dữ liệu có thể được mã hóa tùy chọn qua **C2CAuthenticatedMessage**:
  - `datex-AuthenticationInfo-txt` — thông tin xác thực (OCTET STRING, SIZE 0..255)
  - `datex-DataPacket-nbr` — số thứ tự gói (INTEGER 0..4294967295)
  - `datex-DataPacketPriority-cd` — mức ưu tiên (INTEGER 0..10)
- Phiên bản giao thức: `datex-Version-cd` (experimental, version-1)
- Kiểm tra CRC: `datex-Crc-id` (OCTET STRING SIZE 2)

### 6.4. Kiểm soát phiên

- **Heartbeat timeout**: nếu không nhận được FrED trong thời gian quy định → phiên bị đóng
- **Response timeout** (`datexLogin-ResponseTimeOut-qty`): INTEGER 0..255 giây
- **Giới hạn phiên** (`maxSessionsReached`): từ chối kết nối khi đạt số phiên tối đa

### 6.5. Lý do từ chối kết nối (Reject — trang 571)

Hệ thống từ chối đăng nhập (`datexReject-Login-cd`) khi:
- `unknownDomainName` — tên miền không xác định
- `accessDenied` — truy cập bị từ chối
- `invalidNamePassword` — sai tên/mật khẩu
- `timeoutTooSmall` / `timeoutTooLarge` — timeout không hợp lệ
- `heartbeatTooSmall` / `heartbeatTooLarge` — heartbeat không hợp lệ
- `sessionExists` — phiên đã tồn tại
- `maxSessionsReached` — đạt giới hạn phiên tối đa

### 6.6. Firewall và an ninh mạng (trang 550, 557)

- Kết nối qua firewall với cơ chế mở cổng có kiểm soát
- Yêu cầu đảm bảo an ninh mạng (network security) cho đường truyền
- Hỗ trợ cả đường truyền chuyên dụng (dedicated) và Internet

---



## 7. Tiêu chuẩn áp dụng

### 7.1. Tiêu chuẩn Việt Nam (TCVN)

| Mã tiêu chuẩn | Nội dung |
|----------------|----------|
| TCVN 10849:2015 | Kiến trúc ITS Việt Nam |
| TCVN 10850:2015 | Giao tiếp truyền thông ITS |
| TCVN 10851:2015 | Trao đổi dữ liệu giữa các trung tâm (C2C) |
| TCVN 10852:2015 | Giao tiếp thiết bị ven đường |
| TCVN 12191:2018 | Kiến trúc ITS mở rộng |
| TCVN 12192:2018 | Thông điệp dữ liệu ITS (ASN.1) |
| TCVN 12836-1:2020 | Kiến trúc ITS (phần 1) |
| TCVN 13599-1:2022 | Trao đổi dữ liệu thiết bị ven đường — SNMP |
| TCVN 13599-2:2022 | Trao đổi dữ liệu thiết bị ven đường — AP-DATEX |
| TCVN 13599-3:2022 | Trao đổi dữ liệu thiết bị ven đường — Phụ lục |
| TCVN 13600-1:2022 | Giao diện dữ liệu trung tâm — Tổng quan |
| TCVN 13600-2:2022 | Giao diện dữ liệu trung tâm — DATEX-ASN |
| TCVN 13600-3:2022 | Giao diện dữ liệu trung tâm — XML Profile A |

### 7.2. Tiêu chuẩn quốc tế (ISO/CEN)

| Mã tiêu chuẩn | Nội dung |
|----------------|----------|
| ISO 14827-1 | Giao diện dữ liệu C2C — Phần 1 |
| ISO 14827-2 | Giao diện dữ liệu C2C — Phần 2 (DATEX-ASN) |
| ISO 14827-3 | Giao diện dữ liệu C2C — Phần 3 |
| ISO 14287 | Chuẩn hóa C2C (Centre-to-Centre) |
| ISO 15784 | Chuẩn hóa C2F (Centre-to-Field) |
| ISO/TC 204 | Ủy ban kỹ thuật ITS (12 nhóm làm việc) |
| DATEX II | Trao đổi dữ liệu giao thông (XML web services) |

### 7.3. Phạm vi áp dụng

Các tiêu chuẩn trên được áp dụng cho **toàn bộ các tuyến cao tốc Bắc-Nam** (trang 551), đảm bảo tính liên thông giữa các TMC tuyến khác nhau và với Trung tâm Quốc gia.

---



## 8. Kiến trúc hệ thống

### 8.1. Mô hình phân cấp (Hierarchical — Hình 1, trang 547)

```
┌─────────────────────────────────────────────────┐
│     Trung tâm QLĐHGT Quốc gia (National)       │
└───────────────────────┬─────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ TMC Khu vực  │ │ TMC Khu vực  │ │ TMC Khu vực  │
│   (Vùng)     │ │   (Vùng)     │ │   (Vùng)     │
└──────┬───────┘ └──────┬───────┘ └──────┬───────┘
       │                │                │
   ┌───┼───┐        ┌───┼───┐        ┌───┼───┐
   ▼   ▼   ▼        ▼   ▼   ▼        ▼   ▼   ▼
 TMC TMC TMC      TMC TMC TMC      TMC TMC TMC
tuyến            tuyến            tuyến
   │                │                │
   ▼                ▼                ▼
Thiết bị         Thiết bị         Thiết bị
ven đường        ven đường        ven đường
```

### 8.2. Kiến trúc kết nối (Hình 7, trang 556)

TMC tuyến kết nối với Trung tâm Quốc gia thông qua **Exterior Connection Server** sử dụng giao thức ASN.1:

```
┌────────────────────┐         ┌────────────────────┐
│    TMC Tuyến       │         │  Trung tâm QG      │
│                    │         │  (tương lai)        │
│  ┌──────────────┐  │         │  ┌──────────────┐  │
│  │  ITS Core    │  │  ASN.1  │  │  ITS Core    │  │
│  │  + Chia sẻ   │──┼─────────┼──│  + Tổng hợp  │  │
│  │    dữ liệu   │  │  TCP/IP │  │    dữ liệu   │  │
│  └──────────────┘  │         │  └──────────────┘  │
│                    │         │                    │
│  Exterior Conn.   │         │  Exterior Conn.   │
│  Server            │         │  Server            │
└────────────────────┘         └────────────────────┘
```

### 8.3. Kiến trúc XML/ASN.1 (Hình 9, trang 557)

Hệ thống hỗ trợ 2 phương thức mã hóa dữ liệu:

| Phương thức | Tiêu chuẩn | Mô tả |
|-------------|------------|--------|
| **DATEX-ASN** | TCVN 13600-2:2022 | Mã hóa nhị phân ASN.1, hiệu quả băng thông |
| **XML Profile A** | TCVN 13600-3:2022 | Mã hóa XML, dễ đọc, tương thích web services |

Cả hai đều sử dụng chung:
- **Data Dictionary** (từ điển dữ liệu)
- **Message Rules** (quy tắc thông điệp)
- **Application Profiles** (hồ sơ ứng dụng)

### 8.4. Mô hình tích hợp (Hình 6, trang 555)

Theo TCVN 10851:2015, trung tâm khu vực tổng hợp dữ liệu từ 4 TMC tuyến, mỗi TMC tuyến phụ trách xử lý sự cố cho tuyến cao tốc của mình.

---



## 9. Cấu trúc dữ liệu DATEX-ASN

### 9.1. Tổng quan (Section 4, trang 562)

Cấu trúc dữ liệu DATEX-ASN theo ISO 14827-2 gồm **12 loại gói dữ liệu** (4.1.1 – 4.1.12):

### 9.2. Danh sách cấu trúc gói dữ liệu

| Mục | Tên | Kiểu ASN.1 | Mô tả |
|-----|-----|-----------|--------|
| 4.1.1 | Cấu trúc tổng quát | `DatexDataPacket ::= SEQUENCE` | Gói dữ liệu ngoài cùng, chứa version, data, CRC, C2C message |
| 4.1.2 | Đơn vị giao thức | `PDUs ::= CHOICE` | 10 loại PDU: Initiate, Login, FrED, Terminate, Logout, Subscription, Publication, TransferDone, Accept, Reject |
| 4.1.3 | Gói khởi tạo | `Initiate ::= SEQUENCE` | Chứa Sender-txt, Destination-txt |
| 4.1.4 | Gói đăng nhập | `Login ::= SEQUENCE` | Chứa username, password, encoding rules, heartbeat, timeout, datagram size, initiator |
| 4.1.5 | FrED xác nhận | `FrED ::= INTEGER (0..4294967295)` | Số xác nhận gói (ConfirmPacket-nbr) |
| 4.1.6 | Gói kết thúc | `Terminate ::= ENUMERATED` | Lý do: other, serverRequested, clientRequested, shutdown, commProblems |
| 4.1.7 | Gói đăng xuất | `Logout ::= ENUMERATED` | Cùng danh sách lý do như Terminate |
| 4.1.8 | Gói đăng ký | `Subscription ::= SEQUENCE` | Serial number, type (subscription/cancel), persistent flag, status, mode, format, priority, guarantee |
| 4.1.9 | Gói xuất bản | `Publication ::= SEQUENCE` | Guaranteed flag, format (data hoặc filename) |
| 4.1.10 | Gói chuyển hoàn tất | `TransferDone ::= SEQUENCE` | FileName-txt, Success-bool |
| 4.1.11 | Gói chấp nhận | `Accept ::= SEQUENCE` | Packet-nbr, acceptType (Login-id, single-subscription, Registered-nbr, publication) |
| 4.1.12 | Gói từ chối | `Reject ::= SEQUENCE` | Packet-nbr, rejectType (Login-cd, Subscription-cd, Publication-cd), alternateRequest |

### 9.3. Chi tiết Subscription Mode (trang 566)

```asn1
SubscriptionMode ::= CHOICE {
    single          Null,           -- Gửi một lần
    event-driven    Registered,     -- Gửi khi có sự kiện
    periodic        Registered      -- Gửi định kỳ
}

Registered ::= CHOICE {
    continuous  SEQUENCE {          -- Liên tục
        datexRegistered-UpdateDelay-qty  INTEGER (0..4294967295) DEFAULT 0,
        datexRegistered-StartTime        Time    OPTIONAL,
        datexRegistered-EndTime          Time    OPTIONAL
    },
    daily       SEQUENCE {          -- Hàng ngày
        datexRegistered-UpdateDelay-qty  INTEGER (0..4294967295) DEFAULT 0,
        DaysOfWeek-cd                    BIT STRING (SIZE(8)),
        datexRegistered-StartDate        Time    OPTIONAL,
        datexRegistered-EndDate          Time    OPTIONAL,
        datexRegistered-StartTime        Time    OPTIONAL,
        datexRegistered-Duration-qty     INTEGER (0..65535) OPTIONAL
                                         -- defaults to 1440 (24 hours)
    }
}
```

### 9.4. Chi tiết Publication Management (trang 569-570)

```asn1
PublicationType ::= CHOICE {
    datexPublish-Management-cd    ENUMERATED {
        temporarilySuspended,       -- Tạm dừng
        resume,                     -- Tiếp tục
        terminate-other,            -- Kết thúc (khác)
        terminate-dataNoLongerAvailable,
        terminate-publicationsBeingRejected,
        terminate-PendingShutdown,
        terminate-processingMgmt,
        terminate-bandwidthMgmt,
        terminate-accessDenied,     -- Từ chối truy cập
        unknownRequest
    },
    publicationData               EndApplicationMessage
}
```

---

## PHỤ LỤC

### A. Nguồn tài liệu đã khảo sát

| Trang PDF | Nội dung | Ghi chú |
|-----------|----------|---------|
| 171 | Yêu cầu kỹ thuật tối thiểu PM chia sẻ dữ liệu | 2 chức năng chính |
| 193-201 | Mục 4.4.8 — Yêu cầu phương án kết nối | Tiêu chuẩn, giao thức, session |
| 202-204 | Mục 4.4.9 — Hệ thống phần mềm | Kiến trúc, đặc điểm |
| 543-572 | Mục 1419 — Chuẩn kết nối trung tâm tuyến quốc gia | Chi tiết đầy đủ |

### B. File Excel tham chiếu

- **File:** `2026.04.14_ITS HN-CL_YCKT  Phan mem ITS_VDS.xlsx`
- **Row 181:** Mã TMC-PM-ITS-ESHARE, Item #48, Model TA-ShareData
- **Row 182:** Chức năng 1 — Quản lý giao diện chia sẻ dữ liệu tại trung tâm điều hành
- **Row 183:** Chức năng 2 — Quản lý lịch sử chia sẻ

---

*Kết thúc tài liệu nghiên cứu.*