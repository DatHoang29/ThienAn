using Modules.CfgSystem.Core.Dto.OpConfig;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.CfgSystem.Controllers.OpConfig
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class SysOpConfigController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageSysOpConfigOutput>> Page(PageSysOpConfigInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageSysOpConfigOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<SysOpConfigOutput>> GetList([FromQuery] SysOpConfigInput input)
        {
            return await this.MessBus.InvokeAsync<List<SysOpConfigOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin theo id hoặc code
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Single")]
        [DisplayName("Get single based on id or code")]
        public async Task<SysOpConfigOutput> Single(SysOpConfigSingleInput input)
        {
            return await this.MessBus.InvokeAsync<SysOpConfigOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddSysOpConfig(AddSysOpConfigInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateSysOpConfig(UpdateSysOpConfigInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record")]
        public async Task DeleteSysOpConfig(DeleteSysOpConfigInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteSysOpConfig(List<DeleteSysOpConfigInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        [HttpGet("AllDataList")]
        [DisplayName("Get all dictionary list")]
        public async Task<dynamic> GetAllOpConfigList()
        {
            return await MessBus.InvokeAsync<dynamic>(new AllOpConfigListInput());
        }
    }
}
