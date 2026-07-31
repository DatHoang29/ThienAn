using SqlSugar;
using TA_ShareData_WorkerService.Core.Enums;

namespace TA_ShareData_WorkerService.Core.Entities
{
    [SugarTable("EshSystemLog")]
    public class EshSystemLog : EntityTenant
    {
        public DateTime LoggedAt { get; set; } = DateTime.Now;

        [SugarColumn(Length = EntityConst.Length32, IsNullable = true)]
        public string? Severity { get; set; } = EshEnums.LogSeverity.Warning;

        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? Source { get; set; }

        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? AlertCode { get; set; }

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? PartnerId { get; set; }

        [SugarColumn(Length = EntityConst.KeyFieldLength, IsNullable = true)]
        public string? SubscriptionId { get; set; }

        [SugarColumn(Length = EntityConst.Length512, IsNullable = true)]
        public string? Message { get; set; }

        public bool IsAcknowledged { get; set; } = false;

        [SugarColumn(Length = EntityConst.Length64, IsNullable = true)]
        public string? AcknowledgedBy { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? AcknowledgedAt { get; set; }
    }
}
