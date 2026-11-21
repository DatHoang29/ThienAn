using Modules.TMS.Controllers.MapLocations.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MapLocations.Queries
{
    public class TmsMapLocationQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsMapLocation> _TmsMapLocationRep;
        private BaseCacheService _cache;

        public TmsMapLocationQueryHandler(BaseRepository<TmsMapLocation> TmsMapLocationRep, BaseCacheService cache)
        {
            _TmsMapLocationRep = TmsMapLocationRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageTmsMapLocationsOutput>> HandleAsync(PageTmsMapLocationsInput command)
        {
            return await _TmsMapLocationRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.MapId), u => u.MapId.Contains(command.MapId))
                .Select((u) => new PageTmsMapLocationsOutput()
                {
                }, true).OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<MapLocationsOutput>> HandleAsync(MapLocationsInput command)
        {

            //var list =  _cache.GetOrAdd($"sysTmsMapLocation:{command.Name}",   _ =>
            //{
            //    return   _TmsMapLocationRep.AsQueryable()
            //        .Select((u) => new TmsMapLocationsOutput()
            //        {
            //        }, true)
            //        .OrderBy(u => u.CreateTime).ToListAsync().Result;

            //});

            //            var tvrList = await this._baseRepository.AsQueryable()

            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.MapId), u => u.MapId.Contains(command.MapId)).ToList();
            //return list;
            var tvrList = await _TmsMapLocationRep.AsQueryable()
               .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
               .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
               .WhereIF(!string.IsNullOrWhiteSpace(command.MapId), u => u.MapId.Contains(command.MapId))
               .Select((st) => new MapLocationsOutput() { }, true)
               .OrderBy(st => st.CreateTime)
               .ToListAsync();

            return tvrList;
        }

        /// <summary>
        /// Get tmsMapLocation base on MapId
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<MapLocationsOutput> HandleAsync(MapLocationBasedOnMapIdInput command)
        {
            var entity = await _TmsMapLocationRep.AsQueryable().Where(m => m.MapId == command.MapId)
                                    .Select(m => new MapLocationsOutput() { }, true)
                                    .FirstAsync();
            return entity;
        }

    }
}
