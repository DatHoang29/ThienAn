# Hướng dẫn test UI WPF Video Wall — DS‑C30S‑S11 (12 màn)

> Bước bám **KB gốc** — [`KichBan_VideoWall_DS-C30S-S11_12Man.md`](KichBan_VideoWall_DS-C30S-S11_12Man.md) (chứa XML body mẫu + phụ lục P1–P7).
> Mỗi bước ghi **1 kết quả đúng** dùng chung cho MockServer và thiết bị thật; chỗ nào Mock khác → dòng `⚠️ Mock`.
> Ký hiệu: `T#` = số tab · `9.7.x.y` / `SYS-n` = mã preset trong cột "Danh sách API" của tab (chọn → form tự điền path + body mẫu).

---

## 1. Chuẩn bị

**1.1 Build & mở** — build xong chạy file `.exe` trong `bin\…\net10.0-windows`:

```powershell
dotnet build TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.WPF/Module.VideoWall.WPF.csproj
```

**1.2 MockServer** — chạy trần là đủ cho toàn bộ §4:

```powershell
dotnet run --project scripts/VwMockServerRunner
```

Header WPF: **IP** `127.0.0.1` · **Port** `18080` · **Account** `admin` · **Password** `Password123!` (mật khẩu nào cũng qua) · **WallNo** để trống (dropdown, mờ tới khi Probe).

**1.3 Thiết bị thật DS‑C30S‑S11**

- Nhập IP / Port (`80`/`8080`/`443`) / tài khoản thật. **Đóng web UI thiết bị + iVMS** trước khi chạy WPF (nếu không → `multipleVideowallClientConflict`). Chạy trong khung bảo trì.
- **Backup TRƯỚC:** web UI → *Maintenance → Export/Backup* → tải file cấu hình (lưới output, scene + bố cục, plan, virtualLED, ảnh nền, tên nguồn).
- **Restore SAU:** web UI → *Import* file backup → chờ áp (firmware có thể **reboot**) → `activate` lại scene gốc → **verify tường như cũ** rồi mới bàn giao trực ban.
- "Kết nối" trả 401 + `stale="FALSE"` = **sai mật khẩu → DỪNG NGAY** (thiết bị khoá IP).

**1.4 Kết nối & khảo sát**

- Header → **🔌 Kết nối** → 12 tab sáng lên.
- Header → **🔍 Khảo sát (Probe)**: WPF tự chạy `userCheck → capabilities → VideoWall list → {WallNo}/outputs → inputs/channels`.
- Probe **tự chọn tường đầu**; dropdown **WallNo** bật sau Probe — đổi tường trong dropdown → lưới + 4 thẻ metric **tự nạp lại**.
- ⚠️ Nút **🚀 Đẩy xuống thiết bị** (Tab 1) **xám tới khi Probe xong + đã có tường** — dù 12 tab đã sáng ngay sau Kết nối, **vẫn phải Probe trước** khi push ở Tab 1.

---

## 2. Keyword

### Sơ đồ — luồng tín hiệu (đọc từ trên xuống)

```
1. NGUỒN VÀO ──cáp video (HDMI/SDI) hoặc luồng RTSP──►  cắm vào  Bo INPUT
   (camera, PC dashboard, đầu ghi…)                              → mỗi cổng = 1 "input channel" (cổng VÀO)

2. Bên trong CONTROLLER DS-C30S-S11, tín hiệu chạy qua LẦN LƯỢT từng bo:
        Bo INPUT ──►  Bo GIẢI MÃ (ghép / cắt / scale) ──►  Bo OUTPUT

3. Bo OUTPUT ──cáp HDMI──►  12 MÀN HÌNH  (xếp lưới 4×3)
   → mỗi cổng = 1 "output channel" (cổng RA HDMI), 1 cổng ↔ 1 màn vật lý (×12)

4. PC chạy WPF (máy test) ──LAN / HTTP (ISAPI)──►  Bo QUẢN TRỊ (cổng LAN)
   → chỉ GỬI LỆNH qua mạng, KHÔNG cắm cổng video → KHÔNG phải "input"

▶ VIDEO WALL = lớp LOGIC đặt lên tầng OUTPUT (không phải bo mạch): gộp 12 output channel
  thành 1 mặt phẳng ảo 7680×5760 rồi vẽ nội dung lên đó —
     WallOutput[]     = ô lưới nào ↔ gán output channel nào                  (KB-03 dựng lưới)
     └ WallWindow[]   = cửa sổ chiếu 1 nguồn, phủ lên vùng nào của mặt phẳng (KB-05…12)
        └ SubWindow[] = ô con khi chia 1 cửa sổ thành 4 / 9 / 16             (KB-10)
```

