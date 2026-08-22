# TAC_WebAPI — Integration Tests

Bộ test tích hợp cho các module trong `TAC_WebAPI`. Project test nằm **ngoài** repo product
`TA-ITS015-WEBAPI-V1.0` (không commit vào repo product); mọi đường dẫn tới source đi qua property
`$(RepoRoot)` trong `test.csproj`.

## Cách chạy

Solution riêng cho test: `tests/TA-ITS015.Tests.sln` (gồm `test.csproj` + `TAC_WebAPI.csproj` để
navigate/debug xuyên sang source product). Mở solution này khi làm việc với test, không cần mở
`TAC_WebAPI.sln` của repo product.

```bash
# Toàn bộ test suite (qua solution hoặc qua csproj, kết quả như nhau):
dotnet test tests/TA-ITS015.Tests.sln
dotnet test tests/test.csproj

# Chỉ một phân hệ:
dotnet test tests/test.csproj --filter "FullyQualifiedName~VideoWall"

```

Cần SQL Server local chạy sẵn tại `localhost,14333` (trên macOS Apple Silicon **bắt buộc** dùng image
`mcr.microsoft.com/azure-sql-edge:latest`). Toàn bộ connection string trong `Host.cs` là In-Memory và
bị guard Zero-Trust chặn nếu trỏ ra ngoài `localhost` / `127.0.0.1` / `(localdb)` / `.`.

## Kết quả hiện tại

**227 test — 223 Passed, 4 Skipped, 0 Failed** (~30 giây).

| Nhóm | File | Số test | Ghi chú |
| --- | --- | :---: | --- |
| **VideoWall — Controllers** | `VwControllerTests.cs` | 21 | CRUD + `GetInputChannels` + CircuitBreaker + Negative Validation |
| | `VwSceneTests.cs` | 29 | CRUD + workflow kích hoạt kịch bản + multi-controller + `VwEventTriggerLog` |
| | `VwWindowSceneTests.cs` | 27 | CRUD + boundary + `SwitchSource` + `SetLayer` Z-Order |
| | `VwEventRuleTests.cs` | 19 | CRUD `VwEventRule` |
| | `VwScheduleTests.cs` | 19 | CRUD `VwSchedule` |
| | `VwScreenTests.cs` | 19 | CRUD `VwScreen` |
| | `VwSlotPortTests.cs` | 19 | CRUD `VwSlotPort` |
| | `VwSourceTests.cs` | 18 | CRUD `VwSource` |
| **VideoWall — Services** | `VwISAPIDeviceClientTests.cs` | 25 | 13 ISAPI method, Digest Auth, Circuit Breaker, Timeout, Multi-port |
| | `VwSceneRegionServiceTests.cs` | 11 | Kiểm tra vùng, quy đổi toạ độ uniformCoordinate (1920×1920), cắt lát đa controller |
| | `VwISAPIDeviceServiceTests.cs` | 9 | Digest Credentials, Base URI, refresh credential theo IP/Port |
| | `VwOrgAccessServiceTests.cs` | 7 | Phân quyền đơn vị OrgId: FullAccess vs. Restricted Org |
| **VideoWall — Smoke** | `VwISAPIDeviceSmokeTests.cs` | 4 | **Skipped có chủ ý** — chỉ chạy tay khi có thiết bị Hikvision thật |

Toàn bộ test VideoWall chạy qua `VwISAPIMockServerHikvision` (HttpListener thật trên `127.0.0.1`,
port 18080-18083), không phụ thuộc thiết bị phần cứng hay mạng ngoài.

---

## Kiến trúc Test Host (`Host.cs`)

`Host` là **Generic Host độc lập** (`GenericHost.CreateDefaultBuilder().Inject().UseLamar()`), *không*
phải `WebApplicationFactory`. Cấu hình nạp 100% từ bộ nhớ (`AddInMemoryCollection`) sau khi
`configBuilder.Sources.Clear()`, nên không đọc `appsettings.json` của bất kỳ project nào.

Thứ tự dựng service trong `RegisterTestServices` chép theo `AddSharedInfrastructure` của
`Shared.Infrastructure.dll`, **bỏ duy nhất `LicenseService`**. Vì bỏ `SetDbConfig` của DLL nên Test Host
phải tự làm 3 việc mà DLL/pipeline web vốn làm hộ:

