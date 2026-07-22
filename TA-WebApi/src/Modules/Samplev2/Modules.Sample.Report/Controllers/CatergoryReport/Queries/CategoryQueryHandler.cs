using Mapster;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Sample.Report.Controllers.CatergoryReport.Dto;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.Sample;
using Shared.Utility.Apis.System;
using Wolverine;

namespace Modules.Sample.Report.Controllers.CatergoryReport.Queries
{
    public class CategoryQueryHandler
    {


        IMessageBus _messBus;
        private const string reportCode = "CategoryCarboneDemo";

        public CategoryQueryHandler(IMessageBus messBus)
        {
            _messBus = messBus;
        }

        /// <summary>
        /// Render report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public  async Task<FileStreamResult> HandleAsync(CategoryRenderInput command)
        {
            //Xử lý dữ liệu query bla bla
            var data = await QueryData(command);

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
        public  async Task<FileStreamResult> HandleAsync(CategoryExportInput command)
        {
            //Xử lý lấy DL
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });

            var data = new CategoryRptOutput();
            //Nếu dữ liệu trống >> query lại DB
            if (string.IsNullOrEmpty(dataStr))
            {
                data = await QueryData(command);
            }
            else
            {
                data = JsonConvert.DeserializeObject<CategoryRptOutput>(dataStr);
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
        private async Task<CategoryRptOutput> QueryData(CategoryRenderInput input)
        {

            //Query dữ liệu từ API sử dụng BaseApiClient (tham khảo thêm document của Refit C#)
            var respone = await BaseApiClient<ISampCategoryApi>.Client().List(input);
            var categoryList = respone.Content?.Result;


            var userResp = await BaseApiClient<ISysAuthApi>.Client().UserInfo();
            var user = userResp.Content?.Result;
            // Nhớ kiểm tra check null,... dữ liệu
            var data  = categoryList?.Select(x => x.Adapt<Data>()).ToList();
            data?.ForEach(x =>
            {
                x.Random = new Random().Next(1, 100) + "";
            });

            //Nếu dữ liệu phức tạp, tách thành các function để xử lý
            var reportData =  new CategoryRptOutput()
            {
                Data = data,
                Parameter = new Parameter()
                {
                    Title = "Hello",
                    SubTitle = "Category Report",
                    Footer = user.Account + "  -  " +  DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
                },
                Column = new Column()
                {
                    Name = "Name",
                    Description = "Description"
                }
            };



            return reportData;

        }



    }
}
