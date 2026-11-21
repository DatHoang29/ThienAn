# Coding Rules & Conventions

## Module Creation

### Project Naming
- Module project: `Modules.{ModuleName}` (e.g., `Modules.TMS`, `Modules.VMS`, `Modules.FMS`)
- Report sub-project: `Modules.{ModuleName}.Report`
- Namespace follows project name: `namespace Modules.TMS.Controllers.Equipment`
- Exception: CCTV uses `Module.CCTV` (singular)

### Module Prefix Convention
Each module defines a **short prefix** used across all naming:

| Module | Prefix | BasePath | GroupName |
|--------|--------|----------|-----------|
| TMS | `Tms` | `api/tms` | `TMS` |
| VMS | `Vms` | `api/vms` | `VMS` |
| FMS | `Fms` | `api/fms` | `FMS` |
| CCTV | `Cctv` | `api/cctv` | `CCTV` |
| CfgSystem | `Cfg` | `api/cfgSystem` | `CfgSystem` |
| Sample | `Samp` | `api/sample` | `Sample` |

### Directory Structure for a New Module

```
Modules/{ModuleName}/Modules.{ModuleName}/
├── Controllers/
│   ├── BaseController.cs
│   └── {Feature}/
│       ├── {Prefix}{Feature}Controller.cs
│       ├── Commands/
│       │   └── {Feature}CommandHandler.cs
│       ├── Queries/
│       │   └── {Feature}QueryHandler.cs
│       ├── Dto/
│       │   ├── {Feature}Input.cs
│       │   └── {Feature}Output.cs
│       └── Validators/
│           └── {Feature}Validator.cs
├── Core/
│   ├── Constants/
│   ├── Entities/
│   │   └── {Prefix}{Entity}.cs
│   └── Exceptions/
│       └── BaseMsg.cs
├── Infrastructure/
│   ├── BaseRepository.cs
│   ├── Services/
│   │   └── {Feature}/
│   │       └── {Prefix}{Feature}Service.cs
│   └── Migrations/
├── Extensions/
│   ├── ModuleExtensions.cs
│   └── ServiceCollectionExtensions.cs
└── GlobalUsings.cs
```

## File Templates

### GlobalUsings.cs (Required for every module)
```csharp
global using Furion;
global using Furion.ConfigurableOptions;
global using Furion.DependencyInjection;
global using Furion.DynamicApiController;
global using Furion.FriendlyException;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.Options;
global using Newtonsoft.Json;
global using SqlSugar;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.Data;

global using Wolverine.Attributes;
[assembly: WolverineModule]
```

### BaseController.cs
```csharp
using Shared.Infrastructure.Controllers;
using Wolverine;

namespace Modules.{ModuleName}.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/{moduleLower}";
        protected internal new const string GroupName = "{ModuleName}";
    }
}
```

### ModuleExtensions.cs
```csharp
namespace Modules.{ModuleName}.Extensions
{
    public static class ModuleExtensions
    {
        public static IServiceCollection Add{ModuleName}Module(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .Add{ModuleName}Core()
                .Add{ModuleName}Infrastructure(configuration)
                .Add{ModuleName}Validation();
            return services;
        }
    }
}
```

### ServiceCollectionExtensions.cs
```csharp
namespace Modules.{ModuleName}.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Add{ModuleName}Core(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection Add{ModuleName}Infrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }

        public static IServiceCollection Add{ModuleName}Validation(this IServiceCollection services)
        {
            services.AddControllers().Add{ModuleName}Validation();
            return services;
        }
    }
}
```

### BaseRepository.cs (Module-level)
```csharp
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.{ModuleName}.Infrastructure
{
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
    }
}
```

## Naming Conventions

### Entity Naming
- Class: `{Prefix}{EntityName}` (e.g., `TmsEquipment`, `VmsImage`, `FmsVehicle`)
- Table attribute: `[SugarTable("{Prefix}{EntityName}")]`
- Base class: `: EntityTenant` (includes ID, Code, TenantId, audit fields)
- Properties use `[SugarColumn(IsNullable = true, Length = EntityConst.LengthXX)]`
- Use constants from `EntityConst` for field lengths

