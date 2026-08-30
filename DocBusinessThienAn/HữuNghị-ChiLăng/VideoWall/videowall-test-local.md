# Thử công cụ WPF tại máy local (trước khi đi hiện trường)

> Hai cách chạy thử `Module.VideoWall.WPF` tại máy local trước khi sang hiện trường.
> Checklist đi test 2 ngày: [`videowall-test-2ngay.md`](videowall-test-2ngay.md).

| Cách | Cần gì | Thấy được gì | Thời gian |
|---|---|---|---|
| 1. Chạy test tự động | .NET SDK | luồng Direct chạy đúng với thiết bị giả lập | ~1 phút |
| 2. MockServer riêng + app WPF | .NET SDK, Windows | thao tác tay gần giống hiện trường, kiểm tra auto-log | ~10 phút |

---

## Cách 1 — Chạy bộ test tự động (đã drive MockServer sẵn)

```powershell
# Toàn bộ test WPF — cần TFM net10.0-windows
dotnet test tests/test.csproj -f net10.0-windows --filter "FullyQualifiedName~Tests.Modules.VideoWall.Wpf"
```

Xanh = luồng Ping / Probe / SendIsapi / thiết lập scene / guardrail chạy đúng với `VwISAPIMockServerHikvision` (thiết bị Hikvision giả lập, khởi động tự động trong test Host).

---

## Cách 2 — MockServer đứng riêng + mở app WPF thật *(giống hiện trường nhất)*

### 2.1. Bật MockServer

```powershell
dotnet run --project scripts/VwMockServerRunner
```

In ra:

```
[VwMockServerRunner] Giả lập thiết bị Hikvision DS-C66S-H88-CL tại http://127.0.0.1:18080/
[VwMockServerRunner] Port: 18080, 18081, 18082, 18083
[VwMockServerRunner] Account: admin | Password: Password123!
```

Để cửa sổ này chạy. Ctrl+C để dừng.

### 2.2. Mở app WPF, trỏ vào MockServer

```powershell
dotnet run --project src/Modules/VideoWall/Module.VideoWall.WPF
```

Thanh kết nối (Row 0):
- IP `127.0.0.1` · Port `18080` · Account `admin` · Password **bất kỳ** (mock không kiểm tra hash mật khẩu — chỉ cần account `admin` + có header Digest).
- Thanh kết nối chỉ gồm: IP, Port, Account, Password, WallNo, nút **Ping**, nút **Probe** và thông báo trạng thái (đã bỏ các nút thừa: *Chụp/Khôi phục mặc định*, *Nạp lại kết nối*).

Thao tác kiểm tra (2 nút riêng, bấm lần lượt — Probe KHÔNG tự chạy theo Ping):
- Bấm **Ping** → chỉ kiểm tra kết nối + xác thực Digest Auth, kết quả báo "kết nối thành công", chưa đọc số liệu gì.
- Bấm tiếp **Probe** (nút riêng, bắt buộc trước khi nạp dữ liệu ở Tab 1) → trả về đầy đủ: 2 wall (`VideoWall1`, `HoangNhu`), 2 output, 2 input channel, `maxWindowNums=512`, `maxSceneNums=128`, `isSupportScene=true`.
- **Tab 3–13** (11 nhóm ISAPI: Board, Decoding, Output...): Khung **Response** rộng toàn màn hình tự động hiển thị bên dưới TabControl (có GridSplitter kéo dãn linh hoạt) → gửi GET/PUT/POST ISAPI bất kỳ và xem ngay response XML/JSON.
- **Tab 1 "Thiết lập Scene" / Tab 2 "Kịch bản"**: Khung Response tự động ẩn gọn (`Height = 0`), nhường 100% diện tích cho bảng thiết lập và kịch bản.
- Mọi thao tác đều tự động ghi log vào file `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_*.jsonl`.

---

## MockServer giả lập được gì

| | Nội dung |
|---|---|
| ✅ Có | `Security/userCheck`, `DisplayDev/capabilities` + `.../VideoWall/capabilities`, `DisplayDev/VideoWall` (2 wall), `{id}/outputs` (2), `Video/inputs/channels` (2), windows CRUD (GET trả 2 cửa sổ), `scene/{id}/activate` · `/saveData` · `/isRunning`, `ScreenCtrl/closeAll`, Serial transparent, Digest 401→auth, + 116 route preset |
| ✅ Case lỗi (chỉ bật được **trong code test**, không qua HTTP) | `SimulateBadParameters`, `SimulateInvalidOperation`, `SimulateDeviceFailure`, `SimulateUnreachable`, `SimulateSaveDataFailure`, `SimulateNonceExpiry`, `FailedAuthLockoutThreshold` (khoá IP), hạ `MaxSceneNums` để test chặn SID |
| ⚠️ Khác thiết bị thật DS-C30S-S11 | `maxWindowNums = 512` (thật: **16**) · realm `DS-C66S-H88-CL` · chỉ **2** wall / **2** output / **2** input (thật: 8 wall, 12 output) · số liệu là ground-truth của **DS-C66S-H88-CL**, không phải DS-C30S-S11 |
| ❌ Không có | hình thật trên tường, độ trễ giải mã, băng thông, genlock |

