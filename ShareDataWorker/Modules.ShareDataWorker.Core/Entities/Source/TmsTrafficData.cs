using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsTrafficData")]
    public class TmsTrafficData : EntityTenant
    {
        public string? EquipmentId { get; set; }
        public DateTime? DetectTime { get; set; }
        public string? Type { get; set; }
        public string? LicensePlate { get; set; }
        public decimal? Speed { get; set; }
        public string? Lane { get; set; }
        public string? Direction { get; set; }
        public string? Location { get; set; }
        public decimal? Height { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
    }
}
