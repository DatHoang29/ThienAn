# Plan: NATS-hóa toàn bộ truy cập thiết bị VideoWall từ WebAPI, xoá ProjectReference ngược

> Ngày lập: 11/09/2026. Nguồn: review 113 file thay đổi (permission CRUD + NATS worker sync),
> phát hiện `Module.VideoWall.csproj` ProjectReference ngược tới `ITS.VideoWall` (Service).

## Context

Review 113 file thay đổi (permission CRUD + NATS worker sync đã implement trước đó) phát hiện
`Module.VideoWall.csproj` có 1 `ProjectReference` NGƯỢC tới `..\..\..\Services\VideoWall\ITS.VideoWall\ITS.VideoWall.csproj`
— WebAPI (Module) phụ thuộc Worker (Service), sai hướng chuẩn (Service nên phụ thuộc shared
Core, không phải ngược lại — đúng pattern `ITS.VDS.Core` đang làm). Đào sâu nguyên nhân:

- Lý do duy nhất: `Module.VideoWall`'s `ServiceCollectionExtensions.cs` đăng ký DI trực tiếp 4
  class ISAPI (`VwISAPICredentialResolver`, `VwISAPIDigestHandler`, `VwISAPIDeviceClient`,
  `VwISAPIDeviceService`) mà implementation vật lý nằm trong `ITS.VideoWall`. WebAPI cần chúng vì
  **2 nhóm code đang gọi thẳng ISAPI thiết bị, không qua NATS**:
  1. **Device Setup wizard** (5-6 thao tác, `VwDeviceSetupCommandHandler.cs`) — luồng nghiệp vụ
     admin pair 1 controller mới.
  2. **Diagnostic proxy** (23 endpoint, `VwDeviceController.cs`/`VwDeviceQueryHandler.cs`/
     `VwDeviceCommandHandler.cs`, qua `VwDeviceHandlerRunner.cs`) — endpoint 1:1 với ISAPI thô,
     vốn `[AllowAnonymous]` có chủ ý để kỹ thuật viên test qua Swagger/Postman tại hiện trường.

- Đã xác nhận với người yêu cầu: **cả 2 nhóm đều chuyển sang NATS (Worker thực thi)**, không giữ
  ngoại lệ nào — kể cả nhóm diagnostic (dù nó có lý do chính đáng để gọi thẳng), vì việc test
  hiện trường trực tiếp sẽ chuyển sang dùng WPF client riêng thay vì Swagger vào WebAPI.

- Kết quả: sau khi xong, `Module.VideoWall` không còn lý do gì cần biết tới `IVwISAPIDeviceService`/
  `IVwISAPIDeviceClient` nữa → xoá thẳng 5 dòng DI + `ProjectReference` ngược, không phải "chuyển
  file vào Core để dùng chung" như phương án ban đầu.

**Không đụng luồng NATS vận hành đã có** (`ActivateScene`/`SyncSceneWindows`/`SetWindowLayer`/
`ResetCircuitBreaker`, `VwCommandConsumer`/`VwTelemetryConsumer` hiện tại) — chỉ thêm cơ chế
request-reply MỚI chạy song song.

---

## 1. Cơ chế NATS request-reply tổng quát (hạ tầng mới, dùng cho cả 29 thao tác)

### Quyết định kiến trúc quan trọng: không tạo subscription NATS thứ 2

Đã đọc `BaseDataTransporter.ConsumeData`/`NatsEventHandler.SubscribeData` — mỗi lần gọi
`ConsumeData(subject, cb)` tạo THÊM 1 subscription NATS vật lý cho subject đó (cộng dồn delegate,
không thay thế). Nếu tạo 1 class mới tự `ConsumeData(VideoWallResponse, ...)` sẽ có 2
subscription trùng → mỗi message bị `VwTelemetryConsumer.ProcessTelemetryAsync` xử lý 2 lần vĩnh
viễn. **`VwTelemetryConsumer` phải giữ vai trò chủ sở hữu DUY NHẤT** của subscription
`ta.its.data.videowall.response`; registry request-reply mới là 1 class riêng (SRP), nhận dữ
liệu qua `VwTelemetryConsumer` gọi vào, không tự subscribe.

