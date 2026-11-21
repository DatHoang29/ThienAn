using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    [SugarTable("TmsMapDetail")]
    public class TmsMapDetail : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string MapLocationId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string RefId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.ConfigFieldLength)]
        public string RefType { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string CoordY { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string CoordX { get; set; }
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString)]
        public string Coordinates { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Remark { get; set; }


    }
}
