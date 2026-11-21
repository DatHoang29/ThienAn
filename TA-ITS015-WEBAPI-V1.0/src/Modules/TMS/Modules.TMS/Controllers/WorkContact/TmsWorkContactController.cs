using Modules.TMS.Controllers.WorkContact.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.WorkContact
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/11/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsWorkContactController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 05/11/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageTmsWorkContactOutput>> Page(PageTmsWorkContactInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsWorkContactOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 05/11/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search conditions")]
        public async Task<List<TmsWorkContactOutput>> GetList([FromQuery] TmsWorkContactInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsWorkContactOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin theo id hoặc code
        /// Created date: 09/08/2024
        /// </summary>
        [HttpPost("Single")]
        [DisplayName("Get single based on id or code")]
        public async Task<TmsWorkContactOutput> Single(TmsWorkContactSingleInput input)
        {
            return await this.MessBus.InvokeAsync<TmsWorkContactOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 05/11/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddTmsWorkContact(AddTmsWorkContactInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 05/11/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateTmsWorkContact(UpdateTmsWorkContactInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 05/11/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record")]
        public async Task DeleteTmsWorkContact(DeleteTmsWorkContactInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 05/11/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteTmsWorkContact(List<DeleteTmsWorkContactInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
