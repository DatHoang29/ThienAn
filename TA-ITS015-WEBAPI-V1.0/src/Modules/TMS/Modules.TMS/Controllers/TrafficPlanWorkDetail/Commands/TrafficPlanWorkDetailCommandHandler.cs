using Modules.TMS.Controllers.TrafficPlanWorkDetail.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.TrafficPlanWorkDetail;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.TrafficPlanWorkDetail.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng TmsTrafficPlanWorkDetail
    /// Created date: 11/08/2024
    /// </summary>
    public class TrafficPlanWorkDetailCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsTrafficPlanWorkDetails> _TmsTrafficPlanWorkDetailRep;
        private BaseCacheService _cache;
        private UserManager _userManager;
        private TrafficPlanWorkDetailService _trafficPlanWorkDetailService;
        public TrafficPlanWorkDetailCommandHandler(BaseRepository<TmsTrafficPlanWorkDetails> TmsTrafficPlanWorkDetailRep, TrafficPlanWorkDetailService trafficPlanWorkDetailService,
            BaseCacheService cache, UserManager userManager)
        {
            _TmsTrafficPlanWorkDetailRep = TmsTrafficPlanWorkDetailRep;
            _cache = cache;
            _userManager = userManager;
            _trafficPlanWorkDetailService = trafficPlanWorkDetailService;
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm lưu thông tin xuống bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddTmsTrafficPlanWorkDetailInput command)
        {
            // Kiểm tra nếu tồn tại bản ghi với ExecuteOrgId và WorkDetailId
            var isExist = await _TmsTrafficPlanWorkDetailRep.IsAnyAsync(u =>
                u.ExecuteOrgId == command.ExecuteOrgId &&
                u.WorkDetailId == command.WorkDetailId &&
                u.TrafficPlanId == command.TrafficPlanId

            );

            // Nếu tồn tại, ném ngoại lệ và không cho phép thêm
            if (isExist)
            {
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.TmsWorkDetail.Entity.WorkName);
            }

            // Nếu không tồn tại, thực hiện thêm bản ghi mới
            command.CreateUId = null; // Gán giá trị mặc định cho CreateUId
            await _TmsTrafficPlanWorkDetailRep.InsertWithDiffLogAsync(command.Adapt<TmsTrafficPlanWorkDetails>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficPlanWorkDetail); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsTrafficPlanWorkDetail sau khi thêm mới
        }

        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm cập nhật thông tin xuống bảng TmsTrafficPlanWorkDetail
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateTmsTrafficPlanWorkDetailInput command)
        {
            //if (await _TmsTrafficPlanWorkDetailRep.IsAnyAsync(u => u.ExecuteOrgId == command.ExecuteOrgId && u.WorkDetailId == command.WorkDetailId))
            //    throw Oops.Oh(BaseMsg.BaseException.Exist);

            var entity = await _TmsTrafficPlanWorkDetailRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);
            //if (!_userManager.SuperAdmin && sysPos.CreateUId != _userManager.UserId)
            //    	throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _TmsTrafficPlanWorkDetailRep.UpdateWithDiffLogAsync(command.Adapt(entity));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficPlanWorkDetail); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsTrafficPlanWorkDetail sau khi cập nhật
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>

        public async Task HandleAsync(DeleteTmsTrafficPlanWorkDetailInput command)
        {
            var TmsTrafficPlanWorkDetails = await _TmsTrafficPlanWorkDetailRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            //if (!_userManager.SuperAdmin && TmsTrafficPlanWorkDetails.CreateUId != _userManager.UserId)
            //    throw Oops.Oh(ErrorCodeEnum.D6002);

            await _trafficPlanWorkDetailService.deleteTrafficPlanWorkDetail(TmsTrafficPlanWorkDetails);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficPlanWorkDetail); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsTrafficPlanWorkDetail sau khi xóa

        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa nhiều record trong bảng tmsModule
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(List<DeleteTmsTrafficPlanWorkDetailInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = _TmsTrafficPlanWorkDetailRep.AsQueryable().Where(u => ids.Contains(u.ID));

            if (list?.Count() != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);


            await _TmsTrafficPlanWorkDetailRep.SoftDeleteAsync<TmsTrafficPlanWorkDetails>(l => ids.Contains(l.ID));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsTrafficPlanWorkDetail); //ChuongLP - 01/04/2026 Xóa cache liên quan đến TmsTrafficPlanWorkDetail sau khi xóa
        }
    }
}
