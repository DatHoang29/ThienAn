# Tài liệu bàn giao — Test API ISAPI Video Wall Controller

**Phạm vi:** thiết lập môi trường, cơ chế bảo mật ISAPI, và toàn bộ phát hiện từ buổi test đầu tiên
**Thiết bị:** Hikvision Video Wall Controller `DS-C66S-H88-CL` · SN `GW2405704` · IP `10.10.9.236`

> Đọc hết mục 1 → 4 trước khi gửi request đầu tiên. Mục 5 → 9 là tra cứu khi cần.
> **Mục 3.3 và 4 là hai mục tiết kiệm thời gian nhất** — chúng chứa những lỗi đã mất nhiều giờ để tìm ra nguyên nhân.

---

## MỤC LỤC

| # | Nội dung |
|---|---|
| 1 | ISAPI là gì — 3 điều phải hiểu trước |
| 2 | Bảo mật ISAPI: Digest Authentication |
| 3 | Thiết lập môi trường Postman |
| 4 | ⚠️ Bảng bẫy đã biết — đọc kỹ |
| 5 | Bản đồ phần cứng và bảng ID thực tế |
| 6 | Hệ toạ độ video wall |
| 7 | Đọc lỗi ISAPI |
| 8 | Quy trình test an toàn |
| 9 | Trạng thái hiện tại & việc còn treo |

---

# 1. ISAPI là gì — 3 điều phải hiểu trước

**ISAPI** = Intelligent Security API. Giao thức tầng ứng dụng dựa trên HTTP, kiến trúc REST, do Hikvision định nghĩa để phần mềm nói chuyện với thiết bị.

### 1.1. Thiết bị là server, ta là client

Tài liệu mục 2.1.1:

> *"the device acts as the server to listen on the fixed port and the user's application acts as the client to actively log in to the device"*

Thiết bị phải có **IP tĩnh**. Client (Postman / backend) chủ động gọi vào.

### 1.2. Không có API login

🔴 **Đây là điều dễ gây nhầm nhất với người quen làm REST hiện đại.**

- Không có `/login`
- Không có token, không JWT, không session, không cookie
- **Mỗi request tự mang thông tin xác thực**

"Kết nối thành công" nghĩa là: gọi `GET /ISAPI/Security/userCheck` và nhận HTTP 200. Không có bước nào khác.

### 1.3. Dữ liệu chủ yếu là XML

Phần lớn API trả về **XML**. Chỉ những endpoint mà tài liệu ghi rõ `?format=json` trong URL mới hỗ trợ JSON. Thêm `?format=json` vào API không hỗ trợ thì bị bỏ qua hoặc trả `notSupport`.

Mọi XML gửi lên **phải có namespace**:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<TênThẻGốc xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
```

Thiếu `xmlns` hoặc sai `version` → lỗi 400.

---

# 2. Bảo mật ISAPI: Digest Authentication

## 2.1. Cơ chế

Tài liệu mục 3.3.1 và 4.1 yêu cầu **Digest Authentication (RFC 7616)**.

```
① Client → GET /ISAPI/System/deviceInfo          (chưa có Authorization)
② Thiết bị → 401 Unauthorized
             WWW-Authenticate: Digest qop="auth", realm="HIK Device",
                               nonce="4e6b...", stale="FALSE"
③ Client → GET /ISAPI/System/deviceInfo
             Authorization: Digest username="admin", realm="HIK Device",
                            nonce="4e6b...", uri="/ISAPI/System/deviceInfo",
                            qop=auth, nc=00000001, cnonce="...",
                            response="<hash>"
④ Thiết bị → 200 OK + XML
```

⚠️ **Một request thành công = 2 round-trip.** Thấy `401` rồi `200` trong Postman Console là **bình thường**. Chỉ khi thấy **duy nhất một** 401 mới là lỗi.

**Không có gì để "lấy" và lưu lại.** `nonce` đổi mỗi lần, header `Authorization` dùng một lần rồi bỏ. Đây là điểm khác căn bản với OAuth/JWT.

## 2.2. Công thức (để debug, không cần tự viết)

```
HA1      = MD5(username : realm : password)
HA2      = MD5(method : uri)
response = MD5(HA1 : nonce : nc : cnonce : qop : HA2)
```

`uri` **phải khớp chính xác** với đường dẫn gửi lên, **kể cả query string**. Thiếu `?format=json` khi tính hash → 401.

Nếu `algorithm = SHA-256` thì thay MD5 bằng SHA-256.
Nếu challenge **không có** trường `algorithm` → mặc định là MD5 (theo RFC).

## 2.3. Cấu hình xác thực trên thiết bị

Web thiết bị → **Configuration → Network → Advanced → HTTP(S)**

| Mục | Giá trị hiện tại | Ghi chú |
|---|---|---|
| HTTP Port | `80` | Đang dùng |
| HTTPS | Bật, port `443` | Cert self-signed |
| Redirect to HTTPS Automatically | **Tắt** | Nên giữ tắt — redirect làm vỡ Digest |
| Authentication Mode | `digest` | Dropdown **chỉ có** `digest` trên máy này |
| Digest Algorithm Type | **`MD5`** | Đã đổi từ `MD5/SHA256` |

> 📌 Mục này tên **"HTTP(S) Authentication"** — chữ `(S)` nghĩa là áp dụng cho **cả HTTP và HTTPS**, không phải chỉ HTTPS. Nó nằm dưới khối HTTPS trong giao diện nên rất dễ hiểu nhầm.

> 📌 **Vì sao đã đổi sang `MD5`:** ở chế độ `MD5/SHA256`, thiết bị trả **hai challenge trong cùng một header `WWW-Authenticate`**. Nhiều HTTP client tách header bằng dấu phẩy một cách ngây thơ → trộn lẫn `nonce` của challenge này với `algorithm` của challenge kia → hỏng. Đặt một algorithm duy nhất thì mọi client đều chạy. **Đặt cả 4 controller giống nhau.**

## 2.4. Phân quyền (mục 3.3.2)

| Loại | Quyền |
|---|---|
| **admin** | Toàn bộ tài nguyên |
| **operator** | Tài nguyên chung + một phần nâng cao |
| **normal user** | Chỉ tài nguyên chung |

Backend nên dùng **operator**, không dùng `admin`. Nếu operator bị `403` ở API videowall thì mới nâng quyền.

## 2.5. 🔴 CẢNH BÁO: thiết bị khóa IP

Thiết bị có cơ chế khóa IP khi sai mật khẩu nhiều lần (mục 9.1.5.1, `GET/PUT /ISAPI/Event/triggers/illaccess`).

**Quy tắc bắt buộc: sai xác thực 2 lần → DỪNG, kiểm tra lại cấu hình. Không bấm Send tiếp.**

Trong Postman rất dễ bấm Send liên tục theo phản xạ. Mỗi lần thử sai làm hết một lượt, và khi hết lượt thì IP bị khóa có thời hạn — thử tiếp sẽ **reset lại đồng hồ**, càng lâu được vào.

Cách phân biệt:

| Response | Nghĩa |
|---|---|
| 401 + `stale="FALSE"` | **Sai mật khẩu** — dừng ngay |
| 401 + `stale="TRUE"` | Mật khẩu **đúng**, chỉ là `nonce` hết hạn — an toàn, lấy nonce mới |

> ⚠️ Firmware này **không trả về** `lockStatus` / `unlockTime` / `retryLoginTime` trong `userCheck`, dù tài liệu có mô tả. Nghĩa là **không có cách nào biết mình còn mấy lượt** — càng phải cẩn thận.

---

# 3. Thiết lập môi trường Postman

## 3.1. Environment

Tạo **một environment cho mỗi controller** (`CTRL-01`…`CTRL-04`), chỉ khác `host`. Đổi máy = đổi environment, không sửa request.

| Variable | Type | Value |
|---|---|---|
| `scheme` | default | `http` |
| `host` | default | `10.10.9.236` |
| `port` | default | `80` |
| `user` | default | `admin` |
| `pass` | **secret** | *(hỏi người bàn giao)* |
| `base` | default | `{{scheme}}://{{host}}:{{port}}` |