### Thuật ngữ

| Từ | Nghĩa |
|---|---|
| **Endpoint box** | Ô text ở **đầu panel "Tham số API" (T2…T12)**, giữa badge **Method** (khoá theo preset) và nút **Gửi ISAPI**. Chọn preset → tự điền path; gõ đè để đổi. T1 không có. |
| **Gửi ISAPI** | Nút chạy lệnh. Kết quả ở khung **Response** + tab **Logs** (nhấp đúp xem full). |
| **`videoWallID` / WallNo** | ID 1 tường ghép. **Chọn ở dropdown WallNo** header (mờ tới khi Probe; Probe tự chọn tường đầu; đổi tường → lưới/metrics tự nạp lại). Mọi lệnh T6/T8/T12 ghép vào path `.../VideoWall/{WallNo}/...`. |
| **`VWMWID`** | ID 1 cửa sổ (WallWindow), kiểu `33554433`. Lấy từ `GET .../windows`. |
| **`VWSWID`** | ID 1 cửa sổ con. Cửa sổ chưa chia ô → luôn `1`. |
| **`SID`** / **`planTemplateID`** | ID 1 scene / 1 plan. |
| **`channelID`** | ID kênh vào/ra vật lý — số tổ hợp byte, **KHÔNG phải 1..N**. Luôn copy từ list. |
| **`Rect`** | `{Coordinate{x,y}, width, height}` — vùng trên mặt phẳng ảo. |
| **`wndOperateMode`** | `uniformCoordinate` = mỗi ô màn `1920×1920` (mặc định) · `resolutionCoordinate` = pixel thật (`ResolutionRect`). |
| **`baseOutputSize`** | Cạnh 1 ô = **1920**. Canvas = `cols×1920 × rows×1920` → 4×3 = `7680×5760`. |
| **`signalMode`** | Nguồn SubWindow. Camera/HDMI = `video input` (**có dấu cách**). |
| **`layerIdx`** | Z‑order — **số lớn nằm trên**. Chỉ‑đọc; đổi bằng `/top` `/bottom`. |
| **`windowMode`** | Số ô con trong 1 cửa sổ: `1` · `4` · `9` · `16`. |
| **`wallBindOutputStatus`** | `bound` = tường đang gán màn, **có thể đang chiếu thật** · `unbound` = trống, an toàn test ghi. |
| **`isSupport*`** | Cờ năng lực từ `.../capabilities`. `false` = firmware không hỗ trợ → bỏ nhóm đó. |
| **`saveData`** | **Chụp** bố cục tường **đang chạy** vào scene ⇒ phải dựng bố cục thật **trước** khi gọi. |
| **`activate`** | **Phát** 1 scene đã lưu lên tường. |
| **`isRunning`** | Đọc scene / plan đang chạy. |
| **`statusCode` / `subStatusCode`** | `statusCode` **0/1** = OK. Khác → `subStatusCode`: `invalidOperation` (ID sai / tường unbound) · `badParameters` (giá trị XML sai) · `notSupport` (firmware không có) · `badXmlFormat` (body rỗng/hỏng). |
| **🚀 Áp dụng & Đẩy xuống thiết bị** (T1) | **Chỉ bật khi đã Probe + đã có tường** (chưa thì xám). **Luôn ghi thật** (có hộp xác nhận, không có DryRun). Chạy chuỗi `UserCheck → capabilities → POST .../windows ×N → GET .../windows → PUT .../scene/{SID}/saveData`. Không tự xoá window cũ, không tự `activate`. |
| **⚡ Kích hoạt chiếu (Active)** (T1) | Chỉ gọi `PUT .../scene/{SID}/activate`. |

---

## 3. Edge case — import config KHÔNG phục hồi (làm tay sau restore)

