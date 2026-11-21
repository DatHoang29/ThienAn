using Modules.TMS.Controllers.EventType.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.EventType.Queries
{
    public class EventTypeQueryHandler:IWolverineHandler
    {
        private BaseRepository<TmsEventType> _eventTypeRep;
        private BaseCacheService _cache;

        public EventTypeQueryHandler(BaseRepository<TmsEventType> eventTypeRep, BaseCacheService cache)
        {
            _eventTypeRep = eventTypeRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageEventTypeOutput>> HandleAsync(PageEventTypeInput command)
        {
            return await _eventTypeRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ModuleId), u => u.ModuleId.Contains(command.ModuleId))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Type), u => u.Type.ToString().Contains(command.Type))
                .WhereIF(!string.IsNullOrWhiteSpace(command.WarningType), u => u.WarningType.ToString().Contains(command.WarningType))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((u) => new PageEventTypeOutput()
                {
                }, true).OrderBuilder(command, "", "Priority", false)
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<EventTypeOutput>> HandleAsync(EventTypeInput command)
        {

            var fullList = _cache.GetOrAdd($"{CacheConst.Tms.TmsEventType}:{command.Name}", _ =>
            {
                return _eventTypeRep.AsQueryable()
                    .Select((u) => new EventTypeOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;

            });

            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.ModuleId), u => u.ModuleId.Contains(command.ModuleId))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.type), u => u.Type.ToString().Contains(command.type.ToString()))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.warningType), u => u.WarningType.ToString().Contains(command.warningType.ToString()))
            //    .WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name)).ToList();
            //return list;
            var list = fullList.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(command.Type), u => u.Type.ToString().Contains(command.Type.ToString()))
            .WhereIF(!string.IsNullOrWhiteSpace(command.WarningType), u => u.WarningType.ToString().Contains(command.WarningType.ToString()))
            .WhereIF(!string.IsNullOrEmpty(command.ModuleId), u => u.ModuleId == command.ModuleId)
            .WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name))
            .WhereIF(command.Status != null, u => u.Status == command.Status)
            .ToList();

            return list;
        }
    }
}
