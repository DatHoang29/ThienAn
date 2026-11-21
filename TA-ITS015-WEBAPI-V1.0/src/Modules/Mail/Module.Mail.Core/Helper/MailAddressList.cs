using Modules.Mail.Core.Dto.Smtp;

namespace Modules.Mail.Core.Helper
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Danh sách địa chỉ email dạng chuỗi "a@x; b@y" (cột Recipients / Cc / Bcc của lịch và log).
    /// Created date: 02/10/2026
    /// </summary>
    public static class MailAddressList
    {
        private static readonly char[] Separators = { ';', ',', '\n', '\r' };

        /// <summary>Tách chuỗi theo ; , xuống dòng — cắt khoảng trắng, bỏ rỗng, bỏ trùng (không phân biệt hoa thường).</summary>
        public static List<string> Split(string? text)
            => (text ?? string.Empty)
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>Ghép lại thành chuỗi lưu DB; rỗng thì null.</summary>
        public static string? Join(IEnumerable<string>? emails)
        {
            var list = emails?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            return list is { Count: > 0 } ? string.Join("; ", list) : null;
        }

        /// <summary>
        /// Tách ba ô và bỏ trùng GIỮA các ô theo thứ tự ưu tiên To → CC → BCC: địa chỉ đã có ở To thì bỏ khỏi
        /// CC / BCC, đã có ở CC thì bỏ khỏi BCC. Một người chỉ nhận đúng một bản thư.
        /// </summary>
        public static MailRecipients Parse(string? to, string? cc, string? bcc)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            List<string> Take(string? text) => Split(text).Where(seen.Add).ToList();

            return new MailRecipients { To = Take(to), Cc = Take(cc), Bcc = Take(bcc) };
        }
    }
}