| Việc | Vì sao backup không cứu | Xử lý |
|---|---|---|
| **KB‑17 `closeAll`** | Tắt nguồn màn qua RS‑232/485 — trạng thái phần cứng, không nằm trong config. ISAPI không có API bật màn (chỉ `openScreen` trong Plan). | Bật màn tay / remote. **Chạy KB‑17 CUỐI CÙNG.** |
| Scene đang chiếu | `activate` là hành động runtime, không nằm trong config tĩnh | `activate` lại scene gốc |
| Decoding từng window | Trạng thái runtime | `start` lại (KB‑11) |
| Giờ hệ thống (nếu đã đổi ở KB‑19) | Ngoài phạm vi backup | Chỉnh giờ lại tay |
| IP bị khoá (sai mật khẩu, 401 `stale="FALSE"`) | Khoá ở tầng auth, config không đụng tới | Chờ hết khoá / đổi IP nguồn |

---

## 4. Các bước test — KB‑01 … KB‑20

> Thứ tự chạy đề xuất (KB P7): `01 → 02 → 04 → 03 → 05…12 → 13/14/15 → 16 → 18/19/20 → 17`.

### KB‑01 — Kết nối & năng lực

- `Header · Kết nối` — chạy `userCheck`: đăng nhập Digest, kiểm tài khoản đúng & đủ quyền
- `Header · Probe` — chạy `capabilities` + `GET /ISAPI/DisplayDev/VideoWall` (danh sách tường) + `{wall}/outputs` + `inputs/channels`
- `T2 · SYS-1` — `GET SDK/activateStatus`: ping xem thiết bị sống & đã kích hoạt (activate) chưa
- `T2 · SYS-2` — `GET ISAPI/System/deviceInfo`: đọc model / số serial / phiên bản firmware của bộ điều khiển
- ✅ `userCheck` `statusValue=200`; Probe hiện 4 thẻ metric + tự chọn WallNo; `SYS-1` → `<Activated>true`; `SYS-2` → `model = DS-C30S-S11`.
- Ghi lại: `baseOutputSize` (1920), `maxWindowNums`, `maxSceneNums`, `isSupportScene/Roam/Plan`, `videoWallID` thật.
- ⚠️ Mock: 2 tường (`id=1` unbound, `id=2` bound) → Probe **tự chọn** `WallNo=1`; Outputs/Inputs = 2.

### KB‑02 — Lấy ID output / input

- `T4 · 9.7.3.4` — `GET .../Video/outputs/channels`: liệt kê 12 cổng HDMI ra màn — mỗi cổng có `id` thật (dùng ở KB‑03), độ phân giải, đã cắm màn hay chưa
- `T4 · 9.7.3.7` — `GET .../outputs/channels/{channelID}/capabilities`: 1 cổng ra đó **cho đặt** những độ phân giải / tần số quét nào (đọc trước khi định cấu hình cổng)
- `T5 · 9.7.4.8` — `GET .../Video/inputs/channels`: liệt kê các cổng vào (camera / PC) — mỗi cổng có `id`, tên, đang có tín hiệu hay không
- `T5 · 9.7.4.18` — `GET .../inputs/channels/{channelID}/picture`: chụp 1 tấm ảnh JPEG hiện tại của cổng vào đó (xem nhanh nguồn đang vào là gì)
- `T5 · 9.7.4.10` — `PUT .../inputs/channels/{channelID}`: đổi tên / thông số 1 cổng vào — **lệnh ghi nhẹ**, dùng để thử xác thực + quyền ghi mà không phá gì
- Xem nhanh: **T1 → sub‑tab "🖥️ Chi tiết Cổng ra & Kênh tín hiệu"**
- ✅ list trả đủ phần tử; **ghi lại `channelID` 12 output thật** (cần cho KB‑03); PUT tên → `0/1`
- ⚠️ Mock: chỉ 2 output / 2 input

### KB‑03 — Gán 12 output vào lưới 4×3

1. `T6 · 9.7.5.5` — `GET .../VideoWall/{videoWallID}/outputs`: xem lưới hiện tại — cổng ra nào đang ở ô nào, toạ độ nào
2. `T6 · 9.7.5.3` — `PUT .../VideoWall/{videoWallID}`: **dựng lưới tường** — khai báo cổng ra nào đặt ở ô nào (`Rect`); đây là bước tạo bố cục 4×3 (body `WallOutputList`, sample `VideoWallPut`, ô `1920×1920`; HW thật thay ID thật từ KB‑02)
3. `T6 · 9.7.5.5` — đọc lại để verify

- ✅ b2 PUT `ok`; b3 GET trả đủ **12 `WallOutput`** (`gridCol = Rect.x/1920`, `gridRow = Rect.y/1920`)
- ⚠️ Mock: GET vẫn trả 2 `WallOutput` (không dựng lưới thật)

