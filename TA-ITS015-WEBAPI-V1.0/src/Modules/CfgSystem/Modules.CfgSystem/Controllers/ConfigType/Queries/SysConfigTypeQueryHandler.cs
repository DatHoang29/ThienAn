using Modules.CfgSystem.Core.Dto.ConfigType;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.ConfigType.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 09/08/2024
    /// </summary>
    public class SysConfigTypeQueryHandler : IWolverineHandler
    {
        private BaseRepository<SysConfigType> _configTypeRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "sysConfigType";

        public SysConfigTypeQueryHandler(BaseRepository<SysConfigType> configTypeRep, BaseCacheService cache)
        {
            this._cache = cache;
            this._configTypeRepo = configTypeRep;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageSysConfigTypeOutput>> HandleAsync(PageSysConfigTypeInput command)
        {
            var sctList = await this._configTypeRepo.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name),
                    sct => sct.Name != null && sct.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.Code),
                    sct => sct.Code != null && sct.Code.Contains(command.Code))
                .Select((sct) => new PageSysConfigTypeOutput() { }, true)
                .OrderBuilder(command, "", "OrderNo", false)
                .ToPagedListAsync(command.Page, command.PageSize);

            return sctList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<SysConfigTypeOutput>> HandleAsync(SysConfigTypeInput command)
        {
            var sctList = await this._cache.GetOrAdd(_cacheName, _ =>
            {
                return this._configTypeRepo.AsQueryable()
                    .Select(sct => new SysConfigTypeOutput() { }, true)
                    .OrderBy(sct => sct.OrderNo)
                    .ToListAsync();
            });

            var result = sctList?
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Name), sct => sct.Name != null && sct.Name.Contains(command.Name))
                    .WhereIF(!string.IsNullOrWhiteSpace(command.Code), sct => sct.Code != null && sct.Code.Contains(command.Code))
                    .ToList();

            return result;
        }

        public async Task<dynamic> HandleAsync(AllConfigListInput command)
        {
            var ds = await _configTypeRepo.AsQueryable()
                .InnerJoin<SysConfigData>((u, a) => u.ID == a.ConfigTypeId).ClearFilter()
                .Where((u, a) => u.IsDelete == null && a.IsDelete == null && u.Status == BaseEnums.StatusEnum.Enable &&
                                 a.Status == BaseEnums.StatusEnum.Enable)
                .Select((u, a) => new { TypeCode = u.Code, a.Code, a.Name, a.Value, a.Remark, a.OrderNo, a.TagType, a.StyleSetting, a.ClassSetting })
                .ToListAsync();

            return ds.OrderBy(u => u.OrderNo)
                .GroupBy(u => u.TypeCode)
                .ToDictionary(u => u.Key, u => u);

        }
    }
}
