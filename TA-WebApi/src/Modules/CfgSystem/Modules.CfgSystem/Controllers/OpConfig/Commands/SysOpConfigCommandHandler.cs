using Furion.FriendlyException;
using Mapster;
using Modules.CfgSystem.Controllers.OpConfig.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.OpConfig.Commands
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class SysOpConfigCommandHandler
    {
        private BaseRepository<SysOpConfig> _baseRepository;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public SysOpConfigCommandHandler(BaseRepository<SysOpConfig> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._userManager = userManager;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Lưu một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddSysOpConfigInput command)
        {
            if (await this._baseRepository.IsAnyAsync(soc => soc.IsDelete == null && soc.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            await this._baseRepository.InsertWithDiffLogAsync(command.Adapt<SysOpConfig>());

            //remove cache
            _cache.RemoveByPrefixKey("SysOpConfig");
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Cập nhật một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateSysOpConfigInput command)
        {
            // TODO: Check authorization

            if (await this._baseRepository.IsAnyAsync(soc => soc.IsDelete == null && soc.Code == command.Code && soc.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var soc = await this._baseRepository.GetByIdAsync(command.ID);
            if (soc == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._baseRepository.UpdateWithDiffLogAsync(command.Adapt(soc));

            //remove cache
            _cache.RemoveByPrefixKey("SysOpConfig");
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteSysOpConfigInput command)
        {
            // TODO: Check authorization

            var soc = await this._baseRepository.GetFirstAsync(soc => soc.ID == command.ID);
            if (soc == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (soc.IsSysConfig == true)
                throw Oops.Oh(BaseMsg.BaseException.CannotDelete);

            await this._baseRepository.SoftDeleteAsync<SysOpConfig>(soc);

            //remove cache
            _cache.RemoveByPrefixKey("SysOpConfig");
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteSysOpConfigInput> command)
        {
            // TODO: Check authorization

            List<string> ids = command.Select(item  => item.ID).ToList();
            List<SysOpConfig> socList = await this._baseRepository.AsQueryable().Where(soc => ids.Contains(soc.ID)).ToListAsync();

            if (socList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            if (socList.Any(soc => soc.IsSysConfig == true))
                throw Oops.Oh(BaseMsg.BaseException.CannotDelete);

            await this._baseRepository.SoftDeleteAsync<SysOpConfig>(soc => ids.Contains(soc.ID));

            //remove cache
            _cache.RemoveByPrefixKey("SysOpConfig");
        }
    }
}