---

## Kịch bản test FULL tính năng WPF với MockServer (nắm rõ toàn bộ công cụ)

> Phạm vi Direct Mode only ([`videowall-record-replay.md` §A3](videowall-record-replay.md#a3-ràng-buộc-phạm-vi--chỉ-2-tầng-wpf--thiết-bị)): **bỏ Backend mode, tab Lịch, "Bắn 2 trigger" / "So khớp ưu tiên"**.
> Ký hiệu: ✅ chạy thật, kỳ vọng đúng · ⚠️ biết trước là đang lỗi, cứ thử để thấy đúng hiện tượng, đừng tìm nguyên nhân ở cấu hình của bạn · 🚫 đừng test, ngoài phạm vi.

### Bước 0 — Bật MockServer + mở app
1. [ ] Terminal 1, tại `TA-ITS015-WEBAPI-V1.0`: `dotnet run --project scripts/VwMockServerRunner` → để chạy nền.
2. [ ] Terminal 2, cùng thư mục: `dotnet run --project src/Modules/VideoWall/Module.VideoWall.WPF` → mở app.
3. [ ] Nhập **IP** `127.0.0.1` · **Port** `18080` · **Account** `admin` · **Password** bất kỳ (mock không kiểm tra mật khẩu).

### Bước 1 — Kết nối
1. [ ] Bấm **Ping** → kỳ vọng OK (xác nhận kết nối + Digest Auth thành công).
2. [ ] Bấm **Probe** (cạnh nút Ping) → kỳ vọng: Đọc được 2 wall (`VideoWall1`, `HoangNhu`), 2 output, 2 input, `maxWindowNums=512`, `maxSceneNums=128`. Log hiện đủ chuỗi bước `UserCheck → GetCapabilities → GetVideoWalls → GetOutputs → GetInputChannels` (nhóm "Direct"). Sau khi Probe xong, app tự động đồng bộ Nguồn tín hiệu và Cổng ra sang Tab 1.

### Bước 2 — Tab 1 "Thiết lập Scene" (dựng bố cục màn hình & kịch bản trực quan) ✅
Tab này phục vụ thiết lập ma trận màn hình, bố cục cửa sổ trực quan và đẩy cấu hình scene xuống thiết bị:

1. [ ] Chuyển sang Tab 1 "Thiết lập Scene":
   - **Nguồn tín hiệu**: Đã tự động nạp 2 mục từ kết quả Probe (`ProbeResult.InputChannels`: Kênh 1, Kênh 2).
   - **Cổng ra (Output)**: Đã tự động chọn cổng ra sẵn có từ Probe trong ô thêm màn.
   - **Màn hình**: Nạp từ file lưu local `VwLocalScreenStore` (nếu đây là lần đầu chạy máy/thiết bị mới thì danh sách màn hình sẽ trống — hãy thực hiện gán ở bước 2 bên dưới).
   - (Nút **"🔄 Nạp màn hình & nguồn"**: Dùng để nạp lại bất cứ lúc nào khi muốn làm mới lại dữ liệu từ file local và Probe).
2. [ ] **"➕ Thêm màn"**: Chọn cổng Output trong danh sách thả xuống (lấy từ Probe) → nhập Cột/Hàng lưới (VD: Cột 0 Hàng 0) → bấm **"➕ Thêm màn"** → lặp lại cho Output thứ 2 (Cột 1 Hàng 0) → kỳ vọng 2 màn hình hiện ra trong bảng và được tự động lưu local (`VwLocalScreenStore`, còn nguyên ở các lần mở app sau). (Muốn bỏ màn nào thì chọn dòng rồi bấm **"🗑 Xoá màn"**).
3. [ ] Gõ tên `TEST_KHONGCHONG` → **"➕ Tạo mới"** → chọn Cột/Hàng lưới kịch bản → kỳ vọng tên hiện ra ở ô "Kịch bản:".
4. [ ] Để chế độ **"Màn KHÔNG chồng (Từng cổng ra)"**: tick chọn 2 màn hình vừa gán ở trên, gán Nguồn tương ứng → bấm **"▶ Dựng cửa sổ phủ kín"** → kỳ vọng bảng "3. Cửa sổ đã lưu" hiện đủ 2 cửa sổ vừa dựng.
5. [ ] Gõ tên `TEST_CHONG` → **"➕ Tạo mới"** → chuyển sang **"Màn CHỒNG (Cửa sổ xếp lớp)"** → bấm **"➕ Thêm dòng cửa sổ"** (chỉnh ZIndex 1 làm nền, ZIndex 2 đè lên) → bấm **"▶ Dựng cửa sổ xếp lớp"** → kỳ vọng hiện các cửa sổ xếp lớp.
6. [ ] Bật `Chạy thử (DryRun)` (mặc định đang bật) → bấm **"🚀 ĐẨY XUỐNG THIẾT BỊ"** → kỳ vọng các bước gửi cấu hình hiện chi tiết ở Log nhưng KHÔNG gửi lệnh ghi mutate thật xuống thiết bị.
7. [ ] Tắt `DryRun` → xác nhận hộp thoại → bấm **"🚀 ĐẨY XUỐNG THIẾT BỊ"** lần nữa → kỳ vọng mock trả `statusCode 1 (OK)` cho từng bước — đây là lần ghi thật đầu tiên.
8. [ ] Thử vượt giới hạn `maxWindowNums` (nếu cố tình tạo > 512 cửa sổ) → kỳ vọng bị chặn ngay từ client dựa trên dữ liệu Probe, KHÔNG gửi request rác xuống thiết bị.
9. [ ] (Tuỳ chọn) thử **"💾 Cập nhật"** / **"🗑 Xoá"** trên 1 kịch bản local, hoặc bấm **"⚡ Kích hoạt"** để gửi lệnh kích hoạt scene trực tiếp xuống thiết bị.

### Bước 3 — Tab 2 "Kịch bản" (tự động hoá + công cụ kiểm thử)

**3a. Danh sách kịch bản gộp (cột trái "1. Danh sách kịch bản")** ✅
1. [ ] Nhìn cột trái → kỳ vọng thấy **3 mục 📦 built-in ở đầu danh sách** ("📦 1. Thiết lập scene (không chụp hình)", "📦 2. ... (có chụp hình)", "📦 3. Active scene"), rồi tới `TEST_KHONGCHONG`/`TEST_CHONG` vừa tạo ở Tab 1 (hoặc các chuỗi API tự lưu).
2. [ ] Chọn 1 mục 📦 built-in → kỳ vọng nút **"Xoá"** tự khoá (không xoá được built-in).
3. [ ] Chọn 1 mục thường (JSON tự lưu) → kỳ vọng nút **"Xoá"** sáng lại để xoá được.

**3b. Builder thủ công (chuỗi API tự soạn)** ✅
1. [ ] Bấm **"+ Tạo mới"** → chọn 2-3 API ở bảng danh sách bên phải (nhóm Board / Scene...) → bấm **"➕ Thêm bước"** cho mỗi API.
2. [ ] Đặt ô **"Chờ (ms)"** khác 400 → bấm **"💾 Lưu"** (đặt tên riêng, VD `TEST_CHUOI_API`) → kỳ vọng tên mới xuất hiện ngay trong danh sách gộp cột trái.
3. [ ] Chọn lại đúng mục đó từ danh sách → kỳ vọng nạp lại đúng các bước đã cấu hình.
4. [ ] Bấm **"▶ CHẠY KỊCH BẢN"** → kỳ vọng log ghi nhận từng bước chạy và dòng "Chờ … ms trước bước kế tiếp".
5. [ ] Cố tình sửa 1 bước cho sai (VD đường dẫn endpoint sai) rồi chạy lại → kỳ vọng nút đổi thành **"▶ Chạy tiếp từ bước #N"**; bấm để xác nhận resume chạy tiếp đúng từ bước lỗi, không cần chạy lại từ đầu.

**3c. Kịch bản dựng sẵn "Thiết lập scene" (📦 mục 1 và 2)** ⚠️ ĐANG LỖI
1. [ ] Chọn mục 📦 "1. Thiết lập scene (không chụp hình)" → điền Tên Scene/WallNo → bấm **"▶ CHẠY KỊCH BẢN"**.
2. [ ] Kỳ vọng thật (đã biết): ở Direct Mode nhánh này CHỈ gọi 1 lệnh đọc (GET danh sách cửa sổ hiện có) — **KHÔNG tạo scene/cửa sổ nào mới cả**, dù log báo "✅ Hoàn thành". Muốn dựng scene thật thì dùng **Tab 1**.
3. [ ] Chọn mục 📦 "2. ... (có chụp hình)" → chạy thử → kỳ vọng: vì lý do trên, file snapshot cũng **không được lưu** (dùng nhầm field đọc phản hồi thiết bị) — đừng tìm file trong `Logs`, hiện chưa có.

**3d. "📐 2 nguồn tranh vùng — canh kích thước"** ✅ đẩy thật
1. [ ] Điền Nguồn Lớn/Nhỏ (mặc định 1/2), W/H, SID kịch bản thử (`OverlapSceneId`, mặc định 1) → bấm **"▶ Chạy kiểm thử tranh vùng"**.
2. [ ] Kỳ vọng: gọi thật qua thiết bị (mock) — log báo đúng số cửa sổ THẬT SỰ được tạo (`result.Windows.Count`), có `Message` thật nếu thất bại.

**3e. "🧪 Bộ kiểm thử lỗi (3 trường hợp biên)"** ✅ gọi thật & công tắc xem mã lỗi
1. [ ] Quan sát checkbox **"Gửi thật để xem mã lỗi thiết bị"**:
   - Mặc định **TẮT** (an toàn): Client tự dùng dữ liệu Probe để chặn sớm (SceneId ngoài dải hoặc số cửa sổ vượt ngưỡng) mà không gửi request sai xuống thiết bị.
   - Bật **BẬT**: Cho phép request vượt giới hạn đi thật xuống MockServer / thiết bị để ghi nhận mã lỗi HTTP/ISAPI thật mà thiết bị trả về (phục vụ đối chiếu mã lỗi).
2. [ ] Bấm **"Chạy bộ kiểm thử lỗi"**.
3. [ ] Kỳ vọng cả 3 dòng log đều **[PASS]** khi chạy đúng mock chuẩn (512 window / 128 scene):
   - Case A (999 cửa sổ vượt `maxWindowNums`) → thiết bị / client **TỪ CHỐI**.
   - Case B (1 nguồn gán 3 cửa sổ) → thiết bị **CHẤP NHẬN** (đúng theo datasheet — 1 nguồn được phép chiếu ra nhiều cửa sổ).
   - Case C (SceneId 99999 ngoài dải `maxSceneNums`) → thiết bị / client **TỪ CHỐI**.
4. [ ] Nếu ra `[FAIL]` ở case nào → đọc `Message` kèm theo để phân tích phản hồi từ thiết bị.

### Bước 4 — Case lỗi ở tầng thiết bị (mock chỉ bật được qua code test, không qua UI)
1. [ ] Từ `d:\ThienAn`: `dotnet test tests/test.csproj -f net10.0-windows --filter "FullyQualifiedName~Tests.Modules.VideoWall.Wpf"`.
2. [ ] Kỳ vọng xanh hết (99/99 test) — các case `SimulateBadParameters`, `SimulateDeviceFailure`, `SimulateUnreachable`, `SimulateNonceExpiry`, chặn SID vượt `MaxSceneNums` đều được kiểm chứng ở đây.

### Bước 5 — Kiểm tra log
1. [ ] File tự động: `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_{yyyyMMdd_HHmmss}.jsonl` — mỗi dòng có `Time, Stage, Level, Detail, Method, Endpoint, HttpStatus, RequestXml, ResponseXml`.
2. [ ] Nút **"Xuất log"** ở góc trên bảng Logs → xuất file `.json` gộp khi cần lưu nhanh.

---

### Tổng kết — chạy 1 vòng là nắm hết công cụ
| Mục | Trạng thái | Ghi chú |
|---|---|---|
| Tab 1 "Thiết lập Scene" (toàn bộ) | ✅ | Dựng bố cục ma trận màn hình & cửa sổ trực quan; lưu local Screen & Scene |
| Tab 2 — danh sách gộp built-in + đã lưu | ✅ | 1 danh sách, 1 nút chạy, Xoá tự khoá cho built-in |
| Tab 2 — builder thủ công + delay + resume | ✅ | Nút đổi thành `▶ Chạy tiếp từ bước #N` khi có lỗi |
| Tab 2 — 📦 "Thiết lập scene (không/có chụp hình)" | ⚠️ | Chưa dựng gì thật ở Direct Mode, snapshot chưa lưu được — biết trước, chưa sửa |
| Tab 2 — "2 nguồn tranh vùng" | ✅ | Đẩy thật qua thiết bị |
| Tab 2 — "Bộ kiểm thử lỗi" | ✅ | Có công tắc "Gửi thật để xem mã lỗi thiết bị", PASS/FAIL rõ ràng |
| Tab 3..13 — 11 nhóm ISAPI | ✅ | Khung Response toàn màn hình hiện động dưới TabControl, có GridSplitter |
| Log tự động + Xuất log | ✅ | File `.jsonl` tự động ghi liên tục; nút Xuất log ra `.json` |
| Case lỗi tầng thiết bị | ✅ | Qua `dotnet test` (99/99 test pass), không qua UI |

