# Plan: Gom VwPermissionService + fix 6 vấn đề tồn đọng VideoWall (sau khi đối chiếu 127 file NATS redesign)

## Context

Phiên trước (2026-09-11) đã viết plan `videowall-device-nats-full-plan.md` (NATS-hoá truy cập
thiết bị) + 5 memory ghi quyết định. Việc code được giao "bạn khác" làm (đúng
[[feedback-plan-then-handoff-local-files]]). Phiên này đối chiếu **127 file thay đổi** (121 file
staged ở `TA-ITS015-WEBAPI-V1.0` branch `feat/20260909-videowall-device` + 6 file staged ở repo cha
`D:\ThienAn`) bằng cách đọc THẬT code (đúng [[feedback-verify-root-cause-before-fix]]).

**Kết quả đối chiếu plan chính**: mục 1-5 của `videowall-device-nats-full-plan.md` (hạ tầng NATS
request-reply) **đã implement đủ và đúng 100%**. Trong 7 outstanding issues của
[[videowall-outstanding-issues-20260911]], mục 5 (DI `IVwDeviceProxyDispatcher`) đã **RESOLVED**
(đăng ký `AddScoped` explicit). **6 mục còn lại + 1 việc gom code là phạm vi plan fix này.**

**Phát hiện quan trọng về kiến trúc phân quyền** (đào sâu theo yêu cầu người dùng, đối chiếu lại
`2026-09-11-videowall-script.md`):
- VideoWall có 2 cơ chế phân quyền, ra đời **2 thời điểm khác nhau, cho 2 mục đích khác nhau**:
  - `VwOrgAccessService` (+ `VwOrgScope`) — commit `fix/20260809-videowall` (10/08/2026, **trước
    họp 1 tháng**), mục đích gốc "phân quyền điều khiển controller": chặn đơn vị A sửa/xoá cấu hình
    Controller/Scene/Source/Screen thuộc đơn vị B. Chỉ có ĐÚNG 1 cột thật trong DB là
    `VwController.OrgId` — `VwOrgScope` KHÔNG phải bảng, chỉ là object tính toán tại runtime, cache
    lại 1 lần/request nhờ vòng đời DI `Scoped` (field `_scope`, instance mới mỗi HTTP request).
  - `VwUserAreaAccessService` (+ bảng `VwUserAreaPermission`) — tạo 10-11/09/2026, ĐÚNG theo yêu
    cầu họp §1.3: bảng mới `UserId`/`OrgId`/`Config` (ô lưới màn hình), User ưu tiên hơn Org.
  - `VwWindowSceneCommandHandler.cs` dùng CẢ HAI song song (dòng 105-106 + dòng 305) — xác nhận 2
    cơ chế bổ sung nhau, không phải làm trùng việc.
- **Quyết định của người dùng**: dù 2 cơ chế phục vụ mục đích khác nhau và ra đời khác thời điểm,
  vẫn **gom thành 1 class duy nhất `VwPermissionService`** để giảm phân mảnh (mọi handler chỉ cần
  inject 1 chỗ thay vì nhớ tách `_orgAccess`/`_userAreaAccess`). Đây là gom CODE, **không đổi
  schema DB** (không có bảng nào để gom — Tầng 1 vốn không có bảng riêng).

**2 phát hiện gap bảo mật MỚI** (ngoài 6 issue đã biết trong memory):
1. `VwUserAreaPermissionController` **không gọi `_orgAccess` ở bất kỳ đâu** (grep 0 kết quả) — không
   chỉ phần ĐỌC public như outstanding issue #3 mô tả, mà **lệnh GHI (Add/Update) cũng chưa hề bị
   chặn theo org** — user org A hiện tạo được bản ghi phân quyền cho org B.
2. Field `Device` (adhoc IP/mật khẩu, dùng khi Controller chưa khai báo — chỉ có ở endpoint
   `SendPassthrough`, xem phát hiện về `VwDeviceController` bên dưới) không tra CSDL nên không có
   `OrgId` để check quyền. **Người dùng đã xác nhận**: giới hạn nhánh này chỉ cho SuperAdmin.

