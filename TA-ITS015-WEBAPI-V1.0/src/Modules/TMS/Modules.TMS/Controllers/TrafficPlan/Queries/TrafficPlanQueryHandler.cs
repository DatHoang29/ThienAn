using Modules.TMS.Controllers.TrafficPlan.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.TrafficPlan.Queries
{
    public class TrafficPlanQueryHandler
    {
        private BaseRepository<TmsTrafficPlans> _TmsTrafficPlanRep;
        private BaseCacheService _cache;

        // Begin - Add by Ngọc Thắng
        private BaseRepository<TmsTrafficPlanWorkDetails> _tmsTrafficPlanWorkDetailRepo;
        // End - Add by Ngọc Thắng

        public TrafficPlanQueryHandler(BaseRepository<TmsTrafficPlans> TmsTrafficPlanRep, BaseRepository<TmsTrafficPlanWorkDetails> tmsTrafficPlanWorkDetailRepo, BaseCacheService cache)
        {
            _TmsTrafficPlanRep = TmsTrafficPlanRep;
            _cache = cache;

            // Begin - Add by Ngọc Thắng
            this._tmsTrafficPlanWorkDetailRepo = tmsTrafficPlanWorkDetailRepo;
            // End - Add by Ngọc Thắng
        }

        public async Task<SqlSugarPagedList<PageTmsTrafficPlanOutput>> HandleAsync(PageTmsTrafficPlanInput command)
        {
            return await _TmsTrafficPlanRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), u => u.EventTypeId.Contains(command.EventTypeId))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((u) => new PageTmsTrafficPlanOutput()
                {
                }, true).OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<TrafficPlanOutput>> HandleAsync(TrafficPlanInput command)
        {
            //var list = _cache.GetOrAdd($"sysTmsTrafficPlan:{command.Name}", _ =>
            //{
            //    return _TmsTrafficPlanRep.AsQueryable()
            //        .Select((u) => new TmsTrafficPlanOutput()
            //        {
            //        }, true)
            //        .OrderBy(u => u.CreateTime).ToListAsync().Result;

            //});


            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
            //           .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), u => u.EventTypeId.Contains(command.EventTypeId))
            //           .WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name)).ToList();
            //return list;

            var tvrList = await _TmsTrafficPlanRep.AsQueryable()
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.EventTypeId), u => u.EventTypeId.Contains(command.EventTypeId))
                    .WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name))
                    .WhereIF(command.Status != null, u => u.Status == command.Status)
              .Select((st) => new TrafficPlanOutput() { }, true)
              .OrderBy(st => st.CreateTime)
              .ToListAsync();

            return tvrList;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Lấy danh sách kịch bản hợp lệ
        /// </summary>
        public async Task<List<TrafficPlanOutput>> HandleAsync(TmsTrafficPlanValidInput command)
        {
            // TODO: Cache

            var tpList = await this._TmsTrafficPlanRep.AsQueryable()
                .LeftJoin<TmsEventType>((tp, et) => tp.EventTypeId == et.ID)
                .Where((tp, et) => tp.Status == BaseEnums.StatusEnum.Enable && tp.IsDelete == null)
                .OrderBy((tp, et) => et.Priority, OrderByType.Asc)
                .Select((tp, et) => new TrafficPlanOutput() { }, true)
                .ToListAsync();

            if ((tpList?.Count() ?? 0) == 0)
                return null;

            var trafficPlandIdList = tpList.Select(tp => tp.ID).ToList();

            var workDetailMappedList = await this._tmsTrafficPlanWorkDetailRepo.AsQueryable()
                .InnerJoin<TmsWorkDetail>((tpwd, wd) => tpwd.WorkDetailId == wd.ID)
                .Where((tpwd, wd) => wd.Status == BaseEnums.StatusEnum.Enable)
                .Where((tpwd, wd) => tpwd.IsDelete == null && wd.IsDelete == null)
                .Where((tpwd, wd) => trafficPlandIdList.Contains(tpwd.TrafficPlanId))
                .ToListAsync();

            if (workDetailMappedList?.Count() > 0)
            {
                tpList.ForEach(tp =>
                {
                    tp.HasWorkDetailMapped = workDetailMappedList.Any(wdm => wdm.TrafficPlanId == tp.ID);
                });
            }

            return tpList;
        }
    }
}
