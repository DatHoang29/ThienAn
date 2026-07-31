using SqlSugar;

namespace TA_ShareData_WorkerService.Core.Entities
{
    [SugarTable("EshFieldMapping")]
    public class EshFieldMapping : EntityTenant
    {
        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? MappingProfileId { get; set; }

        [SugarColumn(Length = EntityConst.Length128, IsNullable = true)]
        public string? SourceKey { get; set; }

        [SugarColumn(Length = EntityConst.Length128, IsNullable = true)]
        public string? TargetKey { get; set; }

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? Expression { get; set; }

        [SugarColumn(Length = EntityConst.Length256, IsNullable = true)]
        public string? DefaultValue { get; set; }

        public bool IsRequired { get; set; } = false;

        public int OrderNo { get; set; } = 0;
    }
}
