using Modules.Report.Base.Core.Dto;

namespace Modules.TMS.Report.Controllers.TrafficData.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số render dữ liệu ngày
    /// Created Date: 04/11/2024
    /// </summary>
    public class TrafficDataDayRenderInput : BaseReportInput
    {
        public DateTime? ValueDate { get; set; }
        public string? WorkShiftCode { get; set; } // Ex: work_shift_1, work_time_1
        public string? WorkShiftName { get; set; } // Ex: Ca 01, Buổi 01
        public string? WorkShiftValue { get; set; } // Ex: 6h-14h, 0h-8h
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số export dữ liệu ngày
    /// Created Date: 04/11/2024
    /// </summary>
    public class TrafficDataDayExportInput : TrafficDataDayRenderInput
    {
        public string? Format { get; set; }
    }
}