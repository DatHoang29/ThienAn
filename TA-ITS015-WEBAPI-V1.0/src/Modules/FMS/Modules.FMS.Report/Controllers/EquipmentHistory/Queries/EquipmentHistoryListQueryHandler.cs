using Modules.FMS.Report.Controllers.EquipmentHistory.Dto;
using Modules.FMS.Report.Infrastructure.Services.EquipmentHistory;
using Modules.Report.Base.Controllers.Management.Dto;
using Wolverine;

namespace Modules.FMS.Report.Controllers.EquipmentHistory.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create Date: 04/11/2024
    /// </summary>
    public class EquipmentHistoryListQueryHandler : IWolverineHandler
    {
        private IMessageBus _messBus;
        private EquipmentHistoryListService _reportService;

        private const string reportCode = "EquipmentHistoryListReport";

        public EquipmentHistoryListQueryHandler(IMessageBus messBus, EquipmentHistoryListService reportService)
        {
            this._messBus = messBus;
            this._reportService = reportService;
        }

        ///// <summary>
        ///// Author: ThangNN
        ///// Description: Render EquipmentHistory report list
        ///// Create Date: 04/11/2024
        ///// </summary>
        //public async Task<FileStreamResult> HandleAsync(EquipmentHistoryListRenderInput command)
        //{
        //    var reportData = await this._reportService.BuildEquipmentHistoryListReport(command);
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
        ///// Description: Export EquipmentHistory report list
        ///// Create Date: 04/11/2024
        ///// </summary>
        //public async Task<FileStreamResult> HandleAsync(EquipmentHistoryListExportInput command)
        //{
        //    var cacheData = !string.IsNullOrWhiteSpace(command.ReportDataId)
        //        ? await this._messBus.InvokeAsync<string>(new DataReportInput() { ReportDataId = command.ReportDataId })
        //        : string.Empty;

        //    var reportData = !string.IsNullOrWhiteSpace(cacheData)
        //            ? JsonConvert.DeserializeObject<EquipmentHistoryListReport>(cacheData)
        //            : await this._reportService.BuildEquipmentHistoryListReport(command);

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
        public async Task<FileStreamResult> HandleAsync(EquipmentHistoryListRenderInput command)
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
        public async Task<FileStreamResult> HandleAsync(EquipmentHistoryListExportInput command)
        {
            //Xử lý dữ liệu query bla bla
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            var data = JsonConvert.DeserializeObject<EquipmentHistoryListReport>(dataStr);
            if (string.IsNullOrEmpty(dataStr))
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
        private async Task<EquipmentHistoryListReport> QueryData(EquipmentHistoryListRenderInput input)
        {
            //var output = new TrafficDataDayReport();

            return await this._reportService.BuildEquipmentHistoryListReport(input);

            //return output;
        }
    }
}
