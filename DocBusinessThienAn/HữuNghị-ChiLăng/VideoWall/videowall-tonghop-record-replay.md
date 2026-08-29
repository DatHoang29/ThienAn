# VideoWall — Tổng hợp bối cảnh & kế hoạch Ghi/Phát lại

> Gộp từ `videowall_plan.md` (19/08 + 25/08), `transcript-videowall-28082026.md` (28/08),
> datasheet `_source/DS-C30S-S11_Datasheet_20250324.md`, và mã nguồn `Module.VideoWall.WPF`.
> Chốt ngày 29/08/2026. Prompt thực thi kèm theo: `videowall-record-replay-prompt.md`.

---

## 1. Bối cảnh một dòng

Đội mang công cụ **`Module.VideoWall.WPF`** + service sang khách hàng **TCB** (dự án Hữu Nghị –
Chi Lăng) để kiểm thử trực tiếp trên bộ điều khiển tường ghép **Hikvision DS-C30S-S11** trong
**2 ngày**, trên thiết bị khách **đang vận hành**, cam kết **không đụng phần cứng**.

## 2. Thiết bị & site

| Mục | Giá trị |
|---|---|
| Model | DS-C30S-S11 — 11-slot Video Wall Controller (Hikvision; ISAPI + Digest auth, HTTP/TCP) |
| Site TCB | **1 controller, 12 màn** (cách xếp lưới **chưa chốt** — xác nhận khi tới nơi) |
| Năng lực (datasheet) | 8 video wall · tối đa 40 màn/tường · chia ô 1/4/6/8/9/16 · layer/cổng: 8×1080p hoặc 4×4K · 128 scene · 128 plan · trễ auto-switch 400 ms · 16 cửa sổ mở · trễ giải mã 50 ms (local) / 200 ms (network) |
| Kết nối | Digest auth, NAT, web client (IE8/Chrome45+), keyboard mạng/serial, ONVIF |
| Không đụng | Genlock/đồng bộ thời gian giữa bộ hardware (do nhà thầu CT3 làm); các "video 1 / video 2" trên server độc lập hoàn toàn |

## 3. Yêu cầu tiến triển qua từng buổi

**19/08 (`videowall_plan.md`)** — làm **form/dịch vụ test luồng tích hợp** với thiết bị (hạng mục
chính, không chỉ khảo sát); thử split màn theo vị trí; xác định cơ chế điều khiển + giao thức;
kiểm tra khả năng tự liên kết lại giữa các vùng. Giai đoạn 1 cho **cấu hình cứng**; ưu tiên bản
chạy được để treo dài ngày. Đề xuất **kiểm thử qua TCP** (chưa triển khai). *[Cần chốt]*: hãng/model,
tài liệu bộ lệnh, số preset layout, có lấy nguồn hiển thị từ VMS không.

