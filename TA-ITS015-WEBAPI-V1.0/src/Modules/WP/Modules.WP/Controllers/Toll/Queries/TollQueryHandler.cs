using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.WP.Controllers.Toll.Dto;
using Modules.WP.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using SqlSugar;

namespace Modules.WP.Controllers.Toll.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các truy vấn đọc trạm thu phí hệ thống WP (Toll)
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Đọc bảng TollStation, left join TmsWorkUnit (RefId) để lấy mã/tên/điện thoại/
    ///     địa chỉ/thông tin của đơn vị TMS được gán (điểm trạm trên bản đồ).
    /// </summary>
    public class TollQueryHandler
    {
        private readonly BaseRepository<TollStation> _tollRep;
        private readonly BaseCacheService _cache;

        public TollQueryHandler(BaseRepository<TollStation> tollRep, BaseCacheService cache)
        {
            _tollRep = tollRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Toll theo phân trang
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<SqlSugarPagedList<WpPageTollOutput>> HandleAsync(WpPageTollInput command)
        {
            return await BuildQuery(command.StationId, command.Name, command.RefId, command.Active)
                .Select((a, wu) => new WpPageTollOutput
                {
                    RefCode = wu.Code,
                    RefName = wu.Name,
                    RefPhone = wu.Phone,
                    RefAddress = wu.Address,
                    RefInfo = wu.Info,
                }, true)
                .MergeTable()
                .OrderBuilder(command, "", "StationId")
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Toll theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<List<WpTollOutput>> HandleAsync(WpTollInput command)
        {
            return await BuildQuery(command.StationId, command.Name, command.RefId, command.Active)
                .OrderBy(a => a.StationId)
                .Select((a, wu) => new WpTollOutput
                {
                    RefCode = wu.Code,
                    RefName = wu.Name,
                    RefPhone = wu.Phone,
                    RefAddress = wu.Address,
                    RefInfo = wu.Info,
                }, true)
                .ToListAsync();
        }

        // ================== helper ==================

        private ISugarQueryable<TollStation, TmsWorkUnit> BuildQuery(string? stationId, string? name, string? refId, int? active)
        {
            return _tollRep.AsQueryable()
                .LeftJoin<TmsWorkUnit>((a, wu) => wu.ID == a.RefId && wu.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(stationId), (a, wu) => a.StationId.Contains(stationId))
                .WhereIF(!string.IsNullOrWhiteSpace(name), (a, wu) => a.Name.Contains(name))
                .WhereIF(!string.IsNullOrWhiteSpace(refId), (a, wu) => a.RefId == refId)
                .WhereIF(active.HasValue, (a, wu) => a.Active == active);
        }
    }
}
