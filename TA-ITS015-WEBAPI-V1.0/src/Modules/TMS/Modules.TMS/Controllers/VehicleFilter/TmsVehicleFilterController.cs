using Modules.TMS.Controllers.VehicleFilter.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.VehicleFilter
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 13/11/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsVehicleFilterController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 13/11/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageTmsVehicleFilterOutput>> Page(PageTmsVehicleFilterInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsVehicleFilterOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 13/11/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<TmsVehicleFilterOutput>> GetList([FromQuery] TmsVehicleFilterInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsVehicleFilterOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 13/11/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddTmsVehicleFilter(AddTmsVehicleFilterInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 13/11/2024
        /// </summary>
        /// <param name="input">Đối tượng UpdateTmsVehicleFilterInput</param>
        /// <returns></returns>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateTmsVehicleFilter(UpdateTmsVehicleFilterInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 13/11/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record")]
        public async Task DeleteTmsVehicleFilter(DeleteTmsVehicleFilterInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 13/11/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteTmsVehicleFilter(List<DeleteTmsVehicleFilterInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
