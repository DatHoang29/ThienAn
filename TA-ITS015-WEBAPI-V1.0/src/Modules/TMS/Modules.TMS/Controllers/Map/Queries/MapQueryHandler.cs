using Modules.TMS.Controllers.Map.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.Map.Queries
{
    public class MapQueryHandler
    {
        private BaseRepository<TmsMap> _tmsMapRep;
        private BaseCacheService _cache;

        public MapQueryHandler(BaseRepository<TmsMap> tmsMapRep, BaseCacheService cache)
        {
            _tmsMapRep = tmsMapRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageMapOutput>> HandleAsync(PageMapInput command)
        {
            return await _tmsMapRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.Mode), u => u.Mode.ToString().Contains(command.Mode))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Type), u => u.Type.ToString().Contains(command.Type))
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((u) => new PageMapOutput()
                {
                }, true)
                .OrderBuilder(command, "", "OrderNo", false)
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<MapOutput>> HandleAsync(MapInput command)
        {

            var list = _cache.GetOrAdd(CacheConst.Tms.TmsMap, _ =>
            {
                return _tmsMapRep.AsQueryable()
                    .Select((u) => new MapOutput()
                    {
                    }, true)
                    .OrderBy(u => u.CreateTime).ToListAsync().Result;

            });

            var tvrList = list.AsQueryable()
              .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
              .WhereIF(!string.IsNullOrEmpty(command.Name), u => u.Name.Contains(command.Name))
              .WhereIF(!string.IsNullOrEmpty(command.Mode), u => u.Mode.ToString().Contains(command.Mode))
              .WhereIF(!string.IsNullOrWhiteSpace(command.Type), u => u.Type.ToString().Contains(command.Type))
             .OrderBy(st => st.OrderNo)
             .ToList();

            return tvrList;
        }
    }
}
