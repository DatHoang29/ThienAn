using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoNam;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoNam
{
    public class LLTNamController : BaseController
    {
        /// <summary>
        /// Render report theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(LLTNamRenderInput request)
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
        public async Task<FileStreamResult> ExportReport(LLTNamExportInput request)
        {
            //Lưu ý: Sử dụng [NonUnify] để bỏ qua việc thống nhất response
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
