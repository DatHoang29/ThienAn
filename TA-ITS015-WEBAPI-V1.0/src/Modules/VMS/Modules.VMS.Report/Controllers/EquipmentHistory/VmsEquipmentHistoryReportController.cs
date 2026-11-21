using Modules.VMS.Report.Controllers.EquipmentHistory.Dto;

namespace Modules.VMS.Report.Controllers.EquipmentHistory
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Controller xử lý báo cáo lịch sử thiết bị cho VMS và LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryReportController : BaseController
    {
        /// <summary>
        /// Author: ChuongLP
        /// Description: Render danh sách EquipmentHistory (VMS/LCS)
        /// Created date: 07/10/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderList"), HttpPost]
        [DisplayName("Render the EquipmentHistory list")]
        public async Task<FileStreamResult> RenderReport(VmsEquipmentHistoryListRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Export danh sách EquipmentHistory (VMS/LCS)
        /// Created date: 07/10/2026
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportList"), HttpPost]
        [DisplayName("Export the EquipmentHistory list")]
        public async Task<FileStreamResult> ExportReport(VmsEquipmentHistoryListExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
