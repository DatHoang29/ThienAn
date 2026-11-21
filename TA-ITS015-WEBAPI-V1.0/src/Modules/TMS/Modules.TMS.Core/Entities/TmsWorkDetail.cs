using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsWorkDetail")]
    public class TmsWorkDetail : EntityTenant
    {

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128, ColumnDescription = "Tên hiển thị")]
        public string Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string WorkType { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string WorkMode { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string ControlType { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Remark { get; set; }

    }
}
