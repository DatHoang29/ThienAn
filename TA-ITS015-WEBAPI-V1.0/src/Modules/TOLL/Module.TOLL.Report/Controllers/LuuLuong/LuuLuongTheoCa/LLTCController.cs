using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoCa;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoCa
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Controller báo cáo lưu lượng vé theo ca (LLTC)
    /// Created date: 27/07/2026
    /// </summary>
    public class LLTCController : BaseController
    {
        /// <summary>
        /// Render report theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(LLTCRenderInput request)
        {
            //Lưu ý: Sử dụng [NonUnify] để bỏ qua việc thống nhất response
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Export report theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Export"), HttpPost]
        [DisplayName("Export report")]
        public async Task<FileStreamResult> ExportReport(LLTCExportInput request)
        {
            //Lưu ý: Sử dụng [NonUnify] để bỏ qua việc thống nhất response
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
