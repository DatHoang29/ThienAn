using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsSignalLog")]
    public class TmsSignalLog : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Dữ liệu cũ ")]
        public string OldData { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Dữ liệu mới ")]
        public string NewData { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "đường dẫn hình ảnh cũ ")]
        public string OldUrl { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "đường dẫn hình ảnh mới ")]
        public string NewUrl { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? OldFromDate { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? NewFromDate { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? OldToDate { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? NewToDate { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? OldEndDate { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? NewEndDate { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? TimeToLive { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32, ColumnDescription = "")]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "")]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public int? Priority { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public string? ExecuteBy { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "")]
        public DateTime? ExecuteTime { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "")]
        public string? Remark { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "")]
        public string? IncidentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? State { get; set; }
    }
}
