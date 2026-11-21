using Module.TOLL.Core.Dto.CaLamViec;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TOLL.Controllers.CaLamViec.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn danh sách ca làm việc thu phí (bảng TollShift)
    /// Created date: 08/08/2026
    /// </summary>
    public class CaLamViecQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollShift> _caRep;
        private BaseCacheService _cache;

        public CaLamViecQueryHandler(BaseRepository<TollShift> caRep, BaseCacheService cache)
        {
            _caRep = caRep;
            _cache = cache;
        }

        /// <summary>
        /// Danh sách ca làm việc theo điều kiện tìm kiếm (phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<CaLamViecOutput>> HandleAsync(PageCaLamViecInput input)
        {
            if (string.Equals(input.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                input.DescStr = "desc";
            }

            return await BuildQuery(input.Keyword)
                .OrderBuilder(input, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(input.Page, input.PageSize);
        }

        /// <summary>
        /// Toàn bộ danh sách ca làm việc theo điều kiện (không phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// Update: 28/09/2026 - Lấy từ cache (CacheConst.Toll.TollShift) rồi lọc trong bộ nhớ;
        ///     cache được xoá khi bấm "Lấy dữ liệu" (TollCacheService).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task<List<CaLamViecOutput>> HandleAsync(CaLamViecInput input)
        {
            var list = _cache.GetOrAdd(CacheConst.Toll.TollShift, _ =>
            {
                return BuildQuery(null)
                    .OrderBy(a => a.ShiftId)
                    .ToList();
            }) ?? new List<CaLamViecOutput>();

            var result = list
                .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword),
                    a => (a.ShiftId != null && a.ShiftId.Contains(input.Keyword!, StringComparison.OrdinalIgnoreCase))
                      || (a.Name != null && a.Name.Contains(input.Keyword!, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Task.FromResult(result);
        }

        /// <summary>
        /// Dựng câu truy vấn dùng chung cho cả 2 chế độ (phân trang / danh sách)
        /// </summary>
        private ISugarQueryable<CaLamViecOutput> BuildQuery(string? keyword)
        {
            return _caRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(keyword),
                    a => a.ShiftId.Contains(keyword) || a.Name.Contains(keyword))
                .Select(a => new CaLamViecOutput
                {
                    ID = a.ID,
                    ShiftId = a.ShiftId,
                    Name = a.Name,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    Note = a.Note,
                    ShiftBE = a.ShiftBE,
                    LastSyncTime = a.LastSyncTime
                }, true);
        }
    }
}
