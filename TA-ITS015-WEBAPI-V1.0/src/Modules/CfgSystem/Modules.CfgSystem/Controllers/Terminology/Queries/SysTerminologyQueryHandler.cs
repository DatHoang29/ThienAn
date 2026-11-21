using Modules.CfgSystem.Core.Dto.Terminology;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.Terminology.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 09/08/2024
    /// </summary>
    public class SysTerminologyQueryHandler : IWolverineHandler
    {
        private BaseRepository<SysTerminology> _terminologyRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "sysTerminology";

        public SysTerminologyQueryHandler(BaseRepository<SysTerminology> terminologyRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._terminologyRepo = terminologyRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageSysTerminologyOutput>> HandleAsync(PageSysTerminologyInput command)
        {
            var stList = await this._terminologyRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), st => st.Name != null && st.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), st => st.Code != null && st.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), st => st.Value != null && st.Value.Contains(command.Value))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Language), st => st.Lang != null && st.Lang.Contains(command.Language))
                .OrderBuilder(command, "", "OrderNo", false)
                .Select((st) => new PageSysTerminologyOutput() { }, true)
                .ToPagedListAsync(command.Page, command.PageSize);

            return stList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<SysTerminologyOutput>> HandleAsync(SysTerminologyInput command)
        {
            var stList = await this._cache.GetOrAdd(_cacheName, _ =>
            {
                return this._terminologyRepo.AsQueryable()
                    .Select(st => new SysTerminologyOutput() { }, true)
                    .OrderBy(st => st.OrderNo)
                    .ToListAsync();
            });

            var result = stList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), st => st.Name != null && st.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), st => st.Code != null && st.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), st => st.Value != null && st.Value.Contains(command.Value))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Language), st => st.Lang != null && st.Lang.Contains(command.Language))
                .ToList();

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tham số
        /// </summary>
        public async Task<List<SysTerminologyOutput>> HandleAsync(SysTerminologyParaInput command)
        {
            var stList = await this._cache.GetOrAdd(_cacheName, _ =>
            {
                return this._terminologyRepo.AsQueryable()
                    .Select(st => new SysTerminologyOutput() { }, true)
                    .OrderBy(st => st.OrderNo)
                    .ToListAsync();
            });

            var result = stList?
                .WhereIF(command.CodeList?.Count() > 0, st => command.CodeList.Contains(st.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Language), st => st.Lang != null && st.Lang.Contains(command.Language))
                .ToList();

            return result;
        }
    }
}
