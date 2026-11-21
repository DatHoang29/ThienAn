using Modules.TMS.Controllers.VehicleFilterHistory.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.VehicleFilterHistory
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 13/11/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsVehicleFilterHistoryController : BaseController
    {
        /// <summary>Lịch sử theo cửa sổ UTC [from, to), tối đa 60 ngày.</summary>
        [HttpPost("WindowPage")]
        [DisplayName("Vehicle filter history window page")]
        public Task<SqlSugarPagedList<VehicleFilterHistoryWindowOutput>> WindowPage(VehicleFilterHistoryWindowInput input)
            => MessBus.InvokeAsync<SqlSugarPagedList<VehicleFilterHistoryWindowOutput>>(input);

        /// <summary>
        /// Author: KienLT
        /// Description: Lịch sử theo CHUYẾN (mỗi dòng một chuyến A → B → C kèm lộ trình camera), cửa sổ UTC tối đa 60 ngày.
        /// Created date: 23/10/2026
        /// </summary>
        [HttpPost("TripPage")]
        [DisplayName("Vehicle filter history trip page")]
        public Task<SqlSugarPagedList<VehicleFilterHistoryTripOutput>> TripPage(VehicleFilterHistoryTripInput input)
            => MessBus.InvokeAsync<SqlSugarPagedList<VehicleFilterHistoryTripOutput>>(input);

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 13/11/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageTmsVehicleFilterHistoryOutput>> Page(PageTmsVehicleFilterHistoryInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsVehicleFilterHistoryOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 13/11/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<TmsVehicleFilterHistoryOutput>> GetList([FromQuery] TmsVehicleFilterHistoryInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsVehicleFilterHistoryOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 13/11/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task<IActionResult> AddTmsVehicleFilterHistory(AddTmsVehicleFilterHistoryInput input)
        {
            var isCreated = await this.MessBus.InvokeAsync<bool>(input);
            return new Microsoft.AspNetCore.Mvc.StatusCodeResult(isCreated ? 201 : 204);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 13/11/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateTmsVehicleFilterHistory(UpdateTmsVehicleFilterHistoryInput input)
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
        public async Task DeleteTmsVehicleFilterHistory(DeleteTmsVehicleFilterHistoryInput input)
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
        public async Task BatchDeleteTmsVehicleFilterHistory(List<DeleteTmsVehicleFilterHistoryInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
