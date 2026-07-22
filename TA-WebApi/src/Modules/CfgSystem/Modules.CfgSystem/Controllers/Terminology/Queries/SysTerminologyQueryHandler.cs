using Modules.CfgSystem.Controllers.Terminology.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.Terminology.Queries
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Create date: 09/08/2024
    /// </summary>
    public class SysTerminologyQueryHandler
    {
        private BaseRepository<SysTerminology> _baseRepository;
        private BaseCacheService _cache;

        public SysTerminologyQueryHandler(BaseRepository<SysTerminology> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageSysTerminologyOutput>> HandleAsync(PageSysTerminologyInput command)
        {
            var stList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), st => st.Name != null && st.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), st => st.Code != null && st.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), st => st.Value != null && st.Value.Contains(command.Value))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Language), st => st.Lang != null && st.Lang.Contains(command.Language))
                .OrderBuilder(command)
                .Select((st) => new PageSysTerminologyOutput() { }, true)
                .ToPagedListAsync(command.Page, command.PageSize);

            return stList;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<SysTerminologyOutput>> HandleAsync(SysTerminologyInput command)
        {
            // TODO: Cache

            var stList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrWhiteSpace(command.Name), st => st.Name != null && st.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Code), st => st.Code != null && st.Code.Contains(command.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Value), st => st.Value != null && st.Value.Contains(command.Value))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Language), st => st.Lang != null && st.Lang.Contains(command.Language))
                .Select((st) => new SysTerminologyOutput() { }, true)
                .OrderBy(st => st.CreateTime)
                .ToListAsync();

            return stList;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo tham số
        /// </summary>
        public async Task<List<SysTerminologyOutput>> HandleAsync(SysTerminologyParaInput command)
        {
            // TODO: Cache

            var stList = await this._baseRepository.AsQueryable()
                .WhereIF(command.CodeList?.Count() > 0, st => command.CodeList.Contains(st.Code))
                .WhereIF(!string.IsNullOrWhiteSpace(command.Language), st => st.Lang != null && st.Lang.Contains(command.Language))
                .Select((st) => new SysTerminologyOutput() { }, true)
                .OrderBy(st => st.Value)
                .ToListAsync();

            return stList;
        }
    }
}