**Phát hiện làm THAY ĐỔI phạm vi Việc 4** (tra code client thật, không chỉ tin docstring): trong 23
endpoint diagnostic của `VwDeviceController.cs`, **22/23 KHÔNG có client nào gọi qua WebAPI**.
- Web FE: 0 thay đổi (đã xác nhận từ đầu phiên).
- WPF (`ITS.VideoWall.WPF`): `ViewModels/Isapi/VwIsapiFormViewModel.cs` dùng
  `Module.VideoWall.WPF.Api.Direct.Isapi` — tức WPF tự gọi THẲNG thiết bị qua `VwDirectISAPIClient.cs`,
  **bỏ qua hoàn toàn WebAPI/NATS/Worker** cho việc chẩn đoán. Không có `VwDeviceDto.cs` nào trong
  `Api/Dto/` của WPF.
- Đối chiếu docstring gốc: 23 endpoint này vốn là **công cụ debug thủ công qua Swagger** (có trước
  khi WPF tồn tại), không phải tính năng app nào yêu cầu — nay đã bị WPF's "Direct ISAPI" thay thế,
  trùng lặp chức năng.
- **Ngoại lệ duy nhất**: action `DeviceProxyInputChannels` (1/23) **VẪN được dùng thật** — không
  phải qua `VwDeviceController`, mà qua **endpoint riêng** `VwControllerController.GetInputChannels`
  (`VwControllerQueryHandler.cs:124-137`, docstring trích dẫn **"§9.4 HANDOV_1.MD"** — yêu cầu
  nghiệp vụ có thật). Endpoint này **ĐÃ có org-scope** (`_orgAccess.EnsureOrgAccessAsync(entity.OrgId)`
  dòng 129, chạy TRƯỚC khi gọi NATS) — không cần fix gì thêm cho nó.
- **Quyết định người dùng**: xoá hẳn 22 endpoint diagnostic không dùng (giữ lại hạ tầng cho action
  `InputChannels` vì còn 1 consumer thật). Xem chi tiết ở Việc 4 bên dưới (đã viết lại hoàn toàn).

---

## Việc 0 (làm TRƯỚC, nền tảng cho các việc sau): Gom `VwOrgAccessService` + `VwUserAreaAccessService` → `VwPermissionService`

**File mới**: `Module.VideoWall/Infrastructure/Services/Access/VwPermissionService.cs`
**Xoá sau khi gom xong**: `VwOrgAccessService.cs`, `VwUserAreaAccessService.cs` (giữ nguyên
`VwOrgScope.cs` — vẫn là object nội bộ được `VwPermissionService` dùng).

1. Gộp toàn bộ field/constructor 2 class cũ (`BaseRepository<VwController/VwScreen/VwSource/VwScene/VwUserAreaPermission>`,
   `UserManager`, `IOptions<VwDeviceOptions>`, `ILogger`) vào 1 constructor của `VwPermissionService`,
   giữ `: IScoped` (bắt buộc để cache-per-request hoạt động như cũ).
2. Copy nguyên xi toàn bộ method public của `VwOrgAccessService` (`EnsureControllerAccessAsync`,
   `EnsureScreenAccessAsync`, `EnsureSourceAccessAsync`, `EnsureSceneAccessAsync`,
   `EnsureSceneControllerAccessAsync`, `EnsureOrgAccessAsync`, `ResolveOrgIdAsync`,
   `ResolveSceneControllerIdAsync`, `IsFullAccess`, `CurrentOrgId`, `GetScopeAsync`) — KHÔNG đổi
   logic, chỉ đổi tên class chứa chúng.
3. Copy nguyên xi toàn bộ method của `VwUserAreaAccessService`
   (`EnsureWindowInsideUserAllowedAreaAsync`, `GetCoveredCells`, `IsWithinAllowedCells`) vào cùng
   class, đặt dưới 1 `#region Tầng 3 — khu vực màn hình` cho dễ đọc (2 nhóm method vẫn tách bạch về
   mặt đọc code, chỉ chung 1 điểm inject).
