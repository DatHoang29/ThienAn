using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoThang;

namespace Module.TOLL.Report.Controllers.DoanhThu.DoanhThuTheoThang
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller báo cáo doanh thu theo tháng (DTTT)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTTController : BaseController
    {
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(DTTTRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        [NonUnify]
        [ApiDescriptionSettings(Name = "Export"), HttpPost]
        [DisplayName("Export report")]
        public async Task<FileStreamResult> ExportReport(DTTTExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
