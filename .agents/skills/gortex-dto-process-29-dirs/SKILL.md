---
name: gortex-dto-process-29-dirs
description: "Work in the Dto/Process +29 dirs area — 469 symbols across 47 files (90% cohesion)"
---

# Dto/Process +29 dirs

469 symbols | 47 files | 90% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Share/Dto/ShareInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS.Report/Core/Helpers/LogoHelper.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS.Report/Core/Helpers/RptBaseHelper.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/IncidentControl/TmsIncidentControlInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Control/ControlInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Control/ImageControlInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Current/VmsCurrentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/DataSendToDevice.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/LcsProcessInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/PrepareVmsData.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/ProcessInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/ProcessOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/ScheduleProcessInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/TextToImageProcessInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/TimerProcessInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ProcessImg/VmsProcessImgInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsProcess.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsProcessImg.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Control/Commands/ControlVmsCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/Commands/ResetIncidentToDefaultCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/VmsCurrentController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/EventDefault/Commands/EventDefaultCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/LcsProcess/Commands/LcsImageCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/ImageCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/ProcessCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/ScheduleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/TemplateCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/TextCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/TimerCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Queries/ProcessQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ProcessImg/Commands/ProcessImgCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ProcessImg/Queries/ProcessImgQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Template/Commands/TemplateCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/BaseRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/BackgroundService/ResendToDeviceBackgroundService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/BackgroundService/RestartScheduleBackgroundService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/Current/VmsCurrentService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/Imaging/VmsImageGenService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/Process/VmsProcessService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/Schedule/ScheduleService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Constants/Module/VmsControlTypeConst.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Dtos/Tms/TmsSignalLogRequest.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Endpoints/BaseEndpoint.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Endpoints/Tms/TmsEndpoint.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Utilities/BaseUtil.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Utilities/FileServerUtil.cs`
- `tests/ShareData/Services/DataOutboundServiceTests.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Share/Dto/ShareInput.cs` | Type, FilePath, ShareInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS.Report/Core/Helpers/LogoHelper.cs` | LogoHelper, GetLogo |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS.Report/Core/Helpers/RptBaseHelper.cs` | LogoHp |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/IncidentControl/TmsIncidentControlInput.cs` | ResetIncidentToDefaultInput, IncidentId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Control/ControlInput.cs` | TextToImageInput, FontSize, Size, Priority, ScheduleList, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Control/ImageControlInput.cs` | TemplateControlInput, ImageControlInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Current/VmsCurrentInput.cs` | UpdateVmsCurrentInputByProcess |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/DataSendToDevice.cs` | EquipmentInfoInput, Url, Brightness, DataSendToDevice |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/LcsProcessInput.cs` | LcsImageControlInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/PrepareVmsData.cs` | ThreadId, FontSize, Priority, Vendor, ProcessType, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/ProcessInput.cs` | ListProcess, ScheduleList, ListProcess, RestartProcessScheduleInput, ResendToDeviceInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/ProcessOutput.cs` | ProcessOutput, PageVmsProcessOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/ScheduleProcessInput.cs` | VmsID, ProcessType, Priority, ThreadId, StartProcess, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/TextToImageProcessInput.cs` | EquipmentIds, TextToImageProcessInput, TextToImageInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Process/TimerProcessInput.cs` | TimerProcessInput, EquipmentIds, Url |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ProcessImg/VmsProcessImgInput.cs` | AddVmsProcessImgInput, Data, OldUrl, ImageUrl, ListUrl, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsProcess.cs` | VmsId, TimeEnd, ThreadId, Type, Priority, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsProcessImg.cs` | VmsProcessImg, Url, ProcessId, TimeToLive |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Control/Commands/ControlVmsCommandHandler.cs` | input, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/Commands/ResetIncidentToDefaultCommandHandler.cs` | vmsProcessService, ResetIncidentToDefaultCommandHandler.<init>, _vmsProcessService, command, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/VmsCurrentController.cs` | httpContextAccessor, RestartEquipment, VmsCurrentController.<init>, vmsProcessService, Incidentid, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/EventDefault/Commands/EventDefaultCommandHandler.cs` | cache, equipmentTypeRepository, imageService, baseRepository, EventDefaultCommandHandler.<init> |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/LcsProcess/Commands/LcsImageCommandHandler.cs` | LcsImageCommandHandler, userManager, vmsProcessService, _vmsProcessService, command, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/ImageCommandHandler.cs` | _env, ImageCommandHandler, vmsProcessService, _vmsImageGenService, env, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/ProcessCommandHandler.cs` | _vmsProcessService, HandleAsync, ProcessCommandHandler.<init>, input, vmsProcessService, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/ScheduleCommandHandler.cs` | HandleAsync, command, scheduleService, InitialSchedule, VmsProcessService, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/TemplateCommandHandler.cs` | _vmsImageGenService, HandleAsync, env, _env, TemplateCommandHandler, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/TextCommandHandler.cs` | scheduleService, _vmsImageGenService, vmsImageGenService, HandleAsync, userManager, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/TimerCommandHandler.cs` | command, _vmsProcessImgRepository, vmsProcessRepository, StartTimerVms, StopTimerVms, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Queries/ProcessQueryHandler.cs` | _cache, _vmsProcessRepository, vmsProcessRepository, ProcessQueryHandler.<init>, _vmsProcessImgRepository, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ProcessImg/Commands/ProcessImgCommandHandler.cs` | command, userManager, HandleAsync, ProcessImgCommandHandler.<init>, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ProcessImg/Queries/ProcessImgQueryHandler.cs` | baseRepository, userManager, ProcessImgQueryHandler, ProcessImgQueryHandler.<init>, cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Template/Commands/TemplateCommandHandler.cs` | templateService, uploadService, vmsTemplateRep, TemplateCommandHandler.<init>, cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/BaseRepository.cs` | BaseRepository, BaseRepository.<init>, T |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/BackgroundService/ResendToDeviceBackgroundService.cs` | StartAsync, cancellationToken |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/BackgroundService/RestartScheduleBackgroundService.cs` | cancellationToken, StartAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/Current/VmsCurrentService.cs` | GetListVmsCurrent |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/Imaging/VmsImageGenService.cs` | renderer, VmsImageGenService, input, GenerateImage, BuildTextStyle, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/Process/VmsProcessService.cs` | equipmentIds, VmsProcessService, dataList, imageUrl, processList, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Infrastructure/Services/Schedule/ScheduleService.cs` | threadId, vmsIds, listVmsProcessMemory, listVmsProcessImgMemory, StopScheduleByVmsIds, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Constants/Module/VmsControlTypeConst.cs` | Message, VmsControlTypeConst, Text, Schedule, Image, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Dtos/Tms/TmsSignalLogRequest.cs` | OldData, NewToDate, OldFromDate, ExecuteBy, NewUrl, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Endpoints/BaseEndpoint.cs` | TmsEp |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Endpoints/Tms/TmsEndpoint.cs` | TmsEndpoint |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Utilities/BaseUtil.cs` | FileServer |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Utilities/FileServerUtil.cs` | WebRoot, PathTypeEnum, Cdn, GetPath, UrlFileExistAsync, ... |
| `tests/ShareData/Services/DataOutboundServiceTests.cs` | cancellationToken, SendAsync, request |

## Connected Communities

- **Category/Dto +14 dirs** (4 cross-edges)
- **Modules/VMS · DrawText** (2 cross-edges)
- **Infrastructure/Services +17 dirs** (1 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-502")
explore(operation:"context", task:"understand Dto/Process +29 dirs", format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
