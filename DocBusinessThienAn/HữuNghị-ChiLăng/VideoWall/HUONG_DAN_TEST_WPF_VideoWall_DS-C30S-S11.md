# Hướng dẫn test UI WPF Video Wall theo KịchBản DS‑C30S‑S11 (12 màn)

> Đi kèm: [`KichBan_VideoWall_DS-C30S-S11_12Man.md`](KichBan_VideoWall_DS-C30S-S11_12Man.md) — KB gốc, nguồn
> duy nhất của các bước và XML body mẫu.
>
> Mỗi bước ghi **kỳ vọng (Mock)** và **kỳ vọng (HW thật DS‑C30S‑S11)** riêng. Ký hiệu preset `P 9.7.x.y`
> = section trong danh sách API của tab. `Endpoint box` = ô "Endpoint" ở đầu form tab ISAPI (sửa tự do).

---

## Phần 0 — Chuẩn bị

### 0.1. Build & mở UI WPF

```powershell
dotnet build TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.WPF/Module.VideoWall.WPF.csproj
# rồi chạy file .exe trong thư mục bin\... (net10.0-windows), hoặc F5 từ IDE trên Windows
```

### 0.2. Chế độ A — chạy với **MockServer** (nhanh, không rủi ro)

```powershell
dotnet run --project scripts/VwMockServerRunner
# In ra: BaseUrl http://127.0.0.1:18080/  · Port 18080..18083 · admin / Password123!
```

Trên thanh header WPF nhập: **IP** `127.0.0.1` · **Port** `18080` · **Account** `admin` ·
**Password** `Password123!` (thực ra mật khẩu nào cũng qua với Mock) · **WallNo** để trống, bấm Probe sẽ tự gợi ý.

### 0.3. Chế độ B — chạy với **controller thật DS‑C30S‑S11**

Nhập **IP / Port / Account / Password** thật của thiết bị. Port ISAPI HTTP thường `80` / `8080` / `443`.

#### 0.3.1. Backup & khôi phục (bọc quanh mỗi phiên test có lệnh ghi)

1. **Trước khi test** — vào **web UI thiết bị → Maintenance/Configuration → Export/Backup**, tải file cấu hình về máy. File này chứa: lưới `WallOutputList`, toàn bộ scene + bố cục, plan, virtualLED, ảnh nền, tên nguồn, cấu hình serial/mạng/user.
2. **Test thoải mái** các bước ghi (PUT/POST/DELETE) — kể cả trên tường `bound`.
3. **Sau khi test** — web UI → **Import/Restore** file backup → chờ thiết bị áp lại (một số firmware **reboot**) → `activate` lại scene gốc (KB‑14) → **verify tường hiển thị đúng như cũ trước khi bàn giao trực ban**.

> **Import config KHÔNG tự khôi phục 3 thứ — phải làm tay sau restore:**
> - **KB‑17 `closeAll`**: là lệnh tắt nguồn màn qua RS‑232/485, **không phải config** → import không bật màn lại; ISAPI không có API bật màn (chỉ `openScreen` trong Plan). ⇒ Bật màn tay/remote; KB‑17 vẫn để **cuối cùng**.
> - Scene **đang chiếu** (`isRunning`) → `activate` lại tay.
> - Trạng thái **decoding** của window → có thể phải `start` lại (KB‑11).
> - `PUT /ISAPI/System/time` (KB‑19, nếu có chỉnh) → chỉnh giờ lại tay, không nằm trong backup.

#### 0.3.2. Vẫn cần lưu ý dù đã có backup