### File mới
1. `Module.VideoWall.Core/Interfaces/IVwNatsRequestClient.cs` — interface phía caller (WebAPI
   handler dùng thay `_deviceService.X()`):
   ```csharp
   public interface IVwNatsRequestClient
   {
       Task<TResponse> RequestAsync<TResponse>(string action, object? payload,
           string? controllerId = null, string? sceneId = null, CancellationToken ct = default);
       Task RequestAsync(string action, object? payload,
           string? controllerId = null, string? sceneId = null, CancellationToken ct = default);
   }
   ```
2. `Module.VideoWall.Core/Interfaces/IVwNatsReplySink.cs` — interface phía `VwTelemetryConsumer`
   gọi vào khi nhận reply không thuộc nhánh `ActivateScene` sẵn có:
   ```csharp
   public interface IVwNatsReplySink
   {
       void Complete(string messageId, bool success, string? errorMessage, object? data);
       void SweepTimeouts();
   }
   ```
3. `Module.VideoWall/Infrastructure/Services/Messaging/VwNatsRequestClient.cs` — implement cả 2
   interface trên (singleton), sở hữu `ConcurrentDictionary<string, Pending>` (TaskCompletionSource
   theo MessageId). `RequestAsync`: đăng ký pending TRƯỚC rồi mới `publisher.PublishCommand(...)`
   (tránh race reply-về-trước-khi-đăng-ký), await với timeout theo action (`ResolveTimeout`),
   remove khỏi registry trong `finally`. `Complete`/`SweepTimeouts`: set kết quả/lỗi/timeout lên
   TCS tương ứng, bỏ qua êm nếu không khớp `MessageId` nào đang chờ.

### Sửa `VwTelemetryConsumer.cs` (tối thiểu, KHÔNG đụng nhánh `ActivateScene` hiện có)
- Constructor: inject thêm `IVwNatsReplySink replySink`.
- `ProcessTelemetryAsync`: sau khối `if (action == ActivateScene) {...}` hiện có, thêm
  `else { replySink.Complete(messageId, success, errorMessage, jObj["Data"]); }`.
- `ExecuteAsync`'s vòng lặp 1s: thêm `replySink.SweepTimeouts();` cạnh `CheckTimeouts()` sẵn có.

### Timeout — cấu hình tập trung theo action, không per-callsite
Thêm 3 field vào `VwDeviceConnectionOptions` (`Module.VideoWall.Core/Options/VwDeviceOptions.cs`,
cạnh `DeviceAckTimeoutSeconds` đã có): `NatsRequestTimeoutSeconds = 15` (mặc định, đa số action —
tối đa ~2 round-trip ISAPI kể cả `ResolveWall` phụ), `NatsSetupSceneTimeoutSeconds = 60`
(`DeviceSetupScene` — chuỗi ISAPI dài, N lần AddWindow), `NatsPassthroughTimeoutSeconds = 30`
(`DeviceSetupSendPassthrough` — request tuỳ ý, không đoán trước). `VwNatsRequestClient.ResolveTimeout(action)`
switch theo action, giữ caller code gọn (không truyền timeout ở từng call site).

### DI (`Module.VideoWall/Extensions/ServiceCollectionExtensions.cs`, hàm `AddVWInfrastructure`,
cạnh khối đăng ký `VwTelemetryConsumer` dòng 28-33)
```csharp
services.AddSingleton<VwNatsRequestClient>();
services.AddSingleton<IVwNatsRequestClient>(sp => sp.GetRequiredService<VwNatsRequestClient>());
services.AddSingleton<IVwNatsReplySink>(sp => sp.GetRequiredService<VwNatsRequestClient>());
```

---

## 2. `VwCommandActions.cs` — thêm 29 hằng số (6 Device Setup + 23 Diagnostic proxy)

**Nhóm Device Setup (6)**: `DeviceSetupPing`, `DeviceSetupProbe`, `DeviceSetupSyncSources`,
`DeviceSetupScene`, `DeviceSetupSendPassthrough` (map 1:1 tới 5 method của `IVwISAPIDeviceService`
đã có: `Ping/Probe/SyncSources/SetupScene/SendPassthrough`), và `DeviceSetupResetCircuitBreaker`
(action MỚI hoàn toàn — không tái dùng `VwCommandActions.ResetCircuitBreaker` đã có vì khác ngữ
nghĩa: cái cũ nhận `TargetIpOrKey` thô; cái này nhận `ControllerId`, tự tra IP + gọi thêm
`ForgetWall`, copy nguyên thân `VwDeviceSetupCommandHandler.HandleAsync(VwResetCircuitBreakerInput)`).

