using Modules.Report.Base.Core.Dto;

namespace Modules.TMS.Report.Controllers.Incident.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số render chi tiết một sự cố
    /// Created Date: 04/11/2024
    /// </summary>
    public class IncidentDetailRenderInput : BaseReportInput
    {
        public string? ReportName { get; set; }
        public string? IncidentId { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO chứa tham số export chi tiết một sự cố
    /// Created Date: 04/11/2024
    /// </summary>
    public class IncidentDetailExportInput : IncidentDetailRenderInput
    {
        public string? Format { get; set; }
    }
}
