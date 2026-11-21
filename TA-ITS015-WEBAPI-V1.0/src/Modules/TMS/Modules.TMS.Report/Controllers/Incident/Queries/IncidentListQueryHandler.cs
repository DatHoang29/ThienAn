using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Logging;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.TMS.Report.Controllers.Incident.Dto;
using Modules.TMS.Report.Infrastructure.Services.Incident;
using NewLife;
using Shared.Utility.Utilities;
using Wolverine;

namespace Modules.TMS.Report.Controllers.Incident.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create Date: 04/11/2024
    /// </summary>
    public class IncidentListQueryHandler : IWolverineHandler
    {
        private IMessageBus _messBus;
        private IncidentListService _reportService;
        private IStringLocalizer _localizer;

        private const string reportCode = "IncidentIndexReport";

        public IncidentListQueryHandler(IMessageBus messBus, IStringLocalizer localizer, IncidentListService reportService)
        {
            this._messBus = messBus;
            this._localizer = localizer;
            this._reportService = reportService;
        }

        /// <summary>
        /// Render report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(IncidentListRenderInput command)
        {
            //Xử lý dữ liệu query bla bla
            var data = await QueryData(command);

            //Lưu dữ liệu
            var rs = await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
            {
                ReportDataId = command.ReportDataId,
                Data = data
            });

            //Render report
            var report = await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
            {
                Code = reportCode,
                Data = data
            });
            return report;

        }

        /// <summary>
        /// Export report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(IncidentListExportInput command)
        {
            //Xử lý dữ liệu query bla bla
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = !string.IsNullOrWhiteSpace(dataStr)
                ? JsonConvert.DeserializeObject<IncidentListReport>(dataStr)
                : null;
            if (data == null)
                data = await QueryData(command);

            var report = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = reportCode,
                Data = data,
                Format = command.Format, //Lưu ý có format
                FileName = $"Report_{DateTime.Now.ToString("yyyyMMddHHmmss")}"
            });
            return report;

        }

        /// <summary>
        /// Query dữ liệu theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private async Task<IncidentListReport> QueryData(IncidentListRenderInput input)
        {
            //var output = new IncidentListReport();

            return await this._reportService.BuildIncidentListReport(input, this._localizer);

            
            //return output;
        }
    }
}
