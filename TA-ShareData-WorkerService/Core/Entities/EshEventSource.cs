using SqlSugar;
using TA_ShareData_WorkerService.Core.Enums;

namespace TA_ShareData_WorkerService.Core.Entities
{
    [SugarTable("EshEventSource")]
    [SugarIndex("UX_EshEventSource_Code", "Code", OrderByType.Asc, "IsDelete", OrderByType.Asc, true)]
    public class EshEventSource : EntityTenant
    {
        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? Code { get; set; }

        [SugarColumn(Length = EntityConst.Length256, IsNullable = true)]
        public string? Name { get; set; }

        [SugarColumn(Length = EntityConst.Length256, IsNullable = true)]
        public string? Subject { get; set; }

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string DatatypeCode { get; set; } = EshEnums.EventFormat.Json;

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? Description { get; set; }
    }
}
