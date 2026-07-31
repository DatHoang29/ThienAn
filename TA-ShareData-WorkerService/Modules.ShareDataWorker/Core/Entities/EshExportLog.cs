using SqlSugar;
using TA_ShareData_WorkerService.Core.Enums;

namespace TA_ShareData_WorkerService.Core.Entities
{
    [SugarTable("EshExportLog")]
    public class EshExportLog : EntityTenant
    {
        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? MappingId { get; set; }

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? SubscriptionId { get; set; }

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? PartnerId { get; set; }

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? DatatypeId { get; set; } = EshEnums.DataFormat.Json;

        public DateTime ExportedAt { get; set; } = DateTime.Now;

        public long RecordCount { get; set; } = 0;

        public long ByteSize { get; set; } = 0;

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? FilePath { get; set; }

        [SugarColumn(Length = EntityConst.Length128, IsNullable = true)]
        public string? Hash { get; set; }

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? Status { get; set; } = EshEnums.ExportStatus.Success;

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? ErrorMessage { get; set; }

        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? CreatedBy { get; set; }
    }
}