### KB‑04 — Đọc bố cục hiện tại (chỉ GET, an toàn)

- `T6 · 9.7.5.4` — `GET .../VideoWall/{videoWallID}`: thông tin chung của tường — đang gán màn (`bound`) hay trống (`unbound`), tên, kích thước
- `T6 · 9.7.5.5` — `GET .../VideoWall/{videoWallID}/outputs`: bố cục lưới — ô nào ↔ cổng ra nào
- `T12 · 9.7.11.2` — `GET .../VideoWall/{videoWallID}/windows`: liệt kê các cửa sổ đang mở — mỗi cái có `VWMWID` (id), vùng phủ, lớp `layerIdx`, nguồn đang chiếu
- `T3 · 9.7.2.8` — `GET .../VideoWall/{videoWallID}/windows/status`: trạng thái chiếu của mọi cửa sổ — đang giải mã không, FPS, bitrate, mất tín hiệu không
- ✅ cả 4 trả XML đúng gốc, `statusCode 0/1`
- ⚠️ Mock: `9.7.11.2` trả danh sách window **tĩnh**

### KB‑05 — Mở 1 cửa sổ phủ 1 màn + gán nguồn

**Nhanh (Tab 1):** ➕ Tạo mới scene → sub‑tab "🖼️ Bố cục Ô Camera" → ➕ Thêm ô camera (hoặc **Mẫu bố cục**) → 💾 Lưu → **🚀 Đẩy xuống thiết bị**. *(cần Probe trước — chưa Probe thì nút 🚀 xám)*

**Thủ công:**
1. `T12 · 9.7.11.14` — `GET .../windows/capabilities`: tường **cho phép gì** với cửa sổ — chia được mấy ô (4/9/16), kéo cửa sổ qua nhiều màn được không (`isSupportRoam`), đưa lên/xuống lớp được không
2. `T12 · 9.7.11.4` — `POST .../windows`: **mở 1 cửa sổ mới** — chọn vùng phủ (`Rect`) + nguồn (`videoInputChannelID`); trả về `VWMWID` của cửa sổ vừa tạo (sample `WallWindow`, `Rect` `x=col·1920 y=row·1920`)
3. `T12 · 9.7.11.2` — `GET .../windows`: đọc lại để lấy `VWMWID` mới
4. `T3 · 9.7.2.5` — `PUT .../windows/{VWMWID}/sub/{VWSWID}/start`: ra lệnh cửa sổ **bắt đầu chiếu nguồn** (bật giải mã) — không có bước này thì cửa sổ mở ra nhưng đen (không body)
5. `T3 · 9.7.2.6` — `GET .../windows/{VWMWID}/sub/{VWSWID}/status`: kiểm cửa sổ đã chiếu được chưa — `isDecoding`, FPS, độ phân giải ảnh vào

- ✅ b2 trả `ok` + `<ID>`; b5 → `<isDecoding>true`
- ⚠️ Mock: `VWMWID` không đổi theo POST
- Lỗi HW thật: `multipleVideowallClientConflict` (đóng web UI) · `invalidOperation` (nguồn sai / tường unbound) · `badParameters` (thiếu `wndOperateMode`)

### KB‑06 — Cửa sổ phủ nhiều màn

Như KB‑05, **chỉ đổi `Rect`** (cần `isSupportRoam=true`): khối 2×2 = `3840×3840`; toàn tường = `7680×5760`.

### KB‑07 — Đổi nguồn 1 cửa sổ

1. `T12 · 9.7.11.18` — `PUT .../windows/{VWMWID}/sub/{VWSWID}`: đổi nguồn đang chiếu trong 1 cửa sổ sang camera / PC khác — chỉ sửa `videoInputChannelID`, không đụng vị trí (sample `SubWindowSource`)
2. `T3 · 9.7.2.6` — `GET .../sub/{VWSWID}/status`: verify nguồn mới

- Lỗi `badXmlFormat` → dùng `T12 · 9.7.11.6` — `PUT .../windows/{VWMWID}`: ghi đè toàn bộ thông số cửa sổ (vị trí + nguồn + chia ô)
- ✅ trả `ok`; `status` cho thấy `videoInputChannelID` mới

### KB‑08 — Di chuyển / resize

