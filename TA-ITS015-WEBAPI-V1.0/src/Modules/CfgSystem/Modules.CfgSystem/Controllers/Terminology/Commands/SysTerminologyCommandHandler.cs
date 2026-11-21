using Modules.CfgSystem.Core.Dto.Terminology;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Core.Exceptions;
using Modules.CfgSystem.Infrastructure;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.CfgSystem.Controllers.Terminology.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 09/08/2024
    /// </summary>
    public class SysTerminologyCommandHandler : IWolverineHandler
    {
        private BaseRepository<SysTerminology> _terminologyRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "sysTerminology";

        public SysTerminologyCommandHandler(BaseRepository<SysTerminology> terminologyRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._terminologyRepo = terminologyRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(AddSysTerminologyInput command)
        {
            if (await this._terminologyRepo.IsAnyAsync(st => st.IsDelete == null && (st.Code == command.Code && st.Lang == command.Lang)))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            await this._terminologyRepo.InsertWithDiffLogAsync(command.Adapt<SysTerminology>());

            this._cache.RemoveByPrefixKey(_cacheName);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateSysTerminologyInput command)
        {
            if (await this._terminologyRepo.IsAnyAsync(st => st.IsDelete == null && (st.Code == command.Code && st.Lang == command.Lang && st.ID != command.ID)))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            var st = await this._terminologyRepo.GetByIdAsync(command.ID);
            if (st == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._terminologyRepo.UpdateWithDiffLogAsync(command.Adapt(st));

            var rs = this._cache.RemoveByPrefixKey(_cacheName);
            this._cache.Remove(_cacheName);

        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteSysTerminologyInput command)
        {
            var st = await this._terminologyRepo.GetFirstAsync(st => st.ID == command.ID);
            if (st == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._terminologyRepo.SoftDeleteAsync<SysTerminology>(st);

            this._cache.RemoveByPrefixKey(_cacheName);
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteSysTerminologyInput> command)
        {
            List<string> ids = command.Select(item => item.ID).ToList();
            List<SysTerminology> stList = await this._terminologyRepo.GetListAsync(st => ids.Contains(st.ID));

            if (stList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._terminologyRepo.SoftDeleteAsync<SysTerminology>(st => ids.Contains(st.ID));

            this._cache.RemoveByPrefixKey(_cacheName);
        }
    }
}
