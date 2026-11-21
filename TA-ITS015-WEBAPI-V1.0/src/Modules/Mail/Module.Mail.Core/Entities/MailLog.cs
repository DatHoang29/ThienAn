using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Modules.Mail.Core.Constants;

namespace Modules.Mail.Core.Entities
{
    /// <summary>
    /// Description: History Emails
    /// Created date: 2026-08-14
    /// </summary>
    [SugarTable("MailLog")]
    public class MailLog : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ScheduleId { get; set; }

        /// <summary>Nguồn kích hoạt: theo lịch hay bấm tay.</summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.TriggerType? TriggerType { get; set; }

        /// <summary>Thời điểm thực hiện lần gửi.</summary>
        [SugarColumn(IsNullable = false)]
        public DateTime OccurredAt { get; set; }

        /// <summary>
        /// Đầu kỳ dữ liệu của báo cáo đã gửi.
        /// KHÔNG suy được từ OccurredAt: chạy 01/01/2027 là báo cáo của năm 2026.
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? PeriodFrom { get; set; }

        /// <summary>Cuối kỳ dữ liệu.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? PeriodTo { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ReportCode { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.SuccessEnums? Status { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Recipients { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Cc { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length1024)]
        public string? Bcc { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Subject { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? TemplateCode { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? FileName { get; set; }
        [SugarColumn(IsNullable = true)]
        public long? ByteSize { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? Attempts { get; set; }

        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
        public string? ErrorMessage { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
