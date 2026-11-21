using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsMap")]
    public class TmsMap : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string Name { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256, ColumnDescription = "Đường dẫn hình ảnh ")]
        public string Url { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? Width { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? Height { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Type { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Mode { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? IsDefault { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Remark { get; set; }


    }
}
