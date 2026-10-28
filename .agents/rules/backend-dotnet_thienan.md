---
name: backend-dotnet
version: 1.0.0
priority: P1
trigger: model_decision
description: Backend .NET standards, Wolverine bus, SqlSugar ORM, Clean Architecture, and C# coding conventions.
---

# 🏗️ Quy Chuẩn Backend .NET & Clean Architecture — Thiên Ân

> **Thuộc hệ thống SSOT `.agents/rules/`**. Quy định kiến trúc module, Wolverine bus, SqlSugar ORM, FluentValidation và quy chuẩn C# .NET.

---

## 🏛️ 1. Cấu Trúc Module Chuẩn (Layer Separation)

> Nguồn đối chiếu của team: bộ rule Kiro BE (`TA-ITS015-WEBAPI-V1.0/.kiro/steering/` gồm `coding-rules.md`, `structure.md`, `tech.md`, `product.md`; bản `src/.kiro/steering` giống hệt) và bản tóm tắt `DocBusinessThienAn/Rules_FE_BE.md` — **đã tích hợp đủ vào tệp này ngày 20/10/2026** (§1.5 tạo module, §1.6 công nghệ, §2 đặt tên/luồng CQRS/cache/Refit, §3 SqlSugar, §4.24 Base Helper/upload/thời gian); rule nào tệp này đã quy định chặt hơn thì theo tệp này. Các bản sao `DocBusinessThienAn/KiroR/` và `DocBusinessThienAn/Rules_FE_BE.md` đã xóa để bảo đảm SSOT tập trung tại `.agents/rules/`. FE: `frontend-vue_thienan.md` và `TA-ITS015-WEBAPI-V1.0/docs/PROJECT_STRUCTURE_WPF_WORKER_V3.md` (Service).

### 1.1. Thuật ngữ "phân hệ" (Module / Service)
* **Phân hệ** là một mảng nghiệp vụ: ShareData, VMS, VideoWall, TMS, VDS, Toll, CCTV, FMS, WP, Barrier, TrafficLight…
* Mỗi phân hệ có 1 hoặc cả 2 dạng triển khai:
  - **Module**: chạy trong host WebAPI `TAC_WebAPI`, đặt tại `src/Modules/<PhânHệ>/` (`Modules.<X>.Core` + `Modules.<X>`, có thể thêm `Modules.<X>.Report`).
  - **Service**: tiến trình chạy độc lập (Worker / Windows Service, WPF), đặt tại `src/Services/<PhânHệ>/`.
* Nói "phân hệ X" là gồm cả Module lẫn Service của X. Ví dụ phân hệ ShareData gồm Module `Module.ShareData*` và Service `ShareDataWorker*`.
* Không phải phân hệ: module hạ tầng dùng chung (`CfgSystem`, `DataTransporter`, `Mail`, `Media`, `Notification`, `Report`), mẫu (`Samplev2`, `Services/Sample`), thư viện chung (`src/Shared`, `src/Services/Shared`).

### 1.2. Cấu trúc Module
1. **`Modules.<X>.Core`**: chỉ chứa hợp đồng — Entity, DTO, Interface (chỉ loại được phép theo Rule 19.94, ⛔ không đặt interface 1:1 cho service), Enum, Constant, Exception/`BaseMsg`, Options, đăng ký DI (`Extensions`). ⛔ **CẤM** đặt logic xử lý nghiệp vụ trong Core của module (ngoại lệ: `Core/Helpers/` cho tiện ích mà cả WebAPI lẫn Service của phân hệ cùng dùng — §4.24).
2. **`Modules.<X>`**: Controllers, Commands/Queries (Wolverine), FluentValidation, DI Extensions và **logic xử lý tại `Infrastructure/Services/<ChứcNăng>/`**; mở thư mục chức năng chỉ thấy **một tệp service**, tệp khác vào thư mục con (Rule 19.94, §4.24). ⛔ Không tạo thư mục `Services/` ngang hàng `Infrastructure/`.
3. **Driver/giao thức với hệ thống hoặc thiết bị bên ngoài theo hãng** đặt tại `src/Modules/Integration/<PhânHệ>/` (`.Base` chứa interface, project theo hãng chứa phần cài đặt, project gốc làm manager), dùng bởi Module và/hoặc Service.
4. **Thư viện nền của cả tầng `src/Modules`** (`Modules.<X>.Core`, `Modules.<X>`, `Modules.Integration.*`): dùng chung bộ thư viện của dự án qua `Shared.Reference` (Furion, SqlSugar, FluentValidation, Mapster, Wolverine, Refit…) và **ưu tiên tính năng có sẵn của thư viện khi nó hỗ trợ ở nơi mã chạy**; ⛔ không tự dựng lại. Chi tiết và các trường hợp thư viện chưa hỗ trợ: Rule 19.94 (§4.24).

### 1.3. Cấu trúc Service
* Theo mẫu `src/Services/Sample` (V3): `Services.<X>` là thư viện lõi chứa logic dùng chung; `Services.<X>.Worker` / `Services.<X>.Wpf` là host mỏng, chỉ tham chiếu thư viện lõi; dùng `Services.Shared` / `Services.Shared.Runtime` (cấu hình nạp lại qua `IConfigLoader`, log, NATS).
* Thư viện lõi của **service** (`Services.<X>`, `ITS.<X>.Core`) **được** chứa logic — khác với Core của module ở mục 1.2.
* Service ưu tiên tham chiếu `Modules.<X>.Core` hoặc project Integration. Thư viện thuần mã (codec, formatter, connection keys/options, adapters...) dùng chung giữa module và service đặt thành project riêng `Modules.Integration.<PhânHệ>` (hoặc theo cấu trúc Integration), dùng thư viện nền như mọi project trong `src/Modules` (§1.2 mục 4); service **không** tham chiếu project module (chứa controller/handler). Việc service tham chiếu project module (hiện chỉ còn CCTV) là **độ lệch tạm thời**, không phải tiền lệ; phân hệ ShareData đã tách tầng gửi sang `Modules.Integration.Delivery`.
* **Service không bật Furion** (cập nhật 20/10/2026, khảo sát `src/Services`): cả 10 worker đều không gọi `Inject` — `Host.CreateApplicationBuilder` + `HostProfiles.ConfigureWorkerService` (Sample, VDS, VideoWall, WP_Sync) hoặc `WebApplication.CreateBuilder` (Barrier, Toll, TrafficLight, VMS, CctvProxy); ShareDataWorker dùng `Host.CreateDefaultBuilder`. Đăng ký DI tay bằng **chính lớp cụ thể** trong một hàm `Add<X>Services(configuration)` (`AddSampleServices`, `AddVdsServices`, `AddShareDataWorkerCoreServices`) hoặc trong `Program.cs` (VMS) — đúng mẫu `TA-ITS015-WEBAPI-V1.0/docs/PROJECT_STRUCTURE_WPF_WORKER_V3.md` (`services.AddSingleton<…>()`); Kiro BE chỉ quy định WebAPI, không có `src/Services`; `AddHostedService<>` viết tay; hạ tầng dùng chung lấy từ `Services.Shared.Runtime` (`AddServiceFoundation`, `HostProfiles`). Thư viện Integration chỉ chạy dưới Service (`Modules.Integration.Delivery`, `Modules.Integration.Barrier.Base`, `Modules.Integration.VMS/…/BaseManager`) cũng không dùng Furion (không marker, không `Oops`/`App`) và tự đăng ký tay trong hàm `Add<ThưViện>()`. ⛔ Không gắn marker Furion vào `Services.Shared.*` (dùng chung 7 service). Lý do: buổi trao đổi [12:51] "cái service kia chưa có thống nhất cái cấu trúc"; ShareDataWorker từng bật `Inject(false)` (19/10/2026) rồi hoàn tác (20/10/2026) vì lệch 9 worker còn lại, kéo theo nạp mọi `*.json`, phải chặn `BackgroundService` public, xung đột trạng thái tĩnh `App` trong tiến trình test.

### 1.4. Lệch hiện có
* Các chỗ lệch dưới đây chỉ chuẩn hóa khi có task riêng, không tự sửa kèm task khác:
  - VDS/TMS để quy tắc thuần trong Core (`Modules.VDS.Core/Topology`, `Media`, `Modules.TMS.Core/Validators`).
  - CCTV, FMS, WP dùng một project có thư mục `Core/` bên trong (mẫu cũ của `.kiro/steering`); CfgSystem có cả project `Modules.CfgSystem.Core` lẫn thư mục `Core/` trong `Modules.CfgSystem`.
  - Tên project lẫn `Module.*` / `Modules.*`; service lẫn `ITS.<X>.*` / `Services.<X>.*` / `ShareDataWorker`.
  - ShareData để bộ tiện ích có logic dùng chung giữa WebAPI và Worker trong Core (`Module.ShareData.Core/Helpers`: `SqlValidatorHelper`, `SqlParameterHelper`, `ConfigValueParser`) vì Worker chỉ tham chiếu Core; giữ nguyên tới khi có task tách project riêng.

### 1.5. Tạo Module Mới (tích hợp từ Kiro BE, 20/10/2026)
* **Tên project & namespace**: `Modules.<TênModule>` (`Modules.TMS`, `Modules.VMS`); phần báo cáo `Modules.<TênModule>.Report`; namespace theo tên project (`namespace Modules.TMS.Controllers.Equipment`). Tên lệch đã có (`Module.CCTV`, `Module.ShareData`) giữ nguyên (§1.4).
* **Tiền tố module** dùng cho mọi tên (entity, controller, service, khóa cache):

  | Module | Tiền tố | BasePath | GroupName |
  |---|---|---|---|
  | TMS | `Tms` | `api/tms` | `TMS` |
  | VMS | `Vms` | `api/vms` | `VMS` |
  | FMS | `Fms` | `api/fms` | `FMS` |
  | CCTV | `Cctv` | `api/cctv` | `CCTV` |
  | CfgSystem | `Cfg` | `api/cfgSystem` | `CfgSystem` |
  | Sample | `Samp` | `api/sample` | `Sample` |
  | ShareData | `ShareData` | `api/sharedata` | `ShareData` |
* **Tệp bắt buộc của module**:
  - `GlobalUsings.cs`: `Furion`, `Furion.ConfigurableOptions`, `Furion.DependencyInjection`, `Furion.DynamicApiController`, `Furion.FriendlyException`, `Microsoft.AspNetCore.Mvc`, `Microsoft.Extensions.Options`, `Newtonsoft.Json`, `SqlSugar`, `System.ComponentModel`, `System.ComponentModel.DataAnnotations`, `System.Data`, `Wolverine.Attributes`; và **`[assembly: WolverineModule]`** (thiếu thì Wolverine không tìm handler của module).
  - `Controllers/BaseController.cs`: mẫu ở mục dưới.
  - `Extensions/ModuleExtensions.cs`: `Add<TênModule>Module(services, configuration)` gọi lần lượt `.Add<TênModule>Core()`, `.Add<TênModule>Infrastructure(configuration)`, `.Add<TênModule>Validation()`.
  - `Extensions/ServiceCollectionExtensions.cs`: ba hàm trên; `Add<TênModule>Validation` gọi `services.AddControllers().Add<TênModule>Validation()`.
  - `Infrastructure/BaseRepository.cs`: `public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()` (`Shared.Infrastructure.Persistence.SqlSugar`).
* **Đăng ký vào host**: thêm `.Add<TênModule>Module(_config)` trong `RegisterModules` của `TAC_WebAPI/Startup.cs` và `<ProjectReference>` trong `TAC_WebAPI.csproj`; thêm project vào `.sln` theo Rule 19.71/19.93 (§4.12, §4.23).

