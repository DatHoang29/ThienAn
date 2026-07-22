using Modules.Report.Demo.Controllers.CarboneDemo.Dto;

namespace Modules.Report.Demo.Controllers.CarboneDemo
{

    public class CarboneDemoController : BaseController
    {
        /// <summary>
        /// Render report theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [NonUnify]
        [ApiDescriptionSettings(Name = "Render"), HttpPost]
        [DisplayName("Render report")]
        public async Task<FileStreamResult> RenderReport([FromQuery] CarboneDemoRenderInput request)
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
        public async Task<FileStreamResult> ExportReport([FromQuery] CarboneDemoExportInput request)
        {
            //Lưu ý: Sử dụng [NonUnify] để bỏ qua việc thống nhất response
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
