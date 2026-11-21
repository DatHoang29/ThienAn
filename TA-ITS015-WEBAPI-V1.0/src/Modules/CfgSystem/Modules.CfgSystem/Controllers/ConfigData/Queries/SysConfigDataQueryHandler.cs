using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.CfgSystem.Core.Dto.ConfigType;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Persistence.SqlSugar;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.ConfigData.Queries
{
    /// <summary>
    /// Author: ThangNN
    /// Create date: 09/08/2024
    /// </summary>
    public class SysConfigDataQueryHandler : IWolverineHandler
    {
        private IMessageBus _messageBus;
        private BaseRepository<SysConfigData> _configDataRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "sysConfigData";

        public SysConfigDataQueryHandler(IMessageBus messageBús, BaseRepository<SysConfigData> baseRepository, BaseCacheService cache)
        {
            this._cache = cache;
            this._messageBus = messageBús;
            this._configDataRepo = baseRepository;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo phân trang
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<SqlSugarPagedList<PageSysConfigDataOutput>> HandleAsync(PageSysConfigDataInput command)
        {
            var scdList = await this._configDataRepo.AsQueryable()
                .WhereIF(!string.IsNullOrEmpty(command.Name), scd => scd.Name != null && scd.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Code), scd => scd.Code != null && scd.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Value), scd => scd.Value != null && scd.Value.Contains(command.Value, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.ConfigTypeId), scd => scd.ConfigTypeId != null && scd.ConfigTypeId.Contains(command.ConfigTypeId, StringComparison.OrdinalIgnoreCase))
                .Select(scd => new PageSysConfigDataOutput() { }, true)
                .OrderBuilder(command, "", "OrderNo", false)
                .ToPagedListAsync(command.Page, command.PageSize);

            return scdList;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: List theo tìm kiếm
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<List<SysConfigDataOutput>> HandleAsync(SysConfigDataInput command)
        {
            var scdList = await this._cache.GetOrAdd(_cacheName, _ =>
            {
                return this._configDataRepo.AsQueryable()
                    .Select(scd => new SysConfigDataOutput() { }, true)
                    .OrderBy(scd => scd.OrderNo)
                    .ToListAsync();
            }) ?? new List<SysConfigDataOutput>();

            var sctList = await this._messageBus.InvokeAsync<List<SysConfigTypeOutput>>(new SysConfigTypeInput() { }) ?? new List<SysConfigTypeOutput>();

            var dataList = from scd in scdList
                           join sct in sctList on scd.ConfigTypeId equals sct.ID
                           select new SysConfigDataOutput()
                           {
                               ConfigTypeCode = sct.Code,
                               ConfigDataId = scd.ID,
                               Value = scd.Value,
                               Name = scd.Name,
                               Code = scd.Code,
                               ClassSetting = scd.ClassSetting,
                               TagType = scd.TagType,
                               StyleSetting = scd.StyleSetting,
                               OrderNo = scd.OrderNo
                           };

            var result = dataList?
                .WhereIF(!string.IsNullOrEmpty(command.Name), item => item.Name != null && item.Name.Contains(command.Name, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrEmpty(command.Code), item => item.Code != null && item.Code.Contains(command.Code, StringComparison.OrdinalIgnoreCase))
                .WhereIF(!string.IsNullOrWhiteSpace(command.ConfigTypeCode), item => item.ConfigTypeCode != null && item.ConfigTypeCode.Equals(command.ConfigTypeCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return result;
        }
    }
}
