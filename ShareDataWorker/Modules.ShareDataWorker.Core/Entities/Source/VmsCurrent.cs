using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("VmsCurrent")]
    public class VmsCurrent : EntityTenant
    {
        public string? EquipmentId { get; set; }
        public string? Name { get; set; }
        public string? RowData { get; set; }
        public string? Url { get; set; }
        public string? Size { get; set; }
        public int? Priority { get; set; }
        public DateTime? ExecutedDate { get; set; }
    }
}
