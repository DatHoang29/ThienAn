using Modules.TMS.Controllers.VehicleRegistration.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.VehicleRegistration
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 19/08/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsVehicleRegistrationController : BaseController
    {
        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo phân trang
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageTmsVehicleRegistrationOutput>> Page(PageTmsVehicleRegistrationInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsVehicleRegistrationOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo tìm kiếm
        /// Created date: 19/08/2024
        /// </summary>
        [HttpGet("List")]
        [DisplayName("List based on search")]
        public async Task<List<TmsVehicleRegistrationOutput>> GetList([FromQuery] TmsVehicleRegistrationInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsVehicleRegistrationOutput>>(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Thêm mới một record
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddTmsVehicleRegistration(AddTmsVehicleRegistrationInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Cập nhật một record
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateTmsVehicleRegistration(UpdateTmsVehicleRegistrationInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa một record
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record")]
        public async Task DeleteTmsVehicleRegistration(DeleteTmsVehicleRegistrationInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa nhiều record
        /// Created date: 19/08/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteTmsVehicleRegistration(List<DeleteTmsVehicleRegistrationInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
