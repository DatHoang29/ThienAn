using Modules.TMS.Report.Controllers.Incident.Dto;

namespace Modules.TMS.Report.Controllers.Incident
{
    /// <summary>
    /// Author: ThangNN
    /// Created Date: 04/11/2024
    /// </summary>
    public class TmsIncidentReportController: BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: Render danh sách incident
        /// Created Date: 09/12/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderList"), HttpPost]
        [DisplayName("Render the incident list")]
        public async Task<FileStreamResult> RenderReport(IncidentListRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Export danh sách incident
        /// Created Date: 09/12/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportList"), HttpPost]
        [DisplayName("Export the incident list")]
        public async Task<FileStreamResult> ExportReport(IncidentListExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Created Date: 05/11/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderDetail"), HttpPost]
        [DisplayName("Render detailed incident report")]
        public async Task<FileStreamResult> RenderReport(IncidentDetailRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Created Date: 05/11/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportDetail"), HttpPost]
        [DisplayName("Export detailed incident report")]
        public async Task<FileStreamResult> ExportReport(IncidentDetailExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
