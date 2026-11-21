using Modules.TMS.Controllers.TrafficData.Dto;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Controllers.TrafficData
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 12/11/2024
    /// </summary>
    [ApiController]
    [ApiDescriptionSettings(GroupName)]
    public class TmsTrafficDataController : BaseController
    {
        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 12/11/2024
        /// </summary>
        [DisplayName("List based on pagination")]
        public async Task<SqlSugarPagedList<PageTmsTrafficDataOutput>> Page(PageTmsTrafficDataInput input)
        {
            return await MessBus.InvokeAsync<SqlSugarPagedList<PageTmsTrafficDataOutput>>(input);

        }

        /// <summary>
        /// Author: ThangNN
        /// Created date: 12/11/2024
        /// Description: List theo tìm kiếm
        /// </summary>
        [HttpPost("List")]
        [DisplayName("List based on parameters")]
        public async Task<List<TrafficDataOutput>> GetList(TrafficDataInput input)
        {
            return await this.MessBus.InvokeAsync<List<TrafficDataOutput>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Traffic-data của một giờ trước đó
        /// Created date: 12/11/2024
        /// </summary>
        [HttpPost("TrafficDataForHourAgo")]
        [DisplayName("List traffic-data from one hour ago")]
        public async Task<List<TmsTrafficDataVolume>> TrafficDataForHourAgo(TmsTrafficDataHourVolumeInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsTrafficDataVolume>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Traffic-data của một ngày trước đó
        /// Created date: 12/11/2024
        /// </summary>
        [HttpPost("TrafficDataForDayAgo")]
        [DisplayName("List traffic-data of day ago")]

        public async Task<List<TmsTrafficDataVolume>> TrafficDataForDayAgo(TmsTrafficDataDayVolumeInput input)
        {
            return await this.MessBus.InvokeAsync<List<TmsTrafficDataVolume>>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Biểu đồ traffic-data của ngày hiện tại
        /// Created date: 12/11/2024
        /// </summary>
        [HttpPost("TrafficDataToDayChart")]
        [DisplayName("Traffic-data today chart")]

        public async Task<TmsTrafficDataToDayChartOutput> TrafficDataToDayChart(TmsTrafficDataToDayChartInput input)
        {
            return await this.MessBus.InvokeAsync<TmsTrafficDataToDayChartOutput>(input);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Biểu đồ vận tốc trung bình hiện tại
        /// Created date: 12/11/2024
        /// </summary>
        [HttpPost("TrafficDataAverage")]
        [DisplayName("Get average based on conditions")]
        public async Task<TmsTrafficDataAverage> TrafficDataAverage(TmsTrafficAverageChartInput input)
        {
            return await this.MessBus.InvokeAsync<TmsTrafficDataAverage>(input);
        }
    }
}
