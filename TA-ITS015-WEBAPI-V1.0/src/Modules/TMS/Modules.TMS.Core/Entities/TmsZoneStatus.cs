using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsZoneStatus")]
    public class TmsZoneStatus : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ZoneId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? AverageSpeed { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? Condition { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? UpdateTime { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? DetectWay { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? DetectStop { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? DetectSlow { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? DetectOthers { get; set; }
    }
}