### 1.6. Công Nghệ Nền (tích hợp từ Kiro BE `tech.md`)
* ASP.NET Core 10 theo mẫu `Startup`/`Program` (không minimal API); Furion 4.9 (DI, dynamic API controller, friendly exception, localization); **Lamar** làm IoC container; Wolverine 5.x (CQRS qua `MessBus.InvokeAsync`).
* SqlSugar + SQL Server (`BaseRepository<T>`, xóa mềm kèm ghi nhật ký thay đổi `InsertWithDiffLogAsync`/`UpdateWithDiffLogAsync`); FusionCache nhiều tầng + Redis (`StackExchange.Redis`), xóa cache bằng `RemoveByPrefixKey`.
* Thời gian thực: NATS.Net, SocketCluster, `ItsDataTransporter` (phát trạng thái thiết bị). Việc định kỳ: Hangfire lưu SQL Server (bảng điều khiển `/jobs`).
* FluentValidation 12.x, Mapster (`command.Adapt<T>()`); báo cáo FastReport (DLL trong `DLLs/`), Carbone, Magicodes.IE (Excel); HTTP: Refit, Flurl; bảo mật JWT Bearer, BouncyCastle, khóa `TAKey.key`/`TAKeyData.key` (⛔ không commit); log NLog; Swagger qua `AddSpecificationDocuments()` của Furion, versioning `Microsoft.AspNetCore.Mvc.Versioning`.
* Mọi gói NuGet khai tập trung ở `Shared/Shared.Reference/Shared.Reference.csproj`; module tham chiếu `Shared.Reference`. C# bật nullable + implicit usings.
* Lệnh: `dotnet run --project TAC_WebAPI` (từ `src/`). Kiro ghi `dotnet build TAC_WebAPI.sln` — ⛔ trên macOS không build `.sln`, build từng `.csproj` (Rule P0 trong `thienan_rules.md`).

### Cấu Hình `BaseController` & Swagger Auto-Discovery
* Mỗi Module bắt buộc có `BaseController.cs` riêng tại `Modules.[TênHệ].Controllers`.
* Khai báo hằng số `GroupName` và `BasePath`, gắn `[ApiDescriptionSettings(GroupName)]`:
```csharp
using Shared.Infrastructure.Controllers;
using Wolverine;

namespace Module.ShareData.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/sharedata";
        protected internal new const string GroupName = "ShareData";
    }
}
```

---

## ⚙️ 2. Wolverine Bus, CQRS & Validation

### Cấu Trúc Thư Mục Chức Năng (`Controllers/<ChứcNăng>/`)
* **`<ChứcNăng>Controller.cs`**: Thin Controller, **BẮT BUỘC chỉ gọi `MessBus.InvokeAsync()`**. Tuyệt đối không viết business logic tại controller.
* **`Commands/`**: Handler xử lý Ghi (Add/Update/Delete). Bắt buộc implement `IWolverineHandler` với phương thức `HandleAsync(<InputType> command)`. Dùng Mapster để ánh xạ DTO sang Entity.
* **`Queries/`**: Handler xử lý Đọc (Page/GetList/GetById). Truy vấn phân trang trả về `SqlSugarPagedList<Output>`, dùng `.OrderBuilder()` và `.ToPagedListAsync()`. Khi cần sắp xếp bản ghi mới nhất, bắt buộc dùng `BaseConst.SortFieldConst.UpdateOrCreateTime` (Rule 19.60). Trả thẳng đối tượng phân trang, **CẤM** bọc qua `.Adapt<SqlSugarPagedList<TOutput>>()`.
* **`Dto/`**:
  - Input: `PageXxxInput` (kế thừa `BasePageInput`), `AddXxxInput` (kế thừa Entity), `UpdateXxxInput`, `DeleteXxxInput`.
  - Output: `XxxOutput` / `PageXxxOutput`. Cấu hình ánh xạ Mapster (`IRegister`) viết trực tiếp trong file DTO Output, **CẤM** tạo folder `Mappings/` riêng.
  - ⛔ **CẤM dùng DataAnnotation** (`[Required]`, `[Range]`, `[StringLength]`). Toàn bộ kiểm tra dữ liệu thực hiện 100% qua FluentValidation.
* **`Validators/`**: Chứa `AbstractValidator<T>` nhận `IStringLocalizer lz` qua DI. Không tạo class phụ/mock hay constructor không tham số.
* **Repository Naming**: Đặt tên theo prefix `_rsp{EntityName}` (VD: `SqlSugarRepository<EshPartner> _rspEshPartner;`).

### Đặt Tên Theo Chức Năng (tích hợp từ Kiro BE, 20/10/2026)
* **Controller**: `<TiềnTố><ChứcNăng>Controller` kế thừa `BaseController` của module, gắn `[ApiDescriptionSettings(GroupName, Order = …)]`. Hàm chuẩn: `Page(<PageInput>)` (phân trang), `GetList([FromQuery] <Input>)` (cả danh sách), `Add<ChứcNăng>`, `Update<ChứcNăng>`, `Delete<ChứcNăng>`, `BatchDelete<ChứcNăng>(List<DeleteInput>)` — bốn hàm ghi gắn `[HttpPost]`. Mọi hàm chỉ gọi `MessBus.InvokeAsync<TResult>(input)` / `MessBus.InvokeAsync(input)`.
* **DTO** (`Dto/`): `Page<ChứcNăng>Input : BasePageInput`; `<ChứcNăng>Input` (lọc danh sách); `Add<ChứcNăng>Input : <Entity>`; `Update<ChứcNăng>Input : Add<ChứcNăng>Input`; `Delete<ChứcNăng>Input : BaseIdInput`; đầu ra `<ChứcNăng>Output : <Entity>`, `Page<ChứcNăng>Output : <Entity>`, `<TiềnTố><ChứcNăng>DataOutput : <Entity>` (có join).
* **DTO dùng chung giữa module** (gọi qua Refit) đặt ở `Shared.DTO/Dtos/<Module>/`: `<TiềnTố><Entity>Request`, `<TiềnTố><Entity>ListRequest : BasePageReqDto`, `<TiềnTố><Entity>ResponseBase : BaseEntityDto`, `<TiềnTố><Entity>Response : <ResponseBase>`.
* **Handler**: `<ChứcNăng>CommandHandler : IWolverineHandler`; `<ChứcNăng>QueryHandler` không cần interface. Chữ ký `public async Task HandleAsync(<Input> command)` hoặc `Task<<Output>>`. Tiêm qua hàm khởi tạo: `BaseRepository<T>`, `BaseCacheService`, `IMessageBus`, service của module.
* **Một chức năng (một Controller) = một `<ChứcNăng>CommandHandler` + một `<ChứcNăng>QueryHandler`** (27/10/2026): mọi `HandleAsync` ghi của controller gom vào `Commands/<ChứcNăng>CommandHandler.cs`, mọi `HandleAsync` đọc gom vào `Queries/<ChứcNăng>QueryHandler.cs`; **CẤM** tách thêm handler theo nhánh con (vd. `PacketExtractSqlCommandHandler`, `PacketWriteSqlQueryHandler`). Nhóm nhánh con trong lớp bằng `#region <Nhánh>`; logic dùng chung hoặc lớp quá dài thì tách service (mục dưới), không tách handler.
* **Validator**: `<ChứcNăng>Validator : AbstractValidator<<Entity>>` (luật chung); `Add<ChứcNăng>Validator` gọi `Include(new <ChứcNăng>Validator(localizer))`; `Update<ChứcNăng>Validator` gọi `Include(new <ChứcNăng>Validator(localizer, false))`; `Delete<ChứcNăng>Validator` kiểm `ID` không rỗng. Thông điệp qua `IStringLocalizer` với khóa `BaseMsg.BaseEntity.*`, `BaseMsg.BaseValidation.*`, `BaseMsg.BaseException.*`.
* **Khi nào tách service** (`Infrastructure/Services/<ChứcNăng>/<TiềnTố><ChứcNăng>Service.cs`, xem §4.24): CRUD đơn giản để ngay trong handler; tách service khi logic dùng lại ở nhiều handler/controller (cùng truy vấn cho Command + Query hoặc nhiều chức năng), handler quá dài, hoặc thao tác phức tạp trên entity (xóa dây chuyền, tính lại thứ tự, chuyển trạng thái, gọi API ngoài). Service tiêm được thẳng vào controller khi thao tác không hợp CQRS (`TmsEquipmentService` trong `TmsEquipmentController`). Tên field `_<featureService>` hoặc `_<service>`.

### Luồng CQRS Chuẩn & Cache (tích hợp từ Kiro BE)
* **Thêm mới**: `Controller.Add…` → `MessBus.InvokeAsync(input)` → `CommandHandler.HandleAsync`: kiểm trùng (`IsAnyAsync`) → trùng thì `throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code)` → `command.Adapt<Entity>()` → `_repo.InsertWithDiffLogAsync(entity)` → `_cache.RemoveByPrefixKey(...)` → phát sự kiện nếu cần (`ItsDataTransporter.BroadcastData(...)`).
* **Phân trang**: `_repo.AsQueryable().WhereIF(!string.IsNullOrWhiteSpace(command.X), u => u.X.Contains(command.X)).Select(u => new Output { }, true).OrderBuilder(command, "").ToPagedListAsync(command.Page, command.PageSize)`.
* **Danh sách có cache**: `_cache.GetOrAdd($"{CacheConst.<Module>.<Entity>}:{key}", _ => query)`.
* **Khóa cache**: khai ở `Shared.DTO/Constants/Application/CacheConst.cs`, lớp lồng `CacheConst.<Module>.<Entity>`, giá trị `<module>_<entity>` (`tms_equipment`, `vms_image`); ghi xong thì xóa bằng `_cache.RemoveByPrefixKey(CacheConst.<Module>.<Entity>)`.
* **Gọi API giữa các module**: interface Refit `I<ChứcNăng>Api : IBaseApi` ở `Shared.Utility/Apis/<Module>/` (loại interface được phép theo Rule 19.94), đường dẫn khai ở `Shared.Utility/Endpoints/BaseEndpoint.cs` (`[Get(BaseEndpoint.<Module>Ep.<ChứcNăng>.Base + "/list")]`), trả `Task<ApiResponse<WResult<…>>>`.

---

## 💾 3. Thực Thể (Entity) & SqlSugar ORM

### Quy Chuẩn Entity Class
* Tất cả Entity kế thừa `EntityTenant` (từ `Shared.Core.Domain`; có sẵn `ID`, `Code`, `TenantId` và các trường kiểm vết). Tên lớp `<TiềnTố><TênEntity>` và `[SugarTable("<TiềnTố><TênEntity>")]` (`TmsEquipment`, `VmsImage`).
* Ghi qua repository dùng `InsertWithDiffLogAsync` / `UpdateWithDiffLogAsync` để có nhật ký thay đổi và xóa mềm (Kiro BE `tech.md`).
* Base class sử dụng `CreateTime` và `UpdateTime` (ID viết HOA cả hai ký tự: `ID`).
* **Hằng số độ dài `EntityConst`**: BẮT BUỘC dùng `EntityConst.Length32`, `EntityConst.Length64`, `Length128`, `Length256`, `Length512`, `KeyFieldLength`. CẤM hardcode số (như `Length = 32`).
* ⛔ **CẤM gán `Length` cho kiểu không phải chuỗi (Lỗi SQL Server 2716)**: `Length = EntityConst...` CHỈ dùng cho chuỗi (`string` / `string?`). Tuyệt đối cấm gán vào `int`, `long`, `float`, `DateTime`, `bool`.
* **Quy định `IsNullable = true`**: Tất cả cột Entity (trừ khóa chính `ID`) bắt buộc khai báo `[SugarColumn(IsNullable = true, ...)]` và dùng kiểu nullable (`string?`, `DateTime?`, `int?`...).
* **Thuộc tính Entity không ánh xạ CSDL (Rule 19.68)**: BẮT BUỘC gắn `[SugarColumn(IsIgnore = true)]`. Chi tiết xem mục 4.10.