| Việc | Vì sao bắt buộc |
| --- | --- |
| `SqlSugarSetup.ITenant = sqlSugarScope.AsTenant()` | `SetDbConfig` của DLL gán static này. Thiếu nó, ctor không tham số của `SqlSugarRepository<T>` (mà `BaseRepository<T>` của **mọi** module kế thừa) đổ `NullReferenceException` → toàn bộ Wolverine handler đỏ |
| `BindFurionRootServices(...)` | App thật gán `App.RootServices` trong `app.UseInject()`. Generic Host không có pipeline web nên phải gán trực tiếp vào `Furion.InternalApp.RootServices` |
| `column.DefaultValue = "0"` (không bọc nháy) | SqlSugar tự sinh `DEFAULT('0')`. Truyền `"'0'"` sẽ sinh `DEFAULT(''0'')` → SQL Server báo `Incorrect syntax near '0'` ngay tại bảng đầu tiên |

Khởi tạo schema gọi `InitTables(mảng)` một lượt cho toàn bộ entity. Lưu ý thứ tự trong
`InitializeAsync`: **schema init (bước 4) chạy TRƯỚC `ClearAllData()` (bước 5)**, nên bảng vẫn còn dữ
liệu của lần chạy trước lúc `ALTER`. Nếu một entity thêm cột `NOT NULL` mà bảng tương ứng đang có dòng,
SQL Server từ chối `ALTER` và Host không khởi tạo được → mọi test đỏ với lỗi SQL đó.

Cách xử lý khi gặp: đọc tên bảng trong thông báo lỗi rồi `TRUNCATE TABLE <tên>` (hoặc `DROP TABLE`) trong
DB `test` để CodeFirst tạo lại. Muốn triệt gốc thì đảo thứ tự bước 4 và 5 (`CreateDatabase` → truncate →
`InitTables`) để `ALTER` luôn chạy trên bảng rỗng.

### Cảnh báo về dữ liệu DB test

`InitializeAsync` gọi `ClearAllData()` → **TRUNCATE toàn bộ bảng entity** (hiện tại 103 bảng) mỗi lần
chạy test, không chỉ bảng của module đang kiểm thử. Database `test` hiện không phải scratch DB (còn
bảng Hangfire và `Sys*` mang dữ liệu thật), nên đừng để dữ liệu cần giữ trong các bảng có
`[SugarTable]`. Các bảng không thuộc entity type (Hangfire `Job`/`State`, `SysMenu`, `SysFile`...) không
bị ảnh hưởng.

---

## Auto-discovery module (nằm trong `Host.cs`)

`Host.cs` **không** gọi cứng `AddSystemModule` / `AddShareDataModule` / `AddVWModule` nữa và không
`using` bất kỳ namespace module nào. Bước 12 của `RegisterTestServices` gọi:

```csharp
RegisterDiscoveredModules(services, config);
```

Nguyên tắc: **có module nào thì DI module đó, không có thì bỏ qua.**

| Khía cạnh | Quy tắc |
| --- | --- |
| Phạm vi quét | File `*.dll` trong thư mục output có tên bắt đầu `Module.`, `Modules.`, `ShareDataWorker`, hợp nhất với assembly đã nạp trong tiến trình (dedupe theo danh tính) |
| Mẫu tên method | Bắt đầu `Add`, kết thúc `Module` hoặc `CoreServices` (để nhận cả `AddShareDataWorkerCoreServices`) |
| Chữ ký hợp lệ | `(IServiceCollection)` hoặc `(IServiceCollection, IConfiguration)` |
| Thứ tự | Priority map bám `Startup.cs` (`AddSystemModule`=0 → `AddDataTransporterBaseModule`=5 → `AddDataTransporterItsModule`=10 → `AddReportBaseModule`=20 → `AddReportModule`=21 → còn lại 50), tie-break theo tên (Ordinal) để deterministic |
| Không có van chặn | Mọi extension quét được đều được gọi. Lưu ý: `Startup.cs` đang comment out `AddWPModule`/`AddWPReportModule` nhưng test host **vẫn nạp** chúng — đã kiểm chứng không gây lỗi. Muốn tắt module nào thì thêm nhánh loại trừ trong `RegisterDiscoveredModules` |
| Cô lập lỗi | Mỗi lời gọi có checkpoint `services.Count`; ném ngoại lệ thì rollback sạch descriptor của chính nó, in nguyên nhân **gốc** (unwrap `TargetInvocationException`) và **không** làm sập Host |

Build hiện tại quét ra và đăng ký **27 extension**. Log in ra mỗi lần chạy:

```
[MODULE] ✔ AddSystemModule (Modules.System)
[MODULE] ✘ Failed: AddXModule (Modules.X): <loại lỗi>: <nguyên nhân>
[MODULE] Đã đăng ký 27 module, 0 lỗi.
```

Dòng cuối là chỗ cần soi khi test đỏ vì thiếu service: số lỗi khác 0 nghĩa là có module không đăng ký được.

---

