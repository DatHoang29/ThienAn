using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.LoaiVe;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TOLL.Controllers.LoaiVe.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn danh sách loại vé (TollMasterData, DataType = TicketType)
    /// Created date: 13/08/2026
    /// </summary>
    public class LoaiVeQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollMasterData> _masterDataRep;
        private BaseCacheService _cache;

        public LoaiVeQueryHandler(BaseRepository<TollMasterData> masterDataRep, BaseCacheService cache)
        {
            _masterDataRep = masterDataRep;
            _cache = cache;
        }

        /// <summary>
        /// Danh sách loại vé theo điều kiện tìm kiếm (phân trang)
        /// Author: ChuongLP
        /// Date: 13/08/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<LoaiVeOutput>> HandleAsync(PageLoaiVeInput input)
        {
            // SourceId là VARCHAR chứa giá trị số (ví dụ "-1", "1", "2"...).
            var sortField = input.Field?.Trim();
            var sortOrder = input.Order?.Trim();
            if (string.Equals(input.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                input.DescStr = "desc";
            }
            bool sortNumericById = string.Equals(sortField, "sourceId", StringComparison.OrdinalIgnoreCase);

            var query = BuildQuery(input.Keyword, input.GroupCode, input.Active);

            if (sortNumericById)
            {
                // TRY_CAST an toàn: bản ghi SourceId không phải số sẽ trả NULL → xuống cuối.
                var dir = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
                query = query.OrderBy($"TRY_CAST(SourceId AS INT) {dir}");
            }
            else
            {
                query = query.OrderBuilder(input, "", BaseConst.SortFieldConst.UpdateOrCreateTime);
            }

            return await query.ToPagedListAsync(input.Page, input.PageSize);
        }

        /// <summary>
        /// Toàn bộ danh sách loại vé theo điều kiện (không phân trang)
        /// Author: ChuongLP
        /// Date: 13/08/2026
        /// Update: 28/09/2026 - Lấy từ cache (CacheConst.Toll.TollTicketType) rồi lọc trong bộ nhớ;
        ///     cache được xoá khi bấm "Lấy dữ liệu" (TollCacheService, DataType = Master).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task<List<LoaiVeOutput>> HandleAsync(LoaiVeInput input)
        {
            var list = _cache.GetOrAdd(CacheConst.Toll.TollTicketType, _ =>
            {
                return BuildQuery(null, null, null)
                    .OrderBy(a => a.SourceId)
                    .ToList();
            }) ?? new List<LoaiVeOutput>();

            var result = list
                .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword),
                    a => (a.SourceId != null && a.SourceId.Contains(input.Keyword!, StringComparison.OrdinalIgnoreCase))
                      || (a.Name != null && a.Name.Contains(input.Keyword!, StringComparison.OrdinalIgnoreCase)))
                .WhereIF(!string.IsNullOrWhiteSpace(input.GroupCode), a => a.GroupCode == input.GroupCode)
                .WhereIF(input.Active.HasValue, a => a.Active == input.Active)
                .ToList();

            return Task.FromResult(result);
        }

        /// <summary>
        /// Dựng câu truy vấn dùng chung. Luôn lọc DataType = TicketType.
        /// </summary>
        private ISugarQueryable<LoaiVeOutput> BuildQuery(string? keyword, string? groupCode, int? active)
        {
            return _masterDataRep.AsQueryable()
                .Where(a => a.DataType == TollMasterDataType.TicketType)
                .WhereIF(!string.IsNullOrWhiteSpace(keyword),
                    a => a.SourceId.Contains(keyword) || a.Name.Contains(keyword))
                .WhereIF(!string.IsNullOrWhiteSpace(groupCode), a => a.GroupCode == groupCode)
                .WhereIF(active.HasValue, a => a.Active == active)
                .Select(a => new LoaiVeOutput
                {
                    ID = a.ID,
                    SourceId = a.SourceId,
                    Name = a.Name,
                    GroupCode = a.GroupCode,
                    Remark = a.Remark,
                    Active = a.Active,
                    LastSyncTime = a.LastSyncTime
                }, true);
        }
    }
}