⚠️ **Postman chỉ dùng cột "Current value" khi chạy.** Điền cả *Initial value* và *Current value*, rồi **Ctrl+S**. Chỉ điền Initial value là biến sẽ rỗng, và bạn sẽ nhận lỗi `getaddrinfo ENOTFOUND {{base}}`.

⚠️ Mật khẩu có ký tự đặc biệt (`@`, `&`, `*`). Để **nguyên bản** trong biến, không encode. Không bao giờ nhét vào URL kiểu `http://user:pass@host` — dấu `@` sẽ phá URL.

## 3.2. Settings

`Settings → General`:

| Mục | Giá trị | Lý do |
|---|---|---|
| SSL certificate verification | **OFF** | Cert self-signed |
| Automatically follow redirects | OFF | Redirect làm vỡ Digest |

`Settings → Proxy`: **tắt** "Use the system proxy" nếu máy có proxy công ty — proxy chen vào giữa 2 chặng Digest sẽ hỏng.

## 3.3. 🔴 Lỗi Digest Auth của Postman — và 2 cách xử lý

**Hiện tượng:** bật Digest Auth thì Postman báo `Error: Releasing Default reader`, không nhận được response nào. Tắt auth thì 401 trả về bình thường.

**Nguyên nhân:** bug ở đường xử lý retry Digest của `PostmanRuntime/2.1.2`. Đã loại trừ: mạng, thiết bị, credential, header, nén (`Accept-Encoding`). `curl --digest` chạy hoàn toàn bình thường với cùng thông tin.

### Cách A — Tick "disable retrying" + điền nonce tay

Authorization → **Digest Auth** → tick ☑ **"Yes, disable retrying the request"**, rồi mở **Advanced configuration** và điền đầy đủ:

| Field | Giá trị |
|---|---|
| Realm | `HIK Device` |
| Nonce | *lấy từ curl (xem dưới)* |
| Algorithm | `MD5` |
| qop | `auth` |
| Nonce Count | `00000001` |
| Client Nonce | `0a4f113b` |

Lấy nonce:

```
curl.exe -v http://10.10.9.236/ISAPI/Security/userCheck 2>&1 | findstr /i "WWW-Authenticate"
```

Chỉ copy **chuỗi hex giữa hai dấu nháy** sau `nonce=`.

**Hạn chế:** nonce hết hạn sau vài phút. Khi gặp 401 kèm `stale="TRUE"` thì lấy nonce mới dán lại. Không phù hợp cho Collection Runner.

### Cách B — Script tự tính Digest ⭐ khuyến nghị

Bền hơn: lấy nonce mới cho **mỗi** request, không bao giờ gặp `stale`.

**Bước 1:** Collection → **Authorization** → **No Auth**
**Bước 2:** Collection → **Scripts** → **Before request** *(Postman đã đổi tên: Pre-request → Before request)*:

