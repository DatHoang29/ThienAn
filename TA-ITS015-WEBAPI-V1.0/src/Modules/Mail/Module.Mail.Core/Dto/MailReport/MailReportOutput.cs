using Modules.Mail.Core.Entities;

namespace Modules.Mail.Core.Dto.EmailReport
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin báo cáo gửi email (danh sách / chi tiết)
    /// Created date: 01/10/2026
    /// </summary>
    public class MailReportOutput : MailReport
    {
        [SugarColumn(IsIgnore = true)]
        public string? PeriodType { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin báo cáo gửi email (phân trang)
    /// Created date: 01/10/2026
    /// </summary>
    public class MailPageReportOutput : MailReport { }
}
