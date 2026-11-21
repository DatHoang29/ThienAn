using Modules.TMS.Controllers.SignalLogs.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.SignalLogs.Queries
{
    public class TmsSignalLogQueryHandler
    {
        private BaseRepository<TmsSignalLog> _TmsSignalLogRep;
        private BaseCacheService _cache;

        public TmsSignalLogQueryHandler(BaseRepository<TmsSignalLog> TmsSignalLogRep, BaseCacheService cache)
        {
            _TmsSignalLogRep = TmsSignalLogRep;
            _cache = cache;
        }

        public async Task<SqlSugarPagedList<PageTmsSignalLogsOutput>> HandleAsync(PageTmsSignalLogsInput command)
        {
            return await _TmsSignalLogRep.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), u => u.EquipmentId.Contains(command.EquipmentId))
                .Select((u) => new PageTmsSignalLogsOutput()
                {
                }, true).OrderByDescending(st => st.CreateTime)
                .OrderBuilder(command, "")
                .ToPagedListAsync(command.Page, command.PageSize);
        }
        public async Task<List<SignalLogsOutput>> HandleAsync(SignalLogsInput command)
        {

            //var list =  _cache.GetOrAdd($"sysTmsMapDetail:{command.Name}",   _ =>
            //{
            //    return   _TmsSignalLogRep.AsQueryable()
            //        .Select((u) => new TmsSignalLogsOutput()
            //        {
            //        }, true)
            //        .OrderBy(u => u.CreateTime).ToListAsync().Result;

            //});

            //            var tvrList = await this._baseRepository.AsQueryable()

            //list = list.WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.Name), u => u.Name.Contains(command.Name))
            //    .WhereIF(!string.IsNullOrWhiteSpace(command.MapId), u => u.MapId.Contains(command.MapId)).ToList();
            //return list;
            var tvrList = await _TmsSignalLogRep.AsQueryable()
               .WhereIF(!string.IsNullOrWhiteSpace(command.Code), u => u.Code.Contains(command.Code))
               .WhereIF(!string.IsNullOrWhiteSpace(command.EquipmentId), u => u.EquipmentId.Contains(command.EquipmentId))
              .Select((st) => new SignalLogsOutput() { }, true)
              .OrderBy(st => st.CreateTime)
              .ToListAsync();

            return tvrList;
        }
    }
}
