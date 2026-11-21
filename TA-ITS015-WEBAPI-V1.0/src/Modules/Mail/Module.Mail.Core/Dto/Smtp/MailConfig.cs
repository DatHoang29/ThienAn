namespace Modules.Mail.Core.Dto.Smtp
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Toàn bộ cấu hình vận hành của phân hệ Mail: SysOpConfig nhóm mail_config + phần bí mật ở
    ///              Mail.json (MailSettings). Đọc qua MailConfigService.GetAsync — mỗi request / job đọc một lần.
    /// Created date: 02/10/2026
    /// </summary>
    public class MailConfig
    {
        /* ── Máy chủ gửi ── */

        /// <summary>Công tắc tổng (mail_enable). Thiếu cấu hình = TẮT.</summary>
        public bool Enable { get; set; }

        /// <summary>Máy chủ SMTP (mail_smtp_host). VD smtp.gmail.com</summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>mail_smtp_port. 587 = StartTls | 465 = SslOnConnect | 25 = None</summary>
        public int Port { get; set; }

        /// <summary>Mail.json — None | Auto | SslOnConnect | StartTls | StartTlsWhenAvailable.</summary>
        public string SecureSocketOptions { get; set; } = string.Empty;

        /// <summary>Tài khoản SMTP (mail_smtp_username) — đồng thời là địa chỉ người gửi.</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>Mail.json — không lưu DB.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>mail_smtp_require_auth. false = SMTP relay nội bộ mở theo IP.</summary>
        public bool RequireAuthentication { get; set; }

        /// <summary>Tên người gửi người nhận nhìn thấy (mail_from_name).</summary>
        public string FromName { get; set; } = string.Empty;

        /// <summary>Tiền tố trước mọi tiêu đề (mail_subject_prefix). Trống = tắt.</summary>
        public string? SubjectPrefix { get; set; }

        /// <summary>Tổng To + CC + BCC tối đa mỗi thư (mail_max_recipients).</summary>
        public int MaxRecipientsPerMail { get; set; }

        /// <summary>Tổng dung lượng đính kèm tối đa, MB (mail_max_attachment_mb).</summary>
        public int MaxAttachmentMb { get; set; }

        public int TimeoutSeconds { get; set; }
        public int RetryCount { get; set; }
        public int RetryDelaySeconds { get; set; }

        /// <summary>Bản sao ẩn mọi mail hệ thống (mail_bcc_audit). Trống = tắt.</summary>
        public string? BccAudit { get; set; }

        /// <summary>Địa chỉ mặc định cho gửi thử (mail_test_recipient).</summary>
        public string? TestRecipient { get; set; }

        /* ── Lịch gửi báo cáo ── */

        /// <summary>Số báo cáo tối đa trong một lịch (mail_max_items).</summary>
        public int MaxItemsPerSchedule { get; set; }

        /// <summary>Mốc quá hạn hơn số giờ này thì không gửi bù (mail_max_catchup_hours).</summary>
        public int MaxCatchUpHours { get; set; }

        /// <summary>Số ngày giữ nhật ký gửi (mail_log_retention_days). 0 = giữ mãi.</summary>
        public int LogRetentionDays { get; set; }
    }
}
