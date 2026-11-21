using Module.VideoWall.Core.Dto.EventTriggerLog;
using Module.VideoWall.Core.Entities;
using Module.VideoWall.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Wolverine;

namespace Module.VideoWall.Controllers.EventTriggerLog.Queries
{
    /// <summary>
    /// Author: Đạt
    /// Description: Truy vấn nhật ký kích hoạt kịch bản và thao tác thiết bị Video Wall.
    /// Created date: 26/08/2026
    /// </summary>
    public class VwEventTriggerLogQueryHandler(
        BaseRepository<VwEventTriggerLog> vwEventTriggerLogRep) : IWolverineHandler
    {
        private readonly BaseRepository<VwEventTriggerLog> _vwEventTriggerLogRep = vwEventTriggerLogRep;

        /// <summary>
        /// Author: Đạt
        /// Description: Lấy nhật ký theo phân trang, mặc định mới nhất lên đầu.
        /// Created date: 26/08/2026
        /// </summary>
        public async Task<SqlSugarPagedList<VwPageEventTriggerLogOutput>> HandleAsync(VwPageEventTriggerLogInput command)
        {
            return await _vwEventTriggerLogRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.RuleId), u => u.RuleId == command.RuleId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), u => u.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ControllerId), u => u.ControllerId == command.ControllerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SceneId), u => u.TargetSceneId == command.SceneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SceneCode), u => u.SceneCode == command.SceneCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Action), u => u.Action == command.Action)
                .WhereIF(command.Success.HasValue, u => u.Success == command.Success)
                .WhereIF(command.Skipped.HasValue, u => u.Skipped == command.Skipped)
                .WhereIF(command.TriggeredAtFrom.HasValue, u => u.TriggeredAt >= command.TriggeredAtFrom)
                .WhereIF(command.TriggeredAtTo.HasValue, u => u.TriggeredAt <= command.TriggeredAtTo)
                .WhereIF(!string.IsNullOrWhiteSpace(command.OrgId), u => u.OrgId == command.OrgId)
                .Select(u => new VwPageEventTriggerLogOutput() { }, true)
                .OrderBuilder(command, "")
                .OrderBy(u => u.TriggeredAt, OrderByType.Desc)
                .ToPagedListAsync(command.Page, command.PageSize);
        }

        /// <summary>
        /// Author: Đạt
        /// Description: Lấy nhật ký không phân trang, có trần số bản ghi.
        /// Created date: 26/08/2026
        /// </summary>
        public async Task<List<VwEventTriggerLogOutput>> HandleAsync(VwEventTriggerLogInput command)
        {
            var take = command.Take is <= 0 ? 200 : Math.Min(command.Take, 1000);

            return await _vwEventTriggerLogRep.AsQueryable()
                .Where(u => u.IsDelete == null)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
                .WhereIF(!string.IsNullOrWhiteSpace(command.RuleId), u => u.RuleId == command.RuleId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), u => u.EventTypeId == command.EventTypeId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.ControllerId), u => u.ControllerId == command.ControllerId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SceneId), u => u.TargetSceneId == command.SceneId)
                .WhereIF(!string.IsNullOrWhiteSpace(command.SceneCode), u => u.SceneCode == command.SceneCode)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Action), u => u.Action == command.Action)
                .WhereIF(command.Success.HasValue, u => u.Success == command.Success)
                .WhereIF(command.Skipped.HasValue, u => u.Skipped == command.Skipped)
                .WhereIF(command.TriggeredAtFrom.HasValue, u => u.TriggeredAt >= command.TriggeredAtFrom)
                .WhereIF(command.TriggeredAtTo.HasValue, u => u.TriggeredAt <= command.TriggeredAtTo)
                .WhereIF(!string.IsNullOrWhiteSpace(command.OrgId), u => u.OrgId == command.OrgId)
                .Select(u => new VwEventTriggerLogOutput() { }, true)
                .OrderBy(u => u.TriggeredAt, OrderByType.Desc)
                .Take(take)
                .ToListAsync();
        }
    }
}
