using System.Text.RegularExpressions;

namespace Modules.Mail.Infrastructure.Services.Mail.Templates
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Làm sạch HTML người dùng soạn (wangEditor) trước khi lưu: nội dung này vừa hiện trên web (xem trước)
    ///              vừa gửi ra ngoài qua email nên phải bỏ mọi thứ chạy được mã — script, iframe, form, thuộc tính
    ///              on*=, link javascript:. wangEditor không sinh ra các thẻ này, nên chỉ chặn dữ liệu gửi thẳng qua API.
    ///
    ///              Lọc bằng regex theo danh sách CẤM (không phải danh sách cho phép) — đủ cho nội dung do người dùng nội bộ
    ///              soạn. Cần chặt hơn thì thay bằng thư viện HtmlSanitizer (Ganss.Xss).
    /// Created date: 12/10/2026
    /// </summary>
    public static class MailHtmlSanitizer
    {
        private const string BlockedTags = "script|style|iframe|frame|frameset|object|embed|applet|noscript|template|svg|math";
        private const string FormTags = "form|input|button|textarea|select|option|link|meta|base";

        /// <summary>Thẻ nguy hiểm KÈM nội dung bên trong.</summary>
        private static readonly Regex BlockedElements = new(
            $@"<({BlockedTags})\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>Thẻ mở / đóng / tự đóng còn sót của các loại trên.</summary>
        private static readonly Regex BlockedTagsLeft = new(
            $@"</?({BlockedTags}|{FormTags})\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex Comments = new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>Một thẻ mở — chỉ lọc thuộc tính BÊN TRONG thẻ, không đụng chữ nội dung (VD "online = 3").</summary>
        private static readonly Regex OpenTags = new(@"<[a-zA-Z][^>]*>", RegexOptions.Compiled);

        /// <summary>Thuộc tính sự kiện: onclick=, onerror=...</summary>
        private static readonly Regex EventAttributes = new(
            @"\s+on[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Link / nguồn dạng javascript: vbscript: — đổi thành "#".</summary>
        private static readonly Regex ScriptUrls = new(
            @"(\s(?:href|src|action|formaction|xlink:href)\s*=\s*[""']?)\s*(?:javascript|vbscript)\s*:[^""'\s>]*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Sanitize(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var result = Comments.Replace(html, string.Empty);
            result = BlockedElements.Replace(result, string.Empty);
            result = BlockedTagsLeft.Replace(result, string.Empty);
            result = OpenTags.Replace(result, m => ScriptUrls.Replace(EventAttributes.Replace(m.Value, string.Empty), "$1#"));

            return result.Trim();
        }
    }
}
