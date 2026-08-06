using SqlSugar;
using Modules.ShareDataWorker.Core.Constants;

namespace Modules.ShareDataWorker.Core.Entities
{
    public abstract class EntityTenant
    {
        [SugarColumn(IsPrimaryKey = true, Length = EntityConst.KeyFieldLength)]
        public virtual string ID { get; set; } = Guid.NewGuid().ToString("N");

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public virtual string? TenantId { get; set; } = "1";

        [SugarColumn(IsNullable = true)]
        public virtual DateTime? CreateTime { get; set; } = DateTime.Now;

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public virtual string? CreateUId { get; set; } = "1";

        [SugarColumn(IsNullable = true)]
        public virtual DateTime? UpdateTime { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public virtual string? UpdateUId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public virtual string? RowStatus { get; set; } = "1";

        [SugarColumn(IsNullable = true)]
        public virtual bool? IsDelete { get; set; }
    }
}