**Nhóm Diagnostic proxy (23)**: `DeviceProxy<TênEndpoint>` — `UserCheck, Capabilities, Walls,
Outputs, InputChannels, OutputChannels, StreamChannels, SceneList, SceneRunning, SceneInfo,
SceneCreate, SceneRename, SceneSave, SceneActivate, WindowList, WindowGet, WindowAdd,
WindowUpdate, WindowDelete, WindowDeleteAll, WindowTop, WindowBottom, WindowStartDecode`. Lưu ý
`DeviceProxySceneActivate` (kích hoạt theo Sid ISAPI thô) **khác** `VwCommandActions.ActivateScene`
(kích hoạt theo `VwScene.ID` nghiệp vụ, luồng cũ không đụng) và khác `DeviceSetupScene` (wizard
dựng+lưu+kích hoạt trọn gói).

---

## 3. Phía Worker — dispatch 29 action mới

### Nhóm Device Setup (6) — inline trong `VwCommandConsumer.cs`
Thêm 6 `case` + 6 method `HandleDeviceSetupXxxAsync`, đúng hình dạng 4 case hiện có (extract
payload → gọi `deviceService.X(...)` → `PublishTelemetry`). Các method nghiệp vụ đã tồn tại sẵn
và đã tự ghi `VwEventTriggerLog` qua `TrackAndLog` bên trong `VwISAPIDeviceService.DeviceSetup.cs`
— không cần thêm gì cho phần log.

### Nhóm Diagnostic proxy (23) — chuyển `VwDeviceHandlerRunner` sang Worker
`VwDeviceHandlerRunner.cs` (106 dòng, hiện ở `Module.VideoWall/Controllers/Device/`) **chuyển
nguyên sang `ITS.VideoWall/Consumer/DeviceProxy/VwDeviceHandlerRunner.cs`** — giữ nguyên 100%
pattern `RunAsync(input, stepName, method, endpoint, lambda, ct)`, chỉ khác giờ lambda viết ngay
tại Worker (không cần serialize qua NATS nữa — vấn đề "lambda không serialize được" biến mất vì
lambda giờ chạy đúng nơi có `IVwISAPIDeviceClient`). 3 dependency của nó
(`IVwISAPIDeviceClient`, `IVwISAPIDeviceService`, `BaseRepository<VwController>`) đã đăng ký sẵn
trong Worker DI.

Tạo 2 file mirror 1:1 `VwDeviceQueryHandler.cs`/`VwDeviceCommandHandler.cs` cũ (đổi
`HandleAsync(TInput, ct)` → method thường, không còn là Wolverine handler):
- `ITS.VideoWall/Consumer/DeviceProxy/VwDeviceProxyQueryDispatcher.cs` (12 method).
- `ITS.VideoWall/Consumer/DeviceProxy/VwDeviceProxyCommandDispatcher.cs` (11 method).
- `ITS.VideoWall/Consumer/DeviceProxy/IVwDeviceProxyDispatcher.cs` — interface hợp nhất:
  `bool CanHandle(string action)`, `Task<object?> DispatchAsync(string action, object? rawPayload, CancellationToken ct)`.

20/23 method là copy gần nguyên văn thân handler cũ (chỉ đổi input type/endpoint string theo bảng
mục 2) — ví dụ mẫu:
```csharp
public async Task<VwDeviceGenericOutput<VwISAPICapabilitiesResponse>> Capabilities(VwDeviceCapabilitiesInput input, CancellationToken ct)
{
    var (result, step) = await run.RunAsync<VwISAPICapabilitiesResponse>(
        input, "GetCapabilities", "GET", _ => "ISAPI/DisplayDev/VideoWall/capabilities",
        (client, c, _, t) => client.GetCapabilitiesAsync(c, t), ct);
    return new() { Data = result.Data, Step = step };
}
```
**`DeviceProxySceneInfo` là case duy nhất giữ logic map riêng** — copy nguyên khối dòng 124-161
của `VwDeviceQueryHandler.cs` cũ (build `VwDeviceSceneInfoOutput` từ
`VwISAPISceneInfoResponse.WallSceneInfo.SceneWindowList`), không generic-wrap.

