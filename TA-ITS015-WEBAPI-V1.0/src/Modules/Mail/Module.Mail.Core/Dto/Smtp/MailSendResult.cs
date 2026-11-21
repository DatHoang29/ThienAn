namespace Modules.Mail.Core.Dto.Smtp
{
    /// <summary>
    /// Description: Kết quả một lần gửi mail.
    /// Created date: 2026-08-13
    /// </summary>
    public class MailSendResult
    {
        public bool Success { get; set; }

        /// <summary>Thông báo lỗi tiếng Việt, để module gọi ghi thẳng vào bảng log của mình.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Số lần đã thử gửi, tính cả lần đầu.</summary>
        public int Attempts { get; set; }

        /// <summary>Dung lượng tệp đính kèm thực tế (byte), 0 nếu không có.</summary>
        public long AttachmentBytes { get; set; }

        public static MailSendResult Ok(int attempts, long bytes = 0)
            => new() { Success = true, Attempts = attempts, AttachmentBytes = bytes };

        public static MailSendResult Fail(string message, int attempts = 0)
            => new() { Success = false, ErrorMessage = message, Attempts = attempts };
    }
}
