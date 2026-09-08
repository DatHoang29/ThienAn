# LogsAPI — Log đo thiết bị VideoWall (dữ liệu đối chiếu)

> **Đây là nguồn sự thật hạng 1** cho mọi việc liên quan API VideoWall. Khi tài liệu hãng
> (`../../doc/ISAPI-Videowall-Controller/09-api-reference.md`) mâu thuẫn với log, **log thắng**.

Log sinh ra từ công cụ `Module.VideoWall.WPF` chạy chế độ Direct (gọi thẳng thiết bị qua ISAPI + Digest Auth), không đi qua backend.

---

## File dùng để đối chiếu

| File | Ngày | Bản ghi | Nội dung |
|---|---|---:|---|
| `session-20260903-real.json` | 2026-09-03 | **955** | Gộp `session_20260903.json` (phần thiết bị thật) + `session_20260903_v2.json` |
| `session-20260904-real.json` | 2026-09-04 | **1158** | Gộp `session_20260904.json` + `session_20260904_v2.json` |
| | | **2113** | Tổng |

Bản ghi đã **sắp theo `Time` tăng dần**. Mỗi phần tử là một lệnh ISAPI logic với các trường:
`Time`, `Stage`, `Level`, `Detail`, `Name`, `Method`, `Endpoint`, `HttpStatus`, `Success`, `Message`, `RequestPayload`, `ResponsePayload`.

> ⚠️ Mỗi bản ghi là **một lệnh logic**, không phải một request HTTP. Digest cần 2 chặng
> (401 challenge → gửi lại kèm `Authorization`) nên số request HTTP thực tế ≈ **2 × số bản ghi**.
> Chặng 401 không được ghi vào log.

---

## 🚫 Đã loại 86 bản ghi mock server

`session_20260903.json` gốc chứa **hai** nguồn. Người vận hành đổi IP đích lúc `2026-09-03 14:04:46`; mọi chỉ dấu phần cứng đổi đồng thời tại đúng mốc đó.

- **07:57 → 11:53 ngày 03/09 — 86 bản ghi — MOCK SERVER ⇒ ĐÃ LOẠI.**
- **Từ 14:04:46 trở đi — thiết bị thật ⇒ giữ.**

### Cách nhận diện mock (để không lấy lại lần sau)

| Dấu hiệu của MOCK | Thiết bị THẬT |
|---|---|
| Tường tên **`HoangNhu`**, chỉ có 2 tường | 8 tường `VideoWall1`…`VideoWall8` |
| Trả trường **`wallBindOutputStatus`** | **0/2113** bản ghi có trường này |
| `capabilities` chỉ **318 byte / 5 trường**, `maxSceneNums` là con trực tiếp | **3469 byte / 54 tag**, `maxSceneNums` nằm trong `<SceneCap>` |
| `<WallOutput>` không attribute, có thêm `outputWinNum` + `coordinateMode` | có `version="2.0" xmlns="…"`, không có 2 trường đó |
| Wall 1 có **12** cổng ra (lưới 4×3) | Wall 1 có **4** cổng ra (lưới 2×2) |
| Kênh vào: 12 cổng board 1 (`16842753`…`16842764`) | Kênh vào: **2** cổng HDMI board 2 (`16908289`, `16908290`) |

Mock được dựng theo tài liệu `../../doc/KichBan/KichBan_VideoWall_DS-C30S-S11_12Man.md` (09B đã bị loại), nên nó trả **con số trong tài liệu**, không phải số đo thiết bị. Hai lệnh kiểm nhanh:

```powershell
# Phải trả về 0 nếu file sạch mock
$j = Get-Content session-20260903-real.json -Raw | ConvertFrom-Json
($j | Where-Object { $_.ResponsePayload -match 'HoangNhu|wallBindOutputStatus' }).Count
```

---

## Cấu hình thiết bị thật đo được

| Hạng mục | Giá trị |
|---|---|
| Số tường | **8** (`VideoWall1`…`VideoWall8`) |
| `capabilities` | `maxWallNums=8`, `maxWindowNums=512`, `baseOutputSize=1920`, `maxSceneNums=128`, `isSupportScene=true`, `isSupportSceneInfo=true`, `isSupportRoam=true`, `isSupportPlan=false`, `videoWallName max=64` |
| Wall 1 | **4 cổng ra**, `outputID` 17235969…17235972, lưới **2 cột × 2 hàng**, không gian ảo 3840×3840 ⇒ **bound** |
| Wall 4, 7 | `WallOutputList` **rỗng** ⇒ **unbound**; mọi lệnh ghi cửa sổ trả `403 invalidOperation` |
| Wall 2, 3, 5, 6, 8 | **chưa từng đọc `outputs`** |
| Kênh vào | 2 cổng HDMI board 2: `16908289` (`Input 2-1`, `signal`), `16908290` (`Input 2-2`, `noSignal`). `imageWidth`/`imageHeight`/`FPS` luôn `0` |
| Kênh stream IP | **48 kênh, id 1…48**, nhóm `Lane` + `Plate`. **Chứa `EncodeDevInfo` với username/password đầu ghi — không log, không phơi ra API** |
| Công thức ID | `channelID = 0x01000000 + boardID × 0x10000 + portID` (kiểm bằng `<PortInBoard>`) |

### 21 endpoint đã đo

`Security/userCheck` · `DisplayDev/VideoWall/capabilities` · `DisplayDev/VideoWall` · `{wallNo}/outputs` · `Video/inputs/channels` · `Video/streaming/channels` · `Audio/outputs/channels` · `{wallNo}/scene` (GET/POST) · `{wallNo}/scene/isRunning` · `{wallNo}/scene/{sid}` (PUT) · `{sid}/sceneInfo?format=json` · `{sid}/saveData` · `{sid}/activate` · `{wallNo}/windows` (GET/POST/DELETE) · `{wallNo}/windows/{id}` (GET/PUT) · `{id}/sub/{n}/start`

**Chưa từng gọi:** `Video/outputs/channels`, `Security/capabilities`.

---

## Lịch sử gom file

Thư mục này thay thế hoàn toàn `c:\ThienAn\logs\` (đã xoá). Bốn file gốc đã được gộp theo ngày rồi **xoá bản rời**, chỉ giữ hai file kết quả:

| File gốc (đã xoá) | Bản ghi thật | Gộp vào |
|---|---:|---|
| `session_20260903.json` | 333 (loại 86 bản mock) | `session-20260903-real.json` |
| `session_20260903_v2.json` | 622 | `session-20260903-real.json` |
| `session_20260904.json` | 545 | `session-20260904-real.json` |
| `session_20260904_v2.json` | 613 | `session-20260904-real.json` |

Đã xác minh sau khi gộp: **2113 bản ghi**, **21 endpoint** duy nhất, **149 bản ghi thất bại**, **0 dấu vết mock**, khoảng thời gian `2026-09-03 09:57:32` → `2026-09-04 15:08:18`.

> ⚠️ Bản gốc không còn và **không nằm trong git** (`logs/` bị `.gitignore` chặn). Hai file trong thư mục này là **bản duy nhất** — `LogsAPI/` không bị gitignore nên hãy commit để có lịch sử.

---

*Người tiêu thụ chính: `../videowall-device-api-prompt-20260907.md` mục 1.*
