# TAC_WebAPI — Integration Tests Suite

Bộ kiểm thử tích hợp cho toàn bộ hệ thống ITS (`TAC_WebAPI`, các worker background, và client WPF) theo kiến trúc kiểm thử tự động độc lập.

Project test nằm **ngoài** repo product `TA-ITS015-WEBAPI-V1.0` (không commit lẫn vào repo product); mọi đường dẫn tới source code đi qua property `$(RepoRoot)` trong `test.csproj`.

---

## Cấu trúc thư mục

```
tests/BE/
├─ GlobalUsings.cs                    (Global using directives dùng chung cho toàn bộ test suite)
├─ Host.cs                            (Test Generic Host độc lập, DI Lamar, SqlSugar, auto-discovery)
├─ README.MD                          (Tài liệu kiến trúc và hướng dẫn kiểm thử)
├─ appsettings.Test.json              (Cấu hình test: CSDL local, NATS local, thư mục tạm)
├─ test.csproj                        (Dự án kiểm thử xUnit đa target net10.0 & net10.0-windows)
├─ StartupValidation/                 (Kiểm thử hạ tầng khởi động, độc lập ngoài ITS/ vì không thuộc phân hệ nào)
│  └─ ProjectStartupSmokeTests.cs
└─ ITS/                               (Phân hệ nghiệp vụ ITS)
   ├─ ShareData/                      (Phân hệ chia sẻ dữ liệu liên ngành)
   │  ├─ GlobalUsings.ShareData.cs
   │  ├─ Host.ShareData.cs            (Partial Host dọn dẹp thư mục xuất file ShareData)
   │  ├─ Services/
   │  │  ├─ DataChangeTrackingServiceTests.cs
   │  │  └─ DataOutboundServiceTests.cs
   │  └─ Mocks/
   │     └─ ShareDataPartnerServerMock.cs (Giả lập server đối tác, HttpListener port 18090-18093)
   └─ VideoWall/                      (Phân hệ điều khiển tường màn hình ghép)
      ├─ GlobalUsings.VideoWall.cs
      ├─ Host.VideoWall.cs            (Partial Host quản lý vòng đời ISAPI Mock Server)
      ├─ WebApi/
      │  ├─ Controllers/              (12 file kiểm thử API Controller & Wolverine Handler)
      │  │  ├─ VwControllerTests.cs
      │  │  ├─ VwEventRuleTests.cs
      │  │  ├─ VwEventTriggerLogTests.cs
      │  │  ├─ VwSceneAckBeforeCommitTests.cs
      │  │  ├─ VwSceneTests.cs
      │  │  ├─ VwScheduleTests.cs
      │  │  ├─ VwScreenTests.cs
      │  │  ├─ VwSlotPortTests.cs
      │  │  ├─ VwSourceTests.cs
      │  │  ├─ VwWallPermissionTests.cs
      │  │  ├─ VwWallTopologyTests.cs
      │  │  └─ VwWindowSceneTests.cs
      │  └─ Services/                 (Kiểm thử dịch vụ tầng WebApi)
      │     └─ VwPermissionServiceTests.cs
      ├─ Worker/                      (Kiểm thử dịch vụ nền Worker)
      │  ├─ Heartbeat/
      │  │  └─ VwDeviceHeartbeatServiceTests.cs
      │  ├─ ISAPIDevice/             (7 file kiểm thử giao thức ISAPI Hikvision & Cascade)
      │  │  ├─ VwCascadeControllerSelectionTests.cs
      │  │  ├─ VwCascadeCoordTests.cs
      │  │  ├─ VwCascadeIntegrationTests.cs
      │  │  ├─ VwCascadeRoutingCredentialTests.cs
      │  │  ├─ VwDeviceConnectionAndProbeTests.cs
      │  │  ├─ VwISAPIDeviceClientRetryTests.cs
      │  │  └─ VwISAPIDeviceServiceTests.cs
      │  ├─ Messaging/               (2 file kiểm thử điều phối lệnh Wolverine & NATS)
      │  │  ├─ VwCommandConsumerTests.cs
      │  │  └─ VwNatsRealIntegrationTests.cs
      │  └─ Scene/                   (2 file kiểm thử cắt lát toạ độ và đồng bộ kịch bản)
      │     ├─ VwSceneRegionServiceTests.cs
      │     └─ VwSyncActiveSceneTests.cs
      ├─ Wpf/                         (7 file kiểm thử logic client WPF & 1 mock helper)
      │  ├─ VwLocalSourceStoreTests.cs
      │  ├─ VwLocalStoresTests.cs
      │  ├─ VwScenarioStoreTests.cs
      │  ├─ VwWpfCascadeWallTests.cs
      │  ├─ VwWpfCommissioningTests.cs
      │  ├─ VwWpfDirectModeTests.cs
      │  ├─ VwWpfMocks.cs             (Lớp mock môi trường client WPF)
      │  └─ VwWpfStandaloneModeTests.cs
      ├─ Mocks/                       (14 file partial class ISAPI Mock Server Hikvision)
      │  ├─ VwISAPIServerHikvisionMock.cs
      │  ├─ VwISAPIServerHikvisionMock.DigestAuth.cs
      │  ├─ VwISAPIServerHikvisionMock.Routes.*.cs
      │  └─ VwISAPIServerHikvisionMock.Scenario.cs
      └─ VwMockServerRunner/          (Console runner độc lập, port 18080-18083)
         ├─ Program.cs
         └─ VwMockServerRunner.csproj
```