### Controller Naming
- Class: `{Prefix}{Feature}Controller` (e.g., `TmsEquipmentController`, `SampCategoryController`)
- Attribute: `[ApiDescriptionSettings(GroupName, Order = XXX)]`
- Standard methods:
  - `Page({FeatureInput})` → pagination
  - `GetList([FromQuery] {FeatureInput})` → full list
  - `Add{Feature}({AddInput})` → create, marked `[HttpPost]`
  - `Update{Feature}({UpdateInput})` → update, marked `[HttpPost]`
  - `Delete{Feature}({DeleteInput})` → delete, marked `[HttpPost]`
  - `BatchDelete{Feature}(List<{DeleteInput}>)` → bulk delete, marked `[HttpPost]`
- All controller methods dispatch through `MessBus.InvokeAsync<TResult>(input)` or `MessBus.InvokeAsync(input)`

### DTO Naming (in `Dto/` folder)
- **Inputs:**
  - `Page{Feature}Input : BasePageInput` — pagination query
  - `{Feature}Input` — list/filter query
  - `Add{Feature}Input : {Entity}` — create (inherits entity directly)
  - `Update{Feature}Input : Add{Feature}Input` — update (extends create)
  - `Delete{Feature}Input : BaseIdInput` — delete by ID
- **Outputs:**
  - `{Feature}Output : {Entity}` — list item
  - `Page{Feature}Output : {Entity}` — paginated item
  - `{Prefix}{Feature}DataOutput : {Entity}` — extended output with joins

### Shared DTOs (in `Shared.DTO/Dtos/{Module}/`)
- Request: `{Prefix}{Entity}Request`, `{Prefix}{Entity}ListRequest : BasePageReqDto`
- Response: `{Prefix}{Entity}ResponseBase : BaseEntityDto`, `{Prefix}{Entity}Response : {ResponseBase}`
- These are for cross-module communication (Refit APIs)

### Handler Naming
- Command handler: `{Feature}CommandHandler : IWolverineHandler`
- Query handler: `{Feature}QueryHandler` (no interface required)
- Method signature: `public async Task HandleAsync({InputDto} command)` or `public async Task<{OutputDto}> HandleAsync({InputDto} command)`
- Constructor injection: `BaseRepository<T>`, `BaseCacheService`, `IMessageBus`

### Service Naming
- Class: `{Prefix}{Feature}Service : IScoped`
- Location: `Infrastructure/Services/{Feature}/{Prefix}{Feature}Service.cs`
- Use `IScoped` for per-request lifetime via Furion DI

### Validator Naming
- Base validator: `{Feature}Validator : AbstractValidator<{Entity}>`
- Add validator: `Add{Feature}Validator : AbstractValidator<Add{Feature}Input>` with `Include(new {Feature}Validator(localizer))`
- Update validator: `Update{Feature}Validator : AbstractValidator<Update{Feature}Input>` with `Include(new {Feature}Validator(localizer, false))`
- Delete validator: `Delete{Feature}Validator : AbstractValidator<Delete{Feature}Input>` — validates ID not empty
- Use `IStringLocalizer` for validation messages
- Reference `BaseMsg.BaseEntity.*`, `BaseMsg.BaseValidation.*`, `BaseMsg.BaseException.*`

### Localization Keys (BaseMsg pattern)

#### Shared Keys from BaseLocaleManager (DO NOT duplicate)
`BaseLocaleManager` is located in `Shared.DTO/Constants/Localization/BaseLocaleManager.cs` and provides shared localization keys that ALL modules inherit. When creating a module's `BaseMsg`, it extends `BaseLocaleManager` — so all these keys are available without redefinition:

**BaseValidation** (use via `BaseMsg.BaseValidation.*`):
- `Required`, `MaxLength`, `MinLength`, `Email`, `Number`, `Text`, `TextOrNumber`
- `IdRequired`, `MustBeValidValue`, `MustBeAlphanumericSpecial`
- `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`

**BaseException** (use via `BaseMsg.BaseException.*`):
- `Exist`, `NotFound`, `NotExist`, `CannotDelete`
- `DataNotFound`, `DataNotExist`, `DataIsInUse`, `DataExist`
- `CannotSelfParent`, `ParentNotExist`, `IncorrectAcceptValue`
- `NoPermission`, `RequireAdminPermission`, `IllegalOperation`
- `ServerError`, `DataNotMatch`

**BaseEntity** (use via `BaseMsg.BaseEntity.*`):
- `ID`, `Code`, `Name`, `Remark`, `Pid`, `CodeOrName`
- `Info`, `Source`, `Description`, `Content`, `Image`, `Url`, `Type`, `Size`
- `OrderNo`, `Status`, `State`, `Lang`, `Account`, `Password`
- `Data`, `Email`, `Port`, `IpAddress`, `DashboardId`