```javascript
// Digest Auth thủ công cho ISAPI (RFC 7616)
// Bắt buộc: Auth type = No Auth ở cấp collection
const base   = pm.variables.replaceIn('{{base}}');
const uri    = pm.request.url.getPathWithQuery();   // PHẢI có query (?format=json)
const method = pm.request.method;
const user   = pm.environment.get('user');
const pass   = pm.environment.get('pass');

if (!user || !pass) { console.error('Thiếu biến user/pass'); return; }

pm.sendRequest({ url: base + uri, method: method }, function (err, res) {
    if (err) { console.error('Không lấy được challenge:', err); return; }

    const raw = res.headers.get('WWW-Authenticate');
    if (!raw) { console.error('Không có WWW-Authenticate'); return; }

    // Thiết bị có thể trả nhiều challenge trong 1 header
    const challenges = raw.split(/,?\s*Digest\s+/i).filter(s => s.trim());
    const pick = (s, k) => (s.match(new RegExp(k + '\\s*=\\s*"([^"]*)"', 'i')) || [])[1];
    const ch = challenges.find(c => (pick(c,'algorithm')||'MD5').toUpperCase()==='MD5')
               || challenges[0];

    const realm = pick(ch, 'realm');
    const nonce = pick(ch, 'nonce');
    const qop   = pick(ch, 'qop') || 'auth';
    const algo  = (pick(ch, 'algorithm') || 'MD5').toUpperCase();  // thiếu = MD5

    if (!realm || !nonce) { console.error('Challenge thiếu realm/nonce:', raw); return; }

    const CryptoJS = require('crypto-js');
    const H = s => (algo === 'SHA-256' ? CryptoJS.SHA256(s) : CryptoJS.MD5(s)).toString();

    const nc     = '00000001';
    const cnonce = CryptoJS.lib.WordArray.random(8).toString();
    const HA1 = H(`${user}:${realm}:${pass}`);
    const HA2 = H(`${method}:${uri}`);
    const rsp = H(`${HA1}:${nonce}:${nc}:${cnonce}:${qop}:${HA2}`);

    pm.request.headers.upsert({
        key: 'Authorization',
        value: `Digest username="${user}", realm="${realm}", nonce="${nonce}", ` +
               `uri="${uri}", algorithm=${algo}, qop=${qop}, nc=${nc}, ` +
               `cnonce="${cnonce}", response="${rsp}"`
    });
    console.log(`Digest OK → ${method} ${uri} | ${challenges.length} challenge | ${algo}`);
});
```

**Bước 3:** **Ctrl+S** — script cấp collection không tự lưu.
**Bước 4:** Request con để **Inherit auth from parent**.

⚠️ **Không dùng đồng thời Cách A và Cách B.** Script `upsert` header `Authorization`, rồi Digest Auth của Postman lại ghi đè → không đoán được kết quả. Chọn một.

## 3.4. curl — công cụ đối chiếu

Khi Postman có vấn đề, curl là chuẩn để loại trừ:

```
curl.exe --digest -u "admin:<password>" http://10.10.9.236/ISAPI/Security/userCheck
```

PowerShell thì đổi sang nháy đơn. Thêm `-v` để xem cả challenge và header `Authorization` mà curl sinh ra.

Lưu response dài ra file cho dễ đọc:

```
curl.exe --digest -u "admin:<password>" http://10.10.9.236/ISAPI/System/deviceInfo -o out.xml
```

## 3.5. Script tự trích field (tùy chọn)

Collection → **Scripts** → **After response**, đỡ phải mò trong XML dài:

```javascript
const t = pm.response.text();
const grab = (tag) => {
    const m = t.match(new RegExp(`<${tag}>([^<]*)</${tag}>`, 'i'));
    return m ? m[1].trim() : null;
};
['model','firmwareVersion','serialNumber','deviceStatus',
 'videoInputPortNums','videoOutputPortNums','isSupportCutOffSetting',
 'baseOutputSize','maxWindowNums','maxWallNums'
].forEach(k => { const v = grab(k); if (v) console.log(`  ${k} = ${v}`); });

if (t.includes('notSupport'))
    console.warn('⚠️ Firmware KHÔNG hỗ trợ:', pm.request.url.toString());
if (t.includes('stale="FALSE"') && pm.response.code === 401)
    console.error('🔴 SAI MẬT KHẨU — DỪNG, đừng gửi thêm');
```

---

# 4. ⚠️ Bảng bẫy đã biết

Mỗi dòng trong bảng này đã tốn nhiều thời gian để tìm ra. Đọc trước khi bắt đầu.

| # | Bẫy | Xử lý |
|---|---|---|
| 1 | Postman Digest Auth crash `Releasing Default reader` | Xem mục 3.3 |
| 2 | `getaddrinfo ENOTFOUND {{base}}` | Chưa chọn environment, hoặc cột *Current value* trống |
| 3 | **`decoingDevice`** | Firmware **viết sai chính tả** — thiếu chữ `d`. Gõ đúng `decodingDevice` → 404. Node capability cũng sai giống hệt: `isSupportDecoingDeviceStatus` |
| 4 | **`backgroudColor`** | Thiếu chữ `n`. Gõ đúng `background` → sai |
| 5 | `channelID` **không phải** 1…8 | Xem công thức ở mục 5.3 |
| 6 | Tài liệu ghi `Rect range:[0,1920]` | **SAI.** Đã test `height=3840` → OK. Xem mục 6 |
| 7 | PUT bị `badParameters` | Đừng PUT lại nguyên response GET. Chỉ gửi field cần đổi |
| 8 | Element rỗng `<tag></tag>` | Gây `badParameters`. Xóa hẳn thẻ đó |
| 9 | `?format=json` bị 401 | Script Digest phải dùng `getPathWithQuery()`, không phải `getPath()` |
| 10 | Sai mật khẩu | **Khóa IP.** Sai 2 lần thì dừng. Xem mục 2.5 |
| 11 | `outputPortEnabled` không bật/tắt được cổng | Không phải công tắc HDMI. Xem mục 9.2 |
| 12 | `closeAll` trả `invalidOperation` | Cần dây RS-232/485 + tường đã bind output. Xem mục 9.2 |
| 13 | "Pre-request script" không thấy đâu | Postman đổi tên thành **"Before request"** |
| 14 | Response `userCheck` ngắn hơn tài liệu | Nhiều field là `opt`, firmware không trả. Đừng phụ thuộc vào chúng |

---

# 5. Bản đồ phần cứng và bảng ID thực tế

## 5.1. Khung máy

Đọc từ `GET /ISAPI/DisplayDev/decoingDevice/status?format=json` → `DevCaseStatus`: cao **4.5U**, lưới slot **8 hàng × 2 cột**.

| Hàng | Cột 1 (INPUT) | Cột 2 (OUTPUT) |
|---|---|---|
| 1 | — | **MAIN BOARD** |
| 2 | — | ID 0 · trống |
| 3 | **ID 1 · INPUT** 4×HDMI | **ID 7 · OUTPUT** 4×HDMI |
| 4 | **ID 2 · INPUT** 4×HDMI | **ID 8 · OUTPUT** 4×HDMI |
| 5–8 | ID 3–6 · trống | ID 9–12 · trống |