---

## Cách chạy kiểm thử

Chạy trực tiếp qua lệnh CLI `dotnet test tests/BE/test.csproj`. Trình kiểm thử hỗ trợ bộ lọc xUnit linh hoạt theo từng phân hệ hoặc từng test fixture cụ thể.

```bash
# 1. Chạy toàn bộ test suite (tất cả phân hệ):
dotnet test tests/BE/test.csproj

# 2. Chạy riêng từng phân hệ:
# Phân hệ VideoWall:
dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~VideoWall"

# Phân hệ ShareData:
dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~ShareData"

# Hạ tầng khởi động hệ thống (Smoke Tests):
dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~StartupValidation"

# 3. Chạy một lớp test hoặc fixture cụ thể:
dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~VwControllerTests"
dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~DataOutboundServiceTests"
```

### Điều kiện tiên quyết & Cơ chế Zero-Trust

1. **SQL Server Local (`localhost:1433`)**:
   - Bắt buộc có SQL Server local chạy sẵn tại `localhost` hoặc `127.0.0.1` (trên macOS Apple Silicon dùng Docker image `mcr.microsoft.com/azure-sql-edge:latest`).
   - **Zero-Trust Guard**: Toàn bộ chuỗi kết nối trong `Host.cs` được kiểm tra nghiêm ngặt. Nếu phát hiện trỏ ra ngoài máy cục bộ (`localhost`, `127.0.0.1`, `(localdb)`, `.`), Host sẽ lập tức ngắt toàn bộ tiến trình để đảm bảo an toàn tuyệt đối, không ghi đè dữ liệu của môi trường phát triển / staging (`10.10.8.30`).

2. **Vòng lặp NATS Broker (`127.0.0.1:4222`)**:
   - `VwNatsRealIntegrationTests` kiểm tra luồng tích hợp vòng qua broker NATS thật.
   - **Có broker NATS**: Chạy trọn vẹn vòng phát → NATS broker → nhận tin → xử lý nghiệp vụ.
   - **Không có broker NATS**: Bài test tự động nhận diện và chuyển sang kiểm tra hợp đồng offline (Contract Offline Test) và vẫn **PASS**, ghi log rõ ràng về việc đã bỏ qua chặng NATS.

3. **ISAPI Mock Server Hikvision (VideoWall)**:
   - Toàn bộ test của VideoWall sử dụng `VwISAPIServerHikvisionMock` (`HttpListener` thật chạy trên `127.0.0.1`, port 18080-18083).
   - Được `Host.VideoWall.cs` tự động khởi động và giải phóng trong vòng đời test, hoàn toàn không phụ thuộc phần cứng thật hay mạng ngoại vi.
   - Khi cần debug hoặc test bằng tay ngoài suite, có thể chạy console runner độc lập tại `tests/BE/ITS/VideoWall/VwMockServerRunner`.

4. **ShareData Partner Server Mock (ShareData)**:
   - `ShareDataPartnerServerMock` (`HttpListener` chạy port 18090-18093) mô phỏng các endpoint nhận dữ liệu của đối tác (Bộ GTVT, Cục CSGT...).

