using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsEventType")]
    public class TmsEventType : EntityTenant
    {
        public string? Name { get; set; }
    }
}
