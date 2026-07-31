using SqlSugar;

namespace TA_ShareData_WorkerService.Core.Entities
{
    public static class EntityConst
    {
        public const int KeyFieldLength = 64;
        public const int Length32 = 32;
        public const int Length64 = 64;
        public const int Length128 = 128;
        public const int Length256 = 256;
        public const int Length512 = 512;
        public const int Length1024 = 1024;
    }

    public abstract class EntityTenant
    {
        [SugarColumn(IsPrimaryKey = true, Length = EntityConst.KeyFieldLength)]
        public string ID { get; set; } = Guid.NewGuid().ToString("N");

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? TenantId { get; set; } = "1";

        [SugarColumn(IsNullable = true)]
        public DateTime? CreateTime { get; set; } = DateTime.Now;

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? CreateUId { get; set; } = "1";

        [SugarColumn(IsNullable = true)]
        public DateTime? UpdateTime { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? UpdateUId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? RowStatus { get; set; } = "1";

        [SugarColumn(IsNullable = true)]
        public bool? IsDelete { get; set; }
    }
}