---

## Dọn dẹp dữ liệu & tệp kết xuất giữa các phiên chạy

1. **Dọn tệp kết xuất ShareData (`Host.ClearExportedFiles`)**:
   - Chạy **đúng một lần ở đầu phiên test** song song với `ClearAllData()` / `ClearAllCache()`.
   - Xoá sạch thư mục `Out` bên dưới thư mục kết xuất dữ liệu (`NasStorage:BasePath` hoặc mặc định `sharedata/send`).
   - **Dọn ở ĐẦU phiên**: Giữ lại các tệp vừa kết xuất trên đĩa sau khi test kết thúc để kỹ sư có thể soi lại payload thực tế.
   - Mọi lỗi ngoại lệ trong quá trình dọn file rác đều được bắt an toàn để không làm gián đoạn test suite.

2. **Dọn sạch Database Test (`Host.ClearAllData`)**:
   - `InitializeAsync` thực hiện `TRUNCATE` toàn bộ các bảng entity SqlSugar đăng ký trong hệ thống mỗi khi khởi động Host.
   - Từng bài kiểm thử tự dọn các bản ghi nghiệp vụ do mình sinh ra trong `Dispose()` / `DisposeAsync()` theo tiền tố định danh riêng (`TEST_VWCTRL_`, `TEST_VWSCN_`, `TEST_SD_`...).

---

## Danh mục kiểm thử chi tiết

Hiện tại bộ kiểm thử tích hợp có **35 file kiểm thử** (tương ứng **790 test cases**) được chia theo từng phân hệ:

