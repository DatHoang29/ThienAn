using Modules.TMS.Controllers.VehicleFilter.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.VehicleFilter.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 13/11/2024
    /// </summary>
    public class TmsVehicleFilterQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsVehicleFilter> _vehicleFilterRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "tmsVehicleFilter";

        public TmsVehicleFilterQueryHandler(BaseRepository<TmsVehicleFilter> vehicleFilterRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._vehicleFilterRepo = vehicleFilterRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 13/11/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageTmsVehicleFilterOutput>> HandleAsync(PageTmsVehicleFilterInput command)
        {
            var tvfList = await this._vehicleFilterRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), tvf => tvf.LicensePlate != null && tvf.LicensePlate.Contains(command.LicensePlate))
                .WhereIF(command.Type != null, tvf => tvf.Type == command.Type)
                .WhereIF(command.Alarm != null, tvf => tvf.Alarm == command.Alarm)
                .WhereIF(command.FromDate != null, tvf => tvf.EndDate >= command.FromDate)
                .WhereIF(command.ToDate != null, tvf => tvf.StartDate <= command.ToDate)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((st) => new PageTmsVehicleFilterOutput() { }, true)
                .OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);

            return tvfList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 13/11/2024
        /// </summary>
        public async Task<List<TmsVehicleFilterOutput>> HandleAsync(TmsVehicleFilterInput command)
        {
            var tvfList = await this._cache.GetOrAdd(CacheConst.Tms.TmsVehicleFilter, _ =>
            {
                return this._vehicleFilterRepo.AsQueryable()
                    .Select(tvf => new TmsVehicleFilterOutput() { }, true)
                    .OrderBy(tvf => tvf.CreateTime)
                    .ToListAsync();
            });

            var result = tvfList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), tvf => tvf.LicensePlate != null && tvf.LicensePlate.Contains(command.LicensePlate))
                .WhereIF(command.Type != null, tvf => tvf.Type == command.Type)
                .WhereIF(command.Alarm != null, tvf => tvf.Alarm == command.Alarm)
                .WhereIF(command.FromDate != null, tvf => tvf.EndDate >= command.FromDate)
                .WhereIF(command.ToDate != null, tvf => tvf.StartDate <= command.ToDate)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .ToList();

            return result;
        }
    }
}
