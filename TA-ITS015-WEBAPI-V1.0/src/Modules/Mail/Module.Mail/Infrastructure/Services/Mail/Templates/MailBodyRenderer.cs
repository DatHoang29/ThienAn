using System.Net;
using System.Text.RegularExpressions;
using Modules.Mail.Core.Constants;
using Modules.Mail.Core.Dto.Render;

namespace Modules.Mail.Infrastructure.Services.Mail.Templates
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Thay tag hệ thống trong tiêu đề và nội dung thư (HTML soạn bằng wangEditor), rồi bọc nội dung vào
    ///              khung tối thiểu của email (rộng tối đa 640px, font Arial).
    /// Created date: 29/09/2026
    /// </summary>
    public static class MailBodyRenderer
    {
        private static readonly Regex Spaces = new(@"\s{2,}", RegexOptions.Compiled);

        /// <summary>
        /// Dựng tiêu đề email. Kết quả là văn bản thường, chưa gắn SubjectPrefix — MailSender tự gắn.
        /// Xuống dòng thành dấu cách.
        /// </summary>
        public static string RenderSubject(string? template, MailRenderContext ctx)
        {
            var src = string.IsNullOrWhiteSpace(template) ? MailConst.DefaultSubject : template;
            var sys = SystemValues(ctx);

            var text = MailTag.Pattern.Replace(src, m => sys.TryGetValue(m.Value, out var v) ? v : m.Value);

            // Tiêu đề có xuống dòng là hỏng header thư
            text = text.Replace('\r', ' ').Replace('\n', ' ');
            return Spaces.Replace(text, " ").Trim();
        }

        /// <summary>Dựng thân thư: thay tag trong nội dung rồi bọc khung email. Tag lạ giữ nguyên chữ.</summary>
        public static string RenderBody(string? contentHtml, MailRenderContext ctx)
        {
            var sys = SystemValues(ctx);

            var body = MailTag.Pattern.Replace(contentHtml ?? string.Empty, m =>
                sys.TryGetValue(m.Value, out var v) ? WebUtility.HtmlEncode(v) : m.Value);

            return WrapBody(body);
        }

        /// <summary>Bọc nội dung trong khung 640px căn giữa, dùng bảng và CSS inline cho email client.</summary>
        private static string WrapBody(string body)
            => $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        .mail-content img {{ max-width: 100%; height: auto; }}
    </style>
</head>
<body bgcolor=""#f3f4f6"" style=""margin:0; padding:0; background-color:#f3f4f6;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" bgcolor=""#f3f4f6"" style=""width:100%; border-collapse:collapse; background-color:#f3f4f6;"">
        <tr>
            <td align=""center"" style=""padding:24px 16px;"">
                <!--[if mso]>
                <table role=""presentation"" align=""center"" width=""640"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td>
                <![endif]-->
                <table role=""presentation"" align=""center"" width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" bgcolor=""#ffffff"" style=""width:100%; max-width:640px; margin:0 auto; border-collapse:collapse; background-color:#ffffff;"">
                    <tr>
                        <td class=""mail-content"" align=""left"" style=""font-family:Arial, sans-serif; font-size:14px; line-height:1.6; color:#222222; overflow-wrap:break-word;"">
                            {body}
                        </td>
                    </tr>
                </table>
                <!--[if mso]>
                </td></tr></table>
                <![endif]-->
            </td>
        </tr>
    </table>
</body>
</html>";

        /// <summary>
        /// Giá trị của mọi tag hệ thống, dạng văn bản thường (CHƯA mã hoá HTML).
        /// Tag về báo cáo tính trên TOÀN BỘ báo cáo trong thư (một thư có thể gồm nhiều nhóm, nhiều kỳ) — giá trị
        /// trùng chỉ in một lần, giữ thứ tự xuất hiện.
        /// </summary>
        public static Dictionary<string, string> SystemValues(MailRenderContext ctx)
            => SystemTagValues.ToDictionary(x => x.Key, x => x.Value(ctx) ?? string.Empty, StringComparer.Ordinal);

        /// <summary>
        /// CÁCH TÍNH của từng tag hệ thống — mỗi tag một dòng. Mã tag lấy từ MailConst.Placeholder,
        /// nhãn / nhóm / mẫu khai ở MailTag.All. Thêm tag: thêm hằng + một dòng ở All + một dòng ở đây.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Func<MailRenderContext, string?>> SystemTagValues =
            new Dictionary<string, Func<MailRenderContext, string?>>(StringComparer.Ordinal)
            {
                [MailConst.Placeholder.TenLich] = c => c.ScheduleName?.Trim(),
                [MailConst.Placeholder.SoBaoCao] = c => c.Lines.Count.ToString(),
                [MailConst.Placeholder.NgayGui] = c => c.SentAt.ToString("dd/MM/yyyy"),
                [MailConst.Placeholder.GioGui] = c => c.SentAt.ToString("HH:mm"),
                [MailConst.Placeholder.KyBaoCao] = c => JoinDistinct(c.Lines.Select(x => x.PeriodText)),
                [MailConst.Placeholder.DanhSachBaoCao] = c => JoinDistinct(c.Lines.Select(x => x.ReportName)),
                [MailConst.Placeholder.NhomBaoCao] = c => JoinDistinct(c.Lines.Select(x => x.GroupName)),
            };

        /// <summary>Nối các giá trị khác rỗng, bỏ trùng, giữ thứ tự: "Tháng 09/2026, Ngày 01/10/2026".</summary>
        private static string JoinDistinct(IEnumerable<string?> values)
            => string.Join(", ", values
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.Ordinal));
    }
}
