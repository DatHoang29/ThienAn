using Modules.CfgSystem.Core.Dto.ConfigType;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.ConfigType.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class SysConfigTypeCommandHandler : IWolverineHandler
    {
        private BaseRepository<SysConfigType> _configTypeRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "sysConfigType";

        public SysConfigTypeCommandHandler(BaseRepository<SysConfigType> configTypeRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._configTypeRepo = configTypeRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddSysConfigTypeInput command)
        {
            if (await this._configTypeRepo.IsAnyAsync(sct => sct.IsDelete == null && sct.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            await this._configTypeRepo.InsertWithDiffLogAsync(command.Adapt<SysConfigType>());

            this._cache.RemoveByPrefixKey(_cacheName);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateSysConfigTypeInput command)
        {
            if (await this._configTypeRepo.IsAnyAsync(sct => sct.IsDelete == null && sct.Code == command.Code && sct.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var sct = await this._configTypeRepo.GetByIdAsync(command.ID);
            if (sct == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._configTypeRepo.UpdateWithDiffLogAsync(command.Adapt(sct));

            this._cache.RemoveByPrefixKey(_cacheName);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteSysConfigTypeInput command)
        {
            var sct = await this._configTypeRepo.GetFirstAsync(sct => sct.IsDelete == null && sct.ID == command.ID);
            if (sct == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._configTypeRepo.SoftDeleteAsync<SysConfigType>(sct);

            this._cache.RemoveByPrefixKey(_cacheName);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteSysConfigTypeInput> command)
        {
            List<string> ids = command.Select(item => item.ID).ToList();
            List<SysConfigType> sctList = await this._configTypeRepo.AsQueryable().Where(sct => sct.IsDelete == null && ids.Contains(sct.ID)).ToListAsync();

            if (sctList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._configTypeRepo.SoftDeleteAsync<SysConfigType>(sct => ids.Contains(sct.ID));

            this._cache.RemoveByPrefixKey(_cacheName);
        }
    }
}
