using Modules.Report.Base.Core.Dto;

namespace Modules.WP.Report.Controllers.Statistical.Dto
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO chứa tham số render báo cáo thống kê lưu lượng truy cập theo tháng
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalMonthRenderInput : BaseReportInput
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO chứa tham số export báo cáo thống kê lưu lượng truy cập theo tháng
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalMonthExportInput : StatisticalMonthRenderInput
    {
        public string? Format { get; set; }
    }
}
