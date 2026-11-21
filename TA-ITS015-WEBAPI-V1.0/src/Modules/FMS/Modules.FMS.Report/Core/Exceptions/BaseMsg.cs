using Shared.DTO.Constants.Localization;

namespace Modules.FMS.Report.Core.Exceptions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 12/03/2025
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
                internal const string Idx = Prefix.Label + "idx"; // STT
                internal const string Page = Prefix.Label + "page"; // Trang
                internal const string Total = Prefix.Label + "total"; // Tổng
                internal const string Amount = Prefix.Label + "amount"; // Số lượng

                internal const string Time = Prefix.Label + "time"; // Thời gian
                internal const string Day = Prefix.Label + "day"; // Ngày
                internal const string Month = Prefix.Label + "month"; // Tháng
                internal const string Year = Prefix.Label + "year"; // Năm
            }
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: RptEquipmentHistory Msg
        /// Created date: 12/03/2025
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
    }
}
