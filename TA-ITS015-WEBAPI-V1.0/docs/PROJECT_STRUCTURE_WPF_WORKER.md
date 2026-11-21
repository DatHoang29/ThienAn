# Cấu trúc `Services.*` cho WPF và Windows Service

## 1. Mục tiêu và ranh giới bắt buộc

Tài liệu này áp dụng cho một bộ chương trình thiết bị đặt tại `src/Services/{Product}`, ví dụ `src/Services/VDS`.

- **`Services.{Product}.Wpf`** là UI host: View, ViewModel, dialog, navigation, UI dispatcher và thao tác người dùng.
- **`Services.{Product}.Worker`** là Windows Service host: nạp configuration, đăng ký DI, cấu hình service lifetime và gọi `RunAsync()`.
- **`Services.{Product}`** là project logic chung: Application, Infrastructure, adapter, repository, transport, hosted service, polling, reconnect, retry và toàn bộ xử lý nền của sản phẩm.
- **`Services.{Product}.Shared`** chỉ chứa utility/platform registration dùng chung thật sự.

WPF và Worker không chứa `BackgroundService`, vòng polling, subscriber dài hạn, retry/reconnect, truy cập DB/NATS/Socket/DLL vendor hoặc business logic. Hai host chỉ quyết định có chạy logic nền hay không:

- WPF gọi `Add{Product}Shared()` và `Add{Product}Services()`.
- Worker gọi thêm `Add{Product}BackgroundProcessing(configuration)`.

## 2. Cấu trúc solution và dependency

```text
src/Services/{Product}/
├─ {Product}.sln
├─ Services.{Product}.Contracts/     # DTO, event/message, WResult<T>
├─ Services.{Product}.Shared/        # Utility/platform extension dùng chung
├─ Services.{Product}/               # Application + Infrastructure + Background
├─ Services.{Product}.Wpf/           # WPF UI executable
└─ Services.{Product}.Worker/        # Windows Service host executable
```

```text
Services.{Product}.Contracts     Services.{Product}.Shared
              ▲                              ▲
              └──────── Services.{Product} ─┘
                              ▲        ▲
                              │        │
              Services.{Product}.Wpf  Services.{Product}.Worker
```

| Project | Target framework | Reference nội bộ | Vai trò |
|---|---|---|---|
| `Services.{Product}.Contracts` | `net10.0`; dùng `netstandard2.0` nếu Legacy Bridge .NET Framework cần reference | Không có | DTO, event/message, result wrapper. |
| `Services.{Product}.Shared` | `net10.0` | Không có nếu có thể | `TimeProvider`, serialization, retry, file helper. |
| `Services.{Product}` | `net10.0` | `Contracts`, `Shared` | Logic, adapter, repository, transport và background processing. |
| `Services.{Product}.Wpf` | `net10.0-windows` | `Contracts`, `Shared`, `Services.{Product}` | UI executable. |
| `Services.{Product}.Worker` | `net10.0-windows` | `Shared`, `Services.{Product}` | Windows Service composition root. |

Quy tắc:

1. `Contracts` không tham chiếu `Shared` hoặc project logic để giữ boundary nhẹ cho WPF, Worker và Legacy Bridge.
2. `Shared` không biết VDS/vendor/DB/NATS/Socket/background processing.
3. WPF/Worker không gọi trực tiếp repository, device client, NATS/Socket hoặc DLL vendor.
4. Chỉ `App.xaml.cs` và `Program.cs` là composition root gọi extension DI.
5. `BackgroundService` implementation luôn thuộc `Services.{Product}`; host chỉ gọi extension đăng ký phù hợp.

## 3. Quy tắc đặt tên theo scope

Không thêm tiền tố sản phẩm khi namespace, project và folder đã xác định đủ ngữ cảnh. Điều này giúp tái dùng template hoặc chuyển implementation sang product khác mà không phải rename dây chuyền.

| Scope | Quy tắc | Ví dụ VDS |
|---|---|---|
| Project/namespace implementation | Không lặp lại tiền tố sản phẩm; tên phải mô tả trách nhiệm. | `Services.VDS.Infrastructure.Devices.DeviceClientFactory`, `StatusStore`, `IDeviceClient`. |
| Feature namespace | Dùng feature để tạo ngữ cảnh thay vì prefix trong class. | `Services.VDS.Application.Status.Commands.StatusCommandHandler`. |
| DI extension class | Dùng `ServiceCollectionExtensions` trong namespace riêng của project. | `Services.VDS.Extensions.ServiceCollectionExtensions`, `Services.VDS.Shared.Extensions.ServiceCollectionExtensions`. |
| Public Contracts | Giữ prefix sản phẩm khi DTO/event có thể được import cùng nhiều product khác. | `VdsStatusOutput`, `RefreshVdsStatusInput`, `VdsStatusEvent`, `VdsProviderCodes`. |
| Public DI entry point | Giữ prefix sản phẩm để tránh collision ở host dùng nhiều module. | `AddVdsShared`, `AddVdsServices`, `AddVdsBackgroundProcessing`. |
| Deployment/config section | Giữ prefix sản phẩm vì appsettings có thể chứa cấu hình của nhiều module. | `VdsPolling`. |

