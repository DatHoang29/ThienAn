using Mapster;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.WP.Controllers.Toll.Dto;
using Modules.WP.Core.Exceptions;
using Modules.WP.Infrastructure;
using Shared.Core.Interfaces;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Modules.WP.Controllers.Toll.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý các lệnh ghi (Add/Update/Delete) cho trạm thu phí hệ thống WP
    /// Created date: 03/07/2026
    /// Update: 07/10/2026 - Ghi trực tiếp bảng TollStation (dự phòng khi WP_Sync lỗi), bỏ bảng WpToll.
    ///     - Không trùng StationId (mã ETC) giữa các trạm chưa xóa.
    ///     - Mỗi đơn vị TMS (RefId) chỉ gán cho 1 trạm; RefId phải là đơn vị TMS loại "toll" còn tồn tại.
    ///     - Không xóa trạm đang được tuyến (TollRoad.StationIdIn/Out) sử dụng.
    ///     Lưu ý: bản ghi đồng bộ từ ITS10 nếu bị sửa/xóa ở đây sẽ được WP_Sync ghi đè lại ở chu kỳ sau.
    /// </summary>
    public class TollCommandHandler : IWolverineHandler
    {
        private const string WorkUnitTypeToll = "toll";

        private readonly BaseRepository<TollStation> _tollRep;
        private readonly BaseRepository<TmsWorkUnit> _workUnitRep;
        private readonly BaseRepository<TollRoad> _roadRep;
        private readonly BaseCacheService _cache;
        private readonly UserManager _userManager;

        public TollCommandHandler(
            BaseRepository<TollStation> tollRep,
            BaseRepository<TmsWorkUnit> workUnitRep,
            BaseRepository<TollRoad> roadRep,
            BaseCacheService cache,
            UserManager userManager)
        {
            _tollRep = tollRep;
            _workUnitRep = workUnitRep;
            _roadRep = roadRep;
            _cache = cache;
            _userManager = userManager;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 Toll
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpAddTollInput command)
        {
            Normalize(command);
            await ValidateAsync(command, null);

            var entity = command.Adapt<TollStation>();
            entity.Active ??= 1;
            entity.CreateUId = null;
            await _tollRep.InsertWithDiffLogAsync(entity);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 Toll
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpUpdateTollInput command)
        {
            Normalize(command);

            var station = await _tollRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.ID == command.ID)
                .FirstAsync()
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await ValidateAsync(command, command.ID);

            await _tollRep.UpdateWithDiffLogAsync(command.Adapt(station));
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 Toll (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(WpDeleteTollInput command)
        {
            var station = await _tollRep.GetFirstAsync(u => u.ID == command.ID)
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await EnsureNotInUseAsync(new List<string?> { station.StationId });

            await _tollRep.SoftDeleteAsync<TollStation>(station);
            ClearCache();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều Toll (xóa mềm)
        /// Created date: 14/08/2026
        /// </summary>
        public async Task HandleAsync(List<WpDeleteTollInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var stations = await _tollRep.AsQueryable().Where(u => ids.Contains(u.ID)).ToListAsync();
            if (stations.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await EnsureNotInUseAsync(stations.Select(s => s.StationId).ToList());

            await _tollRep.SoftDeleteAsync<TollStation>(l => ids.Contains(l.ID));
            ClearCache();
        }

        // ================== helper ==================

        private static void Normalize(TollStation e)
        {
            e.StationId = e.StationId?.Trim();
            e.Name = e.Name?.Trim();
            e.RefId = string.IsNullOrWhiteSpace(e.RefId) ? null : e.RefId.Trim();
        }

        /// <summary>Kiểm tra trùng mã trạm, trùng/không hợp lệ đơn vị TMS được gán.</summary>
        private async Task ValidateAsync(TollStation e, string? excludeId)
        {
            if (string.IsNullOrWhiteSpace(e.StationId))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, BaseMsg.BaseEntity.Code);

            var duplicateCode = await _tollRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrEmpty(excludeId), u => u.ID != excludeId)
                .AnyAsync(u => u.StationId == e.StationId);
            if (duplicateCode)
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            if (string.IsNullOrEmpty(e.RefId)) return;

            var workUnitExists = await _workUnitRep.AsQueryable()
                .AnyAsync(w => w.ID == e.RefId && w.Type == WorkUnitTypeToll);
            if (!workUnitExists)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            var refInUse = await _tollRep.AsQueryable()
                .ClearFilter()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrEmpty(excludeId), u => u.ID != excludeId)
                .AnyAsync(u => u.RefId == e.RefId);
            if (refInUse)
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Toll);
        }

        /// <summary>Không cho xóa trạm đang được tuyến (TollRoad) sử dụng — bảng giá sẽ mất trạm.</summary>
        private async Task EnsureNotInUseAsync(List<string?> stationIds)
        {
            var codes = stationIds.Where(c => !string.IsNullOrEmpty(c)).ToList();
            if (codes.Count == 0) return;

            var inUse = await _roadRep.AsQueryable()
                .AnyAsync(r => codes.Contains(r.StationIdIn) || codes.Contains(r.StationIdOut));
            if (inUse)
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse);
        }

        /// <summary>Xóa cache trạm của WP và của module TOLL (Tram dùng chung bảng TollStation).</summary>
        private void ClearCache()
        {
            _cache.RemoveByPrefixKey(CacheConst.WP.WpToll);
            _cache.RemoveByPrefixKey(CacheConst.Toll.TollStation);
        }
    }
}
