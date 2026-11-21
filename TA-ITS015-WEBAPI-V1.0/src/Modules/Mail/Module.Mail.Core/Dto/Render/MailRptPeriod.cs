namespace Modules.Mail.Core.Dto.Render
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Kỳ dữ liệu đã suy ra của một báo cáo.
    /// Created date: 17/08/2026
    /// </summary>
    public class MailRptPeriod
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int Nam { get; set; }
        public int Thang { get; set; }
        public int Quy { get; set; }
        public DateTime Ngay { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsPartial { get; set; }
    }

}