| Phân hệ / Tầng kiến trúc | File kiểm thử | Số test | Phạm vi & Nghiệp vụ kiểm thử |
| :--- | :--- | :---: | :--- |
| **Hạ tầng khởi động** | `ProjectStartupSmokeTests.cs` | 3 | Khởi tạo DI container, kiểm thử nạp service cho WebApi, VideoWall Worker, và VideoWall WPF Host |
| **ShareData — Dịch vụ nền** | `DataChangeTrackingServiceTests.cs` | 65 | Polling SQL Server Change Tracking, baseline tracking, version delta, snapshot isolation, xử lý theo lô |
| | `DataOutboundServiceTests.cs` | 117 | Pipeline xuất dữ liệu: định dạng payload, JSON shape mapping, chuyển đổi bảng mã CodeSet, REST/PDU sender, identity headers, phân trang và khoá phân tán |
| **VideoWall — WebApi Controllers** | `VwControllerTests.cs` | 18 | CRUD bộ điều khiển, `GetInputChannels`, DeviceAuthFailure, Negative Validation FluentValidation |
| | `VwEventRuleTests.cs` | 12 | CRUD luật sự kiện `VwEventRule`, trigger action theo điều kiện |
| | `VwEventTriggerLogTests.cs` | 11 | Audit logging trigger, truy vấn lịch sử kích hoạt kịch bản sự kiện |
| | `VwSceneAckBeforeCommitTests.cs` | 2 | Cơ chế Two-Phase commit / Ack từ thiết bị trước khi commit DB |
| | `VwSceneTests.cs` | 26 | CRUD kịch bản hiển thị, kích hoạt kịch bản đa controller, mapping cửa sổ, rollback khi lỗi |
| | `VwScheduleTests.cs` | 12 | CRUD lịch trình chuyển kịch bản tự động `VwSchedule` |
| | `VwScreenTests.cs` | 12 | CRUD màn hình `VwScreen`, kích thước và cấu hình lưới hiển thị |
| | `VwSlotPortTests.cs` | 12 | Quản lý khe cắm và cổng kết nối phần cứng `VwSlotPort` |
| | `VwSourceTests.cs` | 14 | CRUD nguồn tín hiệu đầu vào `VwSource` |
| | `VwWallPermissionTests.cs` | 23 | Phân quyền tường video theo User/Org, gán quyền và kiểm tra phạm vi truy cập |
| | `VwWallTopologyTests.cs` | 18 | Topology tường màn hình, tính toán ma trận ghép màn và validation toạ độ |
| | `VwWindowSceneTests.cs` | 29 | Quản lý cửa sổ kịch bản, Z-Order, chuyển nguồn tín hiệu (`SwitchSource`), boundary validation |
| **VideoWall — WebApi Services** | `VwPermissionServiceTests.cs` | 22 | Phân quyền ô lưới (Grid cell), SuperAdmin bypass, Background worker context, phân quyền theo tài khoản tổ chức |
| **VideoWall — Worker Heartbeat** | `VwDeviceHeartbeatServiceTests.cs` | 5 | Định kỳ kiểm tra nhịp tim thiết bị VideoWall, cập nhật trạng thái Online/Offline, timeout handling |
| **VideoWall — Worker ISAPI & Cascade** | `VwCascadeControllerSelectionTests.cs` | 11 | Thuật toán phân loại & lựa chọn bộ điều khiển trung tâm / bộ con trong topo cascade |
| | `VwCascadeCoordTests.cs` | 14 | Thuật toán chia toạ độ subwindow, quy đổi uniform coordinate và lưu trữ toạ độ cascade DB |
| | `VwCascadeIntegrationTests.cs` | 13 | Tích hợp luồng cascade đa controller end-to-end qua MockServer & DB |
| | `VwCascadeRoutingCredentialTests.cs` | 2 | Định tuyến và giải mã thông tin xác thực (credential) cho từng controller trong cụm cascade |
| | `VwDeviceConnectionAndProbeTests.cs` | 8 | Kết nối, Ping & Probe thiết bị qua ISAPI MockServer |
| | `VwISAPIDeviceClientRetryTests.cs` | 4 | Cơ chế retry khi gọi ISAPI client gặp lỗi kết nối hoặc timeout |
| | `VwISAPIDeviceServiceTests.cs` | 77 | Triển khai đầy đủ các giao thức ISAPI Hikvision (Digest Auth, Base URI, Windowing, Layout, Dynamic Decode, Signal Source, OSD, Plan, Audio, Wallpaper, Led...) |
| **VideoWall — Worker Messaging** | `VwCommandConsumerTests.cs` | 15 | Xử lý command bất đồng bộ từ message queue Wolverine, điều phối lệnh thiết bị |
| | `VwNatsRealIntegrationTests.cs` | 1 | Kiểm thử tích hợp NATS pub/sub thật (hoặc fallback contract test khi offline) |
| **VideoWall — Worker Scene** | `VwSceneRegionServiceTests.cs` | 11 | Kiểm tra vùng hiển thị, toạ độ chuẩn hoá 1920×1920, cắt lát subwindow đa controller |
| | `VwSyncActiveSceneTests.cs` | 5 | Đồng bộ trạng thái kịch bản đang hiển thị thực tế từ controller về DB |
| **VideoWall — Client WPF** | `VwLocalSourceStoreTests.cs` | 8 | Quản lý lưu trữ local nguồn tín hiệu phía WPF Client |
| | `VwLocalStoresTests.cs` | 9 | Lưu trữ cấu hình màn hình và kịch bản local phía client |
| | `VwScenarioStoreTests.cs` | 75 | Quản lý kịch bản, canvas trực quan, tỉ lệ hiển thị (scale) và thực thi kịch bản WPF |
| | `VwWpfCascadeWallTests.cs` | 8 | Điều khiển tường ghép cascade từ giao diện WPF |
| | `VwWpfCommissioningTests.cs` | 63 | Quy trình commissioning thiết bị từ WPF (dò tìm, cấu hình, gán layout) |
| | `VwWpfDirectModeTests.cs` | 57 | Chế độ điều khiển trực tiếp từ WPF tới controller không qua WebApi (Ping, Probe, DynamicDecode, SwitchSource...) |
| | `VwWpfStandaloneModeTests.cs` | 8 | Chế độ Standalone độc lập của client WPF khi mất kết nối máy chủ trung tâm |

> *Ghi chú: Toàn bộ các unit test vụn vặt / thuần in-memory (WPF converter, XML highlighter, string summary parser, wrapper messaging test rỗng) đã được tinh gọn và chuẩn hoá theo quy định kiểm thử chất lượng cao.*

---

## Kiến trúc Test Host (`Host.cs`)