## Module vắng mặt: giải ở tầng biên dịch

Module bị xoá khỏi repo thì reference, lời gọi đăng ký, và source test của nó **cùng biến mất lúc biên
dịch**, project vẫn build xanh. Không có cơ chế Skip runtime nào — không cần, vì khi module vắng thì
test của nó cũng không tồn tại để mà chạy.

| Property | Điều kiện `Exists()` | Tác dụng khi `false` |
| --- | --- | --- |
| `HasShareDataWorker` | `ShareDataWorker.csproj` **và** `ShareDataWorker.Core.csproj` | Bỏ 2 `ProjectReference` |
| `HasShareDataModule` | `Module.ShareData.csproj` | (dùng cho `CanCompileShareDataTests`) |
| `CanCompileShareDataTests` | `HasShareDataWorker` **và** `HasShareDataModule` | `Compile Remove="Modules\ShareData\**\*.cs"` |
| `HasVideoWallModule` | `Module.VideoWall.csproj` | `Compile Remove="Modules\VideoWall\**\*.cs"` |

Reference **cứng** (thiếu là lỗi thật, không im lặng bỏ qua): `TAC_WebAPI.csproj`,
`Shared.Core.dll`, `Shared.Infrastructure.dll`, `Modules.System.dll`.

File chỉ dùng được khi module có mặt phải nằm **trong** thư mục module tương ứng để bị loại cùng nhau:
`Modules/VideoWall/GlobalUsings.VideoWall.cs` (global usings của phân hệ) và
`Modules/VideoWall/Host.VideoWall.cs` (`partial class Host` sở hữu `MockServer`, nối vào Host qua
`partial void StartModuleTestServers()` / `StopModuleTestServers()` — không có phần thân thì trình biên
dịch xoá luôn lời gọi trong `Host.cs`).

> Hiện tại `ShareDataWorker` và `ShareDataWorker.Core` **không còn source** trong repo, nên
> `tests/Modules/ShareData/**` đang bị loại khỏi biên dịch. Build in rõ điều này dưới nhãn
> `[TEST MODULES]`.

Một cờ MSBuild điều khiển đồng thời **3 thứ**, nên chúng luôn khớp nhau. Ví dụ `HasVideoWallModule`:

| Điều khiển | Ở đâu | Khi `false` |
| --- | --- | --- |
| Symbol `HAS_VIDEOWALL` | `test.csproj` mục 2.3 | `#if HAS_VIDEOWALL` trong `Host.cs` rỗng → không gọi `AddVWModule` |
| Biên dịch test | `test.csproj` mục 2.5 | `Compile Remove="Modules\VideoWall\**\*.cs"` |
| `Host.MockServer` | `Modules/VideoWall/Host.VideoWall.cs` | File không biên dịch → `partial void StartModuleTestServers()` mất phần thân → lời gọi trong `Host.cs` bị xoá |

---

## Quy ước viết test

- 1 file test / 1 class nguồn, tên khớp 1-1 + hậu tố `Tests` (`VwSchedule` → `VwScheduleTests.cs`).
- Namespace `Tests.Modules.<TênModule>`, đặt trong `tests/Modules/<TênModule>/`.
- Gọi nghiệp vụ trực tiếp qua Wolverine `IMessageBus` (bypass Controller/HTTP) → nhanh, bỏ qua JWT, bắt được breakpoint.
- Query phân trang trả `SqlSugarPagedList<T>`, đọc dữ liệu qua `.Records`.
- Command có Validator: kiểm thử **negative validation trước** khi `_db.Insertable` (Negative Test First).
- `IStringLocalizer` cho Validator lấy từ `host.Localizer` — dùng chính `JsonStringLocalizerFactory` đã đăng ký trong Host, không tạo test double.
- Tự dọn dữ liệu trong `Dispose()` theo prefix riêng của module (`TEST_VWCTRL_`, `TEST_VWSCN_`...), không đụng bảng module khác.
- XML summary 3 dòng bắt buộc trên mọi class/method: `Author: Đạt` / `Description:` / `Created date: dd/MM/yyyy`.

---

## Tài liệu tham chiếu liên quan

- 🌟 **Sổ tay Thực thi & Response Thực tế:** [`09B-practical-guide-and-tested-responses.md`](../DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/ISAPI-Videowall-Controller/09B-practical-guide-and-tested-responses.md)
- 📖 **Đặc tả Vendor Specification:** [`09A-api-reference.md`](../DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/ISAPI-Videowall-Controller/09A-api-reference.md)
- 🗺️ **Danh mục API Catalog:** [`00-api-catalog.md`](../DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/ISAPI-Videowall-Controller/00-api-catalog.md)
