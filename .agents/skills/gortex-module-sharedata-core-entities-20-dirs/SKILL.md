---
name: gortex-module-sharedata-core-entities-20-dirs
description: "Work in the Module.ShareData.Core/Entities +20 dirs area — 695 symbols across 38 files (89% cohesion)"
---

# Module.ShareData.Core/Entities +20 dirs

695 symbols | 38 files | 89% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataActivityLog.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataAlertLog.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataMapping.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataPacket.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataPacketField.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataPacketWrite.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataPartner.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataSubscription.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsZoneStatus.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneStatus/Dto/ZoneStatusInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneStatus/Dto/ZoneStatusOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsZoneStatus.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Common/Parsing/PacketJsonParser.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Common/Resolvers/PacketMetadataResolver.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Dto/CodeSetDto.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Dto/CodeValueDto.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Enums/ShareDataEnum.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Interfaces/DataOutbound/IDataOutboundExtractionProcess.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Interfaces/DataOutbound/IDataOutboundSender.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Interfaces/IDataOutboundService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Models/DataOutbound/DataMappingResult.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Models/DataOutbound/DataOutboundContext.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Models/DataOutbound/DataOutboundExtractionResult.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Models/DataOutbound/DataOutboundSendResult.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Logging/ShareDataAlertCode.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Logging/ShareDataTransferLog.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataInbound/DataInboundService.Logging.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataInbound/DataInboundService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/DataOutboundService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Extraction/DataOutboundExtractionProcess.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Mapping/DataMappingProcess.Expression.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Mapping/DataMappingProcess.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Scheduling/DataOutboundScheduler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Transport/DataOutboundFileSender.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Transport/DataOutboundRestSender.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Wpf/Logging/ActivityLoggerProvider.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Enums/BaseEnums.cs`
- `tests/ShareData/Services/DataOutboundServiceTests.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataActivityLog.cs` | RecordCount, AfterJson, OperatorIp, Description, OperatorName, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataAlertLog.cs` | Message, SessionId, AlertSource, PartnerId, AckBy, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataMapping.cs` | ShareDataMapping, DatatypeId, Direction, TargetShapeJson, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataPacket.cs` | OrderNo, Code, Name, Description, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataPacketField.cs` | Status, Type, DatatypeId, Remark, IsRequired, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataPacketWrite.cs` | ShareDataPacketWrite, PacketCode, TableName, WriteSql, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataPartner.cs` | Status, Port, ShareDataPartner, SessionState, OrderNo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataSubscription.cs` | DatatypeId, ShareDataSubscription, DebounceSec, IntervalSeconds, NextTimeRun, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsZoneStatus.cs` | Condition, DetectOthers, ZoneId, DetectWay, AverageSpeed, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneStatus/Dto/ZoneStatusInput.cs` | AddZoneStatusInput, UpdateZoneStatusInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneStatus/Dto/ZoneStatusOutput.cs` | PageZoneStatusOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsZoneStatus.cs` | DetectOthers, AverageSpeed, Condition, DetectStop, ZoneId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Common/Parsing/PacketJsonParser.cs` | ParseCodeValuesFromArray, arrayElement |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Common/Resolvers/PacketMetadataResolver.cs` | code, PacketMetadataResolver, ResolveFilterMode, datatypeEnum, ExtractPacketNumber, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Dto/CodeSetDto.cs` | Values, DefaultPartnerValue, DefaultSourceValue, CodeSetDto |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Dto/CodeValueDto.cs` | PartnerValue, DisplayName, CodeValueDto, OrderNo, IsDefault, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Enums/ShareDataEnum.cs` | TestPacket975, ShareDataEnum, Receive, TestPacket984, TransferDirection, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Interfaces/DataOutbound/IDataOutboundExtractionProcess.cs` | IDataOutboundExtractionProcess, sub, packet, onLogWarning, db, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Interfaces/DataOutbound/IDataOutboundSender.cs` | mapping, ctx, Send, ct, IDataOutboundSender |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Interfaces/IDataOutboundService.cs` | cancellationToken, exportedAt, db, IDataOutboundService, sub, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Models/DataOutbound/DataMappingResult.cs` | Success, FinalBytes, DataMappingResult, RecordCount, ErrorMessage |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Models/DataOutbound/DataOutboundContext.cs` | ExportedAt, PartnerCode, Mapping, DataOutboundContext, Partner, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Models/DataOutbound/DataOutboundExtractionResult.cs` | DataOutboundExtractionResult, MaxWatermark, RawRows, MaxLastId |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker.Core/Models/DataOutbound/DataOutboundSendResult.cs` | ErrorMessage, DataOutboundSendResult, DetailJson, RelativePath, ByteSize, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Logging/ShareDataAlertCode.cs` | ShareDataAlertCode |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Logging/ShareDataTransferLog.cs` | filePath, recordCount, Cut, db, hash, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataInbound/DataInboundService.Logging.cs` | db, severity, sub, partner, alertCode, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataInbound/DataInboundService.cs` | state, IsUsableSubscription, partnerCode, Fail, ResolveContextAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/DataOutboundService.cs` | partner, ExecuteExportForSubscription, status, packetVersion, errorMessage, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Extraction/DataOutboundExtractionProcess.cs` | sub, db, packet, db, packet, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Mapping/DataMappingProcess.Expression.cs` | expression, DataMappingProcess, ValidateExpression, MaxRecursionDepth, SupportedFunctions, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Mapping/DataMappingProcess.cs` | MaxShapeDepth, codeSets, node, payload, Transform, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Scheduling/DataOutboundScheduler.cs` | scheduleRoot, ComputeNextDailyRun, value, now, TryGetProperty, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Transport/DataOutboundFileSender.cs` | sub, datatypeId, ResolveDatatypeFolderName, relativePath, SaveExportFile, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Services/DataOutbound/Transport/DataOutboundRestSender.cs` | ctx, ct, ctx, request, DataOutboundRestSender, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Wpf/Logging/ActivityLoggerProvider.cs` | IsEnabled, logLevel |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Enums/BaseEnums.cs` | Funnel, File, Session, Error, Single, ... |
| `tests/ShareData/Services/DataOutboundServiceTests.cs` | datatypeId, RestSender_SendsDirectFinalBytes_WithoutHttpPayloadWrapper_C1_C3_C4_C5_Test, partnerCode, Transform_D3_WhenValueIsEmpty_WithCodeSetAndLeafDefault_PrioritizesCodeSetDefault_Test, ct, ... |

## Entry Points

- `tests/ShareData/Services/DataOutboundServiceTests.cs::DataOutboundServiceTests.QueryPacket101_WithSeededData_ReturnsZoneStatusIdAndExportsSuccessfully_Test`
- `tests/ShareData/Services/DataOutboundServiceTests.cs::DataOutboundServiceTests.OutboundPipeline_EndToEnd_MapTransformAndSend_Succeeds_Test`
- `tests/ShareData/Services/DataOutboundServiceTests.cs::DataOutboundServiceTests.ProcessScheduledSubscriptions_IncrementalCatchUp_AfterIdle_ExportsEveryNewRowExactlyOnce_Test`
- `tests/ShareData/Services/DataOutboundServiceTests.cs::DataOutboundServiceTests.ProcessScheduledSubscriptions_WhenShapeHasHeaderAndRecordArray_WritesSingleHeaderInPayload_Test`
- `tests/ShareData/Services/DataOutboundServiceTests.cs::DataOutboundServiceTests.ProcessScheduledSubscriptions_WhenPacketCodeHasSuffixLikeStagingDb_ResolvesHandlerAndExportsSuccessfully_Test`

## Connected Communities

- **Common/Parsing +1 dirs** (4 cross-edges)
- **DataOutbound/Mapping · TokenType** (2 cross-edges)
- **DataOutbound/Mapping · RenderShapeNode** (2 cross-edges)
- **ShareDataWorker.Core/Enums +2 dirs** (1 cross-edges)
- **Services/DataInbound · Walk** (1 cross-edges)
- **DataOutbound/Mapping · ParsePrimary** (1 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-2401")
explore(operation:"context", task:"understand Module.ShareData.Core/Entities +20 dirs", format:"gcx")
relations(operation:"usages", target:{symbol:"tests/ShareData/Services/DataOutboundServiceTests.cs::DataOutboundServiceTests.QueryPacket101_WithSeededData_ReturnsZoneStatusIdAndExportsSuccessfully_Test"}, format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
