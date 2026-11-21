using Modules.TMS.Controllers.VehicleFilterHistory.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using Shared.Utility.Utilities;

namespace Modules.TMS.Controllers.VehicleFilterHistory.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 13/11/2024
    /// </summary>
    public class TmsVehicleFilterHistoryCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsVehicleFilterHistory> _vehicleFilterHistoryRepo;
        private BaseCacheService _cache;
        private const string _cacheName = "tmsVehicleFilterHistory";
        private const int DuplicateWindowSeconds = 60;
        private const int DatabaseLockTimeoutMs = 10_000;

        public TmsVehicleFilterHistoryCommandHandler(BaseRepository<TmsVehicleFilterHistory> vehicleFilterHistoryRepo, BaseCacheService cache)
        {
            this._cache = cache;
            this._vehicleFilterHistoryRepo = vehicleFilterHistoryRepo;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm một record
        /// Created date: 13/11/2024
        /// </summary>
        public async Task<bool> HandleAsync(AddTmsVehicleFilterHistoryInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu
            command.LicensePlate = command.LicensePlate?.Trim().ToUpperInvariant();

            var normalizedPlate = NormalizeLicensePlate(command.LicensePlate);
            if (string.IsNullOrEmpty(normalizedPlate)) return false;

            var db = this._vehicleFilterHistoryRepo.Context;
            await db.AsTenant().BeginTranAsync();

            try
            {
                // SQL Server application lock đồng bộ theo biển số trên toàn bộ browser và cả nhiều instance BE.
                // Lock thuộc transaction nên luôn tự nhả khi commit/rollback.
                var lockResult = await db.Ado.SqlQuerySingleAsync<int>(
                    @"DECLARE @Result int;
                      EXEC @Result = sys.sp_getapplock
                          @Resource = @Resource,
                          @LockMode = 'Exclusive',
                          @LockOwner = 'Transaction',
                          @LockTimeout = @LockTimeout;
                      SELECT @Result;",
                    new
                    {
                        Resource = $"TmsVehicleFilterHistory:{normalizedPlate}",
                        LockTimeout = DatabaseLockTimeoutMs
                    });

                if (lockResult < 0)
                    throw new InvalidOperationException($"Cannot acquire vehicle filter history lock. Result: {lockResult}.");

                var serverNow = BaseUtil.TimeServer.GetTimeServer();
                var duplicateFrom = serverNow.AddSeconds(-DuplicateWindowSeconds);
                var isDuplicate = await this._vehicleFilterHistoryRepo.IsAnyAsync(tvr =>
                    tvr.IsDelete == null &&
                    tvr.CreateTime >= duplicateFrom &&
                    tvr.LicensePlate != null &&
                    tvr.LicensePlate.Replace(" ", string.Empty)
                        .Replace("-", string.Empty)
                        .Replace(".", string.Empty)
                        .Replace("_", string.Empty)
                        .ToUpper() == normalizedPlate);

                // Duplicate là kết quả hợp lệ: trả 200/void để các client khác không hiện lỗi.
                if (isDuplicate)
                {
                    await db.AsTenant().CommitTranAsync();
                    return false;
                }

                command.CreateTime = serverNow;
                // DetectTime theo hợp đồng là UTC: worker (VehicleFilterNotifier.BuildHistory) ghi thẳng
                // VdsTrafficData.DetectTime vốn là UTC, và API đọc trả ra kèm hậu tố Z. Gán serverNow vào đây
                // như trước 25/09/2026 làm dòng do web thêm lệch đúng số giờ của múi giờ máy chủ so với dòng
                // do worker thêm — cùng một cột mang hai nghĩa.
                // ToUtc chuẩn hoá Kind cho giá trị client gửi lên: Local thì quy về UTC, Unspecified thì hiểu
                // là UTC theo đúng hợp đồng đầu vào của FE.
                command.DetectTime = BaseUtil.TimeServer.ToUtc(command.DetectTime) ?? DateTime.UtcNow;
                await this._vehicleFilterHistoryRepo.InsertWithDiffLogAsync(command.Adapt<TmsVehicleFilterHistory>());
                await db.AsTenant().CommitTranAsync();
            }
            catch
            {
                await db.AsTenant().RollbackTranAsync();
                throw;
            }

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleFilterHistory); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleFilter sau khi thêm mới
            return true;
        }

        private static string NormalizeLicensePlate(string? licensePlate)
        {
            return (licensePlate ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(".", string.Empty)
                .Replace("_", string.Empty)
                .ToUpperInvariant();
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Created date: 13/11/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsVehicleFilterHistoryInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await this._vehicleFilterHistoryRepo.IsAnyAsync(tvr => tvr.IsDelete == null && tvr.LicensePlate == command.LicensePlate && tvr.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.TmsVehicleRegistration.Entity.LicensePlate);

            var tvr = await this._vehicleFilterHistoryRepo.GetByIdAsync(command.ID);
            if (tvr == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._vehicleFilterHistoryRepo.UpdateWithDiffLogAsync(command.Adapt(tvr));

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleFilterHistory); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleFilter sau khi cập nhật
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 13/11/2024
        /// </summary>
        public async Task HandleAsync(DeleteTmsVehicleFilterHistoryInput command)
        {
            var tvr = await this._vehicleFilterHistoryRepo.GetByIdAsync(command.ID);
            if (tvr == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await this._vehicleFilterHistoryRepo.SoftDeleteAsync<TmsVehicleFilterHistory>(tvr);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleFilterHistory);  //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleFilter sau khi xóa
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa nhiều record
        /// Created date: 13/11/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsVehicleFilterHistoryInput> command)
        {
            List<string> ids = command.Select(item => item.ID).ToList();
            List<TmsVehicleFilterHistory> tvrList = await this._vehicleFilterHistoryRepo.GetListAsync(tvr => ids.Contains(tvr.ID));

            if (tvrList?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await this._vehicleFilterHistoryRepo.SoftDeleteAsync<TmsVehicleFilterHistory>(tvr => ids.Contains(tvr.ID));

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsVehicleFilterHistory); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsVehicleFilter sau khi xóa
        }
    }
}
