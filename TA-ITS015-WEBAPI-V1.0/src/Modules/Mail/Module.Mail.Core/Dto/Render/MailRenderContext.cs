
namespace Modules.Mail.Core.Dto.Render
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Toàn bộ dữ liệu cần để thay tag của một thư. Dùng chung cho job gửi thật và bản xem
    ///              trước — nhờ vậy thư xem trước và thư gửi đi dựng bằng đúng một đường.
    /// Created date: 29/09/2026
    /// </summary>
    public class MailRenderContext
    {
        public string? ScheduleName { get; set; }

        public List<MailReportLine> Lines { get; set; } = new();

        public DateTime SentAt { get; set; }
    }
}