- Slot input/output **tách riêng cố định** (cột 1 = input, cột 2 = output) → kiến trúc khung **12 slot**
- Đang dùng **4/12 slot** → **8 HDMI IN + 8 HDMI OUT** (đúng nghĩa `H88`)
- Mở rộng tối đa: 24 in / 24 out
- `GET /ISAPI/DisplayDev/capabilities` trả `videoInputPortNums = 24` và `videoOutputPortNums = 24` — đây là **trần của khung máy**, không phải số cổng đang có. Bằng chứng: `audioInputsPortNums = 200`

## 5.2. Cổng trên main board

Từ `MainBoardStatusList`:

| Cổng | Số lượng | Trạng thái |
|---|---|---|
| Ethernet (RJ-45, 1 Gbps) | **1** | `connected` · `10.10.9.236` · MAC `fc:9f:fd:cf:f1:c8` |
| Console (RJ-45) | 1 | `serialPortType: RJ45Console` |
| RS-232/485 (RJ-45) | 1 | `serialPortType: reusePort` · chưa cấu hình |
| Genlock (BNC) | 2 | `notconnect` |
| USB | 1 | `connected` |
| Audio in / out (3.5mm) | 2 / 2 | `notconnect` |

🔴 **Chỉ có MỘT cổng mạng.** Hai jack RJ-45 còn lại là **Console (serial 115200)** và **RS-232/485** — cắm cáp mạng vào đó không có tác dụng. Đây là nguyên nhân phổ biến nhất của "ping không thấy máy".

## 5.3. ⭐ Công thức ID

Hikvision dùng **ID tổ hợp đóng gói theo byte**, không phải số thứ tự:

```
id = 0x0T000000 | (boardID << 16) | portID
```

Byte đầu `T` là **mã loại tài nguyên**:

| Loại | Prefix | Ví dụ |
|---|---|---|
| Video channel | `0x01` | `0x01070003` = 17235971 → board 7, port 3 |
| Window | `0x02` | `0x02000001` = 33554433 |
| Layer (`layerIdx`) | `0x04` | `0x04000001` = 67108865 |

Dạng thập phân cho channel: `16777216 + boardID × 65536 + portID`

> Dùng công thức để **hiểu**, nhưng BE vẫn phải **đọc ID từ API** — chưa xác nhận công thức đúng với board LED hoặc board decode.

## 5.4. Bảng ID output (đã kiểm chứng)

`GET /ISAPI/DisplayDev/Video/outputs/channels`

| `id` | Hex | Board | Port | Tên | `outputPortAccessStatus` |
|---|---|---|---|---|---|
| 17235969 | `0x01070001` | 7 | 1 | Output 7-1 | notConnected |
| 17235970 | `0x01070002` | 7 | 2 | Output 7-2 | notConnected |
| **17235971** | `0x01070003` | 7 | 3 | Output 7-3 | 🟢 **normal** |
| **17235972** | `0x01070004` | 7 | 4 | Output 7-4 | 🟢 **normal** |
| 17301505 | `0x01080001` | 8 | 1 | Output 8-1 | notConnected |
| 17301506 | `0x01080002` | 8 | 2 | Output 8-2 | notConnected |
| 17301507 | `0x01080003` | 8 | 3 | Output 8-3 | notConnected |
| 17301508 | `0x01080004` | 8 | 4 | Output 8-4 | notConnected |

**2 màn hình đang cắm = `17235971` và `17235972`.**

⚠️ Board 7 dùng `timeSequenceMode: standard`, board 8 dùng **`custom`** (1920×1080). Hai board output **không đồng nhất** — cần đưa về cùng chế độ trước khi ghép tường trải qua cả hai.

## 5.5. Tường và window

`GET /ISAPI/DisplayDev/VideoWall`

| `id` | `name` | `wallBindOutputStatus` |
|---|---|---|
| 1 | VideoWall1 | **`unbound`** — tường rỗng, không màn nào → **sandbox test an toàn** |
| 2 | *(đã đổi tên)* | `bound` — chứa 2 màn hình |

`GET /ISAPI/DisplayDev/VideoWall/2/outputs`

| WallOutput.id | outputID | Rect (x, y, w, h) | Vị trí |
|---|---|---|---|
| 2 | 17235971 | 0, 0, 1920, 1920 | Màn **trên** |
| 3 | 17235972 | 0, 1920, 1920, 1920 | Màn **dưới** |

→ Bố cục **1 cột × 2 hàng**, xếp dọc.

`GET /ISAPI/DisplayDev/VideoWall/2/windows`

| Window id | Rect | layerIdx | Nguồn |
|---|---|---|---|
| 33554433 | 0, 0, 1920, 1920 | 67108865 | `videoInputChannelID = 16842753` |
| 33554434 | 0, 1920, 1920, 1920 | 67108866 | `videoInputChannelID = 16842753` |

Cả hai cùng nguồn `16842753` (= `0x01010001` = input board 1, port 1) → hai màn hiển thị **duplicate**.

---

# 6. Hệ toạ độ video wall

## 6.1. `uniformCoordinate` — mỗi màn là một ô VUÔNG 1920×1920

`GET /ISAPI/DisplayDev/VideoWall/capabilities` → `baseOutputSize = 1920`.

🔴 **Bẫy lớn nhất khi code kéo-thả:** ô lưới là **1920×1920**, **không phải** 1920×1080. Hệ toạ độ ảo **không giữ tỉ lệ khung hình thật** — `baseOutputSize` áp cho cả chiều rộng lẫn chiều cao, bất kể panel thật tỉ lệ bao nhiêu.

```
wall_width_ảo  = số_cột  × 1920
wall_height_ảo = số_hàng × 1920
```

Tường hiện tại (1 cột × 2 hàng):

| | Toạ độ ảo | Vật lý thật |
|---|---|---|
| Rộng | 1920 | 1920 px |
| Cao | **3840** | 2160 px |

Quy đổi khi FE vẽ:

```
x_thật = x_ảo × (rộng_thật_1_màn / 1920)
y_thật = y_ảo × (cao_thật_1_màn  / 1920)      → với 1080p: × 0.5625
```

