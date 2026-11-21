using Mapster;
using Module.VideoWall.Core.Dto.Scene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Core.Exceptions;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.Scene.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu VwScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwSceneQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwScene> _vwSceneRep;
        private readonly BaseRepository<VwController> _vwControllerRep;
        private readonly BaseCacheService _cache;

        public VwSceneQueryHandler(BaseRepository<VwScene> vwSceneRep, BaseRepository<VwController> vwControllerRep, BaseCacheService cache)
        {
            _vwSceneRep = vwSceneRep;
            _vwControllerRep = vwControllerRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách VwScene theo phân trang
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageSceneOutput>> HandleAsync(VwPageSceneInput command)
        {
            var pagedList = await _vwSceneRep.AsQueryable()
                // Ô tìm "Mã / Tên": khớp mã HOẶC tên
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => (u.Name != null && u.Name.Contains(command.Name!)) || (u.Code != null && u.Code.Contains(command.Name!)))
                .WhereIF(!string.IsNullOrWhiteSpace(command.OutputId), u => u.OutputId != null && u.OutputId.Contains(command.OutputId!))
                .WhereIF(command.IsDefault != null, u => u.IsDefault == command.IsDefault)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .WhereIF(!string.IsNullOrWhiteSpace(command.WallTopologyId), u => u.WallTopologyId == command.WallTopologyId)
                .Select(u => new VwPageSceneOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách VwScene theo tiêu chí lọc (không phân trang)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<List<VwSceneOutput>> HandleAsync(VwSceneInput command)
        {
            // 1. Lấy dữ liệu thô từ Cache hoặc DB.
            var entities = _cache.GetOrAdd(CacheConst.Vw.VwScene, _ =>
            {
                return _vwSceneRep.AsQueryable()
                .Select((u) => new VwSceneOutput()
                {
                }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync().Result;
            });

            // 2. Lọc theo tiêu chí tìm kiếm
            var filtered = entities.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                // Khớp mã HOẶC tên (giống API phân trang)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => (!string.IsNullOrWhiteSpace(u.Name) && u.Name!.Contains(command.Name!, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(u.Code) && u.Code!.Contains(command.Name!, StringComparison.OrdinalIgnoreCase)))
                .WhereIF(!string.IsNullOrWhiteSpace(command.OutputId), u => !string.IsNullOrWhiteSpace(u.OutputId) && u.OutputId!.Contains(command.OutputId!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.IsDefault != null, u => u.IsDefault == command.IsDefault)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .WhereIF(!string.IsNullOrWhiteSpace(command.WallTopologyId), u => u.WallTopologyId == command.WallTopologyId)
                .OrderBy(st => st.CreateTime)
                .ToList();

            return filtered;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 VwScene theo ID
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<VwSceneOutput> HandleAsync(VwIdSceneInput command)
        {
            var entity = await _vwSceneRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Đọc mở: mọi tài khoản xem được chi tiết kịch bản của mọi vùng.
            return entity.Adapt<VwSceneOutput>();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy kịch bản đang được kích hoạt.
        ///              Dùng cho FE khi tải lại trang hoặc client mới kết nối để dựng lại
        ///              giao diện đúng trạng thái hiện tại (không được gọi API Active vì đó là lệnh ghi).
        /// Created date: 2026-08-03
        /// </summary>
        public async Task<VwGetActiveSceneOutput> HandleAsync(VwGetActiveSceneInput command)
        {
            // Ưu tiên vùng được kích hoạt gần nhất: khi các vùng chạy kịch bản khác nhau, endpoint
            // này buộc phải chọn một, và "mới nhất" là lựa chọn ít gây bất ngờ nhất.
            var latest = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null && u.ActiveSceneId != null && u.ActiveSceneId != "")
                .OrderBy(u => u.ActiveSceneAt, OrderByType.Desc)
                .FirstAsync();

            var scene = latest == null
                ? null
                : await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null && u.ID == latest.ActiveSceneId);

            // Dự phòng cho dữ liệu cũ: chưa có vùng nào được ghi thì đọc theo cờ như trước.
            scene ??= await _vwSceneRep.GetFirstAsync(u => u.IsDelete == null
                                                        && u.ActiveScene == BaseEnums.ActiveScene.Activate);

            if (scene == null)
                throw Oops.Oh(BaseMsg.Vw.Exception.NoActiveScene);

            return scene.Adapt<VwGetActiveSceneOutput>();
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy kịch bản đang chiếu của TỪNG vùng màn hình (mỗi bộ điều khiển một dòng).
        ///              Dùng thay cho <see cref="VwGetActiveSceneInput"/> khi các vùng chạy kịch bản
        ///              khác nhau — đó mới là trạng thái thật của tường.
        /// Created date: 2026-08-13
        /// </summary>
        public async Task<List<VwActiveRegionOutput>> HandleAsync(VwGetActiveRegionInput command)
        {
            var controllers = await _vwControllerRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ControllerId), u => u.ID == command.ControllerId)
                .OrderBy(u => u.Code)
                .ToListAsync();

            var sceneIds = controllers
                .Select(u => u.ActiveSceneId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var scenes = sceneIds.Count == 0
                ? new List<VwScene>()
                : await _vwSceneRep.AsQueryable().Where(u => sceneIds.Contains(u.ID)).ToListAsync();

            return controllers.Select(ctrl =>
            {
                var scene = scenes.FirstOrDefault(s => s.ID == ctrl.ActiveSceneId);
                return new VwActiveRegionOutput
                {
                    ControllerId = ctrl.ID,
                    ControllerCode = ctrl.Code,
                    ControllerName = ctrl.Name,
                    SceneId = scene?.ID,
                    SceneCode = scene?.Code,
                    SceneName = scene?.Name,
                    ActivatedAt = ctrl.ActiveSceneAt,
                };
            }).ToList();
        }
    }
}
