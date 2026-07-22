using Microsoft.AspNetCore.Mvc;
using Modules.CfgSystem.Controllers.ConfigType.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;
using System.ComponentModel;

namespace Modules.CfgSystem.Controllers.ConfigType
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class SysConfigTypeController: BaseController
    {
        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageSysConfigTypeOutput>> Page(PageSysConfigTypeInput input)
        {
            return await this.MessBus.InvokeAsync<SqlSugarPagedList<PageSysConfigTypeOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Lis theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<SysConfigTypeOutput>> GetList([FromQuery] SysConfigTypeInput input)
        {
            return await this.MessBus.InvokeAsync<List<SysConfigTypeOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Thêm mới 1 record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddSysConfigType(AddSysConfigTypeInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Cập nhật 1 record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateSysConfigType(UpdateSysConfigTypeInput input)
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
        public async Task DeleteSysConfigType(DeleteSysConfigTypeInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteSysConfigType(List<DeleteSysConfigTypeInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }


        [HttpGet("AllDataList")]
        [DisplayName("Get all dictionary list")]
        public async Task<dynamic> GetAllConfigDataList()
        {
            return await MessBus.InvokeAsync<dynamic>(new AllConfigListInput());
        }
    }
}
