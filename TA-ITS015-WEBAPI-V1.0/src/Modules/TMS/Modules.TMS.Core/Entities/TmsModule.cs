using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsModules")]
    public class TmsModule : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Key quy định")]
        public string KeyName { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Tên hiển thị")]
        public string Name { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Remark { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string PluginId { get; set; }



    }
}
