using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsZoneStatus")]
    public class TmsZoneStatus : EntityTenant
    {
        public string ZoneId { get; set; } = null!;
        public string? AverageSpeed { get; set; }
        public string? Condition { get; set; }
    }
}
