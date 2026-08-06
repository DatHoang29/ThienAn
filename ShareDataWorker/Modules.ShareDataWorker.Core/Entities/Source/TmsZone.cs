using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsZone")]
    public class TmsZone : EntityTenant
    {
        public string? Name { get; set; }
        public decimal? FromKmNumber { get; set; }
        public decimal? FromMetNumber { get; set; }
        public decimal? ToKmNumber { get; set; }
        public decimal? ToMetNumber { get; set; }
        public string? LaneId { get; set; }
        public decimal? MaxSpeed { get; set; }
    }
}
