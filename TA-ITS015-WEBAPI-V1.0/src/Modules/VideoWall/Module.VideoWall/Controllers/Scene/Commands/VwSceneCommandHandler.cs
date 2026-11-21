using Mapster;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure.Services.Layout;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;
using Module.VideoWall.Core.Exceptions;

namespace Module.VideoWall.Controllers.Scene.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSceneCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwScene> _vwSceneRep;
        private readonly BaseRepository<VwWindowScene> _vwWindowSceneRep;
        private readonly VwPermissionService _permission;
        private readonly VwWallLayoutService _layout;
        private readonly BaseCacheService _cache;

        public VwSceneCommandHandler(
            BaseRepository<VwScene> vwSceneRep,
            BaseRepository<VwWindowScene> vwWindowSceneRep,
            VwPermissionService permission,
            VwWallLayoutService layout,
            BaseCacheService cache)
        {
            _vwSceneRep = vwSceneRep;
            _vwWindowSceneRep = vwWindowSceneRep;
            _permission = permission;
            _layout = layout;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 VwScene
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwAddSceneInput command)
        {
            // 1. Kiểm tra trùng mã
            command.Code = command.Code?.Trim();
            if (await _vwSceneRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);
            command.CreateUId = null;

            // 1b. Phân quyền vùng màn hình: chưa được cấp vùng nào thì không tạo kịch bản.
            //     Cờ mặc định chỉ đặt qua API SetDefault (SuperAdmin).
            await _permission.EnsureHasAnyAreaAsync();
            if (!_permission.IsSuperAdmin)
                command.IsDefault = BaseEnums.DefaultEnum.None;

            // 1c. Kịch bản luôn dựng trên bố cục đang dùng (client không chọn được bố cục khác)
            command.WallTopologyId = await _layout.GetCurrentIdAsync();

            // 1d. Kịch bản mới chưa có bản lưu trên thiết bị — sao chép từ kịch bản khác không được mang theo
            //     ô kịch bản (OutputId), nếu không 2 kịch bản sẽ ghi đè cùng 1 ô trên thiết bị.
            command.OutputId = null;

            // 2. Ghi DB + xóa cache
            await _vwSceneRep.InsertWithDiffLogAsync(command.Adapt<VwScene>());
            // Dùng RemoveByPrefixKey cho đồng bộ với Update/Delete: xoá cả các key con
            // dạng "vw_scene:*" nếu sau này có thêm.
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScene);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 VwScene
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwUpdateSceneInput command)
        {
            // 1. Kiểm tra trùng mã với bản ghi khác
            command.Code = command.Code?.Trim();
            if (await _vwSceneRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 2. Lấy bản ghi gốc
            var entity = await _vwSceneRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 2b. Phân quyền vùng màn hình: sửa thông tin kịch bản (mã, tên, ghi chú...) chỉ khi MỌI cửa sổ
            //     của kịch bản nằm trong vùng được cấp. Kịch bản phủ nhiều vùng: user chỉ sửa được cửa sổ
            //     trong vùng mình (qua API cửa sổ), không sửa được thông tin kịch bản.
            await _permission.EnsureSceneInsideUserAllowedAreaAsync(entity.ID);

            // Cờ mặc định chỉ đổi qua API SetDefault (SuperAdmin) ⇒ giữ nguyên giá trị cũ.
            // Bố cục gắn khi tạo, không đổi qua Update.
            var originalIsDefault = entity.IsDefault;
            var originalTopologyId = entity.WallTopologyId;
            var originalStatus = entity.Status;

            // 3. Map dữ liệu từ command vào bản ghi gốc.
            //    THIẾU BƯỚC NÀY thì Update trả về thành công nhưng DB không đổi gì.
            command.Adapt(entity);

            // 3b. Ghi chú rỗng ⇒ null (lưu với ignoreAllNullColumns = false để xoá được ghi chú cũ)
            entity.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();
            entity.IsDefault = originalIsDefault;
            entity.WallTopologyId = originalTopologyId;

            // 3c. Đổi bố cục thì kịch bản bố cục cũ bị tắt ⇒ bật lại chỉ khi kịch bản thuộc bố cục đang dùng
            if (entity.Status == BaseEnums.StatusEnum.Enable && originalStatus != BaseEnums.StatusEnum.Enable)
                await _layout.EnsureSceneOnCurrentLayoutAsync(entity);

            // 4. Ghi DB + xóa cache
            await _vwSceneRep.UpdateWithDiffLogAsync(entity, false);
            _cache.Remove(CacheConst.Vw.VwScene);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 VwScene (xóa mềm, kèm các cửa sổ của kịch bản)
        /// Created date: 2026-08-01 — cập nhật 28/10/2026 (xoá luôn cửa sổ)
        /// </summary>
        public async Task HandleAsync(VwDeleteSceneInput command)
        {
            var entity = await _vwSceneRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Phân quyền vùng màn hình: chỉ xoá được kịch bản khi mọi cửa sổ nằm trong vùng được cấp
            var sceneWindows = await _vwWindowSceneRep.AsQueryable()
                .Where(w => w.IsDelete == null && w.SceneId == entity.ID)
                .ToListAsync();
            await _permission.EnsureWindowsInsideUserAllowedAreaAsync(sceneWindows);

            await _vwSceneRep.SoftDeleteAsync<VwScene>(entity);
            await _vwWindowSceneRep.SoftDeleteAsync<VwWindowScene>(u => u.IsDelete == null && u.SceneId == entity.ID);
            _cache.Remove(CacheConst.Vw.VwScene);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều VwScene (xóa mềm, kèm các cửa sổ của kịch bản)
        /// Created date: 2026-08-01 — cập nhật 28/10/2026 (xoá luôn cửa sổ)
        /// </summary>
        public async Task HandleAsync(List<VwDeleteSceneInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _vwSceneRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            // Phân quyền vùng màn hình: chỉ xoá được khi mọi cửa sổ của các kịch bản nằm trong vùng được cấp
            var sceneWindows = await _vwWindowSceneRep.AsQueryable()
                .Where(w => w.IsDelete == null && ids.Contains(w.SceneId!))
                .ToListAsync();
            await _permission.EnsureWindowsInsideUserAllowedAreaAsync(sceneWindows);

            await _vwSceneRep.SoftDeleteAsync<VwScene>(u => ids.Contains(u.ID));
            await _vwWindowSceneRep.SoftDeleteAsync<VwWindowScene>(u => u.IsDelete == null && ids.Contains(u.SceneId));
            _cache.Remove(CacheConst.Vw.VwScene);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwWindowScene);
        }

        /// <summary>
        /// Description: Đặt 1 kịch bản làm mặc định — bỏ cờ ở các kịch bản khác rồi đặt cờ cho kịch bản được chọn,
        ///              trong 1 lần gọi. Kịch bản mặc định là cài đặt của cả tường ⇒ chỉ SuperAdmin.
        /// Created date: 28/09/2026
        /// </summary>
        public async Task HandleAsync(VwSetDefaultSceneInput command)
        {
            _permission.EnsureSuperAdmin(BaseMsg.Vw.Exception.SetDefaultSceneSuperAdminOnly);

            var entity = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Kịch bản thuộc bố cục không đang dùng không áp dụng được ⇒ không đặt làm mặc định
            await _layout.EnsureSceneOnCurrentLayoutAsync(entity);

            // Mỗi bố cục có kịch bản mặc định riêng (26/10/2026) ⇒ chỉ bỏ cờ trong cùng bố cục
            await _vwSceneRep.AsUpdateable()
                .SetColumns(u => new VwScene { IsDefault = BaseEnums.DefaultEnum.None })
                .Where(u => u.IsDelete == null && u.ID != entity.ID && u.IsDefault == BaseEnums.DefaultEnum.Default && u.WallTopologyId == entity.WallTopologyId)
                .ExecuteCommandAsync();

            entity.IsDefault = BaseEnums.DefaultEnum.Default;
            await _vwSceneRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwScene);
        }
    }
}
