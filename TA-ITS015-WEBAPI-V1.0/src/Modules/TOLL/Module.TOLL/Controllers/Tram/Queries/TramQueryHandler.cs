using Module.TOLL.Core.Dto.Tram;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TOLL.Controllers.Tram.Queries
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Truy vấn danh sách trạm thu phí (bảng TollStation)
    /// Created date: 08/08/2026
    /// </summary>
    public class TramQueryHandler : IWolverineHandler
    {
        private BaseRepository<TollStation> _tramRep;
        private BaseCacheService _cache;

        public TramQueryHandler(BaseRepository<TollStation> tramRep, BaseCacheService cache)
        {
            _tramRep = tramRep;
            _cache = cache;
        }

        /// <summary>
        /// Danh sách trạm thu phí theo điều kiện tìm kiếm (phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// </summary>
        public async Task<SqlSugarPagedList<TramOutput>> HandleAsync(PageTramInput input)
        {
            if (string.Equals(input.Order, "desc", StringComparison.OrdinalIgnoreCase))
            {
                input.DescStr = "desc";
            }

            return await _tramRep.AsQueryable()
                .LeftJoin<TmsWorkUnit>((a, wu) => wu.ID == a.RefId && wu.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword),
                    a => a.StationId.Contains(input.Keyword) || a.Name.Contains(input.Keyword))
                .WhereIF(input.StationType.HasValue, a => a.StationType == input.StationType)
                .WhereIF(input.Active.HasValue, a => a.Active == input.Active)
                .Select((a, wu) => new TramOutput
                {
                    ID = a.ID,
                    StationId = a.StationId,
                    Name = a.Name,
                    Remark = a.Note, // Đồng bộ TCS ghi chú vào cột Note (FillStation)
                    Company = a.Company,
                    StationType = a.StationType,
                    StationBE = a.StationBE,
                    Type = a.Type,
                    Active = a.Active,
                    LastSyncTime = a.LastSyncTime,
                    RefId = a.RefId,
                    RefCode = wu.Code,
                    RefName = wu.Name,
                    CreateTime = a.CreateTime,
                    UpdateTime = a.UpdateTime
                }, true)
                .MergeTable()
                .OrderBuilder(input, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(input.Page, input.PageSize);
        }

        /// <summary>
        /// Toàn bộ danh sách trạm thu phí theo điều kiện (không phân trang)
        /// Author: ChuongLP
        /// Date: 08/08/2026
        /// Update: 28/09/2026 - Lấy từ cache (CacheConst.Toll.TollStation) rồi lọc trong bộ nhớ;
        ///     cache được xoá khi bấm "Lấy dữ liệu" (TollCacheService).
        /// </summary>
        public Task<List<TramOutput>> HandleAsync(TramInput input)
        {
            var list = _cache.GetOrAdd(CacheConst.Toll.TollStation, _ =>
            {
                return _tramRep.AsQueryable()
                    .LeftJoin<TmsWorkUnit>((a, wu) => wu.ID == a.RefId && wu.IsDelete == null)
                    .OrderBy(a => a.StationId)
                    .Select((a, wu) => new TramOutput
                    {
                        ID = a.ID,
                        StationId = a.StationId,
                        Name = a.Name,
                        Remark = a.Note,
                        Company = a.Company,
                        StationType = a.StationType,
                        StationBE = a.StationBE,
                        Type = a.Type,
                        Active = a.Active,
                        LastSyncTime = a.LastSyncTime,
                        RefId = a.RefId,
                        RefCode = wu.Code,
                        RefName = wu.Name
                    }, true)
                    .ToList();
            }) ?? new List<TramOutput>();

            var result = list
                .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword),
                    a => (a.StationId != null && a.StationId.Contains(input.Keyword!, StringComparison.OrdinalIgnoreCase))
                      || (a.Name != null && a.Name.Contains(input.Keyword!, StringComparison.OrdinalIgnoreCase)))
                .WhereIF(input.StationType.HasValue, a => a.StationType == input.StationType)
                .WhereIF(input.Active.HasValue, a => a.Active == input.Active)
                .ToList();

            return Task.FromResult(result);
        }
    }
}
