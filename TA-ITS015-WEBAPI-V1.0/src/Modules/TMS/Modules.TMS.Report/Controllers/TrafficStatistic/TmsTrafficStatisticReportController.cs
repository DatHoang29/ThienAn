using Modules.TMS.Report.Controllers.TrafficStatistic.Dto;

namespace Modules.TMS.Report.Controllers.TrafficStatistic
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/12/2024
    /// </summary>
    public class TmsTrafficStatisticReportController: BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: Render danh sách TrafficStatistic cho đoạn tuyến
        /// Created date: 05/12/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderListForSection"), HttpPost]
        [DisplayName("Render the TrafficStatistic list for section")]
        public async Task<FileStreamResult> RenderReport(TrafficStatisticSectionRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Export danh sách TrafficStatistic cho đoạn tuyến
        /// Created date: 05/12/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportListForSection"), HttpPost]
        [DisplayName("Export the TrafficStatistic list for section")]
        public async Task<FileStreamResult> ExportReport(TrafficStatisticSectionExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Render danh sách TrafficStatistic cho zone và camera
        /// Created date: 05/12/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderListForZone"), HttpPost]
        [DisplayName("Render the TrafficStatistic list for zone")]
        public async Task<FileStreamResult> RenderReport(TrafficStatisticZoneRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Export danh sách TrafficStatistic cho zone và camera
        /// Created date: 05/12/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportListForZone"), HttpPost]
        [DisplayName("Export the TrafficStatistic list for zone")]
        public async Task<FileStreamResult> ExportReport(TrafficStatisticZoneExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
