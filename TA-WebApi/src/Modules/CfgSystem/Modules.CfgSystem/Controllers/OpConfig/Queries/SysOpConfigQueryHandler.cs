using Modules.CfgSystem.Controllers.OpConfig.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.OpConfig.Queries
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Create date: 09/08/2024
    /// </summary>
    public class SysOpConfigQueryHandler
    {
        private BaseRepository<SysOpConfig> _baseRepository;
        private BaseCacheService _cache;

        public SysOpConfigQueryHandler(BaseRepository<SysOpConfig> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageSysOpConfigOutput>> HandleAsync(PageSysOpConfigInput command)
        {
            // TODO: Cache

            var socList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), soc => soc.Name != null && soc.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), soc => soc.Code != null && soc.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), soc => soc.Value != null && soc.Value.Contains(command.Value, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.GroupCode), soc => soc.GroupCode != null && soc.GroupCode.Contains(command.GroupCode, StringComparison.OrdinalIgnoreCase))
                //.Where(soc => soc.Status == BaseEnums.StatusEnum.Enable && soc.IsDelete == null)
                .Select(soc => new PageSysOpConfigOutput() { }, true)
                .OrderBuilder(command)
                .ToPagedListAsync(command.Page, command.PageSize);

            return socList;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<SysOpConfigOutput>> HandleAsync(SysOpConfigInput command)
        {
            // TODO: Cache

            var socList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), soc => soc.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), soc => soc.Code != null && soc.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), soc => soc.Value != null && soc.Value.Contains(command.Value, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.GroupCode), soc => soc.GroupCode != null && soc.GroupCode.Contains(command.GroupCode, StringComparison.OrdinalIgnoreCase))
                .Where(soc => soc.Status == BaseEnums.StatusEnum.Enable && soc.IsDelete == null)
                .Select(soc => new SysOpConfigOutput() { }, true)
                .OrderBy(soc => soc.GroupCode)
                .OrderBy(soc => soc.Code)
                .OrderBy(soc => soc.CreateTime)
                .ToListAsync();
            
            return socList;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Lấy thông tin theo id hoặc code
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SysOpConfigOutput> HandleAsync(SysOpConfigSingleInput command)
        {
            var sysOpConfig = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Id), soc => soc.ID == command.Id)
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), soc => soc.Code == command.Code)
                .Where(soc => soc.IsDelete == null)
                .Select(soc => new SysOpConfigOutput() { }, true)
                .FirstAsync();

            return sysOpConfig;
        }
    }
}
