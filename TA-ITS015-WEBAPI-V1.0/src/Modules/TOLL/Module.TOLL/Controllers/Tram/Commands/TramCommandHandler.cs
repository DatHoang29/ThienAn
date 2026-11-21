using Module.TOLL.Core.Constants;
using Module.TOLL.Core.Dto.Tram;
using Modules.TMS.Core.Entities;
using Modules.TOLL.Core.Entities;
using Modules.TOLL.Core.Exceptions;
using Modules.TOLL.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Constants.Localization;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TOLL.Controllers.Tram.Commands
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Xử lý gán (map) trạm thu phí (TollStation) với đơn vị TMS loại Trạm thu phí (TmsWorkUnit.Type = toll)
    /// Created date: 07/10/2026
    /// </summary>
    public class TramCommandHandler : IWolverineHandler
    {
        private readonly BaseRepository<TollStation> _tramRep;
        private readonly BaseRepository<TmsWorkUnit> _workUnitRep;
        private readonly BaseCacheService _cache;

        public TramCommandHandler(BaseRepository<TollStation> tramRep, BaseRepository<TmsWorkUnit> workUnitRep, BaseCacheService cache)
        {
            _tramRep = tramRep;
            _workUnitRep = workUnitRep;
            _cache = cache;
        }

        /// <summary>
        /// Gán / bỏ gán đơn vị TMS cho trạm thu phí.
        /// Author: HieuNV
        /// Date: 07/10/2026
        /// </summary>
        /// <returns></returns>
        public async Task HandleAsync(MapWorkUnitTramInput command)
        {
            var station = await _tramRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseLocaleManager.BaseException.DataNotExist);

            var refId = string.IsNullOrWhiteSpace(command.RefId) ? null : command.RefId.Trim();

            if (refId != null)
            {
                var tollWorkUnits = await _workUnitRep.AsQueryable()
                    .Where(u => u.IsDelete == null && u.Type == "toll")
                    .ToListAsync();

                var workUnit = tollWorkUnits.FirstOrDefault(u => u.ID == refId)
                    ?? throw Oops.Oh(BaseMsg.Exception.WorkUnitNotToll, refId);

                var mappedStation = await _tramRep.GetFirstAsync(u => u.RefId == refId && u.ID != station.ID);
                if (mappedStation != null)
                    throw Oops.Oh(BaseMsg.Exception.WorkUnitAlreadyMapped, workUnit.Name ?? refId, mappedStation.Name ?? mappedStation.StationId ?? string.Empty);
            }

            if (string.Equals(station.RefId, refId, StringComparison.Ordinal))
                return;

            station.RefId = refId;
            await _tramRep.UpdateWithDiffLogAsync(station);
            _cache.RemoveByPrefixKey(CacheConst.Toll.TollStation);
        }
    }
}