`Host` là **Generic Host độc lập** (`GenericHost.CreateDefaultBuilder().Inject().UseLamar()`), *không* sử dụng `WebApplicationFactory`. Cấu hình nạp 100% từ bộ nhớ (`AddInMemoryCollection`) sau khi xoá các nguồn cấu hình ngoài (`configBuilder.Sources.Clear()`), do đó không phụ thuộc vào `appsettings.json` của bất kỳ project product nào.

Thứ tự dựng service trong `RegisterTestServices` mô phỏng theo `AddSharedInfrastructure` của `Shared.Infrastructure.dll`, **bỏ duy nhất `LicenseService`**. Vì bỏ `SetDbConfig` của DLL nên Test Host chủ động thực hiện 3 thiết lập bắt buộc:

| Hạng mục | Cơ chế kỹ thuật |
| :--- | :--- |
| `SqlSugarSetup.ITenant = sqlSugarScope.AsTenant()` | Gán static tenant cho SqlSugar. Nếu thiếu, constructor không tham số của `SqlSugarRepository<T>` sẽ throw `NullReferenceException` khi các handler Wolverine khởi tạo |
| `BindFurionRootServices(...)` | App thật gán `App.RootServices` trong middleware `app.UseInject()`. Generic Host gán trực tiếp vào `Furion.InternalApp.RootServices` |
| `column.DefaultValue = "0"` (không bọc nháy) | SqlSugar tự sinh `DEFAULT('0')`. Truyền `"'0'"` sẽ sinh `DEFAULT(''0'')` khiến SQL Server báo lỗi cú pháp |

Quá trình khởi tạo schema gọi `InitTables` cho các entity. Thứ tự trong `InitializeAsync`: **schema init (bước 4) chạy TRƯỚC `ClearAllData()` (bước 5)**. Nếu gặp lỗi khi thêm cột `NOT NULL` vào bảng có sẵn dữ liệu cũ, chỉ cần `TRUNCATE TABLE <tên>` hoặc `DROP TABLE` trong DB `test` để CodeFirst tạo lại tự động.

---

## Tự động khám phá Module (Auto-discovery)

`Host.cs` không hardcode các lệnh gọi đăng ký service của từng module (`AddSystemModule`, `AddShareDataModule`, `AddVWModule`...). Bước 12 của `RegisterTestServices` gọi hàm tự động quét:

```csharp
RegisterDiscoveredModules(services, config);
```

Nguyên tắc: **Có module nào trong bản build thì đăng ký module đó, vắng mặt thì bỏ qua một cách an toàn.**

| Tiêu chí | Cơ chế thực thi |
| :--- | :--- |
| **Phạm vi quét** | File `*.dll` trong thư mục output có tên bắt đầu bằng `Module.`, `Modules.`, `ShareDataWorker`, hợp nhất với assembly đã nạp trong tiến trình (deduplicate) |
| **Mẫu tên method** | Bắt đầu bằng `Add`, kết thúc bằng `Module` hoặc `CoreServices` (ví dụ `AddShareDataWorkerCoreServices`) |
| **Chữ ký hợp lệ** | `(IServiceCollection)` hoặc `(IServiceCollection, IConfiguration)` |
| **Thứ tự ưu tiên** | Priority map tương ứng với `Startup.cs` (`AddSystemModule` = 0 → `AddDataTransporterBaseModule` = 5 → `AddDataTransporterItsModule` = 10 → `AddReportBaseModule` = 20 → `AddReportModule` = 21 → các module khác = 50), tie-break theo tên (Ordinal) |
| **Cô lập lỗi** | Mỗi lời gọi đăng ký đều có checkpoint `services.Count`. Nếu có ngoại lệ phát sinh, Host sẽ rollback sạch các descriptor của module đó, in nguyên nhân gốc (`TargetInvocationException.InnerException`) và tiếp tục mà không làm crash Test Host |

Build hiện tại quét và đăng ký thành công **27 DI extensions**:
```text
[MODULE] ✔ AddSystemModule (Modules.System)
[MODULE] ...
[MODULE] Đã đăng ký 27 module, 0 lỗi.
```

---

## Cơ chế biên dịch có điều kiện (`test.csproj`)

Tất cả các module và phân hệ được quản lý linh hoạt qua cờ MSBuild trong `test.csproj`. Khi một phân hệ hoặc service vắng mặt trong repo, source code kiểm thử tương ứng sẽ tự động bị loại khỏi biên dịch (`Compile Remove="..."`), đảm bảo project test luôn biên dịch thành công 0 lỗi.

