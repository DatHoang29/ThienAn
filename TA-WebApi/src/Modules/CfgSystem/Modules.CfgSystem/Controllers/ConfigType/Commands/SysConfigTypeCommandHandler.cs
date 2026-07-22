using Furion.FriendlyException;
using Mapster;
using Modules.CfgSystem.Controllers.ConfigType.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.ConfigType.Commands
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class SysConfigTypeCommandHandler
    {
        private BaseRepository<SysConfigType> _baseRepository;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public SysConfigTypeCommandHandler(BaseRepository<SysConfigType> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._userManager = userManager;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Thêm 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddSysConfigTypeInput command)
        {
            if (await this._baseRepository.IsAnyAsync(sct => sct.IsDelete == null && sct.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            await this._baseRepository.InsertWithDiffLogAsync(command.Adapt<SysConfigType>());
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Cập nhật 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateSysConfigTypeInput command)
        {
            // TODO: Check authorization

            if (await this._baseRepository.IsAnyAsync(sct => sct.IsDelete == null && sct.Code == command.Code && sct.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var sct = await this._baseRepository.GetByIdAsync(command.ID);
            if (sct == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._baseRepository.UpdateWithDiffLogAsync(command.Adapt(sct));
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteSysConfigTypeInput command)
        {
            // TODO: Check authorization

            var sct = await this._baseRepository.GetFirstAsync(sct => sct.IsDelete == null && sct.ID == command.ID);
            if (sct == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._baseRepository.SoftDeleteAsync<SysConfigType>(sct);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteSysConfigTypeInput> command)
        {
            // TODO: Check authorization

            List<string> ids = command.Select(item  => item.ID).ToList();
            List<SysConfigType> sctList = await this._baseRepository.AsQueryable().Where(sct => sct.IsDelete == null && ids.Contains(sct.ID)).ToListAsync();

            if (sctList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._baseRepository.SoftDeleteAsync<SysConfigType>(sct => ids.Contains(sct.ID));
        }
    }
}
