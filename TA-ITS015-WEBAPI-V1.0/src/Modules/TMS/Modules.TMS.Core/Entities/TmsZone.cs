using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsZone")]
    public class TmsZone : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Name")]
        public string? Name { get; set; }


        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "ParentID")]
        public string? Pid { get; set; } = "0";

        [SugarColumn(IsNullable = true)]
        public int? FromKmNumber { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? FromMetNumber { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? ToKmNumber { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? ToMetNumber { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? LaneId { get; set; } //Sử dụng cho direction

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        [SugarColumn(IsIgnore = true)]
        public List<TmsZone> Children { get; set; }
        [SugarColumn(IsNullable = true)]
        public float? MinSpeed { get; set; } //Toc do toi thieu

        [SugarColumn(IsNullable = true)]
        public float? MaxSpeed { get; set; } // Toc do toi da

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Location { get; set; } // vị trí tuyen /ham/ tuyen phu/... trong bang config
    }
}
