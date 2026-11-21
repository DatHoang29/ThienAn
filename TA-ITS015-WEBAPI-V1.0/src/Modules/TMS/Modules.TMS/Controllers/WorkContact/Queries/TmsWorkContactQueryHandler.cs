using Modules.TMS.Controllers.WorkContact.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.WorkContact.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 05/11/2024
    /// </summary>
    public class TmsWorkContactQueryHandler : IWolverineHandler
    {
        private BaseRepository<TmsWorkContact> _workContactRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "tmsWorkContact";

        public TmsWorkContactQueryHandler(BaseRepository<TmsWorkContact> workContactRepo, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._workContactRepo = workContactRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 05/11/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageTmsWorkContactOutput>> HandleAsync(PageTmsWorkContactInput command)
        {
            var wuList = await this._workContactRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), wu => wu.Name != null && wu.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), wu => wu.Code != null && wu.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Phone), wu => wu.Phone != null && wu.Phone.Contains(command.Phone))
                .WhereIF(!string.IsNullOrWhiteSpace(command.RefId), wc => wc.RefId == command.RefId)
                .WhereIF(command.Type != null, wu => wu.Type == command.Type)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .Select((st) => new PageTmsWorkContactOutput() { }, true)
                .OrderBuilder(command, "", BaseConst.SortFieldConst.UpdateOrCreateTime)
                .ToPagedListAsync(command.Page, command.PageSize);

            return wuList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin chi tiết
        /// Created date: 05/11/2024
        /// </summary>
        public async Task<TmsWorkContactOutput> HandleAsync(TmsWorkContactSingleInput command)
        {
            var wcList = await this._cache.GetOrAdd(CacheConst.Tms.TmsWorkContact, _ =>
            {
                return this._workContactRepo.AsQueryable()
                    .Select(wc => new TmsWorkContactOutput() { }, true)
                    .OrderBy(wc => wc.Name)
                    .ToListAsync();
            });

            var result = wcList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Id), wc => wc.ID == command.Id)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), wc => wc.Code == command.Code)

                .Where(wc => wc.IsDelete == null)
                .FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 05/11/2024
        /// </summary>
        public async Task<List<TmsWorkContactOutput>> HandleAsync(TmsWorkContactInput command)
        {
            var wcList = await this._cache.GetOrAdd(CacheConst.Tms.TmsWorkContact, _ =>
            {
                return this._workContactRepo.AsQueryable()
                    .Select(wc => new TmsWorkContactOutput() { }, true)
                    .OrderBy(wc => wc.Name)
                    .ToListAsync();
            });

            var result = wcList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), wu => wu.Name != null && wu.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), wu => wu.Code != null && wu.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Phone), wu => wu.Phone != null && wu.Phone.Contains(command.Phone))
                .WhereIF(!string.IsNullOrWhiteSpace(command.RefId), wc => wc.RefId == command.RefId)
                .WhereIF(command.Type != null, wu => wu.Type == command.Type)
                .WhereIF(command.Status != null, u => u.Status == command.Status)
                .ToList();

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin chi tiết theo tham số
        /// Created date: 05/11/2024
        /// </summary>
        public async Task<TmsWorkContact> HandleAsync(TmsWorkContactPara command)
        {
            var wcList = await this._cache.GetOrAdd(CacheConst.Tms.TmsWorkContact, _ =>
            {
                return this._workContactRepo.AsQueryable()
                    .Select(wc => new TmsWorkContactOutput() { }, true)
                    .OrderBy(wc => wc.Name)
                    .ToListAsync();
            });

            var result = wcList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Id), wc => wc.ID == command.Id)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), wc => wc.Code == command.Code)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), wc => wc.Name == command.Name)
                .WhereIF(!string.IsNullOrWhiteSpace(command.RefId), wc => wc.RefId == command.RefId)
                .FirstOrDefault();

            return result;
        }
    }
}
