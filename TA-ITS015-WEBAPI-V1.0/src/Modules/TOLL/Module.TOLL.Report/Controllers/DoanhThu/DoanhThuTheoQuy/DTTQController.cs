using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoQuy;

namespace Module.TOLL.Report.Controllers.DoanhThu.DoanhThuTheoQuy
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller báo cáo doanh thu theo quý (DTTQ)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTQController : BaseController
    {
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(DTTQRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        [NonUnify]
        [ApiDescriptionSettings(Name = "Export"), HttpPost]
        [DisplayName("Export report")]
        public async Task<FileStreamResult> ExportReport(DTTQExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