### Khóa Chính `ID` (Rule 19.44, cập nhật 19/10/2026 theo mã và CSDL thật)
* `ID` là chuỗi GUID. Bản ghi tạo qua WebAPI có dạng GUID tuần tự chữ thường (ví dụ `0dcdde08-134b-45cb-898b-59fd11a42504`). Không phân hệ nào dùng Snowflake/`YitIdHelper`.
* **WebAPI**: AOP của `Shared.Core` tự sinh `ID` khi Insert. ⛔ **CẤM tự gán tay `entity.ID`** trong WebAPI.
* **Service/Worker** (không qua AOP): gán `entity.ID = Guid.NewGuid().ToString()` trước khi insert — quy ước chung của VDS, Toll, TMS, CCTV (`Services/Toll/README.md`). Mã ShareData Worker đang dùng `.ToString("N")` thì giữ nguyên (Rule 19.89).
* **Seed trong mã** (`ISqlSugarEntitySeedData<T>`, hiện chỉ CfgSystem và Report): được dùng ID số cố định 13 chữ số theo nhóm (`1000000001000`, `1100000001001`), vì ID là khóa để nạp lại không trùng.
* **Script `.sql`** (seed, di trú, dữ liệu giả lập) — theo mẫu `Services/VDS/Config/20261008_VdsConfig_ZoneSlow_Seed.sql`:
  - ID sinh bằng `LOWER(CONVERT(NVARCHAR(64), NEWID()))` (chữ thường cho khớp ID do WebAPI sinh). ⛔ Không viết cứng ID số hay GUID trong script.
  - Chống chạy trùng bằng khóa nghiệp vụ kèm `IsDelete IS NULL` (`Code`; `GroupCode + Code + TenantId`; `TableName`…), không bằng `ID`.
  - Bản ghi con lấy ID cha bằng cách tra theo khóa nghiệp vụ (`SELECT @PacketId = ID FROM ShareDataPacket WHERE Code = … AND IsDelete IS NULL`).
  - Cần nhận lại các dòng do script tạo (để dọn hoặc làm lại) thì đánh dấu bằng `CreateUId` riêng (VDS: `N'VDS-SEED-20261008'`).

### Quy Tắc Thao Tác SqlSugar (P0)
1. **Ưu tiên ORM tuyệt đối khi đã có Entity (Rule 19.20)**: TUYỆT ĐỐI CẤM dùng raw SQL DML (`UPDATE`, `INSERT`, `DELETE`) thủ công.
2. **CẤM nối chuỗi SQL (Rule 19.17)**: Khi bắt buộc dùng SQL truy vấn động, BẮT BUỘC dùng tham số hóa qua `SugarParameter`.
3. **SqlSugar Client trong Worker (Rule 19.41)**: Trong Background Worker, Polling Loop hoặc Concurrent Tasks, BẮT BUỘC khởi tạo bản copy độc lập:
   ```csharp
   using var db = baseClient.CopyNew();
   ```
4. **Không tự dựng tính năng có sẵn (Rule 19.33)**: SqlSugar đã có API sẵn (`InsertReturnIdentity`, `UpdateColumns`, `SplitTable`...) thì BẮT BUỘC dùng, cấm tự viết lại.

---

## 🧹 4. Quy Chuẩn Code C# (Clean Code Standards)

1. **Zero Unnecessary Usings (IDE0005)**:
   - Kiểm tra `GlobalUsings.cs` trước khi thêm `using`. Nếu namespace đã có trong `GlobalUsings.cs`, **TUYỆT ĐỐI CẤM** thêm vào file riêng lẻ.
   - Luôn rà soát và dọn dẹp các `using` thừa sau khi sửa code.
2. **Không Dùng Hậu Tố `-Async` (No-Async Suffix)**:
   - Mọi phương thức trả về `Task` hoặc `Task<T>` đặt tên trực diện: `GetData()`, `ExecuteCommand()`, `ProcessQueue()`.
   - ⛔ KHÔNG đặt tên `GetDataAsync()`, `ExecuteCommandAsync()`. (Ngoại lệ: implement interface từ thư viện ngoài như Wolverine `HandleAsync`).
3. **Ưu tiên `var` cho biến cục bộ (IDE0007 / Rule 19.39)**:
   - Dùng `var` khi kiểu dữ liệu hiển nhiên hoặc có thể suy luận rõ ràng: `var entity = new EshPartner();`.
4. **Auto-Property & Cấm Backing Field Thừa (Rule 19.22)**:
   - Dùng auto-property: `public string Name { get; set; }`. Cấm tách `private string _name` nếu không có logic phụ trợ đặc thù.
5. **Xử lý Exception Tập Trung (Rule 19.19, 19.48)**:
   - ⛔ CẤM `try-catch` dồn dập, lồng nhau nhiều lớp.
   - ⛔ CẤM nuốt lỗi ở hàm con / helper (`catch { return null; }`). Để exception ném tự nhiên lên orchestrator / tầng cha điều phối để log và rollback giao dịch.
6. **Thứ Tự Thành Viên Class (Rule 19.47)**:
   - Cấu trúc theo nhóm: Constants/Fields tĩnh -> Fields/Dependencies DI -> Kiểu lồng (record/class/struct/enum) -> Constructor -> Public Methods -> Protected/Internal Methods -> Private Helper Methods (chi tiết mục 17).
   - Khi thêm code mới, thêm vào cuối nhóm tương ứng (Append-only).
7. **Tôn Trọng Phong Cách Mã Nguồn & Signature (Rule 19.49, 19.50, 19.51)**:
   - Cấm tự ý đổi signature phương thức / service dùng chung.
   - Cấm sinh hàm mới làm hàm cũ mồ côi (dead code).
8. **Vị Trí `using` & Quy Chuẩn `namespace` (Rule 19.65)**:
   - **Vị trí `using` luôn đứng đầu file**: Mọi chỉ thị `using` **BẮT BUỘC đặt ở đầu tệp tin**, phía trước khai báo `namespace`. ⛔ **TUYỆT ĐỐI CẤM** đặt `using` sau khai báo `namespace` (kể cả khi dùng file-scoped namespace).
   - **Dùng `using`, cấm tên kiểu đầy đủ trong mã (bổ sung 25/10/2026)**: Cần kiểu ở namespace khác thì thêm `using <Namespace>;` đầu tệp rồi viết tên ngắn. ⛔ **CẤM** viết tên đầy đủ kèm namespace ngay trong khai báo hay thân hàm (vd ❌ `Module.ShareData.Core.Helpers.SqlVariableSet? Variables = null` → ✅ `using Module.ShareData.Core.Helpers;` + `SqlVariableSet? Variables = null`). Chỉ viết đầy đủ khi hai kiểu trùng tên thật sự xung đột; khi đó ưu tiên bí danh `using X = ...;` đầu tệp.
   - **Đồng nhất kiểu `namespace`**: Tuân thủ phong cách namespace của module / file gốc hiện hữu. Khi tạo file mới hoặc tách `partial class`, **BẮT BUỘC đồng nhất** với các file cùng class/module: nếu các file hiện tại dùng block-scoped namespace (`namespace X { ... }`) thì file mới bắt buộc dùng block-scoped namespace, ⛔ **CẤM** viết lẫn lộn file-scoped namespace (`namespace X;`) trong cùng một partial class.
9. **Can Thiệp Tối Thiểu & Cấm Sửa Code Ngoài Phạm Vi (Minimal Diff - Rule 19.6, 19.35)**:
   - Chi tiết xem [thienan_rules.md](./thienan_rules.md) bảng P0 quy tắc #13.
10. **Xóa Triệt Để Mã Không Dùng & Thuộc Tính Entity Không Ánh Xạ (Rule 19.68)**:
    - **Bỏ là bỏ hết các liên đới (Cascading Cleanup)**: Khi bỏ một tính năng, nút bấm hoặc API: BẮT BUỘC xóa sạch các thành phần liên đới từ Frontend đến Backend (handler, DTO, hằng số `BaseMsg`, script dịch thuật, test case). Tuyệt đối không để lại mã chết / zombie code.
    - Đối với code C# thông thường (service, helper, util, DTO, class, method, handler...): Khi không dùng hoặc đã được thay thế bằng giải pháp mới, **BẮT BUỘC XÓA BỎ HOÀN TOÀN (Clean Delete)** khỏi solution.
    - ⛔ **TUYỆT ĐỐI CẤM** để lại mã rác rồi gắn thuộc tính `[Obsolete]` làm bẩn codebase và sinh warning trình biên dịch (CS0618).
    - Mọi test case kiểm thử mã cũ cũng phải được xóa bỏ hoặc thay thế tương ứng bằng bài test cho giải pháp mới.
    - **Thuộc tính Entity không ánh xạ CSDL**: Nếu Entity class có thuộc tính phụ trợ, tính toán, trạng thái runtime hoặc tạm thời không lưu trữ trong database, **BẮT BUỘC gắn `[SugarColumn(IsIgnore = true)]`** để SqlSugar bỏ qua không sinh/truy vấn cột trên bảng CSDL, tránh lỗi lệch schema (`Invalid column name`).
11. **Chuẩn Hóa Thông Báo Lỗi & Ngoại Lệ — Cấm Hardcode String Trong `Oops.Oh` (Rule 19.69)**:
    - ⛔ **TUYỆT ĐỐI CẤM** hardcode chuỗi string trực tiếp (tiếng Việt hoặc tiếng Anh) trong `Oops.Oh("...")` hay `throw new AppFriendlyException("...")`.
    - **Định danh thông báo qua `BaseMsg`**: Mọi thông báo lỗi, thông điệp kiểm tra dữ liệu (validation), thông báo ngoại lệ nghiệp vụ (exception) **BẮT BUỘC** khai báo tập trung trong `BaseMsg` của module (kế thừa `BaseLocaleManager`) theo tiền tố chuẩn:
      - `lz.validation.<module>.<mãKey>` (hoặc `BaseMsg.BaseValidation.*`): Dùng cho lỗi kiểm tra dữ liệu đầu vào.
      - `lz.exception.<module>.<mãKey>` (hoặc `BaseMsg.BaseException.*`): Dùng cho lỗi logic nghiệp vụ, vi phạm trạng thái.
      - `lz.entity.<module>.<mãKey>` (hoặc `BaseMsg.BaseEntity.*`): Dùng cho tên trường, tên thực thể phục vụ ghép vào placeholder `{0}`, `{1}`.
    - **Tham số hóa thông điệp qua Furion**: Khi thông báo cần chèn tham số động (phạm vi, tên đối tượng, mã lỗi), sử dụng placeholder `{0}`, `{1}` trong CSDL và truyền đối số vào `Oops.Oh`:
      ```csharp
      throw Oops.Oh(BaseMsg.ShareData.Validation.LookbackDaysRange, 1, 365);
      ```
    - **Cấu hình trên CSDL `SysTerminology`**: Mọi key thông báo mới **BẮT BUỘC** tạo script SQL idempotent (`DocBusinessThienAn/**/sql/*.sql`) để seed vào bảng `SysTerminology` trên CSDL với đầy đủ 2 ngôn ngữ (`Lang = 'vi-VN'` và `Lang = 'en-US'`), `Status = 1`:
      - `Name`: Tiền tố nhóm (`lz.validation`, `lz.exception`, `lz.entity`...).
      - `Code`: Mã phân cấp sau tiền tố (ví dụ `sharedata.lookbackDaysRange`, `cctv.deviceNotFound`).
      - `Value`: Nội dung thông điệp (hỗ trợ tham số hóa qua placeholder `{0}`, `{1}`...).
    - **Cấm sửa tay file tĩnh Backend**: ⛔ **TUYỆT ĐỐI CẤM** sửa tay vào `TAC_WebAPI/Resources/*.json` vì các file này được sinh tự động (auto-generated) từ CSDL `SysTerminology` qua endpoint `LoadServerTerm`.
