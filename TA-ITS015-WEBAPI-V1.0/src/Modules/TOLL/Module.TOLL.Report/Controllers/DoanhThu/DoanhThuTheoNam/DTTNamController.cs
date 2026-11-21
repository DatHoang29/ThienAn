using Module.TOLL.Core.Dto.DoanhThu.DoanhThuTheoNam;

namespace Module.TOLL.Report.Controllers.DoanhThu.DoanhThuTheoNam
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller báo cáo doanh thu theo năm (DTTNam)
    /// Created date: 28/07/2026
    /// </summary>
    public class DTTNamController : BaseController
    {
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(DTTNamRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        [NonUnify]
        [ApiDescriptionSettings(Name = "Export"), HttpPost]
        [DisplayName("Export report")]
        public async Task<FileStreamResult> ExportReport(DTTNamExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
