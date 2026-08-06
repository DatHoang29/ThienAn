using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsSignalLog")]
    public class TmsSignalLog : EntityTenant
    {
        public string? NewData { get; set; }
        public string? State { get; set; }
    }
}