1. `T12 · 9.7.11.6` — `PUT .../windows/{VWMWID}`: **di chuyển / phóng to‑thu nhỏ** cửa sổ — gửi `Rect` (x, y, rộng, cao) mới
2. `T12 · 9.7.11.5` — `GET .../windows/{VWMWID}`: đọc lại 1 cửa sổ xem `Rect` đã đổi đúng chưa

- Pixel thật: `wndOperateMode=resolutionCoordinate` + `ResolutionRect`
- ✅ GET trả `Rect` mới. PUT `ok` mà tường không đổi → cửa sổ khác `layerIdx` cao hơn đè → KB‑09
- ⚠️ Mock: GET vẫn dữ liệu tĩnh (không thấy `Rect` mới)

### KB‑09 — Z‑order

1. `T12 · 9.7.11.14` — `GET .../windows/capabilities`: kiểm tường có cho đổi lớp cửa sổ không (`isSupportWinTopBottom`)
2. `T12 · 9.7.11.13` — `PUT .../windows/{VWMWID}/top`: **đưa cửa sổ này lên trên cùng** (che các cửa sổ khác)
3. `T12 · 9.7.11.8` — `PUT .../windows/{VWMWID}/bottom`: **đẩy cửa sổ này xuống dưới cùng**
4. `T12 · 9.7.11.2` — `GET .../windows`: đọc lại xem `layerIdx` đã đổi chưa

- Chỉ có `/top` và `/bottom` — không đặt số lớp tuỳ ý
- ⚠️ Mock: `top`/`bottom` trả `ok` nhưng `layerIdx` **không đổi**

### KB‑10 — Chia 1 cửa sổ thành 4 / 9 / 16 ô

1. `T12 · 9.7.11.6` — `PUT .../windows/{VWMWID}`: **chia 1 cửa sổ thành lưới con** — `windowMode=4/9/16` + khai báo từng ô con (`SubWindow`) + nguồn của mỗi ô
2. `T3 · 9.7.2.5` — `PUT .../sub/{VWSWID}/start`: bật chiếu cho từng ô con (`VWSWID` = 1..4)

- `windowMode` hợp lệ: `1, 4, 9, 16`. Phóng to 1 ô: `wndShowMode=fullScreenMode` + `amplifyingSubWndNo` (GET không trả lại field này)
- ⚠️ Mock: không dựng sub‑window thật

### KB‑11 — Start / Stop decoding

- `T3 · 9.7.2.5` — `PUT .../sub/{VWSWID}/start`: **bật chiếu** 1 ô cửa sổ (bắt đầu giải mã nguồn)
- `T3 · 9.7.2.7` — `PUT .../sub/{VWSWID}/stop`: **tắt chiếu** 1 ô (cửa sổ vẫn còn, chỉ thành đen)
- `T3 · 9.7.2.6` — `GET .../sub/{VWSWID}/status`: xem 1 cửa sổ đang chiếu ra sao (FPS, bitrate, lỗi giải mã)
- `T3 · 9.7.2.8` — `GET .../windows/status`: xem **tất cả** cửa sổ đang chiếu ra sao
- Không body — nếu `badXmlFormat` gửi placeholder `<Request xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0"></Request>`
- ✅ status trả `<WallWindowStatusList>` có `isDecoding`, `videoFPS`, `streamRate`
- ⚠️ Mock: giá trị **cố định**; mã lỗi decode khi nguồn hỏng (P5) chỉ có ở HW thật

### KB‑12 — Xoá cửa sổ

- `T12 · 9.7.11.7` — `DELETE .../windows/{VWMWID}`: **đóng 1 cửa sổ** (gỡ khỏi tường)
- `T12 · 9.7.11.3` — `DELETE .../windows`: **đóng hết mọi cửa sổ** trên tường (không hoàn tác — cũng là cách "làm sạch tường" trước khi test lại)
- ⚠️ Mock: `GET .../windows` không phản ánh xoá

### KB‑13 — Tạo scene & lưu bố cục

1. `T8 · 9.7.7.5` — `GET .../scene/capabilities`: hỏi lưu được tối đa mấy scene (`maxSceneNums`), có hỗ trợ export/import không
2. `T8 · 9.7.7.1` — `GET .../scene`: liệt kê các scene đã lưu (`id` + tên)
3. `T8 · 9.7.7.X1` — `POST .../scene`: **tạo 1 scene rỗng** — nhận `SID` mới (chưa có bố cục; bố cục lưu sau bằng `saveData`)
4. **Dựng bố cục THẬT lên tường** — chạy KB‑05…KB‑10 (hoặc 🚀 ở Tab 1)
5. `T8 · 9.7.7.2` — `PUT .../scene/{SID}`: đổi tên scene
6. `T8 · 9.7.7.4` — `PUT .../scene/{SID}/saveData`: **chụp nguyên trạng tường đang chiếu** (mọi cửa sổ + nguồn + vị trí) và cất vào scene `SID` (body placeholder)

