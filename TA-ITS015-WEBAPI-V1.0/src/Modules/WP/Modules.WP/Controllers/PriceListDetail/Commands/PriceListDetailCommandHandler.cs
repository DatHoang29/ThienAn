using Mapster;
using Modules.TOLL.Core.Entities;
using Modules.WP.Controllers.PriceListDetail.Dto;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Core.Interfaces;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.WP.Controllers.PriceListDetail.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Add/Update/Delete) cho chi tiết bảng giá hệ thống WP
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Ghi trực tiếp bảng TollPriceListDetail (dự phòng khi WP_Sync lỗi).
    /// </summary>
    public class PriceListDetailCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<TollPriceListDetail> _priceListDetailRep;
        private readonly BaseRepository<TollPriceList> _priceListRep;
        private readonly BaseRepository<TollRoad> _roadRep;
        private readonly BaseCacheService _cache;
        private readonly UserManager _userManager;

        public PriceListDetailCommandHandler(
            BaseRepository<TollPriceListDetail> priceListDetailRep,
            BaseRepository<TollPriceList> priceListRep,
            BaseRepository<TollRoad> roadRep,
            BaseCacheService cache,
            UserManager userManager)
        {
            _priceListDetailRep = priceListDetailRep;
            _priceListRep = priceListRep;
            _roadRep = roadRep;
            _cache = cache;
            _userManager = userManager;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 Price List Detail
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpAddPriceListDetailInput command)
        {
            Normalize(command);
            await EnsureReferencesAsync(command);

            if (await IsDuplicateAsync(command, null))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Data);

            var entity = command.Adapt<TollPriceListDetail>();
            entity.Active ??= true;
            entity.CreateUId = null;
            await _priceListDetailRep.InsertWithDiffLogAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 Price List Detail
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpUpdatePriceListDetailInput command)
        {
            Normalize(command);

            var detail = await _priceListDetailRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.ID == command.ID)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await EnsureReferencesAsync(command);

            if (await IsDuplicateAsync(command, command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseException.DataExist);

            await _priceListDetailRep.UpdateWithDiffLogAsync(command.Adapt(detail));
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 Price List Detail (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpDeletePriceListDetailInput command)
        {
            var detail = await _priceListDetailRep.GetFirstAsync(u => u.ID == command.ID)
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _priceListDetailRep.SoftDeleteAsync<TollPriceListDetail>(detail);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều Price List Detail (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(List<WpDeletePriceListDetailInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var count = await _priceListDetailRep.AsQueryable().Where(u => ids.Contains(u.ID)).CountAsync();
            if (count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await _priceListDetailRep.SoftDeleteAsync<TollPriceListDetail>(l => ids.Contains(l.ID));
            ClearCache();
        }

        // ================== helper ==================

        private static void Normalize(TollPriceListDetail e)
        {
            e.Code = e.Code?.Trim();
            e.Name = e.Name?.Trim();
            e.PriceListSourceId = e.PriceListSourceId?.Trim();
            e.VehicleTypeSourceId = e.VehicleTypeSourceId?.Trim();
            e.RoadSourceId = e.RoadSourceId?.Trim();
        }

        /// <summary>Bảng giá (PriceListSourceId) và tuyến (RoadSourceId) phải tồn tại.</summary>
        private async Task EnsureReferencesAsync(TollPriceListDetail e)
        {
            var hasPriceList = await _priceListRep.AsQueryable().AnyAsync(u => u.SourceId == e.PriceListSourceId);
            if (!hasPriceList)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            var hasRoad = await _roadRep.AsQueryable().AnyAsync(r => r.SourceId == e.RoadSourceId);
            if (!hasRoad)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
        }

        /// <summary>Trùng khi cùng bảng giá + loại xe + loại vé + tuyến (bỏ qua bản ghi đã xóa mềm và chính nó).</summary>
        private Task<bool> IsDuplicateAsync(TollPriceListDetail e, string? excludeId)
        {
            return _priceListDetailRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrEmpty(excludeId), u => u.ID != excludeId)
                .AnyAsync(u =>
                    u.PriceListSourceId == e.PriceListSourceId &&
                    u.VehicleTypeSourceId == e.VehicleTypeSourceId &&
                    u.TicketTypeSourceId == e.TicketTypeSourceId &&
                    u.RoadSourceId == e.RoadSourceId);
        }

        /// <summary>Xóa cache chi tiết bảng giá của WP và của module TOLL (ChiTietBangGia dùng chung bảng).</summary>
        private void ClearCache()
        {
            _cache.RemoveByPrefixKey(CacheConst.WP.WpPriceListDetail);
            _cache.RemoveByPrefixKey(CacheConst.Toll.TollPriceListDetail);
        }
    }
}
