using Modules.TMS.Controllers.VehicleRegistration.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.VehicleRegistration.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 17/12/2024
    /// </summary>
    public class TmsVehicleRegistrationCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsVehicleRegistration> _vehicleRegistrationRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "tmsVehicleRegistration";

        public TmsVehicleRegistrationCommandHandler(BaseRepository<TmsVehicleRegistration> vehicleRegistrationRepo, BaseCacheService cache, UserManager userManager)
        {
            this._cache = cache;
            this._vehicleRegistrationRepo = vehicleRegistrationRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 17/12/2024
        /// </summary>
        public async Task HandleAsync(AddTmsVehicleRegistrationInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2026 - Loại bỏ khoảng trắng ở đầu 
            command.LicensePlate = NormalizeLicensePlate(command.LicensePlate); // // ChuongLP - 29/07/2026 <-- thêm

            if (command.DateRegistrations.HasValue && command.ExpirationDate.HasValue && command.DateRegistrations.Value > command.ExpirationDate.Value)
                throw Oops.Oh(BaseMsg.TmsVehicleRegistration.Validation.DateRegistrationsExceedsExpiration);

            if (await this._vehicleRegistrationRepo.IsAnyAsync(tvr => tvr.IsDelete == null && tvr.LicensePlate == command.LicensePlate))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.TmsVehicleRegistration.Entity.LicensePlate);

            await this._vehicleRegistrationRepo.InsertWithDiffLogAsync(command.Adapt<TmsVehicleRegistration>());

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleRegistration); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleRegistration sau khi thêm mới
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 17/12/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsVehicleRegistrationInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 
            command.LicensePlate = NormalizeLicensePlate(command.LicensePlate); // // ChuongLP - 29/07/2026 <-- thêm

            if (command.DateRegistrations.HasValue && command.ExpirationDate.HasValue && command.DateRegistrations.Value > command.ExpirationDate.Value)
                throw Oops.Oh(BaseMsg.TmsVehicleRegistration.Validation.DateRegistrationsExceedsExpiration);

            if (await this._vehicleRegistrationRepo.IsAnyAsync(tvr => tvr.IsDelete == null && tvr.LicensePlate == command.LicensePlate && tvr.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.TmsVehicleRegistration.Entity.LicensePlate);

            var tvr = await this._vehicleRegistrationRepo.GetByIdAsync(command.ID);
            if (tvr == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            tvr = command.Adapt(tvr);
            tvr.DateRegistrations = command.DateRegistrations;
            tvr.ExpirationDate = command.ExpirationDate;

            await this._vehicleRegistrationRepo.UpdateWithDiffLogAsync(tvr, false);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleRegistration); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleRegistration sau khi cập nhật
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 17/12/2024
        /// </summary>
        public async Task HandleAsync(DeleteTmsVehicleRegistrationInput command)
        {
            var tvr = await this._vehicleRegistrationRepo.GetByIdAsync(command.ID);
            if (tvr == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._vehicleRegistrationRepo.SoftDeleteAsync<TmsVehicleRegistration>(tvr);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleRegistration); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleRegistration sau khi xóa
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 17/12/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsVehicleRegistrationInput> command)
        {
            List<string> ids = command.Select(item => item.ID).ToList();
            List<TmsVehicleRegistration> tvrList = await this._vehicleRegistrationRepo.GetListAsync(tvr => ids.Contains(tvr.ID));

            if (tvrList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._vehicleRegistrationRepo.SoftDeleteAsync<TmsVehicleRegistration>(tvr => ids.Contains(tvr.ID));

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleRegistration); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleRegistration sau khi xóa
        }


        /// <summary>
        /// Chuẩn hóa biển số: bỏ các ký tự phân tách (-, _, ., khoảng trắng),
        /// chỉ giữ chữ và số, đưa về chữ hoa để tránh sai khác a/A.
        /// </summary>
        private static string? NormalizeLicensePlate(string? licensePlate)
        {
            if (string.IsNullOrWhiteSpace(licensePlate))
                return licensePlate;

            return licensePlate
                .Replace("-", "")
                .Replace("_", "")
                .Replace(".", "")
                .Replace(" ", "")
                .Trim()
                .ToUpperInvariant();
        }

    }
}