- ✅ b3 trả `<ID>`; b6 trả `ok`
- Lưu ý: `saveData` chụp **tường đang chạy** — không có "soạn nháp"; `GET .../scene` chỉ trả `id` + `name`

### KB‑14 — Phát scene lên tường (chạy đủ Mock + HW)

1. `T8 · 9.7.7.6` — `GET .../scene/isRunning`: hỏi tường **đang chiếu scene nào** (`sceneID`), hay không có
2. `T8 · 9.7.7.1` — `GET .../scene`: xác nhận `SID` cần phát còn tồn tại
3. `T8 · 9.7.7.3` — `PUT .../scene/{SID}/activate`: **phát scene `SID` lên tường** — thiết bị tự dựng lại toàn bộ cửa sổ theo scene đã lưu (**không body, không Content‑Type**; hoặc nút **⚡ Kích hoạt chiếu** T1)
4. chờ 2–3 giây
5. `T8 · 9.7.7.6` — `GET .../scene/isRunning`: verify `sceneID` = SID vừa phát

- ✅ b5 trả đúng SID
- HW thật: gặp `inSceneSwitchingPleaseDoNotOperate (0x4000A1AB)` → **không gọi lệnh nào khác**, chờ, verify bằng `isRunning`, **không retry activate**

### KB‑15 — Quản lý scene

| Việc | Preset (T8) — API |
|---|---|
| Liệt kê / đổi tên | `9.7.7.1` `GET .../scene` (liệt kê scene) · `9.7.7.2` `PUT .../scene/{SID}` (đổi tên) |
| Xoá 1 / xoá tất cả | `9.7.7.X2` `DELETE .../scene/{SID}` · `9.7.7.X3` `DELETE .../scene` |
| Xem chi tiết 1 scene | `9.7.7.X4` `GET .../scene/{SID}/sceneInfo?format=json` (JSON — các cửa sổ trong scene) |
| Backup / khôi phục / nhân bản | `9.7.7.X5` `GET .../scene/export?format=json` (tải toàn bộ scene về file) · `9.7.7.X6` `POST .../scene/import?format=json` (nạp lại từ file) · `9.7.7.X7` `PUT .../scene/{SID}/copy` (tạo bản sao) |
| Hiệu ứng chuyển cảnh | `9.7.7.8` `GET .../VideoWallScene/SceneControlParams` (đọc) · `9.7.7.9` `PUT …` (đặt thời gian mờ dần khi đổi scene…) |

- ✅ mỗi lệnh trả `ok`; sau xoá/tạo, `9.7.7.1` phản ánh danh sách mới
- ⚠️ Mock: scene chỉ `{id,name}` trong RAM; `sceneInfo` trả JSON tối giản

### KB‑16 — Poll trạng thái (chạy tốt trên Mock)

| Preset — API | Nhịp | Đọc gì |
|---|---|---|
| `T3 · 9.7.2.8` `GET .../windows/status` | 3–5s | mọi cửa sổ đang chiếu ra sao (giải mã, FPS, mất tín hiệu) |
| `T4 · 9.7.3.4` `GET .../Video/outputs/channels` | 5–10s | 12 cổng ra: `outputPortAccessStatus` = còn cắm màn không |
| `T5 · 9.7.4.8` `GET .../Video/inputs/channels` | 5–10s | các cổng vào: `signalStatus` = còn tín hiệu không |
| `T3 · 9.7.2.1` `GET .../decoingDevice/status?format=json` (thiếu chữ `d` là **đúng**) | 30–60s | sức khoẻ phần cứng: nhiệt độ backplane, `runTime` (giờ chạy) từng bo mạch |
| `T2 · 9.7.1.4` `GET .../System/Board/status/capabilities` | on‑demand | danh mục các chỉ số sức khoẻ mà từng bo mạch báo được |

- ✅ tất cả trả XML/JSON đúng gốc. `runTime` 1 board **giảm đột ngột** = board vừa reboot

