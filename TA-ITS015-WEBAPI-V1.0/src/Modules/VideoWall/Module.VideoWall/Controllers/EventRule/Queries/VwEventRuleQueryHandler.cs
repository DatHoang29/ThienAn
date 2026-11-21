using Mapster;
using Module.VideoWall.Core.Dto.EventRule;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;
using Wolverine;

namespace Module.VideoWall.Controllers.EventRule.Queries
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý truy vấn dữ liệu VwEventRule
    /// Created date: 2026-08-01
    /// </summary>
    public class VwEventRuleQueryHandler : IWolverineHandler
    {
        private readonly BaseRepository<VwEventRule> _vwEventRuleRep;
        private readonly BaseCacheService _cache;

        public VwEventRuleQueryHandler(BaseRepository<VwEventRule> vwEventRuleRep, BaseCacheService cache)
        {
            _vwEventRuleRep = vwEventRuleRep;
            _cache = cache;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy danh sách VwEventRule theo phân trang
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageEventRuleOutput>> HandleAsync(VwPageEventRuleInput command)
        {
            var pagedList = await _vwEventRuleRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), u => u.EventTypeId != null && u.EventTypeId.Contains(command.EventTypeId!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetSceneId), u => u.TargetSceneId != null && u.TargetSceneId.Contains(command.TargetSceneId!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventSource), u => u.EventSource != null && u.EventSource.Contains(command.EventSource!))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Priority), u => u.Priority != null && u.Priority.Contains(command.Priority!))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select(u => new VwPageEventRuleOutput() { }, true)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);

            return pagedList;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy toàn bộ danh sách VwEventRule theo tiêu chí lọc (không phân trang)
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<List<VwEventRuleOutput>> HandleAsync(VwEventRuleInput command)
        {
            // 1. Lấy dữ liệu thô từ Cache hoặc DB.
            var entities = _cache.GetOrAdd(CacheConst.Vw.VwEventRule, _ =>
            {
                return _vwEventRuleRep.AsQueryable()
                .Select((u) => new VwEventRuleOutput()
                {
                }, true)
                .OrderBy(u => u.CreateTime)
                .ToListAsync().Result;
            });

            // 2. Lọc theo tiêu chí tìm kiếm
            var filtered = entities.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), u => !string.IsNullOrWhiteSpace(u.EventTypeId) && u.EventTypeId!.Contains(command.EventTypeId!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.TargetSceneId), u => !string.IsNullOrWhiteSpace(u.TargetSceneId) && u.TargetSceneId!.Contains(command.TargetSceneId!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventSource), u => !string.IsNullOrWhiteSpace(u.EventSource) && u.EventSource!.Contains(command.EventSource!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Priority), u => !string.IsNullOrWhiteSpace(u.Priority) && u.Priority!.Contains(command.Priority!, StringComparison.OrdinalIgnoreCase))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .OrderBy(st => st.CreateTime)
                .ToList();

            return filtered;
        }

        /// <summary>
        /// Author: HieuNV
        /// Description: Lấy chi tiết 1 VwEventRule theo ID
        /// Created date: 2026-08-01
        /// </summary>
        public async Task<VwEventRuleOutput> HandleAsync(VwIdEventRuleInput command)
        {
            var entity = await _vwEventRuleRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            return entity.Adapt<VwEventRuleOutput>();
        }
    }
}
