# Kiến trúc `Services.VDS` cho WPF và Windows Service — V2

> Phiên bản: 3.0  
> Ngày cập nhật: 16/07/2026  
> Phạm vi: `src/Services/VDS`

`Services.VDS` là pipeline VDS độc lập trên .NET 10. Worker nhận telemetry thật từ HTTP long-poll (khi được bật), WPF dùng form vận hành và form Demo, còn tất cả nguồn dữ liệu đều đi qua **một processing pipeline chung**. V1 tại `docs/PROJECT_STRUCTURE_WPF_WORKER.md` chỉ giữ để đối chiếu lịch sử.

## 1. Mục tiêu

1. WPF chỉ chứa UI, binding và thao tác vận hành.
2. Worker là Windows Service composition root; không chứa nghiệp vụ hay polling implementation.
3. Acquisition thật, Demo manual và Demo periodic dùng cùng queue, handler, persistence, NATS và runtime state.
4. Cấu hình camera/event ưu tiên SQLSugar nhưng có fallback `appsettings.json` an toàn.
5. SQLSugar hỗ trợ hai connection được định danh: cấu hình và vận hành.
6. NLog là logging provider của cả Worker và WPF.
7. NATS dùng adapter VDS riêng trên project hiện có `Modules.DataTransporter.Natsio`.
8. Không reference project, DLL, namespace hoặc static runtime từ VDS legacy.

## 2. Topology và dependency direction

```text
src/Services/VDS/
├─ VDS.sln
├─ Services.VDS.Contracts/
├─ Services.VDS.Shared/
├─ Services.VDS/
├─ Services.VDS.Wpf/
└─ Services.VDS.Worker/
```

```text
Services.VDS.Contracts     Services.VDS.Shared
              ▲                      ▲
              └──── Services.VDS ────┘
                        ▲       ▲
                        │       │
       Services.VDS.Wpf │       │ Services.VDS.Worker
```

| Project | Trách nhiệm |
|---|---|
| `Services.VDS.Contracts` | DTO normalized, configuration snapshot, command/query input và result public. |
| `Services.VDS.Shared` | Utility host-neutral có sẵn. Không chứa adapter hoặc handler VDS. |
| `Services.VDS` | Application, queue, configuration catalog, persistence, NATS, acquisition adapter và Demo scheduler. |
| `Services.VDS.Wpf` | WPF composition root, View/ViewModel và thao tác người vận hành. |
| `Services.VDS.Worker` | Windows Service composition root, bật real acquisition khi cấu hình cho phép. |

Quy tắc:

- Contracts/Shared không biết WPF/Worker.
- Logic project không reference WPF/Worker.
- WPF/Worker không gọi repository, SQLSugar, queue, NATS client hoặc vendor client trực tiếp.
- `Program.cs` và `App.xaml.cs` chỉ cấu hình host/DI/logging.
- `BackgroundService` nằm trong `Services.VDS`, không nằm trong executable Worker.

## 3. Pipeline telemetry thống nhất

```text
Real HTTP long-poll ─┐
                     ├─ normalize raw payload
Demo manual ─────────┤
                     ├─ TelemetryIngressQueue (bounded Channel)
Demo periodic ───────┘                 │
                                        ▼
                         TelemetryPipelineHostedService
                                        │
                                        ▼
                     ProcessVdsCounter/Detection/StatisticInput
                                        │
                                        ▼
                         TelemetryProcessingService
                    validate → idempotent persist/audit → NATS → state
                                        │
                                        ▼
                           completion/result cho caller hoặc log
```

`VdsTelemetryOrigin` ghi nguồn `Acquisition`, `DemoManual` hoặc `DemoScheduled` trong processing, persistence audit và NATS payload. Demo không gọi `ProcessVds*Input` trực tiếp: `DemoTelemetrySource` submit vào `TelemetryIngressQueue` rồi chờ completion của chính pipeline consumer.

### 3.1. Queue và backpressure

`TelemetryIngressQueue` dùng bounded `Channel<TelemetryEnvelope>`:

- `Vds:Pipeline:Capacity` mặc định `1000`, bắt buộc lớn hơn `0`.
- `FullMode.Wait` tạo backpressure thay vì mất data hoặc cấp phát bộ nhớ vô hạn.
- Một reader giúp preserve ordering của sample pipeline.
- Acquisition dùng `EnqueueAsync(...)`; Demo manual dùng `SubmitAsync(...)` có `TaskCompletionSource` để WPF nhận đúng kết quả persistence/processing.

### 3.2. Processing order

`TelemetryProcessingService` thực hiện theo thứ tự:

1. Validate dữ liệu normalized.
2. Persist idempotent theo `TransactionId` và ghi audit.
3. Publish NATS nếu item chưa duplicate.
4. Cập nhật `OperationalStateStore` trong bộ nhớ.
5. Trả `WResult`/completion.

NATS lỗi không làm mất dữ liệu đã persist; item vẫn được cập nhật state và log warning. Hiện chưa có outbox/retry publisher bền vững, xem [Giới hạn production](#11-giới-hạn-production-còn-lại).

## 4. Contracts công khai

### Telemetry và detection

- `VdsCounterData`, `VdsStatisticData`, `VdsDetectionEvent`.
- `VdsDetectionEvent.EventCode` giữ mapping event configuration.
- `ProcessVdsCounterInput`, `ProcessVdsDetectionInput`, `ProcessVdsStatisticInput` có `Origin`.
- `VdsOperationalSnapshot` và `GetVdsOperationalSnapshotInput` phục vụ form vận hành.

### Configuration và Demo

- `VdsDeviceConfiguration`: camera/device ID, code, IP, zone, lane mặc định, trạng thái active/demo.
- `VdsEventConfiguration`: event code, detection type, severity, incident candidate.
- `VdsConfigurationSnapshot`.
- `GetVdsConfigurationSnapshotInput`, `ReloadVdsConfigurationInput`.
- `GenerateVdsDemoDataInput`: camera được chọn và `DetectionEventCode` tùy chọn.
- `StartVdsDemoScheduleInput`, `StopVdsDemoScheduleInput`, `GetVdsDemoScheduleStatusInput`, `VdsDemoScheduleStatus`.

Không đưa vào Contracts:

- Raw JSON/model của Hik hoặc vendor.
- Entity SqlSugar, connection string, endpoint, credential, `HttpClient` hoặc NATS client.
- `TelemetryEnvelope`, queue completion và hosted service.
- WPF type hoặc vendor SDK.

## 5. Cấu trúc source chính

```text
Services.VDS.Contracts/
├─ Configuration/
│  ├─ VdsConfigurationSnapshot.cs
│  ├─ VdsDeviceConfiguration.cs
│  └─ VdsEventConfiguration.cs
├─ Detection/VdsDetectionEvent.cs
├─ Messages/
│  ├─ GenerateVdsDemoDataInput.cs
│  ├─ GetVdsConfigurationSnapshotInput.cs
│  ├─ ReloadVdsConfigurationInput.cs
│  ├─ StartVdsDemoScheduleInput.cs
│  ├─ StopVdsDemoScheduleInput.cs
│  └─ VdsDemoScheduleStatus.cs
└─ Telemetry/
   ├─ VdsCounterData.cs
   ├─ VdsStatisticData.cs
   └─ VdsTelemetryOrigin.cs

Services.VDS/
├─ Application/
│  ├─ Configuration/Commands/VdsConfigurationCommandHandler.cs
│  ├─ Demo/Commands/
│  │  ├─ DemoCommandHandler.cs
│  │  └─ DemoScheduleCommandHandler.cs
│  ├─ Processing/TelemetryProcessingService.cs
│  ├─ Traffic/Commands/TrafficCommandHandler.cs
│  ├─ Detection/Commands/DetectionCommandHandler.cs
│  └─ Statistics/Commands/StatisticsCommandHandler.cs
├─ Infrastructure/
│  ├─ Acquisition/Hik/
│  │  ├─ HikLongPollingVdsTelemetrySource.cs
│  │  ├─ HikLongPollingVdsHostedService.cs
│  │  ├─ HikVdsRawResponses.cs
│  │  └─ HikVdsTelemetryMapper.cs
│  ├─ Background/Ingestion/
│  ├─ Configuration/
│  │  ├─ AppSettingsVdsConfigurationSource.cs
│  │  ├─ VdsConfigurationCatalog.cs
│  │  ├─ VdsConfigurationReloadHostedService.cs
│  │  └─ Options/
│  ├─ Persistence/
│  │  ├─ Entities/
│  │  ├─ VdsSqlSugarClientFactory.cs
│  │  ├─ VdsSqlSugarTelemetryPersistence.cs
│  │  └─ VdsInMemoryTelemetryPersistence.cs
│  ├─ Simulation/
│  │  ├─ DemoTelemetryGenerator.cs
│  │  ├─ DemoTelemetrySource.cs
│  │  └─ VdsDemoScheduleService.cs
│  ├─ State/OperationalStateStore.cs
│  └─ Transport/
│     ├─ NatsVdsTelemetryPublisher.cs
│     └─ VdsNatsConnectionHostedService.cs
└─ Extensions/ServiceCollectionExtensions.cs
```

## 6. Configuration catalog

`VdsConfigurationCatalog` giữ immutable snapshot và reload theo `Vds:Configuration:ReloadIntervalSeconds` (mặc định 300 giây).

1. Khi `Vds:Database:Enabled=false`, catalog đọc `Vds:Configuration:Devices` và `Events` từ `appsettings.json`.
2. Khi database bật, catalog đọc `VdsDeviceConfiguration` và `VdsEventConfiguration` từ configuration connection SQLSugar.
3. Nếu database load lỗi, snapshot fallback sang `appsettings.json`; snapshot active trước đó chỉ bị thay khi load thành công.
4. WPF gọi `ReloadVdsConfigurationInput` để reload ngay, sau đó cập nhật camera/event selectors.

Worker và WPF đều có ba camera/event default an toàn để Demo dùng được ngay. Đây là fallback local, không phải thiết bị production. Cấu hình production phải thay thế bằng DB hoặc deployment config phù hợp.

## 7. SQLSugar persistence đa kết nối

`VdsSqlSugarClientFactory` tạo một `SqlSugarScope` immutable với hai `ConfigId` khác nhau:

| Connection | Mục đích | Config mặc định |
|---|---|---|
| `VdsConfiguration` | Đọc camera/event configuration. | `Vds:Database:Configuration` |
| `VdsOperational` | Ghi telemetry và audit. | `Vds:Database:Operational` |

Khi `Vds:Database:Enabled=false`, `IVdsTelemetryPersistence` dùng `VdsInMemoryTelemetryPersistence`; nó vẫn deduplicate trong lifetime process, phù hợp WPF/local Demo nhưng không bền qua restart.

Khi database bật, `VdsSqlSugarTelemetryPersistence` ghi:

| Table | Nội dung |
|---|---|
| `VdsCounterTelemetry` | Counter normalized, origin và thời điểm persist. |
| `VdsDetectionTelemetry` | Detection normalized, event code, severity, image reference. |
| `VdsStatisticTelemetry` | Statistic normalized. |
| `VdsTelemetryAudit` | Audit persist theo telemetry kind/transaction/device/origin. |
| `VdsDeviceConfiguration` | Device configuration source. |
| `VdsEventConfiguration` | Event mapping source. |

`TransactionId` là primary/idempotency key cho từng telemetry table. Persistence sử dụng transaction để insert telemetry và audit cùng nhau. Race duplicate vẫn được bảo vệ bởi primary key trong DB.

`Vds:Database:AutoCreateTables` mặc định `false`. Chỉ bật tạm trong môi trường kiểm thử/khởi tạo schema có kiểm soát; nó thay đổi DB nên không bật mặc định cho production.

Ví dụ cấu hình (không commit secret thật):

```json
{
  "Vds": {
    "Database": {
      "Enabled": true,
      "AutoCreateTables": false,
      "Configuration": {
        "ConnectionId": "VdsConfiguration",
        "DbType": "SqlServer",
        "ConnectionString": "<configuration-db-connection-string>"
      },
      "Operational": {
        "ConnectionId": "VdsOperational",
        "DbType": "SqlServer",
        "ConnectionString": "<operational-db-connection-string>"
      }
    }
  }
}
```

## 8. NLog và NATS

### NLog

- Worker: `NLog.Extensions.Logging` + `Services.VDS.Worker/NLog.config` ghi `logs/vds-worker-{date}.log` và console.
- WPF: `NLog.Extensions.Logging` + `Services.VDS.Wpf/NLog.config` ghi `logs/vds-wpf-{date}.log`.
- `ILogger<T>` được dùng xuyên Core; không dùng `Debug.WriteLine` cho pipeline.

### NATS

`NatsVdsTelemetryPublisher` bọc `Modules.DataTransporter.Natsio.Infrastructure.Handlers.NatsClient`; handler không gọi static `NatsService` trực tiếp.

| Subject mặc định | Payload |
|---|---|
| `its.vds.counter` | Counter và metadata origin. |
| `its.vds.detection` | Detection và metadata origin. |
| `its.vds.statistic` | Statistic và metadata origin. |

`Vds:Nats:Enabled=false` mặc định, vì vậy không có outbound network call khi chưa cấu hình. Khi bật, phải cung cấp `Vds:Nats:Url` và tùy chọn auth/TLS theo `VdsNatsOptions`. `VdsNatsConnectionHostedService` reconnect theo `RetryIntervalSeconds`.

## 9. Real acquisition adapter

`HikLongPollingVdsTelemetrySource` là adapter HTTP có raw models **internal**. `HikLongPollingVdsHostedService` chỉ được đăng ký ở Worker bằng:

```csharp
.AddVdsBackgroundProcessing(builder.Configuration, includeRealAcquisition: true)
```

WPF gọi `.AddVdsBackgroundProcessing(builder.Configuration)` nên không chạy long-poll production.

Mặc định:

```json
{
  "Vds": {
    "Acquisition": {
      "Enabled": false,
      "BaseUrl": "",
      "TrafficPath": "/traffic",
      "StatisticPath": "/statistic",
      "PollIntervalSeconds": 2,
      "RequestTimeoutSeconds": 45
    }
  }
}
```

Khi bật, adapter:

1. Gọi traffic/statistic endpoint bằng named `HttpClient`.
2. Map raw device ID/code/IP sang `VdsDeviceConfiguration` active.
3. Map event code sang `VdsEventConfiguration` active.
4. Bỏ qua payload không map được và ghi log warning.
5. Enqueue counter/detection/statistic với origin `Acquisition`.

Endpoint và raw property names là adapter generic dựa trên behavior legacy; trước khi production phải đối chiếu payload thật của vendor, authentication và retry/backoff SLA. Không đặt credential trong repository.

## 10. WPF Demo và form vận hành

### Form vận hành

`OperationalView` chỉ đọc `GetVdsOperationalSnapshotInput`, hiển thị:

- Camera đang theo dõi từ configuration catalog.
- Freshness và timestamp telemetry.
- Latest counter/statistic.
- Recent detection events.
- Processing counts.

### Form Demo

`DemoView` có ba tab manual:

1. Counter random theo camera đang chọn.
2. Detection random theo camera và event configuration đang chọn.
3. Statistic random theo camera đang chọn.

Phần Demo periodic cho phép:

- Chọn một hoặc nhiều camera configured/demo-enabled.
- Chọn interval.
- Bật/tắt Counter, Detection, Statistic.
- Dùng event code đang chọn cho Detection.
- Start/stop/status qua Wolverine command.

Scheduler nằm tại `VdsDemoScheduleService`, không dùng `DispatcherTimer`; `VdsDemoScheduleLifetimeHostedService` hủy scheduler cùng lifecycle host.

## 11. Giới hạn production còn lại

Các phần sau chưa được coi là hoàn chỉnh cho production và phải được lên kế hoạch riêng:

1. **Outbox/retry NATS bền vững:** hiện NATS publish sau DB commit. Nếu publish thất bại, dữ liệu vẫn persist/state update nhưng không có outbox replay tự động.
2. **Vendor contract thực:** endpoint, JSON fields, token/TLS và error classification của thiết bị thật chưa được cung cấp; adapter phải được verify với traffic/statistic payload thật.
3. **Incident/zone/TAP correlation:** hiện chỉ persist/state telemetry, chưa tạo incident hoặc warning workflow theo legacy.
4. **Media lifecycle:** image reference được giữ, chưa download/upload/retention file.
5. **Dead-letter/metrics/health checks:** invalid payload hiện log và skip; cần monitoring/alerting production.
6. **Cross-process WPF data:** `OperationalStateStore` là in-memory mỗi process. WPF Demo quan sát state của WPF host; để quan sát Worker remote cần query persistence/API hoặc subscriber NATS riêng.
7. **Load/recovery testing:** chưa có integration test với DB/NATS/vendor endpoint hoặc throughput test.

## 12. Host composition

Worker:

```text
Host.CreateApplicationBuilder(args)
  → AddNLog()
  → AddWindowsService(...)
  → AddVdsShared()
  → AddVdsServices(configuration)
  → AddVdsBackgroundProcessing(configuration, includeRealAcquisition: true)
  → AddWolverine(discover Services.VDS)
  → Build/RunAsync
```

WPF:

```text
Host.CreateApplicationBuilder(...)
  → AddNLog()
  → AddVdsShared()
  → AddVdsServices(configuration)
  → AddVdsBackgroundProcessing(configuration)
  → AddWolverine(discover Services.VDS)
  → Build/Start host
  → Load MainWindowViewModel configuration
  → Show MainWindow
```

## 13. Deployment

Build toàn bộ VDS solution:

```powershell
Set-Location "S:\Work\TAC\ITS_Project\ITS015\SourceCode\Web\TA-ITS015-WEBAPI-V1.0\src\Services\VDS"
dotnet build .\VDS.sln -c Debug
```

Publish Worker self-contained:

```powershell
dotnet publish .\Services.VDS.Worker\Services.VDS.Worker.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Triển khai từ thư mục publish, không chạy service production từ `bin/Debug`. Service account cần quyền DB/network/file theo configuration đã bật. Giữ `Vds:Database:Enabled`, `Vds:Nats:Enabled`, `Vds:Acquisition:Enabled` là `false` cho đến khi endpoint, credential, schema và monitoring được xác thực.

## 14. Xác minh hiện tại

- `dotnet build .\VDS.sln -c Debug` đã thành công sau implementation.
- Worker/WPF `appsettings.json` đã parse JSON thành công.
- Không có `ProjectReference`/DLL/path reference tới `TA_VDS_Service`, `Test_API_VDS` hoặc `ACS010_DM_SERVICE` trong `src/Services/VDS`.
- Build hiện vẫn hiển thị dependency vulnerability warnings inherited từ `Shared.Reference`/`Shared.DTO`/`Modules.DataTransporter.Natsio` (ví dụ `Microsoft.OpenApi`, `SixLabors.ImageSharp`, `SQLitePCLRaw`, `System.Security.Cryptography.Xml`). Chúng không được tạo bởi VDS pipeline code nhưng cần được xử lý ở dependency graph chung trước production.

## 15. Anti-pattern cần tránh

- Business logic trong `Program.cs`, `App.xaml.cs`, Window hoặc ViewModel.
- `BackgroundService` trong Worker project.
- Static mutable configuration/list xuyên thread.
- `.Result`, `.Wait()`, `Thread.Abort()`, manual thread lifecycle hoặc queue không giới hạn.
- Demo gọi processing handler trực tiếp, bỏ qua queue.
- Publish NATS trước khi telemetry persistence hoàn tất.
- Raw vendor model làm public contract.
- Reference lại legacy project/DLL để “tái sử dụng nhanh”.
- Bật DB/NATS/acquisition bằng credential hard-code hoặc không có readiness/monitoring.
