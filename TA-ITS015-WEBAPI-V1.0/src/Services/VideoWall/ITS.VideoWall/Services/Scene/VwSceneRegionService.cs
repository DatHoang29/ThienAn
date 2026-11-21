using Module.VideoWall.Core.Constants;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Helper;
using Module.VideoWall.Core.Interfaces;
using ITS.VideoWall.Infrastructure;
using Shared.DTO.Enums;

namespace ITS.VideoWall.Services.Scene
{
    /// <summary>
    /// Description: Cài đặt IVwSceneRegionService dành riêng cho Worker VideoWall: dựng dữ liệu quy đổi
    ///              cửa sổ → master từ bố cục đang áp dụng và cổng ra (VwControllerOutput) đã đồng bộ.
    /// Created date: 11/09/2026 — cập nhật 06/11/2026 (quy đổi theo cổng ra)
    /// </summary>
    public class VwSceneRegionService : IVwSceneRegionService
    {
        private readonly BaseRepository<VwWallTopology> _vwTopologyRep;
        private readonly BaseRepository<VwWallBlock> _vwBlockRep;
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseRepository<VwControllerOutput> _vwOutputRep;

        public VwSceneRegionService(
            BaseRepository<VwWallTopology> vwTopologyRep,
            BaseRepository<VwWallBlock> vwBlockRep,
            BaseRepository<VwController> vwControllerRep,
            BaseRepository<VwControllerOutput> vwOutputRep)
        {
            _vwTopologyRep = vwTopologyRep;
            _vwBlockRep = vwBlockRep;
            _vwControllerRep = vwControllerRep;
            _vwOutputRep = vwOutputRep;
        }

        public async Task<VwMasterProjection> GetMasterProjectionAsync()
        {
            // 1. Bố cục đang áp dụng + khối
            var topology = await _vwTopologyRep.GetFirstAsync(u => u.IsDelete == null && u.IsApply == BaseEnums.YesNoEnum.Y);
            var blocks = topology == null
                ? []
                : await _vwBlockRep.AsQueryable()
                    .Where(u => u.IsDelete == null && u.WallTopologyId == topology.ID)
                    .ToListAsync();

            // 2. Bộ phân phối (master) nhận lệnh dựng kịch bản
            var masterIds = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.Role == VwControllerRole.Distribution)
                .Select(u => u.ID)
                .ToListAsync();

            // 3. Canvas master từ cổng ra đã đồng bộ của bộ phân phối
            var masterOutputs = masterIds.Count == 0 ? [] : await _vwOutputRep.AsQueryable()
                .Where(u => u.IsDelete == null && masterIds.Contains(u.ControllerId))
                .ToListAsync();

            return VwWallPlacement.BuildProjection(blocks, masterOutputs);
        }
    }
}