### KB‑17 — Tắt màn hình ⛔ CHẠY CUỐI CÙNG (không có API bật lại)

- `T9 · 9.7.8.1` — `PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll`: **gửi lệnh tắt nguồn màn hình** qua cổng serial — body `<OutputID>` (1 màn) / `<VideoWallID>` (cả tường) / rỗng (tất cả)
- `T2 · SYS-4` — `GET .../System/Serial/ports`: liệt kê các cổng RS‑232/485 trên thiết bị
- `T2 · SYS-5` — `GET .../System/Serial/ports/capabilities`: 1 cổng serial hỗ trợ baud rate / chế độ (`workMode`) nào
- `T2 · SYS-6` — `GET .../System/Serial/capabilities`: có mấy cổng serial, mỗi cổng làm được gì (điều khiển màn / trung tính…)
- ✅ HW thật: `ok` **chỉ khi** có dây RS‑232/485 + cổng serial `workMode=screenCtrl` + đúng protocol màn. Chưa đủ → `invalidOperation`
- ⚠️ Mock: mặc định `invalidOperation` (đúng); thử nhánh `ok` → runner `--closeall-ok`

### KB‑18 — Crop nguồn

1. `T5 · 9.7.4.17` — `GET .../inputs/channels/{channelID}/cutOff/capabilities`: nguồn này có cho **cắt viền ảnh** không, cắt tối đa bao nhiêu pixel mỗi cạnh
2. `T5 · 9.7.4.15` — `GET .../inputs/channels/{channelID}/cutOff`: đang cắt viền bao nhiêu (trái / phải / trên / dưới)
3. `T5 · 9.7.4.16` — `PUT .../inputs/channels/{channelID}/cutOff`: **đặt số pixel cắt mỗi cạnh** (trái/phải/trên/dưới), miền `[0,30]`

- Ghép nhiều cổng vào thành 1 nguồn lớn (vd 4 camera → 1 khung 2×2): `T5 · 9.7.4.24` `GET .../inputs/joinSignal` (xem cấu hình ghép hiện tại) · `9.7.4.27` `GET …/joinSignal/capabilities` (ghép được kiểu nào) · `9.7.4.25` `PUT …/joinSignal/{channelID}` (đặt cách ghép cho 1 cổng)

### KB‑19 — Plan (lịch tự động)

1. `T2 · SYS-3` — `GET ISAPI/System/time`: đọc / đặt đồng hồ thiết bị 🔴 đồng bộ trước (lệch giờ → plan chạy sai giờ)
2. `T7 · 9.7.6.3` — `GET .../plan/capabilities`: tạo được tối đa mấy plan (`maxPlanNums`), hỗ trợ loại hành động nào
3. `T7 · 9.7.6.1` — `POST .../plan`: **tạo 1 lịch tự động** — giờ nào làm gì (phát scene / tắt màn / bật màn) — nhận `planTemplateID` (body `WallPlan` từ KB‑19)
4. `T7 · 9.7.6.2` — `GET .../plan/{planTemplateID}/capabilities`: 1 plan đó cho khai báo mấy mốc thời gian, hành động nào
5. `T7 · 9.7.6.4` — `GET .../plan/isRunning`: plan nào đang chạy (`planID`), hay không có
6. `T7 · 9.7.6.X1` `GET .../plan` (liệt kê) · `X2` `PUT .../plan/{id}` (sửa) · `X3` `DELETE .../plan/{id}` (xoá) · `X4` `PUT .../plan/{id}/start` (cho chạy) · `X5` `PUT .../plan/{id}/stop` (dừng)

- `operationType`: `activateScene · closeScreen · openScreen · switchBaseMap`. **`openScreen` chỉ có trong Plan** = cách duy nhất "bật màn" qua API
- ✅ POST trả `<ID>`; `start` → `isRunning` báo `planID`; `stop` → hết chạy

### KB‑20 — Virtual LED / Wallpaper