> - **Đóng web UI thiết bị và iVMS** khi đang chạy WPF (nếu không: `multipleVideowallClientConflict 0x4000A4F8`). Chỉ mở web UI lúc export/import backup.
> - Chạy trong **khung giờ bảo trì** — import config có thể reboot thiết bị; restore + activate + verify mất vài phút.
> - Bước "Kết nối" trả 401: xem `stale`. `stale="FALSE"` = **sai mật khẩu, DỪNG NGAY** (thiết bị khoá IP — restore config **không** mở khoá).
> - **`bound` / `unbound`** = field `wallBindOutputStatus` trong `GET /ISAPI/DisplayDev/VideoWall` (KB‑01 #6, xem Tab 6 `P 9.7.5.2` hoặc bấm Probe): `unbound` = tường chưa gán màn (khung trống — nhiều lệnh mở window/activate trả `invalidOperation`); `bound` = tường đang gán màn & có thể đang chiếu. Đã có backup thì test trên `bound` cũng được, nhưng ưu tiên `unbound` nếu có (đỡ phải restore).
> - Thứ tự an toàn KB Phụ lục P7: KB‑01→02→04 (GET) → 03 → 05…11 → 08/09/10 → 13/14/15 → 16 → 12 (dọn) → 18/19/20 → **17 cuối cùng**.

### 0.4. Nơi xem kết quả

| Nơi | Nội dung |
|---|---|
| **Khung Response** (dưới Tab ISAPI) | Body XML/JSON trả về của lệnh **Gửi ISAPI** gần nhất |
| **Tab Logs** (đáy cửa sổ) | Mọi bước gọi API: Method · Endpoint · HttpStatus · Success · Message. **Nhấp đúp** 1 dòng để xem Request/Response đầy đủ |
| Nút **Xuất log** | Lưu toàn bộ log phiên ra `.json` (không cần server) |
| File log phiên | `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\` và `<repo>\logs\session_YYYYMMDD.jsonl` |

### 0.5. Quy ước "Gửi lệnh tuỳ chỉnh" (dùng nhiều ở KB‑03/07/13/15/17/19)

UI **không cho sửa Method trực tiếp** — Method đi theo preset đang chọn. Để gửi `POST`/`DELETE`/`PUT`
tới một đường dẫn không có preset:

1. Mở tab bất kỳ, ở **Danh sách API** chọn **một preset có đúng Method cần** (xem bảng dưới).
2. Sửa ô **Endpoint** thành đường dẫn thật (điền sẵn `videoWallID`, `VWMWID`, `SID`… — không để `{...}`).
3. Dán **Body** (từ KịchBản) nếu là POST/PUT.
4. Bấm **Gửi ISAPI**, đọc **Response** + **Logs**.

| Method cần | Preset "mồi" tiện dùng |
|---|---|
| `GET` | bất kỳ (vd Tab 2 `P 9.7.1.4`) |
| `POST` | Tab 7 `P 9.7.6.1` (Plan) · Tab 10 `P 9.7.9.3` (TextLED) · Tab 12 `P 9.7.11.4` (Window) |
| `PUT` | Tab 8 `P 9.7.7.2` (Scene) · Tab 12 `P 9.7.11.6` (Window) |
| `DELETE` | Tab 12 `P 9.7.11.7` (Window) · Tab 5 `P 9.7.4.31` (Stream) |

---

## Phần 1 — Kết nối & khảo sát (KB‑01, KB‑02)

### Thuật ngữ (đọc trước bước 1.2)

```
Camera / PC ──► [INPUT channels] ──► [ ma trận giải mã ] ──► [OUTPUT channels] ──► 12 màn hình vật lý
                (cổng VÀO)                                    (cổng RA HDMI)

VIDEO WALL (logic): xếp 12 OUTPUT channel thành lưới 4×3 → 1 mặt phẳng ảo 7680 × 5760
   • WallOutput[]  = ô lưới nào  ↔  output channel nào
   • WallWindow[]  = cửa sổ chiếu nguồn, phủ lên vùng nào của mặt phẳng đó
```

| Từ | Nghĩa |
|---|---|
| **Channel** (kênh / cổng vật lý) | Cổng thật trên bo mạch controller. **Input channel** = cổng **VÀO** (HDMI/SDI/VGA/DVI/RTSP từ camera, PC, đầu ghi) — chính là "nguồn tín hiệu". **Output channel** = cổng **RA** HDMI tới từng màn hình vật lý (DS‑C30S‑S11 = **12**). `id` là số tổ hợp byte `T×16777216 + boardID×65536 + portID` — **KHÔNG phải 1..N**, luôn đọc `id` thật từ list. |
| **Video Wall** (tường ghép) | Một **bố cục lưới logic** gộp nhiều output channel thành 1 mặt phẳng hiển thị liền mạch. 1 controller có thể định nghĩa **nhiều tường**. `GET /ISAPI/DisplayDev/VideoWall` trả **danh sách tường** — mỗi cái có `id`, `name`, `wallBindOutputStatus` (bound/unbound — xem mục 0.3). |
| **`wallNo` / `videoWallID`** | Số `id` của **1 tường cụ thể**. Ô **WallNo** ở thanh header WPF = số này; mọi lệnh ở Tab 6/8/12 ghép vào path `.../VideoWall/{wallNo}/...`. 🔴 **Đừng đoán là 1** — bấm **Probe** để WPF đọc danh sách tường và cho chọn. |
| **`WallOutput`** | 1 **ô trong lưới** của tường: gắn 1 `outputID` (= một output channel) + `Rect` (x, y, width, height của ô trên canvas ảo). **KB‑03** thiết lập cái này. Suy vị trí: `gridCol = Rect.x / 1920`, `gridRow = Rect.y / 1920`. |
| **`WallWindow`** | 1 **cửa sổ chiếu nguồn** đang mở trên tường (KB‑05…12): có `Rect` riêng, **có thể vắt qua nhiều ô** (nhiều output channel). |

**Hai endpoint tên "outputs" — khác nhau:**

| Endpoint | Trả gì |
|---|---|
| `GET /ISAPI/DisplayDev/Video/outputs/channels` | **12 cổng ra vật lý** của controller (bo mạch, độ phân giải, `outputPortAccessStatus` = đã cắm màn chưa) — **chưa** gắn với tường nào |
| `GET /ISAPI/DisplayDev/VideoWall/{wallNo}/outputs` | 12 cổng đó **đã được xếp vào lưới của tường `{wallNo}`** như thế nào (ô nào, toạ độ nào) — dùng **verify KB‑03** |

### Bước 1.1 — Kết nối (KB‑01 #2)

- Bấm **🔌 Kết nối** trên header.
- **Kỳ vọng (Mock)**: "Kết nối trực tiếp thành công tới 127.0.0.1:18080 (… ms)."; TabControl 12 tab **sáng lên** (bật `IsConnected`).
- **Kỳ vọng (HW thật)**: tương tự. Nếu 401 → xem `stale` (mục 0.3).

### Bước 1.2 — Khảo sát (Probe) (KB‑01 #4/#5/#6, KB‑02 #1/#3)

> Thuật ngữ `channel` / `Video Wall` / `wallNo` / 2 kiểu `outputs` — xem **mục Thuật ngữ** ngay trên.

- Bấm **🔍 Khảo sát (Probe)**.
- WPF tự chạy: `userCheck` → `GET DisplayDev/VideoWall/capabilities` → `GET DisplayDev/VideoWall` (danh sách tường → lấy `wallNo`) → (nếu có WallNo) `GET .../{wall}/outputs` (lưới ô của tường) → `GET Video/inputs/channels` (danh sách nguồn vào).
- Đọc **4 thẻ metric** ở Tab 1: Tổng kích thước tường · Cổng ra (Outputs) · Nguồn vào (Inputs) · Max window / Max scene.
- **Kỳ vọng (Mock)**: tìm thấy 2 tường (`id=1` VideoWall1 `unbound`, `id=2` HoangNhu `bound`); chọn `WallNo=1`. Outputs = **2**, Inputs = **2**, Max window `512`, Max scene `128`, canvas `1920 × 3840` (do mock chỉ 2 ô xếp dọc). 👉 **Mock chỉ giả lập 2 cổng — số liệu không đại diện tường 12 màn thật.**
- **Kỳ vọng (HW thật)**: `videoWallID` thật (đừng đoán là 1 — KB‑01 #6). Outputs = **12**, canvas nên là **7680 × 5760** (lưới 4×3). Ghi lại `baseOutputSize` (phải = **1920**), `maxWindowNums`, `maxSceneNums`, `isSupportScene/Roam/Plan/VirtualLED/BaseMap`.

### Bước 1.3 — Đọc chi tiết Output/Input (KB‑02)

- Tab 1 → sub‑tab **"🖥️ Chi tiết Cổng ra & Kênh tín hiệu (Topology)"**: xem bảng Outputs (Hardware ID, X, Y, W, H, độ phân giải, `coordinateMode`) và bảng Inputs (ID kênh, tên, loại cổng, trạng thái truy cập, board/port).
- **Ghi lại 12 `outputID` thật** (HW thật) — cần cho KB‑03. Công thức KB: `id = 0x01·16777216 + boardID·65536 + portID`.
- **KB‑02 #2** năng lực từng output: Tab 4 → `P 9.7.3.7`, điền `channelID`, **Gửi ISAPI**.
  - Mock: `OutputResolutionListCap` → `1920*1080@60HZ`. HW thật: `OutputResolutionCapList` đầy đủ.
- **KB‑02 #4** snapshot nguồn: Tab 5 → `P 9.7.4.18`, điền `channelID`, **Gửi ISAPI**.
  - Mock: trả JPEG mẫu (Response hiển thị nhị phân/không rõ). HW thật: ảnh JPEG thật của nguồn.
- **KB‑02 (đổi tên input – test ghi an toàn)**: Tab 5 → `P 9.7.4.10`, `channelID` = id thật, Body:
  ```xml
  <?xml version="1.0" encoding="UTF-8"?>
  <VideoInputChannel xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
    <id>16842753</id>
    <name>Dashboard GT</name>
  </VideoInputChannel>
  ```
  - Kỳ vọng (cả 2): `statusCode` 0/1, `subStatusCode = ok`.

### Bước 1.4 — KB‑01 #1 (`GET /SDK/activateStatus`) & #3 (`GET /ISAPI/System/deviceInfo`)

- **KB‑01 #1**: Tab 2 (Board) → chọn preset **`SYS-1`** (`SDK/activateStatus`) → **Gửi ISAPI**.
  - Mock & HW thật: `<Activated><Activated>true</Activated></Activated>`.
- **KB‑01 #3**: Tab 2 (Board) → chọn preset **`SYS-2`** (`ISAPI/System/deviceInfo`) → **Gửi ISAPI**.
  - Mock & HW thật: `<DeviceInfo>` với `model` (`DS-C30S-S11`), `serialNumber`, `firmwareVersion`.

---

## Phần 2 — KB‑03 … KB‑20

> Chuẩn bị trước mỗi lần chạy lại (HW thật, tường sandbox): "làm sạch tường" bằng `DELETE .../windows`
> (KB‑12) — **chỉ khi chắc chắn không xoá nhầm tường đang vận hành**.

### KB‑03 — Gán 12 output vào lưới 4×3

| # | Tab / thao tác | Body |
|---|---|---|
| 3.1 | Tab 6 → `P 9.7.5.5` (`GET .../VideoWall/1/outputs`) → Gửi ISAPI | — |
| 3.2 | Tab 6 → chọn `P 9.7.5.3` (`PUT .../VideoWall/{videoWallID}`), điền `videoWallID=1` | Sample body `VideoWallPut` đã có sẵn chuẩn KB-03 (1920×1920/ô). Trên HW thật thay bằng 12 `outputID` thật từ bước 1.3 |
| 3.3 | Tab 6 → `P 9.7.5.5` lần nữa để verify | — |

- **Kỳ vọng (Mock)**: 3.2 trả `statusCode` 1 / `ok`. 3.3 trả `WallOutputList`.
- **Kỳ vọng (HW thật)**: 3.2 `ok`. 3.3 phải trả **đủ 12** `WallOutput`, suy vị trí ô: `gridCol = Rect.x/1920`, `gridRow = Rect.y/1920`.

### KB‑04 — Đọc bố cục & trạng thái hiện tại (chỉ GET, an toàn)

| # | Tab / preset | Kỳ vọng Mock | Kỳ vọng HW thật |
|---|---|---|---|
| 4.1 | Tab 6 · `P 9.7.5.4` (`GET .../VideoWall/1`) | `<VideoWall>` id 1, `wallBindOutputStatus` | cấu hình đầy đủ + `sceneSwitchDelayEnabled` |
| 4.2 | Tab 6 · `P 9.7.5.5` (`GET .../VideoWall/1/outputs`) | 2 `WallOutput` | 12 `WallOutput` |
| 4.3 | Tab 12 · `P 9.7.11.2` (`GET .../VideoWall/1/windows`) | danh sách window đại diện | danh sách window thật + `layerIdx` |
| 4.4 | Tab 3 · `P 9.7.2.8` (`GET .../VideoWall/1/windows/status`) | `<WallWindowStatusList>` | `<WallWindowStatusList>` với `isDecoding`, `videoFPS`, `streamRate` |

### KB‑05 — Mở window phủ 1 màn + gán nguồn

**Cách khuyến nghị (an toàn, XML đúng) — dùng SceneSetup:**

1. Tab 1 → "➕ Tạo mới" → nhập tên → "💾 Lưu kịch bản".
2. Sub‑tab "🖼️ Bố cục Ô Camera" → "➕ Thêm ô camera" (hoặc chọn nút Mẫu bố cục). Chỉnh **Màn bắt đầu / X / Y / Kích thước / Camera / Lớp (Z)** (hệ toạ độ chuẩn `UniformTileSize = 1920`).
3. "💾 Lưu cấu hình" → **🚀 Áp dụng & Đẩy xuống thiết bị**.
4. Verify: Tab 12 `P 9.7.11.2` (`GET .../windows`) → lấy `VWMWID` mới.

**Cách thủ công (đối chiếu từng lệnh KB):**

| # | Tab / preset | Ghi chú |
|---|---|---|
| 5.1 | Tab 12 · `P 9.7.11.14` (`GET .../windows/capabilities`) | Mock: `WallWindowCap`. HW: kiểm `windowMode opt`, `isSupportWinTopBottom` |
| 5.2 | Tab 12 · `P 9.7.11.4` (`POST .../windows`) | Sample body `WallWindow` đã chuẩn hoá theo ISAPI 09A (`<Rect><Coordinate><x><y>`, `uniformCoordinate`, `signalMode` = `video input`) |
| 5.3 | Tab 12 · `P 9.7.11.2` | Mock: `<ID>33554435</ID>` ở 5.2. HW: `VWMWID` mới xuất hiện |
| 5.4 | Tab 3 · `P 9.7.2.5` (`PUT .../windows/{VWMWID}/sub/1/start`) | điền `videoWallID`, `VWMWID`, `VWSWID=1`. Không body |
| 5.5 | Tab 3 · `P 9.7.2.6` (`GET .../windows/{VWMWID}/sub/1/status`) | `<WallWindowStatusList>` với `<isDecoding>true</isDecoding>` |

### KB‑06 — Window phủ nhiều màn (cần `isSupportRoam=true`)

Giống KB‑05, **chỉ đổi `Rect`**:
- Khối 2×2 góc trên‑trái: `<Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>3840</width><height>3840</height></Rect>`
- Toàn tường 12 màn: `width=7680`, `height=5760`.
- Mock: trả OK bất kể `Rect`. HW thật: hợp lệ tới `7680 × 5760`.

### KB‑07 — Đổi nguồn của window

- **Cách 1** (Tab 12 → `P 9.7.11.18`): chọn preset **`9.7.11.18`** ("Đổi nguồn của 1 cửa sổ con"), điền `videoWallID=1`, `VWMWID`, `VWSWID=1`, sample body `SubWindowSource` đã có sẵn.
- **Cách 2** (Tab 12 → `P 9.7.11.6`): `PUT .../windows/{VWMWID}` với body `<WallWindow>`.
- Verify: Tab 3 · `P 9.7.2.6` (trả `<WallWindowStatusList>`).

### KB‑08 — Di chuyển / resize window

- Tab 12 · `P 9.7.11.6` (`PUT .../windows/{VWMWID}`), Body:
  ```xml
  <?xml version="1.0" encoding="UTF-8"?>
  <WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
    <id>33554433</id>
    <wndOperateMode>uniformCoordinate</wndOperateMode>
    <Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>7680</width><height>5760</height></Rect>
  </WallWindow>
  ```
- Chế độ pixel: thay `<wndOperateMode>resolutionCoordinate</wndOperateMode>` + `<ResolutionRect>…`.
- Verify: Tab 12 · `P 9.7.11.5` (`GET .../windows/{VWMWID}`).
- **Mock**: trả OK, nhưng `GET .../windows` trả danh sách window tĩnh (không có window store trạng thái) → không thấy `Rect` mới.
- **HW thật**: nếu PUT `ok` mà tường không đổi → window khác `layerIdx` cao hơn đang đè → xem KB‑09.

### KB‑09 — Z‑order (top / bottom)

| # | Tab / preset |
|---|---|
| 9.1 | Tab 12 · `P 9.7.11.14` — kiểm `isSupportWinTopBottom` |
| 9.2 | Tab 12 · `P 9.7.11.13` (`PUT .../windows/{VWMWID}/top`) — không body |
| 9.3 | Tab 12 · `P 9.7.11.8` (`PUT .../windows/{VWMWID}/bottom`) — không body |
| 9.4 | Tab 12 · `P 9.7.11.2` — verify `layerIdx` |

- **Mock**: 9.2/9.3 trả `ok`; 9.4 `layerIdx` **không đổi** (Mock window list tĩnh).
- **HW thật**: `layerIdx` là **số lớn nằm trên**, chỉ‑đọc; chỉ có `/top` và `/bottom`.

### KB‑10 — Chia window thành 4 / 9 / 16 ô

- Tab 12 · `P 9.7.11.6` (`PUT .../windows/{VWMWID}`), Body có `<windowMode>4</windowMode>` + `<wndShowMode>subWndMode</wndShowMode>` + `<SubWindowList>` 4 `<SubWindow>` (id 1..4) như KB‑10.
- Start từng sub: Tab 3 · `P 9.7.2.5` với `VWSWID` = 1..4. Verify: `P 9.7.2.6`.
- `windowMode` hợp lệ: `1, 4, 9, 16`. Phóng to 1 ô: `<wndShowMode>fullScreenMode</wndShowMode><amplifyingSubWndNo>3</amplifyingSubWndNo>` (firmware **không** trả lại field này khi GET).
- **Mock**: trả OK, không dựng sub‑window thật. **HW thật**: kiểm hiển thị vật lý.

### KB‑11 — Start / Stop decoding

| Hành động | Tab / preset |
|---|---|
| Start | Tab 3 · `P 9.7.2.5` (`PUT .../sub/1/start`) |
| Stop | Tab 3 · `P 9.7.2.7` (`PUT .../sub/1/stop`) |
| Status 1 window | Tab 3 · `P 9.7.2.6` (`GET .../sub/1/status`) |
| Status tất cả | Tab 3 · `P 9.7.2.8` (`GET .../windows/status`) |

- Không body. Nếu `badXmlFormat` → gửi placeholder `<Request xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0"></Request>`.
- **Mock**: start/stop `ok`; cả `.../sub/1/status` và `.../windows/status` trả `<WallWindowStatusList>` có `<isDecoding>true</isDecoding>`, `videoFPS`, `streamRate` (đã chuẩn hoá theo KB‑11) — nhưng là **số cố định**, không phản ánh nguồn thật.
- **HW thật**: `<WallWindowStatusList>…<SubWinStatus><isLinked>…<isDecoding>…<imageWidth>…<videoFPS>…</SubWinStatus>` với giá trị thật. Mã lỗi decode khi nguồn hỏng: xem KB Phụ lục P5.

### KB‑12 — Xoá window

| Hành động | Tab / preset |
|---|---|
| Xoá 1 window | Tab 12 · `P 9.7.11.7` (`DELETE .../windows/{VWMWID}`) |
| Xoá tất cả | Tab 12 · `P 9.7.11.3` (`DELETE .../windows`) |

- Không body. **Mock**: `ok`, nhưng `GET .../windows` vẫn trả danh sách tĩnh (không phản ánh xoá). **HW thật**: tường sạch window (hiện nền). `DELETE .../windows` **không hoàn tác** — đây cũng là cách "làm sạch tường" trước khi test lại.

### KB‑13 — Tạo scene & lưu bố cục

| # | Tab / thao tác | Body |
|---|---|---|
| 13.1 | Tab 8 · `P 9.7.7.5` (`GET .../scene/capabilities`) | — |
| 13.2 | Tab 8 · `P 9.7.7.1` (`GET .../scene`) — đếm scene đã có | — |
| 13.3 | Tab 8 · `P 9.7.7.X1` (`POST .../scene`) | Sample body `ScenePost` đã có sẵn (`<WallScene><name>Ca dem</name></WallScene>`) |
| 13.4 | *(dựng bố cục THẬT lên tường: chạy KB‑05…KB‑10)* | — |
| 13.5 | Tab 8 · `P 9.7.7.2` (`PUT .../scene/3`) — đặt tên | `<WallScene …><id>3</id><name>Ca dem</name></WallScene>` |
| 13.6 | Tab 8 · `P 9.7.7.4` (`PUT .../scene/3/saveData`) — ⭐ chụp bố cục | placeholder `<Request xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0"></Request>` |

- **Kỳ vọng (Mock)**: 13.3 trả `statusCode` 1 kèm `<ID>` cấp mới (lưu vào `SceneStore`); 13.2 trả danh sách scene cập nhật. 13.6 `ok` (mô phỏng lỗi qua `--savedata-fail`).
- **Kỳ vọng (HW thật)**: 13.3 `<ID>` trong `ResponseStatus` (hoặc diff `GET /scene` trước/sau). `saveData` **chụp tường đang chạy** — phải dựng bố cục thật trước (13.4).

### KB‑14 — ACTIVE scene lên tường  ✅ chạy trọn vẹn cả 2 môi trường

| # | Tab / thao tác |
|---|---|
| 14.1 | Tab 8 · `P 9.7.7.6` (`GET .../scene/isRunning`) — SID đang chạy |
| 14.2 | Tab 8 · `P 9.7.7.1` (`GET .../scene`) — xác nhận SID tồn tại |
| 14.3 | Tab 8 · `P 9.7.7.3` (`PUT .../scene/3/activate`) — **không body, không Content‑Type** · hoặc nút **⚡ Kích hoạt chiếu (Active)** ở Tab 1 |
| 14.4 | *chờ 2–3 giây* |
| 14.5 | Tab 8 · `P 9.7.7.6` — verify `sceneID` = 3 |
| 14.6 | Tab 12 · `P 9.7.11.2` — đọc bố cục mới |

- **Mock**: 14.3 ghi `ActiveSceneId`; 14.5 trả đúng SID vừa activate (**state**). Nút "⚡ Kích hoạt chiếu" ở Tab 1 gọi đúng `PUT .../scene/{sid}/activate` (`VwDirectISAPIClient.ActivateScene`).
- **HW thật**: nếu `inSceneSwitchingPleaseDoNotOperate (0x4000A1AB)` → **không gọi lệnh nào khác**, chờ 2–3s rồi verify bằng `isRunning`, **không retry activate**.

### KB‑15 — Quản lý scene

| Hành động | Tab / thao tác | Mock | HW thật |
|---|---|---|---|
| Liệt kê | Tab 8 · `P 9.7.7.1` | List từ `SceneStore` | list thật (chỉ `id`+`name`) |
| Đổi tên | Tab 8 · `P 9.7.7.2` (`PUT .../scene/3`) | `ok` | `ok` (chỉ sửa `id`/`name`) |
| Xoá 1 | Tab 8 · `P 9.7.7.X2` (`DELETE .../scene/{SID}`) | `ok`, xoá khỏi `SceneStore` | scene biến mất khỏi list |
| Xoá tất cả | Tab 8 · `P 9.7.7.X3` (`DELETE .../scene`) | `ok`, dọn `SceneStore` | tất cả scene bị xoá |
| Scene đang chạy | Tab 8 · `P 9.7.7.6` | ✅ state | ✅ |
| Tham số chuyển cảnh | Tab 8 · `P 9.7.7.8` / `P 9.7.7.9` | ✅ | ✅ |
| **API ẩn** `sceneInfo` | Tab 8 · `P 9.7.7.X4` (`GET .../scene/{SID}/sceneInfo?format=json`) | Trả JSON `{"id":...,"name":"..."}` | đọc thông tin scene |
| **API ẩn** `export` | Tab 8 · `P 9.7.7.X5` (`GET .../scene/export?format=json`) | Trả JSON `SceneExport` | backup toàn bộ scene |
| **API ẩn** `import` | Tab 8 · `P 9.7.7.X6` (`POST .../scene/import?format=json`) | Thêm scene giả lập vào store, trả `<ID>` | khôi phục scene |
| **API ẩn** `copy` | Tab 8 · `P 9.7.7.X7` (`PUT .../scene/{SID}/copy`) | Tạo bản sao trong store, trả `ok` | nhân bản scene |

### KB‑16 — Poll trạng thái  ✅ chạy tốt trên Mock

| # | Tab / preset | Tần suất | Mock | HW thật |
|---|---|---|---|---|
| 16.1 | Tab 3 · `P 9.7.2.8` (`GET .../windows/status`) | 3–5s | `<WallWindowStatusList>` | `<WallWindowStatusList>` |
| 16.2 | Tab 4 · `P 9.7.3.4` (`GET .../Video/outputs/channels`) | 5–10s | 2 kênh, `outputPortAccessStatus` `normal` | 12 kênh, `normal`/`notConnected` |
| 16.3 | Tab 5 · `P 9.7.4.8` (`GET .../Video/inputs/channels`) | 5–10s | 2 kênh (`signal`/`noSignal`) | các nguồn thật |
| 16.4 | mục 0.5 → GET → Endpoint `ISAPI/DisplayDev/decoingDevice/status?format=json` (chú ý **thiếu chữ `d`** — đây là URL thật) | 30–60s | **JSON giàu**: `DevCaseStatus.row/col`, `BackplaneStatusList[].backplaneTemperature`, `SubBoardStatusList[].status/subBoardType/runTime` | health phần cứng thật; theo dõi `runTime` từng board là chỉ báo sớm reboot |
| 16.5 | Tab 2 · `P 9.7.1.4` (`GET /ISAPI/System/Board/status/capabilities`) | on‑demand | `BoardStatusCap` | capability sub‑board |

### KB‑17 — Tắt màn hình  (⛔ CHẠY CUỐI CÙNG trên HW thật — không có API bật lại)

- Tab 9 · `P 9.7.8.1` (`PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll`), Body theo mục đích:
  - Tắt 1 màn: `<ScreenCtrl …><OutputID>17235971</OutputID></ScreenCtrl>`
  - Tắt cả tường: `<ScreenCtrl …><VideoWallID>1</VideoWallID></ScreenCtrl>`
  - Tắt tất cả: `<ScreenCtrl xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0"></ScreenCtrl>`
- **Mock**: Mặc định trả `statusCode 4 / invalidOperation` (mô phỏng "chưa có dây RS-232/485"). Để mô phỏng thành công: khởi chạy runner với cờ `--closeall-ok`.
- **HW thật**: `ok` **chỉ khi** đã nối dây RS‑232/485 + cổng serial `workMode = screenCtrl` + đúng protocol màn. Nếu chưa → `statusCode 4 / invalidOperation`.
- **KB‑17 phụ**: Tab 2 → `SYS-4` (`GET /ISAPI/System/Serial/ports`), `SYS-5` (`GET .../ports/capabilities`) & `SYS-6` (`GET /ISAPI/System/Serial/capabilities`) → Mock & HW thật trả danh sách cổng và năng lực serial.

### KB‑18 — Crop nguồn

| # | Tab · preset |
|---|---|
| 18.1 | Tab 5 · `P 9.7.4.17` (`GET .../inputs/channels/{id}/cutOff/capabilities`) |
| 18.2 | Tab 5 · `P 9.7.4.15` (`GET .../inputs/channels/{id}/cutOff`) — giá trị hiện tại |
| 18.3 | Tab 5 · `P 9.7.4.16` (`PUT .../inputs/channels/{id}/cutOff`), Body `<InputCutOff …><leftCutOff>0</leftCutOff><rightCutOff>15</rightCutOff><topCutOff>0</topCutOff><bottomCutOff>0</bottomCutOff></InputCutOff>` |

- Miền `[0,30]` cả 4 chiều. Điều kiện `VideoInputsCap.isSupportCutOffSetting = true`.
- Ghép nhiều input: Tab 5 · `P 9.7.4.24` (`GET .../inputs/joinSignal`), `P 9.7.4.27` (caps), `P 9.7.4.25` (`PUT .../joinSignal/{id}`).
- **Mock**: `CutOffInfo` + OK cho PUT. **HW thật**: test thực nghiệm để suy đơn vị.

### KB‑19 — Plan (lịch tự động)

| # | Tab / thao tác | Mock | HW thật |
|---|---|---|---|
| 19.1 | Tab 2 · `SYS-3` (`GET /ISAPI/System/time`) | `<Time>` thời gian thực ✅ | `<Time>` — 🔴 đồng bộ giờ trước |
| 19.2 | Tab 7 · `P 9.7.6.3` (`GET .../plan/capabilities`) | `PlanCap`/`maxPlanNums` ✅ | ✅ |
| 19.3 | Tab 7 · `P 9.7.6.1` (`POST .../plan`), Body `WallPlan` từ KB‑19 | trả `<ID>` cấp mới ✅ | `<ID>` = `planTemplateID` mới |
| 19.4 | Tab 7 · `P 9.7.6.2` (`GET .../plan/{planTemplateID}/capabilities`) | `WallPlan` mẫu ✅ | ✅ |
| 19.5 | Tab 7 · `P 9.7.6.4` (`GET .../plan/isRunning`) | `RunningPlan/planID` ✅ | ✅ |
| 19.6 | Quản lý plan: Tab 7 `P 9.7.6.X1` (`GET .../plan`), `P 9.7.6.X2/X3` (`PUT/DELETE .../plan/{id}`), `P 9.7.6.X4/X5` (`start/stop`) | `PlanStore` stateful ✅ | ✅ |

- `operationType` hợp lệ: `activateScene · closeScreen · openScreen · switchBaseMap`. `openScreen` **chỉ tồn tại trong Plan** — cách duy nhất "bật màn" qua API.

### KB‑20 — Virtual LED / Wallpaper  ✅ đủ preset & route

**Virtual LED (Tab 10):**

| Hành động | Preset |
|---|---|
| Capability tất cả | `P 9.7.9.8` |
| Đọc tất cả | `P 9.7.9.2` |
| Thêm | `P 9.7.9.3` (POST) |
| Set tất cả | `P 9.7.9.1` (PUT) |
| Đọc 1 / Sửa 1 / Xoá 1 | `P 9.7.9.4` / `P 9.7.9.6` / `P 9.7.9.5` |
| Capability 1 | `P 9.7.9.7` |

**Wallpaper (Tab 11):** `P 9.7.10.7` (caps) · `P 9.7.10.8` (`GET .../baseMap?isGetBaseMapFile=false`) · `P 9.7.10.3` (PUT all) · `P 9.7.10.6` (đọc 1) · `P 9.7.10.5` (sửa 1) · `P 9.7.10.4` (xoá 1) · `P 9.7.10.1`/`P 9.7.10.2` (caps 1 / all).

- **Mock & HW thật**: đều trả đủ (Mock trả mẫu tĩnh). Giới hạn thật đọc từ `virtualLED/capabilities` và `baseMap/capabilities`.
- Nếu `isSupportSaveSceneVirLed / BaseMap = false` → phải PUT lại chữ chạy / ảnh nền sau mỗi lần activate scene.

---

## Phần 3 — Điểm chỉ kiểm chứng đầy đủ trên HW thật DS‑C30S‑S11

Hầu hết endpoint KB‑01…KB‑20 nay chạy được trên Mock (Mock đã bổ sung route `deviceInfo` / `time` /
`Serial/ports` / `SDK/activateStatus`, scene & plan CRUD có trạng thái, decode‑status đúng shape KB‑11).
Còn lại là những thứ Mock **không mô phỏng đúng bản chất vật lý / trạng thái** — bắt buộc xác nhận trên
controller thật:

| KB | Nội dung cần verify trên HW thật | Vì sao Mock không đủ |
|---|---|---|
| KB‑02 / KB‑03 | Topology **12 output / lưới 4×3** thực; `GET .../VideoWall/1/outputs` trả **đủ 12** `WallOutput` đúng vị trí ô | Mock chỉ giả lập **2 cổng** — không dựng lưới 12 màn |
| KB‑04 / KB‑05 / KB‑08 / KB‑09 / KB‑12 | Verify **có trạng thái** window: số lượng window tăng/giảm sau POST/DELETE · `layerIdx` đổi sau `/top` `/bottom` · `Rect` mới sau move/resize | Mock trả `GET .../windows` là danh sách **tĩnh**, không phản ánh thao tác ghi |
| KB‑05 #5 / KB‑11 | Giá trị decode **thật**: `videoFPS`, `streamRate`, `imageWidth/Height`, và **mã lỗi decode** (KB Phụ lục P5) khi nguồn hỏng / không tín hiệu | Mock luôn trả `isDecoding=true` với số cố định |
| KB‑13 / KB‑14 | Bố cục scene **thật** được `saveData` chụp lại & `activate` phát lên tường · độ trễ chuyển cảnh · lỗi `inSceneSwitchingPleaseDoNotOperate (0x4000A1AB)` | Mock chỉ lưu `{id, name}` trong bộ nhớ, không có bố cục window |
| KB‑17 | `closeAll` làm màn **thực sự tắt** | Cần dây RS‑232/485 + cổng serial `workMode=screenCtrl` + đúng protocol màn hình. Cờ `--closeall-ok` chỉ giả `statusCode 1`, **không** chứng minh đấu nối |
| Nhánh lỗi | `stale` nonce giữa chừng · khoá IP do sai mật khẩu liên tiếp · `saveData` lỗi · output `notConnected` · tường `unbound` · thiết bị mất kết nối | Trên Mock **bật được** bằng cờ runner (`--nonce-expiry`, `--lockout=N`, `--savedata-fail`, `--not-connected=17235971,...`, `--no-bound-wall`, `--unreachable`) để tập dượt xử lý; nhưng thông báo & hành vi thật chỉ có ở thiết bị thật |

> Cờ điều khiển Mock (đặt sau `dotnet run --project scripts/VwMockServerRunner`): `--closeall-ok`,
> `--nonce-expiry`, `--savedata-fail`, `--no-bound-wall`, `--multi-bound-wall`, `--unreachable`,
> `--verify-digest`, `--lockout=N`, `--max-scene=N`, `--not-connected=<id,id>`. Hoặc biến môi trường
> `VWMOCK_<TÊN_CỜ_HOA_GẠCH_DƯỚI>=1`.
