using Microsoft.AspNetCore.Mvc;
using Modules.CfgSystem.Controllers.ConfigData.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;
using System.ComponentModel;

namespace Modules.CfgSystem.Controllers.ConfigData
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class SysConfigDataController: BaseController
    {
        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageSysConfigDataOutput>> Page(PageSysConfigDataInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageSysConfigDataOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<SysConfigDataOutput>> GetList([FromQuery] SysConfigDataInput input)
        {
            return await this.MessBus.InvokeAsync<List<SysConfigDataOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo config-type
        /// Created date: 09/08/2024
        /// </summary>
        [HttpGet("DataList/{code}")]
        [DisplayName("List based on config-type code")]
        public async Task<List<SysConfigDataOutput>> GetListByCode(string code)
        {
            return await this.MessBus.InvokeAsync<List<SysConfigDataOutput>>(new SysConfigDataInput() { ConfigTypeCode = code });
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Thêm mới một record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a record")]
        public async Task AddSysConfigData(AddSysConfigDataInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Cập nhật một record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateSysConfigData(UpdateSysConfigDataInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa một record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record")]
        public async Task DeleteSysConfigData(DeleteSysConfigDataInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        /// <returns></returns>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteSysConfigData(List<DeleteSysConfigDataInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