4. Cập nhật constructor + field ở **15 file đang inject 1 trong 2 class cũ** — đổi tên type +
   field (vd `_orgAccess`/`_userAreaAccess` → `_permission`), KHÔNG đổi cách gọi method (chỉ đổi
   nơi gọi từ `_orgAccess.X()`/`_userAreaAccess.Y()` thành `_permission.X()`/`_permission.Y()`):
   `VwControllerCommandHandler`, `VwControllerQueryHandler`, `VwEventRuleCommandHandler`,
   `VwEventRuleQueryHandler`, `VwSceneCommandHandler`, `VwSceneWorkflowCommandHandler`,
   `VwSceneQueryHandler` (chỉ có comment nhắc tới, không inject — sửa comment),
   `VwScheduleCommandHandler`, `VwScheduleQueryHandler`, `VwScreenCommandHandler`,
   `VwScreenQueryHandler`, `VwSlotPortCommandHandler`, `VwSlotPortQueryHandler`,
   `VwSourceCommandHandler`, `VwSourceQueryHandler`, `VwWindowSceneCommandHandler` (đang inject CẢ
   HAI — gộp thành 1 field), `VwWindowSceneQueryHandler` (chỉ comment), `VwEventTriggerLogQueryHandler`.
5. Chuyển docstring checklist `[BẢO MẬT & BÀN GIAO PRODUCTION]` (hiện ở đầu `VwOrgAccessService.cs:29-42`)
   sang đầu `VwPermissionService.cs`, cập nhật theo Việc 3 bên dưới.
6. Không cần sửa DI registration thủ công nếu `IScoped` đang được convention-scan tự động (xác
   nhận lại khi implement: grep `AddScoped.*VwOrgAccessService|AddScoped.*VwUserAreaAccessService`
   trong `ServiceCollectionExtensions.cs` — nếu có dòng explicit thì đổi tên type trong đó).

**Lưu ý phạm vi**: việc gom này đụng tới ~17 file (2 file gộp + 15 file consumer), phần lớn là đổi
tên cơ học (type + field), không đổi logic nghiệp vụ — nhưng vẫn nên build lại toàn bộ
`Module.VideoWall.csproj` sau khi gom xong trước khi làm tiếp Việc 1-6.

---

## Việc 1. `VwUserAreaPermissionCommandHandler` Update thiếu duplicate-check actor
**File**: `Module.VideoWall/Controllers/UserAreaPermission/Commands/VwUserAreaPermissionCommandHandler.cs`
Add (~dòng 42-61) có check trùng actor, Update (~dòng 70-81) không có — cho phép Update đổi
UserId/OrgId sang actor đã có bản ghi active khác.

**Fix**: tách logic duplicate-check trong Add thành 1 private method dùng chung, ví dụ
`EnsureNoActiveDuplicateAsync(string? userId, string? orgId, string? excludeId = null)`. Update gọi
method này với `excludeId = command.ID` trước khi lưu.

## Việc 2. UserId/OrgId chưa `.Trim()` trước khi so sánh/lưu
**File**: cùng file trên, dòng 42, 61, 77.
**Fix**: `.Trim()` `UserId`/`OrgId` ngay khi đọc từ `command`, TRƯỚC duplicate-check ở Việc 1 — áp
dụng cho cả Add và Update.

## Việc 3. `VwUserAreaPermissionController` — vừa thiếu checklist, vừa thật sự chưa có org-scope
**Files**: `VwUserAreaPermissionCommandHandler.cs` + `VwPermissionService.cs` (checklist, sau khi
gom ở Việc 0).

**Fix**:
1. Inject `VwPermissionService _permission` vào `VwUserAreaPermissionCommandHandler`.
2. Add/Update: nếu `command.OrgId` có giá trị → gọi `_permission.EnsureOrgAccessAsync(command.OrgId)`
   (chặn nhắm sang org khác, SuperAdmin bypass qua `IsFullAccess`).
