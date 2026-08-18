# 9B. Sổ Tay Thực Thi & Tham Chiếu Response Thực Tế — ISAPI Video Wall Controller

> **Tài liệu hợp nhất:** Toàn bộ kết quả đo kiểm thực tế, bản đồ phần cứng, hệ toạ độ, cơ chế xác thực Digest và các bẫy kỹ thuật của phân hệ Video Wall Controller tại trạm Thiên An.  
> **Thiết bị đo kiểm:** Hikvision `DS-C66S-H88-CL` · Serial Number: `GW2405704` · IP: `10.10.9.236` · HTTP Port: `80`.  
> **Tài liệu đối chiếu gốc:** [**`09A-api-reference.md`**](file:///c:/ThienAn/DocBusinessThienAn/VideoWall/ISAPI-Videowall-Controller/09A-api-reference.md), [**`00-api-catalog.md`**](file:///c:/ThienAn/DocBusinessThienAn/VideoWall/ISAPI-Videowall-Controller/00-api-catalog.md) & *DS-C66S Series Datasheet*.  
> **Ký hiệu nhận biết:** 🟢 Đã kiểm chứng chạy OK · ❌ Lỗi / Thất bại · ⚠️ Lưu ý kỹ thuật · 🔴 Điểm khác biệt/sai khác so với tài liệu lý thuyết của hãng.

---

## 📑 MỤC LỤC

1. [Tóm tắt 10 nguyên tắc cốt lõi](#1-tóm-tắt-10-nguyên-tắc-cốt-lõi)
2. [Bảo mật & Xác thực Digest (RFC 7616)](#2-bảo-mật--xác-thực-digest-rfc-7616)
3. [Bảng bẫy kỹ thuật đã biết (14 bẫy lõi)](#3-bảng-bẫy-kỹ-thuật-đã-biết-14-bẫy-lõi)
4. [Bản đồ phần cứng & Công thức ID thực tế](#4-bản-đồ-phần-cứng--công-thức-id-thực-tế)
5. [Hệ toạ độ ảo uniformCoordinate & Mô hình Canvas Video Wall](#5-hệ-toạ-độ-ảo-uniformcoordinate--mô-hình-canvas-video-wall)
6. [Tham chiếu Response & Phân tích Endpoint thực tế (Nhóm A → F)](#6-tham-chiếu-response--phân-tích-endpoint-thực-tế)
   - [A. Xác thực (`userCheck` & `WWW-Authenticate`)](#a-xác-thực)
   - [B. Năng lực & Phần cứng (`capabilities` & `decoingDevice/status`)](#b-năng-lực--phần-cứng)
   - [C. Kênh đầu ra Output (`outputs/channels`)](#c-kênh-đầu-ra-output)
   - [D. Quản lý Video Wall (`VideoWall`, `outputs`)](#d-quản-lý-video-wall)
   - [E. Cửa sổ hiển thị Window (`windows`, `Rect`, Z-Order)](#e-cửa-sổ-hiển-thị-window)
   - [F. Điều khiển màn hình Screen Control (`closeAll` & RS-232/485)](#f-điều-khiển-màn-hình-screen-control)
7. [Chẩn đoán lỗi ISAPI & Mẫu Debug (G.1 → G.5)](#7-chẩn-đoán-lỗi-isapi--mẫu-debug)
8. [Quy trình đo kiểm an toàn 4 vòng & Kỹ thuật DevTools](#8-quy-trình-đo-kiểm-an-toàn-4-vòng--kỹ-thuật-devtools)
9. [Lưu ý kiến trúc cho hệ thống 4 Controller](#9-lưu-ý-kiến-trúc-cho-hệ-thống-4-controller)
10. [Phụ lục: 12 điểm sai khác giữa tài liệu hãng và thực tế](#10-phụ-lục-12-điểm-sai-khác-giữa-tài-liệu-hãng-và-thực-tế)

---

## 1. Tóm tắt 10 nguyên tắc cốt lõi

1. **Không có API Login độc lập:** Kết nối thiết bị thành công được định nghĩa khi `GET /ISAPI/Security/userCheck` trả về HTTP `200` với xác thực Digest.
2. **Cơ chế 2 Round-trip là bình thường:** Lần gọi đầu tiên nhận `401 Unauthorized` kèm challenge `WWW-Authenticate`, lần thứ hai gửi kèm header `Authorization` tính đúng hash sẽ nhận `200 OK`.
3. **Quy tắc chống khóa IP (Circuit Breaker):** Sai mật khẩu 2 lần liên tiếp $\rightarrow$ **DỪNG NGAY LẬP TỨC**. Thiết bị có tính năng khóa IP (`illaccess`) nhưng firmware **không trả về** `retryLoginTime` nên không thể biết còn mấy lượt.
4. **Postman Digest Auth có lỗi:** Runtime Postman bị lỗi khi gặp challenge không có trường `algorithm` (mặc định MD5). Cần dùng script Before Request hoặc tick *"disable retrying"*.
5. **ID không phải số thứ tự 1..N:** Hikvision dùng ID tổ hợp đóng gói theo byte (`0x01070003` = `17235971`). Phải đọc ID từ API, tuyệt đối không hardcode.
6. **Hệ toạ độ ảo là ô VUÔNG 1920×1920:** `baseOutputSize = 1920` áp dụng cho cả chiều rộng và chiều cao mỗi màn hình bất kể tỉ lệ thật của màn hình là 16:9 (1920×1080) hay 4K. Giới hạn `range:[0,1920]` trong tài liệu là **SAI**.
7. **Lệnh PUT chỉ gửi payload tối thiểu:** Tuyệt đối không lấy toàn bộ XML của lệnh `GET` ném sang `PUT` (sẽ dính lỗi element rỗng hoặc trường read-only dẫn đến `badParameters`).
8. **Luôn gọi Capabilities trước API chức năng:** Kiểm tra cờ hỗ trợ (`isSupportScene`, `isSupportScreenCtrl`, `isSupportCutOffSetting`...) trước khi gửi lệnh.
9. **Dùng DevTools trên Web Controller để lấy Payload chuẩn:** Giao diện Web của thiết bị cũng gọi ISAPI. Khi tài liệu hãng viết thiếu, mở F12 Network để copy chính xác URL và body XML/JSON.
10. **Sandbox an toàn trên Wall 1 (`unbound`):** Luôn thử nghiệm tạo/sửa trên tường rỗng (Wall 1) trước khi can thiệp vào tường đang xuất hình thật (Wall 2).

---

## 2. Bảo mật & Xác thực Digest (RFC 7616)

### 2.1. Cấu hình xác thực trên Web thiết bị
Truy cập: **Configuration $\rightarrow$ Network $\rightarrow$ Advanced $\rightarrow$ HTTP(S)**:
- **HTTP Port:** `80` (Mặc định dùng nội bộ).
- **Redirect to HTTPS Automatically:** **TẮT** (Redirect HTTP $\rightarrow$ HTTPS sẽ làm đứt chuỗi bắt tay Digest).
- **Authentication Mode:** `digest`.
- **Digest Algorithm Type:** Đặt cố định **`MD5`** trên cả 4 bộ Controller.

> 📌 **Tại sao chọn MD5 thay vì MD5/SHA256:** Ở chế độ kép `MD5/SHA256`, thiết bị trả về 2 challenge trong cùng một header `WWW-Authenticate`. Nhiều HTTP client cắt chuỗi bằng dấu phẩy sẽ bị trộn lẫn `nonce` của MD5 với `algorithm` của SHA-256 gây lỗi xác thực.

### 2.2. Ba biến thể Header `WWW-Authenticate` quan sát được

* **Biến thể 1 (Kép - khi bật MD5/SHA256):**
  ```http
  WWW-Authenticate: Digest qop="auth", realm="HIK Device", nonce="<hex>", algorithm="MD5", stale="FALSE", Digest qop="auth", realm="HIK Device", nonce="4d6b...", algorithm="SHA-256", stale="FALSE"
  ```
* **Biến thể 2 (Đơn - cấu hình chuẩn MD5 hiện tại):**
  ```http
  WWW-Authenticate: Digest qop="auth", realm="HIK Device", nonce="4e6b46444e44637a4e6a673659324d784d44566c59574d3d", stale="FALSE"
  ```
  *(Theo RFC 7616, khi không có trường `algorithm` thì mặc định hiểu là `MD5`).*
* **Biến thể 3 (Nonce hết hạn):**
  ```http
  WWW-Authenticate: Digest qop="auth", realm="HIK Device", nonce="4e6b56444e54...", stale="TRUE"
  ```

### 2.3. Ý nghĩa trường `stale` — Chìa khóa chống khóa IP
| Giá trị `stale` | Ý nghĩa kỹ thuật | Hành động của Hệ Thống / Backend |
|---|---|---|
| **`FALSE`** | 🔴 **Sai Username hoặc Password** | **DỪNG NGAY LẬP TỨC.** Kích hoạt Circuit Breaker, không retry. |
| **`TRUE`** | 🟢 **Mật khẩu ĐÚNG**, chỉ là `nonce` phiên trước đã hết hạn | **An toàn.** Lấy nonce mới từ challenge và gửi lại request. |

---

## 3. Bảng bẫy kỹ thuật đã biết (14 bẫy lõi)

| # | Bẫy kỹ thuật | Nguyên nhân & Cách xử lý |
|---|---|---|
| 1 | Postman Digest Auth báo `Releasing Default reader` | Lỗi parser của Postman khi challenge thiếu trường `algorithm`. Khắc phục bằng Script Before Request hoặc tick *"disable retrying"*. |
| 2 | `getaddrinfo ENOTFOUND {{base}}` | Chưa chọn Environment trong Postman hoặc cột *Current value* bị để trống. |
| 3 | **`decoingDevice`** | Firmware **viết sai chính tả** (thiếu chữ `d`). Gõ đúng `decodingDevice` sẽ bị 404. URL chuẩn: `GET /ISAPI/DisplayDev/decoingDevice/status?format=json`. |
| 4 | **`backgroudColor`** | Trường màu nền trong XML bị **thiếu chữ `n`**. Gõ đúng `backgroundColor` thiết bị sẽ báo lỗi `badParameters`. |
| 5 | `channelID` không phải `1..8` | Hikvision dùng ID tổ hợp byte (`17235971`). Tra cứu mục 4 để tính hoặc đọc từ API. |
| 6 | Tài liệu ghi `Rect range:[0,1920]` | **SAI.** Đã đo thực tế `height=3840` hoạt động hoàn hảo và mở rộng toàn tường. |
| 7 | PUT bị `badParameters` | Do gửi nguyên XML từ lệnh `GET`. Chỉ gửi các thẻ cần thay đổi (tối thiểu). |
| 8 | Element rỗng `<tag></tag>` | Thiết bị chờ kiểu Enum nhưng nhận chuỗi rỗng $\rightarrow$ lỗi `badParameters`. Cần xóa hẳn thẻ rỗng khỏi payload. |
| 9 | URL `?format=json` bị 401 | Khi tính Digest hash `HA2 = MD5(method:uri)`, `uri` bắt buộc phải kèm query string (`/ISAPI/DisplayDev/decoingDevice/status?format=json`). |
| 10 | Sai mật khẩu liên tiếp | Thiết bị kích hoạt khóa IP (`illaccess`). Luôn đặt Circuit Breaker tối đa 2 lần thất bại. |
| 11 | `outputPortEnabled` không tắt được HDMI | Trường này chỉ dành cho card LED Send Card, không phải công tắc nguồn HDMI. |
| 12 | `closeAll` trả `invalidOperation` | Cần cắm dây serial RS-232/485 và cấu hình `workMode = screenCtrl` trên cổng serial. |
| 13 | Vị trí script Postman | Postman phiên bản mới đổi tên tab *Pre-request Script* thành **"Before request"**. |
| 14 | Thiếu các trường bảo mật trong `userCheck` | Firmware không trả về `lockStatus`, `unlockTime`, `retryLoginTime`. Backend không được phụ thuộc vào các trường này. |

---

## 4. Bản đồ phần cứng & Công thức ID thực tế

### 4.1. Cấu trúc khung máy DS-C66S-H88-CL
Đọc từ `GET /ISAPI/DisplayDev/decoingDevice/status?format=json`:
- **Kích thước khung:** Chiều cao **4.5U**, khung gồm **8 hàng × 2 cột slot** (12 slot mở rộng khả dụng).
- **Phân chia cố định:** Cột 1 là khe cắm card **INPUT**, Cột 2 là khe cắm card **OUTPUT**.

```text
       Cột 1 (INPUT)              Cột 2 (OUTPUT)
Hàng 1: [ — ]                     [ MAIN BOARD & CỔNG MẠNG ]
Hàng 2: [ — ]                     [ Slot ID 0 (Trống) ]
Hàng 3: [ Card ID 1: 4×HDMI IN ]  [ Card ID 7: 4×HDMI OUT (Port 3 & 4 đang cắm màn) ]
Hàng 4: [ Card ID 2: 4×HDMI IN ]  [ Card ID 8: 4×HDMI OUT (Chưa cắm màn) ]
Hàng 5-8:[ Slot ID 3-6: Trống ]   [ Slot ID 9-12: Trống ]
```

### 4.2. Cổng giao tiếp trên Main Board
- **Cổng mạng RJ-45 (1 Gbps):** Duy nhất **1 cổng mạng** (`10.10.9.236`, MAC `fc:9f:fd:cf:f1:c8`, cổng SDK `8000`, ISAPI `80`).
- 🔴 **Hai jack RJ-45 còn lại:** 
  1. `RJ45Console`: Cổng Serial Console cấu hình (115200 baud).
  2. `reusePort`: Cổng RS-232/485 điều khiển màn hình.
  > ⚠️ **Cảnh báo:** Tuyệt đối không cắm dây mạng LAN vào 2 jack RJ-45 này.

### 4.3. Công thức tính ID tổ hợp (Byte Packing)
Hikvision đóng gói ID theo cấu trúc 32-bit:
$$\text{ID} = (\text{Type} \ll 24) \mid (\text{BoardID} \ll 16) \mid \text{PortID}$$
Dạng thập phân: $\text{ID} = \text{Type} \times 16777216 + \text{BoardID} \times 65536 + \text{PortID}$

| Mã Type (Byte đầu) | Loại tài nguyên | Ví dụ Hex | Ví dụ Dec | Giải mã |
|---|---|---|---|---|
| `0x01` | Video Channel (Input / Output) | `0x01070003` | `17235971` | Board 7, Port 3 |
| `0x02` | Window hiển thị | `0x02000001` | `33554433` | Cửa sổ số 1 |
| `0x04` | Lớp hiển thị Z-Order (`layerIdx`) | `0x04000001` | `67108865` | Lớp hiển thị số 1 |

### 4.4. Bảng tra cứu ID Output & Input thực tế
* **Kênh Đầu Ra (Output Channels):**
  - `17235969` (`0x01070001`): Board 7 - Port 1 (`notConnected`)
  - `17235970` (`0x01070002`): Board 7 - Port 2 (`notConnected`)
  - 🟢 **`17235971`** (`0x01070003`): Board 7 - Port 3 (**Màn hình Trên** - `normal`)
  - 🟢 **`17235972`** (`0x01070004`): Board 7 - Port 4 (**Màn hình Dưới** - `normal`)
  - `17301505` .. `17301508`: Board 8 - Port 1..4 (`notConnected`)
* **Kênh Đầu Vào (Input Channels):**
  - 🟢 **`16842753`** (`0x01010001`): Board 1 - Port 1 (Nguồn tín hiệu đang chiếu lên màn hình)
  - `16842754` .. `16842756`: Board 1 - Port 2..4
  - `16908289` .. `16908292`: Board 2 - Port 1..4

---

## 5. Hệ toạ độ ảo uniformCoordinate & Mô hình Canvas Video Wall

### 5.1. Bản chất hệ toạ độ ảo: Mỗi màn hình là ô VUÔNG 1920×1920
`GET /ISAPI/DisplayDev/VideoWall/capabilities` trả về hằng số `baseOutputSize = 1920`.
- Toạ độ ảo của một bức tường gồm $C$ cột và $R$ hàng:
  $$\text{Width}_{\text{ảo}} = C \times 1920, \quad \text{Height}_{\text{ảo}} = R \times 1920$$
- Ví dụ với tường hiện tại (1 cột × 2 hàng):
  - Kích thước ảo: **1920 × 3840**
  - Kích thước pixel vật lý thật (2 màn Full HD ghép dọc): **1920 × 2160**

```text
Toạ độ ảo (uniformCoordinate):               Vị trí màn hình vật lý:
(0, 0)     ┌──────────────────┐ (1920, 0)
           │                  │              Màn hình Trên (Output 7-3, ID 17235971)
           │  MÀN HÌNH TRÊN   │              Độ phân giải: 1920 × 1080 px
(0, 1920)  ├──────────────────┤ (1920, 1920) ───────────────────────────────────────
           │                  │              Màn hình Dưới (Output 7-4, ID 17235972)
           │  MÀN HÌNH DƯỚI   │              Độ phân giải: 1920 × 1080 px
(0, 3840)  └──────────────────┘ (1920, 3840)
```

### 5.2. Công thức quy đổi toạ độ Frontend (Canvas $\leftrightarrow$ ISAPI)
Khi Client vẽ hoặc nhận toạ độ từ thiết bị:
$$X_{\text{thật}} = X_{\text{ảo}} \times \frac{\text{Width}_{\text{panel thật}}}{1920}$$
$$Y_{\text{thật}} = Y_{\text{ảo}} \times \frac{\text{Height}_{\text{panel thật}}}{1920} \quad (\text{Với màn 1080p: } Y_{\text{thật}} = Y_{\text{ảo}} \times 0.5625)$$

### 5.3. Mô hình tư duy: Canvas duy nhất và Giấy dán
- Toàn bộ bức tường là **1 Canvas duy nhất**.
- Mỗi cửa sổ (Window) là một **tấm giấy dán** trên canvas có toạ độ `Rect(x, y, w, h)`.
- Màn hình vật lý chỉ là **ô cửa sổ nhìn vào một phần canvas**. Cửa sổ trải qua 2 hay nhiều màn hình không phải là trường hợp đặc biệt mà chỉ là một vùng `Rect` có chiều cao lớn.
- **Xếp lớp Z-Order:** Window có `layerIdx` lớn hơn sẽ đè lên window có `layerIdx` nhỏ hơn. Muốn thay đổi lớp dùng lệnh `/top` hoặc `/bottom`.

---

## 6. Tham chiếu Response & Phân tích Endpoint thực tế

### A. Xác thực
#### `GET /ISAPI/Security/userCheck` (Status: 200 OK)
```xml
<?xml version="1.0" encoding="UTF-8"?>
<userCheck version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <statusValue>200</statusValue>
  <statusString>OK</statusString>
  <isRiskPassword>false</isRiskPassword>
  <isActivated>true</isActivated>
</userCheck>
```

### B. Năng lực & Phần cứng
#### `GET /ISAPI/DisplayDev/capabilities`
- `maxWallNums`: `8` (Tối đa 8 tường logic).
- `maxWindowNums`: `512` (Trần cứng số cửa sổ trên toàn bộ tường).
- `baseOutputSize`: `1920`.
- `isSupportScene`: `true` (`maxSceneNums`: `128`).
- `isSupportCutOffSetting`: `true` (Hỗ trợ crop nguồn cho cửa sổ ghép).
- 🔴 `isSupportVideoPreview`: `false` (Không hỗ trợ xem luồng video preview trực tiếp từ bộ điều khiển qua mạng).
- 🔴 `isSupportSaveSceneVirLed` & `isSupportSaveSceneBaseMap`: `false` (Kịch bản phần cứng không lưu chữ chạy và ảnh nền $\rightarrow$ Backend phải tự lưu và khôi phục).

#### `GET /ISAPI/DisplayDev/decoingDevice/status?format=json`
- `DevCaseStatus`: `height: "4.5U"`, `row: 8`, `col: 2`.
- `BackplaneStatusList[0].backplaneTemperature`: `60` (°C) — Cần đặt ngưỡng cảnh báo ở mức $\ge 65^\circ\text{C}$.
- `SubBoardStatusList`: Board 1, 2 (Input), Board 7, 8 (Output).
- 💡 **Mẹo giám sát:** Theo dõi `runTime` của từng sub-board; nếu `runTime` giảm đột ngột về vài giây tức là bo mạch vừa bị reboot.

### C. Kênh đầu ra Output
#### `GET /ISAPI/DisplayDev/Video/outputs/channels`
- Trả về danh sách 8 cổng Output.
- Trường quan trọng nhất: `<outputPortAccessStatus>normal | notConnected</outputPortAccessStatus>` (dùng để vẽ trạng thái cắm cáp màn hình).

### D. Quản lý Video Wall
#### `GET /ISAPI/DisplayDev/VideoWall`
- Wall 1: `id=1`, `name="VideoWall1"`, `wallBindOutputStatus="unbound"` $\rightarrow$ **Sandbox test an toàn**.
- Wall 2: `id=2`, `name="HoangNhu"`, `wallBindOutputStatus="bound"` $\rightarrow$ Đang gắn màn hình thật.

#### `PUT /ISAPI/DisplayDev/VideoWall/<id>`
- Body mẫu đổi tên tường an toàn:
  ```xml
  <VideoWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
    <id>2</id>
    <name>VideoWall_Main</name>
  </VideoWall>
  ```

### E. Cửa sổ hiển thị Window
#### `GET /ISAPI/DisplayDev/VideoWall/<id>/windows`
- `WallWindow`:
  - `id`: `33554433` (Hex `0x02000001`).
  - `wndOperateMode`: `uniformCoordinate`.
  - `Rect`: `<Coordinate><x>0</x><y>0</y></Coordinate><width>1920</width><height>1920</height>`.
  - `layerIdx`: `67108865`.
  - `SubWindow.SubWindowParam.videoInputChannelID`: `16842753`.

#### `PUT /ISAPI/DisplayDev/VideoWall/<id>/windows/<wndId>`
- Payload kéo dãn cửa sổ phủ toàn bộ tường (1920×3840):
  ```xml
  <WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
    <id>33554433</id>
    <wndOperateMode>uniformCoordinate</wndOperateMode>
    <Rect>
      <Coordinate><x>0</x><y>0</y></Coordinate>
      <width>1920</width>
      <height>3840</height>
    </Rect>
  </WallWindow>
  ```

### F. Điều khiển màn hình Screen Control
#### `PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll`
- Nhận diện lỗi `invalidOperation` (Mã 4): Thiết bị cần có dây RS-232/485 nối sang màn hình và cổng serial phải được cấu hình protocol tương ứng của hãng màn hình (LG, Samsung...).
- Tắt riêng từng màn hình bằng cách truyền thêm `<OutputID>3</OutputID>`.

---

## 7. Chẩn đoán lỗi ISAPI & Mẫu Debug

Cấu trúc chuẩn của phản hồi lỗi:
```xml
<ResponseStatus version="1.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <statusCode>4</statusCode>
  <statusString>Invalid Operation</statusString>
  <subStatusCode>methodNotAllowed</subStatusCode>
</ResponseStatus>
```

### Bảng tra cứu `subStatusCode`
| Tình huống thực tế | `subStatusCode` | Nguyên nhân & Hành động khắc phục |
|---|---|---|
| G.1: Gửi `GET` vào URL chỉ nhận `PUT` | `methodNotAllowed` | **URL tồn tại**, chuyển method sang `PUT`. |
| G.2: `PUT` không gửi Body XML | `badXmlFormat` | Thiếu body hoặc định dạng XML không hợp lệ / dính UTF-8 BOM. |
| G.3: `PUT` gửi kèm thẻ rỗng `<tag></tag>` | `badParameters` | Thẻ rỗng không parse được kiểu Enum $\rightarrow$ Xóa thẻ rỗng. |
| G.4: Gọi `channelID` sai (VD: `id=3`) | `invalidOperation` | Sai quy tắc ID $\rightarrow$ Lấy ID thật từ `outputs/channels`. |
| G.5: Gọi tính năng firmware không hỗ trợ | `notSupport` | Thiết bị không có tính năng này $\rightarrow$ Kiểm tra lại `capabilities`. |

---

## 8. Quy trình đo kiểm an toàn 4 vòng & Kỹ thuật DevTools

```text
VÒNG 1 — Khám Phá & Định Danh (Chỉ GET, an toàn tuyệt đối 100%):
  GET /ISAPI/Security/userCheck               → Kiểm tra Digest Auth
  GET /ISAPI/System/deviceInfo                → Model, Firmware, Serial
  GET /ISAPI/DisplayDev/capabilities          → Cờ tính năng
  GET /ISAPI/DisplayDev/decoingDevice/status?format=json → Phần cứng slot

VÒNG 2 — Khảo Sát Tường & Layout (Chỉ GET):
  GET /ISAPI/DisplayDev/VideoWall             → Danh sách VideoWallID
  GET /ISAPI/DisplayDev/VideoWall/<id>/outputs → Bố cục màn hình
  GET /ISAPI/DisplayDev/VideoWall/<id>/windows → Các cửa sổ hiện hành

VÒNG 3 — Thử Nghiệm Ghi Trên Sandbox (GHI AN TOÀN):
  PUT /ISAPI/DisplayDev/VideoWall/1           → Thử đổi tên Wall 1 (Unbound)
  POST/PUT/DELETE trên Wall 1                 → Thử nghiệm window không ảnh hưởng màn thật

VÒNG 4 — Tác Động Lên Tường Thật (Wall 2):
  GET snapshot lưu ra file trước khi PUT      → Luôn có phương án Rollback
  PUT payload tối thiểu                       → Quan sát màn hình thực tế
```

### ⛔ Các lệnh CẤM TUYỆT ĐỐI dùng để test:
- `PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll` (không kèm `OutputID`): Tắt toàn bộ màn hình (không có API bật lại qua mạng).
- `DELETE /ISAPI/DisplayDev/VideoWall/<id>/windows`: Xóa trắng toàn bộ cửa sổ trên tường.
- `PUT /ISAPI/System/reboot` hoặc `factoryReset`: Khởi động lại hoặc xóa trắng cấu hình thiết bị.

---

## 9. Lưu ý kiến trúc cho hệ thống 4 Controller

1. **Không có cụm Controller ảo ở tầng phần cứng:** Mỗi Controller là một thực thể ISAPI độc lập với IP riêng. Backend đóng vai trò tổng hợp và điều phối phân vùng.
2. **Bảng ChannelID là độc lập:** `channelID` của máy 1 không nhất thiết trùng với máy 2. Backend phải duy trì ánh xạ `ControllerId → ChannelId`.
3. **Đồng nhất phiên bản Firmware & Thuật toán Digest:** Cả 4 máy phải cùng firmware và cùng bật `MD5`.
4. **Đồng bộ thời gian (`/ISAPI/System/time`):** Bắt buộc đồng bộ thời gian từ máy chủ NTP xuống cả 4 controller để các kịch bản chạy theo lịch (`VwSchedule`) không bị lệch giây.
5. **Circuit Breaker độc lập:** Mỗi Controller có 1 instance Circuit Breaker riêng; sự cố trên máy 1 không làm ngắt kết nối tới 3 máy còn lại.

---

## 10. Phụ lục: 12 điểm sai khác giữa tài liệu hãng và thực tế

| # | Tài liệu hãng Hikvision mô tả | Thực tế đo kiểm tại trạm |
|---|---|---|
| 1 | `Rect` có miền giá trị `range:[0,1920]` | **SAI.** Thực tế `height=3840` hoạt động bình thường trên màn ghép. |
| 2 | `userCheck` trả về `lockStatus`, `retryLoginTime` | **KHÔNG TRẢ VỀ.** Backend phải tự quản lý số lần thử sai mật khẩu. |
| 3 | Tên URL `decodingDevice` | **Firmware viết sai: `decoingDevice`** (thiếu chữ `d`). |
| 4 | Trường màu nền `backgroundColor` | **Firmware viết sai: `backgroudColor`** (thiếu chữ `n`). |
| 5 | Không đề cập trường `outputPortAccessStatus` | **Firmware CÓ trả về** — Đây là trường tốt nhất để đọc trạng thái cáp màn hình. |
| 6 | `outputPortEnabled` dùng để bật/tắt cổng ra | **SAI.** Không có tác dụng với cổng HDMI. |
| 7 | `SubBoardInterfaceList` có `signalStatus`, `decodeWallStatus` | **KHÔNG TRẢ VỀ** trên dòng card HDMI tiêu chuẩn. |
| 8 | `WallWindow` trả về `ResolutionRect`, `wndTopKeep` | **KHÔNG TRẢ VỀ** khi đang ở chế độ `uniformCoordinate`. |
| 9 | `closeAll` chỉ có thể tắt toàn bộ tường | **CÓ THỂ tắt từng màn** nếu truyền thêm `<OutputID>`. |
| 10| `configurationData` có thể export qua API với query | Đặc tả thiếu thông tin thuật toán mã hóa secret key $\rightarrow$ Export qua Web UI. |
| 11| `videoInputPortNums = 24`, `videoOutputPortNums = 24` | Đây là **trần tối đa của khung máy**, số lượng thực tế hiện tại là 8 IN / 8 OUT. |
| 12| API gán output (`POST .../outputs`), chỉnh tỉ lệ (`GetVideoWallScale`) | Không có mục mô tả riêng ở chương 9 dù có trong luồng gọi $\rightarrow$ Đã xác nhận tồn tại qua `capabilities`. |
