using System.Diagnostics;
using Mapster;
using Microsoft.Extensions.Localization;
using Modules.Report.Base.Controllers.Management.Dto;
using Modules.Sample.Report.Controllers.CatergoryCarboneReport.Dto;
using Refit;
using Shared.DTO.Dtos.Sample;
using Shared.Infrastructure.Utilities.BaseApiClient;
using Shared.Utility.Apis.Sample;
using Shared.Utility.Apis.System;
using Shared.Utility.Endpoints;
using Wolverine;

namespace Modules.Sample.Report.Controllers.CatergoryCarboneReport.Queries
{
    public class CategoryCQueryHandler
    {


        IMessageBus _messBus;
        private const string reportCode = "CategoryCarboneDemo";

        public CategoryCQueryHandler(IMessageBus messBus)
        {
            _messBus = messBus;
        }

        /// <summary>
        /// Render report
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public  async Task<FileStreamResult> HandleAsync(CategoryCRenderInput command)
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
        public  async Task<FileStreamResult> HandleAsync(CategoryCExportInput command)
        {
            //Xử lý lấy DL
            var dataStr = await _messBus.InvokeAsync<string>(new DataReportInput()
            {
                ReportDataId = command.ReportDataId
            });

            var data = new CategoryCOutput();
            //Nếu dữ liệu trống >> query lại DB
            if (string.IsNullOrEmpty(dataStr))
            {
                data = await QueryData(command);
            }
            else
            {
                data = JsonConvert.DeserializeObject<CategoryCOutput>(dataStr);
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
        private async Task<CategoryCOutput> QueryData(CategoryCRenderInput input)
        {

            //Query dữ liệu từ API sử dụng BaseApiClient (tham khảo thêm document của Refit C#)
            var respone = await BaseApiClient<ISampCategoryApi>.Client().List(input);
            var categoryList = respone.Content?.Result;


            var userResp = await BaseApiClient<ISysAuthApi>.Client().UserInfo();
            var user = userResp.Content?.Result;
            // Nhớ kiểm tra check null,... dữ liệuL
            var data  = categoryList?.Select(x => x.Adapt<Data>()).ToList();
            data?.ForEach(x =>
            {
                x.Random = new Random().Next(1, 100) + "";
            });
            var reportData =  new CategoryCOutput()
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
