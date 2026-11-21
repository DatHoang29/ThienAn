using Modules.CfgSystem.Core.Dto.OpConfig;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;


namespace Modules.CfgSystem.Controllers.OpConfig.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 09/08/2024
    /// </summary>
    public class SysOpConfigQueryHandler : IWolverineHandler
    {
        private BaseRepository<SysOpConfig> _opConfigRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "sysOpConfig";

        public SysOpConfigQueryHandler(BaseRepository<SysOpConfig> opConfigRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._opConfigRepo = opConfigRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageSysOpConfigOutput>> HandleAsync(PageSysOpConfigInput command)
        {
            var socList = await this._opConfigRepo.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), soc => soc.Name != null && soc.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), soc => soc.Code != null && soc.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), soc => soc.Value != null && soc.Value.Contains(command.Value, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.GroupCode), soc => soc.GroupCode != null && soc.GroupCode.Contains(command.GroupCode, StringComparison.OrdinalIgnoreCase))
                .Select(soc => new PageSysOpConfigOutput() { }, true)
                .OrderBuilder(command, "", "OrderNo", false)
                .ToPagedListAsync(command.Page, command.PageSize);

            return socList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<SysOpConfigOutput>> HandleAsync(SysOpConfigInput command)
        {
            var socList = await this._cache.GetOrAdd(_cacheName, _ =>
            {
                return this._opConfigRepo.AsQueryable()
                    .Select(soc => new SysOpConfigOutput() { }, true)
                    .OrderBy(soc => soc.OrderNo)
                    .ToListAsync();
            });

            var result = socList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), soc => soc.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), soc => soc.Code != null && soc.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), soc => soc.Value != null && soc.Value.Contains(command.Value, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.GroupCode), soc => soc.GroupCode != null && soc.GroupCode.Contains(command.GroupCode, StringComparison.OrdinalIgnoreCase))
                .Where(soc => soc.Status == BaseEnums.StatusEnum.Enable && soc.IsDelete == null)
                .ToList();

            return result;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lấy thông tin theo id hoặc code
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SysOpConfigOutput> HandleAsync(SysOpConfigSingleInput command)
        {
            var socList = await this._cache.GetOrAdd(_cacheName, _ =>
            {
                return this._opConfigRepo.AsQueryable()
                    .Select(soc => new SysOpConfigOutput() { }, true)
                    .OrderBy(soc => soc.OrderNo)
                    .ToListAsync();
            });

            var result = socList?
                .WhereIF(!string.IsNullOrWhiteSpace(command.Id), soc => soc.ID == command.Id)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), soc => soc.Code == command.Code)
                .Where(soc => soc.Status == BaseEnums.StatusEnum.Enable && soc.IsDelete == null)
                .FirstOrDefault();

            return result;
        }

        public async Task<dynamic> HandleAsync(AllOpConfigListInput command)
        {
            var ds = await _opConfigRepo.AsQueryable()
                .Where(u => u.IsDelete == null && u.Status == BaseEnums.StatusEnum.Enable)
                .Select(u => new { u.GroupCode, u.Code, u.Name, u.Value, u.Remark, u.OrderNo })
                .ToListAsync();

            return ds.OrderBy(u => u.GroupCode).OrderBy(u => u.OrderNo)
                .GroupBy(u => u.GroupCode)
                .ToDictionary(u => u.Key, u => u);
        }
    }
}
