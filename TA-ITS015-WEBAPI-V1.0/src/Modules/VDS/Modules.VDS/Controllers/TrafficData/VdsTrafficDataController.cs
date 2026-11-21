using Modules.VDS.Controllers.TrafficData.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VDS.Controllers.TrafficData
{
    /// <summary>
    /// Author: KienLT
    /// Description: API traffic data từ QuestDB
    /// Created date: 24/08/2026
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName, Order = 584)]
    public class VdsTrafficDataController : BaseController
    {
        /// <summary>
        /// Get VDS traffic data chart of today
        /// </summary>
        /// <param name="input">Bộ lọc thiết bị/vùng/hướng của biểu đồ.</param>
        /// <returns>Dữ liệu EChart của ngày hiện tại.</returns>
        [HttpPost("TodayChart")]
        [DisplayName("Traffic data chart of today")]
        public async Task<Shared.Utility.Helpers.EChartHelper.EChartResponse> TodayChart(TrafficDataToDayChartInput input)
        {
            return await MessBus.InvokeAsync<Shared.Utility.Helpers.EChartHelper.EChartResponse>(input);
        }

        /// <summary>
        /// Get VDS traffic data detail
        /// </summary>
        /// <param name="eventId">Mã bản tin của vendor.</param>
        /// <param name="detectTime">Mốc phát hiện (UTC) — cùng eventId tạo thành khóa kép.</param>
        /// <returns>Bản ghi thô của một lượt xe.</returns>
        /// <remarks>
        /// <c>[Required]</c> ở đây CHỈ để OpenAPI đánh dấu tham số bắt buộc; nó không chặn được <c>0</c> hay
        /// <c>0001-01-01</c>. Miền hợp lệ do <c>VdsEventKeyRules</c> định nghĩa, <c>VdsRecordReaderService</c> kiểm.
        /// </remarks>
        [HttpGet("Detail")]
        [DisplayName("Traffic data detail")]
        public async Task<TrafficDataOutput> Detail([Required] long eventId, [Required] DateTime detectTime)
        {
            return await MessBus.InvokeAsync<TrafficDataOutput>(
                new TrafficDataDetailInput { EventId = eventId, DetectTime = detectTime });
        }

        /// <summary>
        /// Get VDS traffic data pagination list
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc thiết bị/vùng/hướng/biển số/tốc độ/chiều dài.</param>
        /// <returns>Trang danh sách bản ghi thô.</returns>
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageTrafficDataOutput>> Page(PageTrafficDataInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTrafficDataOutput>>(input);
        }

        /// <summary>
        /// Get VDS traffic data list
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc, không phân trang.</param>
        /// <returns>Danh sách bản ghi thô.</returns>
        [DisplayName("List based on parameters")]
        public async Task<List<TrafficDataOutput>> GetList([FromQuery] TrafficDataListInput input)
        {
            return await MessBus.InvokeAsync<List<TrafficDataOutput>>(input);
        }

        /// <summary>
        /// Get VDS traffic volume by hour
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc.</param>
        /// <returns>Lưu lượng xe gộp theo giờ (SAMPLE BY 1h).</returns>
        [HttpPost("VolumeByHour")]
        [DisplayName("Traffic volume by hour")]
        public async Task<List<TrafficDataVolumeOutput>> VolumeByHour(TrafficDataHourVolumeInput input)
        {
            return await MessBus.InvokeAsync<List<TrafficDataVolumeOutput>>(input);
        }

        /// <summary>
        /// Get VDS traffic volume by day
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc.</param>
        /// <returns>Lưu lượng xe gộp theo ngày (SAMPLE BY 1d).</returns>
        [HttpPost("VolumeByDay")]
        [DisplayName("Traffic volume by day")]
        public async Task<List<TrafficDataVolumeOutput>> VolumeByDay(TrafficDataDayVolumeInput input)
        {
            return await MessBus.InvokeAsync<List<TrafficDataVolumeOutput>>(input);
        }

        /// <summary>
        /// Get VDS average speed and vehicle size distribution
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc.</param>
        /// <returns>Tốc độ trung bình và phân bổ cỡ xe.</returns>
        [HttpPost("Average")]
        [DisplayName("Get average speed and vehicle distribution")]
        public async Task<TrafficDataAverageOutput> Average(TrafficDataAverageInput input)
        {
            return await MessBus.InvokeAsync<TrafficDataAverageOutput>(input);
        }

        /// <summary>
        /// Get VDS hourly volume breakdown by direction and vehicle size
        /// </summary>
        /// <param name="input">Cửa sổ thời gian và bộ lọc — cho report ngày/tháng.</param>
        /// <returns>Số đếm theo giờ, tách theo hướng và cỡ xe.</returns>
        [HttpPost("VolumeBreakdown")]
        [DisplayName("Hourly volume breakdown by direction and size")]
        public async Task<List<Shared.DTO.Dtos.Vds.VdsTrafficVolumeBreakdownRow>> VolumeBreakdown(
            TrafficDataVolumeBreakdownInput input)
        {
            return await MessBus.InvokeAsync<List<Shared.DTO.Dtos.Vds.VdsTrafficVolumeBreakdownRow>>(input);
        }
    }
}
