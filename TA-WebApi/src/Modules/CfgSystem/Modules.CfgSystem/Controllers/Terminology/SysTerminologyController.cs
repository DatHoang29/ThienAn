using Microsoft.AspNetCore.Mvc;
using Modules.CfgSystem.Controllers.Terminology.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;
using System.ComponentModel;

namespace Modules.CfgSystem.Controllers.Terminology
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class SysTerminologyController: BaseController
    {
        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageSysTerminologyOutput>> Page(PageSysTerminologyInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageSysTerminologyOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<SysTerminologyOutput>> GetList([FromQuery] SysTerminologyInput input)
        {
            return await this.MessBus.InvokeAsync<List<SysTerminologyOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List danh sách theo tham số
        /// </summary>
        [HttpPost("ListByPara")]
        [DisplayName("List based on para")]
        public async Task<List<SysTerminologyOutput>> LisByPara(SysTerminologyParaInput input)
        {
            return await this.MessBus.InvokeAsync<List<SysTerminologyOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Thêm mới một record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddSysTerminology(AddSysTerminologyInput input)
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
        public async Task UpdateSysTerminology(UpdateSysTerminologyInput input)
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
        public async Task DeleteSysTerminology(DeleteSysTerminologyInput input)
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
        [DisplayName("Delete multiple records")]
        public async Task BatchDeleteSysTerminology(List<DeleteSysTerminologyInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
