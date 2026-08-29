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

Thanh kết nối (Row 1):
- IP `127.0.0.1` · Port `18080` · Account `admin` · Password **bất kỳ** (mock không kiểm tra hash mật khẩu — chỉ cần account `admin` + có header Digest).

Thao tác kiểm tra:
- **Ping** → OK. **Probe** → 2 wall (`VideoWall1`, `HoangNhu`), 2 output, 2 input channel, `maxWindowNums=512`, `maxSceneNums=128`, `isSupportScene=true`.
- **Tab 1–11**: gửi GET/PUT/POST ISAPI bất kỳ → xem response XML/JSON giả lập.
- **Tab 12–13**: tạo scene, thêm cửa sổ, activate → mock trả `statusCode 1 (OK)`.
- Mọi thao tác đều tự động ghi log vào file `%LOCALAPPDATA%\Module.VideoWall.WPF\Logs\session_*.jsonl`.

---

## MockServer giả lập được gì

| | Nội dung |
|---|---|
| ✅ Có | `Security/userCheck`, `DisplayDev/capabilities` + `.../VideoWall/capabilities`, `DisplayDev/VideoWall` (2 wall), `{id}/outputs` (2), `Video/inputs/channels` (2), windows CRUD (GET trả 2 cửa sổ), `scene/{id}/activate` · `/saveData` · `/isRunning`, `ScreenCtrl/closeAll`, Serial transparent, Digest 401→auth, + 116 route preset |
| ✅ Case lỗi (chỉ bật được **trong code test**, không qua HTTP) | `SimulateBadParameters`, `SimulateInvalidOperation`, `SimulateDeviceFailure`, `SimulateUnreachable`, `SimulateSaveDataFailure`, `SimulateNonceExpiry`, `FailedAuthLockoutThreshold` (khoá IP), hạ `MaxSceneNums` để test chặn SID |
| ⚠️ Khác thiết bị thật DS-C30S-S11 | `maxWindowNums = 512` (thật: **16**) · realm `DS-C66S-H88-CL` · chỉ **2** wall / **2** output / **2** input (thật: 8 wall, 12 output) · số liệu là ground-truth của **DS-C66S-H88-CL**, không phải DS-C30S-S11 |
| ❌ Không có | hình thật trên tường, độ trễ giải mã, băng thông, genlock |
