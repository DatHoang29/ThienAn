namespace Modules.Mail.Core.Dto.Smtp
{
    /// <summary>
    /// Description: Phần cấu hình gửi mail ở Configuration/Mail.json (khóa gốc "Mail") — chỉ những gì không lưu DB.
    ///              Các tham số còn lại ở SysOpConfig (nhóm mail_config). Khi gửi không đọc thẳng lớp này mà dùng
    ///              MailConfig qua MailConfigService.GetAsync.
    /// Created date: 2026-08-13
    /// </summary>
    public class MailSettings
    {
        /// <summary>
        /// None | Auto | SslOnConnect | StartTls | StartTlsWhenAvailable.
        /// Để chuỗi thay vì bool vì bool không phân biệt được StartTls (587) với SslOnConnect (465).
        /// </summary>
        public string SecureSocketOptions { get; set; } = "StartTls";

        /// <summary>Mật khẩu SMTP. Với Gmail là App Password 16 ký tự đã bỏ dấu cách. KHÔNG lưu DB.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Ảnh nhúng vào thân thư (inline, CID). Khoá = tên dùng trong mẫu, giá trị = đường dẫn file
        /// TÍNH TỪ wwwroot. VD { "logo": "Upload/logo.jpg" } thì mẫu viết &lt;img src="cid:logo"&gt;.
        ///
        /// Đọc thẳng file trong wwwroot (dùng chung logo của trang web) nên đổi logo chỉ cần thay file,
        /// không phải build lại. Thiếu file thì thư vẫn gửi, chỉ mất ảnh — có ghi cảnh báo log.
        /// </summary>
        public Dictionary<string, string> InlineImages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
