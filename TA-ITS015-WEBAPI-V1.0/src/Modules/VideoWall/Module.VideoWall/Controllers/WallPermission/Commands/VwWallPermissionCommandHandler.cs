using Shared.DTO.Enums;
using Mapster;
using Module.VideoWall.Core.Dto.WallPermission;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Layout;
using Shared.Core.Extensions;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Wolverine;
using Module.VideoWall.Core.Exceptions;

namespace Module.VideoWall.Controllers.WallPermission.Commands
{
    /// <summary>
    /// Description: Xử lý thêm / sửa / xóa VwWallPermission (kèm danh sách đối tượng VwWallPermissionMember)
    /// Created date: 11/09/2026
    /// </summary>
    public class VwWallPermissionCommandHandler(
        BaseRepository<VwWallPermission> PermissionRep,
        BaseRepository<VwWallPermissionMember> MemberRep,
        BaseCacheService Cache,
        VwWallLayoutService Layout) : IWolverineHandler
    {
        /// <summary>
        /// Description: Thêm mới 1 VwWallPermission
        /// Created date: 11/09/2026
        /// </summary>
        public async Task HandleAsync(VwAddWallPermissionInput command)
        {
            // Quyền thao tác do menu / nút (v-auth) của hệ thống quản lý — không còn chặn SuperAdmin tại đây.
            Normalize(command);

            if (await PermissionRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            await NormalizeAreaAsync(command);

            command.CreateUId = null;
            command.Status ??= BaseEnums.StatusEnum.Enable;
            // Quyền gắn với bố cục đang dùng (bố cục phải đã lưu) — Nhân bản quyền cũ cũng đi qua đây ⇒ sang bố cục hiện tại
            command.WallTopologyId = await Layout.GetCurrentIdAsync();

            var entity = command.Adapt<VwWallPermission>();
            await PermissionRep.InsertWithDiffLogAsync(entity);
            await SaveMembersAsync(entity.ID, command.UserIds!, command.OrgIds!);
            Cache.RemoveByPrefixKey(CacheConst.Vw.VwWallPermission);
        }

        /// <summary>
        /// Description: Cập nhật 1 VwWallPermission
        /// Created date: 11/09/2026
        /// </summary>
        public async Task HandleAsync(VwUpdateWallPermissionInput command)
        {
            Normalize(command);

            if (await PermissionRep.IsAnyAsync(u => u.IsDelete == null && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseLocaleManager.BaseException.Exist, Furion.Localization.L.Text[BaseLocaleManager.BaseEntity.Code]);

            var entity = await PermissionRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Quyền của bố cục khác bố cục đang dùng vẫn sửa được: ô kiểm theo lưới của chính bố cục đó, quyền giữ nguyên bố cục
            await NormalizeAreaAsync(command, entity.WallTopologyId);

            var originalTopologyId = entity.WallTopologyId;
            command.Adapt(entity);
            entity.WallTopologyId = originalTopologyId;

            // Gán tay các trường được sửa; lưu với ignoreAllNullColumns = false để xoá được mô tả / vùng về null.
            entity.Code = command.Code;
            entity.Name = command.Name;
            entity.Config = command.Config;
            entity.IsFullAccess = command.IsFullAccess;
            entity.Remark = command.Remark;
            entity.Status = command.Status ?? entity.Status ?? BaseEnums.StatusEnum.Enable;

            await PermissionRep.UpdateWithDiffLogAsync(entity, false);
            await SaveMembersAsync(entity.ID, command.UserIds!, command.OrgIds!);
            Cache.RemoveByPrefixKey(CacheConst.Vw.VwWallPermission);
        }

        /// <summary>
        /// Description: Xóa 1 VwWallPermission (xóa mềm, kèm các đối tượng được gán)
        /// Created date: 11/09/2026
        /// </summary>
        public async Task HandleAsync(VwDeleteWallPermissionInput command)
        {
            var entity = await PermissionRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            await PermissionRep.SoftDeleteAsync<VwWallPermission>(entity);
            await MemberRep.SoftDeleteAsync<VwWallPermissionMember>(u => u.IsDelete == null && u.PermissionId == entity.ID);
            Cache.RemoveByPrefixKey(CacheConst.Vw.VwWallPermission);
        }

        /// <summary>
        /// Description: Xóa nhiều VwWallPermission (xóa mềm, kèm các đối tượng được gán)
        /// Created date: 11/09/2026
        /// </summary>
        public async Task HandleAsync(List<VwDeleteWallPermissionInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();

            var entities = await PermissionRep.AsQueryable()
                .Where(u => ids.Contains(u.ID))
                .ToListAsync();

            if (entities?.Count != ids.Count)
                throw Oops.Oh(BaseLocaleManager.BaseException.DataNotMatch);

            await PermissionRep.SoftDeleteAsync<VwWallPermission>(u => ids.Contains(u.ID));
            await MemberRep.SoftDeleteAsync<VwWallPermissionMember>(u => u.IsDelete == null && ids.Contains(u.PermissionId));
            Cache.RemoveByPrefixKey(CacheConst.Vw.VwWallPermission);
        }

        /// <summary>
        /// Description: Chuẩn hoá đầu vào: chuỗi rỗng ⇒ null, danh sách đối tượng bỏ rỗng / trùng.
        /// Created date: 22/10/2026
        /// </summary>
        private static void Normalize(VwAddWallPermissionInput command)
        {
            command.Code = command.Code?.Trim();
            command.Name = command.Name?.Trim();
            command.Config = string.IsNullOrWhiteSpace(command.Config) ? null : command.Config.Trim();
            command.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();
            command.UserIds = CleanIds(command.UserIds);
            command.OrgIds = CleanIds(command.OrgIds);
        }

        private static List<string> CleanIds(List<string>? ids) =>
            (ids ?? []).Where(u => !string.IsNullOrWhiteSpace(u)).Select(u => u.Trim()).Distinct().ToList();

        /// <summary>
        /// Description: Đồng bộ đối tượng được gán: đã có thì giữ, mới thì thêm, không còn trong danh sách thì xoá mềm.
        /// Created date: 22/10/2026
        /// </summary>
        private async Task SaveMembersAsync(string permissionId, List<string> userIds, List<string> orgIds)
        {
            var current = await MemberRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.PermissionId == permissionId)
                .ToListAsync();

            var currentUserIds = current.Where(u => !string.IsNullOrWhiteSpace(u.UserId)).Select(u => u.UserId!).ToHashSet();
            var currentOrgIds = current.Where(u => string.IsNullOrWhiteSpace(u.UserId) && !string.IsNullOrWhiteSpace(u.OrgId)).Select(u => u.OrgId!).ToHashSet();

            foreach (var userId in userIds.Where(u => !currentUserIds.Contains(u)))
                await MemberRep.InsertWithDiffLogAsync(new VwWallPermissionMember { PermissionId = permissionId, UserId = userId });

            foreach (var orgId in orgIds.Where(u => !currentOrgIds.Contains(u)))
                await MemberRep.InsertWithDiffLogAsync(new VwWallPermissionMember { PermissionId = permissionId, OrgId = orgId });

            var stale = current.Where(u => !string.IsNullOrWhiteSpace(u.UserId)
                ? !userIds.Contains(u.UserId!)
                : !orgIds.Contains(u.OrgId ?? string.Empty));
            foreach (var member in stale)
                await MemberRep.SoftDeleteAsync<VwWallPermissionMember>(member);
        }

        /// <summary>
        /// Description: Chuẩn hoá vùng được cấp theo bố cục của quyền (thêm mới / quyền chưa gắn bố cục ⇒ bố cục đang dùng,
        ///              sửa ⇒ bố cục quyền được cấp). Toàn quyền chỉ bật cờ, không lưu danh sách ô:
        ///              - Ô phải nằm trong bố cục (khối có thật, hàng/cột trong lưới khối); chọn đủ mọi ô ⇒ IsFullAccess = true.
        ///              - IsFullAccess = true ⇒ Config = null.
        ///              Định dạng JSON đã được validator kiểm.
        /// Created date: 22/10/2026 — cập nhật 28/10/2026 (sửa quyền của bố cục khác; chọn đủ mọi ô = toàn quyền)
        /// </summary>
        private async Task NormalizeAreaAsync(VwAddWallPermissionInput command, string? wallTopologyId = null)
        {
            if (command.IsFullAccess != true)
            {
                var layout = string.IsNullOrWhiteSpace(wallTopologyId) ? await Layout.GetCurrentAsync() : await Layout.GetByIdAsync(wallTopologyId);
                var cells = JsonConvert.DeserializeObject<List<VwGridCell>>(command.Config!) ?? [];
                if (cells.Any(c => !VwWallLayoutService.IsCellInside(layout, c.Block, c.Row, c.Col)))
                    throw Oops.Oh(BaseMsg.Vw.Validation.WallPermissionConfigInvalid);
                var totalCells = layout.Blocks.Sum(b => (b.Rows ?? 0) * (b.Cols ?? 0));
                command.IsFullAccess = totalCells > 0 && cells.Select(c => (c.Block, c.Row, c.Col)).Distinct().Count() == totalCells;
            }

            if (command.IsFullAccess == true)
                command.Config = null;
        }
    }
}