Hệ số phải lấy từ độ phân giải thật trong `Video/outputs/channels`. **Không hardcode.**

## 6.2. Giới hạn trong tài liệu là SAI

Tài liệu ghi `Rect` có `range:[0,1920]` cho x, y, width, height. **Đã test `height=3840` → `statusCode 1 / OK`, và hiển thị đúng.** Giá trị vượt 1920 hoàn toàn hợp lệ.

## 6.3. Ví dụ đặt window

| Muốn | `Rect` |
|---|---|
| Phủ màn trên | `x=0, y=0, w=1920, h=1920` |
| Phủ màn dưới | `x=0, y=1920, w=1920, h=1920` |
| Phủ cả tường | `x=0, y=0, w=1920, h=3840` |
| Nửa trên màn trên | `x=0, y=0, w=1920, h=960` |

## 6.4. Mô hình tư duy: canvas + giấy dán

Tường là **một canvas duy nhất**. Window là **những tấm giấy dán lên canvas**. Màn hình vật lý chỉ là **ô cửa nhìn vào canvas**.

Hệ quả:

- Kéo window = **đổi `Rect`**, không phải "chuyển nội dung sang màn khác"
- Một window nằm vắt qua nhiều màn là chuyện bình thường
- Số window **không liên quan** số màn. `maxWindowNums = 512` là trần cho cả tường
- **Window chồng nhau thì `layerIdx` lớn hơn nằm trên.** Kéo một window to ra mà không thấy đổi gì → nhiều khả năng có window khác đè lên. Xóa nó, hoặc `PUT .../windows/<id>/top`
- API **không có** khái niệm "màn hình đang chiếu gì" — phải tự đối chiếu `Rect` của window với vùng toạ độ của màn lấy từ `VideoWall/<id>/outputs`

## 6.5. Hệ toạ độ thay thế

`wndOperateMode` nhận hai giá trị:

| Giá trị | Ý nghĩa | Khối dùng kèm |
|---|---|---|
| `uniformCoordinate` | Đơn vị chuẩn hoá, 1920/ô | `<Rect>` |
| `resolutionCoordinate` | **Pixel thật** | `<ResolutionRect>` |

⚠️ **Bắt buộc gửi `wndOperateMode`** khi PUT window — nó quyết định `Rect` được hiểu theo hệ nào. Bỏ trống là nguyên nhân `badParameters`.

---

# 7. Đọc lỗi ISAPI

## 7.1. Cấu trúc

```xml
<ResponseStatus version="1.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <requestURL></requestURL>
  <statusCode>4</statusCode>
  <statusString>Invalid Operation</statusString>
  <subStatusCode>methodNotAllowed</subStatusCode>
</ResponseStatus>
```

**Luôn đọc `subStatusCode`.** `statusString` quá chung chung để dùng được.

## 7.2. `statusCode`

| Mã | Nghĩa |
|---|---|
| **0, 1** | OK (cả hai đều là thành công) |
| 2 | Device Busy |
| 3 | Device Error |
| 4 | Invalid Operation |
| 5 | Invalid XML Format |
| 6 | Invalid XML Content |
| 7 | Reboot Required |

## 7.3. Chuỗi lỗi thực tế đã gặp — dùng làm mẫu debug

`subStatusCode` chỉ thẳng vào chỗ cần sửa:

| Hành động | `subStatusCode` | Nghĩa |
|---|---|---|
| GET vào endpoint chỉ nhận PUT | `methodNotAllowed` | **URL đúng**, sai method |
| PUT không có body | `badXmlFormat` | Method đúng, body trống/không parse được |
| PUT body có element rỗng | `badParameters` | XML parse được, **giá trị** sai |
| PUT đúng | `ok` + `statusCode 1` | Xong |
| Gọi `channelID` không tồn tại | `invalidOperation` | ID sai — dùng list endpoint lấy ID thật |
| `closeAll` chưa có dây serial | `invalidOperation` | Hạ tầng chưa sẵn sàng |
| Firmware không hỗ trợ | `notSupport` | Gọi `capabilities` trước |

## 7.4. Bảng tra theo HTTP status

| HTTP | Nguyên nhân | Xử lý |
|---|---|---|
| **401** | Sai credential / Auth chưa phải Digest / sai algorithm | Console có 2 request không? Kiểm tra `stale` |
| **403** | Quyền không đủ, hoặc `forbiddenIP` | Đổi sang admin |
| **400** | Thiếu field `req`, sai namespace, sai `range` | GET trước rồi sửa |
| **404** | Sai URL / API không có trên firmware này | Đối chiếu `capabilities` |
| **500** + `notSupport` | Firmware không hỗ trợ | Gọi `capabilities` trước |

## 7.5. Nguyên tắc `capabilities` trước

Xuyên suốt tài liệu (mục 4.2.3): **trước khi gọi API chức năng, gọi `capabilities` tương ứng.** Nó tiết kiệm rất nhiều thời gian debug những API mà firmware không hỗ trợ.

## 7.6. Đọc annotation trong tài liệu (mục 4.2.2)

Comment trong XML mẫu là **đặc tả field**, không phải dữ liệu:

```xml
<!--ro, req, string, device name, range:[0,132]-->
```

| Ký hiệu | Nghĩa |
|---|---|
| `ro` | read-only — gửi lên bị bỏ qua |
| `rw` | read-write |
| `req` | **bắt buộc** — thiếu là 400 |
| `opt` | tuỳ chọn |
| `dep:` | phụ thuộc field khác |
| `range:[a,b]` | miền giá trị (⚠️ **không phải lúc nào cũng đúng** — xem mục 6.2) |

---

# 8. Quy trình test an toàn

## 8.1. Bốn vòng, theo đúng thứ tự