12. **CẤM Tự Ý Thêm Project & Solution Folder Dư Thừa Vào Solution (`.sln`) (Rule 19.71)**:
    - Solution file chính (`src/TAC_WebAPI.sln`) là tài nguyên dùng chung của toàn hệ thống WebAPI.
    - ⛔ **TUYỆT ĐỐI CẤM** tự ý thêm các project không thuộc phạm vi phân hệ đang làm (như thêm nhầm VideoWall, các project dịch vụ ngoài, hoặc test project `tests/BE/test.csproj`) vào file solution WebAPI.
    - ⛔ **TUYỆT ĐỐI CẤM** tạo các Solution Folder trung gian dư thừa không cần thiết (như tạo thêm Solution Folder lẻ trong khi project có thể lồng trực tiếp vào folder gốc của module, ví dụ lồng thẳng vào `Modules`).
    - Khi bắt buộc phải thêm project của phân hệ vào solution (ví dụ `Modules.Integration.<PhânHệ>`):
      - Chỉ thêm đúng project cần thiết phục vụ build/chạy WebAPI.
      - Phải đăng ký đầy đủ và chính xác trong cả 3 khối: `Project(...)`, `ProjectConfigurationPlatforms`, và `NestedProjects` (trỏ đúng thư mục cha chuẩn, ví dụ `Modules`).
      - Không để sót cấu hình rác hoặc mapping mồ côi.
      - **Đồng bộ commit tệp `.sln` cùng lúc với `.csproj` và mã nguồn (Rule 19.93)**: Khi thêm project mới vào solution, bắt buộc `git add` và commit tệp `.sln` trong cùng commit với project đó; tuyệt đối cấm commit mã nguồn nhưng bỏ quên `.sln` khiến VS Code / Visual Studio không load được project.
13. **Quy Chuẩn Đặt Tên Kiểu Mới Trong Phân Hệ — Tránh Lặp Tiền Tố (Rule 19.73)**:
    - Khi kiểu mới đã nằm trong namespace hoặc thư mục của phân hệ (ví dụ `Modules.Integration.Delivery`, `Modules.ShareData` hoặc `ShareData/`): **TUYỆT ĐỐI KHÔNG lặp lại tiền tố của phân hệ ở tên kiểu mới**.
      - Ví dụ chuẩn: Viết `FormatContext` thay vì `ShareDataFormatContext`, `EncryptManager` thay vì `ShareDataEncryptManager`.
    - **Phạm vi áp dụng**: Bao gồm class, service trong thư mục chức năng (ví dụ thư mục ngoài mang tên `ShareDataConfig/`, tệp bên trong chỉ cần đặt `ConfigService.cs` thay vì `ShareDataConfigService.cs`; tiền lệ: VMS `Schedule/ScheduleService.cs`, `Template/TemplateService.cs`), interface (`IConnectHandler`, không phải `IShareDataConnectHandler`), enum, record, DTO và tên file (tên file = tên kiểu chính).
    - **Tiền tố cần tránh**: Tên phân hệ (`ShareData`, `VMS`, `TMS`...) khi namespace/project đã mang tên phân hệ (`Module.ShareData*`, `Modules.Integration.Delivery`, `ShareDataWorker*`).
    - **Ngoại lệ (giữ nguyên)**:
      - Các kiểu đã có ở `HEAD` (bảo toàn class/kiểu cũ để tránh xáo trộn codebase và giảm thiểu diff).
      - Controller (route, Swagger và client FE sinh theo tên, nên theo các `ShareData*Controller` hiện có).
      - Lớp bọc enum của phân hệ (`NotiEnums`, `TollEnums`, `ShareDataEnum` — Rule 19.90, mục 22).
      - Tên thư mục giữ nguyên theo các thư mục cũ cùng cấp (ví dụ `ShareDataExtraction/`, `ShareDataMetadata/`).
    - **Trùng tên kiểu khác trong solution**: Đặt tên mô tả khác, tuyệt đối không thêm lại tiền tố phân hệ (ví dụ đặt `PayloadDirection` vì `Direction` đã có ở `Shared.DTO/Enums/BaseEnums.cs`).
    - **Kiểm trước khi bàn giao**: Liệt kê khai báo kiểu mới trong `git diff HEAD` có tiền tố phân hệ → danh sách phải rỗng (ngoại trừ controller).
14. **Quy Chuẩn Ngắt Dòng Khi Gọi Hàm / Phương Thức Nhiều Tham Số (Rule 19.75)**:
    - Khi gọi hàm/phương thức có từ 3 tham số trở lên, hoặc tham số biểu thức dài/phức tạp, hoặc sử dụng Named Arguments (`paramName: value`):
      - **BẮT BUỘC xuống dòng ngay sau dấu mở ngoặc `(`**.
      - Mỗi đối số nằm riêng trên một dòng, thụt lề 1 cấp (1 tab hoặc 4 spaces).
      - **Dấu đóng ngoặc `);` nằm riêng trên một dòng mới**, thẳng hàng với dòng bắt đầu gọi phương thức.
      - ⛔ **TUYỆT ĐỐI CẤM** viết mở ngoặc `(` rồi dồn ngay tham số đầu tiên trên cùng dòng mà các tham số sau lại ngắt dòng lộn xộn.
      - **Phạm vi**: chỉ áp cho lời gọi/khai báo **mới viết** hoặc **có thêm đối số**. Dòng cũ không đổi nội dung, hoặc chỉ đổi tên biến/câu thông báo, thì giữ nguyên bố cục cũ — ⛔ không dùng rule này làm lý do định dạng lại dòng cũ (Rule 19.89, mục 21).
      - Ví dụ chuẩn:
        ```csharp
        // CHUẨN:
        var parameters = SqlParameterHelper.BuildParameters(
            lastTime: isSnapshot ? null : (command.FromTime ?? fromDate),
            lastKey: string.Empty,
            pageSize: safeTop,
            fromDate: fromDate,
            toDate: DateTime.Now
        );

        // ❌ CẤM (dồn dòng sau dấu ngoặc mở):
        var parameters = SqlParameterHelper.BuildParameters( lastTime: isSnapshot ? null : (command.FromTime ?? fromDate),
            lastKey: string.Empty, pageSize: safeTop, fromDate: fromDate, toDate: DateTime.Now );
        ```
15. **Cấm Khai Báo Lại / Bí Danh Hằng Của Lớp Khác & Hàm Bọc Mỏng (Rule 19.78)**:
    - ⛔ **CẤM** khai báo field/property/hằng chỉ để gán lại hoặc trỏ sang một thành viên của lớp khác mà không thêm ý nghĩa, ví dụ:
        `public static readonly HashSet<string> ColumnReservedKeys = ConnectConfigValidator.ColumnReservedKeys;`
      Đây là mã thừa: thêm một chỗ phải theo dõi, dễ lệch khi nguồn đổi và buộc đọc mã phải đi qua hai lớp.
    - ⛔ **CẤM** tạo các hàm static/instance bọc mỏng chỉ đơn thuần chuyển tiếp lời gọi hàm sang lớp khác (`return Other.Method(args);`) mà không bổ sung logic nghiệp vụ hay chuyển đổi dữ liệu. Nơi cần dùng thì tham chiếu thẳng nguồn gốc (`ConnectTypes.IsSupported(...)`, `ConnectConfigValidator.Check(...)`).
      ```csharp
      // CHUẨN:
      .Must(v => ConnectTypes.IsSupported(v))
      .Must(v => ConnectConfigValidator.Check(v) != ConnectConfigIssue.EmptyKey)

      // ❌ CẤM (bí danh):
      public static readonly HashSet<string> ColumnReservedKeys = ConnectConfigValidator.ColumnReservedKeys;

      // ❌ CẤM (hàm bọc mỏng):
      public static bool IsValidConnectionType(string type)
      {
          return ConnectTypes.IsSupported(type);
      }
      public static ConnectConfigIssue Check(string? configStr)
      {
          return ConnectConfigValidator.Check(configStr);
      }
      ```
    - Chỉ giữ bí danh / hàm bọc khi có lý do ghi rõ bằng comment, và thuộc một trong các trường hợp:
      (a) Đổi kiểu hoặc đóng gói để che giấu chi tiết triển khai (ví dụ trả về `IReadOnlySet` thay cho `HashSet`);
      (b) Giữ tương thích ngược cho API công khai có nơi dùng bên ngoài đã được kiểm chứng bằng `find_usages`;
      (c) Rút gọn biểu thức dài bằng biến cục bộ trong thân hàm (không phải field/method của lớp).
    - Cách kiểm tra trước khi bàn giao: `find_usages` trên mỗi field/hằng/hàm mới thêm; thành phần chỉ chuyển tiếp sang lớp khác và không có lý do (a)-(c) thì xóa.
    - Liên quan Rule 19.68 (xóa mã không dùng) và 19.33 (không dựng lại tính năng có sẵn).
16. **Cấm Tham Chiếu Prompt / Phiên AI & Cấm Ghi Nhật Ký Thao Tác / Đổi Tên Trong Comment (Rule 19.80, bổ sung 28/10/2026)**:
    - ⛔ **TUYỆT ĐỐI CẤM** ghi vào comment (XML doc `///`, `//`, `/* */`), mô tả test (`[Fact]`, `Description:`), chú thích SQL hoặc tên hàm/biến các tham chiếu tới prompt, phiên AI hay số thứ tự yêu cầu giao cho AI.
    - Lý do: người đọc mã không có prompt đó, tham chiếu mất nghĩa và gây hiểu nhầm; mã phải tự giải thích.
      ```csharp
      // ❌ CẤM:
      // Chuẩn bị payload một lần theo định dạng và codec (Prompt #15)
      /// khác kỳ vọng "có cảnh báo NotReady" trong prompt
      /// xem mục 4.1 của prompt

      // CHUẨN (nêu lý do/hành vi, cần truy nguồn thì dùng mã hạng mục):
      // Chuẩn bị payload một lần theo định dạng, rồi lưu bằng chứng và gửi đúng bộ byte đó (R18)
      ```
    - **Cấm ghi nhật ký thao tác sửa đổi, đổi tên (Changelog / Rename Log) trong Comment/XML Doc**:
      - `/// Modified date: DD/MM/YYYY` (CHỈ ghi ngày tháng chuẩn, ⛔ **TUYỆT ĐỐI CẤM** mở ngoặc giải thích thao tác code hay lý do đổi tên, ví dụ: `(Đổi tên thành ...)`, `(Gom ... vào ...)`, `(Sửa theo Rule ...)`).
      - Lý do: Git version control (`git log`, `git blame`, `git diff`) là nguồn duy nhất quản lý toàn bộ lịch sử chỉnh sửa mã nguồn. Comment và XML doc chỉ giải thích mục đích nghiệp vụ, hợp đồng kỹ thuật và hành vi hiện tại của mã nguồn; không biến comment thành nơi ghi nhật ký thao tác code cá nhân làm rác mã nguồn.
      ```csharp
      // ❌ CẤM:
      /// Modified date: 28/10/2026 (Đổi tên thành SqlParameterHelper)
      /// Modified date: 28/10/2026 (Gom SqlValidationErrorHelper và đổi tên thành SqlValidatorHelper)
      /// Modified date: 20/10/2026 (Đổi tên theo Rule 19.73 tránh lặp tiền tố)

      // ✅ CHUẨN:
      /// Modified date: 28/10/2026
      ```
    - Cách đúng: mô tả lý do hoặc hành vi bằng lời; cần truy nguồn thì dùng mã hạng mục bền vững trong Master Plan (`R*`, `DQ-*`, `SV-*`, `BE-*`) hoặc ngày quyết định kèm link transcript; test mô tả hành vi mong đợi, không dẫn "kỳ vọng trong prompt".
    - Phạm vi: C#, Vue/TS (kể cả test Playwright), SQL và tài liệu kỹ thuật trong repo. Ngoại lệ duy nhất: thư mục `Prompt/` (nơi lưu chính các prompt).
    - Kiểm trước khi bàn giao: tìm `(?i)prompt\s*#?\d*|theo prompt` và các ghi chú `(Đổi tên thành` trong các file đã sửa; trong comment và mô tả phải không còn kết quả.
    - Liên quan Rule 19.74 (chỉ báo cáo thay đổi thực tế).
