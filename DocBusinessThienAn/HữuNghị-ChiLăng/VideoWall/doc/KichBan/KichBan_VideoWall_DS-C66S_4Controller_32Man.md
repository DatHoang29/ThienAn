# Kịch bản test API Video Wall — DS-C66S / 4 controller / 32 màn

> Bản sinh đôi của [KichBan_VideoWall_DS-C30S-S11_12Man.md](KichBan_VideoWall_DS-C30S-S11_12Man.md)
> cho cấu hình thật: **1 khung trung tâm + 3 khung con**, lưới **8 cột × 4 hàng = 32 màn**.
> Xem giải thích topology: [_source/doc/GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md](_source/doc/GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md).

Tất cả URL tương đối, ghép với `{{base}} = http://<ip_controller>:<port>`.
Auth: **Digest** (admin). Header: `Content-Type: application/xml` (bỏ XML declaration, không BOM).
Thành công = `statusCode` **0 hoặc 1**. Lỗi thì đọc `subStatusCode`.

> 📌 **Nguồn sự thật:** response mẫu trong tài liệu này lấy từ **`LogsAPI/`** (đo trực tiếp trên
> thiết bị thật). *(Thư mục `VideoWall/API/` — Postman collection do người khác đưa, chưa kiểm
> chứng — đã xoá để tránh nhầm.)*

> ⛔ **Bẫy khoá IP:** sai mật khẩu **2 lần liên tiếp** trên 1 IP ⇒ khung đó khoá IP, không mở lại
> được qua LAN. Test Digest sai chỉ làm trên **mock server nội bộ**, không bao giờ trên thiết bị
> thật. Backend đã có circuit-breaker theo IP (`VwWallProfile.MaxConsecutiveFailures = 2`,
> `BlockMinutes = 5`).

---

## 0. Bối cảnh & khác biệt so với bản 1 controller

Trước đây test **1 khung DS-C30S-S11 / 12 màn / lưới 4×3**. Giờ là **4 khung DS-C66S / 32 màn /
lưới 8×4**. ISAPI Hikvision **không có lệnh nhóm thiết bị** → mỗi khung là 1 HTTP server độc lập.