```
VÒNG 1 — Nhận dạng (chỉ GET, an toàn tuyệt đối, chạy được cả khi tường đang phục vụ)
  GET /SDK/activateStatus                            ← không cần auth, kiểm tra tầng mạng
  GET /ISAPI/Security/userCheck                      ← xác thực
  GET /ISAPI/System/deviceInfo                       ← model, firmware, SN
  GET /ISAPI/System/Board/status/capabilities        ← liệt kê board
  GET /ISAPI/DisplayDev/capabilities                 ← số cổng + cờ tính năng
  GET /ISAPI/DisplayDev/Video/outputs/channels       ← bảng ID output
  GET /ISAPI/DisplayDev/Video/inputs/channels        ← bảng ID input

VÒNG 2 — Năng lực tường (chỉ GET)
  GET /ISAPI/DisplayDev/VideoWall/capabilities       ← baseOutputSize, maxWindowNums
  GET /ISAPI/DisplayDev/VideoWall                    ← videoWallID
  GET /ISAPI/DisplayDev/VideoWall/<id>/outputs       ← map output ↔ ô
  GET /ISAPI/DisplayDev/VideoWall/<id>/windows
  GET /ISAPI/DisplayDev/VideoWall/<id>/windows/status

VÒNG 3 — Dò API tài liệu ghi thiếu (POST đọc, chưa ghi)
  POST /ISAPI/DisplayDev/VideoWall/GetVideoWallScale?format=json
  POST /ISAPI/DisplayDev/Video/streaming/channels
       → 200 = có · 404/notSupport = không

VÒNG 4 — GHI (chỉ khi tường không phục vụ ai)
  Bắt đầu ở Wall 1 (unbound) — không màn nào, sai cũng không ai thấy
  Đổi <name> trước — field vô hại nhất
  Rồi mới tới window CRUD trên Wall 2
```

## 8.2. Quy tắc khi ghi

1. **`GET` trước mỗi `PUT`**, lưu response ra file — đó là bản rollback của bạn
2. **Viết sẵn lệnh rollback TRƯỚC khi bấm Send**, không phải nghĩ ra sau khi hỏng
3. **Gửi payload tối thiểu** — chỉ field cần đổi. Response GET **không phải** payload PUT: nó chứa field `ro` và giá trị tính toán như `layerIdx`, `videoInputChannelID`
4. **Đọc đặc tả Request Message của mục PUT**, không phải Response Message của mục GET
5. **Verify hai lớp** — API trả OK chưa chắc màn đã đổi. Luôn nhìn màn hình thật
6. **Test trên Wall 1 trước** (unbound, rỗng) rồi mới sang Wall 2

## 8.3. ⛔ Tuyệt đối không dùng để test

| API | Hậu quả |
|---|---|
| `PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll` (không có `OutputID`) | Tắt toàn bộ tường — **không có API bật lại** |
| `DELETE /ISAPI/DisplayDev/VideoWall/<id>/windows` | Xóa sạch mọi window |
| `PUT /ISAPI/System/reboot` | Khởi động lại thiết bị |
| `PUT /ISAPI/System/factoryReset` | Xóa toàn bộ cấu hình |
| Đổi `workMode` cổng serial | Mất chức năng khác của cổng đó |
| Đổi IP / network | Mất kết nối |

## 8.4. Sao lưu cấu hình

API `GET /ISAPI/System/configurationData?secretkey=<...>&mode=<...>` **không dùng được ngay** — tài liệu không ghi giá trị `mode` hợp lệ, và `secretkey` phải là chuỗi **đã mã hoá** (không nói mã hoá kiểu gì). Truyền plaintext → `invalidOperation`.

**Dùng giao diện web:** Maintenance and Security → Maintenance → Export. Xong trong 2 click.

## 8.5. ⭐ Kỹ thuật DevTools — tìm API tài liệu ghi thiếu

Tài liệu **không đầy đủ**. Nhiều API chỉ xuất hiện trong luồng gọi ở chương 8 mà không có mục đặc tả riêng. Web của thiết bị **cũng gọi ISAPI**, nên đây là cách lấy được URL và payload thật:

1. Chrome → đăng nhập `http://10.10.9.236`
2. **F12 → Network → lọc Fetch/XHR**
3. Thực hiện thao tác cần tìm hiểu trên web
4. Xem request `/ISAPI/...` vừa chạy → **Request URL, Method, Payload, Response**
5. Chuột phải → **Copy → Copy as cURL**
6. Postman: **Import → Raw text** → dán

Cách này cho **payload đã được kiểm chứng chạy đúng trên chính firmware này** — đáng tin hơn XML mẫu trong tài liệu, vốn viết chung cho cả dòng sản phẩm.

Web thiết bị cũng là **nguồn sự thật** cho câu hỏi "thiết bị có làm được X không": nếu giao diện web không có nút đó thì gần như chắc chắn API cũng không có.

---

# 9. Trạng thái hiện tại & việc còn treo

## 9.1. Đã kiểm chứng chạy được

| Nhóm | API | Kết quả |
|---|---|---|
| Xác thực | `GET /ISAPI/Security/userCheck` | ✅ `statusValue 200`, `isActivated true` |
| Định danh | `GET /ISAPI/System/deviceInfo` | ✅ |
| Phần cứng | `GET /ISAPI/DisplayDev/decoingDevice/status?format=json` | ✅ Bản đồ slot đầy đủ |
| Năng lực | `GET /ISAPI/DisplayDev/capabilities` | ✅ |
| Output | `GET /ISAPI/DisplayDev/Video/outputs/channels[/<id>]` | ✅ Bảng ID |
| Tường | `GET /ISAPI/DisplayDev/VideoWall[/<id>/outputs]` | ✅ |
| Window | `GET /ISAPI/DisplayDev/VideoWall/<id>/windows` | ✅ |
| **Ghi** | `PUT /ISAPI/DisplayDev/VideoWall/<id>` (đổi `name`) | ✅ `statusCode 1` |
| **Ghi** | `PUT .../windows/<id>` (đổi `Rect` → 1920×3840) | ✅ Hiển thị đúng trên màn thật |

## 9.2. Thiết bị KHÔNG hỗ trợ — loại khỏi thiết kế

Đọc từ `GET /ISAPI/DisplayDev/capabilities`:

