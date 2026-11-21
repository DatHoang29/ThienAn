using Module.TOLL.Core.Dto.BangGia;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TOLL.Controllers.BangGia.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Truy vấn danh sách bảng giá thu phí (bảng TollPriceList)
    /// Created date: 07/10/2026
    /// </summary>
    public class BangGiaQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollPriceList> _bangGiaRep;
        private BaseCacheService _cache;

        public BangGiaQueryHandler(BaseRepository<TollPriceList> bangGiaRep, BaseCacheService cache)
        {
            _bangGiaRep = bangGiaRep;
            _cache = cache;
        }

        /// <summary>
        /// Danh sách bảng giá theo điều kiện tìm kiếm (phân trang)
        /// Author: HieuNV
        /// Date: 07/10/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<BangGiaOutput>> HandleAsync(PageBangGiaInput input)
        {
            if (string.Equals(input.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                input.DescStr = "desc";
            }

            return await BuildQuery(input.SourceId, input.Name, input.Active)
                .OrderBuilder(input, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(input.Page, input.PageSize);
        }

        /// <summary>
        /// Toàn bộ danh sách bảng giá theo điều kiện (không phân trang)
        /// Lấy từ cache (CacheConst.Toll.TollPriceList) rồi lọc trong bộ nhớ;
        /// cache được xoá khi bấm "Lấy dữ liệu" (TollCacheService, DataType = Master).
        /// Author: HieuNV
        /// Date: 07/10/2026
        /// </summary>
        /// <returns></returns>
        public Task<List<BangGiaOutput>> HandleAsync(BangGiaInput input)
        {
            var list = _cache.GetOrAdd(CacheConst.Toll.TollPriceList, _ =>
            {
                return BuildQuery(null, null, null)
                    .OrderBy(a => a.StartDate)
                    .ToList();
            }) ?? new List<BangGiaOutput>();

            var result = list
                .WhereIF(!string.IsNullOrWhiteSpace(input.SourceId),
                    a => a.SourceId != null && a.SourceId.Contains(input.SourceId!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(input.Name),
                    a => a.Name != null && a.Name.Contains(input.Name!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(input.Active.HasValue, a => a.Active == input.Active)
                .ToList();

            return Task.FromResult(result);
        }

        /// <summary>
        /// Dựng câu truy vấn dùng chung cho cả 2 chế độ (phân trang / danh sách)
        /// </summary>
        private ISugarQueryable<BangGiaOutput> BuildQuery(string? sourceId, string? name, bool? active)
        {
            return _bangGiaRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(sourceId), a => a.SourceId.Contains(sourceId))
                .WhereIF(!string.IsNullOrWhiteSpace(name), a => a.Name.Contains(name))
                .WhereIF(active.HasValue, a => a.Active == active)
                .Select(a => new BangGiaOutput
                {
                    ID = a.ID,
                    SourceId = a.SourceId,
                    Name = a.Name,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    Active = a.Active,
                    Note = a.Note,
                    SyncTime = a.SyncTime
                }, true);
        }
    }
}
