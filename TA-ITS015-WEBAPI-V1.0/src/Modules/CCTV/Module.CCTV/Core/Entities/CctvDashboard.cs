using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Modules.CCTV.Core.Entities
{
    [SugarTable("CctvDashboard")]
    public class CctvDashboard : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Tags { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
        public string? Remark { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Config { get; set; }
    }
}