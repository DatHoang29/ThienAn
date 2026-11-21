using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNgay;

namespace Module.TOLL.Report.Controllers.DoanhThu.DoanhThuTheoNgay
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller báo cáo doanh thu theo ngày (DTTN)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTNController : BaseController
    {
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(DTTNRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        [NonUnify]
        [ApiDescriptionSettings(Name = "Export"), HttpPost]
        [DisplayName("Export report")]
        public async Task<FileStreamResult> ExportReport(DTTNExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
