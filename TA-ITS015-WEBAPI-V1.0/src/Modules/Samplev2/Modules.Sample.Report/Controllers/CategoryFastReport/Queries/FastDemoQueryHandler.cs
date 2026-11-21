using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Sample.Report.Controllers.FastDemo.Dto;
using Wolverine;

namespace Modules.Sample.Report.Controllers.FastDemo.Queries
{
    public class CarboneDemoQueryHandler
    {

        IMessageBus _messBus;
        private const string reportCode = "FastDemo";
        public CarboneDemoQueryHandler(IMessageBus messBus)
        {
            _messBus = messBus;
        }

        /// <summary>
        /// Render report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public  async Task<FileStreamResult> HandleAsync(FastDemoRenderInput command)
        {
            //Xử lý dữ liệu query bla bla
            var data = QueryData(command.Name,command.Code);

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
        public  async Task<FileStreamResult> HandleAsync(FastDemoExportInput command)
        {
            //Xử lý dữ liệu query bla bla
            var data = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });
            if(string.IsNullOrEmpty(data))
                data = QueryData(command.Name,command.Code);

            var report = await _messBus.InvokeAsync<FileStreamResult>(new ExportReportInput()
            {
                Code = reportCode,
                Data = data,
                Format = command.Format //Lưu ý có format
            });
            return report;

        }

        /// <summary>
        /// Query dữ liệu theo điều kiện tìm kiếm
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private string QueryData(string Name,string Code)
        {
            //Query dữ liệu theo điều kiện
            var str = "{\"table\":[{\"id\":\"1\",\"name\":\"son\"},{\"id\":\"2\",\"name\":\"45\"},{\"id\":\"3\",\"name\":\"3\"},{\"id\":\"4\",\"name\":\"4\"}],\"list\":[{\"id\":\"1\",\"name\":\"ff\"},{\"id\":\"2\",\"name\":\"world\"},{\"id\":\"3\",\"name\":\"3\"},{\"id\":\"4\",\"name\":\"4\"}],\"parameter\":{\"image\":\"https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRmCy16nhIbV3pI1qLYHMJKwbH2458oiC9EmA&s\",\"title\":\"123\",\"subtitle\":\"44\",\"footer\":\"effee\"}}";
            var obj = JsonConvert.DeserializeObject<dynamic>(str);
            obj.parameter.title = Name;
            obj.parameter.subtitle = Code;
            return JsonConvert.SerializeObject(obj);
        }

    }
}
