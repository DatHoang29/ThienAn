using Modules.WP.Report.Controllers.Statistical.Dto;

namespace Modules.WP.Report.Controllers.Statistical
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller báo cáo thống kê lưu lượng truy cập
    /// Created date: 17/07/2026
    /// </summary>
    public class WpStatisticalReportController : BaseController
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: Render báo cáo thống kê lưu lượng truy cập
        /// Created date: 17/07/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render the statistical report")]
        public async Task<FileStreamResult> RenderReport(StatisticalRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Export báo cáo thống kê lưu lượng truy cập
        /// Created date: 17/07/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Export"), HttpPost]
        [DisplayName("Export the statistical report")]
        public async Task<FileStreamResult> ExportReport(StatisticalExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Render báo cáo thống kê lưu lượng truy cập theo tháng
        /// Created date: 17/07/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderMonth"), HttpPost]
        [DisplayName("Render the monthly statistical report")]
        public async Task<FileStreamResult> RenderReport(StatisticalMonthRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Export báo cáo thống kê lưu lượng truy cập theo tháng
        /// Created date: 17/07/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportMonth"), HttpPost]
        [DisplayName("Export the monthly statistical report")]
        public async Task<FileStreamResult> ExportReport(StatisticalMonthExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Render báo cáo thống kê lưu lượng truy cập theo năm
        /// Created date: 17/07/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderYear"), HttpPost]
        [DisplayName("Render the yearly statistical report")]
        public async Task<FileStreamResult> RenderReport(StatisticalYearRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Export báo cáo thống kê lưu lượng truy cập theo năm
        /// Created date: 17/07/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportYear"), HttpPost]
        [DisplayName("Export the yearly statistical report")]
        public async Task<FileStreamResult> ExportReport(StatisticalYearExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
