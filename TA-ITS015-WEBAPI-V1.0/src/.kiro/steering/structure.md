# Project Structure

## Solution Layout

```
src/
├── TAC_WebAPI.sln                # Visual Studio solution file
├── TAC_WebAPI/                   # Main API host project
│   ├── Program.cs                # Entry point (Lamar + NLog)
│   ├── Startup.cs                # Service registration & middleware
│   ├── Configuration/            # Runtime config files (certs, creds)
│   ├── Properties/               # Launch settings
│   ├── Resources/                # Localization JSON files
│   ├── wwwroot/                  # Static files
│   ├── appsettings.json          # Base configuration
│   └── appsettings.Development.json
├── Modules/                      # Feature modules (independently built)
│   ├── TMS/                      # Traffic Management
│   │   ├── Modules.TMS/          # Core TMS module
│   │   └── Modules.TMS.Report/   # TMS reporting
│   ├── VMS/                      # Variable Message Sign
│   │   └── Modules.VMS/
│   ├── FMS/                      # Fleet Management
│   │   ├── Modules.FMS/
│   │   └── Modules.FMS.Report/
│   ├── CCTV/                     # Camera integration
│   │   └── Module.CCTV/
│   ├── CfgSystem/                # Configuration system
│   │   └── Modules.CfgSystem/
│   ├── DataTransporter/          # Real-time messaging
│   │   ├── Modules.DataTransporter.Base/
│   │   ├── Modules.DataTransporter.ITS/
│   │   ├── Modules.DataTransporter.Natsio/
│   │   └── Modules.DataTransporter.SocketCluster/
│   ├── Integration/              # External system integrations
│   │   ├── Modules.Integration.VMS/
│   │   ├── VMS/                  # VMS device protocols
│   │   └── CCTV/                 # NxVms integration
│   ├── Chainzones/               # Chainzone LED sign integration
│   └── Samplev2/                 # Template/sample module
├── Shared/                       # Shared libraries
│   ├── Shared.DTO/               # DTOs, constants, enums, interfaces
│   ├── Shared.Reference/         # NuGet package references (central)
│   └── Shared.Utility/           # Utility helpers and base APIs
├── DLLs/                         # Pre-compiled DLLs (Report, System, Core)
├── Tools/                        # Test/utility projects
│   ├── ITS.VDS_Test/
│   ├── ITS.VmsControl.Test/
│   ├── MultiCamStream/
│   └── TA-SocketServer/
└── docs/                         # Feature documentation
```

## Module Internal Structure (CQRS Pattern)

Each module follows a consistent structure:

```
Modules.{Name}/
├── Controllers/                  # API endpoints
│   ├── BaseController.cs         # Module base (route prefix, group name)
│   └── {Feature}/                # Feature folder
│       ├── {Feature}Controller.cs
│       ├── Commands/             # Write operations (Wolverine handlers)
│       │   └── {Feature}CommandHandler.cs
│       ├── Queries/              # Read operations (Wolverine handlers)
│       │   └── {Feature}QueryHandler.cs
│       ├── Dto/                  # Input/Output DTOs
│       └── Validators/           # FluentValidation validators
├── Core/                         # Domain layer
│   ├── Constants/                # Module constants
│   ├── Entities/                 # SqlSugar entity definitions
│   └── Exceptions/               # Custom exceptions
├── Infrastructure/               # Data access & services
│   ├── BaseRepository.cs         # Module-specific repository config
│   ├── Services/                 # Business logic services
│   └── Migrations/               # Database migrations
├── Extensions/                   # DI registration
│   ├── ModuleExtensions.cs
│   └── ServiceCollectionExtensions.cs
└── GlobalUsings.cs               # Module-wide using statements
```

## Key Patterns

### CQRS with Wolverine
```csharp
// Controller dispatches via message bus
public async Task<List<OutputDto>> GetList([FromQuery] InputDto input)
{
    return await MessBus.InvokeAsync<List<OutputDto>>(input);
}

// Handler processes the message
public class FeatureQueryHandler : IWolverineHandler
{
    public async Task<List<OutputDto>> HandleAsync(InputDto query) { ... }
}
```

### Module Registration
```csharp
// In Startup.cs
services.AddTMSModule(_config);

// In ModuleExtensions.cs
public static IServiceCollection AddTMSModule(this IServiceCollection services, IConfiguration config) { ... }
```

### Route Convention
- Base path: `api/{module-prefix}/[controller]`
- TMS: `api/tms/...`, VMS: `api/vms/...`, FMS: `api/fms/...`
- API grouping via `[ApiDescriptionSettings(GroupName)]`

### Entity Pattern
- SqlSugar attributes for table/column mapping
- Soft-delete with audit fields (CreateTime, CreateUId, UpdateTime)
- `BaseRepository<T>` for standard CRUD with diff logging

### Caching Pattern
```csharp
_cache.RemoveByPrefixKey(CacheConst.Module.EntityName);
```

### Real-time Broadcasting
```csharp
ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.ChannelName, payload);
```

### NuGet References
All NuGet packages are centralized in `Shared/Shared.Reference/Shared.Reference.csproj`. Modules reference `Shared.Reference` to inherit dependencies.
