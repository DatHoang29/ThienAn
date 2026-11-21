using Modules.CfgSystem.Core.Refresh;
using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.TMS.Controllers.VehicleFilter.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.VehicleFilter.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 13/11/2024
    /// </summary>
    public class TmsVehicleFilterCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsVehicleFilter> _vehicleFilterRepo;
        private BaseCacheService _cache;
        private RefreshModifyService _refreshModify;

        public TmsVehicleFilterCommandHandler(BaseRepository<TmsVehicleFilter> vehicleFilterRepo, BaseCacheService cache,
            RefreshModifyService refreshModify)
        {
            this._cache = cache;
            this._vehicleFilterRepo = vehicleFilterRepo;
            this._refreshModify = refreshModify;
        }

        /// <summary>
        /// KienLT - 22/10/2026: báo service VDS nạp lại danh sách lọc xe ngay (route 'catalog:vehiclefilter' trong
        /// SysConfigData quyết định mã refresh). Trước đây không báo nên service chỉ cập nhật theo chu kỳ 5 phút.
        /// </summary>
        private Task NotifyAsync(string? key, string action)
            => this._refreshModify.NotifyAsync(new RefreshModifyRequest("catalog", "vehiclefilter", key, action));

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record
        /// Created date: 13/11/2024
        /// </summary>
        public async Task HandleAsync(AddTmsVehicleFilterInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await this._vehicleFilterRepo.IsAnyAsync(tvf => tvf.IsDelete == null && tvf.LicensePlate == command.LicensePlate))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.TmsVehicleFilter.Entity.LicensePlate);

            var entity = command.Adapt<TmsVehicleFilter>();
            await this._vehicleFilterRepo.InsertWithDiffLogAsync(entity);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleFilter); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleFilter sau khi thêm mới
            await NotifyAsync(entity.ID, "add");

        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 13/11/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsVehicleFilterInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await this._vehicleFilterRepo.IsAnyAsync(tvf => tvf.IsDelete == null && tvf.LicensePlate == command.LicensePlate && tvf.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.TmsVehicleFilter.Entity.LicensePlate);

            TmsVehicleFilter tvf = await this._vehicleFilterRepo.GetByIdAsync(command.ID);
            if (tvf == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._vehicleFilterRepo.UpdateWithDiffLogAsync(command.Adapt(tvf));

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleFilter); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleFilter sau khi cập nhật
            await NotifyAsync(tvf.ID, "update");
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 13/11/2024
        /// </summary>
        public async Task HandleAsync(DeleteTmsVehicleFilterInput command)
        {
            TmsVehicleFilter tvf = await this._vehicleFilterRepo.GetByIdAsync(command.ID);
            if (tvf == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._vehicleFilterRepo.SoftDeleteAsync<TmsVehicleFilter>(tvf);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleFilter); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleFilter sau khi xóa
            await NotifyAsync(tvf.ID, "delete");
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 13/11/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsVehicleFilterInput> command)
        {
            List<string> ids = command.Select(item => item.ID).ToList();
            List<TmsVehicleFilter> tvfList = await this._vehicleFilterRepo.GetListAsync(tvf => ids.Contains(tvf.ID));

            if (tvfList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._vehicleFilterRepo.SoftDeleteAsync<TmsVehicleFilter>(tvf => ids.Contains(tvf.ID));

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleFilter); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleFilter sau khi xóa
            await NotifyAsync(null, "batch-delete");
        }
    }
}
