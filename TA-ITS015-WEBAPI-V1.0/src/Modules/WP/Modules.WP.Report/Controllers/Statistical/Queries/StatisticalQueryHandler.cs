using Modules.Report.Base.Controllers.Management.Dto;
using Modules.WP.Report.Controllers.Statistical.Dto;
using Modules.WP.Report.Infrastructure.Services.Statistical;
using Wolverine;

namespace Modules.WP.Report.Controllers.Statistical.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý render/export báo cáo thống kê lưu lượng truy cập
    /// Create Date: 17/07/2026
    /// Created date: 14/08/2026
    /// </summary>
    public class StatisticalQueryHandler : IWolverineHandler
    {
        private IMessageBus _messBus;
        private StatisticalService _reportService;

        private const string reportCode = "LLTCTN";

        public StatisticalQueryHandler(IMessageBus messBus, StatisticalService reportService)
        {
            this._messBus = messBus;
            this._reportService = reportService;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Render report
        /// Created date: 14/08/2026
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(StatisticalRenderInput command)
        {
            // Xử lý dữ liệu query
            var data = await QueryData(command);

            // Lưu dữ liệu
            var rs = await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
            {
                ReportDataId = command.ReportDataId,
                Data = data
            });

            // Render report
            var report = await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
            {
                Code = reportCode,
                Data = data
            });
            return report;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Export report
        /// Created date: 14/08/2026
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(StatisticalExportInput command)
        {
            // Lấy dữ liệu đã lưu (nếu có)
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = JsonConvert.DeserializeObject<StatisticalReportOutput>(dataStr);
            if (string.IsNullOrEmpty(dataStr))
                data = await QueryData(command);

            var report = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = reportCode,
                Data = data,
                Format = command.Format, // Lưu ý có format
                FileName = $"Report_{DateTime.Now.ToString("yyyyMMddHHmmss")}"
            });
            return report;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Query dữ liệu theo điều kiện tìm kiếm
        /// Created date: 14/08/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private async Task<StatisticalReportOutput> QueryData(StatisticalRenderInput input)
        {
            return await this._reportService.BuildStatisticalReport(input);
        }
    }
}
