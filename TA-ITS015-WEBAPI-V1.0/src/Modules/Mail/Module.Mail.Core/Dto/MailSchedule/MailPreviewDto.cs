namespace Modules.Mail.Core.Dto.EmailSchedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Dữ liệu form lịch đang soạn, gửi lên để xem trước thư.
    /// Created date: 29/09/2026
    /// </summary>
    public class MailPreviewScheduleInput
    {
        public string? TemplateCode { get; set; }
        public string? Name { get; set; }
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public string? Frequency { get; set; }
        public int? DayOfMonth { get; set; }
        public int? MonthInPeriod { get; set; }
        public string? DaysOfWeek { get; set; }
        public int HourOfDay { get; set; }
        public int MinuteOfHour { get; set; }
        public List<MailScheduleItemInput> Items { get; set; } = new();
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Tham số lấy bản xem trước của một mẫu với dữ liệu giả (ảnh thu nhỏ khi chọn mẫu).
    /// Created date: 29/09/2026
    /// </summary>
    public class MailSampleTemplateInput
    {
        public string? Code { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Thư đã dựng để hiển thị trong iframe (srcdoc).
    /// Created date: 29/09/2026
    /// </summary>
    public class MailPreviewOutput
    {
        public string? TemplateCode { get; set; }

        public string Subject { get; set; } = null!;

        public string Html { get; set; } = null!;

        public DateTime SendAt { get; set; }

        public List<string> Warnings { get; set; } = new();
    }
}
