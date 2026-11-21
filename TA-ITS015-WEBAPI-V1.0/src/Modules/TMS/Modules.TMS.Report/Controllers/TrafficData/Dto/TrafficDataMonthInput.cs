using Modules.Report.Base.Core.Dto;

namespace Modules.TMS.Report.Controllers.TrafficData.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số render dữ liệu tháng
    /// Created Date: 04/11/2024
    /// </summary>
    public class TrafficDataMonthRenderInput : BaseReportInput
    {
        public int? Month { get; set; }
        public int? Year { get; set; }
        public string? ReportType { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số export dữ liệu tháng
    /// Created Date: 04/11/2024
    /// </summary>
    public class TrafficDataMonthExportInput : TrafficDataMonthRenderInput
    {
        public string? Format { get; set; }
    }
}
