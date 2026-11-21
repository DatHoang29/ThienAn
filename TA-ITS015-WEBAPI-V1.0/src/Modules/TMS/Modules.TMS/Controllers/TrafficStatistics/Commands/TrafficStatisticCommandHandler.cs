using Modules.TMS.Controllers.TrafficStatistics.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.TrafficStatistics.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng tmsTrafficStatistic
    /// Created date: 11/08/2024
    /// </summary>
    public class TrafficStatisticCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsTrafficStatistic> _tmsTrafficStatisticRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public TrafficStatisticCommandHandler(BaseRepository<TmsTrafficStatistic> tmsTrafficStatisticRep, BaseCacheService cache, UserManager userManager)
        {
            _tmsTrafficStatisticRep = tmsTrafficStatisticRep;
            _cache = cache;
            _userManager = userManager;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsTrafficStatistic
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddTmsTrafficStatisticInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu  

            if (await _tmsTrafficStatisticRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);
            command.CreateUId = null;
            await _tmsTrafficStatisticRep.InsertWithDiffLogAsync(command.Adapt<TmsTrafficStatistic>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficStatistic); // ChuongLP - 01/04/2024 - Xóa cache sau khi thêm mới
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng tmsTrafficStatistic
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsTrafficStatisticInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await _tmsTrafficStatisticRep.IsAnyAsync(u => u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var entity = await _tmsTrafficStatisticRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //if (!_userManager.SuperAdmin && entity.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(ErrorCodeEnum.D6002);

            await _tmsTrafficStatisticRep.UpdateWithDiffLogAsync(command.Adapt(entity));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficStatistic); // ChuongLP - 01/04/2024 - Xóa cache sau khi cập nhật
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsTrafficStatistic
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteTmsTrafficStatisticInput command)
        {
            var sysPos = await _tmsTrafficStatisticRep.GetFirstAsync(u => u.ID == command.ID)
                    ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            // throw Oops.Oh(ErrorCodeEnum.D6002);


            await _tmsTrafficStatisticRep.SoftDeleteAsync<TmsTrafficStatistic>(sysPos);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficStatistic); // ChuongLP - 01/04/2024 - Xóa cache sau khi xóa
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsTrafficStatistic
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsTrafficStatisticInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsTrafficStatisticRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            await _tmsTrafficStatisticRep.SoftDeleteAsync<TmsTrafficStatistic>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficStatistic); // ChuongLP - 01/04/2024 - Xóa cache sau khi xóa nhiều bản ghi
        }
    }
}
