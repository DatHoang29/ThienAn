using Furion.FriendlyException;
using Mapster;
using Modules.CfgSystem.Controllers.Terminology.Dto;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.Terminology.Commands
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Created date: 09/08/2024
    /// </summary>
    public class SysTerminologyCommandHandler
    {
        private BaseRepository<SysTerminology> _baseRepository;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public SysTerminologyCommandHandler(BaseRepository<SysTerminology> baseRepository, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._userManager = userManager;
            this._baseRepository = baseRepository;
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Thêm mới một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddSysTerminologyInput command)
        {
            if (await this._baseRepository.IsAnyAsync(st => st.IsDelete == null && (st.Code == command.Code && st.Lang == command.Lang)))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            await this._baseRepository.InsertWithDiffLogAsync(command.Adapt<SysTerminology>());
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Cập nhật một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateSysTerminologyInput command)
        {
            if (await this._baseRepository.IsAnyAsync(st => st.IsDelete == null && (st.Code == command.Code && st.Lang == command.Lang && st.ID != command.ID)))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            var st = await this._baseRepository.GetByIdAsync(command.ID);
            if (st == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // TODO: Check authorization

            await this._baseRepository.UpdateWithDiffLogAsync(command.Adapt(st));
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteSysTerminologyInput command)
        {
            var st = await this._baseRepository.GetFirstAsync(st => st.ID == command.ID);
            if (st == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // TODO: Check authorization

            await this._baseRepository.SoftDeleteAsync<SysTerminology>(st);
        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteSysTerminologyInput> command)
        {
            List<string> ids = command.Select(item  => item.ID).ToList();
            List<SysTerminology> stList = await this._baseRepository.GetListAsync(st => ids.Contains(st.ID));

            if (stList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // TODO: Check authorization

            await this._baseRepository.SoftDeleteAsync<SysTerminology>(st => ids.Contains(st.ID));
        }
    }
}
