using Shared.DTO.Constants.Localization;


namespace Modules.TMS.Core.Exceptions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        public class Tms
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tms.";
                public const string Validation = "lz.validation.tms.";
                public const string Exception = "lz.exception.tms.";
                public const string Entity = "lz.entity.tms.";
            }

            public class Entity
            {
                /// <summary>
                /// Zone
                /// </summary>
                public const string ZoneId = Prefix.Entity + "zoneId";

                /// <summary>
                /// Work Detail
                /// </summary>
                public const string WorkDetailId = Prefix.Entity + "workDetailId";

                /// <summary>
                /// Work Contact
                /// </summary>
                public const string WorkContactId = Prefix.Entity + "workContactId";

                /// <summary>
                /// Incident
                /// </summary>
                public const string IncidentId = Prefix.Entity + "incidentId";

                /// <summary>
                /// TrafficPlan
                /// </summary>
                public const string TrafficPlanId = Prefix.Entity + "trafficPlanId";

                /// <summary>
                /// EquipmentType
                /// </summary>
                public const string EquipmentType = Prefix.Entity + "equipmentType";

                /// <summary>
                /// Met
                /// </summary>
                public const string MetNumber = Prefix.Entity + "metNumber";

                /// <summary>
                /// Km
                /// </summary>
                public const string KmNumber = Prefix.Entity + "kmNumber";

                /// <summary>
                /// Direction
                /// </summary>
                public const string Direction = Prefix.Entity + "direction";

                /// <summary>
                /// Active direction
                /// </summary>
                public const string ActiveDirection = Prefix.Entity + "activeDirection";

                /// <summary>
                /// Lane
                /// </summary>
                public const string Lane = Prefix.Entity + "lane";

                /// <summary>
                /// Equipment state
                /// </summary>
                public const string EquipmentState = Prefix.Entity + "equipmentState";

                /// <summary>
                /// Location
                /// </summary>
                public const string Location = Prefix.Entity + "location";

                /// <summary>
                /// Module
                /// </summary>
                public const string Module = Prefix.Entity + "module";

            }
        }

        /// <summary>
        /// Tms Zone Msg
        /// Author: SonTH
        /// </summary>
        public class TmsZone
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsZone.";
                public const string Validation = "lz.validation.tmsZone.";
                public const string Exception = "lz.exception.tmsZone.";
                public const string Entity = "lz.entity.tmsZone.";
            }

            public class Entity
            {
                public const string FromKmNumber = Prefix.Entity + "fromKmNumber";
                public const string FromMetNumber = Prefix.Entity + "fromMetNumber";
                public const string ToKmNumber = Prefix.Entity + "toKmNumber";
                public const string ToMetNumber = Prefix.Entity + "toMetNumber";
                public const string Type = Prefix.Entity + "type";

            }
            public class Exception
            {
                public const string ZoneInUse = Prefix.Exception + "zoneInUse";
            }
        }


        /// <summary>
        /// TmsWorkDetailContact Msg
        /// Author: SonTH
        /// </summary>
        public class TmsWorkDetailContact
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsWorkDetailContact.";
                public const string Validation = "lz.validation.tmsWorkDetailContact.";
                public const string Exception = "lz.exception.tmsWorkDetailContact.";
                public const string Entity = "lz.entity.tmsWorkDetailContact.";
            }

            public class Exception 
            {
                public const string ContactExist = Prefix.Exception + "contactExist";

            }

        }

        /// <summary>
        /// Author: ThangNN
        /// Description: TmsVehicleRegistration Msg
        /// </summary>
        public class TmsVehicleRegistration
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsVehicleRegistration.";
                public const string Validation = "lz.validation.tmsVehicleRegistration.";
                public const string Exception = "lz.exception.tmsVehicleRegistration.";
                public const string Entity = "lz.entity.tmsVehicleRegistration.";
            }

            public class Entity
            {
                public const string LicensePlate = Prefix.Entity + "licensePlate";
                public const string StartDate = Prefix.Entity + "startDate";
                public const string EndDate = Prefix.Entity + "endDate";
                public const string Alarm = Prefix.Entity + "alarm";

                public const string VehicleType = Prefix.Entity + "vehicleType";
                public const string Brand = Prefix.Entity + "brand";
                public const string YearManufacture = Prefix.Entity + "yearManufacture";
                public const string Owner = Prefix.Entity + "owner";
                public const string Address = Prefix.Entity + "address";
                public const string EngineNumber = Prefix.Entity + "engineNumber";
                public const string ChassisNumber = Prefix.Entity + "chassisNumber";
                public const string OverallDimensions = Prefix.Entity + "overallDimensions";
                public const string ContainerDimensions = Prefix.Entity + "containerDimensions";
                public const string Seat = Prefix.Entity + "seat";
                public const string InspectionStamp = Prefix.Entity + "inspectionStamp";
                public const string DateRegistrations = Prefix.Entity + "dateRegistrations";
                public const string ExpirationDate = Prefix.Entity + "expirationDate";
                public const string DataType = Prefix.Entity + "dataType";
            }

            public class Validation
            {
                public const string DateRegistrationsExceedsExpiration = Prefix.Validation + "dateRegistrationsExceedsExpiration";
                public const string ExpirationDateBeforeDateRegistrations = Prefix.Validation + "expirationDateBeforeDateRegistrations";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: TmsVehicleFilter Msg
        /// </summary>
        public class TmsVehicleFilter
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsVehicleFilter.";
                public const string Validation = "lz.validation.tmsVehicleFilter.";
                public const string Exception = "lz.exception.tmsVehicleFilter.";
                public const string Entity = "lz.entity.tmsVehicleFilter.";
            }

            public class Entity
            {
                public const string LicensePlate = Prefix.Entity + "licensePlate";
                public const string StartDate = Prefix.Entity + "startDate";
                public const string EndDate = Prefix.Entity + "endDate";
                public const string Alarm = Prefix.Entity + "alarm";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: TmsVehicleFilterHistory Msg
        /// </summary>
        public class TmsVehicleFilterHistory
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsVehicleFilterHistory.";
                public const string Validation = "lz.validation.tmsVehicleFilterHistory.";
                public const string Exception = "lz.exception.tmsVehicleFilterHistory.";
                public const string Entity = "lz.entity.tmsVehicleFilterHistory.";
            }

            public class Entity
            {
                public const string LicensePlate = Prefix.Entity + "licensePlate";
                public const string DetectTime = Prefix.Entity + "detectTime";
                public const string Direction = Prefix.Entity + "direction";
                public const string Type = Prefix.Entity + "type";
                public const string LocationId = Prefix.Entity + "locationId";
                public const string LaneId = Prefix.Entity + "laneId";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: TmsWorkUnit Msg
        /// </summary>
        public class TmsWorkUnit
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsWorkUnit.";
                public const string Validation = "lz.validation.tmsWorkUnit.";
                public const string Exception = "lz.exception.tmsWorkUnit.";
                public const string Entity = "lz.entity.tmsWorkUnit.";
            }

            public class Entity
            {
                public const string Phone = Prefix.Entity + "phone";
                public const string Address = Prefix.Entity + "address";
                public const string Type = Prefix.Entity + "ltype";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: TmsWorkContact Msg
        /// </summary>
        public class TmsWorkContact
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsWorkContact.";
                public const string Validation = "lz.validation.tmsWorkContact.";
                public const string Exception = "lz.exception.tmsWorkContact.";
                public const string Entity = "lz.entity.tmsWorkContact.";
            }

            public class Entity
            {
                public const string Phone = Prefix.Entity + "phone";
                public const string Address = Prefix.Entity + "address";
                public const string Type = Prefix.Entity + "type";
                public const string RefId = Prefix.Entity + "refId";
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: TmsIncident Msg
        /// </summary>
        public class TmsIncident
        {
            public const string MetadataLabelPrefix = "lz.label.tmsIncident.";
            protected class Prefix
            {
                public const string Message = "lz.message.tmsIncident.";
                public const string Validation = "lz.validation.tmsIncident.";
                public const string Exception = "lz.exception.tmsIncident.";
                public const string Entity = "lz.entity.tmsIncident.";
                public const string Label = "lz.label.tmsIncident.";
            }

            public class Label
            {
                /// <summary>
                /// Nhóm sự cố KHÔNG khai được loại sự kiện: <c>EventTypeId</c> null, hoặc trỏ tới
                /// <c>TmsEventType</c> đã bị xoá nên LEFT JOIN trả <c>Name</c> null.
                ///
                /// <para>Phải là chuỗi ĐÃ DỊCH khi đi ra biểu đồ, không phải khoá i18n: FE chỉ gọi
                /// <c>$t()</c> cho <c>title.text</c> và <c>xAxis.name</c>, còn <c>legend.data</c> được
                /// hiển thị nguyên văn và đồng thời là <c>series.name</c> để ECharts ghép đường với
                /// legend.</para>
                /// </summary>
                public const string UnknownEventType = Prefix.Label + "unknownEventType";
            }

            public class Entity
            {
                public const string EventTypeId = Prefix.Entity + "eventTypeId";
                public const string Source = Prefix.Entity + "source";
                public const string Location = Prefix.Entity + "location";
                public const string ZoneId = Prefix.Entity + "zoneId";
                public const string InfluenceScope = Prefix.Entity + "influenceScope";
                public const string TrafficPlanId = Prefix.Entity + "trafficPlanId";
                public const string EquipmentId = Prefix.Entity + "equipmentId";

                public const string KmNumber = Prefix.Entity + "kmNumber";
                public const string MetNumber = Prefix.Entity + "metNumber";
                public const string InjuredNumber = Prefix.Entity + "injuredNumber";
                public const string VehicleNumber = Prefix.Entity + "vehicleNumber";
                public const string CreatedBy = Prefix.Entity + "createdBy";
                public const string CreatedDate = Prefix.Entity + "createdDate";
                public const string ValidatedBy = Prefix.Entity + "validatedBy";
                public const string ValidatedDate = Prefix.Entity + "validatedDate";
                public const string ApprovedBy = Prefix.Entity + "approvedBy";
                public const string ApprovedDate = Prefix.Entity + "approvedDate";
                public const string FinishedBy = Prefix.Entity + "finishedBy";
                public const string FinishedDate = Prefix.Entity + "finishedDate";
            }

            public class Validation
            {
                public const string LimitDateRange = Prefix.Validation + "limitDateRange";
            }

            public class Exception
            {
                public const string PagingInvalid = Prefix.Exception + "pagingInvalid";
                public const string DisplayWindowInvalid = Prefix.Exception + "displayWindowInvalid";
                public const string SortFieldInvalid = Prefix.Exception + "sortFieldInvalid";
                public const string SortDirectionInvalid = Prefix.Exception + "sortDirectionInvalid";
                public const string DateRangeInvalid = Prefix.Exception + "dateRangeInvalid";

                /// <summary>Cửa sổ tra cứu bản tin đã ghi nhận vượt số ngày cho phép.</summary>
                public const string RecordedWindowTooLarge = Prefix.Exception + "recordedWindowTooLarge";

                /// <summary>Loại sự cố chưa được cấu hình là Event hay Incident.</summary>
                public const string EventGroupNotConfigured = Prefix.Exception + "eventGroupNotConfigured";

                /// <summary>Không được đổi loại của hồ sơ đã tạo.</summary>
                public const string EventTypeLocked = Prefix.Exception + "eventTypeLocked";

                /// <summary>Bản tin nhóm Event chỉ ghi nhận, không có luồng xử lý sự cố.</summary>
                public const string EventHasNoWorkflow = Prefix.Exception + "eventHasNoWorkflow";

                /// <summary>Bản tin nhóm Event không được chuyển trạng thái sự cố.</summary>
                public const string EventStateNotChangeable = Prefix.Exception + "eventStateNotChangeable";

                /// <summary>Không được chuyển sự cố lùi trạng thái (Đã xác minh → Đăng ký, Đang xử lý → Đã xác minh/Đăng ký).</summary>
                public const string StateBackwardNotAllowed = Prefix.Exception + "stateBackwardNotAllowed";

                /// <summary>Sự cố đã ở trạng thái Đang xử lý, không chọn kịch bản/sinh công việc lần nữa.</summary>
                public const string AlreadyInProgress = Prefix.Exception + "alreadyInProgress";

                /// <summary>Hồ sơ không thuộc nhóm sự cố có thể xử lý.</summary>
                public const string NotProcessableIncident = Prefix.Exception + "notProcessableIncident";

                /// <summary>Nguồn dữ liệu snapshot SSE tạm thời không khả dụng.</summary>
                public const string SnapshotUnavailable = Prefix.Exception + "snapshotUnavailable";

                /// <summary>KienLT - 23/10/2026: Tạo sự cố từ sự kiện: loại được chọn không thuộc nhóm sự cố (hoặc là unknown).</summary>
                public const string FromEventTypeNotIncident = Prefix.Exception + "fromEventTypeNotIncident";

                /// <summary>KienLT - 23/10/2026: Bản tin đã từng có sự cố nhưng sự cố đó đã bị xoá; tham số {0} mã sự cố.</summary>
                public const string FromEventIncidentDeleted = Prefix.Exception + "fromEventIncidentDeleted";
            }
        }

        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TmsMap
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMap.";
                public const string Validation = "lz.validation.tmsMap.";
                public const string Exception = "lz.exception.tmsMap.";
                public const string Entity = "lz.entity.tmsMap.";
            }

            public class Entity
            {
                public const string Url = Prefix.Entity + "url";
                public const string Type = Prefix.Entity + "type";
                public const string Width = Prefix.Entity + "width";
                public const string Height = Prefix.Entity + "height";
                public const string OrderNo = Prefix.Entity + "order";
                public const string IsDefault = Prefix.Entity + "isDefault";
                public const string Mode = Prefix.Entity + "mode";

            }

            public class Exception
            {
                public const string ItCannotTurnedOff = Prefix.Exception + "itCannotTurnedOff";
                public const string MapUseByDefaultCannotBeReverted = Prefix.Exception + "mapUseByDefaultCannotBeReverted";
                public const string NoSetMapToDefaultToUnusedState = Prefix.Exception + "noSetMapToDefaultToUnusedState";
                public const string MapDefaultCanNotDelete = Prefix.Exception + "mapDefaultCanNotDelete";
                public const string CannotDeleteDefaultMap = Prefix.Exception + "cannotDeleteDefaultMap";

            }

        }


        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TmsMapLocation
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMapLocation.";
                public const string Validation = "lz.validation.tmsMapLocation.";
                public const string Exception = "lz.exception.tmsMapLocation.";
                public const string Entity = "lz.entity.tmsMapLocation.";
            }

            public class Entity
            {
                public const string MapId = Prefix.Entity + "mapId";
                public const string GeoJson = Prefix.Entity + "geoJson";
                public const string Config = Prefix.Entity + "config";
            }
        }

        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TmsMapDetail
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMapDetail.";
                public const string Validation = "lz.validation.tmsMapDetail.";
                public const string Exception = "lz.exception.tmsMapDetail.";
                public const string Entity = "lz.entity.tmsMapDetail.";
            }

            public class Entity
            {
                public const string MapId = Prefix.Entity + "mapId";
                public const string GeoJson = Prefix.Entity + "geoJson";
                public const string RefId = Prefix.Entity + "refId";
                public const string RefType = Prefix.Entity + "refType";
                public const string CoordY = Prefix.Entity + "coordY";
                public const string CoordX = Prefix.Entity + "coordX";
                public const string Coordinates = Prefix.Entity + "coordinates";
            }
        }

        public class TmsEquipment
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsEquipment.";
                public const string Validation = "lz.validation.tmsEquipment.";
                public const string Exception = "lz.exception.tmsEquipment.";
                public const string Entity = "lz.entity.tmsEquipment.";
            }

            public class Entity
            {
                public const string Code = Prefix.Entity + "code";
                public const string Name = Prefix.Entity + "name";
                public const string Url = Prefix.Entity + "url";
                public const string Username = Prefix.Entity + "username";
                public const string Password = Prefix.Entity + "password";
            }
        }

        /// <summary>
        /// TMSEquipmentType
        /// </summary>
        public class EquipmentType
        {

            public class Entity
            {
                public const string _Prefix = "lz.entity.tmsEquipmentType.";

                public const string ID = _Prefix + "id";
                public const string Code = _Prefix + "code";
                public const string Name = _Prefix + "name";
                public const string Size = _Prefix + "size";
                public const string DefaultIcon = _Prefix + "defaultIcon";
                public const string ActiveIcon = _Prefix + "activeIcon";
                public const string InactiveIcon = _Prefix + "inactiveIcon";
                public const string FaultIcon = _Prefix + "faultIcon";
                public const string ModuleId = _Prefix + "moduleId";

            }
        }

        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class EventType
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsEventType.";
                public const string Validation = "lz.validation.tmsEventType.";
                public const string Exception = "lz.exception.tmsEventType.";
                public const string Entity = "lz.entity.tmsEventType.";
            }

            /// <summary>
            /// Generate entity messages
            /// </summary>
            public class Entity
            {
                public const string WarningType = Prefix.Entity + "warningType";
                public const string ModuleId = Prefix.Entity + "moduleId";
                public const string Priority = Prefix.Entity + "priority";
            }
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Cấu hình hiển thị VMS/LCS theo loại sự kiện (trước/sau sự cố)
        /// Created date: 16/07/2026
        /// </summary>
        public class TmsEquipmentEventRule
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsEquipmentEventRule.";
                public const string Validation = "lz.validation.tmsEquipmentEventRule.";
                public const string Exception = "lz.exception.tmsEquipmentEventRule.";
                public const string Entity = "lz.entity.tmsEquipmentEventRule.";
            }

            public class Entity
            {
                public const string EventTypeId = Prefix.Entity + "eventTypeId";
                public const string EquipmentTypeId = Prefix.Entity + "equipmentTypeId";
                public const string ProcessType = Prefix.Entity + "processType";
                public const string SuggestionRangeMeters = Prefix.Entity + "suggestionRangeMeters";
                public const string MaxTargetCount = Prefix.Entity + "maxTargetCount";
            }

            public class Validation
            {
                public const string ProcessTypeInvalid = Prefix.Validation + "processTypeInvalid";
            }

            public class Exception
            {
                /// <summary>
                /// Đã tồn tại rule cho cùng loại sự kiện + loại thiết bị + loại xử lý
                /// </summary>
                public const string Duplicate = Prefix.Exception + "duplicate";
            }
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Cấu hình tự động xử lý sự kiện/sự cố theo loại sự kiện
        /// Created date: 24/07/2026
        /// </summary>
        public class TmsIncidentAutomationConfig
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsIncidentAutomationConfig.";
                public const string Validation = "lz.validation.tmsIncidentAutomationConfig.";
                public const string Exception = "lz.exception.tmsIncidentAutomationConfig.";
                public const string Entity = "lz.entity.tmsIncidentAutomationConfig.";
            }

            public class Entity
            {
                public const string EventTypeId = Prefix.Entity + "eventTypeId";
                public const string IsAutoExecuteEnabled = Prefix.Entity + "isAutoExecuteEnabled";
                public const string TriggerState = Prefix.Entity + "triggerState";
                public const string AutoVerifyIncident = Prefix.Entity + "autoVerifyIncident";
                public const string WarningDelaySeconds = Prefix.Entity + "warningDelaySeconds";
                public const string TrafficPlanId = Prefix.Entity + "trafficPlanId";
                public const string IsAutoFinishEnabled = Prefix.Entity + "isAutoFinishEnabled";
                public const string AutoFinishAfterSeconds = Prefix.Entity + "autoFinishAfterSeconds";
                public const string FailurePolicy = Prefix.Entity + "failurePolicy";
                public const string ResetPolicy = Prefix.Entity + "resetPolicy";
            }

            public class Validation
            {
                public const string TriggerStateInvalid = Prefix.Validation + "triggerStateInvalid";
                public const string FailurePolicyInvalid = Prefix.Validation + "failurePolicyInvalid";
                public const string ResetPolicyInvalid = Prefix.Validation + "resetPolicyInvalid";
                public const string AutoVerifyRequiredForRegistrationTrigger = Prefix.Validation + "autoVerifyRequiredForRegistrationTrigger";
                public const string AutoFinishAfterSecondsRequired = Prefix.Validation + "autoFinishAfterSecondsRequired";
            }

            public class Exception
            {
                /// <summary>
                /// Đã tồn tại cấu hình tự động cho loại sự kiện này
                /// </summary>
                public const string Duplicate = Prefix.Exception + "duplicate";

                /// <summary>
                /// Kịch bản tự động phải cùng loại sự kiện, đang Enable và ExecutionMode Automatic/Both
                /// </summary>
                public const string TrafficPlanInvalid = Prefix.Exception + "trafficPlanInvalid";

                /// <summary>
                /// Tự hoàn thành chưa xử lý chỉ áp dụng cho nhóm event hoặc incident
                /// </summary>
                public const string UnclaimedFinishGroupInvalid = Prefix.Exception + "unclaimedFinishGroupInvalid";

                /// <summary>
                /// Tự hoàn thành chưa xử lý không được bật cùng chạy kịch bản hoặc chọn kịch bản
                /// </summary>
                public const string UnclaimedFinishPlanConflict = Prefix.Exception + "unclaimedFinishPlanConflict";

                /// <summary>
                /// Thời gian tự hoàn thành chưa xử lý phải từ {0} đến {1} giây
                /// </summary>
                public const string UnclaimedFinishSecondsRange = Prefix.Exception + "unclaimedFinishSecondsRange";

                /// <summary>
                /// Nguồn áp dụng phải bao gồm VDS khi bật tự hoàn thành chưa xử lý
                /// </summary>
                public const string UnclaimedFinishSourceRequiresVds = Prefix.Exception + "unclaimedFinishSourceRequiresVds";
            }
        }

        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TmsModule
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMoudule.";
                public const string Validation = "lz.validation.tmsMoudule.";
                public const string Exception = "lz.exception.tmsMoudule.";
                public const string Entity = "lz.entity.tmsMoudule.";
            }

            public class Entity
            {
                public const string KeyName = Prefix.Entity + "keyName";
                public const string PluginId = Prefix.Entity + "pluginId";
                public const string Config = Prefix.Entity + "config";
            }
        }


        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TrafficPlan
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsTrafficPlan.";
                public const string Validation = "lz.validation.tmsTrafficPlan.";
                public const string Exception = "lz.exception.tmsTrafficPlan.";
                public const string Entity = "lz.entity.tmsTrafficPlan.";
            }

            public class Entity
            {
                public const string EventTypeId = Prefix.Entity + "eventTypeId";
                public const string ExecutionMode = Prefix.Entity + "executionMode";
                public const string PlanVersion = Prefix.Entity + "planVersion";
            }
        }

        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TmsTrafficPlanWorkDetail
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsTrafficPlanWorkDetail.";
                public const string Validation = "lz.validation.tmsTrafficPlanWorkDetail.";
                public const string Exception = "lz.exception.tmsTrafficPlanWorkDetail.";
                public const string Entity = "lz.entity.tmsTrafficPlanWorkDetail.";
            }
            public class Entity
            {

                public const string TrafficPlanId = Prefix.Entity + "trafficPlanId";
                public const string WorkDetailId = Prefix.Entity + "workDetailId";
                public const string ExecuteOrgId = Prefix.Entity + "executeOrgId";
                public const string Step = Prefix.Entity + "step";
                public const string State = Prefix.Entity + "state";
            }
        }


        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TmsSignalLog
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsSignalLog.";
                public const string Validation = "lz.validation.tmsSignalLog.";
                public const string Exception = "lz.exception.tmsSignalLog.";
                public const string Entity = "lz.entity.tmsSignalLog.";
            }

            public class Entity
            {
                public const string EquipmentId = Prefix.Entity + "equipmentId";
                public const string OldData = Prefix.Entity + "oldData";
                public const string NewData = Prefix.Entity + "newData";
                public const string OldUrl = Prefix.Entity + "oldUrl";
                public const string NewUrl = Prefix.Entity + "newUrl";
                public const string IncidentId = Prefix.Entity + "incidentId";
            }
        }

        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TmsTrafficStatistic
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsTrafficStatistic.";
                public const string Validation = "lz.validation.tmsTrafficStatistic.";
                public const string Exception = "lz.exception.tmsTrafficStatistic.";
                public const string Entity = "lz.entity.tmsTrafficStatistic.";
            }

            public class Entity
            {
                public const string AvgSpeed = Prefix.Entity + "avgSpeed";
                public const string LocationDetail = Prefix.Entity + "locationDetail";
                public const string Location = Prefix.Entity + "location";
                public const string IpAddress = Prefix.Entity + "ipAddress";
                public const string SpaceOccupy = Prefix.Entity + "spaceOccupy";
                public const string TimeOccupy = Prefix.Entity + "timeOccupy";
                public const string SpaceHeadway = Prefix.Entity + "spaceHeadway";
                public const string TimeHeadway = Prefix.Entity + "timeHeadway";
                public const string StopTime = Prefix.Entity + "stopTime";
                public const string QueueLength = Prefix.Entity + "queueLength";
            }
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Bảng ma trận vận tốc / lưu lượng theo giờ.
        /// Created date: 11/08/2026
        /// </summary>
        public class TmsTrafficMatrix
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsTrafficMatrix.";
                public const string Validation = "lz.validation.tmsTrafficMatrix.";
                public const string Exception = "lz.exception.tmsTrafficMatrix.";
                public const string Entity = "lz.entity.tmsTrafficMatrix.";
                public const string Label = "lz.label.tmsTrafficMatrix.";
            }

            public class Entity
            {
                public const string ValueDate = Prefix.Entity + "valueDate";
                public const string VehicleType = Prefix.Entity + "vehicleType";
                public const string AvgSpeed = Prefix.Entity + "avgSpeed";
                public const string VehicleCount = Prefix.Entity + "vehicleCount";
            }

            public class Label
            {
                /// <summary>Tiêu đề bảng ma trận</summary>
                public const string Title = Prefix.Label + "title";

                /// <summary>Cột đầu: tên đoạn tuyến</summary>
                public const string Section = Prefix.Label + "section";

                /// <summary>Chế độ xem: vận tốc</summary>
                public const string MetricSpeed = Prefix.Label + "metricSpeed";

                /// <summary>Chế độ xem: lưu lượng</summary>
                public const string MetricVolume = Prefix.Label + "metricVolume";

                /// <summary>Nhãn dưới/trên dải màu đầu-cuối</summary>
                public const string BandUnder = Prefix.Label + "bandUnder";
                public const string BandOver = Prefix.Label + "bandOver";

                /// <summary>Nhóm dữ liệu của camera chưa khai hướng</summary>
                public const string UnknownDirection = Prefix.Label + "unknownDirection";
            }
        }


        public class TmsWorkExecute
        {
            public class Entity
            {
                public const string _Prefix = "lz.entity.tmsWorkExecute.";
                public const string ID = _Prefix + "id";
                public const string Code = _Prefix + "code";
            }
        }


        public class TmsWorkExecuteHistory
        {
            public class Entity
            {
                public const string _Prefix = "lz.entity.tmsWorkExecuteHistory.";

                public const string ID = _Prefix + "id";
                public const string IncidentId = Tms.Entity.IncidentId; // Duplication with TmsIncident
                public const string TrafficPlanId = Tms.Entity.TrafficPlanId; // Duplication with TmsTrafficPlan
                public const string WorkDetailId = Tms.Entity.WorkDetailId; // Duplication with TmsWorkDetail
                public const string Step = _Prefix + "step";
                public const string WorkName = _Prefix + "workName";
                public const string ExecuteOrg = _Prefix + "executeOrg";
                public const string ExecuteBy = _Prefix + "executeBy";
                public const string ExecuteDate = _Prefix + "executeDate";
            }
        }

        /// <summary>
        /// Author: TuanHA
        /// Created date: 15/01/2025
        /// </summary>
        public class TmsWorkDetail
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsWorkDetail.";
                public const string Validation = "lz.validation.tmsWorkDetail.";
                public const string Exception = "lz.exception.tmsWorkDetail.";
                public const string Entity = "lz.entity.tmsWorkDetail.";
            }

            public class Entity
            {
                public const string WorkName = Prefix.Entity + "workName";
                public const string WorkType = Prefix.Entity + "workType";
                public const string WorkMode = Prefix.Entity + "workMode";
                public const string ControlType = Prefix.Entity + "controlType";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 07/11/2025
        /// Description: TmsPatrolTeam Messages
        /// </summary>
        public class TmsPatrolTeam
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsPatrolTeam.";
                public const string Validation = "lz.validation.tmsPatrolTeam.";
                public const string Exception = "lz.exception.tmsPatrolTeam.";
                public const string Entity = "lz.entity.tmsPatrolTeam.";
            }

            public class Entity
            {
                public const string Name = Prefix.Entity + "name";
                public const string Description = Prefix.Entity + "description";
                public const string Status = Prefix.Entity + "status";
            }

            public class Validation
            {
                public const string NameRequired = Prefix.Validation + "nameRequired";
                public const string StatusInvalid = Prefix.Validation + "statusInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyExists = Prefix.Exception + "alreadyExists";
                public const string DeleteFailed = Prefix.Exception + "deleteFailed";
                public const string InUse = Prefix.Exception + "inUse";
            }

            public class Message
            {
                public const string AddSuccess = Prefix.Message + "addSuccess";
                public const string UpdateSuccess = Prefix.Message + "updateSuccess";
                public const string DeleteSuccess = Prefix.Message + "deleteSuccess";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 07/11/2025
        /// Description: TmsDutySchedule Messages
        /// </summary>
        public class TmsDutySchedule
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsDutySchedule.";
                public const string Validation = "lz.validation.tmsDutySchedule.";
                public const string Exception = "lz.exception.tmsDutySchedule.";
                public const string Entity = "lz.entity.tmsDutySchedule.";
            }

            public class Entity
            {
                public const string Name = Prefix.Entity + "name";
                public const string StartTime = Prefix.Entity + "startTime";
                public const string EndTime = Prefix.Entity + "endTime";
                public const string PatrolTeamId = Prefix.Entity + "patrolTeamId";
                public const string Status = Prefix.Entity + "status";
            }

            public class Validation
            {
                public const string NameRequired = Prefix.Validation + "nameRequired";
                public const string StartTimeRequired = Prefix.Validation + "startTimeRequired";
                public const string EndTimeRequired = Prefix.Validation + "endTimeRequired";
                public const string EndTimeBeforeStartTime = Prefix.Validation + "endTimeBeforeStartTime";
                public const string StatusInvalid = Prefix.Validation + "statusInvalid";
                public const string UserConflict = Prefix.Validation + "userConflict";
                public const string NoUsers = Prefix.Validation + "noUsers";
                public const string TargetUserRequired = Prefix.Validation + "targetUserRequired";
                public const string TargetScheduleRequired = Prefix.Validation + "targetScheduleRequired";
                public const string ReplaceTargetRequired = Prefix.Validation + "replaceTargetRequired";
                public const string TransferTargetOrNewRequired = Prefix.Validation + "transferTargetOrNewRequired";
                public const string NewScheduleInfoRequired = Prefix.Validation + "newScheduleInfoRequired";
                public const string ApproveStatusInvalid = Prefix.Validation + "approveStatusInvalid";
                public const string ChangeRequestTypeInvalid = Prefix.Validation + "changeRequestTypeInvalid";
                public const string SwapRequiresTargetAndSchedule = Prefix.Validation + "swapRequiresTargetAndSchedule";
                public const string TargetUserStatusCannotReplace = Prefix.Validation + "targetUserStatusCannotReplace";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyExists = Prefix.Exception + "alreadyExists";
                public const string DeleteFailed = Prefix.Exception + "deleteFailed";
                public const string PatrolTeamNotFound = Prefix.Exception + "patrolTeamNotFound";
                public const string UserNotInSchedule = Prefix.Exception + "userNotInSchedule";
                public const string UserNotIdentified = Prefix.Exception + "userNotIdentified";
                public const string NoManagerPermission = Prefix.Exception + "noManagerPermission";
                public const string ChangeRequestNotFound = Prefix.Exception + "changeRequestNotFound";
                public const string ChangeRequestAlreadyProcessed = Prefix.Exception + "changeRequestAlreadyProcessed";
                public const string ChangeRequestNoApprovePermission = Prefix.Exception + "changeRequestNoApprovePermission";
                public const string ChangeRequestNoCancelPermission = Prefix.Exception + "changeRequestNoCancelPermission";
                public const string ChangeRequestOnlyPendingCancel = Prefix.Exception + "changeRequestOnlyPendingCancel";
                public const string PendingRequestExists = Prefix.Exception + "pendingRequestExists";
                public const string RequesterNotInSchedule = Prefix.Exception + "requesterNotInSchedule";
                public const string TargetNotInSchedule = Prefix.Exception + "targetNotInSchedule";
                public const string TargetNotInThisSchedule = Prefix.Exception + "targetNotInThisSchedule";
                public const string AlreadyInTargetSchedule = Prefix.Exception + "alreadyInTargetSchedule";
                public const string TargetScheduleNotFound = Prefix.Exception + "targetScheduleNotFound";
                public const string TimeConflictCannotSwap = Prefix.Exception + "timeConflictCannotSwap";
                public const string TransferNoTargetSchedule = Prefix.Exception + "transferNoTargetSchedule";
                public const string TransferUserNotFound = Prefix.Exception + "transferUserNotFound";
                public const string ReplaceNoTargetUser = Prefix.Exception + "replaceNoTargetUser";
                public const string ReplaceNoReplacer = Prefix.Exception + "replaceNoReplacer";
                public const string ReplaceTargetNotFound = Prefix.Exception + "replaceTargetNotFound";
                public const string CannotReplaceSelf = Prefix.Exception + "cannotReplaceSelf";
                public const string UpdateScheduleIdFailed = Prefix.Exception + "updateScheduleIdFailed";
            }

            public class Message
            {
                public const string AddSuccess = Prefix.Message + "addSuccess";
                public const string UpdateSuccess = Prefix.Message + "updateSuccess";
                public const string DeleteSuccess = Prefix.Message + "deleteSuccess";
                public const string AddUserSuccess = Prefix.Message + "addUserSuccess";
                public const string RemoveUserSuccess = Prefix.Message + "removeUserSuccess";
                public const string UpdateUserRoleSuccess = Prefix.Message + "updateUserRoleSuccess";
                public const string HistoryCreated = Prefix.Message + "historyCreated";
                public const string HistoryDeleted = Prefix.Message + "historyDeleted";
                public const string HistoryStatusChanged = Prefix.Message + "historyStatusChanged";
                public const string HistoryNameChanged = Prefix.Message + "historyNameChanged";
                public const string HistoryStartTimeChanged = Prefix.Message + "historyStartTimeChanged";
                public const string HistoryEndTimeChanged = Prefix.Message + "historyEndTimeChanged";
                public const string HistoryTeamChanged = Prefix.Message + "historyTeamChanged";
                public const string HistoryRemarkChanged = Prefix.Message + "historyRemarkChanged";
                public const string HistoryMembersChanged = Prefix.Message + "historyMembersChanged";
                public const string HistoryAddMember = Prefix.Message + "historyAddMember";
                public const string HistoryRemoveMember = Prefix.Message + "historyRemoveMember";
                public const string HistoryRoleChanged = Prefix.Message + "historyRoleChanged";
                public const string HistoryAbsent = Prefix.Message + "historyAbsent";
                public const string HistorySwap = Prefix.Message + "historySwap";
                public const string HistoryTransferOut = Prefix.Message + "historyTransferOut";
                public const string HistoryTransferIn = Prefix.Message + "historyTransferIn";
                public const string HistoryReplace = Prefix.Message + "historyReplace";
                public const string StatusActive = Prefix.Message + "statusActive";
                public const string StatusInactive = Prefix.Message + "statusInactive";
                public const string StatusCompleted = Prefix.Message + "statusCompleted";
                public const string StatusCancelled = Prefix.Message + "statusCancelled";
                public const string StatusPending = Prefix.Message + "statusPending";
                public const string StatusUnknown = Prefix.Message + "statusUnknown";
                public const string TimeConflict = Prefix.Message + "timeConflict";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 07/11/2025
        /// Description: TmsDutyScheduleDetail Messages
        /// </summary>
        public class TmsDutyScheduleDetail
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsDutyScheduleDetail.";
                public const string Validation = "lz.validation.tmsDutyScheduleDetail.";
                public const string Exception = "lz.exception.tmsDutyScheduleDetail.";
                public const string Entity = "lz.entity.tmsDutyScheduleDetail.";
            }

            public class Entity
            {
                public const string DutyScheduleId = Prefix.Entity + "dutyScheduleId";
                public const string UserId = Prefix.Entity + "userId";
                public const string Role = Prefix.Entity + "role";
                public const string Status = Prefix.Entity + "status";
                public const string SwappedFromUserId = Prefix.Entity + "swappedFromUserId";
                public const string Reason = Prefix.Entity + "reason";
            }

            public class Validation
            {
                public const string DutyScheduleIdRequired = Prefix.Validation + "dutyScheduleIdRequired";
                public const string UserIdRequired = Prefix.Validation + "userIdRequired";
                public const string RoleInvalid = Prefix.Validation + "roleInvalid";
                public const string StatusInvalid = Prefix.Validation + "statusInvalid";
                public const string TargetUserStatusInvalid = Prefix.Validation + "targetUserStatusInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyExists = Prefix.Exception + "alreadyExists";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 07/11/2025
        /// Description: TmsPatrolTeamMember Messages
        /// </summary>
        public class TmsPatrolTeamMember
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsPatrolTeamMember.";
                public const string Validation = "lz.validation.tmsPatrolTeamMember.";
                public const string Exception = "lz.exception.tmsPatrolTeamMember.";
                public const string Entity = "lz.entity.tmsPatrolTeamMember.";
            }

            public class Entity
            {
                public const string PatrolTeamId = Prefix.Entity + "patrolTeamId";
                public const string UserId = Prefix.Entity + "userId";
                public const string Role = Prefix.Entity + "role";
            }

            public class Validation
            {
                public const string PatrolTeamIdRequired = Prefix.Validation + "patrolTeamIdRequired";
                public const string UserIdRequired = Prefix.Validation + "userIdRequired";
                public const string RoleInvalid = Prefix.Validation + "roleInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyExists = Prefix.Exception + "alreadyExists";
                public const string PatrolTeamNotFound = Prefix.Exception + "patrolTeamNotFound";
            }

            public class Message
            {
                public const string AddSuccess = Prefix.Message + "addSuccess";
                public const string UpdateSuccess = Prefix.Message + "updateSuccess";
                public const string DeleteSuccess = Prefix.Message + "deleteSuccess";
            }
        }

        #region Maintenance Schedule

        /// <summary>
        /// Author: System
        /// Created date: 11/01/2025
        /// Description: TmsMaintenanceGuide Messages
        /// </summary>
        public class TmsMaintenanceGuide
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMaintenanceGuide.";
                public const string Validation = "lz.validation.tmsMaintenanceGuide.";
                public const string Exception = "lz.exception.tmsMaintenanceGuide.";
                public const string Entity = "lz.entity.tmsMaintenanceGuide.";
            }

            public class Entity
            {
                public const string Name = Prefix.Entity + "name";
                public const string EquipmentTypeId = Prefix.Entity + "equipmentTypeId";
                public const string Manufacturer = Prefix.Entity + "manufacturer";
                public const string Frequency = Prefix.Entity + "frequency";
                public const string FrequencyValue = Prefix.Entity + "frequencyValue";
                public const string EstimatedDuration = Prefix.Entity + "estimatedDuration";
                public const string Checklist = Prefix.Entity + "checklist";
                public const string Priority = Prefix.Entity + "priority";
            }

            public class Validation
            {
                public const string NameRequired = Prefix.Validation + "nameRequired";
                public const string EquipmentTypeIdRequired = Prefix.Validation + "equipmentTypeIdRequired";
                public const string FrequencyRequired = Prefix.Validation + "frequencyRequired";
                public const string FrequencyValueRequired = Prefix.Validation + "frequencyValueRequired";
                public const string FrequencyInvalid = Prefix.Validation + "frequencyInvalid";
                public const string PriorityInvalid = Prefix.Validation + "priorityInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyExists = Prefix.Exception + "alreadyExists";
                public const string DeleteFailed = Prefix.Exception + "deleteFailed";
                public const string EquipmentTypeNotFound = Prefix.Exception + "equipmentTypeNotFound";
                public const string DuplicateName = Prefix.Exception + "duplicateName";
            }

            public class Message
            {
                public const string AddSuccess = Prefix.Message + "addSuccess";
                public const string UpdateSuccess = Prefix.Message + "updateSuccess";
                public const string DeleteSuccess = Prefix.Message + "deleteSuccess";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 11/01/2025
        /// Description: TmsMaintenanceSchedule Messages
        /// </summary>
        public class TmsMaintenanceSchedule
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMaintenanceSchedule.";
                public const string Validation = "lz.validation.tmsMaintenanceSchedule.";
                public const string Exception = "lz.exception.tmsMaintenanceSchedule.";
                public const string Entity = "lz.entity.tmsMaintenanceSchedule.";
            }

            public class Entity
            {
                public const string Code = Prefix.Entity + "code";
                public const string Name = Prefix.Entity + "name";
                public const string ScheduledDate = Prefix.Entity + "scheduledDate";
                public const string DueDate = Prefix.Entity + "dueDate";
                public const string Priority = Prefix.Entity + "priority";
                public const string Status = Prefix.Entity + "status";
                public const string ReminderDaysBefore = Prefix.Entity + "reminderDaysBefore";
            }

            public class Validation
            {
                public const string NameRequired = Prefix.Validation + "nameRequired";
                public const string ScheduledDateRequired = Prefix.Validation + "scheduledDateRequired";
                public const string DueDateRequired = Prefix.Validation + "dueDateRequired";
                public const string DueDateBeforeScheduledDate = Prefix.Validation + "dueDateBeforeScheduledDate";
                public const string StatusInvalid = Prefix.Validation + "statusInvalid";
                public const string PriorityInvalid = Prefix.Validation + "priorityInvalid";
                public const string NoEquipment = Prefix.Validation + "noEquipment";
                public const string InvalidStatusTransition = Prefix.Validation + "invalidStatusTransition";
                public const string ScheduleMustBeInProgress = Prefix.Validation + "scheduleMustBeInProgress";
                public const string EquipmentCheckRequiresInProgress = Prefix.Validation + "equipmentCheckRequiresInProgress";
                public const string TaskStatusRequiresInProgress = Prefix.Validation + "taskStatusRequiresInProgress";
                public const string TaskStatusInvalid = Prefix.Validation + "taskStatusInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyExists = Prefix.Exception + "alreadyExists";
                public const string DeleteFailed = Prefix.Exception + "deleteFailed";
                public const string CannotModifyCompletedSchedule = Prefix.Exception + "cannotModifyCompletedSchedule";
                public const string CannotDeleteActiveSchedule = Prefix.Exception + "cannotDeleteActiveSchedule";
                public const string EquipmentNotFound = Prefix.Exception + "equipmentNotFound";
                public const string EquipmentAlreadyInSchedule = Prefix.Exception + "equipmentAlreadyInSchedule";
                public const string ScheduleEquipmentNotFound = Prefix.Exception + "scheduleEquipmentNotFound";
                public const string TaskNotFound = Prefix.Exception + "taskNotFound";
                public const string CannotUpdateCompletedOrCancelled = Prefix.Exception + "cannotUpdateCompletedOrCancelled";
                public const string CannotDeleteInProgress = Prefix.Exception + "cannotDeleteInProgress";
            }

            public class Message
            {
                public const string AddSuccess = Prefix.Message + "addSuccess";
                public const string UpdateSuccess = Prefix.Message + "updateSuccess";
                public const string DeleteSuccess = Prefix.Message + "deleteSuccess";
                public const string StatusChangeSuccess = Prefix.Message + "statusChangeSuccess";
                public const string CompleteSuccess = Prefix.Message + "completeSuccess";
                public const string HistoryCreated = Prefix.Message + "historyCreated";
                public const string HistoryUpdated = Prefix.Message + "historyUpdated";
                public const string HistoryDeleted = Prefix.Message + "historyDeleted";
                public const string HistoryStatusChanged = Prefix.Message + "historyStatusChanged";
                public const string HistoryAddEquipment = Prefix.Message + "historyAddEquipment";
                public const string HistoryRemoveEquipment = Prefix.Message + "historyRemoveEquipment";
                public const string HistoryEquipmentStatusChanged = Prefix.Message + "historyEquipmentStatusChanged";
                public const string HistoryEquipmentChecked = Prefix.Message + "historyEquipmentChecked";
                public const string HistoryEquipmentSkipped = Prefix.Message + "historyEquipmentSkipped";
                public const string HistoryEquipmentPending = Prefix.Message + "historyEquipmentPending";
                public const string HistoryAssignTask = Prefix.Message + "historyAssignTask";
                public const string HistoryRemoveTask = Prefix.Message + "historyRemoveTask";
                public const string HistoryTaskStatusChanged = Prefix.Message + "historyTaskStatusChanged";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 11/01/2025
        /// Description: TmsMaintenanceScheduleEquipment Messages
        /// </summary>
        public class TmsMaintenanceScheduleEquipment
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMaintenanceScheduleEquipment.";
                public const string Validation = "lz.validation.tmsMaintenanceScheduleEquipment.";
                public const string Exception = "lz.exception.tmsMaintenanceScheduleEquipment.";
                public const string Entity = "lz.entity.tmsMaintenanceScheduleEquipment.";
            }

            public class Entity
            {
                public const string MaintenanceScheduleId = Prefix.Entity + "maintenanceScheduleId";
                public const string EquipmentId = Prefix.Entity + "equipmentId";
                public const string MaintenanceGuideId = Prefix.Entity + "maintenanceGuideId";
                public const string Status = Prefix.Entity + "status";
            }

            public class Validation
            {
                public const string MaintenanceScheduleIdRequired = Prefix.Validation + "maintenanceScheduleIdRequired";
                public const string EquipmentIdRequired = Prefix.Validation + "equipmentIdRequired";
                public const string StatusInvalid = Prefix.Validation + "statusInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyExists = Prefix.Exception + "alreadyExists";
                public const string ScheduleNotFound = Prefix.Exception + "scheduleNotFound";
                public const string EquipmentNotFound = Prefix.Exception + "equipmentNotFound";
                public const string GuideNotFound = Prefix.Exception + "guideNotFound";
            }

            public class Message
            {
                public const string AddSuccess = Prefix.Message + "addSuccess";
                public const string UpdateSuccess = Prefix.Message + "updateSuccess";
                public const string DeleteSuccess = Prefix.Message + "deleteSuccess";
                public const string CompleteSuccess = Prefix.Message + "completeSuccess";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 11/01/2025
        /// Description: TmsMaintenanceTask Messages
        /// </summary>
        public class TmsMaintenanceTask
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMaintenanceTask.";
                public const string Validation = "lz.validation.tmsMaintenanceTask.";
                public const string Exception = "lz.exception.tmsMaintenanceTask.";
                public const string Entity = "lz.entity.tmsMaintenanceTask.";
            }

            public class Entity
            {
                public const string MaintenanceScheduleId = Prefix.Entity + "maintenanceScheduleId";
                public const string ScheduleEquipmentId = Prefix.Entity + "scheduleEquipmentId";
                public const string AssignedUserId = Prefix.Entity + "assignedUserId";
                public const string Role = Prefix.Entity + "role";
                public const string Status = Prefix.Entity + "status";
            }

            public class Validation
            {
                public const string MaintenanceScheduleIdRequired = Prefix.Validation + "maintenanceScheduleIdRequired";
                public const string AssignedUserIdRequired = Prefix.Validation + "assignedUserIdRequired";
                public const string RoleInvalid = Prefix.Validation + "roleInvalid";
                public const string StatusInvalid = Prefix.Validation + "statusInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyAssigned = Prefix.Exception + "alreadyAssigned";
                public const string ScheduleNotFound = Prefix.Exception + "scheduleNotFound";
                public const string EquipmentNotFound = Prefix.Exception + "equipmentNotFound";
                public const string UserNotFound = Prefix.Exception + "userNotFound";
            }

            public class Message
            {
                public const string AddSuccess = Prefix.Message + "addSuccess";
                public const string UpdateSuccess = Prefix.Message + "updateSuccess";
                public const string DeleteSuccess = Prefix.Message + "deleteSuccess";
                public const string CompleteSuccess = Prefix.Message + "completeSuccess";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 11/01/2025
        /// Description: TmsMaintenanceAlert Messages
        /// </summary>
        public class TmsMaintenanceAlert
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMaintenanceAlert.";
                public const string Validation = "lz.validation.tmsMaintenanceAlert.";
                public const string Exception = "lz.exception.tmsMaintenanceAlert.";
                public const string Entity = "lz.entity.tmsMaintenanceAlert.";
            }

            public class Entity
            {
                public const string EquipmentId = Prefix.Entity + "equipmentId";
                public const string AlertRuleId = Prefix.Entity + "alertRuleId";
                public const string AlertType = Prefix.Entity + "alertType";
                public const string Status = Prefix.Entity + "status";
                public const string ErrorCount = Prefix.Entity + "errorCount";
                public const string Priority = Prefix.Entity + "priority";
            }

            public class Validation
            {
                public const string EquipmentIdRequired = Prefix.Validation + "equipmentIdRequired";
                public const string AlertTypeInvalid = Prefix.Validation + "alertTypeInvalid";
                public const string StatusInvalid = Prefix.Validation + "statusInvalid";
                public const string PriorityInvalid = Prefix.Validation + "priorityInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string EquipmentNotFound = Prefix.Exception + "equipmentNotFound";
                public const string RuleNotFound = Prefix.Exception + "ruleNotFound";
                public const string AlreadyProcessed = Prefix.Exception + "alreadyProcessed";
            }

            public class Message
            {
                public const string AcknowledgeSuccess = Prefix.Message + "acknowledgeSuccess";
                public const string DismissSuccess = Prefix.Message + "dismissSuccess";
                public const string CreateScheduleSuccess = Prefix.Message + "createScheduleSuccess";
                public const string EmergencyScheduleName = Prefix.Message + "emergencyScheduleName";
                public const string ScheduleFromAlertDescription = Prefix.Message + "scheduleFromAlertDescription";
                public const string EquipmentFromAlertRemark = Prefix.Message + "equipmentFromAlertRemark";
                public const string ManualAlertTitle = Prefix.Message + "manualAlertTitle";
            }
        }

        /// <summary>
        /// Author: System
        /// Created date: 11/01/2025
        /// Description: TmsMaintenanceAlertRule Messages
        /// </summary>
        public class TmsMaintenanceAlertRule
        {
            protected class Prefix
            {
                public const string Message = "lz.message.tmsMaintenanceAlertRule.";
                public const string Validation = "lz.validation.tmsMaintenanceAlertRule.";
                public const string Exception = "lz.exception.tmsMaintenanceAlertRule.";
                public const string Entity = "lz.entity.tmsMaintenanceAlertRule.";
            }

            public class Entity
            {
                public const string Name = Prefix.Entity + "name";
                public const string EquipmentTypeId = Prefix.Entity + "equipmentTypeId";
                public const string ErrorThreshold = Prefix.Entity + "errorThreshold";
                public const string PeriodDays = Prefix.Entity + "periodDays";
                public const string AlertPriority = Prefix.Entity + "alertPriority";
                public const string SuggestedGuideId = Prefix.Entity + "suggestedGuideId";
                public const string IsActive = Prefix.Entity + "isActive";
            }

            public class Validation
            {
                public const string NameRequired = Prefix.Validation + "nameRequired";
                public const string EquipmentTypeIdRequired = Prefix.Validation + "equipmentTypeIdRequired";
                public const string ErrorThresholdRequired = Prefix.Validation + "errorThresholdRequired";
                public const string ErrorThresholdInvalid = Prefix.Validation + "errorThresholdInvalid";
                public const string PeriodDaysRequired = Prefix.Validation + "periodDaysRequired";
                public const string PeriodDaysInvalid = Prefix.Validation + "periodDaysInvalid";
                public const string AlertPriorityInvalid = Prefix.Validation + "alertPriorityInvalid";
            }

            public class Exception
            {
                public const string NotFound = Prefix.Exception + "notFound";
                public const string AlreadyExists = Prefix.Exception + "alreadyExists";
                public const string DeleteFailed = Prefix.Exception + "deleteFailed";
                public const string EquipmentTypeNotFound = Prefix.Exception + "equipmentTypeNotFound";
                public const string GuideNotFound = Prefix.Exception + "guideNotFound";
            }

            public class Message
            {
                public const string AddSuccess = Prefix.Message + "addSuccess";
                public const string UpdateSuccess = Prefix.Message + "updateSuccess";
                public const string DeleteSuccess = Prefix.Message + "deleteSuccess";
                public const string ActivateSuccess = Prefix.Message + "activateSuccess";
                public const string DeactivateSuccess = Prefix.Message + "deactivateSuccess";
            }
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Lỗi khi TMS đọc dữ liệu VDS in-process qua message bus (ma trận, báo cáo lưu lượng/thống kê).
        /// Created date: 13/10/2026
        /// </summary>
        public class VdsQuery
        {
            protected class Prefix
            {
                public const string Exception = "lz.exception.vdsQuery.";
            }

            public class Exception
            {
                /// <summary>Không đọc được dữ liệu VDS để dựng ma trận.</summary>
                public const string MatrixUnavailable = Prefix.Exception + "matrixUnavailable";

                /// <summary>Không đọc được dữ liệu VDS để lập báo cáo.</summary>
                public const string ReportDataUnavailable = Prefix.Exception + "reportDataUnavailable";

                /// <summary>Không đọc được thống kê VDS để lập báo cáo.</summary>
                public const string ReportStatisticUnavailable = Prefix.Exception + "reportStatisticUnavailable";
            }
        }


        #endregion
    }
}