**BaseMessage** (use via `BaseMsg.BaseMessage.*`):
- `ImportNumberSuccess`, `AccountNotExist`, `MapCanNotDisabled`

#### Module-specific Keys
Only add keys to your module's `BaseMsg` that are NOT already in `BaseLocaleManager`. Each module has a `BaseMsg : BaseLocaleManager` class in `Core/Exceptions/`:

```csharp
internal class BaseMsg : BaseLocaleManager
{
    internal class {EntityName}
    {
        protected class Prefix
        {
            internal const string Message = "lz.message.{camelCaseEntity}.";
            internal const string Validation = "lz.validation.{camelCaseEntity}.";
            internal const string Exception = "lz.exception.{camelCaseEntity}.";
            internal const string Entity = "lz.entity.{camelCaseEntity}.";
        }

        // Only define field names specific to this entity
        internal class Entity { /* module-specific field keys */ }
        internal class Validation { /* module-specific validation keys */ }
        internal class Exception { /* module-specific exception keys */ }
        internal class Message { /* module-specific message keys */ }
    }
}
```

#### Localization Usage Rules
1. **Check BaseLocaleManager first** — If a key already exists (e.g., `Name`, `Code`, `Status`, `Required`, `MaxLength`, `Exist`, `DataNotExist`), use it directly from `BaseMsg.BaseEntity.*`, `BaseMsg.BaseValidation.*`, or `BaseMsg.BaseException.*`
2. **Only add module-specific keys** — Entity field names unique to the module (e.g., `EquipmentType`, `Lane`, `KmNumber`) go in the module's `BaseMsg.{Entity}.Entity` class
3. **Prefix pattern** — All localization keys follow `lz.{category}.{camelCaseEntity}.{fieldName}` format
4. **JSON resource files** — Corresponding translations live in `TAC_WebAPI/Resources/` as JSON files (vi-VN, en-US)

### Cache Key Convention
- Defined in `Shared.DTO/Constants/Application/CacheConst.cs`
- Pattern: `{module}_{entity}` (e.g., `tms_equipment`, `vms_image`, `samp_category`)
- Grouped in nested class: `CacheConst.{Module}.{Entity}`
- Invalidation: `_cache.RemoveByPrefixKey(CacheConst.{Module}.{Entity})`

## Service Extraction (Reusable Logic)

### When to Extract to a Service
When a handler contains logic that could be reused by other handlers or controllers, extract it into a service in `Infrastructure/Services/`. Indicators:
- Same data-access logic needed in multiple handlers (Command + Query, or across features)
- Complex business logic that makes the handler too long
- Utility functions operating on entities (e.g., delete with cascade, recalculate ordering, state transitions)

### Service Location and Pattern
```
Infrastructure/Services/{Feature}/{Prefix}{Feature}Service.cs
```

```csharp
namespace Modules.{ModuleName}.Infrastructure.Services.{Feature}
{
    public class {Prefix}{Feature}Service : IScoped
    {
        private BaseRepository<{Entity}> _{entityRepo};

        public {Prefix}{Feature}Service(BaseRepository<{Entity}> entityRepo)
        {
            _{entityRepo} = entityRepo;
        }

        // Reusable business logic methods
        public async Task DoSomething({Entity} entity) { ... }
    }
}
```

### Handler Calls Service
```csharp
public class {Feature}CommandHandler : IWolverineHandler
{
    private BaseRepository<{Entity}> _{repo};
    private BaseCacheService _cache;
    private {Prefix}{Feature}Service _{service};  // Inject service

    public {Feature}CommandHandler(
        BaseRepository<{Entity}> repo,
        BaseCacheService cache,
        {Prefix}{Feature}Service service)
    {
        _{repo} = repo;
        _cache = cache;
        _{service} = service;
    }

    public async Task HandleAsync(Add{Feature}Input command)
    {
        // Direct CRUD in handler
        await _{repo}.InsertWithDiffLogAsync(entity);

        // Reusable logic delegated to service
        await _{service}.DoSomething(entity);
    }
}
```

