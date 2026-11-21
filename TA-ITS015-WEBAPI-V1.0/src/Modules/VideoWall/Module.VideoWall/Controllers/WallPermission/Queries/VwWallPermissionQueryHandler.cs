using Shared.DTO.Enums;
using Mapster;
using Module.VideoWall.Core.Dto.WallPermission;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Access;
using Newtonsoft.Json;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.WallPermission.Queries
{
    /// <summary>
    /// Description: Xử lý truy vấn dữ liệu VwWallPermission
    /// Created date: 11/09/2026
    /// </summary>
    public class VwWallPermissionQueryHandler(
        BaseRepository<VwWallPermission> PermissionRep,
        BaseRepository<VwWallPermissionMember> MemberRep,
        VwPermissionService Permission) : IWolverineHandler
    {
        /// <summary>
        /// Description: Lấy danh sách VwWallPermission theo phân trang
        /// Created date: 11/09/2026
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageWallPermissionOutput>> HandleAsync(VwPageWallPermissionInput command)
        {
            var keyword = command.Keyword?.Trim();

            var pagedList = await PermissionRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(keyword), u => u.Code!.Contains(keyword!) || u.Name!.Contains(keyword!))
                // ID chọn từ danh mục ⇒ so khớp chính xác, không dùng LIKE
                .WhereIF(!string.IsNullOrWhiteSpace(command.UserId), u => SqlFunc.Subqueryable<VwWallPermissionMember>()
                    .Where(m => m.IsDelete == null && m.PermissionId == u.ID && m.UserId == command.UserId).Any())
                .WhereIF(!string.IsNullOrWhiteSpace(command.OrgId), u => SqlFunc.Subqueryable<VwWallPermissionMember>()
                    .Where(m => m.IsDelete == null && m.PermissionId == u.ID && (m.UserId == null || m.UserId == string.Empty) && m.OrgId == command.OrgId).Any())
                .WhereIF(!string.IsNullOrWhiteSpace(command.WallTopologyId), u => u.WallTopologyId == command.WallTopologyId)
                // Dữ liệu cũ chưa có trạng thái (null) coi như Dùng
                .WhereIF(command.Status == BaseEnums.StatusEnum.Enable, u => u.Status == null || u.Status == BaseEnums.StatusEnum.Enable)
                .WhereIF(command.Status.HasValue && command.Status != BaseEnums.StatusEnum.Enable, u => u.Status == command.Status)
                .Select(u => new VwPageWallPermissionOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            await FillMembersAsync(pagedList.Records);
            return pagedList;
        }

        /// <summary>
        /// Description: Lấy toàn bộ danh sách VwWallPermission theo tiêu chí lọc (không phân trang)
        /// Created date: 11/09/2026
        /// </summary>
        public async Task<List<VwWallPermissionOutput>> HandleAsync(VwWallPermissionInput command)
        {
            var entities = await PermissionRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.UserId), u => SqlFunc.Subqueryable<VwWallPermissionMember>()
                    .Where(m => m.IsDelete == null && m.PermissionId == u.ID && m.UserId == command.UserId).Any())
                .WhereIF(!string.IsNullOrWhiteSpace(command.OrgId), u => SqlFunc.Subqueryable<VwWallPermissionMember>()
                    .Where(m => m.IsDelete == null && m.PermissionId == u.ID && (m.UserId == null || m.UserId == string.Empty) && m.OrgId == command.OrgId).Any())
                .WhereIF(!string.IsNullOrWhiteSpace(command.WallTopologyId), u => u.WallTopologyId == command.WallTopologyId)
                .Select((u) => new VwWallPermissionOutput() { }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync();

            await FillMembersAsync(entities);
            return entities;
        }

        /// <summary>
        /// Description: Lấy chi tiết 1 VwWallPermission theo ID
        /// Created date: 11/09/2026
        /// </summary>
        public async Task<VwWallPermissionOutput> HandleAsync(VwIdWallPermissionInput command)
        {
            var entity = await PermissionRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var output = entity.Adapt<VwWallPermissionOutput>();
            await FillMembersAsync([output]);
            return output;
        }

        /// <summary>
        /// Description: Gắn danh sách người dùng / đơn vị được gán cho từng quyền (1 truy vấn cho cả trang).
        /// Created date: 22/10/2026
        /// </summary>
        private async Task FillMembersAsync<T>(IEnumerable<T>? permissions) where T : VwWallPermissionOutput
        {
            var list = permissions?.ToList() ?? [];
            if (list.Count == 0)
                return;

            var ids = list.Select(u => u.ID).ToList();
            var members = await MemberRep.AsQueryable()
                .Where(u => u.IsDelete == null && ids.Contains(u.PermissionId))
                .OrderBy(u => u.CreateTime)
                .ToListAsync();

            var byPermission = members.ToLookup(u => u.PermissionId);
            foreach (var item in list)
            {
                var rows = byPermission[item.ID].ToList();
                item.UserIds = rows.Where(u => !string.IsNullOrWhiteSpace(u.UserId)).Select(u => u.UserId!).Distinct().ToList();
                item.OrgIds = rows.Where(u => string.IsNullOrWhiteSpace(u.UserId) && !string.IsNullOrWhiteSpace(u.OrgId)).Select(u => u.OrgId!).Distinct().ToList();
            }
        }

        /// <summary>
        /// Description: Lấy cấu hình vùng lưới hiệu lực của người dùng đang đăng nhập (phục vụ FE vẽ ô lưới)
        /// Created date: 13/09/2026
        /// </summary>
        public async Task<VwMyWallPermissionOutput> HandleAsync(VwGetMyWallPermissionInput command)
        {
            // Dùng chung logic với kiểm tra quyền để FE và BE luôn cho cùng kết quả
            return await Permission.GetMyAccessAsync();
        }
    }
}
