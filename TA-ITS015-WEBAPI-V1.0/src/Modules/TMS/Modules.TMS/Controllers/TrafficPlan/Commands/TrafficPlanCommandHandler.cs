using Modules.TMS.Controllers.TrafficPlan.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.TrafficPlan.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng TmsTrafficPlan
    /// Created date: 11/08/2024
    /// </summary>
    public class TrafficPlanCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsTrafficPlans> _tmsTrafficPlanRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public TrafficPlanCommandHandler(BaseRepository<TmsTrafficPlans> tmsTrafficPlanRep, BaseCacheService cache, UserManager userManager)
        {
            _tmsTrafficPlanRep = tmsTrafficPlanRep;
            _cache = cache;
            _userManager = userManager;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddTmsTrafficPlanInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await _tmsTrafficPlanRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);
            command.CreateUId = null;
            await _tmsTrafficPlanRep.InsertWithDiffLogAsync(command.Adapt<TmsTrafficPlans>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficPlan); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsTrafficPlan sau khi thêm mới
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng TmsTrafficPlan
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsTrafficPlanInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 

            if (await _tmsTrafficPlanRep.IsAnyAsync(u => u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            var entity = await _tmsTrafficPlanRep.GetByIdAsync(command.ID)
               ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    	throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _tmsTrafficPlanRep.UpdateWithDiffLogAsync(command.Adapt(entity));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficPlan); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsTrafficPlan sau khi cập nhật
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteTmsTrafficPlanInput command)
        {
            var sysPos = await _tmsTrafficPlanRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);


            await _tmsTrafficPlanRep.SoftDeleteAsync<TmsTrafficPlans>(sysPos);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficPlan); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsTrafficPlan sau khi xóa
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsTrafficPlanInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _tmsTrafficPlanRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);


            await _tmsTrafficPlanRep.SoftDeleteAsync<TmsTrafficPlans>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficPlan); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsTrafficPlan sau khi xóa nhiều record
        }
    }
}
