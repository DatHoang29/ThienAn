using Modules.TMS.Controllers.TrafficPlanWorkDetail.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.TrafficPlanWorkDetail.Queries
{
    public class TrafficPlanWorkDetailQueryHandler
    {
        private BaseRepository<TmsTrafficPlanWorkDetails> _TmsTrafficPlanWorkDetailRep;
        private BaseCacheService _cache;

        public TrafficPlanWorkDetailQueryHandler(BaseRepository<TmsTrafficPlanWorkDetails> TmsTrafficPlanWorkDetailRep, BaseCacheService cache)
        {
            _TmsTrafficPlanWorkDetailRep = TmsTrafficPlanWorkDetailRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageTmsTrafficPlanWorkDetailOutput>> HandleAsync(PageTmsTrafficPlanWorkDetailInput command)
        {
            return await _TmsTrafficPlanWorkDetailRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.TrafficPlanId), u => u.TrafficPlanId.Contains(command.TrafficPlanId))
                .WhereIF(!string.IsNullOrWhiteSpace(command.WorkDetailId), u => u.WorkDetailId.Contains(command.WorkDetailId))
                .OrderBy(u => u.Step)
                .Select((u) => new PageTmsTrafficPlanWorkDetailOutput()
                {
                }, true).OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<TrafficPlanWorkDetailOutput>> HandleAsync(TrafficPlanWorkDetailInput command)
        {

            //var list =  _cache.GetOrAdd($"sysTmsTrafficPlanWorkDetail:{command.TrafficPlanId}",   _ =>
            //{
            //    return   _TmsTrafficPlanWorkDetailRep.AsQueryable()
            //        .Select((u) => new TmsTrafficPlanWorkDetailOutput()
            //        {
            //        }, true)
            //        .OrderBy(u => u.Step).ToListAsync().Result;

            //});


            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.TrafficPlanId), u => u.TrafficPlanId.Contains(command.TrafficPlanId))
            //           .WhereIF(!string.IsNullOrEmpty(command.WorkDetailId),u=>u.WorkDetailId.Contains(command.WorkDetailId)).ToList();
            //return list;
            var tvrList = await _TmsTrafficPlanWorkDetailRep.AsQueryable()
              .WhereIF(!string.IsNullOrWhiteSpace(command.TrafficPlanId), u => u.TrafficPlanId.Contains(command.TrafficPlanId))
              .WhereIF(!string.IsNullOrEmpty(command.WorkDetailId), u => u.WorkDetailId.Contains(command.WorkDetailId))
             .Select((st) => new TrafficPlanWorkDetailOutput() { }, true)
             .OrderBy(st => st.CreateTime)
             .ToListAsync();

            return tvrList;
        }
    }
}
