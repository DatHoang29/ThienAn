using Modules.TOLL.Core.Entities;
using Modules.WP.Controllers.PriceList.Dto;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using SqlSugar;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.WP.Controllers.PriceList.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các truy vấn đọc bảng giá hệ thống WP (PriceList), bao gồm xác định trạng thái sử dụng
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Đọc trực tiếp bảng TollPriceList (Active, StartDate/EndDate, SourceId).
    /// </summary>
    public class PriceListQueryHandler
    {
        private readonly BaseRepository<TollPriceList> _priceListRep;
        private readonly BaseCacheService _cache;

        public PriceListQueryHandler(BaseRepository<TollPriceList> priceListRep, BaseCacheService cache)
        {
            _priceListRep = priceListRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Price List theo phân trang
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<SqlSugarPagedList<WpPagePriceListOutput>> HandleAsync(WpPagePriceListInput command)
        {
            var appliedId = await GetAppliedIdAsync();
            var inUseCode = (int)PriceListUseStateEnum.InUse;
            var pendingCode = (int)PriceListUseStateEnum.UpComming;
            var oldCode = (int)PriceListUseStateEnum.Old;

            return await BuildQuery(command.SourceId, command.Name, command.Active)
                .Select(u => new WpPagePriceListOutput
                {
                    UseState = u.Active != true
                        ? oldCode
                        : (u.ID == appliedId ? inUseCode : pendingCode)
                }, true)
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách Price List theo tiêu chí lọc
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<List<WpPriceListOutput>> HandleAsync(WpPriceListInput command)
        {
            var appliedId = await GetAppliedIdAsync();

            var list = await BuildQuery(command.SourceId, command.Name, command.Active)
                .Select(u => new WpPriceListOutput(), true)
                .ToListAsync();

            foreach (var item in list)
                item.UseState = ResolveUseState(item, appliedId);

            return list;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy bảng giá đang áp dụng tại thời điểm CurrentDate (mặc định: hiện tại)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task<WpPriceListViewDetailOutput> HandleAsync(WpPriceListViewDetailInput command)
        {
            var currentDate = command.CurrentDate ?? DateTime.Now;

            var activePriceList = await ActiveQuery(currentDate)
                .OrderByDescending(u => u.StartDate)
                .Select(u => new WpPriceListViewDetailOutput(), true)
                .ToListAsync();

            if (!activePriceList.Any())
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Bảng có StartDate gần currentDate nhất
            var currentPriceList = activePriceList.First();

            // Nhiều bảng cùng ngày bắt đầu -> dữ liệu không hợp lệ
            if (activePriceList.Count(x => x.StartDate == currentPriceList.StartDate) > 1)
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse);

            return currentPriceList;
        }

        // ================== helper ==================

        private ISugarQueryable<TollPriceList> BuildQuery(string? sourceId, string? name, bool? active)
        {
            return _priceListRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(sourceId), u => u.SourceId.Contains(sourceId))
                .WhereIF(!string.IsNullOrWhiteSpace(name), u => u.Name.Contains(name))
                .WhereIF(active.HasValue, u => u.Active == active)
                .OrderBy(u => u.Active == true ? 0 : 1, OrderByType.Asc)
                .OrderBy(u => u.StartDate, OrderByType.Desc);
        }

        /// <summary>Bảng giá đang hoạt động và còn hiệu lực tại thời điểm chỉ định.</summary>
        private ISugarQueryable<TollPriceList> ActiveQuery(DateTime currentDate)
        {
            return _priceListRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.IsDelete == null && u.Active == true)
                .Where(u => u.StartDate <= currentDate && (u.EndDate == null || u.EndDate >= currentDate));
        }

        /// <summary>ID bảng giá đang áp dụng (StartDate gần nhất trong các bảng còn hiệu lực).</summary>
        private Task<string> GetAppliedIdAsync()
            => ActiveQuery(DateTime.Now)
                .OrderByDescending(u => u.StartDate)
                .Select(u => u.ID)
                .FirstAsync();

        private static int ResolveUseState(TollPriceList p, string appliedId)
        {
            if (p.Active != true)
                return (int)PriceListUseStateEnum.Old;

            if (!string.IsNullOrEmpty(appliedId) && p.ID == appliedId)
                return (int)PriceListUseStateEnum.InUse;

            return (int)PriceListUseStateEnum.UpComming;
        }
    }
}
