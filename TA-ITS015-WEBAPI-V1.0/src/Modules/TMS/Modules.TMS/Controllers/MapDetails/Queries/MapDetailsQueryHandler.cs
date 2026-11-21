using Modules.TMS.Controllers.MapDetails.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.MapDetails.Queries
{
    public class PositionQueryHandler
    {
        private BaseRepository<TmsMapDetail> _TmsMapDetailRep;
        private BaseCacheService _cache;

        public PositionQueryHandler(BaseRepository<TmsMapDetail> TmsMapDetailRep, BaseCacheService cache)
        {
            _TmsMapDetailRep = TmsMapDetailRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageTmsMapDetailsOutput>> HandleAsync(PageTmsMapDetailsInput command)
        {
            return await _TmsMapDetailRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.MapLocationId), u => u.MapLocationId.Contains(command.MapLocationId))
                .Select((u) => new PageTmsMapDetailsOutput()
                {
                }, true).OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<MapDetailsOutput>> HandleAsync(MapDetailsInput command)
        {

            //var list =  _cache.GetOrAdd($"sysTmsMapDetail:{command.Name}",   _ =>
            //{
            //    return   _TmsMapDetailRep.AsQueryable()
            //        .Select((u) => new TmsMapDetailsOutput()
            //        {
            //        }, true)
            //        .OrderBy(u => u.CreateTime).ToListAsync().Result;

            //});

            //            var tvrList = await this._baseRepository.AsQueryable()

            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.MapId), u => u.MapId.Contains(command.MapId)).ToList();
            //return list;
            var tvrList = await _TmsMapDetailRep.AsQueryable()
               .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
               .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
               .WhereIF(!string.IsNullOrWhiteSpace(command.MapLocationId), u => u.MapLocationId.Contains(command.MapLocationId))
              .Select((st) => new MapDetailsOutput() { }, true)
              .OrderBy(st => st.CreateTime)
              .ToListAsync();

            return tvrList;
        }
    }
}
