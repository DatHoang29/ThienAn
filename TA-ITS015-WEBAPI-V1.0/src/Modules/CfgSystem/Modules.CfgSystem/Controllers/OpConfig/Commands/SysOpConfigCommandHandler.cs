using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.CfgSystem.Core.Dto.OpConfig;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.CfgSystem.Core.Refresh;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.OpConfig.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class SysOpConfigCommandHandler : IWolverineHandler
    {
        private BaseRepository<SysOpConfig> _opConfigRepo;
        private BaseCacheService _cache;
        private RefreshModifyService _refreshModify;
        private const string _cacheName = "sysOpConfig";

        public SysOpConfigCommandHandler(BaseRepository<SysOpConfig> opConfigRepo, BaseCacheService cache, RefreshModifyService refreshModify)
        {
            this._cache = cache;
            this._opConfigRepo = opConfigRepo;
            this._refreshModify = refreshModify;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Lưu một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddSysOpConfigInput command)
        {
            if (await this._opConfigRepo.IsAnyAsync(soc => soc.IsDelete == null && soc.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            await this._opConfigRepo.InsertWithDiffLogAsync(command.Adapt<SysOpConfig>());

            this._cache.RemoveByPrefixKey(_cacheName);
            await NotifyAsync(command.GroupCode, command.Code, "add");
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateSysOpConfigInput command)
        {
            if (await this._opConfigRepo.IsAnyAsync(soc => soc.IsDelete == null && soc.Code == command.Code && soc.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var soc = await this._opConfigRepo.GetByIdAsync(command.ID);
            if (soc == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._opConfigRepo.UpdateWithDiffLogAsync(command.Adapt(soc));

            this._cache.RemoveByPrefixKey(_cacheName);
            await NotifyAsync(soc.GroupCode, soc.Code, "update");
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteSysOpConfigInput command)
        {
            var soc = await this._opConfigRepo.GetFirstAsync(soc => soc.ID == command.ID);
            if (soc == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (soc.IsSysConfig == true)
                throw Oops.Oh(BaseMsg.BaseException.CannotDelete);

            await this._opConfigRepo.SoftDeleteAsync<SysOpConfig>(soc);

            this._cache.RemoveByPrefixKey(_cacheName);
            await NotifyAsync(soc.GroupCode, soc.Code, "delete");
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteSysOpConfigInput> command)
        {
            List<string> ids = command.Select(item => item.ID).ToList();
            List<SysOpConfig> socList = await this._opConfigRepo.AsQueryable().Where(soc => ids.Contains(soc.ID)).ToListAsync();

            if (socList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            if (socList.Any(soc => soc.IsSysConfig == true))
                throw Oops.Oh(BaseMsg.BaseException.CannotDelete);

            await this._opConfigRepo.SoftDeleteAsync<SysOpConfig>(soc => ids.Contains(soc.ID));

            this._cache.RemoveByPrefixKey(_cacheName);
            foreach (var group in socList.GroupBy(x => x.GroupCode))
                await NotifyAsync(group.Key, null, "batch-delete");
        }

        private Task NotifyAsync(string? groupCode, string? key, string action)
        {
            if (string.IsNullOrWhiteSpace(groupCode)) return Task.CompletedTask;
            return this._refreshModify.NotifyAsync(new RefreshModifyRequest("opconfig", groupCode, key, action));
        }
    }
}
