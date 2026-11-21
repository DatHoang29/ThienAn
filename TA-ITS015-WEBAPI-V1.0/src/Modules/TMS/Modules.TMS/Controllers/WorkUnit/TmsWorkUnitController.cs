using Modules.TMS.Controllers.WorkUnit.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.WorkUnit
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 25/11/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsWorkUnitController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 25/11/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageTmsWorkUnitOutput>> Page(PageTmsWorkUnitInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsWorkUnitOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 25/11/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<TmsWorkUnitOutput>> GetList([FromQuery] TmsWorkUnitInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsWorkUnitOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin chi tiết
        /// Created date: 25/11/2024
        /// </summary>
        [HttpPost("Single")]
        [DisplayName("Get single based on id or code")]
        public async Task<TmsWorkUnitOutput> Single(TmsWorkUnitSingleInput input)
        {
            return await this.MessBus.InvokeAsync<TmsWorkUnitOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record
        /// Created date: 25/11/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddTmsWorkUnit(AddTmsWorkUnitInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 25/11/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateTmsWorkUnit(UpdateTmsWorkUnitInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 25/11/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record")]
        public async Task DeleteTmsWorkUnit(DeleteTmsWorkUnitInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 25/11/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteTmsWorkUnit(List<DeleteTmsWorkUnitInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo vị trí
        /// Created date: 25/11/2024
        /// </summary>
        [HttpPost("ListByPosition")]
        [DisplayName("List TmsWorkUnitOutput based on position")]
        public async Task<List<TmsWorkUnitOutput>> ListTmsWorkUnitByPosition(TmsWorkUnitPositionInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsWorkUnitOutput>>(input);
        }
    }
}
