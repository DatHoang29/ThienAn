using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TollStation")]
    public class TollStation : EntityTenant
    {
        public string StationId { get; set; } = null!;
        public string? Name { get; set; }
    }
}