| Cờ = `false` | Hệ quả |
|---|---|
| `isSupportVideoPreview`, `isSupportSubStreamVideoPreview`, `isSupportWallNPreMonitor`, `isSupportWallLPreMonitor` | 🔴 **Không xem preview video nguồn qua mạng.** Toàn bộ nhóm `nPreMonitor` (9.7.2.2, 2.3, 2.14, 2.15, 2.16) vô dụng. Chỉ còn chụp **ảnh tĩnh** (`isSupportPictureCapture = true`) |
| `isSupportSaveSceneVirLed`, `isSupportSaveSceneBaseMap` | 🔴 **Scene không lưu chữ chạy và ảnh nền.** Chuyển scene xong hai thứ đó không được khôi phục — BE phải tự lưu và tự áp lại |
| `isSupportSceneCopy` | Không nhân bản scene |
| `isSupportPostionSetting` | Không chỉnh vị trí ảnh input (9.7.4.19) |
| `isSupportEDIDResolution` (cả in và out) | Không đọc/áp EDID — phải set resolution thủ công |
| `isSupportAutoSwitchMainSub` | Không tự chuyển main/sub stream (9.7.11.15/16) |
| `isSupportDisplayWinNo` | Không hiện số hiệu window lên tường |
| `isSupportAlarmLinkage` | Không liên động cảnh báo |

**Cờ = `true` cần biết:** `isSupportCutOffSetting` (crop — cần cho composite window), `isSupportScreenCtrl`, `isSupportRoam`, `isSupportScene` (128 scene), `isSupportPlan`, `isSupportBaseMap` (3 ảnh, JPEG, ≤1920×1080), `isSupportVirtualLED` (3 LED/tường), `isSupportJoinSignalCfg`, `isSupportGetVideoWallScale`, `isSupportModifyVideoWallScale`, `isSupportVideoOutputChannelStatus`, `streamingNums = 2048`.

### Hai kết luận về việc "tắt màn hình"

**1. Không có API bật/tắt từng cổng HDMI.** Trường `outputPortEnabled` **không phải** công tắc HDMI: nó không xuất hiện trong response danh sách, nằm cùng cụm với các trường `LEDSendCard*`, cả hai cổng đang hoạt động đều báo `false`, và set `true` không có tác dụng.

**2. Hikvision thiết kế việc tắt màn qua RS-232/485.** `PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll` gửi lệnh nguồn tới chính màn hình theo protocol của hãng màn. Điều kiện: đấu dây serial + cổng serial đặt `workMode = screenCtrl` + đúng protocol. Hiện cổng `reusePort` chưa cấu hình → API trả `invalidOperation`.

📌 Request body của `closeAll` nhận **`VideoWallID`** và **`OutputID`**, cả hai `opt` → **tắt được từng màn riêng lẻ**, ngược với ghi chú "chỉ tắt tất cả" trong Excel. Cần test sau khi có dây serial.

📌 **Không có API đọc trạng thái bật/tắt của màn hình.** `ScreenCtrl` là một chiều. Nếu ai cầm remote tắt màn, controller không biết.

### Trạng thái màn hình — dùng gì

**API tốt nhất: `GET /ISAPI/DisplayDev/Video/outputs/channels`** — một request, cả 8 cổng, kèm tên, độ phân giải, board/port và:

```xml
<outputPortAccessStatus>normal | notConnected</outputPortAccessStatus>
```

⚠️ Trường này **không có trong tài liệu** nhưng firmware trả về. Nhẹ hơn hẳn `decoingDevice/status`.

### Sức khỏe thiết bị (không phải sức khỏe màn hình)

`GET /ISAPI/DisplayDev/decoingDevice/status?format=json`:

| Chỉ số | Đường dẫn | Giá trị đo được |
|---|---|---|
| Nhiệt độ backplane | `BackplaneStatusList[].backplaneTemperature` | **60 °C** |
| CPU / RAM từng board | `SubBoardStatusList[].CPUUtilization` / `.memoryUtilization` | 0–4 % / 36–50 % |
| Thời gian chạy | `.runTime` | main 12.444 s |
| Lỗi | `.exceptionList`, `.status` | rỗng / normal |

> 💡 **Mẹo giám sát:** theo dõi `runTime` từng board. **Giảm đột ngột = board vừa khởi động lại** — dấu hiệu sự cố sớm, xuất hiện trước cả khi `exceptionList` có gì. Cách này đã phát hiện board ID 8 chỉ chạy 33 giây trong khi các board khác đã chạy hàng giờ.

**Sức khỏe màn hình** (nhiệt độ, giờ chạy) chỉ đọc được qua **serial transparent transmission** (mục 5.9): `PUT .../Transparent/channels/<id>/open` → `transData` → `close`, gửi lệnh gốc của hãng màn. Cần dây + biết protocol (Samsung MDC, LG, NEC, Philips SICP…). ⚠️ Một cổng serial chỉ có một `workMode` — chọn `transparent` thì mất `screenCtrl`.

## 9.3. 🔴 API còn thiếu trong Excel — cần tìm payload

Chương **8.2.2 API Calling Flow** (luồng chuẩn của hãng để mở window lên tường) gọi những API **không có mục đặc tả riêng** ở chương 9:

| API | Vai trò | Ưu tiên |
|---|---|---|
| `POST /ISAPI/DisplayDev/VideoWall/<vwID>/outputs` | **Gán output vào ô màn hình.** Excel chỉ có bản GET | 🔴 **Cao nhất** — không có nó thì BE không setup được tường, và Wall 1 mãi `unbound` |
| `POST /ISAPI/DisplayDev/VideoWall/GetVideoWallScale?format=json` | Đọc tỉ lệ tường | Đã xác nhận `true` |
| `POST /ISAPI/DisplayDev/VideoWall/ModifyVideoWallScale?format=json` | Sửa tỉ lệ tường | Đã xác nhận `true` |
| `POST /ISAPI/DisplayDev/Video/streaming/channels` | Signal resource. Excel ghi *"ngoài phạm vi"* nhưng luồng chuẩn **bắt buộc** | Cao |
| `POST /ISAPI/DisplayDev/VideoWall/<ID>/scene` | **Tạo scene mới.** Excel chỉ có GET/PUT | Trung bình |
| `DELETE /ISAPI/DisplayDev/VideoWall/<vwID>/scene[/<SID>]` | Xóa scene | Trung bình |
| URL của `isSupportVideoOutputChannelStatus` | API trạng thái output nhẹ | Thấp |
| Output identity (`isSupportOutputIdentity = true`) | Hiện số lên màn thật → map cổng ↔ màn không cần rút cáp | Hữu ích |