17. **Khai Báo Field & Kiểu Lồng Luôn Đặt Ở Đầu Lớp & Cấm Mở Hàm `Reset*` Chỉ Phục Vụ Test (Rule 19.47, Rule 19.83)**:
    - **Vị trí khai báo Field**: Mọi khai báo hằng và biến thành viên (`const`, `static readonly`, `readonly`, field) **BẮT BUỘC đặt ở ĐẦU thân lớp**, phía trước tất cả các phương thức (hoặc tại tệp khai báo field chung của `partial class`), kèm XML doc chuẩn dự án. ⛔ **TUYỆT ĐỐI CẤM** đặt khai báo field chen giữa các phương thức hoặc ở cuối lớp.
    - **Vị trí kiểu lồng (bổ sung 18/10/2026)**: `record`, `record struct`, `class`, `struct`, `enum` khai **bên trong** lớp (kể cả `private sealed record` chỉ dùng nội bộ, lớp `Visitor`, lớp giả trong test) cũng **BẮT BUỘC đặt ở đầu thân lớp**, ngay sau các field và trước hàm khởi tạo. Thứ tự: hằng/field tĩnh → field DI/biến thành viên → kiểu lồng → hàm khởi tạo → phương thức. `partial class`: đặt ở đầu thân lớp trong tệp partial dùng kiểu đó (dùng ở nhiều tệp thì đặt ở tệp chính). Áp dụng cả mã test.
      ```csharp
      // ❌ CẤM — kiểu lồng nằm sau các hàm:
      private async Task<...> GetTrackedTableVersions(...) { ... }
      private sealed record TrackedTableVersion { ... }

      // ✅ CHUẨN:
      private readonly ILogger<DataChangeTrackingService> _logger;
      private sealed record TrackedTableVersion { ... }
      public DataChangeTrackingService(...) { ... }
      ```
    - **Chỉ áp cho khai báo mới (Rule 19.89 ưu tiên)**: field/kiểu lồng do lần sửa này thêm vào thì đặt đúng chỗ ngay khi viết. Khai báo đã có ở `HEAD` mà lần sửa không đổi nội dung thì ⛔ **không dời**, kể cả khi đang nằm sai chỗ — chỉ báo lại trong báo cáo. Dời khai báo mới (đã lỡ đặt sai) thì giữ nguyên nội dung khối.
    - **Kiểm trước khi bàn giao**: chạy `mcp__gortex__search_ast` với `language: "csharp"`, `path_prefix` là thư mục đã sửa (dạng `ThienAn/TA-ITS015-WEBAPI-V1.0/src/...` hoặc `ThienAn/tests/BE/...`, test thì thêm `exclude_tests: false`) và mẫu dưới; không được còn kết quả là **khai báo mới** (kết quả là khai báo đã có ở `HEAD` thì bỏ qua):
      ```
      (declaration_list [(method_declaration) (constructor_declaration)] [(field_declaration) (record_declaration) (class_declaration) (struct_declaration) (enum_declaration) (interface_declaration)] @match)
      ```
    - **Cấm hàm `Reset*` chỉ phục vụ test (No Test-Only Reset Seam)**: ⛔ **TUYỆT ĐỐI CẤM** thêm các phương thức `public`/`internal` kiểu `Reset*()`, `Clear*()` vào production code chỉ để gọi từ bài test xUnit, đặc biệt khi khóa bộ nhớ trong đã tự cô lập theo Unique ID của dữ liệu test (ví dụ `{partnerId}:{configHash}`).
18. **Ưu Tiên Dependency Injection Qua Hàm Khởi Tạo (Rule 19.86, bổ sung 18/10/2026)**:
    - **Lấy phụ thuộc qua hàm khởi tạo**: lớp cần service, cấu hình đọc từ DB/`SysOpConfig`, `ISqlSugarClient`, logger… thì **BẮT BUỘC nhận qua hàm khởi tạo** (DI). ⛔ **CẤM** tự `new` service có phụ thuộc trong mã nghiệp vụ, ⛔ **CẤM** gọi `GetService`/`GetRequiredService` rải trong mã nghiệp vụ (service locator) — chỉ dùng `IServiceScopeFactory` khi lớp Singleton (BackgroundService, bộ đệm) cần phụ thuộc Scoped.
    - **Phụ thuộc là bắt buộc**: ⛔ **CẤM** khai phụ thuộc tùy chọn `IXxx? x = null` rồi viết nhánh `x != null ? ... : mặc định` ở nơi dùng. Giá trị mặc định/dự phòng đặt **một chỗ duy nhất** bên trong chính lớp cung cấp (ví dụ `DataWorkerConfigService`, `ShareDataConfigService` tự trả mặc định khi DB trống/sai).
    - **Mỗi lớp tự tiêm thứ nó dùng**: ⛔ **CẤM** tiêm ở lớp gọi rồi truyền giá trị cấu hình/phụ thuộc xuống làm tham số hàm chỉ để lớp dưới khỏi tiêm (ví dụ Worker đọc tham số rồi truyền `opt` xuống service). Trong một lượt xử lý, hàm điểm vào đọc cấu hình **một lần** rồi truyền cho hàm private/con của chính lớp đó.
    - **Tiêm bằng lớp cụ thể** (Rule 19.94): service, process, lớp đọc cấu hình tiêm thẳng bằng tên lớp. Chỉ tiêm bằng interface khi kiểu đó được phép có interface theo Rule 19.94 (thư viện bắt buộc hoặc plugin nạp lúc chạy); nhiều cách làm sẵn trong mã thì lớp điều phối nhận thẳng các lớp cụ thể (ví dụ `DeliveryDispatcher` nhận 4 lớp gửi). **Đăng ký DI đúng một lần** ở một chỗ (marker Furion trên chính lớp; host chưa bật Furion thì `Extensions` của phân hệ); ⛔ cấm đăng ký trùng (tạo hai bản Singleton, hai bộ đệm). Vòng đời: bộ đệm/cấu hình dùng chung → `Singleton`; truy cập CSDL theo yêu cầu → `Scoped`/`IScoped`.
    - **Ngoại lệ không cần DI**: lớp tĩnh thuần tính toán không trạng thái, không I/O (ví dụ `SqlValidatorHelper`, hằng số, hàm định dạng), DTO/record/entity. Test lấy lớp thật từ `host.Services` (Rule 19.18), không tự `new` lớp có phụ thuộc.
