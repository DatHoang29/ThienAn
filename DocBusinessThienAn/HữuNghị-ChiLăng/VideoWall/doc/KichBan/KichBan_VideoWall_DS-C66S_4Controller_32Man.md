# Kịch bản test API Video Wall — DS-C66S cascade / 1 bộ trung tâm + 3 bộ con / 32 màn

> Cấu hình thật: **1 khung trung tâm (compositor) + 3 khung con (fan-out)**, tường **8 cột × 4 hàng
> = 32 màn**. Kiến trúc đầy đủ: [KienTruc_VideoWall_DS-C66S-Cascade.md](../KienTruc_VideoWall_DS-C66S-Cascade.md).
>
> 🔴 **Điểm mấu chốt:** backend chỉ nói ISAPI với **bộ trung tâm**. 3 bộ con **KHÔNG nhận lệnh API**
> trong luồng scene/window — chúng chỉ nhận tín hiệu 4K qua cáp HDMI, và được cấu hình lưới 2×2 tĩnh
> **một lần** lúc lắp (qua Web UI / ISAPI của **chính bộ con đó**). Ngoại lệ duy nhất: KB-17 (tắt màn
> qua cổng serial của bộ con).

Tất cả URL tương đối, ghép với `{{base}} = http://<ip_bộ_trung_tâm>:<port>`.
Auth: **Digest** (admin). Header `Content-Type: application/xml` (bỏ XML declaration, không BOM).
Thành công = `statusCode` **0 hoặc 1**. Lỗi thì đọc `subStatusCode` ([P4](#p4-mã-lỗi)).

> 📌 **Nguồn sự thật:** response mẫu lấy từ `../data/logs-api/session-20260904-real.json` (đo trực
> tiếp trên 1 khung DS-C66S bench).

> ⛔ **Bẫy khoá IP:** sai mật khẩu **2 lần liên tiếp** ⇒ khung khoá IP, không mở lại được qua LAN.
> Test Digest sai chỉ trên **mock server nội bộ**, không bao giờ trên thiết bị thật. Backend có
> circuit-breaker theo IP (`VwWallProfile.MaxConsecutiveFailures = 2`, `BlockMinutes = 30`).

### Công cụ chạy KB

- **`Module.VideoWall.WPF`** (chế độ **Direct** — gọi thẳng ISAPI, không qua backend): **dùng luôn,
  không cần sửa**. Cascade chỉ cần 1 IP nên WPF không thiếu gì. Chạy KB qua **tab ISAPI thô**
  (VideoWall / Scene / Window / SignalSource …), nhập **IP bộ trung tâm** + Digest, và **điền field
  `WallNo`** = wall `bound` từ KB-00 (code mặc định `wallNo ?? 1` — đừng để trống).
  ⚠️ *Tab 1 "Thiết lập Kịch bản & Bố cục"* còn ghim cứng lưới 4×3 / canvas `7680×5760`
  (`SceneSetupViewModel.cs:942`, `SceneWindowRow.cs:220`, `VisualWallCanvas.xaml.cs:22`) → chỉ tiện
  lợi, không đúng cho 8×4; muốn dùng thì sửa cho đọc lưới từ `GET .../outputs`. Tab ISAPI thô không bị.
- **`Module.VideoWall.WPF`** chế độ **qua backend** (`VideoWallApiClient.cs`): phần cascade nằm ở
  backend (đã fix), WPF chỉ gửi lệnh nghiệp vụ — không cần đổi.
- **Backend integration test** (`tests/Modules/VideoWall/`): dùng mock server cascade — xem prompt fix.

---

## 0. Bối cảnh & khác biệt so với bản 1 controller cũ

Bản cũ ([KichBan_VideoWall_DS-C30S-S11_12Man.md](KichBan_VideoWall_DS-C30S-S11_12Man.md)): 1 khung
DS-C30S-S11, 12 màn, lưới 4×3. Bản này: cascade DS-C66S, 32 màn, lưới 8×4.

**Luồng API KHÔNG đổi** — vẫn `connect → GET outputs → tạo scene (SID) → POST windows → PUT
scene/{SID}/activate` trên **1 IP**. Chỉ khác tham số:

| Bản 1 controller (DS-C30S) | Bản cascade (DS-C66S) |
|---|---|
| 1 IP standalone | 1 IP = **bộ trung tâm** |
| Lưới cứng 4×3, canvas ảo `7680×5760` | Lưới **8×4**, canvas ảo **`centerCanvas` suy từ `GET .../{wall}/outputs`** — KHÔNG hardcode |
| 1 `wallNo` | `ResolveWall` trên bộ trung tâm (1 khung C66S có 8 wall-logic `VideoWall1..8`); có thể ra **nhiều wall-logic** cùng khung — xem KB-14 |
| `Rect` cửa sổ = toạ độ ảo toàn tường | Quy đổi tỉ lệ toạ độ tường tuyệt đối → `centerCanvas` bằng **1 phép** ([P2](#p2-công-thức-tocenteruniformrect)) — **KHÔNG cắt lát cho từng khung** |
| Activate 1 SID | Activate **1 SID trên bộ trung tâm** |

### Bản đồ khung (từ sơ đồ — xác nhận bằng KB-00)

| Khung | `Code` | Vai trò | Vùng tường | Số màn | Backend gọi API? |
|---|---|---|---|---|---|
| Trung tâm | C1 | **compositor** — nhận nguồn, ghép layout, chia 8×4K | toàn tường | 32 (qua 8 cổng 4K OUT) | ✅ **có** |
| Con 1 | C2 | fan-out 4K→FHD (inventory) | cột 1–4, hàng 1–4 | 16 | ❌ không (trừ KB-17) |
| Con 2 | C3 | fan-out (inventory) | cột 5–6, hàng 1–4 | 8 | ❌ không (trừ KB-17) |
| Con 3 | C4 | fan-out (inventory) | cột 7–8, hàng 1–4 | 8 | ❌ không (trừ KB-17) |

Trong DB: C1 = `Role=center IntegrationMode=active`; C2/C3/C4 = `Role=sub IntegrationMode=inventory`.

### Chỉ mục

| KB | Mục đích |
|---|---|
| [KB-00](#kb-00-probe-read-only) | **Probe read-only — chốt ẩn số trước khi làm gì khác** (bắt buộc trên C1; C2/C3/C4 tuỳ chọn) |
| [KB-01](#kb-01-kết-nối--đọc-năng-lực) | Kết nối, đọc năng lực, lấy `videoWallID` bộ trung tâm |
| [KB-02](#kb-02-lấy-id-output--input) | Lấy outputID + inputID của bộ trung tâm |
| [KB-03](#kb-03-khai-lưới-toàn-tường-8×4-trên-bộ-trung-tâm) | Khai lưới toàn tường 8×4 (một lần) |
| [KB-04](#kb-04-đọc-bố-cục-hiện-tại) | Đọc bố cục hiện tại |
| [KB-05](#kb-05-mở-window-1-màn--gán-nguồn) | Mở window 1 màn + gán nguồn |
| [KB-06](#kb-06-cửa-sổ-vắt-nhiều-vùng) | **Cửa sổ vắt nhiều vùng = 1 window trên bộ trung tâm** (không slicing) |
| [KB-07](#kb-07-đổi-nguồn--di-chuyển--resize--z-order) | Đổi nguồn / move / resize / z-order |
| [KB-11](#kb-11-startstop-decoding) | Start / Stop decoding |
| [KB-12](#kb-12-xóa-window) | Xóa window |
| [KB-13](#kb-13-tạo-scene--saveData) | Tạo scene + `saveData` (1 SID trên bộ trung tâm) |
| [KB-14](#kb-14-active-scene) | **Active scene** (1 SID; nhiều wall-logic ⇒ lặp cùng IP) |
| [KB-16](#kb-16-poll-trạng-thái) | Poll trạng thái bộ trung tâm (+ đọc read-only bộ con) |
| [KB-17](#kb-17-tắt-màn-qua-serial) | Tắt màn qua serial — **con đường backend→bộ con duy nhất** |
| [KB-18](#kb-18-genlock--kiểm-mép-ghép) | **GENLOCK & kiểm mép ghép** (vật lý, không API) |

---

## KB-00. Probe read-only

**Chỉ `GET`. An toàn tuyệt đối. Chạy TRƯỚC mọi thứ khác.** Mục tiêu: chốt ẩn số ở
[mục 11 doc kiến trúc](../KienTruc_VideoWall_DS-C66S-Cascade.md#11-phải-đo--xác-nhận-tại-hiện-trường).

**Bắt buộc trên C1 (bộ trung tâm).** Chạy thêm trên C2/C3/C4 chỉ để kiểm kê + kiểm feed 4K sống —
không bắt buộc cho luồng backend.

| # | Method | URL | Đọc gì / chốt điều gì |
|---|---|---|---|
| 1 | `GET` | `/SDK/activateStatus` | `activated` — khung sống *(không cần auth)* |
| 2 | `GET` | `/ISAPI/Security/userCheck` | `statusValue=200`, `isActivated` — Digest OK |
| 3 | `GET` | `/ISAPI/System/deviceInfo` | `model`, `serialNumber`, `firmwareVersion` → **ẩn số 1: mã khung** (S12? chưa từng đo) |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall` | list `VideoWall1..N`, `wallBindOutputStatus` → **wall nào `bound`, có 1 hay nhiều** |
| 5 | `GET` | `/ISAPI/DisplayDev/VideoWall/<boundWall>/outputs` | `WallOutput[].Rect` (sắp theo `Coordinate`) → **`centerCanvas` = bounding box các Rect; số output; suy lưới bộ trung tâm** |
| 6 | `GET` | `/ISAPI/DisplayDev/VideoWall/capabilities` | `baseOutputSize` (bench = **1920**), `maxWallNums`, `maxWindowNums`, `isSupportScene/Roam/Plan` |
| 7 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | `id`, `portType`, `signalStatus` → **ẩn số 2: camera vào HDMI hay IP** + map `VwSource.SignalNo` |
| 8 | `GET` | `/ISAPI/DisplayDev/Video/streaming/channels` | có/không stream IP → xác nhận có card `DS-C66S-DEC` không |
| 9 | `GET` | `/ISAPI/DisplayDev/VideoWall/<boundWall>/scene` | list SID → **ẩn số 4: SID scheme** (số nguyên nhỏ đếm theo wall?) |
| 10 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | `id` + `PortInBoard` (boardID/portID) — đối chiếu công thức [P1](#p1-công-thức-id) |

### Response mẫu (đo thật, `../data/logs-api/session-20260904-real.json`)

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

Bước 4 — `VideoWall` (1 khung C66S trả **8 tường**):
```xml
<VideoWallList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <VideoWall><id>1</id><name>VideoWall1</name>...</VideoWall>
  ...
  <VideoWall><id>8</id><name>VideoWall8</name>...</VideoWall>
</VideoWallList>
```
> Trên bench: có wall `bound` (đang cắm màn) và wall `unbound` (sandbox). **Đừng mặc định `wallNo=1`**
> — đọc `wallBindOutputStatus` rồi chọn wall `bound`.

Bước 5 — `outputs` (bench, wall 1 chỉ 2×2 sandbox — tường thật sẽ khác):
```xml
<WallOutputList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <WallOutput><id>1</id><outputID>17235969</outputID>
    <Rect><Coordinate><x>1920</x><y>0</y></Coordinate><width>1920</width><height>1920</height></Rect></WallOutput>
  ... (4 output, mỗi Rect 1920×1920 ⇒ centerCanvas bench = 3840×3840)
</WallOutputList>
```

### Bảng nghiệm thu (điền tay tại hiện trường)

| Ẩn số | C1 (trung tâm) | C2 | C3 | C4 |
|---|---|---|---|---|
| `model` / serial | | | | |
| Wall `bound` (1 hay nhiều) | | *(chỉ kiểm kê)* | | |
| `centerCanvas` (W×H) từ `outputs` | | — | — | — |
| Số output | | — | — | — |
| `baseOutputSize` | | | | |
| Camera vào: HDMI / IP | | — | — | — |
| Có card DEC? | | — | — | — |
| SID list | | — | — | — |
| Feed 4K từ C1 có tín hiệu? | — | | | |

---

## KB-01. Kết nối & đọc năng lực

Chạy **trên bộ trung tâm**. Ghi lại:

```
VideoWallCap.baseOutputSize          ← dùng cho phép quy đổi toạ độ
VideoWallCap.maxWindowNums
VideoWallCap.isSupportScene / isSupportRoam / isSupportPlan
VideoWallCap.SceneCap.maxSceneNums / isSupportSceneInfo / isSupportSceneCopy
```

`GET /ISAPI/DisplayDev/VideoWall` → lấy **các `videoWallID` `bound`** của bộ trung tâm (KB-00 bước
4/5). Backend: `VwISAPIDeviceService.ResolveWall` cache 30′. Nếu **nhiều wall-logic `bound`** ⇒ backend
lặp tất cả trên **cùng IP** (KB-14).

---

## KB-02. Lấy ID output & input

Tất cả trên bộ trung tâm.

| # | Method | URL | Lấy gì |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | `id` output + `PortInBoard` + `outputPortAccessStatus` |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>/outputs` | `WallOutput[].outputID` + `Rect` → ô nào trong `centerCanvas` |
| 3 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | `id` input + `portType` (HDMI) + `signalStatus` → map `VwSource.SignalNo` (ITS + 20 camera) |

> 🔴 `id` **không phải 1..N**. Công thức [P1](#p1-công-thức-id): `id = T×16777216 + boardID×65536 +
> portID`, `T=0x01` cho video channel. Luôn đọc từ list endpoint.
>
> Bộ trung tâm có **8 cổng 4K OUT** (xuống 3 bộ con). Số output thấy ở `.../outputs` tuỳ cách firmware
> phơi (8 hay 32) — KB-00 bước 5 chốt. Cấu hình `outputs` của **bộ con** nằm ngoài phạm vi test này.

---

## KB-03. Khai lưới toàn tường 8×4 trên bộ trung tâm

🔴 **Khác bản cũ:** khai **1 lưới toàn tường 8×4** cho bộ trung tâm trong `uniformCoordinate`
(`centerCanvas` từ KB-00). **KHÔNG** khai "lưới cục bộ từng khung" — bộ con không nhận lệnh này.

```http
PUT {{base}}/ISAPI/DisplayDev/VideoWall/<wallNo>
```
```xml
<VideoWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>WALLNO</id>
  <name>Tuong Trung Tam</name>
  <WallOutputList>
    <!-- mỗi WallOutput: chỉ outputID (req) + Rect. Rect theo centerCanvas.
         Nếu firmware phơi 8 output (4 cột × 2 hàng quad): mỗi ô 2·bos × 2·bos.
         Nếu phơi 32 output (8×4 per-panel): mỗi ô bos × bos. -->
    <WallOutput><outputID>ID_1</outputID>
      <Rect><Coordinate><x>0</x><y>0</y></Coordinate><width>...</width><height>...</height></Rect></WallOutput>
    <!-- ... -->
  </WallOutputList>
</VideoWall>
```

> ⚠️ Chỉ gửi `outputID` + `Rect`. Gửi lại nguyên response GET (có field chỉ-đọc) ⇒ `badParameters`.
> `outputID` lấy từ KB-02 bước 2.
>
> **Bộ con:** người lắp đặt vào Web UI của **từng bộ con**, khai "mỗi cổng 4K in → lưới 2×2 → 4 cổng
> FHD out". Làm 1 lần, không đụng nữa. Không phải bước test API.

---

## KB-04. Đọc bố cục hiện tại

Trên bộ trung tâm.

| # | Method | URL | Lấy gì |
|---|---|---|---|
| 1 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>` | cấu hình tường |
| 2 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>/outputs` | output ở ô nào (`gridCol = Rect.x / bos`) |
| 3 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>/windows` | window đang mở + nguồn |
| 4 | `GET` | `/ISAPI/DisplayDev/VideoWall/<wallNo>/windows/status` | trạng thái decode |

---

## KB-05. Mở window 1 màn + gán nguồn

Toạ độ trong `centerCanvas`. Ví dụ đưa nguồn lên **1 màn** (0-indexed `col=1, row=1`), giả sử
`centerCanvas` là 8×4 ô `bos=1920`:

```http
POST {{base}}/ISAPI/DisplayDev/VideoWall/<wallNo>/windows
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
      <videoInputChannelID>ID_INPUT_C1</videoInputChannelID>
    </SubWindowParam></SubWindow>
  </SubWindowList>
</WallWindow>
```

> ⚠️ `signalMode` = `video input` (**có dấu cách**). `wndOperateMode` bắt buộc.
> ⚠️ `videoInputChannelID` là kênh input **của bộ trung tâm** (KB-02 bước 3) — camera/ITS cắm vào
> card 04HI của C1. Kênh IP stream (nếu không có card DEC) ⇒ `403 invalidOperation` (khớp log dòng 972).

---

## KB-06. Cửa sổ vắt nhiều vùng

🔴 **Khác bản cũ hoàn toàn.** Bản cũ (mô hình "4 khung fan-out") bảo cắt cửa sổ thành 3 mảnh gọi
`POST .../windows` 3 lần cho C2/C3/C4. **Cascade KHÔNG làm vậy.**

Cửa sổ ITS-MAP phủ **cột 2–7 × hàng 2–3** (12 màn giữa) = **1 window duy nhất trên bộ trung tâm**.
Firmware bộ trung tâm tự chia phần cửa sổ đó ra 8 cổng 4K OUT; 3 bộ con nhận quad của mình rồi fan-out
— **không bộ con nào "biết có cửa sổ ITS"**.

```
Toạ độ tường tuyệt đối (panel P×P_h, đánh số cột/hàng từ 0):
  x = 1·P, y = 1·P_h, w = 6·P, h = 2·P_h
  → panel 1920×1080:  (1920, 1080, 11520, 2160),  wallCanvas = 15360×4320
```

Quy đổi bằng **1 phép** [P2](#p2-công-thức-tocenteruniformrect) sang `centerCanvas`:

```
X' = x · centerCanvas.W / wallCanvas.W          (tương tự Y', W', H')
```

Ví dụ với `centerCanvas = 15360×7680` (giả định 8×4 ô 1920² — **đo thật ở KB-00**):
```
X' = 1920 · 15360/15360 = 1920
Y' = 1080 · 7680/4320   = 1920
W' = 11520 · 15360/15360 = 11520
H' = 2160 · 7680/4320   = 3840
→ POST .../windows MỘT lần, Rect (1920, 1920, 11520, 3840), videoInputChannelID = kênh ITS trên C1
```

> 🔴 `POST .../windows` **đúng 1 lần**. Không có 3 lần, không có `SliceWindowForControllers` trong
> luồng cascade. Cần `isSupportRoam = true` (đã xác nhận C66S).

---

## KB-07. Đổi nguồn / di chuyển / resize / z-order

Trên bộ trung tâm, toạ độ `centerCanvas`.

| Hành động | Method | URL |
|---|---|---|
| Đổi nguồn sub-window | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/sub/1` (fallback: PUT cả window) |
| Move / resize | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>` (`id` + `wndOperateMode` + `Rect`) |
| Đưa lên trên | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/top` |
| Xuống dưới | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/bottom` |

> ⚠️ PUT trả OK nhưng tường không đổi ⇒ có window khác `layerIdx` cao hơn đè. Dùng `/top` hoặc DELETE.
> Cửa sổ vắt nhiều vùng vẫn là **1 window** ⇒ `/top` **một lần** là đủ (không phải "từng khung").

---

## KB-11. Start/Stop decoding

Trên bộ trung tâm.

| Hành động | Method | URL |
|---|---|---|
| Start | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/sub/1/start` |
| Stop | `PUT` | `.../VideoWall/<wallNo>/windows/<VWMWID>/sub/1/stop` |
| Status 1 window | `GET` | `.../VideoWall/<wallNo>/windows/<VWMWID>/sub/1/status` |
| Status tất cả | `GET` | `.../VideoWall/<wallNo>/windows/status` |

Mã lỗi decode: [P4](#p4-mã-lỗi) (nhóm nguồn vào / đầu ra / stream / phần cứng).

---

## KB-12. Xóa window

Trên bộ trung tâm.

| Hành động | Method | URL |
|---|---|---|
| Xóa 1 window | `DELETE` | `.../VideoWall/<wallNo>/windows/<VWMWID>` |
| Xóa tất cả window | `DELETE` | `.../VideoWall/<wallNo>/windows` |

> `DELETE .../windows` không hoàn tác. Đây là bước đầu của `SyncSceneWindowsToDevice`. Nếu bộ trung
> tâm có **nhiều wall-logic** ⇒ DELETE trên từng `wallNo` (vẫn cùng IP).

---

## KB-13. Tạo scene & saveData

Trên **bộ trung tâm**, **1 SID** → lưu vào `VwScene.OutputId` (1 field là đủ, vì chỉ 1 khung tạo scene).

| # | Method | URL | Ghi chú |
|---|---|---|---|
| 1 | `GET` | `.../VideoWall/<wallNo>/scene/capabilities` | `maxSceneNums`, độ dài tên |
| 2 | `GET` | `.../VideoWall/<wallNo>/scene` | đếm SID đã có |
| 3 | `POST` | `.../VideoWall/<wallNo>/scene` | body `<WallScene><name>...</name></WallScene>` → trả `SID` mới |
| 4 | — | *dựng bố cục THẬT lên bộ trung tâm (KB-05/06/11)* | 🔴 `saveData` chụp tường ĐANG CHẠY |
| 5 | `PUT` | `.../VideoWall/<wallNo>/scene/<SID>` | đặt tên |
| 6 | `PUT` | `.../VideoWall/<wallNo>/scene/<SID>/saveData` | body placeholder `<Request .../>` — ⭐ chụp bố cục |

> `PUT .../scene/<SID>/saveData` trả **`403 invalidOperation` nếu SID chưa tồn tại** → backend fallback:
> `POST .../scene` tạo SID → retry `saveData` (khớp log dòng 328–356).
>
> `isSupportSceneCopy = false` → không "soạn nháp", phải dựng bố cục thật.
> `isSupportSaveSceneVirLed/BaseMap = false` → chữ chạy / ảnh nền KHÔNG lưu vào scene, phải áp lại
> sau mỗi activate.
>
> Nhiều wall-logic ⇒ tạo scene trên từng `wallNo` (cùng IP); nếu SID lệch giữa các wall-logic thì
> cần map `wallNo → SID` (ẩn số 4 KB-00).

---

## KB-14. Active scene

### Luồng backend (`VwISAPIDeviceService.ActivateScene`)

```
target = bộ trung tâm (VwController Role=center, IntegrationMode=active)  — DUY NHẤT
foreach wallNo in ResolveWalls(center):        // thường 1; >1 nếu firmware không gộp 8 output
    GET  .../VideoWall/capabilities   → isSupportScene? (không ⇒ bỏ qua)
    PUT  .../VideoWall/<wallNo>/scene/<SID>/activate   (không body)
```

### Test tay

| # | Method | URL | Mục đích |
|---|---|---|---|
| 1 | `GET` | `.../VideoWall/<wallNo>/scene/isRunning` | scene đang chạy; trùng SID ⇒ bỏ qua |
| 2 | `PUT` | `.../VideoWall/<wallNo>/scene/<SID>/activate` | ⭐ KÍCH HOẠT |
| 3 | — | *chờ 2–3 giây* | 🔴 chuyển cảnh không tức thời |
| 4 | `GET` | `.../VideoWall/<wallNo>/scene/isRunning` | verify `sceneID` = SID |
| 5 | `GET` | `.../VideoWall/<wallNo>/windows` | đọc bố cục mới |

### Lệch pha

- **1 wall-logic (thường gặp):** activate là 1 lệnh → OK hoặc fail, **không có "3/4"**.
- **Nhiều wall-logic trên bộ trung tâm:** các lệnh activate **không nguyên tử**. Nếu 1 wall-logic
  fail → backend phát `HardwareOutOfSync` (NATS) với `succeeded/failed` (đây là các **wall-logic của
  cùng 1 khung**, không phải 4 khung). Không retry mù.
- `inSceneSwitchingPleaseDoNotOperate` (`0x4000A1AB`) ⇒ **không gọi lệnh khác**, chờ rồi verify
  `isRunning`.
- `multipleVideowallClientConflict` (`0x4000A4F8`) ⇒ có client khác (web UI / iVMS) đang giữ bộ
  trung tâm → đóng rồi thử lại.

### Cách 2 — "activate" bằng dựng lại window (không dùng scene thiết bị)

`DELETE .../windows` → `POST .../windows` ×N (Rect `centerCanvas`) → `.../sub/1/start` ×N → `/top` ×N
theo z-order. Chậm hơn, không phụ thuộc SID. (Chính là `SyncSceneWindowsToDevice` khi `scene.OutputId`
rỗng.)

---

## KB-16. Poll trạng thái

**Bắt buộc — trên bộ trung tâm:**

| # | Method | URL | Tần suất |
|---|---|---|---|
| 1 | `GET` | `.../VideoWall/<wallNo>/windows/status` | 3–5s — trạng thái decode |
| 2 | `GET` | `/ISAPI/DisplayDev/Video/outputs/channels` | 5–10s — cáp màn (nhẹ) |
| 3 | `GET` | `/ISAPI/DisplayDev/Video/inputs/channels` | 5–10s — `signalStatus` nguồn |
| 4 | `GET` | `/ISAPI/DisplayDev/decoingDevice/status?format=json` | 30–60s — ⚠️ nặng, health phần cứng |

> 🔴 URL bước 4 viết là **`decoingDevice`** (thiếu `d`) — lỗi chính tả của hãng nhưng là URL thật.

**Tuỳ chọn — read-only trên C2/C3/C4** (cho sơ đồ topology): `GET .../capabilities` +
`Video/inputs/channels` → kiểm feed 4K từ bộ trung tâm còn tín hiệu không. Không lệnh ghi.
Backend có `VwController.Status` + `GenlockInConnected/GenlockOutConnected` để hiển thị.

---

## KB-17. Tắt màn qua serial

🔴 **Đây là con đường backend → bộ con DUY NHẤT.** Màn nối cổng RS-232/485 **của từng bộ con** (bộ
con lái cụm màn của nó). `closeAll` gửi lệnh tắt nguồn qua serial. Endpoint backend:
`VwISAPIDeviceClient.EndpointSerialTransData`
(`ISAPI/System/Serial/ports/{portId}/Transparent/channels/{channelId}/transData`).

Gọi tới **IP bộ con** phụ trách cụm màn cần tắt (C2 / C3 / C4):

| # | Method | URL |
|---|---|---|
| 1 | `GET` | `/ISAPI/System/Serial/capabilities` — kiểm `workMode` hỗ trợ `screenCtrl` |
| 2 | `PUT` | `.../Serial/ports/<portId>/Transparent/channels/<chId>/open` |
| 3 | `PUT` | `.../Serial/ports/<portId>/Transparent/channels/<chId>/transData` — payload lệnh tắt của hãng màn |
| 4 | `PUT` | `.../Serial/ports/<portId>/Transparent/channels/<chId>/close` |

> ⛔ **Không có API bật lại** (`ScreenCtrl` một chiều; `openScreen` chỉ có trong Plan, mà C66S
> `isSupportPlan = false`). Tắt xong bật bằng tay/remote.
> Chưa có dây serial ⇒ thay thế: `DELETE .../windows` (màn sáng, hiện nền) hoặc `wallBackMode=color`
> **trên bộ trung tâm**.

---

## KB-18. GENLOCK & kiểm mép ghép

Không phải API — kiểm tra vật lý + cấu hình.

1. Xác nhận chuỗi GENLOCK: **trung tâm LOOP → C2 IN → C2 LOOP → C3 IN → C3 LOOP → C4 IN**.
2. Đối chiếu DB: `VwController.GenlockInConnected / GenlockOutConnected` khớp thực tế từng khung.
3. Mở **1 cửa sổ video động** (camera có chuyển động) trên bộ trung tâm, vắt **ranh giới C2 | C3**
   (quanh cột 4–5). Quan sát đường ghép dọc:
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

Luôn đọc ID thật từ list endpoint **của bộ trung tâm**.

## P2. Công thức `ToCenterUniformRect`

Nguồn: `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Infrastructure/Services/Scene/VwSceneRegionService.cs`.

```
# wallCanvas   = (Cols·ScreenWidth, Rows·ScreenHeight)  từ VwWallTopology
# centerCanvas = bounding box các Rect trong GET .../{wall}/outputs  (KB-00 bước 5, cache 30′)
# KHÔNG hardcode. Tính bằng long để tránh cộng dồn làm tròn.

X' = (long)X · centerCanvas.W / wallCanvas.W
Y' = (long)Y · centerCanvas.H / wallCanvas.H
W' = (long)W · centerCanvas.W / wallCanvas.W
H' = (long)H · centerCanvas.H / wallCanvas.H
```

Guard: `W<=0 || H<=0` ⇒ bỏ qua. Sau quy đổi `W'==0 || H'==0` ⇒ ép `=1`.
`EnsureWindowInsideSceneRegionAsync`: chặn nếu cửa sổ ra ngoài `wallCanvas`.

### Ví dụ đối chiếu

Cửa sổ **toàn tường 32 màn**, panel `1920×1080` ⇒ `wallCanvas = 15360×4320`.
`centerCanvas = 15360×7680` (giả định 8×4 ô 1920² — đo thật KB-00):
```
X=0 Y=0 W=15360 H=4320
→ X'=0  Y'=0  W'=15360  H'=15360·7680... khoan: H' = 4320·7680/4320 = 7680
→ (0, 0, 15360, 7680)   # phủ trọn centerCanvas
```

Cửa sổ ITS-MAP (cột 2–7 × hàng 2–3), panel `1920×1080`:
```
X=1920 Y=1080 W=11520 H=2160
→ (1920, 1920, 11520, 3840)
```

> ⚠️ Con số tuyệt đối (1920 vs 3840) không quan trọng — miễn **`VwWallTopology` seed · `VwWallProfile.cs`
> · FE `wallConstants.ts`** dùng **CÙNG** đơn vị panel px. Sơ đồ .jpg ghi FHD 1920×1080; code hiện
> 3840×2160 → phải chốt 3 chỗ bằng nhau (khuyến nghị đổi cả 3 về 1920×1080).

## P3. Bảng toạ độ 8×4 (tường tổng, đơn vị = panel px `P × P_h`)

| Hàng (row) | Cột 0–3 (C2) | Cột 4–5 (C3) | Cột 6–7 (C4) |
|---|---|---|---|
| 0 | y = 0 | y = 0 | y = 0 |
| 1 | y = P_h | y = P_h | y = P_h |
| 2 | y = 2·P_h | y = 2·P_h | y = 2·P_h |
| 3 | y = 3·P_h | y = 3·P_h | y = 3·P_h |

`x = col · P`. `wallCanvas = 8P × 4P_h`. Cột C2/C3/C4 chỉ để **vẽ sơ đồ / seed `VwScreen.ControllerId`**
— backend không cắt theo mốc này.

## P4. Mã lỗi

`statusCode`: `0,1` = OK · `2` Busy · `3` Error · `4` Invalid Operation · `5` Invalid XML Format ·
`6` Invalid XML Content · `7` Reboot Required.

| Hiện tượng | `subStatusCode` | Nghĩa |
|---|---|---|
| GET vào endpoint chỉ nhận PUT | `methodNotAllowed` | URL đúng, sai method |
| PUT không body | `badXmlFormat` | body trống / có BOM / declaration utf-16 |
| PUT element rỗng / field chỉ-đọc | `badParameters` | gửi tối thiểu, đừng PUT lại response GET |
| ID không tồn tại / kênh sai / tường `unbound` | `invalidOperation` | lấy ID thật từ list |
| Firmware không hỗ trợ | `notSupport` | gọi `capabilities` trước |
| Client khác giữ tường | `multipleVideowallClientConflict` (`0x4000A4F8`) | đóng web UI / iVMS |
| Đang chuyển cảnh | `inSceneSwitchingPleaseDoNotOperate` (`0x4000A1AB`) | chờ 2–3s, verify `isRunning`, không retry activate |

Mã lỗi decode (`.../sub/<n>/start`): nhóm **nguồn vào** (`unstableInputSignal 0x4000A404`,
`inputResolutionIsNotSupported 0x4000A405`…), **đầu ra** (`outputModeMismatch 0x4000A40C`…),
**stream** (`streamingIsNotContinuous 0x4000A3FB`…), **phần cứng** (`inputChipException 0x4000A402`…).

## P5. Thứ tự test khuyến nghị

```
KB-00  probe (chỉ GET, an toàn)                     ← LÀM ĐẦU TIÊN, chốt ẩn số
  ↓
KB-01 → KB-02   kết nối, lấy ID bộ trung tâm        (an toàn)
  ↓
KB-04           đọc bố cục hiện tại                 (an toàn)
  ↓
KB-03           khai lưới toàn tường 8×4            ⚠️ ghi — làm trên wall unbound trước
  ↓
KB-05 → KB-11   window + nguồn + decode             ⚠️ ghi
  ↓
KB-06           cửa sổ vắt nhiều vùng (1 window)    ⚠️ ghi
  ↓
KB-07           move/resize/đổi nguồn/z-order       ⚠️ ghi
  ↓
KB-13 → KB-14   scene + activate                   ⚠️ ghi
  ↓
KB-16           poll                               (an toàn)
  ↓
KB-12           dọn window                         ⚠️ ghi
  ↓
KB-18           genlock & mép ghép                 (quan sát vật lý)
  ↓
KB-17           tắt màn (serial → bộ con)          ⛔ CUỐI CÙNG — không có API bật lại
```

> ⛔ Đừng dùng `closeAll` hay `DELETE .../windows` để test kết nối — tắt/xóa cả tường thật.
> ✅ Test ghi an toàn: `GET` rồi `PUT` lại y nguyên tên 1 input trên wall `unbound` của bộ trung tâm.
