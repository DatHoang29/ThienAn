using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.LoaiPhuongTien;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TOLL.Controllers.LoaiPhuongTien.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Truy vấn danh sách loại phương tiện (TollMasterData, DataType = VehicleType)
    /// Created date: 08/08/2026
    /// </summary>
    public class LoaiPhuongTienQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollMasterData> _masterDataRep;
        private BaseCacheService _cache;

        public LoaiPhuongTienQueryHandler(BaseRepository<TollMasterData> masterDataRep, BaseCacheService cache)
        {
            _masterDataRep = masterDataRep;
            _cache = cache;
        }

        /// <summary>
        /// Danh sách loại phương tiện theo điều kiện tìm kiếm (phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<SqlSugarPagedList<LoaiPhuongTienOutput>> HandleAsync(PageLoaiPhuongTienInput input)
        {
            if (string.Equals(input.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                input.DescStr = "desc";
            }

            return await BuildQuery(input.Keyword, input.GroupCode, input.Active)
                .OrderBuilder(input, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(input.Page, input.PageSize);
        }

        /// <summary>
        /// Toàn bộ danh sách loại phương tiện theo điều kiện (không phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// Update: 28/09/2026 - Lấy từ cache (CacheConst.Toll.TollVehicleType) rồi lọc trong bộ nhớ;
        ///     cache được xoá khi bấm "Lấy dữ liệu" (TollCacheService, DataType = Master).
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task<List<LoaiPhuongTienOutput>> HandleAsync(LoaiPhuongTienInput input)
        {
            var list = _cache.GetOrAdd(CacheConst.Toll.TollVehicleType, _ =>
            {
                return BuildQuery(null, null, null)
                    .OrderBy(a => a.SourceId)
                    .ToList();
            }) ?? new List<LoaiPhuongTienOutput>();

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
        /// Dựng câu truy vấn dùng chung. Luôn lọc DataType = VehicleType.
        /// </summary>
        private ISugarQueryable<LoaiPhuongTienOutput> BuildQuery(string? keyword, string? groupCode, int? active)
        {
            return _masterDataRep.AsQueryable()
                .Where(a => a.DataType == TollMasterDataType.VehicleType)
                .WhereIF(!string.IsNullOrWhiteSpace(keyword),
                    a => a.SourceId.Contains(keyword) || a.Name.Contains(keyword))
                .WhereIF(!string.IsNullOrWhiteSpace(groupCode), a => a.GroupCode == groupCode)
                .WhereIF(active.HasValue, a => a.Active == active)
                .Select(a => new LoaiPhuongTienOutput
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