3. Nếu chỉ có `UserId` (không `OrgId`) → **cần xác nhận thêm quy tắc nghiệp vụ**: có bắt buộc
   target UserId cùng org người gọi không, tra org của UserId đó bằng cách nào — đánh dấu ĐIỂM CẦN
   CHỐT LẠI khi review, chưa đủ thông tin để khoá cứng thuật toán trong plan này.
4. Thêm `VwUserAreaPermissionController` vào checklist gỡ `[AllowAnonymous]` (mục 11) trong
   `VwPermissionService.cs`.

## Việc 4. Xoá 22 endpoint diagnostic không dùng + fix org-scope cho 6 endpoint Setup wizard (còn dùng thật)

### 4a. Xoá `VwDeviceController` + hạ tầng 22/23 action không còn consumer
**Files xoá**:
- `Module.VideoWall/Controllers/Device/` — toàn bộ (`VwDeviceController.cs`,
  `Queries/VwDeviceQueryHandler.cs`, `Commands/VwDeviceCommandHandler.cs`, `Validators/VwDeviceValidator.cs`)
- `Module.VideoWall.Core/Dto/Device/VwDeviceInput.cs` — xoá HẾT class TRỪ `VwDeviceTargetInput`
  (base) và `VwDeviceInputChannelsInput` (2 class này giữ lại, `InputChannels` vẫn cần)
- `Services/VideoWall/ITS.VideoWall/Consumer/DeviceProxy/VwDeviceProxyCommandDispatcher.cs` — xoá
  toàn bộ (11 method, không còn action nào dùng)
- `Services/VideoWall/ITS.VideoWall/Consumer/DeviceProxy/VwDeviceProxyQueryDispatcher.cs` — xoá 11/12
  method, GIỮ LẠI `InputChannels`
- `Module.VideoWall.Core/Constants/VwCommandActions.cs` — xoá 22 hằng số `DeviceProxy*`, GIỮ
  `DeviceProxyInputChannels`
- `IVwDeviceProxyDispatcher.cs`, `VwDeviceHandlerRunner.cs` (Worker) — **cân nhắc xoá luôn cả 2**:
  hạ tầng dispatcher này tồn tại để chạy chung cho 23 method, giờ chỉ còn 1 method
  (`InputChannels`) thì nên inline thẳng thành 1 case thứ 7 trong `VwCommandConsumer.cs` (giống
  hình dạng 6 case `DeviceSetup*` đã có), gọi thẳng `IVwISAPIDeviceClient.GetInputChannelsAsync(...)`
  — bỏ hẳn lớp trừu tượng dispatcher cho 1 method duy nhất. Đây là gợi ý, người implement có thể
  giữ dispatcher nếu thấy code gọn hơn.
- `VwCommandConsumer.cs` — bỏ nhánh `default: if (proxyDispatcher.CanHandle(...))`, thay bằng 1
  case `DeviceProxyInputChannels` trực tiếp (nếu theo gợi ý trên) + khôi phục nhánh
  `default:` gốc (chỉ log "Unsupported action").
- **KHÔNG xoá**: `VwISAPIResult`, `IVwISAPIDeviceClient`, `IVwISAPIDeviceService`, `VwDeviceGenericOutput<T>`
  (còn dùng cho `InputChannels` + toàn bộ ISAPIDevice service — theo Việc 4b/4c dưới), 6 endpoint
  Setup wizard và mọi thứ nó dùng.

**Không cần cập nhật checklist AllowAnonymous cho `VwDeviceController`** — vì controller này bị xoá,
không còn tồn tại để đưa vào checklist.

