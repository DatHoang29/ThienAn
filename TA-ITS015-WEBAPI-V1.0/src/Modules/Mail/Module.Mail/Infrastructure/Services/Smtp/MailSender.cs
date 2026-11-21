using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text.RegularExpressions;
using Modules.Mail.Core.Dto.Smtp;
using Modules.Mail.Core.Helper;
using Modules.Mail.Infrastructure.Services.Mail.Config;

namespace Modules.Mail.Infrastructure.Smtp
{
    /// <summary>
    /// Description: Cài đặt IMailSender bằng MailKit.
    /// Created date: 2026-08-13
    /// </summary>
    public class MailSender : IMailSender, IScoped
    {
        private static readonly Regex EmailPattern = new(
            @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
            RegexOptions.Compiled);

        private readonly MailConfigService _configService;
        private readonly ILogger<MailSender> _logger;

        /// <summary>Cấu hình đọc mỗi lần gửi qua MailConfigService — sửa cấu hình không phải khởi động lại API.</summary>
        public MailSender(MailConfigService configService, ILogger<MailSender> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        public bool IsValidEmail(string? email)
            => !string.IsNullOrWhiteSpace(email) && EmailPattern.IsMatch(email.Trim());

        public async Task<MailSendResult> SendAsync(
            MailRecipients recipients,
            string subject,
            string bodyHtml,
            IEnumerable<MailAttachment>? attachments = null,
            CancellationToken cancellationToken = default)
        {
            var cfg = await _configService.GetAsync();

            if (!cfg.Enable)
                return MailSendResult.Fail("Chức năng gửi mail đang tắt (SysOpConfig mail_enable).");

            if (string.IsNullOrWhiteSpace(cfg.Host))
                return MailSendResult.Fail("Chưa cấu hình máy chủ SMTP (SysOpConfig mail_smtp_host).");

            var all = MailAddressList.Parse(
                MailAddressList.Join(recipients?.To), MailAddressList.Join(recipients?.Cc), MailAddressList.Join(recipients?.Bcc));

            var invalid = all.To.Concat(all.Cc).Concat(all.Bcc).Where(x => !IsValidEmail(x)).ToList();
            var valid = new MailRecipients
            {
                To = all.To.Where(IsValidEmail).ToList(),
                Cc = all.Cc.Where(IsValidEmail).ToList(),
                Bcc = all.Bcc.Where(IsValidEmail).ToList(),
            };

            if (valid.To.Count == 0)
                return MailSendResult.Fail(invalid.Count > 0
                    ? $"Không có người nhận hợp lệ. Địa chỉ sai định dạng: {string.Join(", ", invalid)}"
                    : "Danh sách người nhận trống.");

            if (valid.Count > cfg.MaxRecipientsPerMail)
                return MailSendResult.Fail(
                    $"Số địa chỉ nhận (người nhận + CC + BCC = {valid.Count}) vượt giới hạn {cfg.MaxRecipientsPerMail}.");

            var files = (attachments ?? Enumerable.Empty<MailAttachment>())
                .Where(x => x?.Content is { Length: > 0 })
                .ToList();

            long bytes = files.Sum(x => x.Content.LongLength);
            long maxBytes = (long)cfg.MaxAttachmentMb * 1024 * 1024;
            if (bytes > maxBytes)
                return MailSendResult.Fail(
                    $"Tệp đính kèm {bytes / 1024d / 1024d:0.0}MB vượt giới hạn {cfg.MaxAttachmentMb}MB.");

            MimeMessage message;
            try
            {
                message = BuildMessage(cfg, valid, subject, bodyHtml, files);
            }
            catch (Exception ex)
            {
                return MailSendResult.Fail($"Lỗi dựng nội dung thư: {ex.Message}");
            }

            var attempts = 0;
            var maxAttempts = Math.Max(1, cfg.RetryCount + 1);
            string lastError = string.Empty;

            while (attempts < maxAttempts)
            {
                attempts++;
                try
                {
                    await SendCoreAsync(cfg, message, cancellationToken);

                    if (invalid.Count > 0)
                        _logger.LogWarning("Gửi mail bỏ qua {Count} địa chỉ sai định dạng: {List}",
                            invalid.Count, string.Join(", ", invalid));

                    return MailSendResult.Ok(attempts, bytes);
                }
                catch (OperationCanceledException)
                {
                    return MailSendResult.Fail("Đã hủy trước khi gửi xong.", attempts);
                }
                catch (AuthenticationException ex)
                {
                    // Sai tài khoản thì thử lại bao nhiêu lần cũng vô ích.
                    _logger.LogError(ex, "Xác thực SMTP thất bại");
                    return MailSendResult.Fail(
                        "Xác thực SMTP thất bại. Với Gmail phải dùng App Password 16 ký tự đã bỏ dấu cách, " +
                        "không phải mật khẩu tài khoản.", attempts);
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogWarning(ex, "Gửi mail lỗi lần {Attempt}/{Max}", attempts, maxAttempts);

                    if (attempts < maxAttempts && cfg.RetryDelaySeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(cfg.RetryDelaySeconds), cancellationToken);
                }
            }

            return MailSendResult.Fail($"Gửi thất bại sau {attempts} lần. Lỗi cuối: {lastError}", attempts);
        }

        /// <summary>
        /// Description: Dựng MimeMessage. Gmail sẽ ghi đè địa chỉ người gửi thành UserName,
        ///              nhưng vẫn giữ nguyên tên hiển thị FromName.
        /// Created date: 2026-08-13
        /// </summary>
        private MimeMessage BuildMessage(
            MailConfig cfg, MailRecipients recipients, string subject, string bodyHtml,
            List<MailAttachment> attachments)
        {
            var message = new MimeMessage();

            // Tài khoản SMTP là địa chỉ người gửi (Gmail cũng luôn ghi đè như vậy)
            message.From.Add(new MailboxAddress(cfg.FromName ?? string.Empty, cfg.UserName));

            foreach (var to in recipients.To)
                message.To.Add(MailboxAddress.Parse(to));
            foreach (var cc in recipients.Cc)
                message.Cc.Add(MailboxAddress.Parse(cc));
            foreach (var bcc in recipients.Bcc)
                message.Bcc.Add(MailboxAddress.Parse(bcc));

            if (IsValidEmail(cfg.BccAudit))
                message.Bcc.Add(MailboxAddress.Parse(cfg.BccAudit!.Trim()));

            var prefix = cfg.SubjectPrefix?.Trim();
            message.Subject = string.IsNullOrEmpty(prefix) ? subject : $"{prefix} {subject}";

            var builder = new BodyBuilder { HtmlBody = bodyHtml };

            // Trùng tên tệp thì hộp thư của người nhận có thể chỉ hiện một cái, nên đánh số thứ tự
            // cho các tên lặp lại thay vì để mất tệp.
            var used = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in attachments)
            {
                // Ảnh nội tuyến (logo...): gắn vào phần multipart/related, HTML trỏ tới bằng cid:
                if (!string.IsNullOrWhiteSpace(file.ContentId))
                {
                    var resource = string.IsNullOrWhiteSpace(file.ContentType)
                        ? builder.LinkedResources.Add(file.FileName, file.Content)
                        : builder.LinkedResources.Add(file.FileName, file.Content, ContentType.Parse(file.ContentType));
                    resource.ContentId = file.ContentId.Trim();
                    resource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(file.FileName) ? "dinhkem.pdf" : file.FileName.Trim();

                if (used.TryGetValue(name, out var seen))
                {
                    used[name] = seen + 1;
                    var ext = Path.GetExtension(name);
                    name = $"{Path.GetFileNameWithoutExtension(name)}_{seen + 1}{ext}";
                }
                else used[name] = 1;

                builder.Attachments.Add(name, file.Content);
            }

            message.Body = builder.ToMessageBody();
            return message;
        }

        private async Task SendCoreAsync(MailConfig cfg, MimeMessage message, CancellationToken cancellationToken)
        {
            using var client = new SmtpClient
            {
                Timeout = cfg.TimeoutSeconds * 1000
            };

            await client.ConnectAsync(cfg.Host, cfg.Port, ResolveSecureOption(cfg), cancellationToken);

            if (cfg.RequireAuthentication && !string.IsNullOrWhiteSpace(cfg.UserName))
                await client.AuthenticateAsync(cfg.UserName, cfg.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }

        /// <summary>
        /// Description: Đọc SecureSocketOptions từ cấu hình. Sai chính tả thì suy theo cổng
        ///              thay vì ném lỗi — 465 dùng SslOnConnect, còn lại StartTls.
        /// Created date: 2026-08-13
        /// </summary>
        private SecureSocketOptions ResolveSecureOption(MailConfig cfg)
        {
            if (Enum.TryParse<SecureSocketOptions>(cfg.SecureSocketOptions, true, out var parsed))
                return parsed;

            _logger.LogWarning("Mail:SecureSocketOptions không hợp lệ ('{Value}'), suy theo cổng {Port}",
                cfg.SecureSocketOptions, cfg.Port);

            return cfg.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        }
    }
}
