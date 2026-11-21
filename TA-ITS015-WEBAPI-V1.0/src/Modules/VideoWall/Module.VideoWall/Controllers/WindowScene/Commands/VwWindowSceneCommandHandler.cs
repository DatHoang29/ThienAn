using Mapster;
using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Dto.WindowScene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Core.Interfaces;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure.Services.Layout;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.WindowScene.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwWindowSceneCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwWindowScene> _vwWindowSceneRep;
        private readonly BaseRepository<VwScene> _vwSceneRep;
        private readonly BaseRepository<VwSource> _vwSourceRep;
        private readonly BaseCacheService _cache;
        private readonly VwPermissionService _permission;
        private readonly VwWallLayoutService _layout;

        public VwWindowSceneCommandHandler(
            BaseRepository<VwWindowScene> vwWindowSceneRep,
            BaseRepository<VwScene> vwSceneRep,
            BaseRepository<VwSource> vwSourceRep,
            BaseCacheService cache,
            VwPermissionService permission,
            VwWallLayoutService layout)
        {
            _vwWindowSceneRep = vwWindowSceneRep;
            _vwSceneRep = vwSceneRep;
            _vwSourceRep = vwSourceRep;
            _cache = cache;
            _permission = permission;
            _layout = layout;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 VwWindowScene
        /// </summary>
        public async Task HandleAsync(VwAddWindowSceneInput command)
        {
            // 1. Kiểm tra trùng mã
            command.Code = command.Code?.Trim();
            if (await _vwWindowSceneRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 2. Kiểm tra khóa ngoại + khối (thuộc bố cục của kịch bản) + vị trí trong khối.
            //    Client gửi WallBlockId hoặc BlockNo; client cũ (WPF) không gửi khối ⇒ khối đầu tiên.
            await CheckReferenceAsync(command, command.SceneId, command.WallBlockId,
                command.BlockNo ?? (string.IsNullOrWhiteSpace(command.WallBlockId) ? 0 : null),
                command.X ?? 0, command.Y ?? 0, command.W ?? 0, command.H ?? 0);
            await EnsureSourceNotUsedInSceneAsync(command.SceneId, command.SourceId, null);
            command.CreateUId = null;

            var entity = command.Adapt<VwWindowScene>();

            // 3. Ghi DB + xóa cache
            // Chỉ ghi DB — kịch bản lên tường khi được Áp dụng (10/11/2026)
            await _vwWindowSceneRep.InsertWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 VwWindowScene
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwUpdateWindowSceneInput command)
        {
            // 1. Kiểm tra trùng mã với bản ghi khác
            command.Code = command.Code?.Trim();
            if (await _vwWindowSceneRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 2. Lấy bản ghi gốc
            var entity = await _vwWindowSceneRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 3a. Vị trí CŨ cũng phải thuộc vùng được cấp — chặn kéo cửa sổ của vùng khác về vùng mình
            await _permission.EnsureWindowInsideUserAllowedAreaAsync(entity.BlockNo, entity.X ?? 0, entity.Y ?? 0, entity.W ?? 0, entity.H ?? 0, entity.Name ?? entity.ID);

            // 3b. Kiểm tra khóa ngoại + khối + vị trí MỚI (trường không gửi thì giữ giá trị cũ) nằm trong khối và vùng được cấp.
            //     Cửa sổ không chuyển sang kịch bản khác.
            command.SceneId = entity.SceneId;
            var wallBlockId = string.IsNullOrWhiteSpace(command.WallBlockId) ? entity.WallBlockId : command.WallBlockId;
            var blockNo = command.BlockNo ?? (string.IsNullOrWhiteSpace(wallBlockId) ? entity.BlockNo ?? 0 : null);
            await CheckReferenceAsync(command, entity.SceneId, wallBlockId, blockNo,
                command.X ?? entity.X ?? 0, command.Y ?? entity.Y ?? 0, command.W ?? entity.W ?? 0, command.H ?? entity.H ?? 0);

            // 3c. Đổi nguồn ⇒ nguồn mới chưa được cửa sổ khác của kịch bản dùng.
            //     Chỉ kiểm khi đổi nguồn để dữ liệu cũ đang trùng vẫn sửa được vị trí / kích thước.
            if (!string.IsNullOrWhiteSpace(command.SourceId) && command.SourceId != entity.SourceId)
                await EnsureSourceNotUsedInSceneAsync(entity.SceneId, command.SourceId, entity.ID);

            // 4. Map dữ liệu từ command vào bản ghi gốc.
            //    THIẾU BƯỚC NÀY thì Update trả về thành công nhưng DB không đổi gì.
            command.Adapt(entity);

            // 5. Ghi DB + xóa cache (chỉ DB — kịch bản lên tường khi được Áp dụng)
            await _vwWindowSceneRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 VwWindowScene (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwDeleteWindowSceneInput command)
        {
            var entity = await _vwWindowSceneRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Phân quyền vùng màn hình: cửa sổ phải nằm trong vùng được cấp của người dùng
            await _permission.EnsureWindowInsideUserAllowedAreaAsync(entity.BlockNo, entity.X ?? 0, entity.Y ?? 0, entity.W ?? 0, entity.H ?? 0, entity.Name ?? entity.ID);

            // Chỉ ghi DB — kịch bản lên tường khi được Áp dụng
            await _vwWindowSceneRep.SoftDeleteAsync<VwWindowScene>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều VwWindowScene (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(List<VwDeleteWindowSceneInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _vwWindowSceneRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            // Chỉ cho xóa khi TẤT CẢ bản ghi đều nằm trong vùng lưới được cấp
            await _permission.EnsureWindowsInsideUserAllowedAreaAsync(entities);

            // Chỉ ghi DB — kịch bản lên tường khi được Áp dụng
            await _vwWindowSceneRep.SoftDeleteAsync<VwWindowScene>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Xử lý đổi nguồn tín hiệu trực tiếp cho 1 cửa sổ trên Video Wall.
        ///              Kiểm tra quyền truy cập, cập nhật SourceId trong DB và đồng bộ xuống thiết bị vật lý.
        /// Created date: 17/08/2026
        /// </summary>
        public async Task HandleAsync(VwSwitchWindowSourceInput command)
        {
            var entity = await _vwWindowSceneRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Kịch bản của cửa sổ phải thuộc bố cục đang dùng
            await EnsureSceneOnCurrentLayoutAsync(entity.SceneId);

            // Phân quyền vùng màn hình: cửa sổ phải nằm trong vùng được cấp của người dùng
            await _permission.EnsureWindowInsideUserAllowedAreaAsync(entity.BlockNo, entity.X ?? 0, entity.Y ?? 0, entity.W ?? 0, entity.H ?? 0, entity.Name ?? entity.ID);

            var source = await _vwSourceRep.GetFirstAsync(s => s.ID == command.SourceId)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            if (command.SourceId != entity.SourceId)
                await EnsureSourceNotUsedInSceneAsync(entity.SceneId, command.SourceId, entity.ID);

            entity.SourceId = command.SourceId;

            // Chỉ ghi DB — kể cả kịch bản đang phát, nguồn mới lên tường khi bấm Áp dụng (10/11/2026)
            await _vwWindowSceneRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>
        /// Description: Xử lý điều chỉnh thứ tự xếp lớp Z-Order (Top / Bottom) của cửa sổ trên Video Wall.
        /// Created date: 17/08/2026
        /// </summary>
        public async Task HandleAsync(VwSetWindowLayerInput command)
        {
            var entity = await _vwWindowSceneRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Kịch bản của cửa sổ phải thuộc bố cục đang dùng
            await EnsureSceneOnCurrentLayoutAsync(entity.SceneId);

            // Phân quyền vùng màn hình: cửa sổ phải nằm trong vùng được cấp của người dùng
            await _permission.EnsureWindowInsideUserAllowedAreaAsync(entity.BlockNo, entity.X ?? 0, entity.Y ?? 0, entity.W ?? 0, entity.H ?? 0, entity.Name ?? entity.ID);

            var scene = await _vwSceneRep.GetFirstAsync(s => s.ID == entity.SceneId);
            //if (scene?.ControllerId != null && !string.IsNullOrWhiteSpace(entity.DeviceWindowId))
            //{
            //    // Phát lệnh đổi layer qua NATS (Luồng A - Fire-and-forget)
            //    _publisher.PublishCommand(
            //        VwCommandActions.SetWindowLayer,
            //        new VwSetWindowLayerPayload
            //        {
            //            ControllerId = scene.ControllerId,
            //            DeviceWindowId = entity.DeviceWindowId,
            //            Action = command.Action.ToString()
            //        },
            //        controllerId: scene.ControllerId,
            //        sceneId: scene.ID);
            //}

            var maxZIndex = await _vwWindowSceneRep.AsQueryable()
                .Where(w => w.SceneId == entity.SceneId)
                .MaxAsync(w => w.ZIndex) ?? 0;

            entity.ZIndex = command.Action == VwWindowLayerAction.Top ? maxZIndex + 1 : 0;
            await _vwWindowSceneRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        #region Private Helpers

        /// <summary>
        /// Author: Đạt
        /// Description: Kiểm tra các khóa ngoại của VwWindowScene có tồn tại hay không, và vị trí MỚI
        ///              (khối, x, y, w, h — toạ độ trong khối) nằm trọn trong khối và trong vùng được cấp.
        ///              Khối tra trong bố cục của kịch bản (phải là bố cục đang dùng): có blockNo thì theo blockNo
        ///              (kéo cửa sổ sang khối khác), không có thì theo wallBlockId.
        ///              Ghi lại WallBlockId + BlockNo (đã chuẩn hoá) vào command.
        /// Created date: 2026-08-01 — cập nhật 22/10/2026 (vị trí theo khối, khối gắn bố cục của kịch bản)
        /// </summary>
        private async Task CheckReferenceAsync(VwAddWindowSceneInput command, string? sceneId, string? wallBlockId, int? blockNo, int x, int y, int w, int h)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
                throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.SceneId]);
            var scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == sceneId)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.SceneId]);
            if (!string.IsNullOrWhiteSpace(command.SourceId)
                && !await _vwSourceRep.IsAnyAsync(u => u.IsDelete == null && u.ID == command.SourceId))
                throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.SourceId]);

            // Chỉ dựng / sửa cửa sổ của kịch bản thuộc bố cục đang dùng ⇒ bố cục kiểm vị trí chính là bố cục đang dùng
            await _layout.EnsureSceneOnCurrentLayoutAsync(scene);
            var layout = await _layout.GetCurrentAsync();

            // Khối phải thuộc bố cục của kịch bản; cửa sổ nằm trọn trong 1 khối (không vắt qua 2 khối)
            var label = command.Label ?? command.Name;
            var block = blockNo != null
                ? VwWallLayoutService.FindBlock(layout, blockNo)
                : layout.Blocks.FirstOrDefault(u => u.ID == wallBlockId);
            if (block == null || !VwWallLayoutService.IsRectInside(layout, block.BlockNo, x, y, w, h))
                throw Oops.Oh(BaseMsg.Vw.Exception.WindowOutsideWallBlock, label ?? string.Empty, VwWallLayoutService.BlockLabel(block?.BlockNo ?? blockNo));

            command.WallBlockId = block.ID;
            command.BlockNo = block.BlockNo;

            // Phân quyền vùng màn hình: vị trí MỚI của cửa sổ phải nằm trong vùng được cấp của người dùng.
            // Bỏ qua khi toàn quyền (SuperAdmin / tiến trình nền / IsFullAccess); chưa được cấp vùng ⇒ chặn.
            await _permission.EnsureWindowInsideUserAllowedAreaAsync(block.BlockNo, x, y, w, h, label);
        }

        /// <summary>
        /// Description: Mỗi nguồn chỉ hiển thị ở 1 cửa sổ trong một kịch bản — chặn thêm / đổi sang nguồn
        ///              đã có cửa sổ khác (chưa xoá) của cùng kịch bản dùng. excludeWindowId: cửa sổ đang sửa.
        /// Created date: 22/10/2026
        /// </summary>
        private async Task EnsureSourceNotUsedInSceneAsync(string? sceneId, string? sourceId, string? excludeWindowId)
        {
            if (string.IsNullOrWhiteSpace(sceneId) || string.IsNullOrWhiteSpace(sourceId))
                return;

            var used = await _vwWindowSceneRep.IsAnyAsync(u => u.IsDelete == null && u.SceneId == sceneId && u.SourceId == sourceId
                && (excludeWindowId == null || u.ID != excludeWindowId));
            if (!used)
                return;

            var source = await _vwSourceRep.GetFirstAsync(u => u.ID == sourceId);
            throw Oops.Oh(BaseMsg.Vw.Exception.SourceUsedInScene, source?.Name ?? source?.Code ?? sourceId);
        }

        /// <summary>
        /// Description: Kịch bản (theo ID) phải thuộc bố cục đang dùng. Không tìm thấy kịch bản thì bỏ qua.
        /// Created date: 22/10/2026
        /// </summary>
        private async Task EnsureSceneOnCurrentLayoutAsync(string? sceneId)
        {
            if (string.IsNullOrWhiteSpace(sceneId))
                return;
            var scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == sceneId);
            if (scene != null)
                await _layout.EnsureSceneOnCurrentLayoutAsync(scene);
        }

        #endregion
    }
}
