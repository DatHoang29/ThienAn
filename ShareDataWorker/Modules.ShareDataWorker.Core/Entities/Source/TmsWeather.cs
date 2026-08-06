using SqlSugar;

namespace Modules.ShareDataWorker.Core.Entities.Source
{
    [SugarTable("TmsWeather")]
    public class TmsWeather : EntityTenant
    {
        public string? RefId { get; set; }
        public string? LocationDetail { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Hudmidity { get; set; }
        public decimal? WindSpeed { get; set; }
        public string? WindDirection { get; set; }
        public decimal? Rain { get; set; }
        public decimal? RainHour { get; set; }
        public decimal? Foresight { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public DateTime? TimeDetect { get; set; }
    }
}
