using Modules.VMS.Report.Controllers.OperationLogHistory.Dto;

namespace Modules.VMS.Report.Controllers.OperationLogHistory
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller xử lý báo cáo lịch sử điều khiển thiết bị cho VMS và LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsOperationLogHistoryReportController : BaseController
    {
        /// <summary>
        /// Author: ChuongLP
        /// Description: Render danh sách lịch sử điều khiển thiết bị (VMS/LCS)
        /// Created date: 07/10/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderList"), HttpPost]
        [DisplayName("Render the OperationLogHistory list")]
        public async Task<FileStreamResult> RenderReport(VmsOperationLogHistoryListRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Export file danh sách lịch sử điều khiển thiết bị (VMS/LCS)
        /// Created date: 07/10/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportList"), HttpPost]
        [DisplayName("Export the OperationLogHistory list")]
        public async Task<FileStreamResult> ExportReport(VmsOperationLogHistoryListExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
