using Module.TOLL.Core.Dto.LuuLuong.LuuLuongTheoThang;

namespace Module.TOLL.Report.Controllers.LuuLuong.LuuLuongTheoThang
{
    public class LLTTController : BaseController
    {
        /// <summary>
        /// Render report theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport(LLTTRenderInput request)
        {
            //Lưu ý: Sử dụng [NonUnify] để bỏ qua việc thống nhất response
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
        public async Task<FileStreamResult> ExportReport(LLTTExportInput request)
        {
            //Lưu ý: Sử dụng [NonUnify] để bỏ qua việc thống nhất response
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