**Virtual LED (T10)** — dòng chữ chạy đè lên tường:
- `9.7.9.8` `GET .../virtualLED/capabilities` (tạo được mấy dòng, font / màu / tốc độ nào) · `9.7.9.7` `GET .../virtualLED/{SubtitlesID}/capabilities` (giới hạn của 1 dòng)
- `9.7.9.2` `GET .../virtualLED` (đọc mọi dòng chữ chạy đang có) · `9.7.9.4` `GET .../virtualLED/{SubtitlesID}` (đọc 1 dòng)
- `9.7.9.3` `POST .../virtualLED` (thêm dòng mới) · `9.7.9.1` `PUT .../virtualLED` (ghi đè toàn bộ danh sách) · `9.7.9.6` `PUT .../virtualLED/{SubtitlesID}` (sửa 1 dòng) · `9.7.9.5` `DELETE .../virtualLED/{SubtitlesID}` (xoá 1 dòng)

**Wallpaper (T11)** — ảnh nền tường:
- `9.7.10.7` `GET .../baseMap/capabilities` (định dạng / kích thước ảnh nền cho phép)
- `9.7.10.8` `GET .../baseMap?isGetBaseMapFile=…` (đọc các ảnh nền đang có) · `9.7.10.6` `GET .../baseMap/{mapFileID}` (đọc 1 ảnh nền)
- `9.7.10.3` `PUT .../baseMap` (đặt / cập nhật danh sách ảnh nền) · `9.7.10.5` `PUT .../baseMap/{mapFileID}` (sửa 1 ảnh nền) · `9.7.10.4` `DELETE .../baseMap/{mapFileID}` (xoá 1 ảnh nền)
- ✅ tất cả trả đúng gốc
- Lưu ý: `isSupportSaveSceneVirLed / BaseMap = false` → PUT lại chữ chạy / ảnh nền **sau mỗi lần** `activate` scene

---

## 5. Chỉ kiểm chứng đủ trên thiết bị thật

| Nội dung | Vì sao Mock không đủ |
|---|---|
| KB‑02/03 — đủ **12 output**, lưới 4×3 đúng vị trí | Mock chỉ 2 cổng |
| KB‑04/05/08/09/12 — verify **có trạng thái**: số window đổi sau POST/DELETE, `layerIdx` đổi sau `/top` `/bottom`, `Rect` mới sau resize | `GET .../windows` của Mock là **tĩnh** |
| KB‑11 — giá trị decode thật + **mã lỗi decode** khi nguồn hỏng (P5) | Mock trả số cố định |
| KB‑13/14 — bố cục scene thật được `saveData` chụp & `activate` phát lên màn; độ trễ chuyển cảnh | Mock chỉ lưu `{id,name}` |
| KB‑17 — `closeAll` làm màn **thực sự tắt** | `--closeall-ok` chỉ giả `statusCode`, không chứng minh đấu nối serial |
| Nhánh lỗi (`stale` / khoá IP / `saveData` lỗi / `notConnected`) | Mock giả được đường đi (cờ mục 6), không giả được phản ứng thiết bị thật |

---

## 6. Cờ MockServer — chỉ khi muốn ép Mock trả lỗi

> **Bỏ qua mục này nếu chỉ chạy happy‑path.** Mặc định (chạy runner không cờ) Mock trả **thành công cho mọi lệnh** — đủ cho toàn bộ §4. Chỉ thêm cờ khi muốn **cố tình bắt Mock trả lỗi** để tập cách xử lý: mất kết nối, sai mật khẩu khoá IP, `saveData` lỗi, màn chưa cắm, `closeAll` chưa đấu serial…

Đặt sau lệnh runner, vd `dotnet run --project scripts/VwMockServerRunner --closeall-ok --lockout=3` (hoặc biến môi trường `VWMOCK_<TÊN_CỜ_HOA_GẠCH_DƯỚI>=1`):

| Cờ | Tác dụng | Test KB |
|---|---|---|
| `--closeall-ok` | `closeAll` trả `ok` thay vì `invalidOperation` | KB‑17 nhánh thành công |
| `--nonce-expiry` | 1 lần trả `stale="true"` giữa chừng | KB‑01 nonce hết hạn |
| `--lockout=N` | Sai auth N lần liên tiếp → khoá IP | xử lý khoá IP |
| `--savedata-fail` | `saveData` trả `invalidOperation` | KB‑13 nhánh lỗi |
| `--not-connected=17235971,17235972` | các output đó báo `notConnected` | KB‑02/16 màn chưa cắm |
| `--no-bound-wall` / `--multi-bound-wall` | ép trạng thái `bound`/`unbound` của tường | KB‑01/05 |
| `--unreachable` | thiết bị không phản hồi | xử lý mất kết nối |
| `--max-scene=N` | hạ `maxSceneNums` | KB‑13 chặn SID vượt dải |