Hai class cùng tên `ServiceCollectionExtensions` trong hai namespace khác nhau là hợp lệ. Host chỉ dùng extension methods mang prefix sản phẩm; nếu cần gọi static class trực tiếp thì dùng fully qualified name hoặc alias.

Không tạo `AddServices()` hay `AddBackgroundProcessing()` quá chung ở public boundary: các tên này dễ collision khi một host đăng ký VDS, VMS và CCTV cùng lúc.

## 4. Trách nhiệm và layout VDS thực tế

### 4.1. Contracts

```text
Services.VDS.Contracts/
├─ Dto/Vds/
│  ├─ VdsModels.cs
│  ├─ RefreshVdsStatusInput
│  ├─ GetVdsStatusInput
│  └─ VdsStatusOutput
├─ Events/VdsStatusEvent.cs
└─ ResponseWrapper/WResult.cs
```

Contracts là cross-product/process boundary. Không đặt entity SqlSugar, object vendor, WPF type, SDK hoặc `I...Service` vào đây.

### 4.2. Shared

```text
Services.VDS.Shared/
└─ Extensions/
   └─ ServiceCollectionExtensions.cs
```

`AddVdsShared()` chỉ đăng ký platform utility như `TimeProvider`, serializer hoặc retry helper. Không đặt handler, repository, adapter, device client, hosted service, entity hoặc rule nghiệp vụ vào Shared.

### 4.3. Logic project

```text
Services.VDS/
├─ Application/
│  └─ Status/
│     ├─ Commands/StatusCommandHandler.cs
│     └─ Queries/StatusQueryHandler.cs
├─ Infrastructure/
│  ├─ Background/StatusPolling/
│  │  └─ DeviceStatusPollingHostedService.cs
│  ├─ Config/Options/
│  │  └─ DeviceStatusPollingOptions.cs
│  └─ Devices/
│     ├─ IDeviceClient.cs
│     ├─ SampleDeviceClients.cs
│     ├─ DeviceClientFactory.cs
│     └─ StatusStore.cs
└─ Extensions/
   └─ ServiceCollectionExtensions.cs
```

Các implementation nội bộ không cần lặp `Vds` trong tên vì `Services.VDS` đã là product boundary. Khi có nhiều provider, `IDeviceClient` và `DeviceClientFactory` là boundary phù hợp; factory chỉ chọn implementation đã đăng ký DI, không chứa business logic và không `new` vendor SDK theo request.

## 5. DI, mediator và background processing

Ví dụ đăng ký cho WPF:

```csharp
builder.Services
    .AddVdsShared()
    .AddVdsServices();
```

Worker gọi thêm registration background:

```csharp
builder.Services
    .AddVdsShared()
    .AddVdsServices()
    .AddVdsBackgroundProcessing(builder.Configuration);
```

`AddVdsBackgroundProcessing` nằm trong `Services.VDS.Extensions`, bind `DeviceStatusPollingOptions`, validate configuration khi startup và đăng ký `DeviceStatusPollingHostedService`. `Program.cs` không biết polling interval, provider hoặc device ID.

Cả WPF và Worker dùng `typeof(StatusCommandHandler).Assembly` làm Wolverine discovery marker. `StatusCommandHandler` không cần prefix `Vds` vì namespace `Services.VDS.Application.Status.Commands` và assembly `Services.VDS` đã xác định product context; đây là implementation marker, không phải cross-product contract.

WPF và hosted service chỉ dispatch DTO/event chuẩn qua Wolverine:

```csharp
var result = await _messageBus.InvokeAsync<WResult<VdsStatusOutput>>(
    new RefreshVdsStatusInput(),
    cancellationToken);
```

Luồng chuẩn:

```text
WPF action / hosted service / NATS event
  → Input hoặc Event trong Services.{Product}.Contracts
  → IMessageBus.InvokeAsync(...)
  → CommandHandler / QueryHandler trong Services.{Product}
  → Repository / transport / adapter / device client
  → WResult<T> hoặc event chuẩn hóa
```

- Không dispatch raw telemetry tốc độ cao qua mediator; parse/lọc/batch bằng `Channel<T>` trong `Services.{Product}` trước.
- `BackgroundService` là singleton; nếu xử lý batch cần dependency scoped, implementation trong logic project phải tạo scope đúng chỗ.
- NATS chỉ ack sau khi handler thành công; failure cần retry/backoff hoặc dead-letter flow.

## 6. WPF và Worker host

### WPF

```text
App.xaml.cs
  → AddVdsShared()
  → AddVdsServices()
  → AddWolverine(discover Services.VDS)
  → Build/start Generic Host
  → Resolve và mở MainWindow
```

WPF ViewModel có thể inject `IMessageBus`, nhận DTO Contracts và hiển thị kết quả. Nó không tạo `BackgroundService`, timer polling dài hạn, consumer hoặc reconnect vendor.

