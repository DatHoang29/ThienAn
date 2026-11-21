using Modules.Report.Base.Controllers.Management.Dto;
using Modules.TMS.Report.Controllers.TrafficData.Dto;
using Modules.TMS.Report.Infrastructure.Services.TrafficData;
using Wolverine;

namespace Modules.TMS.Report.Controllers.TrafficData.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create Date: 04/11/2024
    /// </summary>
    public class TrafficDataMonthQueryHandler : IWolverineHandler
    {
        private IMessageBus _messBus;
        private TrafficDataMonthService _reportService;

        private const string reportCode = "TrafficVolumeByMonth";

        public TrafficDataMonthQueryHandler(IMessageBus messBus, TrafficDataMonthService reportService)
        {
            this._messBus = messBus;
            this._reportService = reportService;
        }

        ///// <summary>
        ///// Author: ThangNN
        ///// Created Date: 04/11/2024
        ///// Description: Render lưu lượng xe theo tháng
        ///// </summary>
        //public async Task<FileStreamResult> HandleAsync(TrafficDataMonthRenderInput command)
        //{
        //    var reportData = await this._reportService.BuildTrafficDataMonthReportList(command);
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
        //        Code = reportCode,
        //        Data = reportDataJson
        //    });

        //    return rptFileStream;
        //}

        /////// <summary>
        /////// Author: ThangNN
        ///// Created Date: 04/11/2024
        /////// Description: Export báo cáo lưu lượng xe theo tháng
        /////// </summary>
        //public async Task<FileStreamResult> HandleAsync(TrafficDataMonthExportInput command)
        //{
        //    var cacheData = !string.IsNullOrWhiteSpace(command.ReportDataId)
        //        ? await this._messBus.InvokeAsync<string>(new DataReportInput() { ReportDataId = command.ReportDataId })
        //        : string.Empty;

        //    var reportData = !string.IsNullOrWhiteSpace(cacheData)
        //            ? JsonConvert.DeserializeObject<TrafficDataMonthReport>(cacheData)
        //            : await this._reportService.BuildTrafficDataMonthReportList(command);

        //    var reportDataJson = JsonConvert.SerializeObject(reportData);

        //    var rptFileStream = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
        //    {
        //        Code = reportCode,
        //        Data = reportDataJson,
        //        Format = command.Format ?? "pdf"
        //    });

        //    return rptFileStream;
        //}



        /// <summary>
        /// Render report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(TrafficDataMonthRenderInput command)
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
        public async Task<FileStreamResult> HandleAsync(TrafficDataMonthExportInput command)
        {
            //Xử lý dữ liệu query bla bla
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId,
                DeleteAfterGet = false
            });
            var data = !string.IsNullOrWhiteSpace(dataStr)
                ? JsonConvert.DeserializeObject<TrafficDataMonthReport>(dataStr)
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
        private async Task<TrafficDataMonthReport> QueryData(TrafficDataMonthRenderInput input)
        {
            //var output = new TrafficDataMonthReport();

            return await this._reportService.BuildTrafficDataMonthReportList(input);

            
            //return output;
        }
    }
}
