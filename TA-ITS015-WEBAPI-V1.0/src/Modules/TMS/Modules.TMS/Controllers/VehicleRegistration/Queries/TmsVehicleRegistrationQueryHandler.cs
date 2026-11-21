using Modules.TMS.Controllers.VehicleRegistration.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.VehicleRegistration.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 17/12/2024
    /// </summary>
    public class TmsVehicleRegistrationQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsVehicleRegistration> _vehicleRegistrationRepo;
        private BaseCacheService _cache;

        public TmsVehicleRegistrationQueryHandler(BaseRepository<TmsVehicleRegistration> vehicleRegistrationRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._vehicleRegistrationRepo = vehicleRegistrationRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 17/12/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageTmsVehicleRegistrationOutput>> HandleAsync(PageTmsVehicleRegistrationInput command)
        {
            var tvrList = await this._vehicleRegistrationRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Owner), tvr => tvr.Owner != null && tvr.Owner.Contains(command.Owner))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Brand), tvr => tvr.Brand != null && tvr.Brand.Contains(command.Brand))
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleType), tvr => tvr.VehicleType != null && tvr.VehicleType.Contains(command.VehicleType))
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), tvr => tvr.LicensePlate != null && tvr.LicensePlate.Contains(command.LicensePlate))
                .Select((st) => new PageTmsVehicleRegistrationOutput() { }, true)
                .OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);

            return tvrList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 17/12/2024
        /// </summary>
        public async Task<List<TmsVehicleRegistrationOutput>> HandleAsync(TmsVehicleRegistrationInput command)
        {
            var tvfList = await this._cache.GetOrAdd(CacheConst.Tms.TmsVehicleRegistration, _ =>
            {
                return this._vehicleRegistrationRepo.AsQueryable()
                    .Select(tvf => new TmsVehicleRegistrationOutput() { }, true)
                    .OrderBy(tvf => tvf.CreateTime)
                    .ToListAsync();
            });

            var result = tvfList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Owner), tvr => tvr.Owner != null && tvr.Owner.Contains(command.Owner))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Brand), tvr => tvr.Brand != null && tvr.Brand.Contains(command.Brand))
                .WhereIF(!string.IsNullOrWhiteSpace(command.VehicleType), tvr => tvr.VehicleType != null && tvr.VehicleType.Contains(command.VehicleType))
                .WhereIF(!string.IsNullOrWhiteSpace(command.LicensePlate), tvr => tvr.LicensePlate != null && tvr.LicensePlate.Contains(command.LicensePlate))
                .ToList();

            return result;
        }
    }
}
