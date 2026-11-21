using Modules.Report.Base.Core.Dto;

namespace Modules.WP.Report.Controllers.Statistical.Dto
{
    /// <summary>
    /// Author: HieuNV
    /// Description: DTO chứa tham số render báo cáo thống kê lưu lượng truy cập theo năm
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalYearRenderInput : BaseReportInput
    {
        public int? Year { get; set; }
    }

    /// <summary>
    /// Author: HieuNV
    /// Description: DTO chứa tham số export báo cáo thống kê lưu lượng truy cập theo năm
    /// Created date: 17/07/2026
    /// </summary>
    public class StatisticalYearExportInput : StatisticalYearRenderInput
    {
        public string? Format { get; set; }
    }
}