| Bản 1 controller | Bản 4 controller |
|---|---|
| 1 kết nối / 1 IP | **Mỗi KB dưới đây chạy 1 lần cho MỖI khung** (trừ KB nói rõ "toàn tường") — 4 IP, 4 bộ Digest |
| Lưới cứng 4×3, canvas ảo `7680×5760` | Lưới **8×4**; canvas ảo **suy từ `GET .../{wallNo}/outputs`**, KHÔNG hardcode |
| 1 `wallNo` | Mỗi khung tự `ResolveWall` (1 khung C66S có 8 wall logic `VideoWall1..8`) — số có thể khác nhau |
| `Rect` cửa sổ = toạ độ ảo toàn tường | **Phải cắt** theo vùng panel từng khung (công thức [P2](#p2-công-thức-toisapilocalrect)) |
| Activate 1 SID | Activate SID **trên từng khung**; chờ 2–3s; verify `scene/isRunning` **cả 4**; 3/4 OK ⇒ lệch pha |

**Bản đồ khung ↔ vùng tường** (giả định từ sơ đồ — **xác nhận bằng KB-00**):

| Khung | `VwController.Code` | Vùng phụ trách | Số màn | Số cổng 4K IN (từ center) | Số cổng FHD OUT |
|---|---|---|---|---|---|
| Trung tâm | C1 | nhận nguồn, chia 8×4K | — | — | 8 × 4K (xuống 3 con) |
| Con 1 | C2 | cột 1–4, hàng 1–4 | 16 | 4 | 16 |
| Con 2 | C3 | cột 5–6, hàng 1–4 | 8 | 2 | 8 |
| Con 3 | C4 | cột 7–8, hàng 1–4 | 8 | 2 | 8 |

| Kịch bản | Mục đích |
|---|---|
| [KB-00](#kb-00-probe-read-only-4-khung-tại-hiện-trường) | **Probe read-only 4 khung — chốt 6 ẩn số trước khi làm gì khác** |
| [KB-01](#kb-01-kết-nối--đọc-năng-lực-từng-khung) | Kết nối, đọc năng lực, lấy `videoWallID` từng khung |
| [KB-02](#kb-02-lấy-id-output--input-từng-khung) | Lấy outputID + inputID từng khung |
| [KB-03](#kb-03-gán-output-vào-lưới-của-từng-khung) | Setup lưới cho từng khung (không phải toàn tường) |
| [KB-04](#kb-04-đọc-bố-cục-hiện-tại-từng-khung) | Đọc bố cục hiện tại |
| [KB-05](#kb-05-mở-window-1-màn--gán-nguồn-1-khung) | Mở window 1 màn + nguồn (trong 1 khung) |
| [KB-06](#kb-06-cửa-sổ-vắt-nhiều-khung--slicing) | **Cửa sổ vắt nhiều khung → cắt lát** |
| [KB-07](#kb-07-đổi-nguồn--di-chuyển--resize--z-order) | Đổi nguồn / move / resize / z-order |
| [KB-11](#kb-11-startstop-decoding) | Start / Stop decoding |
| [KB-12](#kb-12-xóa-window) | Xóa window |
| [KB-13](#kb-13-tạo-scene--saveData-trên-từng-khung) | Tạo scene + `saveData` **trên từng khung** |
| [KB-14](#kb-14-active-scene--đa-khung--lệch-pha) | **Active scene đa khung + xử lý lệch pha** |
| [KB-16](#kb-16-poll-trạng-thái-4-khung) | Poll trạng thái 4 khung |
| [KB-17](#kb-17-tắt-màn-qua-serial) | Tắt màn (serial transparent transmission) |
| [KB-18](#kb-18-genlock--kiểm-mép-ghép) | **GENLOCK & kiểm mép ghép giữa 2 khung** |

---

## KB-00. Probe read-only 4 khung tại hiện trường

**Chỉ `GET`. An toàn tuyệt đối. Chạy TRƯỚC mọi thứ khác.** Mục tiêu: chốt 6 ẩn số ở
[mục 6 của doc giải thích](_source/doc/GiaiThich_KetNoi_VideoWall_DS-C66S-H88-CL.md#6-những-điểm-phải-xác-nhận-với-hikvision--nhà-cung-cấp).

Chạy **cho từng IP** (C1, C2, C3, C4):

| # | Method | URL | Đọc gì / chốt điều gì |
|---|---|---|---|
| 1 | `GET` | `/SDK/activateStatus` | `activated` — khung sống, đã activate *(không cần auth)* |
| 2 | `GET` | `/ISAPI/Security/userCheck` | `statusValue=200`, `isActivated` — Digest OK |
| 3 | `GET` | `/ISAPI/System/deviceInfo` | `model`, `serialNumber`, `firmwareVersion` → **ẩn số 1: mã khung thật** (S12? S6?) |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall` | list `VideoWall1..N`, `wallBindOutputStatus` → khung có mấy wall, wall nào `bound` |
| 5 | `GET` | `/ISAPI/DisplayDev/VideoWall/<boundWall>/outputs` | `WallOutput[].Rect` (sắp theo `Coordinate`) → **suy lưới thật** của khung + **ẩn số 3: panel px** |
| 6 | `GET` | `/ISAPI/DisplayDev/VideoWall/capabilities` | `baseOutputSize`, `maxWallNums`, `isSupportScene`, `isSupportRoam` |
| 7 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | `portType` (HDMI/…), `signalStatus` → **ẩn số 2: camera vào HDMI hay IP** |
| 8 | `GET` | `/ISAPI/DisplayDev/Video/streaming/channels` | có/không stream IP → xác nhận có card `DS-C66S-DEC` không |
| 9 | `GET` | `/ISAPI/DisplayDev/VideoWall/<boundWall>/scene` | list SID → **ẩn số 4: SID scheme** (độc lập theo wall?) |
| 10 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | `id` + `PortInBoard` (boardID/portID) — chưa từng đo thật, lấy để đối chiếu |

### Response mẫu (đo thật trên thiết bị test, `LogsAPI/session-20260904-real.json`)

Bước 6 — `capabilities`:
```xml
<VideoWallCap version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <maxWallNums>8</maxWallNums>
  <maxWindowNums>512</maxWindowNums>
  <baseOutputSize>1920</baseOutputSize>
  <isSupportScene>true</isSupportScene>
  <isSupportRoam>true</isSupportRoam>
  <isSupportPlan>false</isSupportPlan>
  <SceneCap>
    <maxSceneNums>128</maxSceneNums>
    <isSupportSceneInfo>true</isSupportSceneInfo>
    <isSupportSceneCopy>false</isSupportSceneCopy>
    <isSupportSaveSceneVirLed>false</isSupportSaveSceneVirLed>
    <isSupportSaveSceneBaseMap>false</isSupportSaveSceneBaseMap>
  </SceneCap>
</VideoWallCap>
```

Bước 4 — `VideoWall` (1 khung C66S trả **8 tường**, không phải 1):
```xml
<VideoWallList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <VideoWall><id>1</id><name>VideoWall1</name>...</VideoWall>
  <VideoWall><id>2</id><name>VideoWall2</name>...</VideoWall>
  ...
  <VideoWall><id>8</id><name>VideoWall8</name>...</VideoWall>
</VideoWallList>
```
> Trên thiết bị đã đo (`LogsAPI/`): có wall ở trạng thái `bound` (đang cắm màn) và wall `unbound`
> (sandbox). **Đừng mặc định wallNo = 1** — phải đọc `wallBindOutputStatus` rồi chọn wall `bound`.

### Bảng nghiệm thu (điền tay tại hiện trường)

| Ẩn số | C1 (trung tâm) | C2 (con 1) | C3 (con 2) | C4 (con 3) |
|---|---|---|---|---|
| `model` / serial | | | | |
| Wall nào `bound` | | | | |
| Lưới suy từ `outputs` (cols×rows) | — | | | |
| `baseOutputSize` | | | | |
| Panel px (từ `Rect`) | | | | |
| Camera vào: HDMI / IP | | — | — | — |
| Có card DEC? | | | | |
| SID list | | | | |

---

## KB-01. Kết nối & đọc năng lực (từng khung)

Giống KB-01 bản 1 controller, **chạy 4 lần**. Ghi lại cho **từng khung**:

```
VideoWallCap.baseOutputSize          ← DÙNG RIÊNG cho phép toạ độ của khung đó
VideoWallCap.maxWindowNums
VideoWallCap.isSupportScene / isSupportRoam / isSupportPlan
VideoWallCap.SceneCap.maxSceneNums / isSupportSceneInfo
```

`GET /ISAPI/DisplayDev/VideoWall` → lấy **`videoWallID` bound** của khung đó (KB-00 bước 4/5). Nếu
nhiều wall `bound`: backend lấy wall đầu tiên (`VwISAPIDeviceService.WallResolution.cs`), cache 30′.

---

## KB-02. Lấy ID output & input (từng khung)

| # | Method | URL | Lấy gì |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | các `id` output + `PortInBoard` (board/port) + `outputPortAccessStatus` |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>/outputs` | `WallOutput[].outputID` + `Rect` → **ô nào của khung** |
| 3 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | các `id` input + `portType` + `signalStatus` |

> 🔴 `id` **không phải 1..N**. Công thức: `id = T×16777216 + boardID×65536 + portID`, `T=0x01` cho
> video channel. Luôn đọc từ list endpoint. Xem [P1](#p1-công-thức-id).
>
> **Số output mỗi khung khác nhau:** C2 = 16, C3 = 8, C4 = 8. C1 (trung tâm) output là 8×4K
> **xuống 3 con**, không ra màn — cấu hình `outputs` của C1 nằm ngoài phạm vi test hiển thị này.

---

## KB-03. Gán output vào lưới CỦA TỪNG KHUNG

🔴 **Điểm khác lớn nhất:** không gán "lưới toàn tường 8×4" cho 1 khung. Mỗi khung chỉ khai
**lưới cục bộ của vùng nó** trong toạ độ `uniformCoordinate` (ô vuông `baseOutputSize`).

- **C2 (con 1)** — vùng 4 cột × 4 hàng → canvas cục bộ `4·bos × 4·bos` (vd `7680×7680` nếu bos=1920):

```http
PUT {{base_C2}}/ISAPI/DisplayDev/VideoWall/<wallNo_C2>
```
```xml
<VideoWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>WALLNO_C2</id>
  <name>Vung Con 1</name>
  <WallOutputList>
    <!-- 16 WallOutput, mỗi ô vuông bos×bos, x = col_cục_bộ·bos, y = row_cục_bộ·bos -->
    <WallOutput><outputID>ID_1</outputID>
      <Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <WallOutput><outputID>ID_2</outputID>
      <Rect><Coordinate><x>1920</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
    <!-- ... tới (3,3) cục bộ -->
  </WallOutputList>
</VideoWall>
```

- **C3 (con 2)** — vùng 2 cột × 4 hàng → canvas cục bộ `2·bos × 4·bos`.
- **C4 (con 3)** — như C3.

> `outputID` lấy từ KB-02 của **đúng khung đó**. Chỉ gửi `outputID` (req) + `Rect`, bỏ field
> chỉ-đọc (gửi lại nguyên response GET ⇒ `badParameters`).

---

## KB-04. Đọc bố cục hiện tại (từng khung)

| # | Method | URL | Lấy gì |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>` | cấu hình tường của khung |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>/outputs` | output ở ô cục bộ nào (`gridCol = Rect.x / bos`) |
| 3 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>/windows` | window đang mở + nguồn |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>/windows/status` | trạng thái decode |

---

## KB-05. Mở window 1 màn + gán nguồn (1 khung)

Giống KB-05 bản cũ nhưng toạ độ là **cục bộ trong khung**. Ví dụ: đưa nguồn lên ô (col=1,row=1)
**cục bộ của C2**:

```http
POST {{base_C2}}/ISAPI/DisplayDev/VideoWall/<wallNo_C2>/windows
```
```xml
<WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <wndOperateMode>uniformCoordinate</wndOperateMode>
  <Rect><Coordinate><x>1920</x><y>1920</y></Coordinate><width>1920</width><height>1920</height></Rect>
  <windowMode>1</windowMode>
  <wndShowMode>subWndMode</wndShowMode>
  <SubWindowList>
    <SubWindow><id>1</id><SubWindowParam>
      <signalMode>video input</signalMode>
      <videoInputChannelID>ID_INPUT_C2</videoInputChannelID>
    </SubWindowParam></SubWindow>
  </SubWindowList>
</WallWindow>
```

> ⚠️ `signalMode` = `video input` (có dấu cách). `wndOperateMode` bắt buộc.
> ⚠️ `videoInputChannelID` phải là input **của đúng khung C2**, không phải của center.

---

## KB-06. Cửa sổ vắt nhiều khung → slicing

Đây là ca mà backend `VwSceneRegionService` xử lý tự động; khi test tay bằng Postman phải **tự cắt**.

**Ví dụ:** cửa sổ ITS-MAP phủ **cột 2–7 × hàng 2–3** trên tường tổng (toạ độ tuyệt đối, panel px
`P` — dùng đúng đơn vị FE/DB lưu):

- Toạ độ tuyệt đối: `x = 1·P`, `y = 1·P_h`, `w = 6·P`, `h = 2·P_h` *(cột/hàng đánh số từ 0)*.

Cắt cho từng khung bằng công thức [P2](#p2-công-thức-toisapilocalrect):

| Khung | Vùng cục bộ nhận (cột × hàng, gốc 0) | Rect cục bộ ISAPI (bos=1920) |
|---|---|---|
| C2 (cột 0–3) | cột 1–3, hàng 1–2 | `x=1920, y=1920, w=5760, h=3840` |
| C3 (cột 4–5) | cột 0–1, hàng 1–2 | `x=0, y=1920, w=3840, h=3840` |
| C4 (cột 6–7) | cột 0, hàng 1–2 | `x=0, y=1920, w=1920, h=3840` |

→ `POST .../windows` **3 lần**, mỗi khung 1 mảnh, cùng `videoInputChannelID` là nguồn ITS **đã có
trên khung đó** (nếu ITS-MAP chỉ vào center thì con con phải nhận nó qua cổng 4K từ center —
`signalMode`/`videoInputChannelID` trỏ vào input 4K tương ứng).

> 🔴 Ranh giới vùng phải trùng ranh giới "quad" — không cửa sổ nào được yêu cầu 1 khung vẽ phần
> nằm ngoài vùng panel nó lái.
> Cần `isSupportRoam = true` (đã xác nhận trên C66S).

---

## KB-07. Đổi nguồn / di chuyển / resize / z-order

Giống KB-07/08/09 bản cũ, **trong phạm vi 1 khung**, toạ độ cục bộ:

| Hành động | Method | URL |
|---|---|---|
| Đổi nguồn sub-window | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/sub/1` (fallback: PUT cả window) |
| Move / resize | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>` (`id` + `wndOperateMode` + `Rect`) |
| Đưa lên trên | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/top` |
| Xuống dưới | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/bottom` |

> ⚠️ PUT trả OK nhưng tường không đổi ⇒ có window khác `layerIdx` cao hơn đè. `/top` hoặc DELETE.
> Với cửa sổ vắt nhiều khung: phải `/top` **trên từng khung** cho khớp z-order.

---

## KB-11. Start/Stop decoding

Không đổi so với bản cũ, per-khung:

| Hành động | Method | URL |
|---|---|---|
| Start | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/sub/1/start` |
| Stop | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/sub/1/stop` |
| Status 1 window | `GET` | `.../VideoWall/<wallNo>/windows/<VWMWID>/sub/1/status` |
| Status tất cả | `GET` | `.../VideoWall/<wallNo>/windows/status` |

Mã lỗi decode: xem [P4](#p4-mã-lỗi) (nhóm nguồn vào / đầu ra / stream / phần cứng).

---

## KB-12. Xóa window

| Hành động | Method | URL |
|---|---|---|
| Xóa 1 window trên 1 khung | `DELETE` | `.../VideoWall/<wallNo>/windows/<VWMWID>` |
| Xóa tất cả window 1 khung | `DELETE` | `.../VideoWall/<wallNo>/windows` |
| **Làm sạch toàn tường** | `DELETE .../windows` trên **cả 4 khung** | — |

> `DELETE .../windows` không hoàn tác. Đây cũng là bước đầu của `SyncSceneWindowsToDevice` cho
> mỗi khung.

---

## KB-13. Tạo scene & saveData TRÊN TỪNG KHUNG

Kịch bản **toàn tường** = tạo scene + `saveData` + (sau này) `activate` trên **cả 4 khung**, mỗi
khung một `SID` riêng. Kịch bản **vùng** = chỉ khung sở hữu.

Cho **mỗi khung tham gia**:

| # | Method | URL | Ghi chú |
|---|---|---|---|
| 1 | `GET` | `.../VideoWall/<wallNo>/scene/capabilities` | `maxSceneNums`, độ dài tên |
| 2 | `GET` | `.../VideoWall/<wallNo>/scene` | đếm SID đã có |
| 3 | `POST` | `.../VideoWall/<wallNo>/scene` | body `<WallScene><name>...</name></WallScene>` → trả `SID` mới |
| 4 | — | *(dựng bố cục THẬT lên khung: KB-05/06/11)* | 🔴 `saveData` chụp tường ĐANG CHẠY |
| 5 | `PUT` | `.../VideoWall/<wallNo>/scene/<SID>` | đặt tên |
| 6 | `PUT` | `.../VideoWall/<wallNo>/scene/<SID>/saveData` | body placeholder `<Request .../>` — ⭐ chụp bố cục |

> Map lại: `VwScene.OutputId` trong DB hiện là **1 field** — với 4 khung mỗi khung 1 SID, cần
> quyết định lưu SID theo khung thế nào (**ẩn số 4** KB-00). Nếu firmware đánh SID **giống nhau
> giữa các khung** khi tạo cùng thứ tự thì 1 field vẫn đủ; nếu lệch thì phải bảng phụ
> `controllerId → SID`.
>
> `isSupportSceneCopy = false` (C66S) → không "soạn nháp" trên thiết bị, phải dựng bố cục thật.
> `isSupportSaveSceneVirLed/BaseMap = false` → chữ chạy / ảnh nền KHÔNG lưu vào scene, phải áp lại
> sau mỗi lần activate, **trên từng khung**.

---

## KB-14. ACTIVE scene — đa khung + lệch pha

### Luồng backend (`VwISAPIDeviceService.ActivateScene`)

```
targetControllerIds = (VwScene.ControllerId rỗng) ? [C1..C4 lái panel] : [khung sở hữu]
foreach controllerId:
    GET  .../VideoWall/capabilities         → isSupportScene? (không ⇒ bỏ qua)
    ResolveWall(controller)
    PUT  .../VideoWall/<wallNo>/scene/<SID>/activate     (không body)
gom succeededControllerIds
```

### Test tay

| # | Method | URL | Mục đích |
|---|---|---|---|
| 1 | `GET` | `.../VideoWall/<wallNo>/scene/isRunning` (mỗi khung) | scene đang chạy; trùng SID ⇒ bỏ qua khung đó |
| 2 | `PUT` | `.../VideoWall/<wallNo>/scene/<SID>/activate` (mỗi khung) | ⭐ KÍCH HOẠT — **gửi lần lượt 4 khung** |
| 3 | — | *chờ 2–3 giây* | 🔴 chuyển cảnh không tức thời |
| 4 | `GET` | `.../VideoWall/<wallNo>/scene/isRunning` (mỗi khung) | verify `sceneID` = SID trên **cả 4** |
| 5 | `GET` | `.../VideoWall/<wallNo>/windows` (mỗi khung) | đọc bố cục mới |

### 🔴 Lệch pha (partial failure)

4 lệnh `activate` **không nguyên tử**. Nếu 3/4 khung OK, 1 khung fail:

- Backend phát cảnh báo `HardwareOutOfSync` (NATS) với `succeededControllerIds` /
  `failedControllerIds`; FE hiển thị "3/4 vùng đã đổi".
- **Không retry mù** cả 4 — chỉ retry khung fail sau khi kiểm `subStatusCode`.
- `inSceneSwitchingPleaseDoNotOperate` (`0x4000A1AB`) trên 1 khung ⇒ **không gọi lệnh khác cho
  khung đó**, chờ rồi verify bằng `isRunning`.
- `multipleVideowallClientConflict` (`0x4000A4F8`) ⇒ có client khác (web UI / iVMS) đang giữ
  khung đó → đóng rồi thử lại.

### Cách 2 — "activate" bằng dựng lại window (không dùng scene thiết bị)

Cho **mỗi khung**: `DELETE .../windows` → `POST .../windows` ×N (Rect cục bộ đã cắt) →
`.../sub/1/start` ×N → `/top` ×N theo z-order. Chậm hơn nhưng không phụ thuộc SID.

---

## KB-16. Poll trạng thái 4 khung

| # | Method | URL (mỗi khung) | Tần suất |
|---|---|---|---|
| 1 | `GET` | `.../VideoWall/<wallNo>/windows/status` | 3–5s — trạng thái decode |
| 2 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | 5–10s — cáp màn (nhẹ) |
| 3 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | 5–10s — `signalStatus` nguồn |
| 4 | `GET` | `/ISAPI/DisplayDev/decoingDevice/status?format=json` | 30–60s — ⚠️ nặng, health phần cứng |

> 🔴 URL bước 4 viết là **`decoingDevice`** (thiếu `d`) — lỗi chính tả của hãng nhưng là URL thật.
> Với 4 khung: poll **song song 4 IP**, gộp kết quả. Backend đã có `VwController.Status` +
> `GenlockInConnected/GenlockOutConnected` để hiển thị topology.

---

## KB-17. Tắt màn (qua serial)

`closeAll` gửi lệnh tắt nguồn **qua RS-232/485** tới màn — mỗi khung có cổng serial riêng nối tới
**cụm màn của nó**. Endpoint backend: `VwISAPIDeviceClient.EndpointSerialTransData`
(`ISAPI/System/Serial/ports/{portId}/Transparent/channels/{channelId}/transData`).

| # | Method | URL (khung phụ trách cụm màn cần tắt) |
|---|---|---|
| 1 | `GET` | `/ISAPI/System/Serial/capabilities` — kiểm `workMode` hỗ trợ `screenCtrl` |
| 2 | `PUT` | `.../Serial/ports/<portId>/Transparent/channels/<chId>/open` |
| 3 | `PUT` | `.../Serial/ports/<portId>/Transparent/channels/<chId>/transData` — payload lệnh tắt của hãng màn |
| 4 | `PUT` | `.../Serial/ports/<portId>/Transparent/channels/<chId>/close` |

> ⛔ **Không có API bật lại** (`ScreenCtrl` một chiều) — chỉ `openScreen` trong Plan, mà C66S
> `isSupportPlan = false`. Tắt xong bật bằng tay/remote.
> Chưa có dây serial ⇒ thay thế: `DELETE .../windows` (màn sáng, hiện nền) hoặc `wallBackMode=color`.

---

## KB-18. GENLOCK & kiểm mép ghép

Không phải API — kiểm tra vật lý + cấu hình.

1. Xác nhận chuỗi GENLOCK: **center LOOP → C2 IN → C2 LOOP → C3 IN → C3 LOOP → C4 IN**.
2. Đối chiếu DB: `VwController.GenlockInConnected / GenlockOutConnected` phải khớp thực tế từng khung.
3. Test mép: mở **1 cửa sổ video động** (camera có chuyển động) vắt **ranh giới C2 | C3** (quanh
   cột 4–5). Quan sát đường ghép dọc:
   - **Không xé, không lệch dòng** ⇒ genlock OK.
   - **Xé hình / rách ngang khi vật di chuyển** ⇒ genlock chưa khoá hoặc sai thứ tự chuỗi.
4. Cùng test cho ranh giới **C3 | C4** (cột 6–7).

---

# PHỤ LỤC

## P1. Công thức ID

```
id = T × 16777216 + boardID × 65536 + portID
```

| Loại | `T` | Ví dụ |
|---|---|---|
| Video channel (in/out) | `0x01` | `17235971` = `0x01070003` → board 7, port 3 |
| Window (`VWMWID`) | `0x02` | `33554433` = `0x02000001` → window #1 |
| Layer (`layerIdx`) | `0x04` | chỉ đọc, số lớn nằm trên |

Luôn đọc ID thật từ list endpoint **của đúng khung**. Mỗi khung có không gian ID riêng.

## P2. Công thức `ToIsapiLocalRect`

Nguồn: `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Infrastructure/Services/Scene/VwSceneRegionService.cs`.

```
# originCol/originRow = min GridCol/GridRow của các màn thuộc khung (từ VwScreen)
# panelWidthPx/panelHeightPx = kích thước 1 panel, ĐÚNG đơn vị FE/DB lưu VwWindowScene.X/Y/W/H
# baseOutputSize = từ GET .../capabilities của khung đó (đo thật = 1920)

localX  = X_tuyệt_đối − originCol × panelWidthPx
localY  = Y_tuyệt_đối − originRow × panelHeightPx

X_ISAPI = localX × baseOutputSize / panelWidthPx
Y_ISAPI = localY × baseOutputSize / panelHeightPx
W_ISAPI = W     × baseOutputSize / panelWidthPx
H_ISAPI = H     × baseOutputSize / panelHeightPx
```

**Cắt lát trước khi quy đổi** (cửa sổ toàn tường): giao hình chữ nhật cửa sổ với hình bao vùng
panel của khung `[originCol·P , (maxCol+1)·P] × [originRow·P_h , (maxRow+1)·P_h]`, lấy phần giao,
rồi mới `ToIsapiLocalRect`.

### Ví dụ đối chiếu (phải khớp `VwSceneRegionService.ToIsapiLocalRect`)

Cửa sổ **toàn tường 32 màn**, panel FHD `1920×1080`, `baseOutputSize = 1920`, khung **C2**
(`originCol=0, originRow=0`, vùng 4×4):

```
X_tuyệt_đối = 0, Y = 0, W = 8×1920 = 15360, H = 4×1080 = 4320
giao với vùng C2: [0, 4×1920] × [0, 4×1080] = 7680 × 4320
ToIsapiLocalRect(0,0, 0,0, 7680,4320, 1920,1080, 1920):
  localX = 0, localY = 0
  X_ISAPI = 0
  Y_ISAPI = 0
  W_ISAPI = 7680 × 1920 / 1920 = 7680
  H_ISAPI = 4320 × 1920 / 1080 = 7680
  → (0, 0, 7680, 7680)   # 4×4 ô vuông 1920 trong uniformCoordinate
```

> ⚠️ **Panel 4K hay FHD cho ra CÙNG `X_ISAPI/W_ISAPI`** *miễn là* `VwWindowScene.X/Y/W/H` được lưu
> cùng đơn vị với `panelWidthPx`. Rủi ro thật: FE (`wallConstants.ts`) và BE (`VwWallProfile.cs`)
> dùng **hai con số panel px khác nhau** → lệch 2×. Sơ đồ .jpg ghi FHD, code ghi 4K → **phải chốt**
> (KB-00 bước 5).

## P3. Bảng toạ độ 8×4 (tường tổng, đơn vị = panel px `P`×`P_h`)

| Ô | col | row | x (×P) | y (×P_h) | Khung |
|---|---|---|---|---|---|
| (1,1)…(4,1) | 0–3 | 0 | 0,1,2,3 | 0 | C2 |
| (5,1)…(6,1) | 4–5 | 0 | 4,5 | 0 | C3 |
| (7,1)…(8,1) | 6–7 | 0 | 6,7 | 0 | C4 |
| … hàng 2 (row=1), hàng 3 (row=2), hàng 4 (row=3) tương tự | | | | | |

Canvas tổng = `8P × 4P_h`. Canvas cục bộ: C2 = `4·bos × 4·bos`, C3 = `2·bos × 4·bos`,
C4 = `2·bos × 4·bos`.

## P4. Mã lỗi

`statusCode`: `0,1` = OK · `2` Busy · `3` Error · `4` Invalid Operation · `5` Invalid XML Format ·
`6` Invalid XML Content · `7` Reboot Required.

| Hiện tượng | `subStatusCode` | Nghĩa |
|---|---|---|
| GET vào endpoint chỉ nhận PUT | `methodNotAllowed` | URL đúng, sai method |
| PUT không body | `badXmlFormat` | body trống / có BOM / declaration utf-16 |
| PUT element rỗng / field chỉ-đọc | `badParameters` | gửi tối thiểu, đừng PUT lại response GET |
| ID không tồn tại / tường `unbound` | `invalidOperation` | lấy ID thật từ list |
| Firmware không hỗ trợ | `notSupport` | gọi `capabilities` trước |
| Client khác giữ tường | `multipleVideowallClientConflict` (`0x4000A4F8`) | đóng web UI / iVMS |
| Đang chuyển cảnh | `inSceneSwitchingPleaseDoNotOperate` (`0x4000A1AB`) | chờ 2–3s, verify `isRunning`, không retry activate |

Mã lỗi decode (`.../sub/<n>/start`): nhóm **nguồn vào** (`unstableInputSignal 0x4000A404`,
`inputResolutionIsNotSupported 0x4000A405`…), **đầu ra** (`outputModeMismatch 0x4000A40C`…),
**stream** (`streamingIsNotContinuous 0x4000A3FB`…), **phần cứng** (`inputChipException 0x4000A402`…).

## P5. Thứ tự test khuyến nghị

```
KB-00  probe 4 khung (chỉ GET, an toàn)              ← LÀM ĐẦU TIÊN, chốt 6 ẩn số
  ↓
KB-01 → KB-02   kết nối, lấy ID (mỗi khung)          (an toàn)
  ↓
KB-04           đọc bố cục hiện tại                  (an toàn)
  ↓
KB-03           gán lưới cục bộ từng khung           ⚠️ ghi — làm trên wall unbound trước
  ↓
KB-05 → KB-11   window + nguồn + decode (1 khung)    ⚠️ ghi
  ↓
KB-06           cửa sổ vắt nhiều khung (slicing)     ⚠️ ghi
  ↓
KB-07           move/resize/đổi nguồn/z-order        ⚠️ ghi
  ↓
KB-13 → KB-14   scene + activate đa khung            ⚠️ ghi — test lệch pha
  ↓
KB-16           poll 4 khung                         (an toàn)
  ↓
KB-12           dọn window                           ⚠️ ghi
  ↓
KB-18           genlock & mép ghép                   (quan sát vật lý)
  ↓
KB-17           tắt màn                              ⛔ CUỐI CÙNG — không có API bật lại
```

> ⛔ Đừng dùng `closeAll` hay `DELETE .../windows` để test kết nối — tắt/xóa cả tường thật.
> ✅ Test ghi an toàn: `GET` rồi `PUT` lại y nguyên tên 1 input (`/inputs/channels/<id>`) trên
> wall `unbound` của 1 khung.
