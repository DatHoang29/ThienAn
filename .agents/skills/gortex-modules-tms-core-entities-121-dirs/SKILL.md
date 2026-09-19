---
name: gortex-modules-tms-core-entities-121-dirs
description: "Work in the Modules.TMS.Core/Entities +121 dirs area — 2230 symbols across 238 files (82% cohesion)"
---

# Modules.TMS.Core/Entities +121 dirs

2230 symbols | 238 files | 82% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/Commands/DashboardCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Core/Entities/CctvDashboard.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Entities/SysOpConfig.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Entities/SysTerminology.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Commands/SysOpConfigCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Dto/SysOpConfigInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Dto/SysOpConfigOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/Commands/SysTerminologyCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/Dto/SysTerminologyInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/Dto/SysTerminologyOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Core/Entities/SysOpConfig.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Core/Entities/SysTerminology.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Infrastructure/Persistence/SeedData/SysOpConfigSeedData.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Core/Entities/FmsEquipmentTelemetry.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Core/Entities/FmsTopology.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail.Core/Entities/RptEmailLog.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail.Core/Entities/RptEmailSchedule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail.Core/Entities/RptEmailScheduleItem.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Constants/TrafficPlanConst.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/Equipment/EquipmentMsgInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/Incident/TmsIncidentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/IncidentAutomationConfig/IncidentAutomationConfigInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/IncidentAutomationRun/IncidentAutomationRunOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsDutySchedule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsDutyScheduleChangeRequest.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsDutyScheduleDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsDutyScheduleHistory.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsEquipment.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsEquipmentType.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsEventData.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsEventType.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsIncident.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsIncidentAutomationConfig.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsIncidentAutomationRun.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceAlert.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceAlertRule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceGuide.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceHistory.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceScheduleEquipment.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceTask.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMap.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMapDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMapLocation.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsModule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsPatrolTeam.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsPatrolTeamMember.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsSignalLog.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficAnalysis.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficData.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficJourney.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficPlanWorkDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficPlans.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficSegment.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficStatistic.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsVehicleFilter.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsVehicleFilterHistory.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsVehicleRegistration.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWeather.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkContact.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkDetailContact.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkExecute.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkExecuteHistory.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkUnit.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsZone.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsZoneEquipment.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Exceptions/BaseMsg.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Commands/DutyScheduleChangeRequestCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Commands/DutyScheduleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Commands/EquipmentCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Dto/EquipmentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Queries/EquipmentByTypeQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Queries/EquipmentQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Validators/EquipmentValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentEventRule/Validators/EquipmentEventRuleValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentType/Commands/EquipmentTypeCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentType/Dto/EquipmentTypeInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentType/Validators/EquipmentTypeValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EventType/Commands/EventTypeCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EventType/Dto/EventTypeInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EventType/Validators/EventTypeValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Commands/IncidentCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Dto/TmsIncidentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Dto/TmsIncidentOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Validators/IncidentValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationConfig/Commands/IncidentAutomationConfigCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationConfig/Validators/IncidentAutomationConfigValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationRun/Dto/IncidentAutomationRunInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationRun/TmsIncidentAutomationRunController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceAlertCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceAlertRuleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceGuideCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceAlertInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceGuideInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Map/Commands/MapCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Map/Validators/MapValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Commands/MapDetailCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Dto/MapDetailsInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Validators/MapDetailsValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/Commands/MapLocationCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/Dto/MapLocationsInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/Validators/MapLocationsValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Module/Commands/ModuleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Module/Dto/ModuleInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Module/Validators/ModuleValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/Commands/PatrolTeamCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/Dto/PatrolTeamInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogHistory/Dto/SignalLogHistoryOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/Commands/SignalLogCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/Dto/SignalLogsInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/TmsSignalLogController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/Validators/SignalLogsValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficInfo/TmsTrafficInfoController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficMatrix/Validators/TrafficMatrixValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/Commands/TrafficPlanCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/Dto/TrafficPlanInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/Validators/TrafficPlanValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Commands/TrafficPlanWorkDetailCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Dto/TrafficPlanWorkDetailInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Validators/TrafficPlanWorkDetailValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficStatistics/Commands/TrafficStatisticCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficStatistics/Dto/TrafficStatisticInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficStatistics/Validators/TrafficStatisticValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Commands/TmsVehicleFilterCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Dto/TmsVehicleFilterInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Validators/VehicleFilterValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Commands/TmsVehicleFilterHistoryCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Dto/TmsVehicleFilterHistoryInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Commands/TmsVehicleRegistrationCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Dto/TmsVehicleRegistrationInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Validators/VehicleRegistrationValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Commands/TmsWeatherCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Commands/TmsWorkContactCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Dto/TmsWorkContactInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Queries/TmsWorkContactQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Validators/WorkContactValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Commands/WorkDetailCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Dto/WorkDetailInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Validators/WorkDetailValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Commands/WorkDetailContactCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Dto/TmsWorkDetailContactInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Validators/WorkDetailContactValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Commands/TmsWorkUnitCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Dto/TmsWorkUnitInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Validators/WorkUnitValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Commands/ZoneCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Dto/ZoneInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Dto/ZoneOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Validators/ZoneValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Commands/ZoneEquipmentCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Dto/AddZoneEquipmentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Dto/ConfirmZoneEquipmentMappingInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Dto/DeleteZoneEquipmentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Dto/UpdateZoneEquipmentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsDutySchedule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsDutyScheduleChangeRequest.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsDutyScheduleDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsDutyScheduleHistory.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsIncident.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceAlert.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceAlertRule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceGuide.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceHistory.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceScheduleEquipment.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceTask.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMapDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsPatrolTeam.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsPatrolTeamMember.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsSignalLog.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficAnalysis.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficData.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficPlanWorkDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficPlans.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficStatistic.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWeather.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWorkDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWorkDetailContact.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWorkExecute.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWorkExecuteHistory.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsZoneEquipment.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsEquipment.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsEquipmentType.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsEventType.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsMap.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsMapLocation.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsModule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsVehicleFilter.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsVehicleFilterHistory.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsVehicleRegistration.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsWorkContact.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsWorkUnit.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsZone.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/IncidentAutomation/TmsIncidentAutomationService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/SignalLog/SignalLogService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TrafficInfo/TrafficInfoService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TrafficPlanWorkDetail/TrafficPlanWorkDetailService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollLane.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollPriceList.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollPriceListDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollRoad.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollShift.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollStation.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollSyncLog.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollTicketOutSummary.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollTransactionIn.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollTransactionOut.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Message/VmsMessageInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsEquipmentTelemetry.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsEventDefault.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsMessage.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Message/Commands/VmsMessageCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/ScheduleCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Schedule/VwScheduleOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/SlotPort/VwSlotPortOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwControllerSlot.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwSchedule.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwSlotPort.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwWallTopology.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/Validators/VwScheduleValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/SlotPort/Validators/VwSlotPortValidator.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceList/Dto/WpPriceListOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Toll/Dto/WpTollOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/WpConfig/Commands/ConfigCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/WpConfig/Dto/WpConfigOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpConfig.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpMenu.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpPriceList.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpPriceListDetail.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpToll.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Persistence/VdsDiagnosticsRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Events/Persistence/EventDataRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/Persistence/VehicleFilterRepository.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/VehicleFilterManager.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Constants/Application/CacheConst.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Constants/Application/EntityConst.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Enums/BaseEnums.cs`
- `tests/VideoWall/Controllers/VwScheduleTests.cs`
- `tests/VideoWall/Controllers/VwSlotPortTests.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/Commands/DashboardCommandHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Core/Entities/CctvDashboard.cs` | Name, Config, Remark, Tags, CctvDashboard |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Entities/SysOpConfig.cs` | SysOpConfig, Value, GroupCode, IsSysConfig, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Entities/SysTerminology.cs` | OrderNo, SysTerminology, Value, Lang, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Commands/SysOpConfigCommandHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Dto/SysOpConfigInput.cs` | AddSysOpConfigInput, UpdateSysOpConfigInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Dto/SysOpConfigOutput.cs` | PageSysOpConfigOutput, SysOpConfigOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/Commands/SysTerminologyCommandHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/Dto/SysTerminologyInput.cs` | AddSysTerminologyInput, UpdateSysTerminologyInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/Dto/SysTerminologyOutput.cs` | PageSysTerminologyOutput, SysTerminologyOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Core/Entities/SysOpConfig.cs` | Status, GroupCode, Name, IsSysConfig, Value, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Core/Entities/SysTerminology.cs` | Name, Status, Lang, Remark, Code, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Infrastructure/Persistence/SeedData/SysOpConfigSeedData.cs` | HasData, SysOpConfigSeedData |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Core/Entities/FmsEquipmentTelemetry.cs` | EquipmentTypeId, CollectedTime, EquipmentId, Succeeded, EnvironmentMetric, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Core/Entities/FmsTopology.cs` | Name, Value, Remark, FmsTopology |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail.Core/Entities/RptEmailLog.cs` | ByteSize, Status, TriggerType, ScheduleId, Recipients, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail.Core/Entities/RptEmailSchedule.cs` | Frequency, LastRunKey, NextTimeRun, DayOfMonth, MinuteOfHour, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail.Core/Entities/RptEmailScheduleItem.cs` | Tram, Ca, RptEmailScheduleItem, Format, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Constants/TrafficPlanConst.cs` | TrafficPlanConst |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/Equipment/EquipmentMsgInput.cs` | EquipmentIds, GetEquipmentByIdsInput, GetEquipmentByTypeCodesInput, TypeCodes |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/Incident/TmsIncidentInput.cs` | UpdateTmsIncidentInput, AddTmsIncidentInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/IncidentAutomationConfig/IncidentAutomationConfigInput.cs` | UpdateIncidentAutomationConfigInput, AddIncidentAutomationConfigInput, DeleteIncidentAutomationConfigInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/IncidentAutomationRun/IncidentAutomationRunOutput.cs` | IncidentAutomationRunOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsDutySchedule.cs` | Status, Name, EndTime, TmsDutySchedule, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsDutyScheduleChangeRequest.cs` | RequesterId, TargetUserId, Remark, TargetScheduleId, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsDutyScheduleDetail.cs` | Reason, SwappedFromUserId, TmsDutyScheduleDetail, Remark, DutyScheduleId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsDutyScheduleHistory.cs` | RelatedScheduleId, ActionBy, NewData, Reason, UserAgent, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsEquipment.cs` | Config, Manufacturer, LaneId, EquipmentTypeId, ActiveDirection, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsEquipmentType.cs` | Remark, Size, Name, ActiveIcon, FaultIcon, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsEventData.cs` | ZoneId, Speed, Remark, PlateType, Severity, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsEventType.cs` | ModuleId, Url, Remark, TmsEventType, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsIncident.cs` | State, ProcessMode, Name, StartDate, ValidatedBy, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsIncidentAutomationConfig.cs` | TrafficPlanId, Status, EventTypeId, AutoVerifyIncident, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsIncidentAutomationRun.cs` | ConfigSnapshotJson, TrafficPlanId, AutoFinishAt, ErrorCode, ErrorMessage, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceAlert.cs` | AlertType, DismissReason, AcknowledgedBy, ScheduleId, DismissedBy, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceAlertRule.cs` | EquipmentTypeId, Remark, ErrorThreshold, AlertPriority, IsActive, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceGuide.cs` | Checklist, FrequencyValue, Manufacturer, Name, Frequency, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceHistory.cs` | Description, UserAgent, IpAddress, ActionType, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceScheduleEquipment.cs` | MaintenanceScheduleId, CheckedNote, EquipmentStateSnapshot, Remark, MaintenanceGuideId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMaintenanceTask.cs` | Result, Role, MaintenanceScheduleId, TaskName, StartTime, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMap.cs` | Url, Status, Mode, OrderNo, Width, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMapDetail.cs` | CoordY, MapLocationId, TmsMapDetail, RefType, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsMapLocation.cs` | Config, Status, TmsMapLocation, Remark, GeoJson, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsModule.cs` | KeyName, Status, Remark, Name, PluginId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsPatrolTeam.cs` | Description, Status, TmsPatrolTeam, Remark, Name |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsPatrolTeamMember.cs` | Remark, Role, TmsPatrolTeamMember, UserId, PatrolTeamId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsSignalLog.cs` | NewToDate, State, NewUrl, TimeToLive, OldToDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficAnalysis.cs` | SourceId, DetectTime, TmsTrafficAnalysis, Width, VehicleType, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficData.cs` | AlarmSpeed, PlateColor, VendorEventId, EventType, Length, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficJourney.cs` | ToEquipmentId, IsValid, SegmentId, Type, ToTime, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficPlanWorkDetail.cs` | ResetPolicy, DeviceCommandConfigId, RetryIntervalSeconds, ExecuteOrgId, DelayAfterPreviousSeconds, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficPlans.cs` | Name, PlanVersion, TmsTrafficPlans, EventTypeId, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficSegment.cs` | ToEquipmentId, MinTravelSeconds, MaxTravelSeconds, FromEquipmentId, OrderNo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsTrafficStatistic.cs` | TimeOccupy, SpaceMeanKmh, FlowVph, FromTime, IpAddress, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsVehicleFilter.cs` | Status, Remark, Alarm, TmsVehicleFilter, EndDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsVehicleFilterHistory.cs` | TmsVehicleFilterHistory, LaneId, LocationId, Type, EndDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsVehicleRegistration.cs` | OverallDimensions, Address, ChassisNumber, Seat, LicensePlate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWeather.cs` | Description, Temperature, TmsWeather, Type, ShortDescription, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkContact.cs` | Address, Info, Type, Status, RefId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkDetail.cs` | Status, Remark, TmsWorkDetail, ControlType, WorkType, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkDetailContact.cs` | TmsWorkDetailContacts, OrderNo, WorkDetailId, WorkContactId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkExecute.cs` | State, WorkDetailId, TmsWorkExecute, AutomationRunId, TrafficPlanId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkExecuteHistory.cs` | WorkName, TrafficPlanId, AutomationRunId, IncidentId, WorkMode, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsWorkUnit.cs` | Status, Address, TmsWorkUnit, Remark, Phone, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsZone.cs` | FromMetNumber, FromKmNumber, Remark, Type, ToKmNumber, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Entities/TmsZoneEquipment.cs` | Type, EquipmentId, Status, LaneId, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Exceptions/BaseMsg.cs` | BaseMsg |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Commands/DutyScheduleChangeRequestCommandHandler.cs` | scheduleId, command, replacerId, targetUserId, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Commands/DutyScheduleCommandHandler.cs` | status, command, GetStatusDisplay, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Commands/EquipmentCommandHandler.cs` | HandleAsync, EquipmentCommandHandler, HandleAsync, _tmsEquipRep, cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Dto/EquipmentInput.cs` | DeleteEquipmentInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Queries/EquipmentByTypeQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Queries/EquipmentQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Validators/EquipmentValidator.cs` | isCreate, DeleteEquipmentValidator.<init>, UpdateEquipmentValidator, AddEquipmentValidator, localizer, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentEventRule/Validators/EquipmentEventRuleValidator.cs` | AddEquipmentEventRuleValidator, UpdateEquipmentEventRuleValidator.<init>, isCreate, localizer, localizer, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentType/Commands/EquipmentTypeCommandHandler.cs` | HandleAsync, HandleAsync, command, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentType/Dto/EquipmentTypeInput.cs` | DeleteEquipmentTypesInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentType/Validators/EquipmentTypeValidator.cs` | DeleteEquipmentTypeValidator.<init>, localizer, localizer, AddEquipmentTypeValidator, UpdateEquipmentTypeValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EventType/Commands/EventTypeCommandHandler.cs` | uploadService, EventTypeCommandHandler, userManager, command, command, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EventType/Dto/EventTypeInput.cs` | DeleteEventTypeInput, UpdateEventTypeInput, AddEventTypeInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EventType/Validators/EventTypeValidator.cs` | UpdateEventTypeValidator.<init>, DeleteEventTypeValidator.<init>, localizer, UpdateEventTypeValidator, EventType.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Commands/IncidentCommandHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Dto/TmsIncidentInput.cs` | UpdateTmsIncidentInput, AddTmsIncidentInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Dto/TmsIncidentOutput.cs` | EventTypeName, PageTmsIncidentOutput, EventTypeCode, EventTypeCode, EventTypeName, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Validators/IncidentValidator.cs` | DeleteIncidentValidator.<init>, IncidentValidator.<init>, DeleteIncidentValidator, AddIncidentValidator.<init>, IncidentValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationConfig/Commands/IncidentAutomationConfigCommandHandler.cs` | HandleAsync, command, _configRep, cache, _cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationConfig/Validators/IncidentAutomationConfigValidator.cs` | DeleteIncidentAutomationConfigValidator, localizer, UpdateIncidentAutomationConfigValidator, IncidentAutomationConfigValidator.<init>, isCreate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationRun/Dto/IncidentAutomationRunInput.cs` | GetIncidentAutomationRunByIncidentInput, IncidentId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationRun/TmsIncidentAutomationRunController.cs` | GetLatestByIncident, input |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceAlertCommandHandler.cs` | HandleAsync, HandleAsync, command, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceAlertRuleCommandHandler.cs` | cache, guideRep, _ruleRep, command, MaintenanceAlertRuleCommandHandler.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Commands/MaintenanceGuideCommandHandler.cs` | command, uploadService, MaintenanceGuideCommandHandler.<init>, HandleAsync, command, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceAlertInput.cs` | ID, SuggestedGuideName, ActivateAlertRuleInput, EquipmentTypeName, MaintenanceAlertRuleOutput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceGuideInput.cs` | FilePath, DeleteMaintenanceGuideInput, FileName, UploadedAt, GuideDocumentInfo |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Map/Commands/MapCommandHandler.cs` | command, HandleAsync, HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Map/Validators/MapValidator.cs` | MapValidator, MapValidator.<init>, localizer, isCreate, AddMapValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Commands/MapDetailCommandHandler.cs` | command, HandleAsync, HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Dto/MapDetailsInput.cs` | AddTmsMapDetailInput, UpdateTmsMapDetailInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Validators/MapDetailsValidator.cs` | localizer, TmsMapDetailValidator.<init>, AddTmsMapDetailValidator.<init>, localizer, UpdateTmsMapDetailValidator.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/Commands/MapLocationCommandHandler.cs` | command, cache, userManager, tmsMapLocationRep, MapLocationCommandHandler.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/Dto/MapLocationsInput.cs` | UpdateTmsMapLocationInput, AddTmsMapLocationInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/Validators/MapLocationsValidator.cs` | AddTmsMapLocationValidator, localizer, TmsMapLocationValidator, UpdateTmsMapLocationValidator.<init>, localizer, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Module/Commands/ModuleCommandHandler.cs` | HandleAsync, _tmsModuleRep, HandleAsync, command, _cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Module/Dto/ModuleInput.cs` | AddTmsModuleInput, UpdateTmsModuleInput, DeleteTmsModuleInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Module/Validators/ModuleValidator.cs` | TmsModuleValidator, TmsModuleValidator.<init>, localizer, DeleteTmsModuleValidator.<init>, isCreate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/Commands/PatrolTeamCommandHandler.cs` | patrolTeamRep, _cache, PatrolTeamCommandHandler, command, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/Dto/PatrolTeamInput.cs` | DeletePatrolTeamInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogHistory/Dto/SignalLogHistoryOutput.cs` | SignalLogHistoryOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/Commands/SignalLogCommandHandler.cs` | HandleAsync, command, HandleAsync, HandleAsync, command, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/Dto/SignalLogsInput.cs` | DeleteTmsSignalLogInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/TmsSignalLogController.cs` | incident, GetIncident |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/Validators/SignalLogsValidator.cs` | UpdateTmsSignalLogValidator, AddTmsSignalLogValidator.<init>, UpdateTmsSignalLogValidator.<init>, localizer, AddTmsSignalLogValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficInfo/TmsTrafficInfoController.cs` | CheckIncident |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficMatrix/Validators/TrafficMatrixValidator.cs` | TrafficMatrixValidator.<init>, localizer, TrafficMatrixValidator |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/Commands/TrafficPlanCommandHandler.cs` | HandleAsync, _cache, userManager, command, TrafficPlanCommandHandler.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/Dto/TrafficPlanInput.cs` | AddTmsTrafficPlanInput, UpdateTmsTrafficPlanInput, DeleteTmsTrafficPlanInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/Validators/TrafficPlanValidator.cs` | localizer, DeleteTmsTrafficPlanValidator, TmsTrafficPlanValidator.<init>, AddTmsTrafficPlanValidator.<init>, DeleteTmsTrafficPlanValidator.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Commands/TrafficPlanWorkDetailCommandHandler.cs` | command, HandleAsync, TmsTrafficPlanWorkDetailRep, trafficPlanWorkDetailService, _cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Dto/TrafficPlanWorkDetailInput.cs` | UpdateTmsTrafficPlanWorkDetailInput, AddTmsTrafficPlanWorkDetailInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Validators/TrafficPlanWorkDetailValidator.cs` | DeleteTmsTrafficPlanWorkDetailValidator.<init>, localizer, localizer, localizer, isCreate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficStatistics/Commands/TrafficStatisticCommandHandler.cs` | tmsTrafficStatisticRep, cache, _cache, HandleAsync, command, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficStatistics/Dto/TrafficStatisticInput.cs` | DeleteTmsTrafficStatisticInput, UpdateTmsTrafficStatisticInput, AddTmsTrafficStatisticInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficStatistics/Validators/TrafficStatisticValidator.cs` | DeleteTmsTrafficStatisticValidator, localizer, UpdateTmsTrafficStatisticValidator.<init>, DeleteTmsTrafficStatisticValidator.<init>, TmsTrafficStatisticValidator.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Commands/TmsVehicleFilterCommandHandler.cs` | HandleAsync, TmsVehicleFilterCommandHandler.<init>, TmsVehicleFilterCommandHandler, cache, command, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Dto/TmsVehicleFilterInput.cs` | UpdateTmsVehicleFilterInput, DeleteTmsVehicleFilterInput, AddTmsVehicleFilterInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Validators/VehicleFilterValidator.cs` | UpdateVehicleFilterValidator.<init>, isCreate, lz, localizer, DeleteVehicleFilterValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Commands/TmsVehicleFilterHistoryCommandHandler.cs` | DuplicateWindowSeconds, HandleAsync, HandleAsync, TmsVehicleFilterHistoryCommandHandler.<init>, licensePlate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Dto/TmsVehicleFilterHistoryInput.cs` | UpdateTmsVehicleFilterHistoryInput, AddTmsVehicleFilterHistoryInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Commands/TmsVehicleRegistrationCommandHandler.cs` | command, _cacheName, HandleAsync, TmsVehicleRegistrationCommandHandler.<init>, _cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Dto/TmsVehicleRegistrationInput.cs` | AddTmsVehicleRegistrationInput, UpdateTmsVehicleRegistrationInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Validators/VehicleRegistrationValidator.cs` | DeleteVehicleRegistrationValidator.<init>, VehicleRegistrationValidator.<init>, VehicleRegistrationValidator, lz, AddVehicleRegistrationValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Commands/TmsWeatherCommandHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Commands/TmsWorkContactCommandHandler.cs` | _workContactRepo, command, HandleAsync, _cache, TmsWorkContactCommandHandler.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Dto/TmsWorkContactInput.cs` | TmsWorkContactPara, AddTmsWorkContactInput, Code, DeleteTmsWorkContactInput, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Queries/TmsWorkContactQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Validators/WorkContactValidator.cs` | lz, AddWorkContactValidator.<init>, WorkContactValidator, DeleteWorkContactValidator.<init>, AddWorkContactValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Commands/WorkDetailCommandHandler.cs` | HandleAsync, HandleAsync, command, cache, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Dto/WorkDetailInput.cs` | AddWorkDetailInput, UpdateWorkDetailInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Validators/WorkDetailValidator.cs` | WorkDetailValidator.<init>, localizer, localizer, localizer, AddWorkDetailValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Commands/WorkDetailContactCommandHandler.cs` | HandleAsync, command, HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Dto/TmsWorkDetailContactInput.cs` | AddTmsWorkDetailContactInput, UpdateTmsWorkDetailContactInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Validators/WorkDetailContactValidator.cs` | AddWorkDetailContactValidator, UpdateWorkDetailContactValidator.<init>, DeleteWorkDetailContactValidator.<init>, isCreate, UpdateWorkDetailContactValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Commands/TmsWorkUnitCommandHandler.cs` | workUnitRepo, _messBus, command, _workUnitRepo, TmsWorkUnitCommandHandler, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Dto/TmsWorkUnitInput.cs` | AddTmsWorkUnitInput, DeleteTmsWorkUnitInput, UpdateTmsWorkUnitInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Validators/WorkUnitValidator.cs` | lz, WorkUnitValidator, DeleteWorkUnitValidator.<init>, UpdateWorkUnitValidator.<init>, DeleteWorkUnitValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Commands/ZoneCommandHandler.cs` | HandleAsync, command, command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Dto/ZoneInput.cs` | UpdateZoneInput, AddZoneInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Dto/ZoneOutput.cs` | PageZoneOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/Validators/ZoneValidator.cs` | isCreate, UpdateZoneValidator, UpdateZoneValidator.<init>, localizer, DeleteZoneValidator, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Commands/ZoneEquipmentCommandHandler.cs` | _cache, command, HandleAsync, HandleAsync, command, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Dto/AddZoneEquipmentInput.cs` | AddZoneEquipmentInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Dto/ConfirmZoneEquipmentMappingInput.cs` | ClearExistingMappings, ConfirmZoneEquipmentMappingInput, Mappings, ZoneId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Dto/DeleteZoneEquipmentInput.cs` | DeleteZoneEquipmentInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/Dto/UpdateZoneEquipmentInput.cs` | UpdateZoneEquipmentInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsDutySchedule.cs` | StartTime, Status, EndTime, Remark, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsDutyScheduleChangeRequest.cs` | ReplacerId, ApprovalNote, Type, Reason, DutyScheduleId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsDutyScheduleDetail.cs` | SwappedFromUserId, Reason, DutyScheduleId, UserId, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsDutyScheduleHistory.cs` | OldData, RelatedScheduleId, Reason, IpAddress, ChangeRequestId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsIncident.cs` | FinishedBy, InjuredNumber, Metadata, ApprovedBy, FinishedDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceAlert.cs` | EquipmentId, CreatedByUserId, AcknowledgedTime, Remark, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceAlertRule.cs` | Remark, EquipmentTypeId, PeriodDays, IsActive, Manufacturer, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceGuide.cs` | Remark, Documents, Priority, FrequencyValue, Manufacturer, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceHistory.cs` | ActionType, Remark, MaintenanceScheduleId, AffectedUserId, UserAgent, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceScheduleEquipment.cs` | EquipmentTypeNameSnapshot, Status, CompletedBy, CompletedDate, CheckedBy, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMaintenanceTask.cs` | Remark, Status, StartTime, MaintenanceScheduleId, Result, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsMapDetail.cs` | Name, Remark, CoordX, CoordY, Coordinates, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsPatrolTeam.cs` | Remark, Name, Description, Status |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsPatrolTeamMember.cs` | Role, UserId, PatrolTeamId, Remark |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsSignalLog.cs` | EquipmentId, OldUrl, ExecuteTime, Remark, OldFromDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficAnalysis.cs` | Location, DetectTime, MoveType, Width, Direction, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficData.cs` | LicensePlate, Direction, Lane, AlarmWidth, ZoneId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficPlanWorkDetail.cs` | ExecuteOrgId, WorkDetailId, TrafficPlanId, State, OrderNo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficPlans.cs` | Status, EventTypeId, Name, Remark |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsTrafficStatistic.cs` | FromTime, SpaceOccupy, ToTime, LocationDetail, TimeOccupy, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWeather.cs` | WindSpeed, Foresight, LocationDetail, ShortDescription, WindDirection, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWorkDetail.cs` | WorkType, WorkMode, Name, Status, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWorkDetailContact.cs` | WorkContactId, WorkDetailId, OrderNo |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWorkExecute.cs` | TrafficPlanId, Step, WorkMode, State, ExecuteOrg, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsWorkExecuteHistory.cs` | TrafficPlanId, ExecuteOrg, IncidentId, WorkMode, ExecuteBy, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/TmsZoneEquipment.cs` | ZoneId, Type, EquipmentId, LaneId, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsEquipment.cs` | OrderNo, Manufacturer, MetNumber, EquipmentVisible, EquipmentState, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsEquipmentType.cs` | Name, Status, Size, ActiveIcon, ModuleId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsEventType.cs` | Name, Remark, Priority, WarningType, ModuleId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsMap.cs` | Width, Name, Url, Height, Type, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsMapLocation.cs` | Status, MapId, GeoJson, Remark, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsModule.cs` | KeyName, Name, Status, PluginId, Remark |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsVehicleFilter.cs` | Status, Alarm, Remark, Type, LicensePlate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsVehicleFilterHistory.cs` | LaneId, Type, Direction, LicensePlate, EndDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsVehicleRegistration.cs` | LicensePlate, Seat, ChassisNumber, YearManufacture, InspectionStamp, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsWorkContact.cs` | Type, RefId, Address, Remark, Info, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsWorkUnit.cs` | Status, Info, Type, Address, Phone, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Core/Entities/tmsZone.cs` | FromKmNumber, Location, Children, Pid, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/IncidentAutomation/TmsIncidentAutomationService.cs` | GetLatestByIncidentAsync, incidentId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/SignalLog/SignalLogService.cs` | GetIncident, incidentId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TrafficInfo/TrafficInfoService.cs` | Incident |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/TrafficPlanWorkDetail/TrafficPlanWorkDetailService.cs` | deleteTrafficPlanWorkDetail, _hostingEnvironment, TmsTrafficPlanWorkDetail, _TmsTrafficPlanWorkDetail, TrafficPlanWorkDetailService.<init>, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollLane.cs` | Note, Direction, LaneBE, LaneId, TollLane, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollPriceList.cs` | TicketTypeId, PriceTypeId, SyncTime, PriceListDetail, Code, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollPriceListDetail.cs` | VehicleTypeSourceId, RoadSourceId, PriceListSourceId, TollPriceListDetail, SyncTime, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollRoad.cs` | Active, StationIdIn, StationIdOut, Name, Km, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollShift.cs` | LastSyncTime, ShiftId, ShiftBE, Note, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollStation.cs` | TollStation, Note, Type, LastSyncTime, StationType, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollSyncLog.cs` | DataType, CursorTo, EndTime, Status, FromDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollTicketOutSummary.cs` | StartTime, ProcessDate, AmountEdit, TicketTypeId, AmountSum, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollTransactionIn.cs` | OperatorName, TicketId, PlateEdit, SyncTime, LaneId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Core/Entities/TollTransactionOut.cs` | PlateEdit, ImageLane, TransactionDateTime, StationId, ShiftCode, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Message/VmsMessageInput.cs` | AddVmsMessageInput, UpdateVmsMessageInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsEquipmentTelemetry.cs` | EquipmentId, EquipmentName, Succeeded, Source, Vendor, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsEventDefault.cs` | VmsEventDefault, BackColor, Name, EquipmentTypeId, Color, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Entities/VmsMessage.cs` | OrderNo, VmsMessage, Type, Name, Content2, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Message/Commands/VmsMessageCommandHandler.cs` | command, command, HandleAsync, HandleAsync, cache, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Process/Commands/ScheduleCommandHandler.cs` | equipmentList |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Schedule/VwScheduleOutput.cs` | VwDeleteScheduleOutput, VwAddScheduleOutput, VwUpdateScheduleOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/SlotPort/VwSlotPortOutput.cs` | VwDeleteSlotPortOutput, VwUpdateSlotPortOutput, VwAddSlotPortOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwControllerSlot.cs` | CardType, SlotsType, Remark, CardModel, PortNumber, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwSchedule.cs` | NextRun, Weekdays, Status, ScheduleType, OnceDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwSlotPort.cs` | Name, ConnectedSourcecId, Status, Resolution, PortNo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Entities/VwWallTopology.cs` | ScreenHeight, VwWallTopology, Rows, Name, ScreenWidth, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/Validators/VwScheduleValidator.cs` | VwDeleteScheduleValidator |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/SlotPort/Validators/VwSlotPortValidator.cs` | VwDeleteSlotPortValidator |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceList/Dto/WpPriceListOutput.cs` | WpUpdateStatusPriceListOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Toll/Dto/WpTollOutput.cs` | TollDetailOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/WpConfig/Commands/ConfigCommandHandler.cs` | input, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/WpConfig/Dto/WpConfigOutput.cs` | WpConfigUpdateOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpConfig.cs` | Remark, WpConfig, Code, Value, OrderNo, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpMenu.cs` | Children, Remark, Path, IsAffix, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpPriceList.cs` | WpPriceList, ToDate, Remark, CurrencyUnit, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpPriceListDetail.cs` | ExitStation, VehicleType, WpPriceListDetail, Price, EntryStation, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Core/Entities/WpToll.cs` | Name, Remark, Telephone, Info, RefId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Common/Persistence/VdsDiagnosticsRepository.cs` | CountRowsAsync, TrafficAnalysis, TrafficData, TrafficStatistic, VdsRowCounts, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/Events/Persistence/EventDataRepository.cs` | entity, UpdateAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/Persistence/VehicleFilterRepository.cs` | QueryFiltersAsync, ct |
| `TA-ITS015-WEBAPI-V1.0/src/Services/VDS/ITS.VDS.Core/Features/VehicleFilter/VehicleFilterManager.cs` | FromDatabaseAsync, ct |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Constants/Application/CacheConst.cs` | KeyConst, KeyOpenAccess, KeyBaseApi, KeyUserButton, KeyPhoneVerCode, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Constants/Application/EntityConst.cs` | Length1024, Length512, Length128, EntityConst, RemarkFieldLength, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Enums/BaseEnums.cs` | Success, Fail, SuccessEnums |
| `tests/VideoWall/Controllers/VwScheduleTests.cs` | VwScheduleCommand_BatchDeleteVwSchedule_SoftDeletesRecords_Test, VwScheduleCommand_DeleteVwSchedule_SoftDeletesRecord_Test |
| `tests/VideoWall/Controllers/VwSlotPortTests.cs` | VwSlotPortCommand_BatchDeleteVwSlotPort_SoftDeletesRecords_Test, VwSlotPortCommand_DeleteVwSlotPort_SoftDeletesRecord_Test |

## Entry Points

- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Validators/IncidentValidator.cs::IncidentValidator.<init>`

## Connected Communities

- **DutySchedule/Dto +7 dirs** (2 cross-edges)
- **Category/Dto +14 dirs** (1 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-301")
explore(operation:"context", task:"understand Modules.TMS.Core/Entities +121 dirs", format:"gcx")
relations(operation:"usages", target:{symbol:"TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Validators/IncidentValidator.cs::IncidentValidator.<init>"}, format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
