using Furion.FriendlyException;
using Mapster;
using Modules.CfgSystem.Controllers.ConfigData.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.ConfigData.Commands
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class SysConfigDataCommandHandler
    {
        private BaseRepository<SysConfigData> _baseRepository;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public SysConfigDataCommandHandler(BaseRepository<SysConfigData> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._userManager = userManager;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Thêm mới 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddSysConfigDataInput command)
        {
            if (await this._baseRepository.IsAnyAsync(scd => scd.IsDelete == null && scd.Code == command.Code && scd.ConfigTypeId == command.ConfigTypeId))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            await this._baseRepository.InsertWithDiffLogAsync(command.Adapt<SysConfigData>());
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Cập nhật 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateSysConfigDataInput command)
        {
            // TODO: Check authorization

            if (await this._baseRepository.IsAnyAsync(scd => scd.IsDelete == null && scd.Code == command.Code && scd.ConfigTypeId == command.ConfigTypeId && scd.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var scd = await this._baseRepository.GetByIdAsync(command.ID);
            if (scd == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._baseRepository.UpdateWithDiffLogAsync(command.Adapt(scd));
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa 1 record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteSysConfigDataInput command)
        {
            // TODO: Check authorization

            var scd = await this._baseRepository.GetFirstAsync(scd => scd.ID == command.ID);
            if (scd == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._baseRepository.SoftDeleteAsync<SysConfigData>(scd);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteSysConfigDataInput> command)
        {
            // TODO: Check authorization

            List<string> ids = command.Select(item  => item.ID).ToList();
            List<SysConfigData> scdList = await this._baseRepository.AsQueryable().Where(scd => ids.Contains(scd.ID)).ToListAsync();

            if (scdList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._baseRepository.SoftDeleteAsync<SysConfigData>(scd => ids.Contains(scd.ID));
        }
    }
}
