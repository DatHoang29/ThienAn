using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Report.Demo.Controllers.CarboneDemo.Dto;
using Wolverine;

namespace Modules.Report.Demo.Controllers.CarboneDemo.Queries
{
    public class CarboneDemoQueryHandler
    {

        IMessageBus _messBus;
        private const string reportCode = "CarboneDemo";
        public CarboneDemoQueryHandler(IMessageBus messBus)
        {
            _messBus = messBus;
        }

        /// <summary>
        /// Render report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(CarboneDemoRenderInput command)
        {
            //Xử lý dữ liệu query bla bla
            var data = QueryData(command.Name);

            //save dữ liệu
            var rs = await _messBus.InvokeAsync<bool>(new SaveDataReportInput()
            {
                ReportDataId = command.ReportDataId,
                Data = JsonConvert.SerializeObject(data) // data string
            });


            //render report
            var report = await _messBus.InvokeAsync<FileStreamResult>(new RenderReportInput()
            {
                Code = reportCode,
                Data = data // data object
            });
            return report;

        }

        /// <summary>
        /// Export report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FileStreamResult> HandleAsync(CarboneDemoExportInput command)
        {
            //Xử lý lấy DL
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });

            object? data = new object();
            //Nếu dữ liệu trống >> query lại DB
            if (string.IsNullOrEmpty(dataStr))
            {
                data = QueryData(command.Name);
            }
            else
            {
                data = JsonConvert.DeserializeObject<object>(dataStr ?? "");
            }

            //Export report
            var report = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = reportCode,
                Data = data, // data object
                Format = command.Format //Lưu ý có format
            });
            return report;

        }

        /// <summary>
        /// Query dữ liệu theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private object QueryData(string name)
        {
            //Xử lý query dữ liệu theo điều kiện tìm kiếm
            return null;
        }



    }
}