### Guidelines
1. **Simple CRUD stays in handlers** — Insert, Update, Delete, basic queries remain directly in `CommandHandler`/`QueryHandler`
2. **Complex or shared logic goes to services** — Cascade deletes, order recalculation, cross-entity validation, external API calls
3. **Services use `IScoped`** — Registered per-request via Furion's DI (no manual registration needed)
4. **Services can also be injected into controllers** — For operations that don't fit CQRS (e.g., `TmsEquipmentService` injected directly in `TmsEquipmentController`)
5. **Naming** — Service class: `{Prefix}{Feature}Service`, field: `_{featureService}` or `_{service}`

## CQRS Flow

### Standard Create Flow
```
Controller.Add{Feature}(AddInput) 
  → MessBus.InvokeAsync(input)
    → {Feature}CommandHandler.HandleAsync(AddInput command)
      → Validate uniqueness (IsAnyAsync)
      → Throw Oops.Oh(...) if duplicate
      → Map with Mapster: command.Adapt<Entity>()
      → Insert: _repo.InsertWithDiffLogAsync(entity)
      → Clear cache: _cache.RemoveByPrefixKey(...)
      → Broadcast event: ItsDataTransporter.BroadcastData(...)
```

### Standard Query Flow
```
Controller.Page(PageInput) 
  → MessBus.InvokeAsync<SqlSugarPagedList<Output>>(input)
    → {Feature}QueryHandler.HandleAsync(PageInput command)
      → _repo.AsQueryable()
        .WhereIF(!string.IsNullOrWhiteSpace(command.Field), u => u.Field.Contains(command.Field))
        .Select(u => new Output() { }, true)
        .OrderBuilder(command, "")
        .ToPagedListAsync(command.Page, command.PageSize)
```

### Standard List Flow (with caching)
```
Controller.GetList(Input) 
  → MessBus.InvokeAsync<List<Output>>(input)
    → {Feature}QueryHandler.HandleAsync(Input command)
      → _cache.GetOrAdd($"{CacheConst.Module.Entity}:{key}", _ => query)
```

## Error Handling

- Use Furion `Oops.Oh()` for business exceptions
- Pattern: `throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code)`
- Common exceptions from `BaseLocaleManager`:
  - `BaseMsg.BaseException.Exist` — duplicate record
  - `BaseMsg.BaseException.DataNotExist` — record not found
  - `BaseMsg.BaseException.DataNotMatch` — data mismatch

## Shared APIs (Cross-Module Communication)

For calling APIs between modules, use Refit interfaces in `Shared.Utility/Apis/{Module}/`:
```csharp
public interface I{Feature}Api : IBaseApi
{
    [Get(BaseEndpoint.{Module}Ep.{Feature}.Base + "/list")]
    Task<ApiResponse<WResult<List<{Response}>>>> List({Request} query);
}
```

Endpoints defined in `Shared.Utility/Endpoints/BaseEndpoint.cs`.

## Registration in Startup

After creating a module, register it in `TAC_WebAPI/Startup.cs`:
```csharp
private void RegisterModules(IServiceCollection services)
{
    services
        // ... existing modules ...
        .Add{ModuleName}Module(_config);
}
```

And add project reference in `TAC_WebAPI.csproj`:
```xml
<ProjectReference Include="..\Modules\{ModuleName}\Modules.{ModuleName}\Modules.{ModuleName}.csproj" />
```

## Code Comments

- Use `/// <summary>` XML docs on classes and public methods
- Include author, description, and date:
  ```csharp
  /// <summary>
  /// Author: {Name}
  /// Description: {Vietnamese or English description}
  /// Created date: dd/MM/yyyy
  /// </summary>
  ```
- Vietnamese comments in business logic are acceptable and common


## Shared Utility Functions (Lưu ý quan trọng)

### Timezone / Thời gian (`BaseUtil.TimeServer`)

FE gửi thời gian dạng UTC+0. Khi nhận DateTime từ FE input, **bắt buộc** phải chuyển đổi múi giờ về local timezone trước khi lưu DB hoặc xử lý logic:

```csharp
using Shared.Utility.Utilities;

// Chuyển DateTime từ UTC+0 (FE gửi lên) về local timezone
DateTime? localTime = BaseUtil.TimeServer.GetTimeServer(command.StartTime);

// Lấy thời gian server hiện tại
DateTime now = BaseUtil.TimeServer.GetTimeServer();
```