**Bắt buộc**: chuyển `VwDeviceGenericOutput<T>` (hiện lồng trong `VwDeviceQueryHandler.cs` cũ,
project WebAPI-only) sang `Module.VideoWall.Core/Dto/Device/VwDeviceOutput.cs` — nếu không, dispatcher
ở Worker không biên dịch được vì không thấy type này.

### Sửa `VwCommandConsumer.ProcessCommandAsync` — 1 nhánh mới, không sửa 4 case cũ
Đổi `default:` hiện có (case 6 Device Setup thêm bình thường vào switch):
```csharp
default:
    if (proxyDispatcher.CanHandle(envelope.Action))
    {
        var data = await proxyDispatcher.DispatchAsync(envelope.Action, envelope.Payload, ct);
        await PublishTelemetry(envelope.MessageId, envelope.Action, true, null, data, ct);
    }
    else { /* giữ nguyên log+PublishTelemetry(false, "Unsupported action") hiện có */ }
    break;
```
**Lưu ý ngữ nghĩa quan trọng**: `IVwISAPIDeviceClient` là "Never-throw" theo đúng doc comment gốc
— đa số 23 handler diagnostic không throw khi ISAPI lỗi, chỉ trả `Step.Success = false` trong dữ
liệu (giữ nguyên hành vi HTTP 200 hiện tại). `success: true` trong `PublishTelemetry` ở đây nghĩa
là "Worker tạo được response" (mức transport), không phải "lệnh ISAPI thành công" (mức nghiệp vụ,
nằm trong `Step`/`Data`) — khớp đúng hành vi gốc, không đổi.

---

## 4. Phía WebAPI — viết lại 29 caller theo 1 pattern chung

```csharp
// Trước:
public async Task<VwSetupSceneStep> HandleAsync(VwPingDeviceInput command, CancellationToken ct)
    => await _deviceService.Ping(command.ID, ct);

// Sau:
public async Task<VwSetupSceneStep> HandleAsync(VwPingDeviceInput command, CancellationToken ct)
    => await _requestClient.RequestAsync<VwSetupSceneStep>(
        VwCommandActions.DeviceSetupPing, command.ID, command.ID, ct: ct);
```
Áp dụng cho toàn bộ 6 method `VwDeviceSetupCommandHandler.cs` + 12 method `VwDeviceQueryHandler.cs`
+ 11 method `VwDeviceCommandHandler.cs` (tên action tương ứng ở mục 2). 2 file Query/Command đổi
constructor từ `(VwDeviceHandlerRunner run)` sang `(IVwNatsRequestClient requestClient)`.
**`VwDeviceController.cs`/`VwDeviceSetupController.cs` không cần sửa** — vẫn `MessBus.InvokeAsync<T>(input)`
như cũ, chỉ thân handler đổi cách lấy dữ liệu. Xoá
`Module.VideoWall/Controllers/Device/VwDeviceHandlerRunner.cs` cũ (đã chuyển sang Worker mục 3).

**EventTriggerLog**: không cần thiết kế gì thêm — 23 handler diagnostic hiện KHÔNG ghi
`VwEventTriggerLog` ở đâu cả, giữ nguyên hành vi này sau khi chuyển sang Worker.

---

## 5. Dọn dẹp phụ thuộc ngược (mục tiêu gốc)

1. Grep lại `IVwISAPIDeviceService|IVwISAPIDeviceClient` trong `Module.VideoWall/` (không phải
   `.Core`) — xác nhận chỉ còn 5 dòng DI, không còn handler nào tham chiếu.
2. Xoá 5 dòng DI tại `Module.VideoWall/Extensions/ServiceCollectionExtensions.cs` (dòng 38-43,
   hàm `AddVWInfrastructure`: `VwISAPICredentialResolver`, `VwISAPIDigestHandler`,
   `AddHttpClient<IVwISAPIDeviceClient,...>`, `IVwISAPIDeviceService`).
3. Xoá dòng `<ProjectReference Include="..\..\..\Services\VideoWall\ITS.VideoWall\ITS.VideoWall.csproj" />`
   tại `Module.VideoWall.csproj:20`.
