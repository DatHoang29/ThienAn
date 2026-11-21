using Mapster;
using Module.VideoWall.Core.Dto.Screen;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Layout;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.Screen.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa VwScreen — màn hình là tài sản.
    ///              Cổng đang cắm (ControllerOutputId) và thông tin thiết bị (serial khi đã cắm, nhiệt độ, giờ chạy…)
    ///              chỉ do đồng bộ thiết bị ghi; web quản lý mã / tên / thông số panel / trạng thái.
    /// Created date: 2026-08-01 — cập nhật 06/11/2026 (tách vị trí sang VwControllerOutput)
    /// </summary>
    public class VwScreenCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwScreen> _vwScreenRep;
        private readonly BaseRepository<VwControllerOutput> _vwOutputRep;
        private readonly BaseCacheService _cache;
        private readonly VwWallLayoutService _layout;

        public VwScreenCommandHandler(BaseRepository<VwScreen> vwScreenRep, BaseRepository<VwControllerOutput> vwOutputRep, BaseCacheService cache, VwWallLayoutService layout)
        {
            _vwScreenRep = vwScreenRep;
            _vwOutputRep = vwOutputRep;
            _cache = cache;
            _layout = layout;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 VwScreen (màn trong kho — chưa cắm cổng nào)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwAddScreenInput command)
        {
            // 1. Kiểm tra trùng mã / serial
            command.Code = command.Code?.Trim();
            command.SerialNumber = string.IsNullOrWhiteSpace(command.SerialNumber) ? null : command.SerialNumber.Trim();
            await EnsureUniqueAsync(null, command.Code, command.SerialNumber);
            command.CreateUId = null;

            // 2. Màn mới luôn ở trong kho, chưa có cổng: đặt lên ô và gán cổng ở sơ đồ (SaveWallMap)
            var entity = command.Adapt<VwScreen>();
            entity.ControllerOutputId = null;
            entity.WallBlockId = null;
            entity.BlockNo = null;
            entity.GridCol = null;
            entity.GridRow = null;
            entity.DeviceSerialNumber = null;
            entity.InstalledAt = null;
            entity.RemovedAt = null;
            VwWallLayoutService.ApplyPanelDefaults(entity, await _layout.GetCurrentAsync());

            // 3. Ghi DB + xóa cache
            await _vwScreenRep.InsertWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 VwScreen — chỉ các trường web quản lý; trường thiết bị giữ nguyên.
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwUpdateScreenInput command)
        {
            // 1. Lấy bản ghi gốc
            var entity = await _vwScreenRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 2. Serial do người dùng khai báo (serial thiết bị báo nằm ở DeviceSerialNumber)
            command.Code = command.Code?.Trim();
            var serial = string.IsNullOrWhiteSpace(command.SerialNumber) ? null : command.SerialNumber.Trim();
            await EnsureUniqueAsync(entity.ID, command.Code, serial);

            // 3. Giữ trường thiết bị + vị trí / cổng (sửa ở sơ đồ), map phần còn lại
            var device = (entity.ControllerOutputId, entity.SoftwareVersion, entity.InstalledAt, entity.RemovedAt,
                entity.Temperature, entity.RunTimeHours, entity.ScreenState, entity.LastCheckedAt, entity.DeviceSerialNumber);
            var place = (entity.WallBlockId, entity.BlockNo, entity.GridCol, entity.GridRow);
            command.Adapt(entity);
            (entity.ControllerOutputId, entity.SoftwareVersion, entity.InstalledAt, entity.RemovedAt,
                entity.Temperature, entity.RunTimeHours, entity.ScreenState, entity.LastCheckedAt, entity.DeviceSerialNumber) = device;
            (entity.WallBlockId, entity.BlockNo, entity.GridCol, entity.GridRow) = place;
            entity.SerialNumber = serial;

            // 4. Ghi DB + xóa cache (false: ghi cả cột null — xoá được giá trị web; trường thiết bị / vị trí đã giữ ở bước 3)
            await _vwScreenRep.UpdateWithDiffLogAsync(entity, false);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwControllerOutput);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 VwScreen (xóa mềm) — chặn màn đang cắm cổng.
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwDeleteScreenInput command)
        {
            var entity = await _vwScreenRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            await EnsureNotAttachedAsync([entity]);
            await _vwScreenRep.SoftDeleteAsync<VwScreen>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều VwScreen (xóa mềm) — chặn màn đang cắm cổng.
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(List<VwDeleteScreenInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _vwScreenRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            await EnsureNotAttachedAsync(entities);
            await _vwScreenRep.SoftDeleteAsync<VwScreen>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScreen);
        }

        /// <summary>
        /// Description: Lưu sơ đồ màn (vị trí + cổng gán cho màn) trong 1 lần — VwWallLayoutService.SaveWallMapAsync.
        /// Created date: 07/11/2026
        /// </summary>
        public async Task HandleAsync(VwSaveWallMapInput command)
            => await _layout.SaveWallMapAsync(command.Items);

        /// <summary>
        /// Description: Mã và serial (nếu có) không trùng màn khác.
        /// Created date: 06/11/2026
        /// </summary>
        private async Task EnsureUniqueAsync(string? exceptId, string? code, string? serial)
        {
            if (await _vwScreenRep.IsAnyAsync(u => u.IsDelete == null && u.Code == code && u.ID != exceptId))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            if (!string.IsNullOrWhiteSpace(serial)
                && await _vwScreenRep.IsAnyAsync(u => u.IsDelete == null && u.SerialNumber == serial && u.ID != exceptId))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.SerialNumber]);
        }

        /// <summary>
        /// Description: Màn đang cắm cổng ra thì không xoá được (tháo màn rồi đồng bộ lại).
        /// Created date: 06/11/2026
        /// </summary>
        private async Task EnsureNotAttachedAsync(List<VwScreen> screens)
        {
            var attached = screens.FirstOrDefault(u => u.ControllerOutputId != null);
            if (attached == null)
                return;

            var output = await _vwOutputRep.GetFirstAsync(u => u.ID == attached.ControllerOutputId);
            throw Oops.Oh(BaseMsg.Vw.Exception.ScreenAttachedToOutput,
                attached.Code ?? attached.Name ?? attached.ID,
                output?.DisplayName ?? output?.Name ?? attached.ControllerOutputId);
        }
    }
}