**Rules:**
1. **Mọi DateTime từ FE input** đều phải qua `BaseUtil.TimeServer.GetTimeServer(utcTime)` để convert
2. Không dùng `DateTime.Now` trực tiếp — dùng `BaseUtil.TimeServer.GetTimeServer()` để thống nhất
3. Các trường `CreateTime`, `UpdateTime` trong entity đã được xử lý tự động bởi framework

### Upload Service (`IUploadService`)

Service upload file được cung cấp từ `Shared.Core` (DLL), inject qua DI. Có 3 cách upload:

#### Upload Base64 (hình ảnh, icon)
```csharp
using Shared.Core.Utilities;
using Shared.DTO.Upload;

// Inject
private IUploadService _uploadService;

// Upload base64 image
string filePath = await _uploadService.UploadAsync(new BaseUploadInput()
{
    Data = base64String,                           // Dữ liệu base64
    FolderPath = BaseUploadConfig.Folder.EquipmentType,  // Thư mục lưu (xem BaseUploadConfig)
    // Extension = ".png"                          // Optional: force extension
});
```

#### Upload IFormFile (file document, attachment)
```csharp
// Upload file từ form multipart
var fileName = $"{DateTime.Now:yyMMddHHmmss}_{command.File.FileName}";
string filePath = await _uploadService.UploadAsync(command.File, "Upload/Feature/Documents", fileName);
```

#### Upload Text Content (JSON, text files)
```csharp
string filePath = await _uploadService.UploadTextAsync(new BaseUploadInput()
{
    Data = jsonContent,
    FolderPath = "Upload/Vms/Template",
    FileName = "template.json"
});
```

#### Predefined Upload Folders (`BaseUploadConfig`)
```csharp
BaseUploadConfig.Folder.EquipmentType  // "Upload\\EquipmentType"
BaseUploadConfig.Folder.EventType      // "Upload\\EventType"
BaseUploadConfig.Vms.Template          // "Upload\\Vms\\Template"
BaseUploadConfig.Vms.SendToDevice      // "Upload\\Vms\\SendToDevice"
```

### File URL Utilities (`BaseUtil.FileServer`)

```csharp
// Lấy base URL để truy cập file đã upload (WebRoot)
string baseUrl = BaseUtil.FileServer.GetPath();  // "https://host/share"

// Lấy full URL từ relative path
string fullUrl = BaseUtil.FileServer.GetFileServer("Upload/img.png");

// Kiểm tra file tồn tại qua URL
bool exists = await BaseUtil.FileServer.UrlFileExistAsync(url);
```

### File Helpers (`BaseHelper.FileHp`)

```csharp
using Shared.Utility.Helpers;

// Lấy MIME type theo extension
string mime = BaseHelper.FileHp.FileMimeHelper.GetByExtension("pdf"); // "application/pdf"

// Format path cho storage (OS-specific separator)
string storagePath = BaseHelper.FileHp.FileMimeHelper.FormatUrlForStorage(path);

// Format path cho web (forward slash)
string webPath = BaseHelper.FileHp.FileMimeHelper.FormatUrlForWeb(path, baseUrl);

// File I/O with retry (exponential backoff)
using var stream = BaseHelper.FileHp.OpenReadWithRetry(filePath);
BaseHelper.FileHp.DeleteWithRetry(filePath);
BaseHelper.FileHp.MoveReplaceWithRetry(srcPath, dstPath);
```

### Other Helpers

```csharp
// Network: Kiểm tra kết nối IP
bool connected = BaseHelper.NetworkHp.CheckConnect("192.168.1.1", timeout: 200);

// Image: Convert to Base64
string base64 = BaseHelper.ImageHp.ToBase64FromUrl(filePath);

// Excel: Export danh sách ra file
string file = await BaseHelper.ExcelHp.ExportList(dataList, outputPath);
string file = await BaseHelper.ExcelHp.ExportByTemplate(data, templatePath, outputPath);

// Retry: Thực thi với retry
BaseHelper.RetryHp.RetryOnException(3, TimeSpan.FromSeconds(1), () => DoSomething());
var result = BaseHelper.RetryHp.RetryOnException(3, TimeSpan.FromSeconds(1), () => GetData());

// List: Dictionary helpers
var value = dictionary.GetDictionaryValue<int>("key");
string desc = enumValue.GetEnumDescription();

// EChart: Tạo danh sách ngày/giờ cho chart
var days = BaseHelper.EChartHp.GetListDay(startTime, endTime, "dd/MM");
var hours = BaseHelper.EChartHp.GetListHour(startTime, endTime, "HH:mm");
```
