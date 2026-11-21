using Modules.TMS.Controllers.WorkDetail.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.WorkDetail.Queries
{
    public class WorkDetailQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsWorkDetail> _tmsWorkDetailRep;
        private BaseRepository<TmsTrafficPlanWorkDetails> _trafficPlanWorkDetailRep;
        private BaseRepository<TmsTrafficPlans> _trafficPlanRep;
        private BaseRepository<TmsWorkExecute> _workExecuteRep;
        private BaseRepository<TmsWorkExecuteHistory> _workExecuteHistoryRep;
        private BaseCacheService _cache;
        private const string _cacheName = "tmsWorkDetail";

        public WorkDetailQueryHandler(
            BaseRepository<TmsWorkDetail> tmsWorkDetailRep,
            BaseRepository<TmsTrafficPlanWorkDetails> trafficPlanWorkDetailRep,
            BaseRepository<TmsTrafficPlans> trafficPlanRep,
            BaseRepository<TmsWorkExecute> workExecuteRep,
            BaseRepository<TmsWorkExecuteHistory> workExecuteHistoryRep,
            BaseCacheService cache)
        {
            _tmsWorkDetailRep = tmsWorkDetailRep;
            _trafficPlanWorkDetailRep = trafficPlanWorkDetailRep;
            _trafficPlanRep = trafficPlanRep;
            _workExecuteRep = workExecuteRep;
            _workExecuteHistoryRep = workExecuteHistoryRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageWorkDetailOutput>> HandleAsync(PageWorkDetailInput command)
        {
            var pagedList = await _tmsWorkDetailRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.WorkType), u => u.WorkType.Contains(command.WorkType))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ControlType), u => u.ControlType == command.ControlType)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((u) => new PageWorkDetailOutput()
                {
                }, true).OrderBuilder(command, "", "Code")
                .ToPagedListAsync(command.Page, command.PageSize);

            if (pagedList?.Records != null && pagedList.Records.Any())
            {
                var records = pagedList.Records.ToList();
                var ids = records.Where(u => !string.IsNullOrEmpty(u.ID)).Select(u => u.ID!).ToList();

                var inTrafficPlanIds = await _trafficPlanWorkDetailRep.AsQueryable()
                    .InnerJoin<TmsTrafficPlans>((d, p) => d.TrafficPlanId == p.ID)
                    .Where((d, p) => d.WorkDetailId != null && ids.Contains(d.WorkDetailId))
                    .Select((d, p) => d.WorkDetailId)
                    .Distinct()
                    .ToListAsync();

                var inWorkExecuteIds = await _workExecuteRep.AsQueryable()
                    .Where(u => u.WorkDetailId != null && ids.Contains(u.WorkDetailId))
                    .Select(u => u.WorkDetailId!)
                    .Distinct()
                    .ToListAsync();

                var inWorkExecuteHistoryIds = await _workExecuteHistoryRep.AsQueryable()
                    .Where(u => u.WorkDetailId != null && ids.Contains(u.WorkDetailId))
                    .Select(u => u.WorkDetailId!)
                    .Distinct()
                    .ToListAsync();

                var inUseSet = new HashSet<string>(inTrafficPlanIds.Where(x => !string.IsNullOrEmpty(x))!);
                inUseSet.UnionWith(inWorkExecuteIds.Where(x => !string.IsNullOrEmpty(x))!);
                inUseSet.UnionWith(inWorkExecuteHistoryIds.Where(x => !string.IsNullOrEmpty(x))!);

                foreach (var item in records)
                {
                    item.IsInUse = item.ID != null && inUseSet.Contains(item.ID);
                }
            }

            return pagedList;
        }
        public async Task<List<WorkDetailOutput>> HandleAsync(WorkDetailInput command)
        {

            //var list =  _cache.GetOrAdd($"sysTmsWorkDetail:{command.Name}",   _ =>
            //{
            //    return   _tmsWorkDetailRep.AsQueryable()
            //        .Select((u) => new TmsWorkDetailOutput()
            //        {
            //        }, true)
            //        .OrderBy(u => u.CreateTime).ToListAsync().Result;

            //});


            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
            //           .WhereIF(!string.IsNullOrWhiteSpace(command.WorkType), u => u.WorkType.Contains(command.WorkType))
            //           .WhereIF(!string.IsNullOrWhiteSpace(command.ControlType), u => u.ControlType.Contains(command.ControlType))
            //           .WhereIF(!string.IsNullOrEmpty(command.Name),u=>u.Name.Contains(command.Name)).ToList();
            //return list;
            var tvrList = await _tmsWorkDetailRep.AsQueryable()
              .WhereIF(!string.IsNullOrWhiteSpace(command.ID), u => u.ID == command.ID)
              .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
              .WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name))
              .WhereIF(!string.IsNullOrWhiteSpace(command.WorkType), u => u.WorkType == command.WorkType)
              .Where(u => u.Status.HasValue && u.Status.Value == BaseEnums.StatusEnum.Enable)
              .WhereIF(command.Status != null, u => u.Status == command.Status)
             .Select((st) => new WorkDetailOutput() { }, true)
             .OrderBy(st => st.Code)
             .ToListAsync();

            return tvrList;
        }
    }
}
