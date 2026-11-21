using Shared.DTO.Constants.Localization;

namespace Modules.VMS.Report.Core.Exceptions
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Quản lý localization key cho phân hệ báo cáo VMS
    /// Created date: 07/10/2026
    /// </summary>
    internal class BaseMsg : BaseLocaleManager
    {
        internal class RptBase
        {
            protected class Prefix
            {
                internal const string Label = "lz.label.base.";
            }

            internal class Label
            {
                internal const string Idx = Prefix.Label + "idx";
                internal const string Page = Prefix.Label + "page";
                internal const string Total = Prefix.Label + "total";
                internal const string Amount = Prefix.Label + "amount";
                internal const string Time = Prefix.Label + "time";
                internal const string Day = Prefix.Label + "day";
                internal const string Month = Prefix.Label + "month";
                internal const string Year = Prefix.Label + "year";
            }
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Localization key cho báo cáo lịch sử thiết bị
        /// Created date: 07/10/2026
        /// </summary>
        internal class RptEquipmentHistory
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptEquipmentHistory.";
                internal const string Validation = "lz.validation.rptEquipmentHistory.";
                internal const string Message = "lz.message.rptEquipmentHistory.";
                internal const string Entity = "lz.entity.rptEquipmentHistory.";
                internal const string Label = "lz.label.rptEquipmentHistory.";
            }

            internal class Entity
            {
                internal const string Name = Prefix.Entity + "name";
                internal const string EquipmentType = Prefix.Entity + "equipmentType";
                internal const string OldState = Prefix.Entity + "oldState";
                internal const string NewState = Prefix.Entity + "newState";
                internal const string Remark = Prefix.Entity + "remark";
            }

            internal class Label
            {
                internal const string RptTitle = Prefix.Label + "rptTitle";
            }
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Localization key cho báo cáo lịch sử điều khiển thiết bị VMS/LCS
        /// Created date: 07/10/2026
        /// </summary>
        internal class RptOperationLogHistory
        {
            internal class Prefix
            {
                internal const string Exception = "lz.exception.rptOperationLogHistory.";
                internal const string Validation = "lz.validation.rptOperationLogHistory.";
                internal const string Message = "lz.message.rptOperationLogHistory.";
                internal const string Entity = "lz.entity.rptOperationLogHistory.";
                internal const string Label = "lz.label.rptOperationLogHistory.";
            }

            internal class Entity
            {
                internal const string ExecuteTime = Prefix.Entity + "executeTime";
                internal const string Equipment = Prefix.Entity + "equipment";
                internal const string BeforeData = Prefix.Entity + "beforeData";
                internal const string AfterData = Prefix.Entity + "afterData";
                internal const string Status = Prefix.Entity + "status";
                internal const string ExecuteBy = Prefix.Entity + "executeBy";
            }

            internal class Label
            {
                internal const string RptTitle = Prefix.Label + "rptTitle";
                internal const string StateSuccess = Prefix.Label + "stateSuccess";
                internal const string StateFailed = Prefix.Label + "stateFailed";
                internal const string StateError = Prefix.Label + "stateError";
                internal const string StatePending = Prefix.Label + "statePending";
                internal const string StateProcessing = Prefix.Label + "stateProcessing";
            }
        }
    }
}
