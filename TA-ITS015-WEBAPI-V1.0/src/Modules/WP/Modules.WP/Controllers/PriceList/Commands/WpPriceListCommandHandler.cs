using Mapster;
using Modules.TOLL.Core.Entities;
using Modules.WP.Controllers.PriceList.Dto;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Core.Interfaces;
using Shared.Core.Utilities;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;
using Wolverine;

namespace Modules.WP.Controllers.PriceList.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Add/Update/UpdateStatus/Delete) cho bảng giá hệ thống WP
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Ghi trực tiếp bảng TollPriceList (dự phòng khi WP_Sync lỗi).
    ///     Mã bảng giá = SourceId (TollPriceListDetail.PriceListSourceId tham chiếu mã này),
    ///     hiệu lực = StartDate/EndDate, trạng thái = Active.
    ///     Lưu ý: bản ghi đồng bộ từ ITS10 nếu bị sửa/xóa ở đây sẽ được WP_Sync ghi đè lại ở chu kỳ sau.
    /// </summary>
    public class PriceListCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<TollPriceList> _priceListRep;
        private readonly BaseCacheService _cache;
        private readonly UserManager _userManager;

        public PriceListCommandHandler(BaseRepository<TollPriceList> priceListRep, BaseCacheService cache, UserManager userManager)
        {
            _priceListRep = priceListRep;
            _cache = cache;
            _userManager = userManager;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 Price List
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpAddPriceListInput command)
        {
            command.Name = command.Name?.Trim();
            command.SourceId = command.SourceId?.Trim();

            if (string.IsNullOrWhiteSpace(command.SourceId))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            var isExist = await _priceListRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => u.SourceId == command.SourceId && u.IsDelete == null);
            if (isExist)
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            var duplicateDate = await _priceListRep.AsQueryable()
                .ClearFilter()
                .AnyAsync(u => command.StartDate == u.StartDate && u.IsDelete == null);
            if (duplicateDate)
                throw Oops.Oh(BaseMsg.WP.Validation.DuplicateDate);

            if (command.StartDate >= command.EndDate)
                throw Oops.Oh(BaseMsg.WP.Validation.MustBeValidDateRange);

            command.CreateUId = null;
            await _priceListRep.InsertWithDiffLogAsync(command.Adapt<TollPriceList>());
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 Price List
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpUpdatePriceListInput command)
        {
            command.Name = command.Name?.Trim();
            command.SourceId = command.SourceId?.Trim();

            if (string.IsNullOrWhiteSpace(command.SourceId))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            var priceList = await _priceListRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.ID == command.ID)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Không cho tắt trực tiếp bảng giá đang hoạt động (dùng UpdateStatus)
            if (priceList.Active == true && command.Active == false)
                throw Oops.Oh(BaseMsg.WP.Exception.ActivePriceListCannotBeDisabled);

            // Check trùng mã
            var isExist = await _priceListRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.IsDelete == null && u.ID != command.ID)
                .AnyAsync(u => u.SourceId == command.SourceId);
            if (isExist)
                throw Oops.Oh(BaseMsg.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);

            if (command.StartDate >= command.EndDate)
                throw Oops.Oh(BaseMsg.WP.Validation.MustBeValidDateRange);

            // Check trùng ngày bắt đầu với bảng giá khác
            var duplicateDate = await _priceListRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.IsDelete == null && u.ID != command.ID)
                .AnyAsync(u => command.StartDate == u.StartDate);
            if (duplicateDate)
                throw Oops.Oh(BaseMsg.WP.Validation.DuplicateDate);

            await _priceListRep.UpdateWithDiffLogAsync(command.Adapt(priceList));
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Tự động cập nhật trạng thái bảng giá theo ngày hiện tại
        ///     (tắt bảng giá hết hạn, chỉ giữ 1 bảng giá đang hiệu lực, cảnh báo trùng/hở ngày 10 ngày tới)
        /// Created date: 03/07/2026
        /// </summary>
        public async Task HandleAsync(WpUpdateStatusPriceListInput command)
        {
            var currentDate = command.CurrentDate ?? DateTime.Now;

            await _priceListRep.AsUpdateable()
                .Where(u =>
                    u.Active == true &&
                    (
                        u.IsDelete != null ||
                        (u.StartDate <= currentDate && u.EndDate != null && u.EndDate < currentDate)
                    ))
                .SetColumns(u => new TollPriceList { Active = false })
                .ExecuteCommandAsync();

            var effectiveIds = await _priceListRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.Active == true)
                .Where(u => u.StartDate <= currentDate && (u.EndDate == null || u.EndDate >= currentDate))
                .OrderByDescending(u => u.StartDate)
                .Select(u => u.ID)
                .ToListAsync();
            if (effectiveIds.Count > 1)
            {
                var idsToDisable = effectiveIds.Skip(1).ToList();
                await _priceListRep.AsUpdateable()
                    .Where(u => idsToDisable.Contains(u.ID))
                    .SetColumns(u => new TollPriceList { Active = false })
                    .ExecuteCommandAsync();
            }
            ClearCache();

            var futureDate = currentDate.AddDays(10);
            var upcoming = await _priceListRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.IsDelete == null && u.Active == true)
                .Where(u => u.StartDate <= futureDate && (u.EndDate == null || u.EndDate >= currentDate))
                .Select(u => new { u.ID, u.StartDate, u.EndDate })
                .ToListAsync();

            var dup = upcoming
                .Where(u => u.StartDate > currentDate)
                .GroupBy(u => u.StartDate!.Value.Date)
                .FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
                throw Oops.Oh(BaseMsg.WP.Exception.DuplicatePriceListEffectiveDate, dup.Count(), dup.Key.ToString("dd/MM/yyyy"));

            DateTime? gapStart = null;
            for (var d = currentDate; d <= futureDate; d = d.AddDays(1))
            {
                var covered = upcoming.Any(u =>
                    u.StartDate!.Value.Date <= d && (u.EndDate == null || u.EndDate.Value.Date >= d));
                if (!covered) { gapStart = d; break; }
            }

            if (gapStart != null)
            {
                throw gapStart == currentDate
                    ? Oops.Oh(BaseMsg.WP.Exception.NoPriceListInEffect)
                    : Oops.Oh(BaseMsg.WP.Exception.PriceListCoverageGap, gapStart.Value.ToString("dd/MM/yyyy"));
            }
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 Price List (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpDeletePriceListInput command)
        {
            var priceList = await _priceListRep.GetFirstAsync(u => u.ID == command.ID)
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Chặn xoá bảng giá đang áp dụng, nếu không web tra cứu của người dân sẽ không còn bảng giá để hiển thị.
            var inUseId = await GetInUsePriceListIdAsync();
            if (!string.IsNullOrEmpty(inUseId) && inUseId == command.ID)
                throw Oops.Oh(BaseMsg.WP.Exception.PriceListInUseCannotDelete);

            await _priceListRep.SoftDeleteAsync<TollPriceList>(priceList);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều Price List (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(List<WpDeletePriceListInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var count = await _priceListRep.AsQueryable().Where(u => ids.Contains(u.ID)).CountAsync();
            if (count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            var inUseId = await GetInUsePriceListIdAsync();
            if (!string.IsNullOrEmpty(inUseId) && ids.Contains(inUseId))
                throw Oops.Oh(BaseMsg.WP.Exception.PriceListInUseCannotDelete);

            await _priceListRep.SoftDeleteAsync<TollPriceList>(l => ids.Contains(l.ID));
            ClearCache();
        }

        // ================== helper ==================

        private async Task<string> GetInUsePriceListIdAsync()
        {
            var currentDate = BaseUtil.TimeServer.GetTimeServer();

            return await _priceListRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.IsDelete == null && u.Active == true)
                .Where(u => u.StartDate <= currentDate && (u.EndDate == null || u.EndDate >= currentDate))
                .OrderByDescending(u => u.StartDate)
                .Select(u => u.ID)
                .FirstAsync();
        }

        /// <summary>Xóa cache bảng giá của WP và của module TOLL (BangGia dùng chung bảng TollPriceList).</summary>
        private void ClearCache()
        {
            _cache.RemoveByPrefixKey(CacheConst.WP.WpPriceList);
            _cache.RemoveByPrefixKey(CacheConst.Toll.TollPriceList);
        }
    }
}
