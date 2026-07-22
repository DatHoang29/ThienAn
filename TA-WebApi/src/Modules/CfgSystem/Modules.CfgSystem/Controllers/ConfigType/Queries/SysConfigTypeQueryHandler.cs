using Modules.CfgSystem.Controllers.ConfigType.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;
using Modules.System.Core.Entities;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.ConfigType.Queries
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Create date: 09/08/2024
    /// </summary>
    public class SysConfigTypeQueryHandler
    {
        private BaseRepository<SysConfigType> _configTypeRep;
        private BaseCacheService _cache;

        public SysConfigTypeQueryHandler(BaseRepository<SysConfigType> configTypeRep, BaseCacheService cache,
            UserManager userManager)
        {
            this._cache = cache;
            this._configTypeRep = configTypeRep;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageSysConfigTypeOutput>> HandleAsync(PageSysConfigTypeInput command)
        {
            // TODO: Cache

            var sctList = await this._configTypeRep.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name),
                    sct => sct.Name != null && sct.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.Code),
                    sct => sct.Code != null && sct.Code.Contains(command.Code))
                .Select((sct) => new PageSysConfigTypeOutput() { }, true)
                .OrderBuilder(command)
                .ToPagedListAsync(command.Page, command.PageSize);

            return sctList;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<SysConfigTypeOutput>> HandleAsync(SysConfigTypeInput command)
        {
            // TODO: Cache

            var sctList = await this._configTypeRep.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name),
                    sct => sct.Name != null && sct.Name.Contains(command.Name))
                .WhereIF(!string.IsNullOrEmpty(command.Code),
                    sct => sct.Code != null && sct.Code.Contains(command.Code))
                .Select((sct) => new SysConfigTypeOutput() { }, true)
                .OrderBy(sct => sct.CreateTime)
                .ToListAsync();

            return sctList;
        }

        public async Task<dynamic> HandleAsync(AllConfigListInput command)
        {
            var ds = await _configTypeRep.AsQueryable()
                .InnerJoin<SysConfigData>((u, a) => u.ID == a.ConfigTypeId).ClearFilter()
                .Where((u, a) => u.IsDelete == null && a.IsDelete == null && u.Status == BaseEnums.StatusEnum.Enable &&
                                 a.Status == BaseEnums.StatusEnum.Enable)
                .Select((u, a) => new {TypeCode = u.Code, a.Code, a.Name, a.Value, a.Remark, a.OrderNo, a.TagType})
                .ToListAsync();

            return ds.OrderBy(u => u.OrderNo)
                .GroupBy(u => u.TypeCode)
                .ToDictionary(u => u.Key, u => u);

        }
    }
}