**Lưu ý bàn giao**: đây là REVERT MỘT PHẦN phần việc "23 endpoint diagnostic" trong
`videowall-device-nats-full-plan.md` (đã review là "đã implement đúng" trước đó) — có căn cứ rõ
ràng (0 consumer thật, WPF đã tự làm bằng đường Direct ISAPI khác), không phải revert tuỳ tiện. Nên
cập nhật lại [[videowall-nats-device-access-redesign]] sau khi xoá xong (từ "29 action" xuống còn
"7 action": 6 DeviceSetup + 1 InputChannels).

### 4b. Fix org-scope thật cho 6 endpoint Setup wizard (WPF đang gọi thật, xem `VideoWallApiClient.cs:199,217,232,247`)
**File**: `Module.VideoWall/Controllers/DeviceSetup/Commands/VwDeviceSetupCommandHandler.cs`

Xác nhận (đọc code thật): handler gọi thẳng `requestClient.RequestAsync(...)`, KHÔNG gọi check
quyền nào — org-scope bị rớt hoàn toàn khi chuyển sang NATS. Trong 6 input DTO
(`VwDeviceSetupInput.cs`), chỉ **`VwISAPIPassthroughInput`** (endpoint `SendPassthrough`, WPF gọi ở
`vwdevicesetup/isapi`) có nhánh adhoc `Device` (IP/mật khẩu, không qua CSDL); 5 input còn lại
(`VwPingDeviceInput`, `VwProbeDeviceInput`, `VwSyncSourcesInput`, `VwSetupSceneInput`,
`VwResetCircuitBreakerInput`) LUÔN có `ControllerId`/`ID` — không có nhánh adhoc.

**Fix**:
1. Inject `VwPermissionService _permission` vào `VwDeviceSetupCommandHandler`.
2. Với 5 method có `ID`/`ControllerId` bắt buộc: gọi `await _permission.EnsureControllerAccessAsync(input.ID)`
   (hoặc `ControllerId` tuỳ tên field) trước khi `RequestAsync`.
3. Với `SendPassthrough` (`VwISAPIPassthroughInput`):
   ```csharp
   if (!string.IsNullOrWhiteSpace(input.ControllerId))
       await _permission.EnsureControllerAccessAsync(input.ControllerId);
   else if (!_permission.IsFullAccess) // nhánh adhoc Device — chỉ SuperAdmin
       throw Oops.Oh(BaseLocaleManager.BaseException.NoPermission);
   ```
4. Cập nhật checklist trong `VwPermissionService.cs`: sửa ghi chú mục `VwDeviceSetupController`
   (đang ghi "mở lại _orgAccess" — sai, logic ISAPI giờ chạy trong Worker) → đúng vị trí check mới
   (tại WebAPI handler, theo fix mục 2-3 trên).

### 4c. Dọn 3 file ISAPI nằm sai chỗ (không đổi do Việc 4a — vẫn cần vì Worker + `InputChannels` còn dùng)
**Files** (di chuyển từ `Module.VideoWall.Core` sang `ITS.VideoWall`, sửa `namespace`/`using` ở nơi
Worker đang import — đã grep xác nhận `Module.VideoWall` 0 tham chiếu tới cả 3):
- `Module.VideoWall.Core/Dto/ISAPI/VwISAPIResult.cs` (record `VwISAPIResult`/`VwISAPIResult<T>`)
- `Module.VideoWall.Core/Interfaces/IVwISAPIDeviceClient.cs`
- `Module.VideoWall.Core/Interfaces/IVwISAPIDeviceService.cs`

Lưu ý: các DTO khác cùng thư mục `Dto/ISAPI/` (`VwISAPIResponse.cs`, `VwISAPISceneResponse.cs`,
`VwISAPIWallResponse.cs`, `VwISAPIWindowRequest.cs`) và `VwDeviceGenericOutput<T>` **KHÔNG di
chuyển** — vẫn là hợp đồng dùng chung thật giữa WebAPI (`InputChannels`) và Worker.

Cũng dọn code chết liên quan (không đổi do Việc 4a):
- `Services/VideoWall/ITS.VideoWall/Services/ISAPIDevice/VwISAPIDeviceService.cs:67` — tham số
  `object? orgAccess` không dùng trong constructor.
