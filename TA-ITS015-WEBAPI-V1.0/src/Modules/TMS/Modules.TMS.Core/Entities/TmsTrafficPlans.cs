using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsTrafficPlan")]
    public class TmsTrafficPlans : EntityTenant
    {

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Tên hiển thị")]
        public string Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string EventTypeId { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Remark { get; set; }

        /// <summary>
        /// Chế độ thực thi: Manual, Automatic, Both. Xem TrafficPlanConst.ExecutionMode.
        /// Mặc định Manual để tương thích dữ liệu cũ.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.ConfigFieldLength, DefaultValue = "Manual", ColumnDescription = "Chế độ thực thi: Manual/Automatic/Both")]
        public string ExecutionMode { get; set; } = "Manual";

        /// <summary>
        /// Version kịch bản, tăng khi thay đổi cấu trúc/payload đã được một automation run snapshot.
        /// Author: SonTH - 24/07/2026
        /// </summary>
        [SugarColumn(IsNullable = false, DefaultValue = "1", ColumnDescription = "Version kịch bản")]
        public int PlanVersion { get; set; } = 1;


    }
}
