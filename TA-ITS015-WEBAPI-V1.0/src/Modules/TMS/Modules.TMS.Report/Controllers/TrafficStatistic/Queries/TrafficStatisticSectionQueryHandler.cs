using Modules.Report.Base.Controllers.Management.Dto;
using Modules.TMS.Report.Controllers.TrafficStatistic.Dto;
using Modules.TMS.Report.Infrastructure.Services.TrafficStatistic;
using Wolverine;

namespace Modules.TMS.Report.Controllers.TrafficStatistic.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/12/2024
    /// </summary>
    public class TrafficStatisticSectionQueryHandler : IWolverineHandler
    {
        private IMessageBus _messBus;
        private TrafficStatisticSectionReportService _reportService;

        private const string reportCode = "TrafficStatisticSection";

        public TrafficStatisticSectionQueryHandler(IMessageBus messBus, TrafficStatisticSectionReportService reportService)
        {
            this._messBus = messBus;
            this._reportService = reportService;
        }

        ///// <summary>
        ///// Author: ThangNN
        ///// Description: Render dữ liệu TrafficStatistic cho đoạn tuyến
        ///// Created date: 05/12/2024
        ///// </summary>
        //public async Task<FileStreamResult> HandleAsync(TrafficStatisticSectionRenderInput command)
        //{
        //    var reportData = await this._reportService.BuildTrafficStatisticReport(command);
        //    var reportDataJson = JsonConvert.SerializeObject(reportData);

        //    // Save cache file 
        //    if (!string.IsNullOrWhiteSpace(command.ReportDataId))
        //    {
        //        await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
        //        {
        //            ReportDataId = command.ReportDataId,
        //            Data = reportDataJson
        //        });
        //    }

        //    // Create report file
        //    var rptFileStream = await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
        //    {
        //        Code = _reportCode,
        //        Data = reportDataJson
        //    });

        //    return rptFileStream;
        //}

        ///// <summary>
        ///// Author: ThangNN
        ///// Description: Export dữ liệu TrafficStatistic cho đoạn tuyến
        ///// Created date: 05/12/2024
        ///// </summary>
        //public async Task<FileStreamResult> HandleAsync(TrafficStatisticSectionExportInput command)
        //{
        //    var cacheData = !string.IsNullOrWhiteSpace(command.ReportDataId)
        //        ? await this._messBus.InvokeAsync<string>(new DataReportInput() { ReportDataId = command.ReportDataId })
        //        : string.Empty;

        //    var reportData = !string.IsNullOrWhiteSpace(cacheData)
        //            ? JsonConvert.DeserializeObject<TrafficStatisticReport>(cacheData)
        //            : await this._reportService.BuildTrafficStatisticReport(command);

        //    var reportDataJson = JsonConvert.SerializeObject(reportData);
        //    var rptFileStream = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
        //    {
        //        Code = _reportCode,
        //        Data = reportDataJson,
        //        Format = command.ReportFormat ?? "pdf"
        //    });

        //    return rptFileStream;
        //}


        /// <summary>
        /// Render report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(TrafficStatisticSectionRenderInput command)
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
        public async Task<FileStreamResult> HandleAsync(TrafficStatisticSectionExportInput command)
        {
            //Xử lý dữ liệu query bla bla
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = !string.IsNullOrWhiteSpace(dataStr)
                ? JsonConvert.DeserializeObject<TrafficStatisticReport>(dataStr)
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
        private async Task<TrafficStatisticReport> QueryData(TrafficStatisticSectionRenderInput input)
        {

            return await this._reportService.BuildTrafficStatisticReport(input);

        }
    }
}
