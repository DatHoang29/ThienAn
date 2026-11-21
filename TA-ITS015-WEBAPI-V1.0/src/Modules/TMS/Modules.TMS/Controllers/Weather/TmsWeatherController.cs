using Modules.TMS.Controllers.Weather.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.Weather
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 01/11/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsWeatherController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 01/11/2024
        /// </summary>
        [HttpPost("Page")]
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageTmsWeatherOutput>> Page(PageTmsWeatherInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsWeatherOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thông tin một record
        /// Created date: 01/11/2024
        /// </summary>
        [HttpGet("Detail")]
        [DisplayName("Get single based on conditions")]
        public async Task<TmsWeatherOutput> Detail([FromQuery] TmsWeatherInput input)
        {
            return await this.MessBus.InvokeAsync<TmsWeatherOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 01/11/2024
        /// </summary>
        [HttpPost("Add")]
        [DisplayName("Add a new record")]
        public async Task AddTmsWeather(AddTmsWeatherInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 01/11/2024
        /// </summary>
        [HttpPost("Update")]
        [DisplayName("Update a record")]
        public async Task UpdateTmsWeather(UpdateTmsWeatherInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 01/11/2024
        /// </summary>
        [HttpPost("Delete")]
        [DisplayName("Delete a record")]
        public async Task DeleteTmsWeather(DeleteTmsWeatherInput input)
        {
            await this.MessBus.InvokeAsync(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 01/11/2024
        /// </summary>
        [HttpPost("BatchDelete")]
        [DisplayName("Delete multiple record")]
        public async Task BatchDeleteTmsWeather(List<DeleteTmsWeatherInput> input)
        {
            await this.MessBus.InvokeAsync(input);
        }
    }
}
