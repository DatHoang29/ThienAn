#region using reference

using System.ComponentModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Modules.VMS.Controllers.Template.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

#endregion

namespace Modules.VMS.Controllers.Template
{
  
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp controller xử lý nghiệp vụ cho đối tượng VmsTemplate
    /// Created date: 09/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class VmsTemplateController: BaseController
    {
        private readonly IWebHostEnvironment _env;
        public VmsTemplateController(IWebHostEnvironment env)
        {
            _env = env;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm phân trang danh sách VMS Template
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="input">Đối tượng PageTemplateOutput</param>
        /// <returns>Danh sách PageTemplateOutput</returns>
        [HttpPost("Page")]
        [DisplayName("List Vms Template pagination")]
        public async Task<SqlSugarPagedList<PageTemplateOutput>> Page(PageTemplateInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTemplateOutput>>(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm list danh sách VmsTemplate theo điều kiện tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="input">Đối tượng TemplateOutput</param>
        /// <returns>Danh sách TemplateOutput</returns>
        [HttpGet("List")]
        [DisplayName("List Vms Template")]
        public async Task<List<TemplateOutput>> GetList([FromQuery] TemplateInput input)
        {
            return await this.MessBus.InvokeAsync<List<TemplateOutput>>(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Thêm mới một record vào bảng VmsTemplate
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="input">Đối tượng AddTemplateInput</param>
        /// <returns></returns>
        [HttpPost("Add")]
        [DisplayName("Add template")]
        public async Task AddVmsTemplate(AddTemplateInput input)
        {
            
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Cập nhật dữ liệu cho bảng VmsTemplate
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="input">Đối tượng UpdateTemplateInput</param>
        /// <returns></returns>
        [HttpPost("Update")]
        [DisplayName("Update the data for the VmsTemplate table")]
        public async Task UpdateVmsTemplate(UpdateTemplateInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa một dòng trong bảng VmsTemplate
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="input">Đối tượng DeleteTemplateInput</param>
        /// <returns></returns>
        [HttpPost("Delete")]
        [DisplayName("Delete a record in the VmsTemplate table")]
        public async Task DeleteVmsTemplate(DeleteTemplateInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều dòng trong bảng VmsTemplate
        /// Created date: 09/08/2024
        /// </summary>
        /// <param name="input">Danh sách DeleteTemplateInput</param>
        /// <returns></returns>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record in the VmsTemplate table")]
        public async Task BatchDeleteVmsTemplate(List<DeleteTemplateInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }


        [HttpGet("DataList/{id}")]
        [DisplayName("Retrieve a list of SysConfigData based on field code")]
        public async Task<List<TemplateOutput>> GetListByCode(string id)
        {
            var list = await MessBus.InvokeAsync<List<TemplateOutput>>(new TemplateInput { ID = id });

            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item.JsonData))
                {
                    var filePath = Path.Combine(_env.WebRootPath, item.JsonData);


                    // Read the content of the file and assign it to a new property or replace the GeoJson field with the content
                    item.JsonData = await File.ReadAllTextAsync(filePath);

                }
            }

            return list;
        }
    }
}