### Worker

```text
Program.cs
  → Host.CreateApplicationBuilder(args)
  → AddWindowsService(ServiceName)
  → AddVdsShared()
  → AddVdsServices()
  → AddVdsBackgroundProcessing(configuration)
  → AddWolverine(discover Services.VDS)
  → Build/RunAsync
```

`Program.cs` không có class polling, `PeriodicTimer`, retry hoặc device-specific setting. Khi SCM khởi động process, `AddWindowsService` dùng Windows Service lifetime; khi chạy từ terminal để debug, executable vẫn dùng được như console host.

## 7. Cấu hình Windows Service

Worker dùng `Microsoft.NET.Sdk.Worker`, target `net10.0-windows` và package được ghim version:

```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.10" />
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="10.0.10" />
<PackageReference Include="WolverineFx" Version="5.31.0" />
```

`appsettings.json` phải được copy ra output/publish:

```json
{
  "WindowsService": {
    "ServiceName": "Services.VDS.Worker"
  },
  "VdsPolling": {
    "DeviceId": "VDS-001",
    "ProviderCode": "simulator",
    "IntervalSeconds": 5
  }
}
```

`DeviceStatusPollingOptions` validate `DeviceId`, `ProviderCode` và `IntervalSeconds` khi Worker startup. Vẫn giữ section `VdsPolling` vì đây là deployment boundary có thể cùng tồn tại với `VmsPolling`, `CctvPolling` trong một host/configuration.

Publish self-contained Windows x64 từ `src/Services/VDS`:

```powershell
dotnet publish .\Services.VDS.Worker\Services.VDS.Worker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\artifacts\Services.VDS.Worker
```

Cài bằng PowerShell Administrator:

```powershell
$serviceName = 'Services.VDS.Worker'
$displayName = 'TAC VDS Worker'
$exePath = (Resolve-Path '.\artifacts\Services.VDS.Worker\Services.VDS.Worker.exe').Path

sc.exe create $serviceName binPath= "`"$exePath`"" start= auto DisplayName= "$displayName"
sc.exe description $serviceName 'TAC VDS polling and device processing service'
sc.exe failure $serviceName reset= 86400 actions= restart/5000/restart/10000/restart/30000
sc.exe start $serviceName
```

`sc.exe` yêu cầu có dấu cách sau `binPath=`, `start=` và `reset=`. Service account phải có quyền đọc thư mục publish, ghi log và truy cập DB/network/vendor SDK cần thiết.

```powershell
sc.exe stop Services.VDS.Worker
sc.exe delete Services.VDS.Worker
```

## 8. Adapter, mapping và Legacy Bridge

| Tình huống | Cách dùng phù hợp |
|---|---|
| Vendor/protocol/DLL có API hoặc data không tương thích | Adapter hoặc `...DeviceClient` trong `Services.{Product}/Infrastructure`. |
| Chọn implementation theo `ProviderCode`, protocol/version/config | Factory/Resolver nhỏ trong logic project. |
| Cùng input/output nhưng thuật toán khác | Strategy theo feature trong logic project. |
| Entity ↔ `Input`/`Output` | Mapster hoặc mapping tường minh. |
| Trạng thái → màu/icon/text WPF | `IValueConverter` hoặc ViewModel property trong WPF. |

- Một provider: inject concrete `DeviceClient`, không cần interface/factory.
- Từ hai provider cùng capability: tạo `IDeviceClient` và factory nhỏ theo config.
- Parse vendor payload cần mapping tường minh trong adapter/client; không để Mapster map ngầm qua protocol boundary.
- Không ép VDS, VMS, CCTV vào một `IDeviceClient` chung nếu capability/command khác nhau.

Nếu DLL vendor chỉ hỗ trợ `net48`, COM, native hoặc x86, cô lập trong `Services.{Product}.LegacyBridge.Net48`. Bridge chỉ nhận/trả DTO/event từ Contracts; không truyền entity SqlSugar, object vendor hoặc exception framework qua process boundary.

## 9. Checklist

1. Đặt DTO, event và `WResult<T>` vào Contracts; giữ prefix product nếu consumer có thể import nhiều product.
2. Chỉ đặt common utility/platform registration vào Shared.
3. Đặt entity, handler, repository, adapter, transport, options và mọi hosted/background service vào `Services.{Product}`.
4. Bỏ prefix product khỏi implementation khi namespace/folder đã đủ context; dùng tên có nghĩa theo trách nhiệm.
5. Giữ prefix product ở `Add{Product}*`, config section và cross-product DTO/event.
6. WPF gọi `Add{Product}Shared()` và `Add{Product}Services()`; không gọi background registration.
7. Worker gọi cả ba extension, `AddWindowsService(...)` và Wolverine discovery cho assembly logic.
8. Publish Worker cho Windows Runtime Identifier phù hợp; cài executable đã publish bằng SCM với account/quyền phù hợp.