- `Services/VideoWall/ITS.VideoWall/Services/ISAPIDevice/VwISAPIDeviceService.DeviceSetup.cs:770-774,795-796`
  — 2 khối comment check quyền cũ, không còn compile được nếu mở lại.
- `VwEventTriggerLogCommandHandler.cs:16` — doc-comment `<see cref="...VwISAPIDeviceService.DeviceSetup.cs"/>`
  đứt (type giờ chỉ còn ở Worker, `Module.VideoWall` không reference `ITS.VideoWall` nữa) → đổi
  thành text thường.

## Việc 5. `ITS.VideoWall.Worker/appsettings.json` thiếu subject `.scene`
**File**: `Services/VideoWall/ITS.VideoWall.Worker/appsettings.json` (`Nats.Streams[0].SubjectsList`).
**Fix**: thêm `"ta.its.data.videowall.scene"` cho khớp `TAC_WebAPI/Configuration/DataTransporter.json`.
Không gây lỗi hiện tại nhưng nên tường minh, tránh lệch cấu hình 2 phía về sau.

## Việc 6. Heartbeat định kỳ (tính năng MỚI, chưa có gì để sửa — cần thiết kế từ đầu)
Yêu cầu gốc: `2026-09-11-videowall-script.md:103` — *"Service bên dưới định kỳ lấy trạng thái từ
thiết bị, ghi log/CSDL, đồng thời bắn message qua NATS để WebAPI và giao diện Web cập nhật
real-time trạng thái"*. Không có số chu kỳ cụ thể trong biên bản họp.

**Thiết kế đề xuất** (tái dùng pattern có sẵn trong `ITS.VDS.Core`):
1. **Data model**: thêm 2 cột vào `VwController`: `LastSeenAt (DateTime?)`,
   `ConnectionStatus (bool?/enum)` — TÁCH RIÊNG khỏi cột `Status` (cờ Enable/Disable admin set tay).
   **Không** tái dùng `VwEventTriggerLog` (log append-only cho hành động rời rạc, ghi mỗi N giây
   mãi mãi vào đó sai mục đích bảng). Viết migration `.sql` riêng, không tự chạy.
2. **Worker mới**: `VwHeartbeatWorker : BackgroundService` trong `Services/VideoWall/ITS.VideoWall/HeartbeatMonitor/`.
   Copy hình dạng `CountPublishWorker.cs` (`ITS.VDS.Core/Features/Counting/Publishing`) — vòng lặp
   `while (!ct.IsCancellationRequested) { poll; update DB; publish; await Task.Delay(interval, ct); }`,
   try/catch mỗi vòng, `OperationCanceledException` là dừng sạch.
3. **Poll logic**: mỗi `VwController` đang `Status = Enable`, gọi trực tiếp
   `IVwISAPIDeviceService.Ping`/`Probe` đã có sẵn (Worker đã sở hữu `IVwISAPIDeviceClient` tại chỗ,
   không cần qua NATS) → cập nhật `LastSeenAt`/`ConnectionStatus`.
4. **Subject NATS mới**: thêm vào `VwSubjects.cs`, ví dụ `Status = "ta.its.data.videowall.status"` —
   broadcast 1 chiều xuống FE giống `.scene`.
5. **Chu kỳ**: thêm `HeartbeatIntervalSeconds` vào `VwDeviceOptions`, đề xuất mặc định **15s** (theo
   tiền lệ `ZoneStatusJob` trong `ITS.VDS.Core`) — **cần xác nhận lại với anh Sơn**, biên bản họp
   không chốt số.
6. DI: 1 dòng `services.AddHostedService<VwHeartbeatWorker>()` trong
   `ITS.VideoWall/Extensions/ServiceCollectionExtensions.cs`.

---

## Việc KHÔNG làm trong plan này

- Không tự động gỡ `[AllowAnonymous]` khỏi controller nào — plan này chỉ làm cho việc gỡ nó AN
  TOÀN sau này, không tự quyết định thời điểm gỡ.
