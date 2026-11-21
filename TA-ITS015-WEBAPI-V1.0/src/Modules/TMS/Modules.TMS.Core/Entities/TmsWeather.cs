using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    [SugarTable("TmsWeather")]
    public class TmsWeather : EntityTenant
    {
        //TmsWeather {
        // ID varchar(64) pk
        // RefId varchar(64) null
        // LocationDetail string(128) null
        // Temperature float null
        // Hudmidity float null
        // WindDirection varchar(32) null
        // WindSpeed float null
        // Rain float null
        // RainHour float null
        // Foresight float null
        // TimeDetect datetime null
        // Description string(128) null
        // ShortDescription string(32) null
        // Type varchar(16) null
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? RefId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? LocationDetail { get; set; }

        [SugarColumn(IsNullable = true)]
        public float? Temperature { get; set; }

        [SugarColumn(IsNullable = true)]
        public float? Hudmidity { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? WindDirection { get; set; }

        [SugarColumn(IsNullable = true)]
        public float? WindSpeed { get; set; }

        [SugarColumn(IsNullable = true)]
        public float? Rain { get; set; }

        [SugarColumn(IsNullable = true)]
        public float? RainHour { get; set; }

        [SugarColumn(IsNullable = true)]
        public float? Foresight { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? TimeDetect { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Description { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? ShortDescription { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string? Type { get; set; }
    }
}