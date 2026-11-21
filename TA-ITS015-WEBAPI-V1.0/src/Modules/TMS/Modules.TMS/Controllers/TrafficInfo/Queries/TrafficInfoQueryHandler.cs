using Modules.TMS.Controllers.TrafficInfo.Dto;
using Modules.TMS.Infrastructure.Services.TrafficInfo;

namespace Modules.TMS.Controllers.TrafficInfo.Queries
{
    public class TrafficInfoQueryHandler
    {
        private TrafficInfoService _trafficInfoService;
        public TrafficInfoQueryHandler(TrafficInfoService trafficInfoService)
        {
            _trafficInfoService = trafficInfoService;
        }


        /// <summary>
        /// Xử lý lấy thông tin các vùng zone có sự cố
        /// Author: TuanHA
        /// DateCreated: 2026-04-09
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<List<ListZoneIncidentOutput>> HandleAsync(ListZoneIncidentInput command)
        {
            return await _trafficInfoService.GetIncident();
        }
    }
}
