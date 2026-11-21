using Module.TOLL.Core.Constants;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;

namespace Module.TOLL.Infrastructure.Service.Cache
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Xoá cache danh mục thu phí theo loại dữ liệu đồng bộ (TollSyncDataType).
    ///     Dùng chung cho: lệnh lấy dữ liệu thủ công (ManualFetch) và bộ lắng nghe kết quả
    ///     trên kênh ta.its.control.toll (xem ServiceCollectionExtensions.AddTollInfrastructure).
    /// Created date: 28/09/2026
    /// </summary>
    public class TollCacheService : IScoped
    {
        /// <summary>
        /// Ánh xạ loại dữ liệu đồng bộ -> các cache key bị ảnh hưởng.
        /// "Master" gồm loại xe, loại vé, bảng giá (header) và bảng giá chi tiết (cùng 1 lần kéo từ TCS).
        /// Các loại giao dịch/nhật ký không có cache (truy vấn trực tiếp DB) nên không khai báo.
        /// </summary>
        private static readonly Dictionary<string, string[]> _cacheKeyMap = new(StringComparer.OrdinalIgnoreCase)
        {
            [TollSyncDataType.Master] = new[] { CacheConst.Toll.TollVehicleType, CacheConst.Toll.TollTicketType, CacheConst.Toll.TollPriceList, CacheConst.Toll.TollPriceListDetail },
            [TollSyncDataType.Station] = new[] { CacheConst.Toll.TollStation },
            [TollSyncDataType.Lane] = new[] { CacheConst.Toll.TollLane },
            [TollSyncDataType.Shift] = new[] { CacheConst.Toll.TollShift },
        };

        private readonly BaseCacheService _cache;

        public TollCacheService(BaseCacheService cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Xoá cache theo danh sách loại dữ liệu. Loại không có cache thì bỏ qua.
        /// Author: ChuongLP
        /// Date: 28/09/2026
        /// </summary>
        public void ClearByDataTypes(IEnumerable<string>? dataTypes)
        {
            if (dataTypes == null) return;

            var keys = dataTypes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(x => _cacheKeyMap.TryGetValue(x.Trim(), out var mapped) ? mapped : Array.Empty<string>())
                .Distinct();

            foreach (var key in keys)
                _cache.RemoveByPrefixKey(key);
        }
    }
}
