using Modules.CfgSystem.Controllers.ConfigData.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.ConfigData.Queries
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Create date: 09/08/2024
    /// </summary>
    public class SysConfigDataQueryHandler
    {
        private BaseRepository<SysConfigData> _baseRepository;
        private BaseCacheService _cache;

        public SysConfigDataQueryHandler(BaseRepository<SysConfigData> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageSysConfigDataOutput>> HandleAsync(PageSysConfigDataInput command)
        {
            var scdList = await this._baseRepository.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name), scd => scd.Name != null && scd.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Code), scd => scd.Code != null && scd.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Value), scd => scd.Value != null && scd.Value.Contains(command.Value, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.ConfigTypeId), scd => scd.ConfigTypeId != null && scd.ConfigTypeId.Contains(command.ConfigTypeId, StringComparison.OrdinalIgnoreCase))
                //.Where(scd => scd.Status == BaseEnums.StatusEnum.Enable && scd.IsDelete == null) => TẠM THỜI KHÔNG LỌC THEO Status
                .Select(scd => new PageSysConfigDataOutput() { }, true)
                .OrderBuilder(command)
                .ToPagedListAsync(command.Page, command.PageSize);

            return scdList;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<SysConfigDataOutput>> HandleAsync(SysConfigDataInput command)
        {
            // TODO: Cache

            var scdList = await this._baseRepository.Context.Queryable<SysConfigType>()
                .InnerJoin<SysConfigData>((sct, scd) => sct.ID == scd.ConfigTypeId)
                .WhereIF(!string.IsNullOrEmpty(command.Name), (sct, scd) => scd.Name != null && scd.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Code), (sct, scd) => scd.Code != null && scd.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ConfigTypeCode), (sct, scd) => sct.Code != null && sct.Code.Equals(command.ConfigTypeCode, StringComparison.OrdinalIgnoreCase))
                //.Where((sct, scd) => sct.Status == (int)BaseEnums.StatusEnum.Enable && scd.Status == (int)BaseEnums.StatusEnum.Enable) => TẠM THỜI KHÔNG LỌC THEO Status
                .Where((sct, scd) => sct.IsDelete == null && scd.IsDelete == null)
                .OrderBy((sct, scd) => new { scd.OrderNo, scd.Code })
                .Select((sct, scd) => new SysConfigDataOutput(){
                    ConfigTypeCode = sct.Code,
                    ConfigDataId = scd.ID,
                    Value = scd.Value,
                    Name = scd.Name,
                    Code = scd.Code,
                    ClassSetting = scd.ClassSetting,
                    TagType = scd.TagType,
                    StyleSetting = scd.StyleSetting,
                    OrderNo = scd.OrderNo
                }, true)
                .ToListAsync();

            return scdList;
        }
    }
}
