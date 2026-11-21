using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoThoiGian;

namespace Module.TOLL.Report.Controllers.DoanhThu.DoanhThuTheoThoiGian
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller báo cáo doanh thu theo thời gian (DTTTG)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTTGController : BaseController
    {
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(DTTTGRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        [NonUnify]
        [ApiDescriptionSettings(Name = "Export"), HttpPost]
        [DisplayName("Export report")]
        public async Task<FileStreamResult> ExportReport(DTTTGExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
