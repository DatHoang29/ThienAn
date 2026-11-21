namespace Modules.Mail.Core.Constants
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Hằng số dùng chung của lịch gửi báo cáo qua email.
    /// Created date: 14/08/2026
    /// </summary>
    public class MailConst
    {
        /// <summary>
        /// Tiền tố mã recurring job Hangfire của lịch gửi báo cáo: "rpt-email-{ID lịch}" — mỗi lịch một job
        /// (RptEmailJobScheduler).
        /// </summary>
        public const string JobIdPrefix = "rpt-email-";
        public const string ConfigGroupCode = "mail_config";

        /// <summary>SysConfigType.Code của danh sách kỳ lấy số liệu (SysConfigData.Code = MailRptPeriodOffset).</summary>
        public const string ConfigTypePeriodOffset = "mail_period";
        public const string ConfigKeyMaxCatchUpHours = "mail_max_catchup_hours";

        /// <summary>Số ngày giữ nhật ký gửi (RptEmailLog). 0 = giữ mãi.</summary>
        public const string ConfigKeyLogRetentionDays = "mail_log_retention_days";
        public const int DefaultLogRetentionDays = 90;

        /// <summary>
        /// Job Hangfire dọn nhật ký gửi cũ — hằng ngày 02:17. KHÔNG dùng tiền tố JobIdPrefix ("rpt-email-"), nếu không
        /// RptEmailJobScheduler.SyncAllAsync sẽ coi là job thừa và gỡ mất.
        /// </summary>
        public const string LogCleanupJobId = "mail-log-cleanup";
        public const string LogCleanupCron = "17 2 * * *";

        /// <summary>
        /// Khoá SysOpConfig (nhóm mail_config) của cấu hình gửi mail.
        /// Mail.json chỉ còn SecureSocketOptions, Password, InlineImages (MailSettings).
        /// </summary>
        public static class MailKey
        {
            public const string Enable = "mail_enable";
            public const string SmtpHost = "mail_smtp_host";
            public const string SmtpPort = "mail_smtp_port";
            /// <summary>Tài khoản SMTP — cũng là địa chỉ người gửi (bỏ FromAddress riêng).</summary>
            public const string SmtpUserName = "mail_smtp_username";
            public const string SmtpRequireAuth = "mail_smtp_require_auth";
            public const string FromName = "mail_from_name";
            public const string SubjectPrefix = "mail_subject_prefix";
            public const string MaxRecipients = "mail_max_recipients";
            public const string MaxAttachmentMb = "mail_max_attachment_mb";
            public const string TimeoutSeconds = "mail_timeout_seconds";
            public const string RetryCount = "mail_retry_count";
            public const string RetryDelaySeconds = "mail_retry_delay_seconds";
            public const string BccAudit = "mail_bcc_audit";
            public const string TestRecipient = "mail_test_recipient";
        }
        public const int DefaultMaxCatchUpHours = 24;
        public const string LogCodePrefix = "LOG";
        public const int MaxDayOfMonth = 31;

        public const int MonthsPerQuarter = 3;

        public const int MonthsPerYear = 12;
        public const string DefaultTimeZoneId = "SE Asia Standard Time";
        /// <summary>
        /// Số báo cáo tối đa trong một lịch — đọc từ SysOpConfig (ConfigKeyMaxItems, MailConfig.MaxItemsPerSchedule).
        /// Hằng số này chỉ là giá trị MẶC ĐỊNH khi DB thiếu khoá hoặc giá trị sai.
        /// </summary>
        public const int MaxItemsPerSchedule = 10;
        public const string ConfigKeyMaxItems = "mail_max_items";

        /// <summary>Tiêu đề khi lịch để trống ô Tiêu đề.</summary>
        public const string DefaultSubject = "{DanhSachBaoCao} - {KyBaoCao}";

        public class FileFormat
        {
            public const string Pdf = "pdf";
            public const string Excel = "xlsx";
            public const string Word = "docx";
        }

        /// <summary>
        /// Tag hệ thống — kéo thả vào tiêu đề và nội dung thư (wangEditor), thay giá trị lúc gửi.
        /// Tag về báo cáo tính trên TOÀN BỘ báo cáo trong thư, không theo báo cáo đầu tiên.
        /// </summary>
        public class Placeholder
        {
            public const string KyBaoCao = "{KyBaoCao}";
            public const string DanhSachBaoCao = "{DanhSachBaoCao}";
            public const string NhomBaoCao = "{NhomBaoCao}";
            public const string TenLich = "{TenLich}";
            public const string SoBaoCao = "{SoBaoCao}";
            public const string NgayGui = "{NgayGui}";
            public const string GioGui = "{GioGui}";
        }
    }
}
