using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNgay;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoNgay
{
    public class LLTNController : BaseController
    {
        /// <summary>
        /// Render report theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(LLTNRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Export"), HttpPost]
        [DisplayName("Export report")]
        public async Task<FileStreamResult> ExportReport(LLTNExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
