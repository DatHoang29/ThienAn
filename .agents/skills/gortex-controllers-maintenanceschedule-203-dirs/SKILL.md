---
name: gortex-controllers-maintenanceschedule-203-dirs
description: "Work in the Controllers/MaintenanceSchedule +203 dirs area — 1441 symbols across 242 files (82% cohesion)"
---

# Controllers/MaintenanceSchedule +203 dirs

1441 symbols | 242 files | 82% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/DashboardController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/Dto/DashboardInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/DashboardItem/DashboardItemController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/CctvDeviceController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Dto/CctvDevicesInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/CctvPtzController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Dto/CctvPtzOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Template/TemplateController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/ConfigData/SysConfigDataInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/ConfigData/SysConfigDataOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/ConfigType/SysConfigTypeInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/ConfigType/SysConfigTypeOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/OpConfig/SysOpConfigInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/OpConfig/SysOpConfigOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/Terminology/SysTerminologyInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/Terminology/SysTerminologyOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/ConfigData/Queries/SysConfigDataQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/ConfigData/SysConfigDataController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/ConfigType/Queries/SysConfigTypeQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/ConfigType/SysConfigTypeController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Queries/SysOpConfigQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/SysOpConfigController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Share/ShareController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/Queries/SysTerminologyQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/SysTerminologyController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS.Report/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS.Report/Controllers/EquipmentHistory/FmsEquipmentHistoryReportController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentHistory/FmsEquipmentHistoryController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentTelemetry/Dto/EquipmentTelemetryInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentTelemetry/Dto/EquipmentTelemetryOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentTelemetry/FmsEquipmentTelemetryController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentTelemetry/Queries/EquipmentTelemetryQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/Topology/Dto/TopologyInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/Topology/FmsTopologyController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail/Controllers/EmailSchedule/RptEmailScheduleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail/Controllers/TestMail/TestMailController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report.Core/Dto/InfoConfig/RptInfoConfigInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report.Core/Dto/InfoConfig/RptInfoConfigOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report/Controllers/InfoConfig/Queries/RptInfoConfigQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report/Controllers/InfoConfig/RptInfoConfigController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Sample.Report/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Sample.Report/Controllers/CategoryFastReport/SampCategoryFReportController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Sample.Report/Controllers/CatergoryCarboneReport/SampCategoryCReportController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Samplev2/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Samplev2/Controllers/Category/SampCategoryController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Samplev2/Infrastructure/Services/Category/SampCategoryService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/PacketField/ShareDataPacketFieldInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/PacketField/ShareDataPacketFieldOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/Partner/ShareDataPartnerInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/Partner/ShareDataPartnerOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/Subscription/ShareDataSubscriptionOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/AlertLog/ShareDataAlertLogController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/CodeSet/ShareDataCodeSetController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Inbound/ShareDataInboundController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Mapping/ShareDataMappingController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Packet/ShareDataPacketController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/PacketField/Queries/ShareDataPacketFieldQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/PacketField/ShareDataPacketFieldController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Partner/Queries/ShareDataPartnerQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Partner/ShareDataPartnerController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Subscription/ShareDataSubscriptionController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/Incident/TmsIncidentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/Incident/TmsIncidentOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/WorkExecute/TmsWorkExecuteInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/EventData/TmsEventDataController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/Incident/TmsIncidentReportController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/TrafficAnalysis/Dto/TrafficAnalysisOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/TrafficAnalysis/TmsTrafficAnalysisController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/TrafficData/TmsTrafficDataReportController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/TrafficStatistic/TmsTrafficStatisticReportController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Dto/DutyScheduleChangeRequestInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Dto/DutyScheduleInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/TmsDutyScheduleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Dto/EquipmentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/TmsEquipmentController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentEventRule/TmsEquipmentEventRuleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentType/TmsEquipmentTypeController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EventType/TmsEventTypeController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Queries/IncidentQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Queries/IncidentWorkflowQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/TmsIncidentController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationConfig/TmsIncidentAutomationConfigController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceAlertInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceGuideInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceScheduleInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceScheduleOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceScheduleQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceAlertController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceAlertRuleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceGuideController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceScheduleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Map/Dto/MapInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Map/TmsMapController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Commands/MapDetailCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Dto/MapDetailsInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/TmsMapDetailController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/Dto/MapLocationsInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/TmsMapLoacationController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Module/TmsModuleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/Dto/PatrolTeamMemberInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/TmsPatrolTeamController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogHistory/TmsSignalLogHistoryController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/Dto/SignalLogsInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/TmsSignalLogController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficInfo/TmsTrafficInfoController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/TmsTrafficPlanController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Dto/TrafficPlanWorkDetailInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Dto/TrafficPlanWorkDetailOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Queries/TrafficPlanWorkDetailQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/TmsTrafficPlanWorkDetailController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficStatistics/TmsTrafficStatisticController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Dto/TmsVehicleFilterInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Dto/TmsVehicleFilterOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Queries/TmsVehicleFilterQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/TmsVehicleFilterController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Dto/TmsVehicleFilterHistoryInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Dto/TmsVehicleFilterHistoryOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Queries/TmsVehicleFilterHistoryQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/TmsVehicleFilterHistoryController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Dto/TmsVehicleRegistrationInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Dto/TmsVehicleRegistrationOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Queries/TmsVehicleRegistrationQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/TmsVehicleRegistrationController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Commands/TmsWeatherCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Dto/TmsWeatherInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Dto/TmsWeatherOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Queries/TmsWeatherQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/TmsWeatherController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Dto/TmsWorkContactInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Dto/TmsWorkContactOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Queries/TmsWorkContactQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/TmsWorkContactController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Commands/WorkDetailCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Dto/WorkDetailInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/TmsWorkDetailController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Dto/TmsWorkDetailContactInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Dto/TmsWorkDetailContactOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Queries/WorkDetailContactQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/TmsWorkDetailContactController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkExecute/TmsWorkExecuteController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Dto/TmsWorkUnitInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Dto/TmsWorkUnitOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Queries/TmsWorkUnitQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/TmsWorkUnitController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/TmsZoneController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/TmsZoneEquipmentController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/MapLocation/MapLocationService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/SignalLog/SignalLogService.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoNam/DTTNamController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoNgay/DTTNController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoQuy/DTTQController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoThang/DTTTController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoThoiGian/DTTTGController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoCa/LLTCController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoLoTrinh/LLTLTController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoNam/LLTNamController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoNgay/LLTNController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoThang/LLTTController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Current/VmsCurrentInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Current/VmsCurrentOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/EventDefault/VmsEventDefaultInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/EventDefault/VmsEventDefaultOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ImageVms/VmsImageInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ImageVms/VmsImageOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Message/VmsMessageInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ProcessImg/VmsProcessImgInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ProcessImg/VmsProcessImgOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Template/TemplateInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Template/TemplateOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Control/VmsControlController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/Queries/CurrentQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/Queries/DefaultQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/VmsCurrentController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/EquipmentTelemetry/EquipmentTelemetryController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/EventDefault/Queries/EventDefaultQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/EventDefault/VmsEventDefaultController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ImageVms/Queries/VmsImageQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ImageVms/VmsImageController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Message/VmsMessageController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ProcessImg/Queries/ProcessImgQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ProcessImg/VmsProcessImgController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Template/Queries/TemplateQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Template/VmsTemplateController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Controller/VwControllerInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Controller/VwControllerOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/EventRule/VwEventRuleInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/EventRule/VwEventRuleOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Scene/VwSceneInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Scene/VwSceneOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Schedule/VwScheduleInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Schedule/VwScheduleOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Screen/VwScreenOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/SlotPort/VwSlotPortInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/SlotPort/VwSlotPortOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Source/VwSourceOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/WindowScene/VwWindowSceneInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/WindowScene/VwWindowSceneOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Controller/VwControllerController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/EventRule/Queries/VwEventRuleQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/EventRule/VwEventRuleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/Queries/VwSceneQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/VwSceneController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/Queries/VwScheduleQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/VwScheduleController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Screen/VwScreenController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/SlotPort/VwSlotPortController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Source/VwSourceController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/WindowScene/Queries/VwWindowSceneQueryHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/WindowScene/VwWindowSceneController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP.Report/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP.Report/Controllers/Statistical/WpStatisticalReportController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/BaseController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Category/Dto/WPCategoryInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Category/WPCategoryControllers.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Menu/Dto/WpMenuInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Menu/WpMenuController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Post/Dto/PostInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Post/WPPostControllers.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceList/WpPriceListControllers.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceListDetail/Commands/PriceListDetailCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceListDetail/Dto/WpPriceListDetailInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceListDetail/WpPriceListDetailControllers.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Toll/Commands/TollCommandHandler.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Toll/Dto/WpTollInput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Toll/WpTollControllers.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/TrafficLaw/Dto/WpTrafficLawOutput.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/TrafficLaw/WPTrafficLawControllers.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/WpConfig/WpConfigControllers.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Enums/BaseEnums.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Apis/Tms/ISignalLogApi.cs`
- `tests/VideoWall/Controllers/VwControllerTests.cs`
- `tests/VideoWall/Controllers/VwSlotPortTests.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/BaseController.cs` | BaseController, GroupName, BasePath |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/DashboardController.cs` | AddDashboard, DeleteDashboard, input, input, UpdateDashboard, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Dashboard/Dto/DashboardInput.cs` | AddDashboardInput, ID, DeleteDashboardInput, UpdateDashboardInput, Tags |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/DashboardItem/DashboardItemController.cs` | input, DashboardItemController, input, DeleteDashboardItem, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/CctvDeviceController.cs` | DeleteDevice, input, SyncDevice, input, CctvDeviceController, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Devices/Dto/CctvDevicesInput.cs` | CctvGetDeviceInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/CctvPtzController.cs` | input, FocusPtz, DeletePreset, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/PTZ/Dto/CctvPtzOutput.cs` | Message, CctvActionOutput, Success |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CCTV/Module.CCTV/Controllers/Template/TemplateController.cs` | input, DeleteTemplate |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/ConfigData/SysConfigDataInput.cs` | AddSysConfigDataInput, ConfigTypeCode, Value, Name, Code, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/ConfigData/SysConfigDataOutput.cs` | PageSysConfigDataOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/ConfigType/SysConfigTypeInput.cs` | DeleteSysConfigTypeInput, AddSysConfigTypeInput, Description, UpdateSysConfigTypeInput, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/ConfigType/SysConfigTypeOutput.cs` | PageSysConfigTypeOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/OpConfig/SysOpConfigInput.cs` | Name, PageSysOpConfigInput, GroupCode, UpdateSysOpConfigInput, DeleteSysOpConfigInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/OpConfig/SysOpConfigOutput.cs` | PageSysOpConfigOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/Terminology/SysTerminologyInput.cs` | Language, AddSysTerminologyInput, Name, Value, DeleteSysTerminologyInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem.Core/Dto/Terminology/SysTerminologyOutput.cs` | PageSysTerminologyOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/BaseController.cs` | BasePath, GroupName, BaseController |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/ConfigData/Queries/SysConfigDataQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/ConfigData/SysConfigDataController.cs` | input, input, Page, input, UpdateSysConfigData, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/ConfigType/Queries/SysConfigTypeQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/ConfigType/SysConfigTypeController.cs` | input, UpdateSysConfigType, Page, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Queries/SysOpConfigQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/SysOpConfigController.cs` | input, SysOpConfigController, GetAllOpConfigList, input, BatchDeleteSysOpConfig, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Share/ShareController.cs` | input, ShareController, DisplayName, Get |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/Queries/SysTerminologyQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Terminology/SysTerminologyController.cs` | Page, input, input, BatchDeleteSysTerminology, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS.Report/Controllers/BaseController.cs` | GroupName, BasePath, BaseController |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS.Report/Controllers/EquipmentHistory/FmsEquipmentHistoryReportController.cs` | request, ExportReport, FmsEquipmentHistoryReportController, request, RenderReport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentHistory/FmsEquipmentHistoryController.cs` | HttpPost |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentTelemetry/Dto/EquipmentTelemetryInput.cs` | Source, PageEquipmentTelemetryInput, EquipmentCode, EquipmentId, FromDate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentTelemetry/Dto/EquipmentTelemetryOutput.cs` | PageEquipmentTelemetryOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentTelemetry/FmsEquipmentTelemetryController.cs` | FmsEquipmentTelemetryController, input, Page, input, Delete |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/EquipmentTelemetry/Queries/EquipmentTelemetryQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/Topology/Dto/TopologyInput.cs` | SyncTopologyInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/FMS/Modules.FMS/Controllers/Topology/FmsTopologyController.cs` | SyncTopology, input, TmsTopologyController |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail/Controllers/EmailSchedule/RptEmailScheduleController.cs` | _sendService, DeleteRptEmailSchedule, UpdateRptEmailSchedule, input, Page, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Mail/Module.Mail/Controllers/TestMail/TestMailController.cs` | GetConfig |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report.Core/Dto/InfoConfig/RptInfoConfigInput.cs` | PageRptInfoConfigInput, Name, Code, Value, ReportCode, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report.Core/Dto/InfoConfig/RptInfoConfigOutput.cs` | PageRptInfoConfigOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report/Controllers/BaseController.cs` | BasePath, BaseController, GroupName |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report/Controllers/InfoConfig/Queries/RptInfoConfigQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Report/Modules.Report/Controllers/InfoConfig/RptInfoConfigController.cs` | AddRptInfoConfig, input, RptInfoConfigController, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Sample.Report/Controllers/BaseController.cs` | BasePath, BaseController, GroupName |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Sample.Report/Controllers/CategoryFastReport/SampCategoryFReportController.cs` | ExportReport, RenderReport, request, SampCategoryFReportController, request |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Sample.Report/Controllers/CatergoryCarboneReport/SampCategoryCReportController.cs` | RenderReport, request, SampCategoryCReportController, request, ExportReport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Samplev2/Controllers/BaseController.cs` | GroupName, BasePath, BaseController |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Samplev2/Controllers/Category/SampCategoryController.cs` | SampCategoryController.<init>, UpdateCategory, AddCategory, Demo, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Samplev2/Modules.Samplev2/Infrastructure/Services/Category/SampCategoryService.cs` | SampCategoryService, SampCategoryService.<init>, Calculate |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/PacketField/ShareDataPacketFieldInput.cs` | ShareDataIdPacketFieldInput, Code, AliasFieldKey, ShareDataAddPacketFieldInput, ShareDataPacketFieldInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/PacketField/ShareDataPacketFieldOutput.cs` | ShareDataDeletePacketFieldOutput, ShareDataPacketFieldOutput, ShareDataAddPacketFieldOutput, ShareDataUpdatePacketFieldOutput, ShareDataPagePacketFieldOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/Partner/ShareDataPartnerInput.cs` | ShareDataPagePartnerInput, Status, Name, SessionState, Code, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/Partner/ShareDataPartnerOutput.cs` | ShareDataPagePartnerOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/Subscription/ShareDataSubscriptionOutput.cs` | ShareDataPageSubscriptionOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/AlertLog/ShareDataAlertLogController.cs` | AcknowledgeAll, input, Acknowledge, input |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/BaseController.cs` | BaseController, GroupName, BasePath |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/CodeSet/ShareDataCodeSetController.cs` | DeleteShareDataCodeSet, input, BatchDeleteShareDataCodeSet, input |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Inbound/ShareDataInboundController.cs` | input, AllowAnonymous, AddShareDataInbound, ShareDataInboundController |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Mapping/ShareDataMappingController.cs` | input, BatchDeleteShareDataMapping, DeleteShareDataMapping, input |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Packet/ShareDataPacketController.cs` | input, input, DeleteShareDataPacket, BatchDeleteShareDataPacket |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/PacketField/Queries/ShareDataPacketFieldQueryHandler.cs` | command, command, command, HandleAsync, HandleAsync, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/PacketField/ShareDataPacketFieldController.cs` | input, DeleteShareDataPacketField, GetById, Page, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Partner/Queries/ShareDataPartnerQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Partner/ShareDataPartnerController.cs` | Page, input, BatchDeleteShareDataPartner, input, DisconnectShareDataPartner, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Controllers/Subscription/ShareDataSubscriptionController.cs` | input, input, input, ApproveShareDataSubscription, Page, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/Incident/TmsIncidentInput.cs` | Actor, Code, TrafficPlanId, TmsIncidentSingleInput, TmsIncidentTrafficPlanInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/Incident/TmsIncidentOutput.cs` | TmsIncidentOutput, EventTypeName, EventTypeCode |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Core/Dto/WorkExecute/TmsWorkExecuteInput.cs` | State, Actor, Remark, Id, TmsWorkExecuteStateInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/BaseController.cs` | BaseController, BasePath, GroupName |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/EventData/TmsEventDataController.cs` | TmsEventDataController |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/Incident/TmsIncidentReportController.cs` | ExportReport, TmsIncidentReportController, RenderReport, request, request, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/TrafficAnalysis/Dto/TrafficAnalysisOutput.cs` | PageTrafficAnalysisOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/TrafficAnalysis/TmsTrafficAnalysisController.cs` | input, TmsTrafficAnalysisController, Page |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/TrafficData/TmsTrafficDataReportController.cs` | ExportReport, RenderReport, RenderReport, ExportReport, request, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS.Report/Controllers/TrafficStatistic/TmsTrafficStatisticReportController.cs` | ExportReport, TmsTrafficStatisticReportController, request, RenderReport, request, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/BaseController.cs` | BaseController, GroupName, BasePath |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Dto/DutyScheduleChangeRequestInput.cs` | ChangeRequestId, CancelChangeRequestInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/Dto/DutyScheduleInput.cs` | Reason, DetailId, UpdateUserRoleInput, Role |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/DutySchedule/TmsDutyScheduleController.cs` | ApproveChangeRequest, input, input, UpdateStatus, Update, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/Dto/EquipmentInput.cs` | AddEquipmentInput, UpdateEquipmentInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Equipment/TmsEquipmentController.cs` | input, input, input, UpdateTmsEquipmentVisible, UpdateTmsEquipmentInvisible, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentEventRule/TmsEquipmentEventRuleController.cs` | BatchDeleteTmsEquipmentEventRule, input, input, TmsEquipmentEventRuleController, UpdateTmsEquipmentEventRule, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EquipmentType/TmsEquipmentTypeController.cs` | TmsEquipmentTypeController, DeleteTmsEquipmentType, BatchDeleteTmsEquipmentType, AddTmsEquipmentType, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/EventType/TmsEventTypeController.cs` | input, input, TmsEventTypeController.<init>, DeleteTmsEventType, UpdateTmsEventType, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Queries/IncidentQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/Queries/IncidentWorkflowQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Incident/TmsIncidentController.cs` | UpdateIncidentStateToRegistration, input, input, CheckIncident, DeleteTmsIncident, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/IncidentAutomationConfig/TmsIncidentAutomationConfigController.cs` | input, TmsIncidentAutomationConfigController.<init>, DeleteTmsIncidentAutomationConfig, input, AddTmsIncidentAutomationConfig, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceAlertInput.cs` | ID, ID, DismissAlertInput, AcknowledgeAlertInput, Remark, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceGuideInput.cs` | File, UploadMaintenanceGuideDocumentInput, MaintenanceGuideId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceScheduleInput.cs` | ActionType, ToDate, ActionType, ToDate, MaintenanceScheduleId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Dto/MaintenanceScheduleOutput.cs` | AffectedEquipmentName, ActionByName, AffectedUserName, MaintenanceScheduleName, ActionTypeDisplay, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/Queries/MaintenanceScheduleQueryHandler.cs` | HandleAsync, HandleAsync, query, query |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceAlertController.cs` | input, TmsMaintenanceAlertController, input, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceAlertRuleController.cs` | Activate, TmsMaintenanceAlertRuleController, input, Deactivate, GetList, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceGuideController.cs` | input, GetList, input, Update, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MaintenanceSchedule/TmsMaintenanceScheduleController.cs` | UpdateMaintenanceTaskStatus, PageMaintenanceHistory, input, input, GetList, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Map/Dto/MapInput.cs` | DeleteMapInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Map/TmsMapController.cs` | input, input, TmsMapController, AddMap, BatchDeleteMap, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Commands/MapDetailCommandHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/Dto/MapDetailsInput.cs` | DeleteTmsMapDetailInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapDetails/TmsMapDetailController.cs` | input, input, UpdateTmsMapDetail, input, AddTmsMapDetail, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/Dto/MapLocationsInput.cs` | DeleteTmsMapLocationInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/MapLocations/TmsMapLoacationController.cs` | sv, input, input, BatchDeleteTmsMapLocation, TmsMapLocationController, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Module/TmsModuleController.cs` | AddTmsModule, input, input, input, TmsModulesController, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/Dto/PatrolTeamMemberInput.cs` | Role, UpdateMemberRoleInput, MemberId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/PatrolTeam/TmsPatrolTeamController.cs` | input, input, Add, input, BatchDelete, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogHistory/TmsSignalLogHistoryController.cs` | input, TmsSignalLogHistoryController, Page |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/Dto/SignalLogsInput.cs` | timetolive, SignalLog, incidentId, OldUrl, UpdateTmsSignalLogInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/SignalLogs/TmsSignalLogController.cs` | env, BatchDeleteTmsSignalLog, UpdateTmsSignalLog, request, DeleteTmsSignalLog, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficInfo/TmsTrafficInfoController.cs` | trafficInfoService, _trafficInfoService, TmsTrafficInfoController, TmsTrafficInfoController.<init>, equipmentId, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlan/TmsTrafficPlanController.cs` | TmsTrafficPlanController.<init>, AddTmsTrafficPlan, BatchDeleteTmsTrafficPlan, input, TmsTrafficPlanController, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Dto/TrafficPlanWorkDetailInput.cs` | DeleteTmsTrafficPlanWorkDetailInput, Step, TrafficPlanId, WorkDetailId, PageTmsTrafficPlanWorkDetailInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Dto/TrafficPlanWorkDetailOutput.cs` | PageTmsTrafficPlanWorkDetailOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/Queries/TrafficPlanWorkDetailQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficPlanWorkDetail/TmsTrafficPlanWorkDetailController.cs` | input, input, id, TmsTrafficPlanWorkDetailController, PageByID, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/TrafficStatistics/TmsTrafficStatisticController.cs` | UpdatetmsTrafficStatistic, input, DeletetmsTrafficStatistic, BatchDeletetmsTrafficStatistic, tmsTrafficStatisticController, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Dto/TmsVehicleFilterInput.cs` | Alarm, PageTmsVehicleFilterInput, ToDate, Type, LicensePlate, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Dto/TmsVehicleFilterOutput.cs` | PageTmsVehicleFilterOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/Queries/TmsVehicleFilterQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilter/TmsVehicleFilterController.cs` | input, input, TmsVehicleFilterController, BatchDeleteTmsVehicleFilter, UpdateTmsVehicleFilter, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Dto/TmsVehicleFilterHistoryInput.cs` | PageTmsVehicleFilterHistoryInput, LicensePlate, DeleteTmsVehicleFilterHistoryInput, Type, Direction, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Dto/TmsVehicleFilterHistoryOutput.cs` | PageTmsVehicleFilterHistoryOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/Queries/TmsVehicleFilterHistoryQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleFilterHistory/TmsVehicleFilterHistoryController.cs` | AddTmsVehicleFilterHistory, TmsVehicleFilterHistoryController, DeleteTmsVehicleFilterHistory, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Dto/TmsVehicleRegistrationInput.cs` | VehicleType, LicensePlate, PageTmsVehicleRegistrationInput, Brand, Owner, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Dto/TmsVehicleRegistrationOutput.cs` | PageTmsVehicleRegistrationOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/Queries/TmsVehicleRegistrationQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/VehicleRegistration/TmsVehicleRegistrationController.cs` | DeleteTmsVehicleRegistration, AddTmsVehicleRegistration, input, BatchDeleteTmsVehicleRegistration, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Commands/TmsWeatherCommandHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Dto/TmsWeatherInput.cs` | PageTmsWeatherInput, UpdateTmsWeatherInput, AddTmsWeatherInput, DeleteTmsWeatherInput, TimeDetect |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Dto/TmsWeatherOutput.cs` | PageTmsWeatherOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/Queries/TmsWeatherQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Weather/TmsWeatherController.cs` | DeleteTmsWeather, input, input, TmsWeatherController, BatchDeleteTmsWeather, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Dto/TmsWorkContactInput.cs` | Type, Phone, Status, PageTmsWorkContactInput, Code, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Dto/TmsWorkContactOutput.cs` | PageTmsWorkContactOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/Queries/TmsWorkContactQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkContact/TmsWorkContactController.cs` | BatchDeleteTmsWorkContact, input, DeleteTmsWorkContact, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Commands/WorkDetailCommandHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/Dto/WorkDetailInput.cs` | DeleteWorkDetailInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetail/TmsWorkDetailController.cs` | input, input, BatchDeleteTmsWorkDetail, AddTmsWorkDetail, UpdateTmsWorkDetail, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Dto/TmsWorkDetailContactInput.cs` | WorkContactId, OrderNo, WorkDetailId, Code, PageTmsWorkDetailContactInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Dto/TmsWorkDetailContactOutput.cs` | PageTmsWorkDetailContactOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/Queries/WorkDetailContactQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkDetailContact/TmsWorkDetailContactController.cs` | input, TmsWorkDetailContactController.<init>, PageByID, id, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkExecute/TmsWorkExecuteController.cs` | workExecuteId, remark, TmsWorkExecuteController, UpdateWorkExecuteStateToSkipped, UpdateWorkExecuteStateToWaiting, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Dto/TmsWorkUnitInput.cs` | PageTmsWorkUnitInput, Type, Code, Phone, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Dto/TmsWorkUnitOutput.cs` | PageTmsWorkUnitOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/Queries/TmsWorkUnitQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/WorkUnit/TmsWorkUnitController.cs` | DeleteTmsWorkUnit, Page, AddTmsWorkUnit, TmsWorkUnitController, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/Zone/TmsZoneController.cs` | DeleteZone, AddZone, input, input, UpdateZone, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Controllers/ZoneEquipment/TmsZoneEquipmentController.cs` | AddZoneEquipment, TmsZoneEquipmentController, input, input, AutoMapAll, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/MapLocation/MapLocationService.cs` | MapLocationService.<init>, MapLocationService, _env, env |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TMS/Modules.TMS/Infrastructure/Services/SignalLog/SignalLogService.cs` | _messBus, _tmsSignalLogRep, vmsId, SignalLogService.<init>, messBus, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/BaseController.cs` | BaseController, BasePath, GroupName |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoNam/DTTNamController.cs` | DTTNamController, request, RenderReport, request, ExportReport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoNgay/DTTNController.cs` | DTTNController, RenderReport, request, ExportReport, request |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoQuy/DTTQController.cs` | RenderReport, request, request, ExportReport, NonUnify, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoThang/DTTTController.cs` | DTTTController, request, RenderReport, request, ExportReport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/DoanhThu/DoanhThuTheoThoiGian/DTTTGController.cs` | request, ExportReport, DTTTGController, RenderReport, request |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoCa/LLTCController.cs` | LLTCController, request, ExportReport, request, RenderReport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoLoTrinh/LLTLTController.cs` | request, ExportReport, request, LLTLTController, RenderReport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoNam/LLTNamController.cs` | LLTNamController, request, request, ExportReport, RenderReport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoNgay/LLTNController.cs` | LLTNController, RenderReport, request, request, ExportReport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL.Report/Controllers/LuuLuong/LuuLuongTheoThang/LLTTController.cs` | ExportReport, request, RenderReport, LLTTController, request |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/TOLL/Module.TOLL/Controllers/BaseController.cs` | ApiDescriptionSettings |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Current/VmsCurrentInput.cs` | Code, Name, PageVmsDefaultCurrentInput, EquipmentId, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Current/VmsCurrentOutput.cs` | PageVmsDefaultCurrentOutput, PageVmsCurrentOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/EventDefault/VmsEventDefaultInput.cs` | Code, Name, PageVmsEventDefaultInput, EventTypeId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/EventDefault/VmsEventDefaultOutput.cs` | PageVmsEventDefaultOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ImageVms/VmsImageInput.cs` | DeleteVmsImageInput, EquipmentTypeId, Type, Status, PageVmsImageInput, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ImageVms/VmsImageOutput.cs` | PageVmsImageOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Message/VmsMessageInput.cs` | DeleteVmsMessageInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ProcessImg/VmsProcessImgInput.cs` | PageVmsProcessImgInput, ProcessId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/ProcessImg/VmsProcessImgOutput.cs` | PageVmsProcessImgOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Template/TemplateInput.cs` | Size, Status, Name, PageTemplateInput, Lang, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS.Core/Dto/Template/TemplateOutput.cs` | PageTemplateOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/BaseController.cs` | BaseController, BasePath, GroupName |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Control/VmsControlController.cs` | VmsControlController, input, AddVmsProcess |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/Queries/CurrentQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/Queries/DefaultQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Current/VmsCurrentController.cs` | Page, SaveDefaultContent, input, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/EquipmentTelemetry/EquipmentTelemetryController.cs` | input, Delete |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/EventDefault/Queries/EventDefaultQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/EventDefault/VmsEventDefaultController.cs` | VmsEventDefaultController, UpdateVmsEventDefault, Page, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ImageVms/Queries/VmsImageQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ImageVms/VmsImageController.cs` | VmsImageController, input, input, input, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Message/VmsMessageController.cs` | Delete, BatchDelete, Add, Update, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ProcessImg/Queries/ProcessImgQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/ProcessImg/VmsProcessImgController.cs` | BatchDeleteVmsProcessImg, DeleteVmsProcessImg, input, Page, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Template/Queries/TemplateQueryHandler.cs` | command, HandleAsync |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VMS/Modules.VMS/Controllers/Template/VmsTemplateController.cs` | input, env, input, AddVmsTemplate, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Controller/VwControllerInput.cs` | VwIdControllerInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Controller/VwControllerOutput.cs` | VwPageControllerOutput, VwControllerOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/EventRule/VwEventRuleInput.cs` | EventSource, Priority, EventTypeId, TargetSceneId, Status, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/EventRule/VwEventRuleOutput.cs` | VwPageEventRuleOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Scene/VwSceneInput.cs` | VwPageSceneInput, OutputId, Status, IsDefault, Name, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Scene/VwSceneOutput.cs` | VwPageSceneOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Schedule/VwScheduleInput.cs` | TargetSceneId, VwPageScheduleInput, Action, Name, Status |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Schedule/VwScheduleOutput.cs` | VwPageScheduleOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Screen/VwScreenOutput.cs` | VwPageScreenOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/SlotPort/VwSlotPortInput.cs` | VwIdSlotPortInput, VwDeleteSlotPortInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/SlotPort/VwSlotPortOutput.cs` | VwSlotPortOutput, VwPageSlotPortOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/Source/VwSourceOutput.cs` | VwPageSourceOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/WindowScene/VwWindowSceneInput.cs` | Name, Visible, VwPageWindowSceneInput, SourceId, SceneId |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall.Core/Dto/WindowScene/VwWindowSceneOutput.cs` | VwPageWindowSceneOutput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/BaseController.cs` | GroupName, BaseController, BasePath |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Controller/VwControllerController.cs` | input, input, VwControllerController, Page, GetById, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/EventRule/Queries/VwEventRuleQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/EventRule/VwEventRuleController.cs` | VwEventRuleController, BatchDeleteVwEventRule, AddVwEventRule, DeleteVwEventRule, UpdateVwEventRule, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/Queries/VwSceneQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Scene/VwSceneController.cs` | UpdateVwScene, input, input, DeleteVwScene, AddVwScene, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/Queries/VwScheduleQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Schedule/VwScheduleController.cs` | input, Page, DeleteVwSchedule, input, VwScheduleController, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Screen/VwScreenController.cs` | Page, input, UpdateVwScreen, VwScreenController, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/SlotPort/VwSlotPortController.cs` | AddVwSlotPort, input, input, UpdateVwSlotPort, GetById, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/Source/VwSourceController.cs` | Page, input, UpdateVwSource, BatchDeleteVwSource, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/WindowScene/Queries/VwWindowSceneQueryHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/VideoWall/Module.VideoWall/Controllers/WindowScene/VwWindowSceneController.cs` | Page, input, input, VwWindowSceneController, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP.Report/Controllers/BaseController.cs` | BaseController, BasePath, GroupName |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP.Report/Controllers/Statistical/WpStatisticalReportController.cs` | WpStatisticalReportController, RenderReport, request, ExportReport, ExportReport, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/BaseController.cs` | BasePath, BaseController, GroupName |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Category/Dto/WPCategoryInput.cs` | WpDeleteCategoryInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Category/WPCategoryControllers.cs` | input, WpCategoryController, input, AddCategory, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Menu/Dto/WpMenuInput.cs` | WpDeleteMenuInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Menu/WpMenuController.cs` | input, input, input, UpdateMenu, DeleteMenu, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Post/Dto/PostInput.cs` | DeletePostInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Post/WPPostControllers.cs` | AddPost, UpdatePost, WpPostController, DeletePost, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceList/WpPriceListControllers.cs` | BatchDeletePriceList, input, AddPriceList, DeletePriceList, input, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceListDetail/Commands/PriceListDetailCommandHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceListDetail/Dto/WpPriceListDetailInput.cs` | WpDeletePriceListDetailInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/PriceListDetail/WpPriceListDetailControllers.cs` | AddPriceListDetail, input, input, input, BatchDeletePriceListDetail, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Toll/Commands/TollCommandHandler.cs` | HandleAsync, command |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Toll/Dto/WpTollInput.cs` | WpDeleteTollInput |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/Toll/WpTollControllers.cs` | input, input, input, DeleteToll, AddToll, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/TrafficLaw/Dto/WpTrafficLawOutput.cs` | WpChangeStattusTrafficLawOutput, IsPublish |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/TrafficLaw/WPTrafficLawControllers.cs` | DeleteTrafficLaw, input, WpTrafficLawController, input, ChangeStatus, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/WP/Modules.WP/Controllers/WpConfig/WpConfigControllers.cs` | Update, input, WpConfigController |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.DTO/Enums/BaseEnums.cs` | IsRequired, IsRequired, NoRequired |
| `TA-ITS015-WEBAPI-V1.0/src/Shared/Shared.Utility/Apis/Tms/ISignalLogApi.cs` | ISignalLogApi |
| `tests/VideoWall/Controllers/VwControllerTests.cs` | VwControllerQuery_GetById_ReturnsSuccess_Test |
| `tests/VideoWall/Controllers/VwSlotPortTests.cs` | VwSlotPortQuery_GetById_ReturnsSuccess_Test |

## Connected Communities

- **Dto/Incident +3 dirs** (1 cross-edges)

## How to Explore

```
analyze(operation:"communities", id:"community-13")
explore(operation:"context", task:"understand Controllers/MaintenanceSchedule +203 dirs", format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
