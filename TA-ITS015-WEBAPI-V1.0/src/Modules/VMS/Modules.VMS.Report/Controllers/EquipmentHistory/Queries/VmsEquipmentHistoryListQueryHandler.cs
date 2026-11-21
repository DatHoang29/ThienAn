using Modules.Report.Base.Controllers.Management.Dto;
using Modules.VMS.Report.Controllers.EquipmentHistory.Dto;
using Modules.VMS.Report.Infrastructure.Services.EquipmentHistory;
using Wolverine;

namespace Modules.VMS.Report.Controllers.EquipmentHistory.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Handler xử lý truy vấn báo cáo lịch sử thiết bị VMS/LCS
    /// Created date: 07/10/2026
    /// </summary>
    public class VmsEquipmentHistoryListQueryHandler : IWolverineHandler
    {
        private readonly IMessageBus _messBus;
        private readonly VmsEquipmentHistoryListService _reportService;

        // Dùng chung file FR "EquipmentHistoryListReport.frx" của FMS
        private const string reportCode = "EquipmentHistoryListReport";

        public VmsEquipmentHistoryListQueryHandler(IMessageBus messBus, VmsEquipmentHistoryListService reportService)
        {
            _messBus = messBus;
            _reportService = reportService;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Render báo cáo danh sách lịch sử thiết bị
        /// Created date: 07/10/2026
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(VmsEquipmentHistoryListRenderInput command)
        {
            var data = await QueryData(command);

            // Lưu dữ liệu cache report
            await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
            {
                ReportDataId = command.ReportDataId,
                Data = data
            });

            // Render report dùng chung template FastReport của FMS
            var report = await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
            {
                Code = reportCode,
                Data = data
            });

            return report;
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Xuất file báo cáo danh sách lịch sử thiết bị (PDF/Excel)
        /// Created date: 07/10/2026
        /// </summary>
        public async Task<FileStreamResult> HandleAsync(VmsEquipmentHistoryListExportInput command)
        {
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });

            VmsEquipmentHistoryListReport? data = null;
            if (!string.IsNullOrEmpty(dataStr))
            {
                data = JsonConvert.DeserializeObject<VmsEquipmentHistoryListReport>(dataStr);
            }

            if (data == null)
            {
                data = await QueryData(command);
            }

            var report = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = reportCode,
                Data = data,
                Format = command.Format ?? "pdf",
                FileName = $"Report_{DateTime.Now:yyyyMMddHHmmss}"
            });

            return report;
        }

        private async Task<VmsEquipmentHistoryListReport> QueryData(VmsEquipmentHistoryListRenderInput input)
        {
            return await _reportService.BuildEquipmentHistoryListReport(input);
        }
    }
}
