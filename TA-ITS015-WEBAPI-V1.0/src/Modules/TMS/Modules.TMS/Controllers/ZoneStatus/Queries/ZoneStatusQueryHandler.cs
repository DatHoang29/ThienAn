using Modules.TMS.Controllers.ZoneStatus.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.ZoneStatus.Queries
{
    public class ZoneStatusQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsZoneStatus> _zoneStatusRep;
        private BaseCacheService _cache;

        public ZoneStatusQueryHandler(BaseRepository<TmsZoneStatus> zoneStatusRep, BaseCacheService cache)
        {
            _zoneStatusRep = zoneStatusRep;
            _cache = cache;
        }

        public async Task<List<ZoneStatusOutput>> HandleAsync(ZoneStatusInput command)
        {

            var zList = await this._zoneStatusRep.AsQueryable()
                .Select((z) => new ZoneStatusOutput() { }, true)
                .ToListAsync();

            return zList;

        }


    }
}
