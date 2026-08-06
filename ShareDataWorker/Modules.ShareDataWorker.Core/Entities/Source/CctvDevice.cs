using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("CctvDevice")]
    public class CctvDevice : EntityTenant
    {
        public string? DeviceId { get; set; }
        public string? Ip { get; set; }
        public string? Name { get; set; }
        public string? SnapshotUrl { get; set; }
        public DateTime? SnapshotTime { get; set; }
        public int? DeviceState { get; set; }
    }
}