19. **Đặt Tên Bằng Từ Thông Dụng, Dễ Hiểu — Thuật Ngữ Chuyên Biệt Chỉ Dùng Khi Không Còn Cách Khác (Rule 19.87, bổ sung 18/10/2026)**:
    - Tên lớp, hàm, biến, hằng, tên test (C# và TS/Vue) **BẮT BUỘC dùng từ thông dụng nói đúng việc làm**: động từ hành động rõ (`Get`, `Update`, `Advance`, `Mark`, `Take`, `Send`, `Check`) + đối tượng nghiệp vụ (ví dụ `AdvanceTableVersions` thay vì `ClaimTrackedTables`; `markedCount` thay vì `claimed`).
    - ⛔ **CẤM** dùng thuật ngữ chuyên biệt hoặc tiếng lóng kỹ thuật khi đã có từ thông dụng thay được: `claim`, `lease`, `tombstone`, `hydrate`, `fan-out`, `seam`, `upsert`... Chỉ dùng khi đó là **tên chuẩn của C#/.NET/thư viện đang dùng** (ví dụ `Claim`, `ClaimsPrincipal`, `ClaimTypes` của ASP.NET Identity; `Lock`, `Semaphore`; `Merge` khi nói đúng câu lệnh SQL `MERGE`) hoặc thật sự không có từ nào khác diễn đạt được; khi dùng thì XML doc phải giải thích bằng tiếng Việt.
    - Comment, log và thông báo tiếng Việt cũng ⛔ **không chêm thuật ngữ tiếng Anh** khi đã có từ Việt rõ nghĩa (ví dụ viết "giữ khóa", "nhận xử lý", "tiến mốc phiên bản" thay cho "claim lease", "claim LastVersion").
    - Chỉ áp cho **tên mới** đặt trong lần sửa. Tên lớp/hàm/tệp/namespace đã có ở `HEAD` giữ nguyên, kể cả khi lệch rule này — chỉ đổi khi người dùng yêu cầu rõ (Rule 19.89 ưu tiên). ⛔ Tuyệt đối không đổi tên mã của người khác (đã gặp: `ShareDataMappingResolver` của HieuNV bị đổi thành `MappingStatusService`, phải trả lại).
20. **`BackgroundService` Trên .NET 10 — Không Thêm `await Task.Yield()` Đầu `ExecuteAsync` (Rule 19.88, bổ sung 18/10/2026)**:
    - Áp dụng cho project có `<TargetFramework>net10.0</TargetFramework>` trở lên (kiểm trong `.csproj` trước khi viết).
    - Từ .NET 10, `BackgroundService` tự chạy **toàn bộ** `ExecuteAsync` trên luồng nền và trả về ngay, nên mã trước `await` đầu tiên không còn chặn host khởi động ([thông báo thay đổi của Microsoft](https://learn.microsoft.com/en-us/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task)).
    - ⛔ **CẤM** thêm `await Task.Yield();` (hoặc `await Task.Delay(1)`, `Task.Run(...)` bọc cả vòng lặp) ở đầu `ExecuteAsync` chỉ để "nhường luồng"; đây là cách làm của .NET 8/9, trên .NET 10 là mã thừa. Gặp trong mã đang sửa thì bỏ luôn kèm comment giải thích cũ.
    - Cần chạy việc gì **đồng bộ lúc khởi động** (trước khi host coi là đã chạy) thì đặt ở hàm khởi tạo, override `StartAsync` làm trước rồi mới gọi `base.StartAsync`, hoặc cài `IHostedLifecycleService` — ⛔ không dựa vào phần mã trước `await` đầu tiên của `ExecuteAsync`.
    - Project còn chạy .NET 8/9 thì giữ `await Task.Yield()` như cũ.
21. **Giữ Nguyên Dòng Cũ Khi Nội Dung Không Đổi — Phần Thêm Mới Mới Xuống Dòng (Rule 19.89, bổ sung 18/10/2026)**:
    - Áp dụng cho mọi tệp mã (C#, Vue/TS, SQL, JSON i18n) khi sửa tệp đã có ở `HEAD`; là cách làm cụ thể của Rule 19.6 (can thiệp tối thiểu).
    - **Mã của người khác**: xác định tác giả bằng `git log --format='%an' -- <tệp>`, `git blame` từng dòng hoặc dòng `Author:` trong XML doc, so với người dùng hiện tại (`git config user.name`). Mã của người khác thì chỉ sửa đúng chỗ tính năng đang làm bắt buộc phải đụng; ⛔ **CẤM** đổi tên lớp/hàm/tệp/namespace, tách/gộp lớp, dời tệp, dọn mã hay định dạng lại — kể cả khi đang lệch rule đặt tên/thứ tự (chỉ báo lại). Việc chuẩn hóa tên, cấu trúc chỉ làm trên mã do người dùng hiện tại viết, và chỉ khi được yêu cầu.
    - **Luôn ưu tiên hơn các rule định dạng/thứ tự** (Rule 19.47 thứ tự thành viên, 19.75 ngắt dòng tham số, 19.83 field/kiểu lồng ở đầu lớp, 20.9 ngắt dòng thuộc tính Vue...): các rule đó chỉ áp cho mã mới thêm trong lần sửa. Mã đã có ở `HEAD` mà không đổi nội dung thì giữ nguyên vị trí và định dạng, kể cả khi đang lệch rule — chỉ báo lại, không sửa.
    - **Dòng cũ không đổi nội dung → giữ nguyên.** ⛔ **CẤM**: tách hoặc gộp dòng; đổi thụt lề (tab ↔ dấu cách); thêm/bớt khoảng trắng cuối dòng hay dòng trống; đổi BOM hoặc kiểu xuống dòng (CRLF/LF) của tệp; viết lại chú thích cùng nội dung thành nhiều dòng; thêm đối số có giá trị bằng mặc định.
      ```csharp
      // ❌ CẤM — trước và sau cùng nghĩa:
      -        public SubscriptionValidator(IStringLocalizer localizer, bool isCreate = true)
      +        public SubscriptionValidator(
      +            IStringLocalizer localizer,
      +            bool isCreate = true)

      -            Include(new SubscriptionValidator(localizer));
      +            Include(new SubscriptionValidator(
      +                localizer,
      +                isCreate: true));   // isCreate mặc định đã là true
      ```
    - **Sửa một phần dòng cũ → chỉ thay đúng phần đó, giữ bố cục dòng.**
      ```csharp
      // ✅ CHUẨN — chỉ đổi tên biến, dòng giữ nguyên bố cục:
      -                        newlyBroken.Count, string.Join(", ", newlyBroken), TableRetryInterval.TotalMinutes, ...);
      +                        newlyBroken.Count, string.Join(", ", newlyBroken), tableRetryInterval.TotalMinutes, ...);
      // ❌ CẤM — đổi một tên biến mà tách cả lời gọi ra mỗi đối số một dòng.
      ```
    - **Có thêm mới → phần mới viết ở dòng mới.** Câu lệnh, hàm, thuộc tính, rule validator mới viết thành dòng riêng. Lời gọi/khai báo được **thêm đối số** thì được xuống dòng theo Rule 19.75:
      ```csharp
      // ✅ CHUẨN — có thêm tham số configService:
      -        public PacketQueryHandler(BaseRepository<ShareDataPacket> packetRep)
      +        public PacketQueryHandler(
      +            BaseRepository<ShareDataPacket> packetRep,
      +            ShareDataConfigService configService)
      ```
    - **Quan hệ với Rule 19.75 (C#) và Rule 20.9 (thuộc tính template Vue)**: hai rule ngắt dòng chỉ áp cho mã mới và chỗ có thêm mới; ⛔ không dùng làm lý do định dạng lại dòng cũ.
    - **Chú thích nhiều dòng**: mỗi dòng một `//` (hoặc `///`) thật. ⛔ Không để lọt chuỗi `\n` vào giữa dòng mã/chú thích (đã gặp: hai dòng chú thích bị gộp thành một dòng chứa `\n` khi sửa bằng công cụ).
    - **Kiểm trước khi bàn giao**: xem `git diff` từng tệp đã sửa; khối thay đổi nào bỏ hết khoảng trắng/xuống dòng mà hai bên giống nhau thì trả về như cũ. Tìm `\n` lạc bằng `search_text` regex `\\n\s{4,}//`.
22. **Chỗ Đặt Enum Của Phân Hệ — Không Dời Enum Cũ Khỏi `BaseEnums`; Phân Hệ Mới Dùng Lớp Bọc Ở Core (Rule 19.90, bổ sung 18/10/2026)**:
    - **Phân hệ đã có nhóm enum trong `Shared.DTO/Enums/BaseEnums.cs` từ trước** (ShareData, TMS, VMS, VDS, VideoWall, CCTV, WP...):
      - ⛔ **KHÔNG dời** enum cũ ra khỏi `BaseEnums`. Dời làm đổi mọi dòng `BaseEnums.X` (đã gặp: chuyển 21 enum ShareData làm đổi khoảng 318 dòng chỉ để bỏ tiền tố) — Rule 19.89 ưu tiên.
      - Enum **mới** của phân hệ đó, dùng chung WebAPI và Worker: thêm **cạnh nhóm enum của phân hệ trong `BaseEnums`** (cuối nhóm), để phân hệ chỉ có một chỗ chứa enum. Chỉ chuyển cả nhóm sang Core khi có yêu cầu riêng.
      - Enum chỉ service/worker của phân hệ dùng: đặt ở lớp bọc sẵn có của service (ShareData: `ShareDataEnum` ở `ShareDataWorker.Core`).
    - **Phân hệ chưa có enum trong `BaseEnums`**: ⛔ **CẤM** thêm vào `BaseEnums` (chỉ giữ enum dùng chung nhiều phân hệ như `StatusEnum`). Đặt ở `Modules.<X>.Core/Enums/` của phân hệ; WebAPI module và service của phân hệ cùng dùng qua tham chiếu Core, ⛔ không khai thêm bản thứ hai ở `*.Worker.Core`.
    - **Cách khai báo ở Core — theo nếp `NotiEnums` (Notification, SonTH) và `TollEnums` (TOLL)**: một tệp `Enums/<PhânHệ>Enums.cs`, một lớp bọc `public class <PhânHệ>Enums` chứa các enum con. Phân hệ đã có lớp bọc thì thêm vào lớp đó, không tạo lớp mới.
      ```csharp
      namespace Modules.Notification.Core.Enums
      {
          /// <summary>
          /// Author: SonTH
          /// Description: Các enum riêng của phân hệ Notification.
          /// Created date: 17/09/2026
          /// </summary>
          public class NotiEnums
          {
              /// <summary>Phạm vi người nhận của một thông báo.</summary>
              public enum TargetTypeEnum
              {
                  [Description("Người dùng")]
                  User = 1,
                  [Description("Tổ chức")]
                  Org = 2,
              }
          }
      }
      // Gọi: NotiEnums.TargetTypeEnum.User
      ```
    - ⛔ **CẤM** tách mỗi enum một tệp đứng riêng trong namespace (tên chung như `Direction`, `IsRequired`, `AlertSource` đứng trần dễ trùng kiểu khác khi một tệp dùng nhiều namespace). ⛔ Cấm tạo lớp bọc thứ hai cho cùng phân hệ hoặc khai lại enum đã có — trước khi thêm, `search_symbols` tên enum trong toàn solution.
    - Lớp bọc giữ tiền tố phân hệ (`NotiEnums`, `TollEnums`, `ShareDataEnum`) — ngoại lệ của Rule 19.73. Enum con bên trong không lặp tiền tố.
    - Mỗi enum có XML doc; mỗi giá trị gán số tường minh, kèm `[Description("...")]` để Swagger/FE hiển thị.
    - **Ảnh hưởng FE**: Swagger đặt tên model theo **tên kiểu** enum (đã kiểm: `BaseEnums.SessionState` sinh ra `api-services/.../models/session-state.ts`). Dời enum vào/ra lớp bọc không đổi tên model; **đổi tên** enum thì phải sinh lại client FE và sửa chỗ dùng.
    - **Thư viện dùng chung** (`Modules.Integration.<X>.Base`, ví dụ Delivery, Barrier): enum đặt ở `.Base/Enums/` của thư viện, theo cách khai báo đã có trong thư viện đó; ⛔ không đặt trong `Shared.DTO`.
    - **Kiểm trước khi bàn giao**: `git diff HEAD -- src/Shared/Shared.DTO/Enums/BaseEnums.cs` không có dòng `- public enum` (không dời enum cũ) và chỉ có `+ public enum` của phân hệ đã có nhóm trong `BaseEnums`; `find_files` thư mục `Modules.<X>.Core/Enums/` chỉ có tệp lớp bọc của phân hệ.
23. **Toàn Vẹn Tệp Phụ Thuộc Khi Commit — Bắt Buộc Kèm `.sln`, `.csproj` Khi Thêm/Tách Project Mới (Rule 19.93, bổ sung 19/10/2026)**:
    - **Nguyên tắc Atomic Commit & Toàn vẹn môi trường phát triển**: Khi thêm mới, tách hoặc tái cấu trúc một thư viện/project (ví dụ tách tầng gửi gói tin thành thư viện dùng chung `Modules.Integration.Delivery`):
      - BẮT BUỘC đưa đồng thời vào cùng một commit:
        (1) Toàn bộ mã nguồn C# mới (`.cs`);
        (2) Tệp cấu hình project (`.csproj`);
        (3) **Tệp Solution (`src/TAC_WebAPI.sln`)** đã đăng ký project vào đúng Solution Folder;
        (4) Cấu hình DI (Dependency Injection), Extension methods và cấu hình liên quan (nếu có).
      - ⛔ **TUYỆT ĐỐI CẤM** chỉ commit mã nguồn hoặc `.csproj` mà bỏ sót `.sln`. Việc thiếu `.sln` trong commit khiến thành viên khác (hoặc chính lập trình viên khi mở VS Code / Visual Studio trên môi trường khác) không load được project mới vào Solution Explorer, Omnisharp/C# Dev Kit không nhận diện được reference, gây báo đỏ và gãy quy trình làm việc.
      - ⛔ **CẤM** tách thành các commit chắp vá (commit mã nguồn trước, sau đó phát hiện thiếu mới commit `.sln` ở commit lẻ tiếp theo).
      - **Kiểm tra trước khi commit**: Chạy `git status` (hoặc kiểm tra Working Tree) để rà soát: mọi tệp mới trong thư mục project mới đã được `git add`, và tệp `.sln` ở trạng thái staged sẵn sàng commit cùng nhau.
24. **Ưu Tiên Thư Viện Có Sẵn Ở Tầng `src/Modules` (Furion Xuyên Suốt) — Hạn Chế Interface, Phân Cấp Base/`BaseMsg`/Helper Thống Nhất (Rule 19.94, bổ sung 19/10/2026, cập nhật 19/10/2026 theo buổi trao đổi kiến trúc "Street No. 8")**:
    - **Nguồn**: `DocBusinessThienAn/Street No. 8_Transcript.md` (lead kiến trúc, 10/2026) và bộ rule cũ của nhóm viết cho Kiro `TA-ITS015-WEBAPI-V1.0/.kiro/steering/coding-rules.md` (đã tích hợp vào tệp này; lead dặn "mới chưa xong, xài tạm cái cũ đi"). Hai nguồn lệch nhau thì theo transcript.
    - **Ưu tiên tính năng có sẵn của thư viện** ("xài thư viện cho nó dễ"): áp cho cả tầng `src/Modules` — `Modules.<X>.Core`, `Modules.<X>`, `Modules.Integration.*` — và Service dùng mã của tầng này. Thư viện dùng chung qua `Shared.Reference` (Furion, SqlSugar, FluentValidation, Mapster, Wolverine, Refit…); Furion là nền xuyên suốt ("Furion nó đang xuyên suốt dự án", "API chỗ nào cũng được"). Thư viện đã có tính năng thì dùng, ⛔ không tự dựng lại: marker DI thay cho interface + đăng ký tay; `BaseMsg` + `Oops.Oh` thay cho lớp exception riêng; API SqlSugar thay cho SQL tay (Rule 19.33); `BaseHelper` thay cho tiện ích trùng. Chỉ tự viết khi thư viện **không hỗ trợ ở nơi mã chạy**, tính trên mọi host dùng mã đó. Hai trường hợp đã gặp: host chưa bật Furion, và Service chưa có bộ dịch `SysTerminology` (xem các mục dưới).
    - **Mặc định không tạo interface**: lớp nghiệp vụ (service, process, lớp đọc cấu hình, dispatcher/manager…) được tiêm và đăng ký bằng **chính tên lớp cụ thể**. ⛔ **CẤM** interface 1:1 — interface chỉ có một lớp cài đặt (`ITmsEquipmentService` ↔ `TmsEquipmentService`, `IDataOutboundService` ↔ `DataOutboundService`). Lý do: thêm/sửa một hàm phải sửa hai nơi, F12 nhảy vào interface rỗng thay vì mã chạy thật, phải khai thêm ánh xạ interface ↔ lớp khi đăng ký, khó lần lỗi và tốn công refactor.
    - **Chỉ được tạo interface khi có một trong hai lý do sau** (ghi lý do trong `<summary>` của interface; cập nhật 19/10/2026 — người dùng chốt hướng hạn chế interface chặt hơn):
      1. **Thư viện bắt buộc kiểu interface**: Refit client (`IBaseApi`, `IEquipmentApi`)…
      2. **Cách cài đặt nạp lúc chạy từ assembly/DLL bên ngoài (plugin)**, mã gọi không biết trước lớp cụ thể: driver theo hãng trong `Modules.Integration.<PhânHệ>.Base` (`IVmsIntegration` nạp DLL Chainzone/Dysten…).
      - **Nhiều cách làm nằm sẵn trong mã nguồn** (kênh gửi, định dạng, cách mã hóa…) ⛔ **không** còn là lý do tạo interface: mỗi cách là một lớp cụ thể, một lớp điều phối nhận thẳng các lớp đó và rẽ nhánh bằng `switch` theo loại (ví dụ `DeliveryDispatcher` nhận `HttpDeliverySender`, `FileDeliverySender`, `FtpDeliverySender`, `KafkaDeliverySender`). Thêm cách mới = thêm lớp + một nhánh ở đúng lớp điều phối. Lớp "không làm gì" (null object như `PlainEncrypt`) không tính là một cách cài đặt — xử lý bằng nhánh trả nguyên dữ liệu. ⛔ "Sau này có thể có thêm cách cài đặt" **không** phải lý do.
      - **Không tính là tự tạo interface**: cài đặt interface của framework/thư viện (`BackgroundService`/`IHostedService`, `IValidator<T>`, `IDisposable`, `ISqlSugarEntitySeedData<T>`…) và marker Furion (`IScoped`, `ITransient`, `ISingleton`, `IDynamicApiController`).
      - ⛔ **Lớp giả trong test không được tính là lớp cài đặt thứ hai** (Rule 19.18 cấm tự tạo lớp giả cho service nội bộ). Test lấy lớp thật bằng tên lớp: `GetRequiredService<DataOutboundService>()`.
    - **Đăng ký DI bằng marker Furion** (đã chạy thử Furion 4.9.8.41 ngày 19/10/2026):
      ```csharp
      // Kế thừa trực tiếp IScoped, ITransient hoặc ISingleton của Furion (không tạo interface 1:1)
      namespace Modules.TMS.Infrastructure.Services.Equipment
      {
          public class TmsEquipmentService : IScoped
          {
              private readonly BaseRepository<TmsEquipment> _repo;
              public TmsEquipmentService(BaseRepository<TmsEquipment> repo)
              {
                  _repo = repo;
              }

              public async Task ProcessBusinessLogic(long id) { ... }
          }
      }
      ```
      - Lớp kế thừa `IScoped` / `ITransient` / `ISingleton` (chọn vòng đời theo Rule 19.86); ⛔ không viết thêm `services.AddXxx<...>()` cho lớp đã có marker. Vẫn đăng ký tay những gì Furion không quét: `AddHostedService<>`, `AddHttpClient()`, `ISqlSugarClient`, lớp của thư viện ngoài.
      - Marker đăng ký chính lớp cụ thể và interface nghiệp vụ **đầu tiên** của lớp (mặc định `SelfWithFirstInterface`, tự bỏ qua `IDisposable`). Trường hợp hiếm còn interface (plugin), nhiều lớp cài đặt cùng một interface đều được đăng ký đủ trong `IEnumerable<>`, không cần gắn `[Injection]`.
      - **Marker chỉ có tác dụng dưới WebAPI** — host duy nhất bật Furion (`webBuilder.Inject()`). Lớp chạy trong Service (kể cả thư viện Integration chỉ Service dùng) ⛔ không gắn marker: đăng ký tay bằng **chính lớp cụ thể**, đúng một lần trong `Add<X>Services`/`Add<ThưViện>` (`services.AddSingleton<XxxService>()`), giữ `AddHostedService<>` viết tay (§1.3). Mã chạy trong Service ⛔ không dùng thứ chỉ có ở web/Furion: `HttpContext`, `App.*`, `Oops`, người dùng đăng nhập (`UserManager`).
      - Vì sao không bật Furion ở Service (đã chạy thử Furion 4.9.8.41 ngày 19/10/2026): `Inject()` tự chạy **mọi** `BackgroundService` public trong assembly được quét (với `ShareDataWorker` là thêm `HeartbeatPublisher`, `DbStartupGateService`), tự nạp mọi `*.json` ở thư mục chạy, và trạng thái tĩnh `App` xung đột với host WebAPI trong tiến trình test.
      - **Test**: host WebAPI test đã `Inject()` và quét mọi project mà `test.csproj` tham chiếu; lớp của Service (không marker) được đăng ký qua hàm `Add<X>Services` gọi trong `Host.cs` (ShareData: `AddShareDataWorkerCoreServices`). ⛔ Không gọi API Furion (`services.AddDependencyInjection()`, `App.*`) khi tiến trình test chưa có host nào `Inject()` (`TypeInitializationException` hỏng cả tiến trình); ⛔ không dựng host thứ hai gọi `Inject()`.
    - **Phạm vi áp dụng (Rule 19.89 ưu tiên)**: bắt buộc cho mã viết mới. Mã cũ của mình chuẩn hóa bằng task refactor riêng, chia phase, không làm kèm task khác ("cứ viết cho nó lấy trước đi rồi mốt dọn lại sau", "nó refactor mệt"); interface 1:1 đã có trong **mã của mình** thì gỡ khi đang sửa phần đó hoặc khi làm task refactor. **Mã của người khác** (tác giả theo `git log`/`Author:`) chỉ sửa khi người dùng yêu cầu rõ.
    - **Cách gỡ một interface 1:1** (làm trọn trong một lần): xóa tệp interface; bỏ `: IXxx` khỏi lớp; đổi kiểu tham số hàm khởi tạo, đăng ký DI và `GetRequiredService`/`GetService` sang lớp cụ thể; chuyển `<summary>` của interface sang hàm của lớp nếu lớp chưa có; xóa `using` namespace không còn kiểu nào và thư mục `Interfaces/` rỗng; sửa chú thích còn nhắc tên interface. **Giữ nguyên tên tham số/biến đã có** (Rule 19.87), kể cả khi tên trùng tên kiểu — `DataOutboundService DataOutboundService` vẫn biên dịch được.
    - **Cách kiểm nhanh**: Gortex `find_implementations(<id interface>)` hoặc `search_text` mẫu `class \w+[^{]*\bIXxx\b` trong `src/` (không tính `tests/`). Đúng một lớp cài đặt ⇒ interface 1:1 ⇒ gỡ.
    - **Phân cấp Base thống nhất** ("chia cho đúng thôi chứ đừng có mỗi cái tạo một cái riêng"):
      ```text
      Shared (Toàn hệ thống: CommonBaseController, SqlSugarRepository<T>, BaseLocaleManager)
        └── Modules.{Name} (Phân hệ: BaseController, BaseRepository<T>, BaseMsg)
              └── Feature (Chức năng cụ thể: chỉ thêm phần riêng, cấm tự tạo Base ad-hoc)
      ```
      Base toàn hệ thống (`CommonBaseController`, `SqlSugarRepository<T>`, `BaseLocaleManager` ở `Shared.DTO/Constants/Localization`) $\rightarrow$ Base cấp module (`BaseController`, `BaseRepository<T>`, `BaseMsg : BaseLocaleManager`) $\rightarrow$ chức năng chỉ thêm phần riêng của mình. ⛔ **CẤM** tự tạo Base hay lớp exception riêng ở từng chức năng (Feature) hoặc thư viện.
    - **Base Helper trung gian**: Mọi tiện ích hệ thống dùng chung gom vào `Shared.Utility.Helpers.BaseHelper` (`FileHp`, `NetworkHp`, `ImageHp`, `ExcelHp`, `RetryHp`, `EChartHp`) làm tầng trung gian để dễ tìm kiếm và tái sử dụng.
      - **Thời gian (bổ sung 20/10/2026, theo Kiro BE `coding-rules.md` mục "Shared Utility Functions")**: mã mới ở project có tham chiếu `Shared.Utility` lấy giờ hiện tại bằng `BaseUtil.TimeServer.GetTimeServer()`, ⛔ không gọi thẳng `DateTime.Now`; giờ FE gửi lên (UTC+0) đổi về giờ server bằng `BaseUtil.TimeServer.GetTimeServer(utcTime)`. Tiền lệ: CCTV, CfgSystem, FMS, Mail, `Integration.NxVms`. Hiện `GetTimeServer()` trả `DateTime.Now`, nên đổi không làm thay đổi hành vi. Thư viện thuần BCL (`*.Base`, `Modules.Integration.Delivery`) không tham chiếu `Shared.Utility` thì giữ `DateTime.Now`. Mã cũ giữ nguyên (Rule 19.89). `CreateTime`/`UpdateTime` của entity do framework tự gán.
      - **Tải tệp (`IUploadService`, `Shared.Core`, tiêm qua DI)** — tích hợp từ Kiro BE: ảnh/biểu tượng base64 `await _uploadService.UploadAsync(new BaseUploadInput { Data = base64, FolderPath = BaseUploadConfig.Folder.EquipmentType })`; tệp từ form `UploadAsync(command.File, "Upload/<ChứcNăng>/Documents", fileName)`; nội dung chữ/JSON `UploadTextAsync(new BaseUploadInput { Data, FolderPath, FileName })`. Thư mục định sẵn ở `BaseUploadConfig` (`Folder.EquipmentType`, `Folder.EventType`, `Vms.Template`, `Vms.SendToDevice`); ⛔ không tự ghép đường dẫn lưu trữ khi đã có hằng.
      - **Đường dẫn tệp (`BaseUtil.FileServer`)**: `GetPath()` (URL gốc WebRoot), `GetFileServer("Upload/img.png")` (URL đầy đủ), `UrlFileExistAsync(url)`.
      - **`BaseHelper` chi tiết**: `FileHp.FileMimeHelper.GetByExtension/FormatUrlForStorage/FormatUrlForWeb`, `FileHp.OpenReadWithRetry/DeleteWithRetry/MoveReplaceWithRetry`; `NetworkHp.CheckConnect(ip, timeout)`; `ImageHp.ToBase64FromUrl`; `ExcelHp.ExportList/ExportByTemplate`; `RetryHp.RetryOnException(lần, khoảng, hàm)`; `EChartHp.GetListDay/GetListHour`; hàm mở rộng `dictionary.GetDictionaryValue<T>(key)`, `enumValue.GetEnumDescription()`. ⛔ Không tự viết lại các tiện ích này.
    - **Tiện ích trong phân hệ** (theo cách các phân hệ khác đang viết, khảo sát 19/10/2026):
      - Thư mục tên `Helpers/` (13 thư mục; `Helper/` số ít chỉ còn CCTV, VideoWall; `Utils/` chỉ có ở ShareData). ⛔ Không tạo `Utils/`, `Helper/`, `Common/`; ⛔ không chia thư mục con trong `Helpers/` (chưa phân hệ nào làm).
      - Chỗ đặt theo loại project: Module → `Modules.<X>/Infrastructure/Helpers/` (`VmsStatGroupHelper`, `MediaHelper`); project Report → `Core/Helpers/` trong project Report; thư viện Integration → `.Base/Helpers/` (`IntgBarrierNetHelper`, `IntgTrafficLightNetHelper`) hoặc `Infrastructure/Helpers/` của project cài đặt (`IntgVmsStatHelper`); Service → `Helpers/` trong thư viện lõi (`ITS.VMS.Core/Integration/Helpers/VmsFileHelper`). Tiện ích mà cả WebAPI lẫn Service của cùng phân hệ dùng, khi Service chỉ tham chiếu `Modules.<X>.Core`, đặt ở `Modules.<X>.Core/Helpers/` (`Module.ShareData.Core/Helpers/SqlValidatorHelper`, `SqlParameterHelper`, `ConfigValueParser`) — ngoại lệ duy nhất cho phép logic trong Core (§1.2).
      - Lớp: `public static class <TiềnTố><ChủĐề>Helper` (`IntgBarrierAddressHelper`, `VmsStatGroupHelper`, `VwGeometryHelper`); tiện ích chung của cả phân hệ đặt `<PhânHệ>Helper` (`MediaHelper`). ⛔ Không dùng hậu tố `Util` — `Util` chỉ dành cho `Shared.Utility.Utilities` (gọi máy chủ qua `BaseUtil`). Tên đã có không tiền tố (`PacketHelper`, `MoneyHelper`) giữ nguyên (Rule 19.89).
      - **Quy chuẩn lớp trong `Helpers/` BẮT BUỘC mang hậu tố `Helper`**: Mọi lớp tiện ích khi đã nằm trong thư mục `Helpers/` **BẮT BUỘC mang hậu tố `Helper`** (ví dụ `<ChủĐề>Helper`: `SqlParameterHelper`, `SqlFieldHelper`, `SqlTextHelper`, `SqlValidatorHelper`) để đồng nhất với các lớp khác trong cùng thư mục `Helpers/`. Các lớp giữ tên vai trò chuyên biệt (`...Validator`, `...Parser`, `...Builder`, `...Reader`) chỉ đặt tên trần khi nằm trực tiếp cạnh chức năng dùng nó trong thư mục Service/Process (`Modules.VDS/Infrastructure/Services/TrafficData/Sql/VolumeQueryBuilder`, `DataTransporter.Natsio/Infrastructure/Configuration/NatsConfigParser`, `Services.Vds/Features/Counting/CountChartBuilder`); một khi đã gom vào thư mục `Helpers/` thì bắt buộc mang hậu tố `Helper`.
      - Hàm mở rộng (`this T`) viết mới cho một kiểu đặt trong lớp `<Kiểu>Extensions` (`ConnectionTypeExtensions`, `BarrierCommandTypeExtensions`, `ResultExtensions`); hàm mở rộng đã nằm trong lớp Helper thì giữ nguyên.
      - **Chỗ đặt (bổ sung 20/10/2026, khảo sát `src/Modules`)**: thư mục `Extensions/` của project chỉ chứa lớp đăng ký DI/module — `ModuleExtensions` (`Add<X>Module`), `ServiceCollectionExtensions`, `MvcBuilderExtensions` (tiền lệ: `DataTransporter.Base/Extensions/`, mọi module). Lớp `<Kiểu>Extensions` của enum/model đặt **cạnh kiểu đó**, cùng thư mục và namespace (tiền lệ: `Integration.Barrier.Base/Enums/BarrierEnums.cs` chứa `BarrierCommandTypeExtensions`). Lớp `<Kiểu>Extensions` chỉ chứa hàm mở rộng; ⛔ không đặt hằng/giá trị mặc định trong đó (hằng vào `Constants/`). Thư viện dùng chung `Shared.*` giữ bố cục riêng (`Shared.DTO/Extensions/ResultExtensions`). Mã cũ của người khác lệch (`Report.Core/Extensions/RptInfoConfigExtensions`) giữ nguyên (Rule 19.89).
      - Tiện ích chung cho nhiều phân hệ đưa vào `Shared.Utility/Helpers` + `BaseHelper`, không để trong phân hệ.
    - **Thông điệp lỗi & `BaseMsg`** — đúng là `BaseMsg : BaseLocaleManager` của module; sai là lớp exception tự tạo như `Delivery.Base/Exceptions/DecryptFailedException` (buổi trao đổi chỉ ra):
      1. **Kiểm `BaseLocaleManager` trước** (`Shared.DTO/Constants/Localization/BaseLocaleManager.cs`): đã có sẵn các nhóm dùng chung `BaseException` (`Exist`, `NotFound`, `DataNotExist`, `DataIsInUse`, `IllegalOperation`…), `BaseValidation` (`Required`, `MaxLength`, `GreaterThanOrEqual`…), `BaseEntity` (`Code`, `Name`, `Status`…), `BaseMessage`. Khóa đã có thì dùng qua `BaseMsg.BaseException.*`, `BaseMsg.BaseValidation.*`, `BaseMsg.BaseEntity.*`; ⛔ không khai lại.
      2. Chưa có thì thêm vào `BaseMsg` của module hoặc thư viện (`Exceptions/BaseMsg.cs` ở Core; lớp con theo chức năng; tiền tố `lz.{message|validation|exception|entity}.<camelCase>.`), kèm seed `SysTerminology` vi-VN + en-US (Rule 19.69).
      3. Lỗi nghiệp vụ ở WebAPI: `throw Oops.Oh(BaseMsg..., args)`; ⛔ chuỗi viết cứng, kể cả chuỗi dự phòng `?? "..."`. Thông điệp trả về FE trong DTO (`ErrorMessage`…) cũng dịch theo khóa: `Furion.Localization.L.Text[khóa, tham số].Value`. Helper dùng chung WebAPI + Service trả **khóa + tham số**; câu dịch sẵn trong helper (như `SqlValidatorHelper.FormatVietnamese`) chỉ để Service ghi nhật ký, ⛔ WebAPI không ném hay trả câu đó.
      4. Service được có **một** ngoại lệ cấp phân hệ mang mã lỗi (ShareData: `ShareDataException` mang mã ESH ở `ShareDataWorker.Core/Exceptions`); ⛔ không tạo thêm lớp exception theo chức năng.
      5. **Thông điệp do Service ghi (thư viện chưa hỗ trợ)**: Service chưa có bộ dịch `SysTerminology`, còn giao diện hiển thị nguyên văn cột thông điệp của nhật ký (ShareData: `ShareDataAlertLog`, `ShareDataActivityLog`) → ⛔ không ghi khóa `lz.*` vào nhật ký; dùng mã lỗi cấp phân hệ hoặc ngoại lệ có sẵn của .NET (`InvalidOperationException`, `CryptographicException`…). Thư viện chỉ chạy trong Service (ví dụ phần cài đặt `Modules.Integration.Delivery`) theo cách này; thư viện được WebAPI gọi thì trả mã/enum để WebAPI quy ra `BaseMsg` (ví dụ `ConnectConfigIssue` → `PartnerValidator`).
    - **Đặt tên & dữ liệu dùng chung**: Entity bắt đầu bằng tiền tố module (`Tms...`, `Vms...`), không thêm hậu tố `Entity` ("search tiền tố là nó sẽ ra"); kiểu khác giữ hậu tố vai trò ("mấy cái này là phải có cái đuôi": `...Service`, `...Helper`, `...Handler`, `...Validator`, `...Input`/`...Output`). Entity/DTO dùng chung cho nhiều module BẮT BUỘC đặt ở `Shared/Core.Entities` hoặc `Shared.DTO`.
    - **Thư mục service đồng nhất** (bổ sung 20/10/2026 — "vô nó bay vô trong này nó sẽ lấy service không", "Ai vô mà thấy service không... để cho nó đồng nhất, dòm lại nó dễ tìm"):
      - Logic xử lý của module đặt ở `Infrastructure/Services/<ChứcNăng>/` (§1.2). **Mở thư mục chức năng chỉ thấy đúng một tệp service** ở cấp đầu. Tên tệp service duy nhất ở cấp đầu đặt trực diện theo tên chức năng (ví dụ thư mục ngoài `ShareDataConfig/`, tệp bên trong chỉ cần `ConfigService.cs` thay vì `ShareDataConfigService.cs`; thư mục `Schedule/` đặt `ScheduleService.cs`), **tuyệt đối không lặp lại tiền tố phân hệ** theo Rule 19.73 (§4.13).
      - Tệp thứ hai trở đi (service khác, lớp vai trò `...Reader`/`...Resolver`/`...Builder`/`...Rules`, hằng, kiểu dữ liệu, tệp `partial`) đặt vào **thư mục con đặt tên theo chủ đề**. Tiền lệ: `Modules.TMS/Infrastructure/Services/Incident/Classification/`, `Incident/Snapshot/`, `Modules.TMS.Report/Infrastructure/Services/TrafficData/Rules/`. Tương tự, `ShapeReaderService.cs` chuyển vào `ShareDataMapping/Shape/ShapeReaderService.cs`, cấp đầu `ShareDataMapping/` chỉ còn duy nhất `MappingResolverService.cs`.
      - Chuyển tệp có sẵn vào thư mục con chỉ là **đổi vị trí**: giữ nguyên tên lớp, tên tệp, namespace và nội dung (Rule đặt tên — tên đã có ở `HEAD` giữ nguyên), nên không phải sửa `using` ở nơi gọi.
      - Phạm vi: `src/Modules` ("service trong cái module"). `src/Services` chưa áp ("cái service kia chưa có thống nhất cái cấu trúc"). Mã cũ theo Rule 19.89; mã của người khác chỉ sửa khi người dùng yêu cầu. Khảo sát 20/10/2026: nhiều thư mục của phân hệ khác còn ≥ 2 tệp ở cấp đầu (TMS `Incident`, `TrafficData`; VDS `TrafficData`; VMS `Imaging`, `Process`; Notification `Config`…) — không tự sửa.
25. **Ưu Tiên Sửa Code Trước — Chỉ Viết Test Khi Có Yêu Cầu (Rule 19.67 - Fix-First & On-Demand Testing cho Backend, bổ sung 21/10/2026)**:
    - **Tập trung sửa dứt điểm code Backend trước (Fix-First)**: Khi người dùng báo lỗi, yêu cầu chỉnh sửa logic, API, Entity, Handler, DTO hoặc Worker: AI **BẮT BUỘC tập trung sửa code xử lý dứt điểm trước**, đảm bảo build thành công (`dotnet build`) và báo cáo ngay kết quả cho người dùng review.
    - ⛔ **TUYỆT ĐỐI CẤM** tự ý dừng tiến độ để ngồi viết thêm các test method xUnit mới trong `tests/BE/` cho mỗi lần sửa nhỏ/lặt vặt làm chậm tiến độ làm việc của dự án.
    - **Chỉ viết test khi người dùng yêu cầu (On-Demand Testing)**: CHỈ viết thêm hoặc bổ sung test xUnit khi người dùng có chỉ thị rõ ràng (ví dụ: *"viết test"*, *"bổ sung test cho API này"*, *"viết kịch bản kiểm thử"*), hoặc khi triển khai các module/flow lớn có quy định rõ trong spec.
    - **Chạy regression test sẵn có**: Được phép chạy kiểm tra các test xUnit đã có sẵn bằng filter (`dotnet test tests/BE/test.csproj --filter ...`) để đảm bảo không làm gãy logic cũ, nhưng tuyệt đối không tự ý viết thêm mã test mới khi chưa được yêu cầu.




