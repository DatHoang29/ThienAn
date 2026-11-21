using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Modules.CCTV.Core.Entities
{
    [SugarTable("CctvDashboardItem")]
    public class CctvDashboardItem : EntityTenant
    {
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength)]
        public string DashboardId { get; set; } = null!;

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? X { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Y { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? W { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? H { get; set; }
    }
}