| Cờ thuộc tính | Điều kiện kiểm tra tồn tại | Tác động khi `false` |
| :--- | :--- | :--- |
| `HasShareDataWorker` | `ShareDataWorker.csproj` & `ShareDataWorker.Core.csproj` | Bỏ `ProjectReference` tới ShareDataWorker |
| `HasShareDataModule` | `Module.ShareData.csproj` | Kết hợp để tính `CanCompileShareDataTests` |
| `CanCompileShareDataTests` | `HasShareDataWorker` = `true` & `HasShareDataModule` = `true` | Khi `false`: `Compile Remove="ITS\ShareData\**\*.cs"` |
| `HasVideoWallModule` | `Module.VideoWall.csproj` | Khi `false`: `Compile Remove="ITS\VideoWall\**\*.cs"` |
| `CanCompileVideoWallTests` | `HasVideoWallModule` = `true` & tồn tại `IVwISAPIDeviceClient.cs` | Điều khiển biên dịch tầng nghiệp vụ VideoWall |
| `HasVideoWallWpf` | `ITS.VideoWall.WPF.csproj` | Bật target framework `net10.0-windows` và reference WPF |
| `CanCompileVideoWallWpfTests`| `HasVideoWallWpf` = `true` & `TargetFramework == 'net10.0-windows'` | Khi `false`: `Compile Remove="ITS\VideoWall\Wpf\**\*.cs"` |

Mỗi phân hệ tự quản lý các file cấu hình và mock riêng trong thư mục của mình:
- `ITS/ShareData/GlobalUsings.ShareData.cs` & `Host.ShareData.cs`
- `ITS/VideoWall/GlobalUsings.VideoWall.cs` & `Host.VideoWall.cs` (sở hữu MockServer, nối vào Host qua partial method `StartModuleTestServers()` và `StopModuleTestServers()`).

---

## Quy ước viết bài kiểm thử

- **1 file test / 1 class nguồn**: Tên file khớp 1-1 với class nghiệp vụ kèm hậu tố `Tests.cs` (ví dụ `VwSchedule` → `VwScheduleTests.cs`).
- **Namespace chuẩn**: Đặt theo định dạng `Tests.<TênPhânHệ>...` trong thư mục `tests/BE/ITS/<TênPhânHệ>/`. *(Lưu ý: Không đặt namespace là `Tests.ITS.*` để tránh xung đột tên phân giải với namespace `ITS` của các thư viện bên ngoài)*.
- **Wolverine Message Bus**: Gọi nghiệp vụ trực tiếp qua `IMessageBus` (bypass Controller/HTTP) để tối đa hoá tốc độ thực thi, không vướng xác thực JWT và hỗ trợ debug breakpoint dễ dàng.
- **Query phân trang**: Các truy vấn phân trang trả về `SqlSugarPagedList<T>`, đọc dữ liệu danh sách qua `.Records`.
- **Negative Test First**: Với các Command có Validator, luôn kiểm tra trường hợp dữ liệu không hợp lệ trước khi kiểm tra luồng ghi CSDL.
- **IStringLocalizer thực**: Validator lấy localizer từ `host.Localizer` (sử dụng `JsonStringLocalizerFactory` thực đã nạp trong Host), không sử dụng mock giả lập.
- **Cô lập dữ liệu**: Tự dọn sạch dữ liệu thử nghiệm trong `Dispose()` / `DisposeAsync()` theo tiền tố riêng (`TEST_VWCTRL_`, `TEST_VWSCN_`, `TEST_SD_`...), không làm ảnh hưởng bảng của phân hệ khác.
- **XML Documentation**: Bắt buộc gắn XML summary 3 dòng chuẩn trên mọi class/method: `Author: Đạt` / `Description:` / `Created date: dd/MM/yyyy`.

---

## Tài liệu tham chiếu liên quan

- 📖 **Đặc tả Vendor Specification:** [`09-api-reference.md`](../../DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/doc/ISAPI-Videowall-Controller/09-api-reference.md)
- 🗺️ **Danh mục API Catalog:** [`00-api-catalog.md`](../../DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/doc/ISAPI-Videowall-Controller/00-api-catalog.md)
