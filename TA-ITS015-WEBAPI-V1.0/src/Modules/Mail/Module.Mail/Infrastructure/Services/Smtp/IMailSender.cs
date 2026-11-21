using Modules.Mail.Core.Dto.Smtp;

namespace Modules.Mail.Infrastructure.Smtp
{
    /// <summary>
    /// Description: Gửi mail kèm tệp đính kèm. Hạ tầng dùng chung, không biết gì về báo cáo hay lịch.
    /// Created date: 2026-08-13
    /// </summary>
    public interface IMailSender
    {
        /// <summary>Kiểm tra định dạng địa chỉ mail.</summary>
        bool IsValidEmail(string? email);

        /// <summary>
        /// Gửi một email có To / CC / BCC. Địa chỉ sai định dạng bị loại (ghi log); To phải còn ít nhất một địa chỉ.
        /// Tệp có ContentId là ảnh nội tuyến (cid:), còn lại là tệp đính kèm.
        /// </summary>
        Task<MailSendResult> SendAsync(
            MailRecipients recipients,
            string subject,
            string bodyHtml,
            IEnumerable<MailAttachment>? attachments = null,
            CancellationToken cancellationToken = default);
    }
}
