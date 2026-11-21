using Mapster;
using Module.VideoWall.Core.Dto.EventRule;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure.Services.Layout;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.EventRule.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý thêm / sửa / xóa VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwEventRuleCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwEventRule> _vwEventRuleRep;
        private readonly BaseRepository<VwScene> _vwSceneRep;
        private readonly BaseCacheService _cache;
        private readonly VwPermissionService _permission;
        private readonly VwWallLayoutService _layout;

        public VwEventRuleCommandHandler(
            BaseRepository<VwEventRule> vwEventRuleRep,
            BaseRepository<VwScene> vwSceneRep,
            BaseCacheService cache,
            VwPermissionService permission,
            VwWallLayoutService layout)
        {
            _permission = permission;
            _layout = layout;
            _vwEventRuleRep = vwEventRuleRep;
            _vwSceneRep = vwSceneRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Thêm mới 1 VwEventRule
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwAddEventRuleInput command)
        {
            // 1. Kiểm tra trùng mã
            command.Code = command.Code?.Trim();
            if (await _vwEventRuleRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 1b. Mỗi loại sự kiện chỉ gắn 1 kịch bản: không cho trùng loại sự kiện
            await CheckDuplicateEventTypeAsync(command, null);

            // 2. Kiểm tra khóa ngoại
            await CheckReferenceAsync(command);
            command.CreateUId = null;

            // 2b. Phân quyền vùng màn hình: kịch bản đích phải nằm trọn trong vùng được cấp
            //     (lúc chạy tự động không kiểm tra lại ⇒ chặn ngay khi tạo quy tắc sự kiện)
            await _permission.EnsureSceneInsideUserAllowedAreaAsync(command.TargetSceneId);

            // 3. Ghi DB + xóa cache
            await _vwEventRuleRep.InsertWithDiffLogAsync(command.Adapt<VwEventRule>());
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Cập nhật 1 VwEventRule
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwUpdateEventRuleInput command)
        {
            // 1. Kiểm tra trùng mã với bản ghi khác
            command.Code = command.Code?.Trim();
            if (await _vwEventRuleRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            // 1b. Mỗi loại sự kiện chỉ gắn 1 kịch bản: không cho trùng loại sự kiện với quy tắc khác
            await CheckDuplicateEventTypeAsync(command, command.ID);

            // 2. Lấy bản ghi gốc
            var entity = await _vwEventRuleRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // 2b. Phân quyền vùng màn hình: kịch bản CŨ và kịch bản MỚI đều phải nằm trong vùng được cấp
            //     (chặn đổi quy tắc sự kiện của vùng khác sang kịch bản của mình)
            await _permission.EnsureSceneInsideUserAllowedAreaAsync(entity.TargetSceneId);
            if (command.TargetSceneId != entity.TargetSceneId)
                await _permission.EnsureSceneInsideUserAllowedAreaAsync(command.TargetSceneId);

            //// 4. Kiểm tra khóa ngoại
            await CheckReferenceAsync(command);
            command.Adapt(entity);
            // 5. Ghi DB + xóa cache
            await _vwEventRuleRep.UpdateWithDiffLogAsync(entity, false);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa 1 VwEventRule (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(VwDeleteEventRuleInput command)
        {
            var entity = await _vwEventRuleRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Phân quyền vùng màn hình: không xoá được quy tắc sự kiện của kịch bản ngoài vùng được cấp
            await _permission.EnsureSceneInsideUserAllowedAreaAsync(entity.TargetSceneId);

            await _vwEventRuleRep.SoftDeleteAsync<VwEventRule>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Xóa nhiều VwEventRule (xóa mềm)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task HandleAsync(List<VwDeleteEventRuleInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await _vwEventRuleRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            // Phân quyền vùng màn hình: mọi kịch bản đích phải nằm trong vùng được cấp
            foreach (var sceneId in entities.Select(e => e.TargetSceneId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
                await _permission.EnsureSceneInsideUserAllowedAreaAsync(sceneId);

            await _vwEventRuleRep.SoftDeleteAsync<VwEventRule>(u => ids.Contains(u.ID));
            _cache.RemoveByPrefixKey(CacheConst.Vw.VwEventRule);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Kiểm tra các khóa ngoại của VwEventRule có tồn tại hay không
        /// Created date: 2026-08-01
        /// </summary>
        private async Task CheckReferenceAsync(VwAddEventRuleInput command)
        {
            if (string.IsNullOrWhiteSpace(command.TargetSceneId))
                return;

            var scene = await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == command.TargetSceneId)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.NotExist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.TargetSceneId]);

            // Kịch bản đích phải thuộc bố cục đang dùng (kịch bản bố cục cũ không kích hoạt được)
            await _layout.EnsureSceneOnCurrentLayoutAsync(scene);
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Mỗi loại sự kiện chỉ gắn 1 kịch bản hiển thị: chặn tạo / sửa quy tắc trùng loại sự kiện
        ///              với quy tắc khác (chưa xoá). Một kịch bản vẫn gắn được nhiều loại sự kiện.
        /// Created date: 28/10/2026
        /// </summary>
        private async Task CheckDuplicateEventTypeAsync(VwAddEventRuleInput command, string? excludeId)
        {
            if (string.IsNullOrWhiteSpace(command.EventTypeId))
                return;

            if (await _vwEventRuleRep.IsAnyAsync(u => u.IsDelete == null
                                                    && u.EventTypeId == command.EventTypeId
                                                    && (excludeId == null || u.ID != excludeId)))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseMsg.Vw.Entity.EventTypeId]);
        }
    }
}
