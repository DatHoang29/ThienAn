# Kịch bản test API Video Wall — DS-C30S-S11 / 12 màn

Tất cả URL đều tương đối, ghép với `{{base}} = http://<ip>:<port>`.
Auth: **Digest** (admin). Header ghi: `Content-Type: application/xml`.
Thành công = `statusCode` **0 hoặc 1**. Lỗi thì đọc `subStatusCode`.

**Giả định lưới:** 4 cột × 3 hàng, `baseOutputSize = 1920` → canvas ảo **7680 × 5760**.
Nếu xếp khác, xem [bảng toạ độ](#p2-bảng-toạ-độ-12-màn) ở phụ lục.

| Kịch bản | Mục đích |
|---|---|
| [KB-01](#kb-01-kết-nối--đọc-năng-lực) | Kết nối, đọc năng lực, lấy `videoWallID` |
| [KB-02](#kb-02-lấy-id-output--input) | Lấy 12 outputID + các inputID |
| [KB-03](#kb-03-gán-12-output-vào-lưới-4x3) | Setup lưới tường |
| [KB-04](#kb-04-đọc-bố-cục--trạng-thái-hiện-tại) | Đọc bố cục hiện tại |
| [KB-05](#kb-05-mở-window-phủ-1-màn--gán-nguồn) | Mở window 1 màn + nguồn |
| [KB-06](#kb-06-mở-window-phủ-nhiều-màn) | Window phủ nhiều màn |
| [KB-07](#kb-07-đổi-nguồn-của-window) | Đổi nguồn |
| [KB-08](#kb-08-di-chuyển--resize-window) | Move / resize |
| [KB-09](#kb-09-z-order-topbottom) | Z-order |
| [KB-10](#kb-10-chia-window-thành-4-ô) | Chia 4/9/16 ô |
| [KB-11](#kb-11-startstop-decoding) | Start / Stop decoding |
| [KB-12](#kb-12-xóa-window) | Xóa window |
| [KB-13](#kb-13-tạo-scene--lưu-bố-cục) | Tạo scene + lưu bố cục |
| [KB-14](#kb-14-active-scene-lên-tường) | **Active scene lên tường** |
| [KB-15](#kb-15-quản-lý-scene) | Liệt kê / đổi tên / xóa scene |
| [KB-16](#kb-16-poll-trạng-thái) | Poll trạng thái |
| [KB-17](#kb-17-tắt-màn-hình) | Tắt màn |
| [KB-18](#kb-18-crop-nguồn) | Crop nguồn |
| [KB-19](#kb-19-plan-lịch-tự-động) | Plan lịch tự động |
| [KB-20](#kb-20-virtual-led--wallpaper) | Virtual LED / Wallpaper |

---

## KB-01. Kết nối & đọc năng lực

| # | Method | URL | Body | Lấy gì từ response |
|---|---|---|---|---|
| 1 | `GET` | `/SDK/activateStatus` | — | *(không cần auth)* `activated` |
| 2 | `GET` | `/ISAPI/Security/userCheck` | — | `statusValue=200`, `isActivated` |
| 3 | `GET` | `/ISAPI/System/deviceInfo` | — | `model`, `serialNumber`, `firmwareVersion` |
| 4 | `GET` | `/ISAPI/DisplayDev/capabilities` | — | Toàn bộ cờ tính năng (bảng dưới) |
| 5 | `GET` | `/ISAPI/DisplayDev/VideoWall/capabilities` | — | `maxWindowNums`, **`baseOutputSize`**, `isSupportScene`, `isSupportRoam` |
| 6 | `GET` | `/ISAPI/DisplayDev/VideoWall` | — | ⭐ **`videoWallID`** — đừng đoán là 1 |

Bước 4 — cờ cần ghi lại:

```
VideoWallCap.baseOutputSize          ← quan trọng nhất, dùng cho mọi phép toạ độ
VideoWallCap.maxWindowNums
VideoWallCap.isSupportScene          ← false thì bỏ KB-13/14/15
VideoWallCap.isSupportRoam           ← false thì bỏ KB-06
VideoWallCap.isSupportPlan           ← false thì bỏ KB-19
VideoWallCap.isSupportVirtualLED
VideoWallCap.isSupportBaseMap
VideoWallCap.SceneCap.maxSceneNums
VideoWallCap.SceneCap.isSupportSaveSceneVirLed    ← false: scene KHÔNG lưu chữ chạy
VideoWallCap.SceneCap.isSupportSaveSceneBaseMap   ← false: scene KHÔNG lưu ảnh nền
VideoInputsCap.isSupportCutOffSetting             ← cho KB-18
```

Bước 6 — response:

```xml
<VideoWallList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <VideoWall>
    <id>1</id>
    <name>VideoWall1</name>
    <wallBindOutputStatus>unbound</wallBindOutputStatus>
  </VideoWall>
</VideoWallList>
```

- `unbound` = tường chưa gán màn nào → **sandbox test an toàn**, nhưng nhiều lệnh sẽ trả `invalidOperation`.
- `bound` = tường đang có màn hoạt động.

> Nếu bước 2 trả 401: xem header `WWW-Authenticate`. `stale="FALSE"` = **sai mật khẩu, dừng ngay** (thiết bị khóa IP). `stale="TRUE"` = mật khẩu đúng, chỉ nonce cũ, thử lại được.

---

## KB-02. Lấy ID output & input

| # | Method | URL | Body | Lấy gì |
|---|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | — | 12 `id` output + `outputPortAccessStatus` |
| 2 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels/<channelID>/capabilities` | — | (×12) `OutputResolutionCapList` |
| 3 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | — | các `id` input + `signalStatus` |
| 4 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels/<channelID>/picture` | — | (tùy chọn) snapshot JPEG của nguồn |

Bước 1 — response mỗi phần tử:

```xml
<VideoOutputChannel>
  <id>17235969</id>
  <portType>HDMI</portType>
  <name>Output 7-1</name>
  <timeSequenceMode>standard</timeSequenceMode>
  <OutputResolution><resolution>1920*1080@60HZ</resolution></OutputResolution>
  <PortInBoard><boardID>7</boardID><portID>1</portID></PortInBoard>
  <outputPortAccessStatus>normal</outputPortAccessStatus>
</VideoOutputChannel>
```

> 🔴 `id` **không phải 1..12**. Gọi `/channels/3` sẽ trả `invalidOperation`. Công thức: `id = T×16777216 + boardID×65536 + portID`, `T=0x01` cho video channel.
>
> `outputPortAccessStatus`: `normal` = có màn, `notConnected` = chưa cắm.
>
> ⚠️ Kiểm `timeSequenceMode` của cả 12 output có **giống nhau** không. Lệch (board này `standard`, board kia `custom`) sẽ gây lệch timing khi window vắt qua 2 board.

Bước 3 — response:

```xml
<VideoInputChannel>
  <id>16842753</id>
  <portType>HDMI</portType>
  <name>Input 1-1</name>
  <signalStatus>signal</signalStatus>        <!-- signal | noSignal | abnormal -->
  <decodeWallStatus>noDecoding</decodeWallStatus>
</VideoInputChannel>
```

Đổi tên nguồn cho dễ test:

```http
PUT {{base}}/ISAPI/DisplayDev/Video/inputs/channels/16842753
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<VideoInputChannel xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>16842753</id>
  <name>Dashboard GT</name>
</VideoInputChannel>
```

---

## KB-03. Gán 12 output vào lưới 4x3

| # | Method | URL | Body |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | — |
| 2 | `PUT` | `/ISAPI/DisplayDev/Video/outputs/channels/<channelID>` | (×12, nếu cần đồng nhất resolution) |
| 3 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1` | ⭐ `WallOutputList` |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/outputs` | — verify: phải có đủ 12 `WallOutput` |

Bước 3:

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/1
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<VideoWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>1</id>
  <name>Tuong Trung Tam</name>
  <WallOutputList>
    <WallOutput><outputID>17235969</outputID>
      <Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17235970</outputID>
      <Rect><Coordinate><x>1920</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17235971</outputID>
      <Rect><Coordinate><x>3840</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17235972</outputID>
      <Rect><Coordinate><x>5760</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>

    <WallOutput><outputID>17301505</outputID>
      <Rect><Coordinate><x>0</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17301506</outputID>
      <Rect><Coordinate><x>1920</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17301507</outputID>
      <Rect><Coordinate><x>3840</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17301508</outputID>
      <Rect><Coordinate><x>5760</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>

    <WallOutput><outputID>17367041</outputID>
      <Rect><Coordinate><x>0</x><y>3840</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17367042</outputID>
      <Rect><Coordinate><x>1920</x><y>3840</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17367043</outputID>
      <Rect><Coordinate><x>3840</x><y>3840</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>17367044</outputID>
      <Rect><Coordinate><x>5760</x><y>3840</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
  </WallOutputList>
</VideoWall>
```

> Thay 12 `outputID` bằng ID thật lấy ở KB-02.
> Chỉ `outputID` là `req`, còn lại `opt` → **gửi tối thiểu, đừng PUT lại nguyên response GET** (sẽ ra `badParameters` vì có element rỗng và field chỉ-đọc).
>
> Chương 8 ghi có `POST /ISAPI/DisplayDev/VideoWall/1/outputs` để gán output, nhưng **tài liệu không đặc tả payload**. Dùng cách PUT trên.

---

## KB-04. Đọc bố cục & trạng thái hiện tại

| # | Method | URL | Lấy gì |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/1` | Cấu hình đầy đủ tường |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/outputs` | ⭐ Output nào ở ô nào |
| 3 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows` | ⭐ Window đang mở + nguồn |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/status` | Trạng thái decode |

Bước 2 — response:

```xml
<WallOutputList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <WallOutput>
    <id>2</id>                                    <!-- ID bản ghi, KHÔNG phải số ô -->
    <outputID>17235971</outputID>
    <Rect><Coordinate><x>0</x><y>0</y></Coordinate>
          <width>1920</width><height>1920</height></Rect>
    <outputWinNum>1</outputWinNum>
    <coordinateMode>uniformCoordinate</coordinateMode>
  </WallOutput>
</WallOutputList>
```

> Suy vị trí ô: `gridCol = Rect.x / 1920`, `gridRow = Rect.y / 1920`.
> 🔴 **Đừng suy vị trí từ `WallOutput.id`** — nó bắt đầu từ 2 và không theo thứ tự ô.
> 🔴 `height = 1920` chứ không phải 1080. Ô lưới **vuông**.

Bước 3 — response:

```xml
<WallWindowList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <WallWindow>
    <id>33554433</id>                             <!-- ⭐ VWMWID -->
    <wndOperateMode>uniformCoordinate</wndOperateMode>
    <Rect><Coordinate><x>0</x><y>0</y></Coordinate>
          <width>1920</width><height>1920</height></Rect>
    <layerIdx>67108865</layerIdx>                 <!-- z-order, CHỈ ĐỌC, số lớn nằm TRÊN -->
    <windowMode>1</windowMode>
    <wndShowMode>subWndMode</wndShowMode>
    <SubWindowList>
      <SubWindow><id>1</id><SubWindowParam>
        <signalMode>video input</signalMode>
        <videoInputChannelID>16842753</videoInputChannelID>
      </SubWindowParam></SubWindow>
    </SubWindowList>
  </WallWindow>
</WallWindowList>
```

---

## KB-05. Mở window phủ 1 màn + gán nguồn

Mục tiêu: đưa nguồn `16842754` lên ô SCR-06 (col=1, row=1).

| # | Method | URL | Body |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/capabilities` | — |
| 2 | `POST` | `/ISAPI/DisplayDev/VideoWall/1/windows` | ⭐ XML dưới |
| 3 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows` | — lấy `VWMWID` mới |
| 4 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/<VWMWID>/sub/1/start` | *(none)* — nếu chưa tự decode |
| 5 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/<VWMWID>/sub/1/status` | — verify `isDecoding=true` |

Bước 2:

```http
POST {{base}}/ISAPI/DisplayDev/VideoWall/1/windows
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <wndOperateMode>uniformCoordinate</wndOperateMode>
  <Rect>
    <Coordinate><x>1920</x><y>1920</y></Coordinate>
    <width>1920</width>
    <height>1920</height>
  </Rect>
  <windowMode>1</windowMode>
  <wndShowMode>subWndMode</wndShowMode>
  <SubWindowList>
    <SubWindow>
      <id>1</id>
      <SubWindowParam>
        <signalMode>video input</signalMode>
        <videoInputChannelID>16842754</videoInputChannelID>
      </SubWindowParam>
    </SubWindow>
  </SubWindowList>
</WallWindow>
```

Toạ độ: `x = gridCol × 1920`, `y = gridRow × 1920`.

> ⚠️ `signalMode` giá trị là `video input` — **có dấu cách**.
> ⚠️ `wndOperateMode` bắt buộc, bỏ trống → `badParameters`.

Lỗi hay gặp:

| `subStatusCode` | Nghĩa |
|---|---|
| `windowsAmountExceedLimitInSingleOutput` | Vượt số window tối đa trên 1 màn |
| `multipleVideowallClientConflict` (`0x4000A4F8`) | Client khác (iVMS / web UI thiết bị) đang điều khiển tường → **đóng web UI thiết bị rồi test lại** |
| `invalidOperation` | `videoInputChannelID` sai, hoặc tường `unbound` |
| `badParameters` | Thiếu `wndOperateMode`, hoặc `Rect` sai |

---

## KB-06. Mở window phủ nhiều màn

Giống KB-05 hoàn toàn, **chỉ đổi `Rect`**. Cần `isSupportRoam = true`.

Khối 2×2 góc trên-trái (SCR-01,02,05,06):

```xml
<Rect><Coordinate><x>0</x><y>0</y></Coordinate>
      <width>3840</width><height>3840</height></Rect>
```

Phủ toàn bộ 12 màn:

```xml
<Rect><Coordinate><x>0</x><y>0</y></Coordinate>
      <width>7680</width><height>5760</height></Rect>
```

> 🔴 Tài liệu ghi `Rect range:[0,1920]` — **SAI**. Đã test `height=3840` trả `OK` và hiển thị đúng. Với 12 màn, hợp lệ tới `7680 × 5760`.
>
> Xem [bảng toạ độ đầy đủ](#p3-rect-cho-các-bố-cục-thường-dùng) ở phụ lục.

---

## KB-07. Đổi nguồn của window

Hai cách, thử cách 1 trước:

**Cách 1** — PUT sub-window:

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<SubWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>1</id>
  <SubWindowParam>
    <signalMode>video input</signalMode>
    <videoInputChannelID>16842756</videoInputChannelID>
  </SubWindowParam>
</SubWindow>
```

> URL này chỉ có ở chương 8 bước 8, không có mục đặc tả riêng → **tên node gốc chưa chắc**. Nếu `badXmlFormat` thì dùng cách 2.

**Cách 2** — PUT cả window (có đặc tả, chắc chắn chạy):

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/1/windows/33554433
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>33554433</id>
  <wndOperateMode>uniformCoordinate</wndOperateMode>
  <SubWindowList>
    <SubWindow><id>1</id><SubWindowParam>
      <signalMode>video input</signalMode>
      <videoInputChannelID>16842756</videoInputChannelID>
    </SubWindowParam></SubWindow>
  </SubWindowList>
</WallWindow>
```

Sau đó verify: `GET /ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1/status`

---

## KB-08. Di chuyển / resize window

| # | Method | URL | Body |
|---|---|---|---|
| 1 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433` | XML dưới |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433` | — verify |

```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>33554433</id>
  <wndOperateMode>uniformCoordinate</wndOperateMode>
  <Rect>
    <Coordinate><x>0</x><y>0</y></Coordinate>
    <width>7680</width>
    <height>5760</height>
  </Rect>
</WallWindow>
```

> ⚠️ **Bẫy hay gặp:** PUT trả `OK` nhưng tường không đổi. Nguyên nhân: window khác có `layerIdx` cao hơn **đang đè lên**. Xử lý: `PUT .../windows/33554433/top` (KB-09) hoặc `DELETE` window đang đè.
>
> Debug: `GET /windows`, sắp theo `layerIdx` giảm dần — cái đầu là cái đang hiện.

Muốn dùng pixel thật thay toạ độ chuẩn hoá:

```xml
<wndOperateMode>resolutionCoordinate</wndOperateMode>
<ResolutionRect>
  <Coordinate><x>0</x><y>0</y></Coordinate>
  <width>7680</width><height>3240</height>
</ResolutionRect>
```

---

## KB-09. Z-order (top/bottom)

| # | Method | URL | Body |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/capabilities` | — kiểm `isSupportWinTopBottom` |
| 2 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433/top` | *(none)* |
| 3 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433/bottom` | *(none)* |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows` | — verify `layerIdx` đổi |

> 🔴 `layerIdx` **chỉ đọc**, không set số tùy ý. Chỉ có `/top` và `/bottom`, **không có "lên 1 lớp"**.
> Muốn dựng lại cả stack: gọi `/top` lần lượt từ dưới lên trên.

---

## KB-10. Chia window thành 4 ô

| # | Method | URL | Body |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1/capabilities` | — |
| 2 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433` | XML dưới |
| 3 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/<n>/start` | ×4 |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/<n>/status` | ×4 |

```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>33554433</id>
  <wndOperateMode>uniformCoordinate</wndOperateMode>
  <windowMode>4</windowMode>
  <wndShowMode>subWndMode</wndShowMode>
  <SubWindowList>
    <SubWindow><id>1</id><SubWindowParam>
      <signalMode>video input</signalMode><videoInputChannelID>16842753</videoInputChannelID></SubWindowParam></SubWindow>
    <SubWindow><id>2</id><SubWindowParam>
      <signalMode>video input</signalMode><videoInputChannelID>16842754</videoInputChannelID></SubWindowParam></SubWindow>
    <SubWindow><id>3</id><SubWindowParam>
      <signalMode>video input</signalMode><videoInputChannelID>16842755</videoInputChannelID></SubWindowParam></SubWindow>
    <SubWindow><id>4</id><SubWindowParam>
      <signalMode>video input</signalMode><videoInputChannelID>16842756</videoInputChannelID></SubWindowParam></SubWindow>
  </SubWindowList>
</WallWindow>
```

`windowMode` hợp lệ: `1`, `4`, `9`, `16`. Thứ tự `SubWindow.id` với mode 4:

```
┌───┬───┐
│ 1 │ 2 │
├───┼───┤
│ 3 │ 4 │
└───┴───┘
```

Phóng to 1 ô lên toàn window:

```xml
<wndShowMode>fullScreenMode</wndShowMode>
<amplifyingSubWndNo>3</amplifyingSubWndNo>
```

> 🔴 Firmware **không trả về** `amplifyingSubWndNo` khi GET. Ghi được nhưng đọc lại không thấy.

---

## KB-11. Start/Stop decoding

| Hành động | Method | URL | Body |
|---|---|---|---|
| Start | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1/start` | **none** |
| Stop | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1/stop` | **none** |
| Status 1 window | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433/sub/1/status` | — |
| Status tất cả | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/status` | — |

> Tài liệu ghi `Request Message: None`. Nếu trả `badXmlFormat` thì gửi placeholder:
> ```xml
> <?xml version="1.0" encoding="UTF-8"?>
> <Request xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0"></Request>
> ```
>
> `stop` = **window còn, ngừng hình**. Khác `DELETE window` = xóa cả bố cục.

Response `status`:

```xml
<WallWindowStatusList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <WallWindowStatus>
    <id>33554433</id>
    <windowMode>1</windowMode>
    <SubWinStatusList>
      <SubWinStatus>
        <id>1</id>
        <isLinked>true</isLinked>
        <isDecoding>true</isDecoding>
        <isDecodingEnabled>true</isDecodingEnabled>
        <imageWidth>1920</imageWidth>
        <imageHeight>1080</imageHeight>
        <videoFPS>25</videoFPS>
        <streamRate>4096</streamRate>
        <wndDecodeType>dynamic</wndDecodeType>
        <SubWindowParam>
          <signalMode>video input</signalMode>
          <videoInputChannelID>16842753</videoInputChannelID>
        </SubWindowParam>
      </SubWinStatus>
    </SubWinStatusList>
  </WallWindowStatus>
</WallWindowStatusList>
```

Mã lỗi decode (dùng để chẩn đoán màn đen) — xem [phụ lục P5](#p5-mã-lỗi-decoding).

---

## KB-12. Xóa window

| Hành động | Method | URL |
|---|---|---|
| Xóa 1 window | `DELETE` | `/ISAPI/DisplayDev/VideoWall/1/windows/33554433` |
| **Xóa tất cả** | `DELETE` | `/ISAPI/DisplayDev/VideoWall/1/windows` |

Không body. `DELETE .../windows` xóa **toàn bộ** window của tường, không hoàn tác.

> Đây cũng là cách "làm sạch tường" trước khi test lại từ đầu.

---

## KB-13. Tạo scene & lưu bố cục

| # | Method | URL | Body | Ghi chú |
|---|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/scene/capabilities` | — | `maxSceneNums`, độ dài tên |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/scene` | — | Đếm scene đã có |
| 3 | `POST` | `/ISAPI/DisplayDev/VideoWall/1/scene` | XML dưới | Trả `SID` mới |
| 4 | — | *(chạy KB-05 … KB-10)* | — | 🔴 **Dựng bố cục THẬT lên tường** |
| 5 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/scene/3` | XML dưới | Đặt tên |
| 6 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/scene/3/saveData` | placeholder | ⭐ **Chụp bố cục vào scene** |

Bước 3 — tạo scene:

```http
POST {{base}}/ISAPI/DisplayDev/VideoWall/1/scene
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallScene xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <name>Ca dem</name>
</WallScene>
```

Response — `SID` mới nằm trong `ResponseStatus`. Nếu firmware không trả, gọi `GET /scene` trước/sau POST rồi lấy phần chênh.

> 🔶 URL này ở chương 8 bước 3, chương 9 không có mục đặc tả riêng.

Bước 5 — đặt tên:

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/1/scene/3
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallScene xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>3</id>
  <name>Ca dem</name>
</WallScene>
```

Bước 6 — ⭐ `saveData`:

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/1/scene/3/saveData
Content-Type: application/xml
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Request xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0"></Request>
```

> Body là placeholder rỗng, nhưng **phải có** cấu trúc XML này — body trống hoàn toàn ra `badXmlFormat`.

### 🔴 Ba điều phải biết về Scene

1. **`saveData` chụp tường ĐANG CHẠY.** Muốn lưu scene → phải đưa bố cục lên tường thật trước. Không có cách nào "soạn nháp" trên thiết bị (`isSupportSceneCopy = false`).
2. **`GET /scene` chỉ trả `id` + `name`, KHÔNG có bố cục.** Thiết bị là hộp đen, không đọc được scene chứa gì.
3. **`PUT /scene/<SID>` chỉ sửa được `id`/`name`**, không sửa được bố cục.

Nếu `isSupportSaveSceneVirLed = false` → scene **không lưu chữ chạy**.
Nếu `isSupportSaveSceneBaseMap = false` → scene **không lưu ảnh nền**.
Hai thứ đó phải áp lại thủ công sau khi activate.

---

## KB-14. ACTIVE scene lên tường

### Đáp án ngắn — 1 API duy nhất

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/<videoWallID>/scene/<SID>/activate
```

- **Body:** không có
- **Query param:** không có
- **Tham số:** `videoWallID` + `SID`, cả hai nằm trong path
- **Thành công:** `statusCode` = 0 hoặc 1

### Chuỗi đầy đủ để test đúng

| # | Method | URL | Body | Mục đích |
|---|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/scene/isRunning` | — | Scene đang chạy. Trùng `SID` → bỏ qua |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/scene` | — | Xác nhận `SID` còn tồn tại |
| 3 | **`PUT`** | **`/ISAPI/DisplayDev/VideoWall/1/scene/3/activate`** | **none** | ⭐ **KÍCH HOẠT** |
| 4 | — | *chờ 2–3 giây* | — | 🔴 Bắt buộc, chuyển cảnh không tức thời |
| 5 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/scene/isRunning` | — | Verify `sceneID` = 3 |
| 6 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows` | — | Đọc bố cục mới |
| 7 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/status` | — | Kiểm decode |
| 8a | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/virtualLED` | XML | Chỉ khi `isSupportSaveSceneVirLed = false` |
| 8b | `PUT` | `/ISAPI/DisplayDev/VideoWall/baseMap` | XML | Chỉ khi `isSupportSaveSceneBaseMap = false` |

Bước 3 — request đầy đủ:

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/1/scene/3/activate HTTP/1.1
Authorization: Digest username="admin", ...
```

*Không body, không Content-Type.*

Response:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ResponseStatus xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <requestURL></requestURL>
  <statusCode>1</statusCode>
  <statusString>OK</statusString>
  <subStatusCode>ok</subStatusCode>
</ResponseStatus>
```

Bước 1/5 — response `isRunning`:

```xml
<RunningScene version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <sceneID>3</sceneID>
</RunningScene>
```

### Lỗi đặc trưng của activate

| `subStatusCode` | `errorCode` | Nghĩa | Xử lý |
|---|---|---|---|
| **`inSceneSwitchingPleaseDoNotOperate`** | **`0x4000A1AB`** | Đang chuyển cảnh | 🔴 **Không gọi lệnh nào khác.** Chờ 2–3s rồi verify bằng `isRunning`, **không retry activate** |
| `invalidOperation` | — | `SID` không tồn tại, hoặc tường `unbound` | `GET /scene` đối chiếu |
| `multipleVideowallClientConflict` | `0x4000A4F8` | Client khác đang điều khiển | Đóng web UI thiết bị / iVMS |
| `notSupport` | — | `isSupportScene = false` | Bỏ nhóm scene |

### Nếu `sceneSwitchDelay` đang bật, activate sẽ rất chậm

Kiểm bằng `GET /ISAPI/DisplayDev/VideoWall/1`, xem `sceneSwitchDelayEnabled` và `sceneSwitchDelay` (1–604800 giây). Tắt để test nhanh:

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/1
```
```xml
<?xml version="1.0" encoding="UTF-8"?>
<VideoWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>1</id>
  <sceneSwitchDelayEnabled>false</sceneSwitchDelayEnabled>
</VideoWall>
```

### Cách 2 — "activate" bằng cách dựng lại window (không dùng scene thiết bị)

| # | Method | URL |
|---|---|---|
| 1 | `DELETE` | `/ISAPI/DisplayDev/VideoWall/1/windows` |
| 2 | `POST` | `/ISAPI/DisplayDev/VideoWall/1/windows` — ×N |
| 3 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/<VWMWID>/sub/1/start` — ×N |
| 4 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/windows/<VWMWID>/top` — ×N theo thứ tự z-order |

Chậm hơn (N+1 call vs 1 call) và tường "nhảy" từng window, nhưng không phụ thuộc scene thiết bị.

---

## KB-15. Quản lý scene

| Hành động | Method | URL | Body |
|---|---|---|---|
| Liệt kê | `GET` | `/ISAPI/DisplayDev/VideoWall/1/scene` | — |
| Đổi tên | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/scene/3` | `<WallScene><id/><name/></WallScene>` |
| Xóa 1 scene | `DELETE` | `/ISAPI/DisplayDev/VideoWall/1/scene/3` | — 🔶 |
| Xóa tất cả | `DELETE` | `/ISAPI/DisplayDev/VideoWall/1/scene` | — 🔶 |
| Scene đang chạy | `GET` | `/ISAPI/DisplayDev/VideoWall/1/scene/isRunning` | — |
| Capability | `GET` | `/ISAPI/DisplayDev/VideoWall/1/scene/capabilities` | — |
| Tham số chuyển cảnh | `GET`/`PUT` | `/ISAPI/DisplayDev/VideoWallScene/SceneControlParams?format=json` | JSON |

### API ẩn — đáng test, URL nằm trong `desc` của `SceneCap`

| Cờ capability | URL | Test để làm gì |
|---|---|---|
| `isSupportSceneInfo` | `GET /ISAPI/DisplayDev/VideoWall/1/scene/3/sceneInfo?format=json` | ⭐ **Thử ngay** — nếu trả về bố cục scene thì giải quyết được vấn đề "không đọc được scene chứa gì" |
| `isSupportSceneExport` | `GET /ISAPI/DisplayDev/VideoWall/1/scene/export?format=json` | Backup toàn bộ scene |
| `isSupportSceneImport` | `POST /ISAPI/DisplayDev/VideoWall/1/scene/import?format=json` | Restore / nhân bản sang controller khác |
| `isSupportSceneCopy` | `PUT /ISAPI/DisplayDev/VideoWall/1/scene/3/copy` | Nhân bản scene (C66S = false) |

---

## KB-16. Poll trạng thái

| # | Method | URL | Tần suất | Ghi chú |
|---|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/windows/status` | 3–5s | ⭐ Trạng thái decode tất cả window |
| 2 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | 5–10s | ⭐ Trạng thái cắm cáp 12 màn (nhẹ) |
| 3 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | 5–10s | `signalStatus` các nguồn |
| 4 | `GET` | `/ISAPI/DisplayDev/decoingDevice/status?format=json` | 30–60s | ⚠️ **Nặng.** Health phần cứng |
| 5 | `GET` | `/ISAPI/System/Board/status/capabilities` | on-demand | Capability trạng thái sub-board |

> 🔴 URL bước 4 viết là **`decoingDevice`** — **thiếu chữ `d`**. Đây là lỗi chính tả của hãng nhưng **là URL thật**. Gõ đúng `decodingDevice` sẽ ra 404.
>
> ⚠️ Bước 4 là API JSON đầu tiên → nếu tự implement Digest, phải dùng **full path kèm query** (`/ISAPI/...?format=json`) khi tính hash. Bỏ mất `?format=json` sẽ bị 401.

Bước 4 — các field đáng đọc:

```
DevCaseStatus.row / col                              ← lưới slot khung máy
BackplaneStatusList[].backplaneTemperature           ← °C, đặt ngưỡng cảnh báo ~65–70
SubBoardStatusList[].status                          ← normal | alarm | notPower | notInsert
SubBoardStatusList[].subBoardType                    ← input | output
SubBoardStatusList[].runTime                         ← giây. 💡 GIẢM ĐỘT NGỘT = board reboot
SubBoardStatusList[].exceptionList                   ← [] = tốt
SubBoardStatusList[].CPUUtilization / memoryUtilization
SubBoardStatusList[].SubBoardInterfaceList[].outputPortLinkStatus  ← connected | notconnect
MainBoardStatusList[].serialPortList[].status        ← điều kiện cho KB-17
```

> 💡 Theo dõi `runTime` từng board là **chỉ báo sớm tốt nhất** — nó giảm trước khi `exceptionList` có gì.
> 🔴 API này **không cho biết sức khỏe màn hình**, chỉ biết cáp có cắm hay không. Nhiệt độ màn chỉ đọc được qua serial transparent transmission.

---

## KB-17. Tắt màn hình

```http
PUT {{base}}/ISAPI/DisplayDev/ScreenCtrl/closeAll
Content-Type: application/xml
```

Tắt 1 màn:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<ScreenCtrl xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <OutputID>17235971</OutputID>
</ScreenCtrl>
```

Tắt cả tường:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<ScreenCtrl xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <VideoWallID>1</VideoWallID>
</ScreenCtrl>
```

Tắt tất cả — không gửi field nào:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<ScreenCtrl xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0"></ScreenCtrl>
```

> 🔴 **Cả `VideoWallID` và `OutputID` đều là `opt`** → API tên `closeAll` nhưng tắt được từng màn.
>
> ⛔ **Không có API bật lại.** Sau khi tắt phải bật bằng tay/remote.
> ⛔ **Không có API đọc trạng thái bật/tắt.** `ScreenCtrl` một chiều.

### Điều kiện để `closeAll` hoạt động

Lệnh này **gửi lệnh tắt nguồn qua RS-232/485** tới màn hình, không phải ngắt tín hiệu. Cần:

1. Dây RS-232/485 nối controller ↔ màn hình
2. Cổng serial đặt `workMode = screenCtrl`
3. Chọn đúng protocol của hãng màn hình

Nếu chưa đủ → trả `statusCode 4 / invalidOperation`. Kiểm bằng KB-16 bước 4: `MainBoardStatusList[].serialPortList[].status` = `unknown` nghĩa là chưa cấu hình.

Kiểm cổng serial:
```
GET {{base}}/ISAPI/System/Serial/capabilities
GET {{base}}/ISAPI/System/Serial/ports
```

### Thay thế khi chưa có dây serial

| Cách | Hiệu ứng |
|---|---|
| `DELETE /ISAPI/DisplayDev/VideoWall/1/windows/<VWMWID>` | Màn sáng, mất nội dung → hiện nền |
| `PUT /ISAPI/DisplayDev/VideoWall/1` với `wndStaticMode=blackScreen` / `wallBackMode=color` + `backgroundRGBColor` | Màn sáng, hiển thị đen |

> 🔴 Trường `outputPortEnabled` **không phải công tắc bật/tắt cổng HDMI**. Đã kiểm chứng: màn đang hoạt động vẫn báo `false`, set `true` không có tác dụng.

---

## KB-18. Crop nguồn

Dùng khi cần ghép 1 khung hình lớn qua nhiều nguồn — mỗi nguồn crop 1 phần.

| # | Method | URL | Body |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels/16842753/cutOff/capabilities` | — |
| 2 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels/16842753/cutOff` | — đọc giá trị hiện tại |
| 3 | `PUT` | `/ISAPI/DisplayDev/Video/inputs/channels/16842753/cutOff` | XML dưới |

```xml
<?xml version="1.0" encoding="UTF-8"?>
<InputCutOff xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <leftCutOff>0</leftCutOff>
  <rightCutOff>15</rightCutOff>
  <topCutOff>0</topCutOff>
  <bottomCutOff>0</bottomCutOff>
</InputCutOff>
```

> Miền giá trị: **`[0,30]`** cho cả 4 chiều. Đơn vị không được tài liệu nêu rõ (khả năng là % hoặc đơn vị nội bộ) → **test thực nghiệm để suy đơn vị**.
> Điều kiện: `VideoInputsCap.isSupportCutOffSetting = true`.

Ghép nhiều input thành 1 nguồn lớn (thay cho crop):

```
GET  /ISAPI/DisplayDev/Video/inputs/joinSignal
GET  /ISAPI/DisplayDev/Video/inputs/joinSignal/capabilities
PUT  /ISAPI/DisplayDev/Video/inputs/joinSignal/<channelID>
```

---

## KB-19. Plan (lịch tự động)

| # | Method | URL | Body |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/System/time` | — 🔴 **Đồng bộ giờ trước**, lệch giờ → plan chạy sai |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/plan/capabilities` | — `maxPlanNums` |
| 3 | `POST` | `/ISAPI/DisplayDev/VideoWall/1/plan` | XML dưới |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/plan/<planTemplateID>/capabilities` | — |
| 5 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/plan/isRunning` | — `planID` |

Ví dụ: 06:00 thứ 2 chuyển sang scene 1, sau 12h chuyển scene 2, sau 6h tắt màn.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallPlan xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>1</id>                                        <!-- req, range [1,8] -->
  <name>Lich ngay thuong</name>                     <!-- range [1,32] -->
  <ActTimeDetail>
    <actTimeMode>weekly</actTimeMode>               <!-- at once | on day | weekly -->
    <WeeklyTime>
      <TimeBlockList>
        <TimeBlock>
          <dayOfWeek>1</dayOfWeek>                  <!-- 1=Monday -->
          <beginTime>06:00:00+07:00</beginTime>
        </TimeBlock>
      </TimeBlockList>
    </WeeklyTime>
  </ActTimeDetail>
  <PlanDetailList>
    <PlanDetail>
      <operationType>activateScene</operationType>
      <sceneID>1</sceneID>
      <duration>43200</duration>                    <!-- req, giây -->
    </PlanDetail>
    <PlanDetail>
      <operationType>activateScene</operationType>
      <sceneID>2</sceneID>
      <duration>21600</duration>
    </PlanDetail>
    <PlanDetail>
      <operationType>closeScreen</operationType>
      <duration>1</duration>
    </PlanDetail>
  </PlanDetailList>
  <actCount>1</actCount>
</WallPlan>
```

`operationType` hợp lệ: `activateScene` · `closeScreen` · `openScreen` · `switchBaseMap`

> ⭐ **`openScreen` chỉ tồn tại trong Plan** — đây là cách duy nhất "bật màn" qua API, vì `ScreenCtrl` chỉ có `closeAll`.
>
> Lỗi: `thePlanNameAlreadyExists` (`0x4000A30D`), `numberOfScenesInThisPlanReachedLimit` (`0x4000A47C`).
> `duration` là khoảng cách giữa 2 hành động, đơn vị **giây**.

---

## KB-20. Virtual LED / Wallpaper

### Virtual LED (chữ chạy)

| Hành động | Method | URL |
|---|---|---|
| Capability tất cả | `GET` | `/ISAPI/DisplayDev/VideoWall/1/virtualLED/capabilities` |
| Đọc tất cả | `GET` | `/ISAPI/DisplayDev/VideoWall/1/virtualLED` |
| Thêm | `POST` | `/ISAPI/DisplayDev/VideoWall/1/virtualLED` |
| Set tất cả | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/virtualLED` |
| Đọc 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/1/virtualLED/<SubtitlesID>` |
| Sửa 1 | `PUT` | `/ISAPI/DisplayDev/VideoWall/1/virtualLED/<SubtitlesID>` |
| Xóa 1 | `DELETE` | `/ISAPI/DisplayDev/VideoWall/1/virtualLED/<SubtitlesID>` |

Giới hạn đo trên C66S (🔶 đo lại trên máy bạn):
```
virtualLEDNums                      = 3 / tường
perWallClockSubtitlesMaxNum         = 1   (đồng hồ)
perWallDynamicSubtitlesMaxNum       = 1   (chữ chạy ngang)
perWallVerticalSubtitlesMaxNum      = 2   (chữ chạy dọc)
Rect.MaxHoriSubtitleWindowSize      = 38400 × 30720
```

> 🔴 Nếu `isSupportSaveSceneVirLed = false`: chữ chạy **không được lưu vào scene**, phải PUT lại sau mỗi lần activate.

### Wallpaper (ảnh nền)

| Hành động | Method | URL |
|---|---|---|
| Capability | `GET` | `/ISAPI/DisplayDev/VideoWall/baseMap/capabilities` |
| Đọc tất cả | `GET` | `/ISAPI/DisplayDev/VideoWall/baseMap?isGetBaseMapFile=false` |
| Set tất cả | `PUT` | `/ISAPI/DisplayDev/VideoWall/baseMap` |
| Đọc 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/baseMap/<mapFileID>` |
| Sửa 1 | `PUT` | `/ISAPI/DisplayDev/VideoWall/baseMap/<mapFileID>` |
| Xóa 1 | `DELETE` | `/ISAPI/DisplayDev/VideoWall/baseMap/<mapFileID>` |

Giới hạn (C66S): `baseMapNums = 3`, chỉ **JPEG/JPG**, max **8 MB**, max **1920×1080**, kích thước phải là bội số của **16 (rộng) / 8 (cao)**.

> 🔴 Nếu `isSupportSaveSceneBaseMap = false`: ảnh nền **không lưu vào scene**.

---

# PHỤ LỤC

## P1. Công thức ID

```
id = T × 16777216 + boardID × 65536 + portID
```

| Loại | `T` | Ví dụ | Giải mã |
|---|---|---|---|
| Video channel (in/out) | `0x01` | `17235971` = `0x01070003` | board 7, port 3 |
| Window (`VWMWID`) | `0x02` | `33554433` = `0x02000001` | window #1 |
| Layer (`layerIdx`) | `0x04` | `67108865` = `0x04000001` | layer #1 |

Dùng để **hiểu**, nhưng luôn **đọc ID thật từ API**. Chưa xác nhận công thức đúng với board LED/decode.

## P2. Bảng toạ độ 12 màn

**Lưới 4×3** — canvas ảo `7680 × 5760`:

| Ô | col | row | `x` | `y` |
|---|---|---|---|---|
| SCR-01 | 0 | 0 | 0 | 0 |
| SCR-02 | 1 | 0 | 1920 | 0 |
| SCR-03 | 2 | 0 | 3840 | 0 |
| SCR-04 | 3 | 0 | 5760 | 0 |
| SCR-05 | 0 | 1 | 0 | 1920 |
| SCR-06 | 1 | 1 | 1920 | 1920 |
| SCR-07 | 2 | 1 | 3840 | 1920 |
| SCR-08 | 3 | 1 | 5760 | 1920 |
| SCR-09 | 0 | 2 | 0 | 3840 |
| SCR-10 | 1 | 2 | 1920 | 3840 |
| SCR-11 | 2 | 2 | 3840 | 3840 |
| SCR-12 | 3 | 2 | 5760 | 3840 |

Mọi ô: `width = height = 1920`.

**Lưới khác:**

| Lưới | Canvas ảo |
|---|---|
| 6 × 2 | 11520 × 3840 |
| 4 × 3 | 7680 × 5760 |
| 3 × 4 | 5760 × 7680 |
| 12 × 1 | 23040 × 1920 |

Công thức: `canvas_w = cols × 1920`, `canvas_h = rows × 1920`.

### 🔴 Ô lưới là VUÔNG 1920×1920

Màn vật lý là 1920×1080, nhưng trong `uniformCoordinate` mỗi màn chiếm ô **vuông 1920×1920**. `baseOutputSize` áp cho **cả rộng lẫn cao**, bất kể tỉ lệ panel thật.

Quy đổi ra pixel thật (nếu cần):
```
x_thật = x_ảo × (rộng_thật_1_màn / 1920)
y_thật = y_ảo × (cao_thật_1_màn  / 1920)      → 1080p: × 0.5625
```

## P3. `Rect` cho các bố cục thường dùng

Lưới 4×3:

| Muốn | `x` | `y` | `width` | `height` |
|---|---|---|---|---|
| 1 màn SCR-01 | 0 | 0 | 1920 | 1920 |
| 1 màn SCR-07 | 3840 | 1920 | 1920 | 1920 |
| 2 màn ngang (01+02) | 0 | 0 | 3840 | 1920 |
| 2 màn dọc (01+05) | 0 | 0 | 1920 | 3840 |
| Khối 2×2 trên-trái | 0 | 0 | 3840 | 3840 |
| Khối 2×2 trên-phải | 3840 | 0 | 3840 | 3840 |
| Cả hàng 1 | 0 | 0 | 7680 | 1920 |
| Cả hàng 3 | 0 | 3840 | 7680 | 1920 |
| Cả cột 1 | 0 | 0 | 1920 | 5760 |
| **Toàn tường 12 màn** | 0 | 0 | 7680 | 5760 |
| Nửa trên SCR-01 | 0 | 0 | 1920 | 960 |
| Lệch tâm, vắt biên | 960 | 960 | 3840 | 1920 |

> 🔴 Tài liệu ghi `range:[0,1920]` — **SAI**. Đã test `height=3840` trả `OK` và hiển thị đúng.

## P4. Mã lỗi chung

`statusCode`:

| Mã | Nghĩa |
|---|---|
| **0, 1** | OK (**cả hai** đều thành công) |
| 2 | Device Busy |
| 3 | Device Error |
| 4 | Invalid Operation |
| 5 | Invalid XML Format |
| 6 | Invalid XML Content |
| 7 | Reboot Required |

Chuỗi debug chuẩn khi test 1 API ghi:

| Hiện tượng | `subStatusCode` | Nghĩa |
|---|---|---|
| GET vào endpoint chỉ nhận PUT | `methodNotAllowed` | ✅ **URL đúng**, sai method |
| PUT không body | `badXmlFormat` | Method đúng, body trống / có BOM / chưa chọn raw+XML |
| PUT body có element rỗng | `badParameters` | XML parse được, **giá trị** sai |
| PUT đúng | `ok` + `statusCode` 0/1 | ✅ Xong |
| Gọi ID không tồn tại | `invalidOperation` | Lấy ID thật từ list endpoint |
| Firmware không hỗ trợ | `notSupport` | Gọi `capabilities` trước |

| HTTP | Nguyên nhân | Hành động |
|---|---|---|
| 401 | Sai credential / không gửi Digest / sai thuật toán | Xem `stale`. `FALSE` = sai pass, **dừng** |
| 403 | Quyền không đủ / IP bị chặn | Dùng admin |
| 400 | Payload thiếu field `req`, sai `xmlns` | GET trước rồi sửa |
| 404 | Sai URL / API không có trên firmware | Đối chiếu `capabilities` |
| 500 + `notSupport` | Firmware không hỗ trợ | Kiểm `capabilities` |

> ⚠️ `requestURL` trong `ResponseStatus` luôn **rỗng** — không dùng để đối chiếu request nào lỗi.

## P5. Mã lỗi decoding

Trả về từ `PUT .../sub/<VWSWID>/start`:

| Nhóm | `subStatusCode` | `errorCode` |
|---|---|---|
| **Nguồn vào** | `frontInputSignal5vIsNotRecognized` | `0x4000A403` |
| | `unstableInputSignal` | `0x4000A404` |
| | `inputResolutionIsNotSupported` | `0x4000A405` |
| | `inputInterfaceProtocolMismatch` | `0x4000A406` |
| | `inputColorSpaceMismatch` | `0x4000A407` |
| **Đầu ra** | `abnormalChipOfOutput` | `0x4000A40A` |
| | `theHotPlugFrequencyBetweenHdmiOutputAndPostScreenIsTooHigh` | `0x4000A40B` |
| | `outputModeMismatch` | `0x4000A40C` |
| | `unstableOutputSignal` | `0x4000A40D` |
| **Stream** | `streamingIsNotContinuous` | `0x4000A3FB` |
| | `streamDataIsAccumulated` | `0x4000A3FC` |
| | `encodingFormatIsNotSupported` | `0x4000A3FD` |
| | `unstableDecodingFrameRate` | `0x4000A400` |
| **Phần cứng** | `decodingFirmwareException` | `0x4000A401` |
| | `inputChipException` | `0x4000A402` |

## P6. Các bẫy tổng hợp

| # | Bẫy | Cách tránh |
|---|---|---|
| 1 | `Rect range:[0,1920]` trong tài liệu | Sai. Dùng tới `cols×1920 × rows×1920` |
| 2 | Ô lưới tưởng 1920×1080 | Là **vuông 1920×1920** |
| 3 | `channelID` tưởng 1..12 | Là ID tổ hợp byte. Đọc từ `/outputs/channels` |
| 4 | URL `decodingDevice` | Đúng là **`decoingDevice`** (thiếu `d`) |
| 5 | Trường `backgroundColor` | Đúng là **`backgroudColor`** (thiếu `n`) |
| 6 | PUT lại nguyên response GET | `badParameters`. Gửi **tối thiểu** |
| 7 | Bỏ `wndOperateMode` khi PUT window | `badParameters` |
| 8 | `signalMode` viết `videoInput` | Đúng là **`video input`** (có dấu cách) |
| 9 | `outputPortEnabled` tưởng là công tắc | Không phải. Không bật/tắt được cổng HDMI |
| 10 | Kéo window không thấy đổi | Window khác `layerIdx` cao hơn đang đè. Dùng `/top` hoặc `DELETE` |
| 11 | Cắm cáp tưởng đã lên tường | Phải xuất hiện trong `/VideoWall/<id>/outputs` |
| 12 | Retry khi 401 | Thiết bị khóa IP. Xem `stale` trước |
| 13 | Cắm cáp mạng vào jack RJ-45 serial | 2 jack RJ-45 là `RJ45Console` + `reusePort`, **không phải LAN** |
| 14 | `closeAll` trả `invalidOperation` | Chưa có dây RS-232/485 + `workMode=screenCtrl` |
| 15 | Web UI thiết bị đang mở | `multipleVideowallClientConflict 0x4000A4F8`. Đóng trước khi test |
| 16 | Board output khác `timeSequenceMode` | Đồng nhất trước khi cho window vắt qua 2 board |
| 17 | Scene không giữ chữ chạy / ảnh nền | Kiểm `isSupportSaveSceneVirLed/BaseMap`, tự áp lại sau activate |
| 18 | `activate` chậm | Kiểm `sceneSwitchDelayEnabled` / `sceneSwitchDelay` |

## P7. Thứ tự test khuyến nghị

```
KB-01 → KB-02          kết nối, lấy ID              (an toàn, chỉ GET)
  ↓
KB-04                  đọc bố cục hiện tại          (an toàn, chỉ GET)
  ↓
KB-03                  gán lưới                     ⚠️ ghi — làm trên tường unbound trước
  ↓
KB-05 → KB-11          window + nguồn + decode      ⚠️ ghi
  ↓
KB-08 → KB-09 → KB-10  move/resize/z-order/chia ô   ⚠️ ghi
  ↓
KB-13 → KB-14 → KB-15  scene                        ⚠️ ghi
  ↓
KB-16                  poll                         (an toàn)
  ↓
KB-12                  xóa window (dọn dẹp)         ⚠️ ghi
  ↓
KB-18 → KB-19 → KB-20  crop / plan / LED            (mở rộng)
  ↓
KB-17                  tắt màn                      ⛔ CUỐI CÙNG — không có API bật lại
```

> ⛔ **Đừng dùng `closeAll` hay `DELETE .../windows` để test kết nối.** Sẽ tắt/xóa cả tường thật.
> ✅ Test ghi an toàn: `GET` rồi `PUT` lại y nguyên tên 1 input (`/inputs/channels/<id>`).
