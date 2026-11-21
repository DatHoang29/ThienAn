using Modules.TMS.Controllers.WorkContact.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.WorkContact.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 05/11/2024
    /// </summary>
    public class TmsWorkContactCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsWorkContact> _workContactRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "tmsWorkContact";

        public TmsWorkContactCommandHandler(BaseRepository<TmsWorkContact> workContactRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._workContactRepo = workContactRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 05/11/2024
        /// </summary>
        public async Task HandleAsync(AddTmsWorkContactInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await this._workContactRepo.IsAnyAsync(wu => wu.IsDelete == null && wu.Type == command.Type && wu.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Name);

            await this._workContactRepo.InsertWithDiffLogAsync(command.Adapt<TmsWorkContact>());

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkContact); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsWorkContact sau khi thêm mới
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 05/11/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsWorkContactInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await this._workContactRepo.IsAnyAsync(wu => wu.IsDelete == null && wu.Type == command.Type && wu.Code == command.Code && wu.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Name);

            var wu = await this._workContactRepo.GetByIdAsync(command.ID);
            if (wu == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._workContactRepo.UpdateWithDiffLogAsync(command.Adapt(wu));

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkContact); //     ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsWorkContact sau khi cập nhật
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 05/11/2024
        /// </summary>
        public async Task HandleAsync(DeleteTmsWorkContactInput command)
        {
            var wu = await this._workContactRepo.GetByIdAsync(command.ID);
            if (wu == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._workContactRepo.SoftDeleteAsync<TmsWorkContact>(wu);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkContact); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsWorkContact sau khi xóa
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 05/11/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsWorkContactInput> command)
        {
            var ids = command.Select(item => item.ID).ToList();
            var wuList = await this._workContactRepo.GetListAsync(wu => ids.Contains(wu.ID));

            if (wuList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._workContactRepo.SoftDeleteAsync<TmsWorkContact>(wu => ids.Contains(wu.ID));

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkContact); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsWorkContact sau khi xóa nhiều record
        }
    }
}