**Cách tìm: DevTools (mục 8.5).**

### Luồng chuẩn đầy đủ — khung cho BE

```
1. GET  /ISAPI/DisplayDev/VideoWall/capabilities           → maxWallNums, maxWindowNums
2. GET  /ISAPI/DisplayDev/VideoWall                        → cấu hình tất cả tường
3. POST /ISAPI/DisplayDev/VideoWall/GetVideoWallScale?format=json     (tuỳ chọn)
   POST /ISAPI/DisplayDev/VideoWall/ModifyVideoWallScale?format=json  (tuỳ chọn)
4. GET|PUT /ISAPI/DisplayDev/Video/outputs/channels[/<channelID>]
5. POST /ISAPI/DisplayDev/VideoWall/<vwID>/outputs         → gán output vào màn hình
6. POST /ISAPI/DisplayDev/Video/streaming/channels         → lấy/thêm signal resource
7. POST /ISAPI/DisplayDev/VideoWall/<vwID>/windows         → mở window
8. PUT  /ISAPI/DisplayDev/VideoWall/<vwID>/windows/<VWMWID>/sub/<WMSWID>  → gán nguồn
9. GET  /ISAPI/DisplayDev/VideoWall/<vwID>/windows/status   → trạng thái decode
```

## 9.4. Việc nên test tiếp

| # | Việc | Vì sao |
|---|---|---|
| 1 | `POST .../windows` tạo window | Thao tác lõi số 1 của FE. Chưa test. Kết hợp làm luôn việc khôi phục window đã xóa |
| 2 | `GET .../Video/inputs/channels` | Chưa chạy lần nào. Nền cho panel "Nguồn tín hiệu" |
| 3 | `PUT .../windows/<id>/sub/1` đổi nguồn | Chính là hành động "kéo nguồn thả vào cửa sổ" |
| 4 | `GET .../windows/status` | Excel ghi là *"API giám sát lõi màn Monitoring"*, poll 3–5s. Cần đo response nặng bao nhiêu |
| 5 | `PUT .../windows/<id>/top` và `/bottom` | Z-order, nhanh và vô hại |
| 6 | Scene: `POST scene` → `saveData` → `activate` | Lưu/gọi lại bố cục |
| 7 | Crop (`cutOff`, 9.7.4.16) + `joinSignal` | Cần cho composite window |

## 9.5. Lưu ý cho kiến trúc 4 controller

1. **Không có API nào gộp 4 máy.** `videoWallID` là cục bộ trong từng controller. BE phải giữ bảng map `controller_id → ip → videoWallID → outputs`
2. **`channelID` không đảm bảo trùng nhau giữa các máy** — mỗi máy một bảng riêng, và **làm mới khi thay board**
3. **So `firmwareVersion` cả 4 máy.** Firmware lệch = tập API hỗ trợ lệch → tình huống "3 máy chạy 1 máy không" rất khó tìm nguyên nhân. Đưa về cùng phiên bản trước khi code BE
4. **Đặt cùng `Digest Algorithm Type`** trên cả 4 máy
5. **Đồng bộ `GET/PUT /ISAPI/System/time`** — bắt buộc nếu dùng Video Wall Plan (9.7.6), lệch giờ làm plan chạy so le
6. **Timeout ngắn (3–5 s)** cho request poll. Một máy chết không được treo cả 4
7. **Không retry ngay khi gặp 401** — rủi ro khóa IP. Chỉ retry với lỗi mạng/timeout
8. **Mỗi controller một `requests.Session()` riêng** — không tái dùng nonce giữa các IP

## 9.6. Backend không cần script Digest

Toàn bộ mục 3.3 **chỉ là vấn đề của công cụ Postman**. Backend dùng thư viện chuẩn là chạy thẳng:

```python
import requests
from requests.auth import HTTPDigestAuth

r = requests.get('http://10.10.9.236/ISAPI/Security/userCheck',
                 auth=HTTPDigestAuth('admin', PASSWORD),
                 timeout=5)
print(r.text)
```

`requests` xử lý đúng cả trường hợp challenge thiếu `algorithm`.

---

## Tóm tắt 10 điều quan trọng nhất

1. **Không có API login.** Kết nối = `GET /ISAPI/Security/userCheck` trả 200 bằng Digest Auth
2. **401 rồi 200 là bình thường** — cơ chế Digest cần 2 round-trip
3. **Sai mật khẩu 2 lần thì DỪNG.** Thiết bị khóa IP, và firmware này không cho biết còn mấy lượt
4. **Postman Digest Auth có bug** — tick "disable retrying" hoặc dùng script ở mục 3.3
5. **`channelID` không phải 1..N** — đọc từ API, đừng hardcode
6. **`uniformCoordinate`: mỗi màn = ô vuông 1920×1920**, không phải 1920×1080. Giới hạn `[0,1920]` trong tài liệu là sai
7. **PUT gửi tối thiểu** — response GET không phải payload PUT
8. **Luôn `capabilities` trước API chức năng**, luôn `GET` trước `PUT`
9. **Tài liệu không đầy đủ.** Dùng DevTools trên web thiết bị để lấy URL và payload thật
10. **Test ở Wall 1 (unbound) trước** — không màn nào, sai cũng không ai thấy

---

*Tài liệu tổng hợp từ buổi test đầu tiên trên `10.10.9.236`. Nguồn: ISAPI Controller – Videowall Controller (mục 2.1.1, 2.2, 3.2, 3.3, 4.1, 4.2, 5.9, 8.1.2, 8.2.2, 9.1, 9.2, 9.7) + DS-C66S Series Datasheet + kết quả đo thực tế trên thiết bị.*