4. Build `Module.VideoWall.csproj` độc lập, xác nhận không còn phụ thuộc `ITS.VideoWall`.

---

## Trình tự triển khai đề xuất

1. Mục 1 (hạ tầng request-reply) — build + test riêng trước khi đụng 29 caller.
2. Mục 2 (29 hằng số action).
3. Mục 3 (Worker: chuyển `VwDeviceHandlerRunner`, chuyển `VwDeviceGenericOutput<T>` sang Core,
   2 dispatcher mới, 6+1 case vào `VwCommandConsumer`).
4. Mục 4 (WebAPI: viết lại 29 caller, xoá `VwDeviceHandlerRunner.cs` cũ).
5. Mục 5 (dọn DI + ProjectReference ngược, build lại toàn solution).
6. Smoke test thủ công qua Swagger: 1 case Device Setup (`Ping`), 1 case diagnostic đơn giản
   (`Capabilities`), 1 case có mapping riêng (`SceneInfo`), 1 case timeout dài (`SetupScene`/
   `SendPassthrough`).

---

## Critical files

- `Module.VideoWall/Infrastructure/Services/Messaging/VwTelemetryConsumer.cs` — sửa tối thiểu, thêm reply-sink
- `Module.VideoWall.Core/Constants/VwCommandActions.cs` — +29 hằng số
- `Services/VideoWall/ITS.VideoWall/Consumer/VwCommandConsumer.cs` — +7 case, inject proxy dispatcher
- `Module.VideoWall/Controllers/Device/VwDeviceHandlerRunner.cs` — di chuyển sang Worker
- `Module.VideoWall/Controllers/Device/Queries/VwDeviceQueryHandler.cs`, `Commands/VwDeviceCommandHandler.cs` — viết lại 23 method
- `Module.VideoWall/Controllers/DeviceSetup/Commands/VwDeviceSetupCommandHandler.cs` — viết lại 6 method
- `Module.VideoWall/Extensions/ServiceCollectionExtensions.cs` — thêm DI request-reply, xoá DI ISAPI
- `Module.VideoWall/Module.VideoWall.csproj` — xoá ProjectReference ngược
- `Module.VideoWall.Core/Options/VwDeviceOptions.cs` — +3 field timeout
- `Module.VideoWall.Core/Dto/Device/VwDeviceOutput.cs` — thêm `VwDeviceGenericOutput<T>` chuyển từ WebAPI

## Verification

1. Build `ITS.VideoWall.csproj` + `ITS.VideoWall.Worker.csproj` + `Module.VideoWall.csproj` +
   `TAC_WebAPI` — bắt lỗi biên dịch/DI sớm.
2. Grep `IVwISAPIDeviceService|IVwISAPIDeviceClient` trong `Module.VideoWall/` (loại `.Core`) —
   phải trả về 0 kết quả sau mục 5.
3. Build `Module.VideoWall.csproj` riêng lẻ — xác nhận không còn kéo `ITS.VideoWall`.
4. Smoke test qua Swagger (6 bước ở "Trình tự triển khai" mục 6) — xác nhận response shape không
   đổi so với trước (FE/WPF không cần sửa gì phía nhận).
5. Test race-condition: gọi 1 request-reply (VD `Capabilities`) trong lúc Worker OFFLINE — xác
   nhận `RequestAsync` throw đúng `TimeoutException` sau `NatsRequestTimeoutSeconds` (15s mặc
   định), không treo vĩnh viễn.

---

## Việc còn tồn đọng từ review trước (CHƯA đưa vào plan này, cần bàn riêng)

Từ lượt review 113 file trước đó, còn 3 bug phần CRUD phân quyền + 1 gap config nhỏ chưa xử lý:
1. `VwUserAreaPermissionCommandHandler`: Update không duplicate-check actor → có thể tồn tại ≥2
   bản ghi active cùng UserId/OrgId.
2. UserId/OrgId không trim trước khi lưu → có thể tạo bản ghi "ma".
3. `VwUserAreaPermissionController` chưa có trong checklist gỡ `[AllowAnonymous]` trước production.
4. `ITS.VideoWall.Worker/appsettings.json` thiếu khai subject `.scene` trong `Nats.Streams`
   (không gây lỗi nhưng nên bổ sung cho tường minh).
