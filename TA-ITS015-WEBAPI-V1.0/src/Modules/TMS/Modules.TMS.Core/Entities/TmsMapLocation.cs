using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsMapLocation")]
    public class TmsMapLocation : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string MapId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string GeoJson { get; set; }

        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Remark { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string Config { get; set; }
    }
}
