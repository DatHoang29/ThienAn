using Modules.Mail.Core.Entities;

namespace Modules.Mail.Core.Dto.TemplateMail
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin mẫu email (danh sách / chi tiết).
    /// Created date: 12/10/2026
    /// </summary>
    public class RptEmailTemplateOutput : MailTemplate
    {
        /// <summary>Nội dung HTML đọc từ file FilePath.</summary>
        [SugarColumn(IsIgnore = true)]
        public string? Content { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO thông tin mẫu email (phân trang)
    /// Created date: 12/10/2026
    /// </summary>
    public class RptPageEmailTemplateOutput : MailTemplate { }
}
