---
name: gortex-services-device-17-dirs
description: "Work in the Services/Device +17 dirs area — 470 symbols across 24 files (90% cohesion)"
---

# Services/Device +17 dirs

470 symbols | 24 files | 90% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/CctvDeviceController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Commands/CctvDeviceCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Dto/CctvDevicesInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Dto/CctvDevicesOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Queries/CctvDeviceQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Media/CctvMediaController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Media/Dto/CctvMediaInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Media/Dto/CctvMediaOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Media/Queries/CctvMediaQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/CctvPtzController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Commands/CctvPTZCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Dto/CctvPtzInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Dto/CctvPtzOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Queries/CctvPtzQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Core/Entities/CctvDevice.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Device/CctvDeviceConfigService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Device/CctvDeviceResolveService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Device/DeviceService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Media/MediaSerivce.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Vendor/VendorService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Integration/CCTV/Module.Integration.NxVms/Core/Abstracts/BaseCctvService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Integration/CCTV/Module.Integration.NxVms/Core/Abstracts/INxVmsConfigProvider.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Integration/CCTV/Module.Integration.NxVms/Core/Dto/CctvInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Enums/DeviceEnum.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/CctvDeviceController.cs` | input, GetDeviceView |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Commands/CctvDeviceCommandHandler.cs` | command, ip, cctvDeviceRep, ResolveDeviceAsync, id, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Dto/CctvDevicesInput.cs` | DeviceId, DeviceId, CctvDeviceActiveInput, CctvGetDeviceViewInput, Ip, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Dto/CctvDevicesOutput.cs` | ID, TotalInserted, EquipmentTypeId, EquipmentState, TotalUpdated, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Queries/CctvDeviceQueryHandler.cs` | _equipmentTypeRep, cancellationToken, logger, _logger, _cctvDeviceRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Media/CctvMediaController.cs` | CctvMediaController, GetSnapshot, GetFootage, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Media/Dto/CctvMediaInput.cs` | Ip, ToDate, Vendor, Size, PeriodType, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Media/Dto/CctvMediaOutput.cs` | CctvSnapshotOutput, CctvWebRtcStreamOutput, StartTime, DeviceId, Url, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Media/Queries/CctvMediaQueryHandler.cs` | HandleAsync, cancellationToken, input, vendorService, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/CctvPtzController.cs` | input, GetPreset |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Commands/CctvPTZCommandHandler.cs` | result, HandleAsync, _configService, ct, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Dto/CctvPtzInput.cs` | Ip, Vendor, PresetName, CctvPtzNudgeInput, CctvPtzStopInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Dto/CctvPtzOutput.cs` | Name, TourId, CctvPtzTourOutput, CctvPtzPresetOutput, PresetId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Queries/CctvPtzQueryHandler.cs` | CctvPtzQueryHandler.<init>, input, HandleAsync, CctvPtzQueryHandler, deviceService, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Core/Entities/CctvDevice.cs` | Url, Status, DeviceId, Ip, SnapshotTime, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Device/CctvDeviceConfigService.cs` | ct, ct, GetDevicePtzConfigAsync, deviceId, ResolveEquipmentAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Device/CctvDeviceResolveService.cs` | ip, _cctvDeviceRep, ct, cctvDeviceRep, deviceId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Device/DeviceService.cs` | cfg, GetPtzConfigAsync, TryParseStandard, DeviceService, json, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Media/MediaSerivce.cs` | MediaSerivce, value, _hostEnvironment, imageBytes, snapshotUrl, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Vendor/VendorService.cs` | VendorService, _config, _sp, sp, vendorCode, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Integration/CCTV/Module.Integration.NxVms/Core/Abstracts/BaseCctvService.cs` | cancellationToken, GetToursAsync, BaseCctvService, input, cancellationToken, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Integration/CCTV/Module.Integration.NxVms/Core/Abstracts/INxVmsConfigProvider.cs` | GetBaseUrlAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Integration/CCTV/Module.Integration.NxVms/Core/Dto/CctvInput.cs` | Crop, Tilt, Format, FromDate, VmsSnapshotInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Enums/DeviceEnum.cs` | Right, Up, PtzDirection, Left, DownLeft, ... |

## Connected Communities

- **CCTV/Module.CCTV · PtzSpeedConfig** (3 cross-edges)
- **Module.CCTV/Infrastructure** (3 cross-edges)
- **Shared.DTO/Enums · CctvItsStatus** (1 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-25")
explore(operation:"context", task:"understand Services/Device +17 dirs", format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
