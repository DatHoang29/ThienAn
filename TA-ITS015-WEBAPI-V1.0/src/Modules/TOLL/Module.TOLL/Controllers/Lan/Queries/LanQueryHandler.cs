using Module.TOLL.Core.Dto.Lan;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TOLL.Controllers.Lan.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn danh sách làn thu phí (bảng TollLane)
    /// Created date: 08/08/2026
    /// </summary>
    public class LanQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollLane> _lanRep;
        private BaseCacheService _cache;

        public LanQueryHandler(BaseRepository<TollLane> lanRep, BaseCacheService cache)
        {
            _lanRep = lanRep;
            _cache = cache;
        }

        /// <summary>
        /// Danh sách làn thu phí theo điều kiện tìm kiếm (phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<LanOutput>> HandleAsync(PageLanInput input)
        {
            if (string.Equals(input.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                input.DescStr = "desc";
            }

            return await BuildQuery(input.Keyword, input.StationId, input.Direction)
                .OrderBuilder(input, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(input.Page, input.PageSize);
        }

        /// <summary>
        /// Toàn bộ danh sách làn thu phí theo điều kiện (không phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// Update: 28/09/2026 - Lấy từ cache (CacheConst.Toll.TollLane) rồi lọc trong bộ nhớ;
        ///     cache được xoá khi bấm "Lấy dữ liệu" (TollCacheService).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task<List<LanOutput>> HandleAsync(LanInput input)
        {
            var list = _cache.GetOrAdd(CacheConst.Toll.TollLane, _ =>
            {
                return BuildQuery(null, null, null)
                    .OrderBy(a => a.LaneId)
                    .ToList();
            }) ?? new List<LanOutput>();

            var result = list
                .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword),
                    a => (a.LaneId != null && a.LaneId.Contains(input.Keyword!, StringComparison.OrdinalIgnoreCase))
                      || (a.Name != null && a.Name.Contains(input.Keyword!, StringComparison.OrdinalIgnoreCase)))
                .WhereIF(!string.IsNullOrWhiteSpace(input.StationId), a => a.StationId == input.StationId)
                .WhereIF(!string.IsNullOrWhiteSpace(input.Direction), a => a.Direction == input.Direction)
                .ToList();

            return Task.FromResult(result);
        }

        /// <summary>
        /// Dựng câu truy vấn dùng chung cho cả 2 chế độ (phân trang / danh sách)
        /// </summary>
        private ISugarQueryable<LanOutput> BuildQuery(string? keyword, string? stationId, string? direction)
        {
            return _lanRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(keyword),
                    a => a.LaneId.Contains(keyword) || a.Name.Contains(keyword))
                .WhereIF(!string.IsNullOrWhiteSpace(stationId), a => a.StationId == stationId)
                .WhereIF(!string.IsNullOrWhiteSpace(direction), a => a.Direction == direction)
                .Select(a => new LanOutput
                {
                    ID = a.ID,
                    LaneId = a.LaneId,
                    Name = a.Name,
                    Remark = a.Note, // Đồng bộ TCS ghi chú vào cột Note (FillLane)
                    StationId = a.StationId,
                    Direction = a.Direction,
                    LaneBE = a.LaneBE,
                    LastSyncTime = a.LastSyncTime
                }, true);
        }
    }
}
