using Modules.Mail.Core.Entities;



namespace Modules.Mail.Core.Dto.EmailSchedule
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Một báo cáo đính kèm, trả về kèm tên báo cáo dạng chữ để giao diện
    ///              không phải tự tra bảng mã.
    /// Created date: 14/08/2026
    /// </summary>
    public class RptEmailScheduleItemOutput : MailScheduleItem
    {
        /// <summary>Tên báo cáo hiển thị, tra từ RptReportCode.DisplayName.</summary>
        public string? ReportName { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Dữ liệu trả về 1 lịch gửi báo cáo (phân trang).
    /// Created date: 14/08/2026
    /// </summary>
    public class RptPageEmailScheduleOutput : MailSchedule
    {
        /// <summary>Số báo cáo đính kèm.</summary>
        public int ItemCount { get; set; }

        /// <summary>Tên các báo cáo ghép bằng dấu phẩy, dùng cho tooltip trên lưới.</summary>
        public string? ReportSummary { get; set; }

        /// <summary>Diễn giải lịch chạy bằng tiếng Việt, VD "08:00 ngày 01 hàng tháng".</summary>
        public string? ScheduleText { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: Dữ liệu trả về 1 lịch gửi báo cáo (danh sách / chi tiết).
    /// Created date: 14/08/2026
    /// </summary>
    public class RptEmailScheduleOutput : MailSchedule
    {
        public string? ScheduleText { get; set; }

        public int ItemCount { get; set; }

        public string? ReportSummary { get; set; }

        /// <summary>Nội dung thư (HTML) đọc từ file ContentPath — chỉ GetById trả, danh sách để null cho nhẹ.</summary>
        public string? Content { get; set; }

        public List<RptEmailScheduleItemOutput> Items { get; set; } = new();
    }
}
