using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsEquipment")]
    public class TmsEquipment : EntityTenant
    {
        public string? Ip { get; set; }
        public string? Code { get; set; }
        public decimal? KmNumber { get; set; }
        public decimal? MetNumber { get; set; }
        public string? DirectionId { get; set; }
        public string? LaneId { get; set; }
    }
}
