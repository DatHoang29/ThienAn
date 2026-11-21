using Modules.Mail.Core.Dto.Smtp;
using Modules.Mail.Infrastructure.Services.Mail.Config;
using Modules.Mail.Infrastructure.Services.Mail.Log;
using Modules.Mail.Infrastructure.Smtp;

namespace Modules.Mail.Controllers.TestMail
{
    /// <summary>
    /// Description: Endpoint kiểm chứng cấu hình SMTP.
    ///              Mục đích: chứng minh máy chủ tới được bưu điện và giấy tờ hợp lệ,
    ///              không cần chờ có lịch hay báo cáo thật.
    /// Created date: 2026-08-13
    /// </summary>
    public class TestMailController : BaseController
    {
        private readonly IMailSender _mailSender;
        private readonly MailConfigService _configService;
        private readonly RptEmailLogService _logService;

        public TestMailController(IMailSender mailSender, MailConfigService configService, RptEmailLogService logService)
        {
            _mailSender = mailSender;
            _configService = configService;
            _logService = logService;
        }

        /// <summary>
        /// Xem cấu hình mail đang nạp được. Mật khẩu chỉ hiện độ dài, không hiện nội dung.
        /// </summary>
        [ApiDescriptionSettings(Name = "Config"), HttpGet]
        [DisplayName("Xem cấu hình mail")]
        public async Task<object> GetConfig()
        {
            var cfg = await _configService.GetAsync();
            return new
            {
                cfg.Enable,
                cfg.Host,
                cfg.Port,
                cfg.SecureSocketOptions,
                cfg.UserName,
                PasswordLength = cfg.Password?.Length ?? 0,
                cfg.RequireAuthentication,
                cfg.FromName,
                cfg.SubjectPrefix,
                cfg.MaxRecipientsPerMail,
                cfg.MaxAttachmentMb,
                cfg.TimeoutSeconds,
                cfg.RetryCount,
                cfg.RetryDelaySeconds,
                cfg.BccAudit,
                cfg.TestRecipient,
            };
        }

        /// <summary>
        /// Gửi một mail thử. Bỏ trống toEmail thì gửi về mail_test_recipient (SysOpConfig).
        /// </summary>
        [ApiDescriptionSettings(Name = "Send"), HttpPost]
        [DisplayName("Gửi mail thử")]
        public async Task<MailSendResult> Send(TestMailInput request)
        {
            // Không chỉ kiểm tra rỗng mà kiểm tra cả định dạng: Swagger tự điền body mẫu
            // "toEmail": "string", chuỗi đó không rỗng nên sẽ bị đem đi gửi thật.
            var to = _mailSender.IsValidEmail(request.ToEmail)
                ? request.ToEmail
                : (await _configService.GetAsync()).TestRecipient;

            if (string.IsNullOrWhiteSpace(to))
                return MailSendResult.Fail("Chưa truyền toEmail và cũng chưa cấu hình mail_test_recipient (SysOpConfig).");

            MailAttachment? attachment = null;

            if (request.WithAttachment)
            {
                // Phải có global:: vì namespace Modules.System che mất System gốc.
                attachment = new MailAttachment(
                    $"test_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    global::System.Text.Encoding.UTF8.GetBytes(
                        $"Tep dinh kem thu nghiem.\r\nSinh luc: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"));
            }

            var body =
                "<p>Đây là email kiểm thử từ Hệ thống giám sát giao thông.</p>" +
                $"<p>Thời điểm gửi: <b>{DateTime.Now:dd/MM/yyyy HH:mm:ss}</b></p>" +
                "<p>Nhận được thư này nghĩa là cấu hình SMTP đã đúng.</p>";

            var subject = request.Subject ?? "Kiểm thử gửi mail tự động";

            var result = await _mailSender.SendAsync(
                MailRecipients.ToOnly(new[] { to }),
                subject,
                body,
                attachment == null ? null : new[] { attachment });

            // Nhật ký gửi thử (TriggerType.Test, không thuộc lịch)
            await _logService.WriteTestAsync(to, subject, result, attachment?.FileName, attachment?.Content.LongLength ?? 0);
            return result;
        }
    }

    /// <summary>
    /// Description: Tham số gửi mail thử.
    /// Created date: 2026-08-13
    /// </summary>
    public class TestMailInput
    {
        /// <summary>Để trống thì gửi về mail_test_recipient (SysOpConfig).</summary>
        public string? ToEmail { get; set; }

        public string? Subject { get; set; }

        /// <summary>true = kèm một tệp .txt nhỏ để kiểm chứng luồng đính kèm.</summary>
        public bool WithAttachment { get; set; }
    }
}
