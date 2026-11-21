namespace Modules.Mail.Core.Dto.Render
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Một báo cáo đính kèm, ở dạng đã sẵn sàng để in ra thư.
    ///              Toàn bộ là chuỗi hiển thị — MailTemplate không tra cứu hay tính toán gì.
    /// Created date: 17/08/2026
    /// </summary>
    public class MailReportLine
    {
        /// <summary>Tên báo cáo dạng chữ, VD "Doanh thu theo tháng".</summary>
        public string ReportName { get; set; } = string.Empty;

        /// <summary>Tên nhóm báo cáo (SysPlugin.Name theo RptEmailReport.PluginCode), VD "Thu phí".</summary>
        public string? GroupName { get; set; }

        /// <summary>Kỳ dữ liệu, VD "Tháng 07/2026". Rỗng thì ẩn dòng.</summary>
        public string? PeriodText { get; set; }

        /// <summary>Tên tệp đính kèm.</summary>
        public string? FileName { get; set; }
    }
}
