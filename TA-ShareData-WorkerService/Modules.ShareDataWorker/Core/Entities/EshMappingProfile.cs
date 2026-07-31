using SqlSugar;
using TA_ShareData_WorkerService.Core.Enums;

namespace TA_ShareData_WorkerService.Core.Entities
{
    [SugarTable("EshMappingProfile")]
    [SugarIndex("UX_EshMappingProfile_Code", "Code", OrderByType.Asc, "IsDelete", OrderByType.Asc, true)]
    public class EshMappingProfile : EntityTenant
    {
        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? Code { get; set; }

        [SugarColumn(Length = EntityConst.Length256, IsNullable = true)]
        public string? Name { get; set; }

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? PartnerId { get; set; }

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? DatatypeId { get; set; } = EshEnums.DataFormat.Json;

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? Direction { get; set; } = EshEnums.SubDirection.Outbound;

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? DataSourceId { get; set; }

        public bool IsActive { get; set; } = true;

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? Remark { get; set; }
    }
}
