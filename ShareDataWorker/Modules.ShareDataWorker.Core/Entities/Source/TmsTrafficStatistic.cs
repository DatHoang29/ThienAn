using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsTrafficStatistic")]
    public class TmsTrafficStatistic : EntityTenant
    {
        [SugarColumn(ColumnName = "EquipmentId")]
        public string? ZoneId { get; set; }
        public int? TotalVehicleNumber { get; set; }
    }
}