**25/08 (`videowall-25-08.txt`)** — công cụ **WPF** (AI-gen, C#, UI trực quan). Chức năng: đăng
nhập; tham số controller/screen; thông tin cổng input/output; **cấu hình scene** (thêm/xoá/sửa/cấu
hình vị trí); lấy thông tin scene; activate scene; **thời gian hoạt động/bật/tắt**. Tab gồm: url,
user/pass, `[Connect]`, thiết lập scene, `Name [ ] Vị trí [ ] [Set]`. **Log**:
`giờ — thiết lập scene {} / Response OK / Fail {...}` (tận dụng bảng log cũ, không có thì tạo mới).
Scene setup: **các output riêng rẽ** và **các output có chồng windows**.

**28/08 (`transcript`)** — buổi chuẩn bị đi TCB:

- **2 bản kế hoạch**: (1) gửi khách — viết đơn giản, ít kỹ thuật, "mượn thiết bị để kiểm thử",
  kèm cam kết không thay đổi/không mở phần cứng → chị Tuyền duyệt → gửi mail; (2) nội bộ — kỹ thuật.
- **2 ngày**, test trên thiết bị khách đang chạy, ưu tiên **buổi tối/ngoài giờ**. **3 người** đi
  (Đạt + Hiếu + 1 người làm việc với khách).
- **Ngày 1**: kiểm tra thông tin kết nối → **backup cấu hình hiện tại** (tải file từ web của từng
  controller + qua API) → cắm PC vào controller (HDMI) → chạy kịch bản (split, phóng to, chia ô)
  → làm **case khó trước**. **Ngày 2**: rà lỗi ngày 1, bổ sung/nâng cấp, **restore/cập nhật cấu
  hình**, bàn giao lại.
- **Ghi bằng chứng**: log **mọi request/response ra file** (lưu máy rồi tải lên); **chụp màn hình
  từng bước** — màn tường, web quản lý, màn sim/hệ thống, màn công cụ, đèn báo controller. Chạy
  bình thường → chụp là đủ; lỗi → lấy log chi tiết.
- **Quy tắc thời gian**: 1 chức năng khó quá ~1 giờ không xong → **skip**, làm việc khác, quay lại
  ngày 2 hoặc buổi tối.
- **Kịch bản test cần có**: scene **không chồng** cửa sổ; scene **chồng** cửa sổ; **canh size khi
  có 2 trigger**; **case sai tham số** (gửi 21 màn khi thiết bị có 12 → thiết bị phải **báo lỗi**
  → ghi nhận → sửa cách xử lý); **case mất kết nối** (xử lý gọn, ghi nhận, đi tiếp).
- **Tổ chức lại UI**: 1 **tab API** (API riêng lẻ) + 1 **tab Kịch bản** (luồng tích hợp, gom
  1–2–3 hàng thay vì bấm từng API như Postman). Câu hỏi: giữa 2 API có cần chờ bao lâu.
- **probe** = nút gọi API ngầm lấy toàn bộ cấu hình mặc định lúc đầu để backup rồi restore sau test.
- **Giới hạn phần cứng** (chỉ lưu ý, không sửa được bằng phần mềm): 1 controller phủ 32 màn 4K =
  8 card ×4 → viewer/băng thông không kham nổi; stream video không "cắt" chia màn như ảnh tĩnh.

## 4. Vậy mình đang làm gì — 2 nhánh song song

### Nhánh A — Chuyến đi test 2 ngày tại TCB *(sự kiện chính, không thay thế được)*

Con người + tài liệu + quy trình: 2 bản plan (khách/nội bộ), email xin duyệt, quy trình
backup-trước / restore-sau, bộ kịch bản test ở trên, quy tắc skip >1h, và **ghi log + chụp màn
hình mọi bước** làm bằng chứng đối chiếu. *(Nằm ngoài phạm vi code — nhưng là lý do tồn tại của
nhánh B.)*

### Nhánh B — Nâng cấp công cụ WPF

Thêm **chế độ Ghi / Phát lại (Record / Replay)** để:

- **Trước khi đi**: diễn tập/demo luồng bằng tape mẫu, không cần thiết bị.
- **Trong khi đi**: bật **Ghi** → thu **toàn bộ request/response ISAPI** ra file (đúng yêu cầu
  transcript "ghi mọi thứ gửi/nhận ra file"), song song với việc test thật.
- **Sau khi về**: **Phát lại** bản ghi → chạy tiếp case bị skip do luật 1 giờ, test hồi quy khi
  công cụ còn sửa — **không phải mượn lại thiết bị**.

## 5. Công cụ WPF hiện có gì (đối chiếu yêu cầu 25/08 + 28/08)

| Yêu cầu | Đã có trong `Module.VideoWall.WPF` |
|---|---|
| Đăng nhập / kết nối | Backend mode (`SessionState`) + **Direct mode** (IP/port/account/password + Digest) — thanh kết nối Row 1 `MainWindow.xaml` |
| Tham số controller/screen, cổng in/out | `Probe` (capabilities, 8 walls, outputs, input channels); tab 3 Output, 4 Input, 8 Screen; `SceneSetup` "Nạp màn hình & nguồn" |
| Cấu hình scene (thêm/xoá/sửa/vị trí) | Tab **12 "Thiết lập Scene"** — CRUD scene + cửa sổ; **"từng cổng ra"** & **"cửa sổ xếp lớp"** = đúng 2 loại 25/08; guardrail `MaxWindowNums`, SID `1..MaxSceneNums` |
| Lấy thông tin scene / activate | `GetActiveScene`, `ListScenes`, `LoadSceneWindows`; `ActivateScene` + `PushToDevice` + built-in "Active scene" |
| Thời gian bật/tắt | Tab **Lịch** (`ScheduleViewModel`) — CRUD `VwSchedule`, `Action` on/off, weekday/time/cron — **qua backend** |
| Log đúng format | Activity log + `DeviceStepNotification` mang `RequestXml/ResponseXml`; `ExportLog` → JSON (`Method/Endpoint/HttpStatus/RequestXml/ResponseXml`) |
| Luồng tích hợp nhiều API | Tab **13 "Kịch bản"** — build step từ preset, lưu/nạp JSON, **Chạy / Chạy tiếp từ bước lỗi**, delay giữa bước (mặc định 400 ms), 3 kịch bản dựng sẵn, kịch bản 2 nguồn tranh vùng, bắn 2 trigger |
| Backup / restore | Nút **"Chụp lại mặc định" / "Khôi phục mặc định"** + `probe` (`VwDeviceDefaultStore`); snapshot XML cửa sổ ra file |
| An toàn | `DryRun` mặc định true, xác nhận trước khi "đẩy thật", chỉ Direct mode chạm thiết bị |

**Còn thiếu / chưa chắc:**

- **Ghi & Phát lại** — chưa có → phần chính cần làm.
- **Bộ kiểm thử lỗi** tab 13 hiện phần lớn **mô phỏng client-side** (hard-code PASS/FAIL), chưa
  bắn lệnh thật + đọc mã lỗi/subStatusCode thiết bị.

## 5b. Ràng buộc phạm vi — CHỈ 2 TẦNG: WPF ↔ THIẾT BỊ

Đợt này **chỉ đụng giao diện WPF và thiết bị**. Bỏ hết những gì liên quan tới database, tầng
service C# phía server, và **Backend mode** của công cụ.

| Bỏ qua đợt này | Vì sao |
|---|---|
| Backend mode (`VideoWallApiClient` → TAC_WebAPI) | Đi qua tầng service + DB — ngoài "2 tầng" |
| Tab **Lịch** (`ScheduleViewModel`) | Chạy hoàn toàn qua backend |
| "Bắn 2 trigger" (`ActivateSceneByEvent`) | Backend `VwEventRule` |
| Nhánh backend trong `SceneSetupViewModel` / `ScenarioViewModel` | Chỉ giữ **nhánh Direct** |
| `Module.VideoWall` / `Module.VideoWall.Core` (server) | Tầng service — không đụng |

⇒ Đường duy nhất còn lại: **WPF → `VwDirectISAPIClient` (HTTP/ISAPI + Digest) → DS-C30S-S11**.
Record/Replay chèn đúng vào đường này.

## 5c. TCP vs HTTP — và vì sao "kiểm thử qua TCP" CHƯA cần

- **TCP** = tầng vận chuyển: chỉ lo "byte tới nơi, đúng thứ tự, không mất". KHÔNG quy định nội
  dung byte nghĩa là gì → nói chuyện TCP thô thì 2 bên phải tự thoả thuận khung lệnh (ASCII hay
  binary, ký tự kết thúc, độ dài), tự quản keep-alive, tự định nghĩa cách báo OK/lỗi.
- **HTTP** = giao thức ứng dụng **chạy BÊN TRÊN TCP**, đã định sẵn: method (GET/POST/PUT/DELETE),
  đường dẫn, header, body, mã trạng thái (200/401/404/500); mỗi request có đúng 1 response.
- **ISAPI của Hikvision = HTTP** (cổng 80, body XML/JSON, Digest auth). Công cụ WPF **đang** nói
  chuyện với DS-C30S-S11 bằng HTTP/ISAPI — tức là đã đi trên TCP rồi, chỉ là qua lớp HTTP.
- ⇒ "Kiểm thử qua TCP" (plan 19/08 §1.3) chỉ cần khi gặp thiết bị/tính năng có **cổng TCP thô
  riêng** + tài liệu bộ lệnh riêng. Hiện **không có nhu cầu đó** → giữ HTTP/ISAPI.

## 6. Ngoài phạm vi / hoãn lại

- **Case nhiều controller ghép 1 tường** — site TCB chỉ 1 con/12 màn → **ghi chú "chưa test được",
  hoãn** tới khi có ≥2 controller thật. *(Không giả lập.)*
- Genlock / đồng bộ thời gian giữa bộ hardware — do nhà thầu, không đụng.
- Hình thật trên tường, chất lượng YUV444, trễ giải mã, băng thông 32×4K, hiệu năng viewer — chỉ
  **quan sát + chụp** tại chỗ, không phần mềm nào thay được.
- Thông tin cũ "thiết bị có 2 màn" (plan 19/08) đã lỗi thời — nay là 12 màn.

## 7. Điểm chưa chốt (không chặn việc code)

1. Cách xếp lưới 12 màn ở TCB (4×3 / 6×2 / khác) — chỉ ảnh hưởng nội dung **tape mẫu**, xác nhận
   khi tới nơi.

---

## 8. Chốt phạm vi đợt này & phác thảo kỹ thuật

**Làm:** (B1) **Ghi / Phát lại cho Direct mode** + tape mẫu + runbook; (B2) **harness test offline
(xUnit)** trên chồng client Direct.

**Quyết định đã chốt:** nguồn bản ghi = công cụ **tự ghi native** tại hiện trường; độ khớp khi
phát lại = **lỏng** (`METHOD + normalizedPath`, không cần đúng thứ tự, không so body); chỉ 2 tầng
WPF ↔ thiết bị (không backend, không DB, không service C#).

**Thuật ngữ:**
- **tape (bản ghi)** = file JSON chứa các cặp request↔response ghi lại khi công cụ nói chuyện với
  thiết bị. Mỗi dòng = `{ method, path, body gửi, mã HTTP, body nhận }`. Ghi ra ở chế độ *Ghi*,
  đọc vào ở chế độ *Phát lại*.
- **tape mẫu (`sample-tape.json`)** = một tape **làm sẵn bằng tay** (không thu từ thiết bị thật),
  đóng gói kèm công cụ, để **diễn tập/demo trước chuyến đi** và làm **fixture cho harness xUnit**.
  Nội dung viết theo hình dạng DS-C30S-S11 trả về, số liệu lấy từ datasheet (128 scene, 8 wall,
  `maxWindowNums=16`…). Có tape thật sau chuyến đi thì dùng tape thật.

- **Điểm chèn duy nhất:** mọi lệnh tới thiết bị đi qua `HttpClient.SendAsync` trong
  `VwDirectISAPIClient`. Thêm `VwReplayHandler : DelegatingHandler` vào chuỗi:
  `VwDirectDigestHandler → VwReplayHandler → HttpClientHandler`.
  - `Record`: gọi thật + tee request/response ra file "tape".
  - `Replay`: không gọi mạng; tra tape theo khoá, dựng `HttpResponseMessage`; không khớp → 404 +
    body chú thích + log Warning.
- **Tape** = JSON, đọc thẳng được file `videowall-log_*.json` do `ExportLog` xuất ra
  (`Endpoint→Path`, `RequestXml→RequestBody`, `ResponseXml→ResponseBody`).
- **Chọn chế độ:** `enum VwDeviceIoMode { Live, Record, Replay }` + `TapePath` trên
  `ConnectionViewModel`; nới `CanUseSelectedController`/`CanSendIsapi` khi `Replay` (Replay không
  cần IP/account/password thật).
- **Gộp factory:** `ConnectionViewModel.BuildDirectClient()` và
  `SceneSetupViewModel.BuildDirectClientFromConnection()` đang **gần trùng** → gom thành
  `VwDirectClientFactory` dựng `HttpClient` với chuỗi handler theo `DeviceIoMode`. Chỉ một chỗ chèn
  `VwReplayHandler`. (Không đụng `App.xaml.cs` — các client Direct tạo thủ công, không qua DI.)
- **UI:** cụm `Chế độ thiết bị: Live / Ghi / Phát lại` + ô tape + nút chọn tape ở Row 1
  `MainWindow.xaml`; banner "ĐANG PHÁT LẠI" khi Replay; khoá nút "đẩy thật".
- **Tape mẫu:** `Module.VideoWall.WPF/SampleData/sample-tape.json` dựng từ năng lực DS-C30S-S11
  (userCheck OK, capabilities `maxSceneNums=128`, 8 wall, outputs, input channels, windows
  rỗng→N, vài response lỗi mẫu) để demo trước chuyến đi.
- **Runbook:** `videowall-record-replay.md` — cách bật Ghi tại hiện trường + cách Phát lại sau khi
  về + **bảng phạm vi test được / không test được**.
- **B2 — Harness xUnit** `tests/Modules/VideoWall/Module.VideoWall.WPF.Tests`: nạp tape (mẫu hoặc
  tape thật), bơm `VwReplayHandler` vào `HttpClient` → `VwDirectISAPIClient`, drive **chồng client
  Direct** — `VwDirectDeviceConnectionClient.Probe/SendIsapi`, `VwDirectSetupSceneOrchestrator`,
  `VwDeviceDefaultCaptureOrchestrator` — assert số bước OK/Fail, guardrail (`MaxWindowNums`, SID
  `1..MaxSceneNums`), và "rút 1 entry → bước đó Fail". **Không** động tới view-model có phụ thuộc
  backend (`VideoWallApiClient`).

### Phạm vi test được qua Replay vs. bắt buộc tại chỗ

**Test được offline (logic / luồng / UI / guardrail):** Connect·Ping·Probe; 11 tab ISAPI (GET đọc
form; PUT/POST xem request sẽ gửi + response canned); Tab 12 bố cục cửa sổ "từng cổng ra" &
"xếp lớp", chặn `MaxWindowNums`/SID; Tab 13 build & lưu/nạp kịch bản, **Chạy / Chạy tiếp từ bước
lỗi**, delay giữa bước, 3 kịch bản dựng sẵn, snapshot XML ra file, kịch bản 2 nguồn tranh vùng;
bộ kiểm thử lỗi (lưới > màn thật, SID 999, 21 vs 12 màn) với body lỗi lấy từ tape; Chụp/Khôi phục
mặc định (đọc từ tape); định dạng Log; Xuất log / gói bằng chứng.

**Bắt buộc tại chỗ (không tape nào thay được):** hình thật trên tường, chồng lớp, chất lượng
YUV444; trễ giải mã 50 ms/200 ms, genlock/đồng bộ thời gian giữa controller; trần băng thông
32×4K & hiệu năng viewer; mã lỗi/subStatusCode chưa từng ghi; bắt tay Digest thật, NAT, hành vi
timeout/mất kết nối (chỉ *mô phỏng* được bằng cách nhét entry lỗi vào tape); **case nhiều
controller** (site chỉ 1 con).

### Critical files

| Việc | File |
|---|---|
| Handler + tape (mới) | `Module.VideoWall.WPF/Api/Direct/Replay/VwReplayHandler.cs`, `VwTape.cs`, `VwTapeStore.cs` |
| Chọn chế độ | `Module.VideoWall.WPF/ViewModels/ConnectionViewModel.cs` |
| Gộp factory Direct client (mới) | `Api/Direct/VwDirectClientFactory.cs` ← gộp từ `ConnectionViewModel` + `SceneSetupViewModel` |
| Ghi khi Record | `VwReplayHandler` (tee) + `MainViewModel.ExportLog` (thêm "Lưu thành tape") |
| UI | `Module.VideoWall.WPF/Views/MainWindow.xaml` (Row 1) |
| Tape mẫu | `Module.VideoWall.WPF/SampleData/sample-tape.json` + `.csproj` CopyToOutputDirectory |
| Harness | `tests/Modules/VideoWall/Module.VideoWall.WPF.Tests/` (mới) |
| Runbook | `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/videowall-record-replay.md` |

### Verification

1. `dotnet build src/Modules/VideoWall/Module.VideoWall.WPF/Module.VideoWall.WPF.csproj`.
2. **Record**: LAN có thiết bị (hoặc giả lập) → chế độ Ghi → Probe + chạy 1 kịch bản → kiểm tra
   file tape có đủ entry userCheck/capabilities/windows.
3. **Replay**: ngắt mạng thiết bị → chế độ Phát lại → chọn tape → Probe hiện đúng
   `maxWindowNums/maxSceneNums`; tab 13 "Chạy kịch bản" chạy hết bước; rút 1 entry khỏi tape →
   bước đó Fail và "Chạy tiếp từ bước N" hoạt động.
4. Guardrail: lưới 10×10 / SID 999 → tool chặn trước khi gửi (không phụ thuộc tape).
5. `dotnet test tests/test.csproj` — harness B2 xanh; theo CLAUDE.md chỉ chạy DB local (harness
   này không đụng DB nên an toàn).
