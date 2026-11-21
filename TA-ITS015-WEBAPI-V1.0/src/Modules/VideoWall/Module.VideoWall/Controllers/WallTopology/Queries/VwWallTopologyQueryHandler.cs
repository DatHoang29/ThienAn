using Mapster;
using Module.VideoWall.Core.Dto.WallTopology;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Module.VideoWall.Infrastructure.Services.Layout;
using Shared.Infrastructure.Extensions;
using Wolverine;

namespace Module.VideoWall.Controllers.WallTopology.Queries
{
    /// <summary>
    /// Description: Xử lý truy vấn cấu hình tường VwWallTopology
    /// Created date: 09/09/2026
    /// </summary>
    public class VwWallTopologyQueryHandler : IWolverineHandler
    {
        private readonly VwWallLayoutService _layout;
        private readonly BaseRepository<VwWallTopology> _topologyRep;
        private readonly BaseRepository<VwScene> _sceneRep;
        private readonly BaseRepository<VwWallPermission> _permissionRep;

        public VwWallTopologyQueryHandler(
            VwWallLayoutService layout,
            BaseRepository<VwWallTopology> topologyRep,
            BaseRepository<VwScene> sceneRep,
            BaseRepository<VwWallPermission> permissionRep)
        {
            _layout = layout;
            _topologyRep = topologyRep;
            _sceneRep = sceneRep;
            _permissionRep = permissionRep;
        }

        /// <summary>
        /// Description: Lấy cấu hình tường đang dùng kèm danh sách khối.
        ///
        ///              KHÔNG có bản ghi thì trả về giá trị MẶC ĐỊNH (1 khối 8 × 4) và KHÔNG ghi vào CSDL.
        ///              Vì sao không tự ghi: một lần GET không nên tạo dữ liệu — client nào mở
        ///              trang trước sẽ vô tình chốt cấu hình tường cho cả hệ thống. Ghi là việc
        ///              của SaveLayout, do người dùng chủ động bấm lưu.
        /// Created date: 09/09/2026 — cập nhật 22/10/2026 (đọc qua VwWallLayoutService)
        /// </summary>
        public async Task<VwWallTopologyOutput> HandleAsync(VwCurrentWallTopologyInput command)
        {
            // Bản sao: đối tượng trong cache dùng chung, không gắn số kịch bản vào đó
            var output = (await _layout.GetCurrentAsync()).Adapt<VwWallTopologyOutput>();
            if (!output.IsDefault)
            {
                output.SceneCount = await CountScenesAsync(output.ID);
                output.PermissionCount = await CountPermissionsAsync(output.ID);
            }
            return output;
        }

        /// <summary>
        /// Description: Danh sách bố cục (đang dùng trước, rồi theo mã) kèm khối, số kịch bản và số phân quyền.
        /// Created date: 22/10/2026 — cập nhật 26/10/2026 (lọc mã / tên)
        /// </summary>
        public async Task<List<VwWallTopologyOutput>> HandleAsync(VwListWallTopologyInput command)
        {
            var keyword = command.Keyword?.Trim();
            var ids = await _topologyRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(keyword), u => u.Code!.Contains(keyword!) || u.Name!.Contains(keyword!))
                .OrderBy(u => u.IsApply, OrderByType.Desc)
                .OrderBy(u => u.Code)
                .Select(u => u.ID)
                .ToListAsync();

            var counts = await _sceneRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId != null)
                .GroupBy(u => u.WallTopologyId)
                .Select(u => new { u.WallTopologyId, Count = SqlFunc.AggregateCount(u.ID) })
                .ToListAsync();

            var permissionCounts = await _permissionRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.WallTopologyId != null)
                .GroupBy(u => u.WallTopologyId)
                .Select(u => new { u.WallTopologyId, Count = SqlFunc.AggregateCount(u.ID) })
                .ToListAsync();

            var result = new List<VwWallTopologyOutput>();
            foreach (var id in ids)
            {
                var output = (await _layout.GetByIdAsync(id)).Adapt<VwWallTopologyOutput>();
                output.SceneCount = counts.FirstOrDefault(u => u.WallTopologyId == id)?.Count ?? 0;
                output.PermissionCount = permissionCounts.FirstOrDefault(u => u.WallTopologyId == id)?.Count ?? 0;
                result.Add(output);
            }
            return result;
        }

        /// <summary>
        /// Description: Lấy 1 bố cục theo ID kèm khối và số kịch bản — vẽ kịch bản thuộc bố cục khác bố cục đang dùng.
        /// Created date: 22/10/2026
        /// </summary>
        public async Task<VwWallTopologyOutput> HandleAsync(VwIdWallTopologyInput command)
        {
            var output = (await _layout.GetByIdAsync(command.ID)).Adapt<VwWallTopologyOutput>();
            output.SceneCount = await CountScenesAsync(output.ID);
            output.PermissionCount = await CountPermissionsAsync(output.ID);
            return output;
        }

        private Task<int> CountScenesAsync(string topologyId)
            => _sceneRep.AsQueryable().Where(u => u.IsDelete == null && u.WallTopologyId == topologyId).CountAsync();

        private Task<int> CountPermissionsAsync(string topologyId)
            => _permissionRep.AsQueryable().Where(u => u.IsDelete == null && u.WallTopologyId == topologyId).CountAsync();
    }
}
