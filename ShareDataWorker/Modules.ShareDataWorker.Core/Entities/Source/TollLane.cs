using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TollLane")]
    public class TollLane : EntityTenant
    {
        public string LaneId { get; set; } = null!;
        public string? Name { get; set; }
    }
}
