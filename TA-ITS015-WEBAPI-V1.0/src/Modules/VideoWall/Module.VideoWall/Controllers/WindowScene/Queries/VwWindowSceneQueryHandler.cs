using Mapster;
using Module.VideoWall.Core.Dto.WindowScene;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure.Services.Access;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.WindowScene.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu VwWindowScene
    /// Created date: 2026-08-01
    /// </summary>
    public class VwWindowSceneQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwWindowScene> _vwWindowSceneRep;
        private readonly BaseCacheService _cache;

        public VwWindowSceneQueryHandler(BaseRepository<VwWindowScene> vwWindowSceneRep, BaseCacheService cache)
        {
            _vwWindowSceneRep = vwWindowSceneRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách VwWindowScene theo phân trang
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageWindowSceneOutput>> HandleAsync(VwPageWindowSceneInput command)
        {
            var pagedList = await _vwWindowSceneRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name != null && u.Name.Contains(command.Name!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.SceneId), u => u.SceneId != null && u.SceneId.Contains(command.SceneId!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.SourceId), u => u.SourceId != null && u.SourceId.Contains(command.SourceId!))
                .WhereIF(command.Visible != null, u => u.Visible == command.Visible)
                .Select(u => new VwPageWindowSceneOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách VwWindowScene theo tiêu chí lọc (không phân trang)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<List<VwWindowSceneOutput>> HandleAsync(VwWindowSceneInput command)
        {
            // 1. Lấy dữ liệu thô từ Cache hoặc DB.
            var entities = _cache.GetOrAdd(CacheConst.Vw.VwWindowScene, _ =>
            {
                return _vwWindowSceneRep.AsQueryable()
                .Select((u) => new VwWindowSceneOutput()
                {
                }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync().Result;
            });

            // 2. Lọc theo tiêu chí tìm kiếm
            var filtered = entities.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => !string.IsNullOrWhiteSpace(u.Name) && u.Name!.Contains(command.Name!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.SceneId), u => !string.IsNullOrWhiteSpace(u.SceneId) && u.SceneId!.Contains(command.SceneId!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.SourceId), u => !string.IsNullOrWhiteSpace(u.SourceId) && u.SourceId!.Contains(command.SourceId!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Visible != null, u => u.Visible == command.Visible)
                .OrderBy(st => st.CreateTime)
                .ToList();

            return filtered;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 VwWindowScene theo ID
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<VwWindowSceneOutput> HandleAsync(VwIdWindowSceneInput command)
        {
            var entity = await _vwWindowSceneRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            // Đọc mở: xem được cửa sổ của mọi vùng.
            return entity.Adapt<VwWindowSceneOutput>();
        }
    }
}
