using Modules.TMS.Controllers.WorkUnit.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.WorkUnit.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 25/11/2024
    /// </summary>
    public class TmsWorkUnitQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsWorkUnit> _workUnitRepo;
        private BaseCacheService _cache;

        public TmsWorkUnitQueryHandler(BaseRepository<TmsWorkUnit> workUnitRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._workUnitRepo = workUnitRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 25/11/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageTmsWorkUnitOutput>> HandleAsync(PageTmsWorkUnitInput command)
        {
            var wuList = await this._workUnitRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), wu => wu.Name != null && wu.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), wu => wu.Code != null && wu.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Phone), wu => wu.Phone != null && wu.Phone.Contains(command.Phone))
                .WhereIF(command.Type != null, wu => wu.Type == command.Type)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((st) => new PageTmsWorkUnitOutput() { }, true)
                .OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);

            return wuList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 25/11/2024
        /// </summary>
        public async Task<List<TmsWorkUnitOutput>> HandleAsync(TmsWorkUnitInput command)
        {
            var wuList = await this._cache.GetOrAdd(CacheConst.Tms.TmsWorkUnit, _ =>
            {
                return this._workUnitRepo.AsQueryable()
                    .Select(wu => new TmsWorkUnitOutput() { }, true)
                    .OrderBy(wu => wu.Name)
                    .ToListAsync();
            });

            var result = wuList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), wu => wu.Name != null && wu.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), wu => wu.Code != null && wu.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Phone), wu => wu.Phone != null && wu.Phone.Contains(command.Phone))
                .WhereIF(command.Type != null, wu => wu.Type == command.Type)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .ToList();

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin chi tiết
        /// Created date: 25/11/2024
        /// </summary>
        public async Task<TmsWorkUnitOutput> HandleAsync(TmsWorkUnitSingleInput command)
        {
            var wuList = await this._cache.GetOrAdd(CacheConst.Tms.TmsWorkUnit, _ =>
            {
                return this._workUnitRepo.AsQueryable()
                    .Select(wu => new TmsWorkUnitOutput() { }, true)
                    .OrderBy(wu => wu.Name)
                    .ToListAsync();
            });

            var result = wuList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Id), wu => wu.ID == command.Id)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), wu => wu.Code == command.Code)
                .FirstOrDefault();

            return result;
        }
    }
}
