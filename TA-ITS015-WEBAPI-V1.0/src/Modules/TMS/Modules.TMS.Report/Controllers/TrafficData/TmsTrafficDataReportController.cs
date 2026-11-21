using Modules.TMS.Report.Controllers.TrafficData.Dto;

namespace Modules.TMS.Report.Controllers.TrafficData
{
    /// <summary>
    /// Author: ThangNN
    /// Created Date: 04/11/2024
    /// </summary>
    public class TmsTrafficDataReportController: BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: Render danh sách traffic-volume theo ngày
        /// Created Date: 04/11/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderListByDay"), HttpPost]
        [DisplayName("Render the traffic-volume by day")]
        public async Task<FileStreamResult> RenderReport(TrafficDataDayRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Export danh sách traffic-volume theo ngày
        /// Created Date: 04/11/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportListByDay"), HttpPost]
        [DisplayName("Export the traffic-volume by day")]
        public async Task<FileStreamResult> ExportReport(TrafficDataDayExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Render danh sách traffic-volume theo tháng
        /// Created Date: 04/11/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderListByMonth"), HttpPost]
        [DisplayName("Render the traffic-volume by month")]
        public async Task<FileStreamResult> RenderReport(TrafficDataMonthRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Export danh sách traffic-volume theo tháng
        /// Created Date: 04/11/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportListByMonth"), HttpPost]
        [DisplayName("Export the traffic-volume by month")]
        public async Task<FileStreamResult> ExportReport(TrafficDataMonthExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
