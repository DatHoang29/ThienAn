using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Modules.Mail.Core.Constants;

namespace Modules.Mail.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Một báo cáo đính kèm trong lịch gửi email. Mỗi dòng sinh ra một tệp trong thư.
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("MailScheduleItem")]
    public class MailScheduleItem : EntityTenant
    {
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength)]
        public string ScheduleId { get; set; } = null!;
        [SugarColumn(IsNullable = false, Length = EntityConst.Length64)]
        public string ReportCode { get; set; } = null!;
        [SugarColumn(IsNullable = false, Length = EntityConst.Length16)]
        public string PeriodType { get; set; } = null!;
        [SugarColumn(IsNullable = true)]
        public int PeriodOffset { get; set; }

        [SugarColumn(IsNullable = false, Length = EntityConst.Length8)]
        public string Format { get; set; } = MailConst.FileFormat.Pdf;
        [SugarColumn(IsNullable = true, ColumnDataType = "nvarchar(max)")]
        public string? ParamsJson { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}
