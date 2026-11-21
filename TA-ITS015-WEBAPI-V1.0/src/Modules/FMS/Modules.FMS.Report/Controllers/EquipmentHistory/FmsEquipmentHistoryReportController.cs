using Modules.FMS.Report.Controllers.EquipmentHistory.Dto;

namespace Modules.FMS.Report.Controllers.EquipmentHistory
{
    /// <summary>
    /// Author: ThangNN
    /// Created Date: 04/11/2024
    /// </summary>
    public class FmsEquipmentHistoryReportController: BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: Render danh sách EquipmentHistory
        /// Created Date: 09/12/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "RenderList"), HttpPost]
        [DisplayName("Render the EquipmentHistory list")]
        public async Task<FileStreamResult> RenderReport(EquipmentHistoryListRenderInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Export danh sách EquipmentHistory
        /// Created Date: 09/12/2024
        /// </summary>
        [NonUnify]
        [ApiDescriptionSettings(Name = "ExportList"), HttpPost]
        [DisplayName("Export the EquipmentHistory list")]
        public async Task<FileStreamResult> ExportReport(EquipmentHistoryListExportInput request)
        {
            return await MessBus.InvokeAsync<FileStreamResult>(request);
        }
    }
}
