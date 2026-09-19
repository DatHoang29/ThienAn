---
name: gortex-maintenanceschedule-dto-50-dirs
description: "Work in the MaintenanceSchedule/Dto +50 dirs area — 765 symbols across 63 files (81% cohesion)"
---

# MaintenanceSchedule/Dto +50 dirs

765 symbols | 63 files | 81% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/Commands/DashboardCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/Dto/DashboardInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/DashboardItem/Commands/DashboardItemCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/DashboardItem/Dto/DashboardItemInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/DashboardItem/Queries/DashboardItemQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Core/Entities/CctvDashboardItem.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Device/CctvDeviceConfigService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Constants/MaintenanceConst.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/IncidentControl/TmsIncidentControlInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceSchedule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Exceptions/IncidentControlConst.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Commands/DutyScheduleChangeRequestCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Commands/DutyScheduleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Queries/DutyScheduleChangeRequestQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Queries/DutyScheduleHistoryQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Queries/EquipmentByTypeQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Queries/EquipmentPositionQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentControl/Command/TmsIncidentControlCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceAlertCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceScheduleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceAlertInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceGuideInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceScheduleInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceScheduleOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceAlertQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceAlertRuleQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceGuideQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceScheduleQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceAlertController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceScheduleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/Queries/PatrolTeamQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/Queries/TrafficPlanQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Commands/TmsWeatherCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Commands/ZoneCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Queries/ZoneEquipmentQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceSchedule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/BaseRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/Incident/IncidentAccessService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/IncidentAutomation/TmsIncidentAutomationService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TmsEquipments/TmsEquipmentService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TrafficInfo/TrafficInfoService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TrafficMatrix/TmsTrafficMatrixService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/WorkUnit/WorkUnitService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Controller/Commands/VwControllerCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Controller/Queries/VwControllerQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/EventRule/Commands/VwEventRuleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/EventRule/Queries/VwEventRuleQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/Commands/VwSceneCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/Commands/VwSceneWorkflowCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/Queries/VwSceneQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/Commands/VwScheduleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/Queries/VwScheduleQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Screen/Commands/VwScreenCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Screen/Queries/VwScreenQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/SlotPort/Commands/VwSlotPortCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/SlotPort/Queries/VwSlotPortQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Source/Commands/VwSourceCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Source/Queries/VwSourceQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/WindowScene/Commands/VwWindowSceneCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Infrastructure/Services/Access/VwOrgAccessService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Infrastructure/Services/Scene/VwSceneRegionService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/TrafficAnalysisRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Constants/Application/CacheConst.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/Commands/DashboardCommandHandler.cs` | cache, cctvDashboardRep, HandleAsync, command, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/Dto/DashboardInput.cs` | Name, CopyDashboardInput, ID |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/DashboardItem/Commands/DashboardItemCommandHandler.cs` | _cctvDashboardItemRep, command, _cctvDashboardRep, cctvDashboardItemRep, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/DashboardItem/Dto/DashboardItemInput.cs` | Items, W, DashboardItemPayload, AddDashboardItemInput, DashboardId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/DashboardItem/Queries/DashboardItemQueryHandler.cs` | _bus, bus, _cache, DashboardItemQueryHandler, DashboardItemQueryHandler.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Core/Entities/CctvDashboardItem.cs` | X, CctvDashboardItem, EquipmentId, DashboardId, W, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Infrastructure/Services/Device/CctvDeviceConfigService.cs` | equipmentRep, CctvDeviceConfigService.<init>, cctvDeviceRep |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Constants/MaintenanceConst.cs` | MaintenanceConst |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/IncidentControl/TmsIncidentControlInput.cs` | UpdateTmsIncidentControlInput, AddTmsIncidentControlInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceSchedule.cs` | ReminderSent, ScheduledDate, ReminderDaysBefore, Code, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Exceptions/IncidentControlConst.cs` | IncidentControlConst |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Commands/DutyScheduleChangeRequestCommandHandler.cs` | historyRep, changeRequestRep, localizer, dutyScheduleRep, detailRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Commands/DutyScheduleCommandHandler.cs` | patrolTeamRep, DutyScheduleCommandHandler.<init>, user, dutyScheduleRep, historyRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Queries/DutyScheduleChangeRequestQueryHandler.cs` | DutyScheduleChangeRequestQueryHandler, _changeRequestRep, cache, _user, _detailRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Queries/DutyScheduleHistoryQueryHandler.cs` | user, DutyScheduleHistoryQueryHandler.<init>, dutyScheduleRep, historyRep |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Queries/EquipmentByTypeQueryHandler.cs` | EquipmentByTypeQueryHandler.<init>, tmsEquipTypeRep, tmsEquipRep, EquipmentByTypeQueryHandler, _tmsEquipTypeRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Queries/EquipmentPositionQueryHandler.cs` | tmsEquipRep, zoneEquipRep, eventRuleRep, EquipmentPositionQueryHandler.<init> |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentControl/Command/TmsIncidentControlCommandHandler.cs` | HandleAsync, cacheService, TmsIncidentControlCommandHandler, repo, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceAlertCommandHandler.cs` | MaintenanceAlertCommandHandler.<init>, AlertStatusDismissedVariants, _equipmentRep, user, _user, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceScheduleCommandHandler.cs` | CanonicalScheduleStatus, _user, ScheduleStatusRegisterVariants, ScheduleStatusOverdueVariants, equipmentTypeRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceAlertInput.cs` | TotalCount, Priority, CreateManualAlertInput, PendingCount, GetAlertCountByStatusInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceGuideInput.cs` | Required, Task, ChecklistItem, Note, Checklist, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceScheduleInput.cs` | MaintenanceScheduleId, EquipmentId, Status, MaintenanceScheduleFilterMode, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceScheduleOutput.cs` | TaskCount, Manufacturer, MaintenanceScheduleOutput, EquipmentTypeName, EquipmentSummary, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceAlertQueryHandler.cs` | HandleAsync, guideRep, _cache, command, equipmentRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceAlertRuleQueryHandler.cs` | MaintenanceAlertRuleQueryHandler.<init>, _equipmentTypeRep, _ruleRep, cache, equipmentTypeRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceGuideQueryHandler.cs` | MaintenanceGuideQueryHandler.<init>, equipmentRep, equipmentTypeRep, cache, guideRep |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceScheduleQueryHandler.cs` | TaskStatusPendingVariants, scheduleEquipmentRep, ScheduleStatusDraftVariants, user, TaskStatusSkippedVariants, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceAlertController.cs` | GetCountByStatus |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceScheduleController.cs` | input, ConfirmEquipmentChecked, input, GetMaintenanceById, id, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/Queries/PatrolTeamQueryHandler.cs` | memberRep, PatrolTeamQueryHandler.<init>, _memberRep, cache, PatrolTeamQueryHandler, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/Queries/TrafficPlanQueryHandler.cs` | _TmsTrafficPlanRep, TmsTrafficPlanRep, TrafficPlanQueryHandler, _cache, _tmsTrafficPlanWorkDetailRepo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Commands/TmsWeatherCommandHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Commands/ZoneCommandHandler.cs` | tmsMapLocationRep, cache, ZoneCommandHandler.<init>, tmsZoneRep |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Queries/ZoneEquipmentQueryHandler.cs` | _cache, ZoneEquipmentQueryHandler, zoneEquipmentRep, _zoneEquipmentRep, equipmentRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceSchedule.cs` | Status, ReminderSent, Remark, Code, Priority, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/BaseRepository.cs` | iTenant, BaseRepository.<init>, BaseRepository, T |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/Incident/IncidentAccessService.cs` | IncidentAccessService.<init>, logger, workDetailContactRepo, workExecuteRepo, permissionService, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/IncidentAutomation/TmsIncidentAutomationService.cs` | trafficPlanWorkDetailRep, TmsIncidentAutomationService.<init>, workDetailRep, equipmentEventRuleRep, workExecuteRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TmsEquipments/TmsEquipmentService.cs` | _tmsEquipment, tmsModule, tmsEquipment, _hostingEnvironment, TmsEquipmentService, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TrafficInfo/TrafficInfoService.cs` | tmsEquipment, _tmsEquipment, tmsEquipmentType, _tmsIncident, tmsModule, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TrafficMatrix/TmsTrafficMatrixService.cs` | opConfigRepo, configDataRepo, messageBus, TmsTrafficMatrixService.<init>, trafficDataRepo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/WorkUnit/WorkUnitService.cs` | _mapDetailRepo, workUnitRepo, WorkUnitService, _workUnitRepo, mapDetailRepo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Controller/Commands/VwControllerCommandHandler.cs` | cache, _vwControllerRep, VwControllerCommandHandler.<init>, _orgAccess, _cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Controller/Queries/VwControllerQueryHandler.cs` | _vwControllerRep, vwControllerRep, orgAccess, VwControllerQueryHandler, cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/EventRule/Commands/VwEventRuleCommandHandler.cs` | cache, orgAccess, _vwEventRuleRep, VwEventRuleCommandHandler.<init>, vwEventRuleRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/EventRule/Queries/VwEventRuleQueryHandler.cs` | VwEventRuleQueryHandler.<init>, vwEventRuleRep, _cache, cache, _vwEventRuleRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/Commands/VwSceneCommandHandler.cs` | orgAccess, VwSceneCommandHandler, VwSceneCommandHandler.<init>, cache, _orgAccess, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/Commands/VwSceneWorkflowCommandHandler.cs` | _vwEventRuleRep, VwSceneWorkflowCommandHandler.<init>, _vwSceneRep, _cache, VwSceneWorkflowCommandHandler, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/Queries/VwSceneQueryHandler.cs` | _vwControllerRep, vwControllerRep, _cache, vwSceneRep, VwSceneQueryHandler, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/Commands/VwScheduleCommandHandler.cs` | vwSceneRep, VwScheduleCommandHandler, cache, _cache, _orgAccess, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/Queries/VwScheduleQueryHandler.cs` | _cache, VwScheduleQueryHandler.<init>, cache, _orgAccess, orgAccess, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Screen/Commands/VwScreenCommandHandler.cs` | vwControllerRep, vwScreenRep, VwScreenCommandHandler.<init>, cache, orgAccess |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Screen/Queries/VwScreenQueryHandler.cs` | _cache, VwScreenQueryHandler, vwScreenRep, VwScreenQueryHandler.<init>, cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/SlotPort/Commands/VwSlotPortCommandHandler.cs` | orgAccess, cache, vwScreenRep, vwSourceRep, vwSlotPortRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/SlotPort/Queries/VwSlotPortQueryHandler.cs` | cache, orgAccess, VwSlotPortQueryHandler.<init>, _orgAccess, vwSlotPortRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Source/Commands/VwSourceCommandHandler.cs` | vwControllerRep, orgAccess, cache, vwSourceRep, VwSourceCommandHandler.<init> |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Source/Queries/VwSourceQueryHandler.cs` | cache, VwSourceQueryHandler.<init>, vwSourceRep, _orgAccess, _cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/WindowScene/Commands/VwWindowSceneCommandHandler.cs` | _regionService, cache, regionService, VwWindowSceneCommandHandler.<init>, vwWindowSceneRep, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Infrastructure/Services/Access/VwOrgAccessService.cs` | vwSceneRep, vwControllerRep, vwScreenRep, _scope, VwOrgAccessService, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Infrastructure/Services/Scene/VwSceneRegionService.cs` | vwScreenRep, vwSceneRep, VwSceneRegionService.<init> |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Counting/Persistence/TrafficAnalysisRepository.cs` | entity, ct, InsertAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Constants/Application/CacheConst.cs` | App, KeyPasswordErrorTimes |

## Entry Points

- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceScheduleQueryHandler.cs::MaintenanceScheduleQueryHandler.HandleAsync_L453`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceScheduleCommandHandler.cs::MaintenanceScheduleCommandHandler.HandleAsync_L315`

## Connected Communities

- **Category/Dto +14 dirs** (4 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-323")
explore(operation:"context", task:"understand MaintenanceSchedule/Dto +50 dirs", format:"gcx")
relations(operation:"usages", target:{symbol:"TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceScheduleQueryHandler.cs::MaintenanceScheduleQueryHandler.HandleAsync_L453"}, format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
