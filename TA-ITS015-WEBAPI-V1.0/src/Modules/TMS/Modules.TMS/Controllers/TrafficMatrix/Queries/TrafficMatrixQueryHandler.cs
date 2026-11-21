using Modules.TMS.Controllers.TrafficMatrix.Dto;
using Modules.TMS.Infrastructure.Services.TrafficMatrix;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;

namespace Modules.TMS.Controllers.TrafficMatrix.Queries
{
    /// <summary>
    /// Author: KienLT
    /// Description: Query handler của bảng ma trận vận tốc / lưu lượng theo giờ.
    /// Created date: 11/08/2026
    /// </summary>
    public class TrafficMatrixQueryHandler : IWolverineHandler
    {
        private readonly TmsTrafficMatrixService _matrixService;
        private readonly BaseCacheService _cache;

        public TrafficMatrixQueryHandler(TmsTrafficMatrixService matrixService, BaseCacheService cache)
        {
            _matrixService = matrixService;
            _cache = cache;
        }

        /// <summary>
        /// Ma trận theo giờ của một ngày.
        /// <para>
        /// Chỉ cache NGÀY ĐÃ QUA: dữ liệu ngày đó không đổi nữa nên cache là thắng thuần. Ngày hôm nay
        /// vẫn đang có xe chạy vào ⇒ cache sẽ hiển thị số cũ, tệ hơn là chờ thêm một truy vấn gộp.
        /// </para>
        /// </summary>
        public async Task<TrafficMatrixOutput> HandleAsync(TrafficMatrixInput command)
        {
            var valueDate = (BaseUtil.TimeServer.GetTimeServer(command.ValueDate)
                             ?? BaseUtil.TimeServer.GetTimeServer()).Date;
            var today = BaseUtil.TimeServer.GetTimeServer().Date;

            if (valueDate >= today) return await _matrixService.BuildAsync(command);

            var cacheKey = $"{CacheConst.Tms.TmsTrafficMatrix}:{valueDate:yyyyMMdd}"
                           + $":{command.ZoneType}:{command.ZoneId}:{command.Direction}"
                           + $":{command.VehicleType}:{command.CountAllEventTypes}";

            return await _cache.GetOrAdd(cacheKey, async _ => await _matrixService.BuildAsync(command));
        }
    }
}
