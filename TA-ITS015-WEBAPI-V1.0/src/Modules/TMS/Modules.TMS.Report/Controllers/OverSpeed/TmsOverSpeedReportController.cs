using Modules.TMS.Report.Controllers.OverSpeed.Dto;

namespace Modules.TMS.Report.Controllers.OverSpeed
{
    /// <summary>
    /// Author: KienLT
    /// Description: Báo cáo phương tiện vượt quá tốc độ (FastReport, template <c>OverSpeedReport</c>).
    /// Created date: 16/10/2026
    /// </summary>
    public class TmsOverSpeedReportController : BaseController
    {
        /// <summary>
        /// Author: KienLT
        /// Description: Render báo cáo phương tiện vượt quá tốc độ
        /// Created date: 16/10/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderList"), HttpPost]
        [DisplayName("Render the over speed vehicle report")]
        public async Task<FileStreamResult> RenderReport(OverSpeedRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: KienLT
        /// Description: Export báo cáo phương tiện vượt quá tốc độ
        /// Created date: 16/10/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportList"), HttpPost]
        [DisplayName("Export the over speed vehicle report")]
        public async Task<FileStreamResult> ExportReport(OverSpeedExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
