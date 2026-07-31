using SqlSugar;

namespace TA_ShareData_WorkerService.Core.Entities
{
    [SugarTable("EshDataSource")]
    [SugarIndex("UX_EshDataSource_Code", "Code", OrderByType.Asc, "IsDelete", OrderByType.Asc, true)]
    public class EshDataSource : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Code { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? Kind { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? DbRef { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Table { get; set; }

        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
        public string? ColumnsJson { get; set; }

        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
        public string? QueryText { get; set; }

        public int TopN { get; set; } = 50;
    }
}
