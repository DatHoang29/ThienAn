using Modules.TMS.Controllers.SignalLogs.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Dtos.Tms;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.SignalLogs.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng TmsSignalLog
    /// Created date: 11/08/2024
    /// </summary>
    public class SignalLogCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsSignalLog> _TmsSignalLogRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public SignalLogCommandHandler(BaseRepository<TmsSignalLog> TmsSignalLogRep, BaseCacheService cache, UserManager userManager)
        {
            _TmsSignalLogRep = TmsSignalLogRep;
            _cache = cache;
            _userManager = userManager;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng TmsSignalLog
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddTmsSignalLogInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu

            //if (await _TmsSignalLogRep.IsAnyAsync(u => u.Name == command.Name && u.Code == command.Code))
            //    throw Oops.Oh(ErrorCodeEnum.D6000);
            command.CreateUId = null;
            await _TmsSignalLogRep.InsertAsync(command.Adapt<TmsSignalLog>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsSignalLog); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsSignalLog sau khi thêm mới
        }

        /// <summary>
        /// Author: ChuongLP
        /// Description: Ghi TmsSignalLog từ module khác (VMS) IN-PROCESS qua MessBus,
        ///              thay cho HTTP self-call tới ApiGateway:BaseUrl (lỗi SSL khi BaseUrl là https theo IP).
        ///              Dùng lại đúng luồng Add để giữ nguyên hành vi (trim Code, CreateUId, xóa cache).
        /// Created date: 23/10/2026
        /// </summary>
        public async Task HandleAsync(TmsSignalLogAddRequest command)
        {
            var input = command.Adapt<AddTmsSignalLogInput>();
            input.EquipmentId = command.EquipmentID; // Khác hoa/thường (EquipmentID vs EquipmentId) -> gán tay cho chắc
            await HandleAsync(input);
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng TmsSignalLog
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsSignalLogInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu  
            if (await _TmsSignalLogRep.IsAnyAsync(u => u.EquipmentId == command.EquipmentId && u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var entity = await _TmsSignalLogRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(ErrorCodeEnum.D6002);

            await _TmsSignalLogRep.UpdateAsync(command.Adapt(entity));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsSignalLog); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsSignalLog sau khi cập nhật
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng TmsSignalLog
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteTmsSignalLogInput command)
        {
            var entity = await _TmsSignalLogRep.GetFirstAsync(u => u.ID == command.ID)
                 ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    	throw Oops.Oh(BaseMsg.BaseException.DataNotExist);


            await _TmsSignalLogRep.SoftDeleteAsync<TmsSignalLog>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsSignalLog); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsSignalLog sau khi xóa
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng TmsSignalLog
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsSignalLogInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _TmsSignalLogRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);


            await _TmsSignalLogRep.SoftDeleteAsync<TmsSignalLog>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsSignalLog); // ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsSignalLog sau khi xóa nhiều record
        }
    }
}
