using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsIncident")]
    public class TmsIncident : EntityTenant
    {
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public string? EventTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public decimal? KmNumber { get; set; }
        public decimal? MetNumber { get; set; }
        public string? Location { get; set; }
        public string? InfluenceScope { get; set; }
        public int? InjuredNumber { get; set; }
        public int? VehicleNumber { get; set; }
        public string? State { get; set; }
        public string? Description { get; set; }
        public string? Source { get; set; }
    }
}