- Không tự chọn thuật toán check quyền cho case `VwUserAreaPermission` chỉ có `UserId` (Việc 3.3).
- Không tự chốt chu kỳ heartbeat (Việc 6.5) — đề xuất mặc định 15s, cần xác nhận.
- Không build/chạy code — plan cho người khác implement, theo [[feedback-plan-then-handoff-local-files]].
- Không đổi schema/nguồn cấp claim `OrgId` (JWT, module Hệ thống ngoài phạm vi VideoWall) — VideoWall
  chỉ tiêu thụ claim này.

## Critical files

- `Module.VideoWall/Infrastructure/Services/Access/VwPermissionService.cs` (mới, Việc 0) — thay thế `VwOrgAccessService.cs` + `VwUserAreaAccessService.cs`
- `Module.VideoWall/Controllers/UserAreaPermission/Commands/VwUserAreaPermissionCommandHandler.cs` — Việc 1,2,3
- `Module.VideoWall/Controllers/Device/` (toàn bộ, XOÁ ở Việc 4a), `Services/VideoWall/ITS.VideoWall/Consumer/DeviceProxy/` (11+11 method xoá, giữ `InputChannels`), `Controllers/DeviceSetup/Commands/VwDeviceSetupCommandHandler.cs` (fix org-scope Việc 4b) — Việc 4
- `Services/VideoWall/ITS.VideoWall/Services/ISAPIDevice/VwISAPIDeviceService.cs`, `VwISAPIDeviceService.DeviceSetup.cs` — dọn code chết Việc 4c
- `Services/VideoWall/ITS.VideoWall.Worker/appsettings.json` — Việc 5
- `Module.VideoWall.Core/Entities/VwController.cs`, `Module.VideoWall.Core/Constants/VwSubjects.cs`, `Module.VideoWall.Core/Options/VwDeviceOptions.cs`, `Services/VideoWall/ITS.VideoWall/Extensions/ServiceCollectionExtensions.cs` — Việc 6
- 15 file consumer cần đổi tên type/field khi gom Việc 0 (liệt kê đầy đủ ở mục "Việc 0" trên)
- Pattern tham khảo (không sửa): `src/Services/VDS/ITS.VDS.Core/Features/Counting/Publishing/CountPublishWorker.cs`

## Verification (cho người implement chạy sau khi code xong)

1. Build lại `Module.VideoWall.csproj` + `ITS.VideoWall.csproj` + `ITS.VideoWall.Worker.csproj` + `TAC_WebAPI` sau Việc 0 (bắt lỗi đổi tên sót) và sau Việc 1-6.
2. Test Việc 1,2: tạo 2 bản ghi `VwUserAreaPermission` cùng actor (1 Add, 1 cố Update trùng) — phải
   bị chặn; UserId toàn khoảng trắng — phải bị coi như rỗng.
3. Test Việc 4: xác nhận `VwDeviceController` không còn tồn tại (22 route diagnostic cũ trả 404);
   `VwControllerController.GetInputChannels` vẫn hoạt động bình thường (đã có org-scope từ trước,
   không đổi hành vi). Gọi 1 trong 6 endpoint Setup wizard qua WPF với `ControllerId` thuộc org
   khác — nhận lỗi `NoPermission`; gọi `SendPassthrough` bằng nhánh `Device` (adhoc) với tài khoản
   không phải SuperAdmin — bị chặn; SuperAdmin gọi được cả 2 nhánh.
4. Test Việc 6: tắt 1 controller thật, xác nhận `LastSeenAt` ngừng cập nhật sau đúng
   `HeartbeatIntervalSeconds`, FE nhận message NATS `.status` mới.
5. Grep `IVwISAPIDeviceService|IVwISAPIDeviceClient` trong `Module.VideoWall/` — vẫn phải 0 kết quả.
6. Grep `VwOrgAccessService|VwUserAreaAccessService` toàn repo sau Việc 0 — phải 0 kết quả (đã gom hết vào `VwPermissionService`).
