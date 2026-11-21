using Modules.Report.Base.Core.Dto;

namespace Modules.TMS.Report.Controllers.Incident.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số render danh sách sự cố
    /// Created Date: 04/11/2024
    /// </summary>
    public class IncidentListRenderInput : BaseReportInput
    {
        public string? ReportName { get; set; }
        public string? EventTypeId { get; set; }
        public string? Source { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số export danh sách sự cố
    /// Created Date: 04/11/2024
    /// </summary>
    public class IncidentListExportInput : IncidentListRenderInput
    {
        public string? Format { get; set; }
    }
}
