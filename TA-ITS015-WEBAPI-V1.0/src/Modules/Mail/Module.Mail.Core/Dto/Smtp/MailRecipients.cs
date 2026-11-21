namespace Modules.Mail.Core.Dto.Smtp
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Người nhận của một thư: To / CC / BCC
    /// Created date: 02/10/2026
    /// </summary>
    public sealed class MailRecipients
    {
        public List<string> To { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
        public int Count => To.Count + Cc.Count + Bcc.Count;
        public static MailRecipients ToOnly(IEnumerable<string>? to)
            => new() { To = to?.ToList() ?? new List<string>() };
    }
}
