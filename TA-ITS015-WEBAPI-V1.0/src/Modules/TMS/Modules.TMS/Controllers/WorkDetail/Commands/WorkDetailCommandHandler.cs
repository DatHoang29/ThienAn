using Modules.TMS.Controllers.WorkDetail.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.WorkDetail.Commands
{
    /// <summary>
    /// Author: KhoaND
    /// Description: Lớp nghiệp vụ xử lý các lệnh vào của đối tượng TmsWorkDetail
    /// Created date: 11/08/2024
    /// </summary>
    public class WorkDetailCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsWorkDetail> _tmsWorkDetailRep;
        private BaseRepository<TmsTrafficPlanWorkDetails> _trafficPlanWorkDetailRep;
        private BaseRepository<TmsTrafficPlans> _trafficPlanRep;
        private BaseRepository<TmsWorkExecute> _workExecuteRep;
        private BaseRepository<TmsWorkExecuteHistory> _workExecuteHistoryRep;
        private BaseRepository<TmsWorkDetailContacts> _workDetailContactRep;
        private BaseCacheService _cache;
        private UserManager _userManager;

        public WorkDetailCommandHandler(
            BaseRepository<TmsWorkDetail> tmsWorkDetailRep,
            BaseRepository<TmsTrafficPlanWorkDetails> trafficPlanWorkDetailRep,
            BaseRepository<TmsTrafficPlans> trafficPlanRep,
            BaseRepository<TmsWorkExecute> workExecuteRep,
            BaseRepository<TmsWorkExecuteHistory> workExecuteHistoryRep,
            BaseRepository<TmsWorkDetailContacts> workDetailContactRep,
            BaseCacheService cache,
            UserManager userManager)
        {
            _tmsWorkDetailRep = tmsWorkDetailRep;
            _trafficPlanWorkDetailRep = trafficPlanWorkDetailRep;
            _trafficPlanRep = trafficPlanRep;
            _workExecuteRep = workExecuteRep;
            _workExecuteHistoryRep = workExecuteHistoryRep;
            _workDetailContactRep = workDetailContactRep;
            _cache = cache;
            _userManager = userManager;
        }
        /// <summary>
        /// Author: KhoaND SonTH
        /// Description: Hàm lưu thông tin xuống bảng TmsWorkDetail
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(AddWorkDetailInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 
            command.Name = command.Name?.Trim();

            if (string.IsNullOrWhiteSpace(command.Code))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            if (string.IsNullOrWhiteSpace(command.Name))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, Furion.Localization.L.Text[BaseMsg.BaseEntity.Name]);

            if (await _tmsWorkDetailRep.IsAnyAsync(u => u.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            command.WorkType = string.IsNullOrWhiteSpace(command.WorkType) ? null : command.WorkType.Trim();
            command.WorkMode = string.IsNullOrWhiteSpace(command.WorkMode) ? null : command.WorkMode.Trim();
            command.ControlType = (command.WorkType == "control" && !string.IsNullOrWhiteSpace(command.ControlType)) ? command.ControlType.Trim() : null;
            command.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();

            command.CreateUId = null;
            await _tmsWorkDetailRep.InsertWithDiffLogAsync(command.Adapt<TmsWorkDetail>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkDetail);
        }
        /// <summary>
        /// Author: KhoaND  SonTH
        /// Description: Hàm cập nhật thông tin xuống bảng TmsWorkDetail
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(UpdateWorkDetailInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu 
            command.Name = command.Name?.Trim();

            if (string.IsNullOrWhiteSpace(command.Code))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, Furion.Localization.L.Text[BaseMsg.BaseEntity.Code]);
            if (string.IsNullOrWhiteSpace(command.Name))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, Furion.Localization.L.Text[BaseMsg.BaseEntity.Name]);

            if (await _tmsWorkDetailRep.IsAnyAsync(u => u.Code == command.Code && u.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            var entity = await _tmsWorkDetailRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            entity.Code = command.Code;
            entity.Name = command.Name;
            entity.WorkType = string.IsNullOrWhiteSpace(command.WorkType) ? null : command.WorkType.Trim();
            entity.WorkMode = string.IsNullOrWhiteSpace(command.WorkMode) ? null : command.WorkMode.Trim();
            entity.ControlType = (entity.WorkType == "control" && !string.IsNullOrWhiteSpace(command.ControlType)) ? command.ControlType.Trim() : null;
            entity.Status = command.Status;
            entity.Remark = string.IsNullOrWhiteSpace(command.Remark) ? null : command.Remark.Trim();

            await _tmsWorkDetailRep.UpdateWithDiffLogAsync(entity, false);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkDetail);
        }
        /// <summary>
        /// Author: KhoaND
        /// Description: Hàm xóa 1 record trong bảng TmsWorkDetail
        /// Created date: 11/08/2024
        /// </summary>
        public async Task HandleAsync(DeleteWorkDetailInput command)
        {
            var entity = await _tmsWorkDetailRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            // Không cho xóa công việc đang được sử dụng trong Kịch bản hoặc Thực thi sự cố
            if (await IsWorkDetailInUseAsync(command.ID))
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse);

            await _tmsWorkDetailRep.SoftDeleteAsync<TmsWorkDetail>(entity);
            await _workDetailContactRep.DeleteAsync(u => u.WorkDetailId == entity.ID);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkDetail);
        }
        /// <summary>
        /// Author: KhoaND  SonTH
        /// Description: Hàm xóa nhiều record trong bảng TmsWorkDetail
        /// Created date: 11/08/2024 
        /// </summary>
        public async Task HandleAsync(List<DeleteWorkDetailInput> command)
        {
            var ids = command.Select(u => u.ID).ToList();

            var list = await _tmsWorkDetailRep.AsQueryable().Where(u => ids.Contains(u.ID)).ToListAsync();

            if (list?.Count != ids.Count)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            // Không cho xóa nếu có bất kỳ công việc nào đang được sử dụng
            if (await IsAnyWorkDetailInUseAsync(ids))
                throw Oops.Oh(BaseMsg.BaseException.DataIsInUse);

            await _tmsWorkDetailRep.SoftDeleteAsync<TmsWorkDetail>(l => ids.Contains(l.ID));
            await _workDetailContactRep.DeleteAsync(u => ids.Contains(u.WorkDetailId));
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsWorkDetail);
        }

        /// <summary>
        /// Kiểm tra công việc có đang được sử dụng trong Kịch bản hoặc Thực thi sự cố không
        /// </summary>
        private async Task<bool> IsWorkDetailInUseAsync(string workDetailId)
        {
            if (string.IsNullOrWhiteSpace(workDetailId)) return false;

            // 1. Kiểm tra kịch bản (traffic plan) đang còn hiệu lực/chưa xóa có chứa công việc này
            var inTrafficPlan = await _trafficPlanWorkDetailRep.AsQueryable()
                .InnerJoin<TmsTrafficPlans>((d, p) => d.TrafficPlanId == p.ID)
                .Where((d, p) => d.WorkDetailId == workDetailId)
                .AnyAsync();
            if (inTrafficPlan) return true;

            // 2. Kiểm tra công việc thực thi sự cố
            var inWorkExecute = await _workExecuteRep.AsQueryable()
                .Where(u => u.WorkDetailId == workDetailId)
                .AnyAsync();
            if (inWorkExecute) return true;

            // 3. Kiểm tra lịch sử thực thi sự cố
            var inWorkExecuteHistory = await _workExecuteHistoryRep.AsQueryable()
                .Where(u => u.WorkDetailId == workDetailId)
                .AnyAsync();
            if (inWorkExecuteHistory) return true;

            return false;
        }

        /// <summary>
        /// Kiểm tra xem có bất kỳ công việc nào trong danh sách đang được sử dụng không
        /// </summary>
        private async Task<bool> IsAnyWorkDetailInUseAsync(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return false;

            // 1. Kiểm tra kịch bản (traffic plan) đang còn hiệu lực/chưa xóa có chứa công việc này
            var inTrafficPlan = await _trafficPlanWorkDetailRep.AsQueryable()
                .InnerJoin<TmsTrafficPlans>((d, p) => d.TrafficPlanId == p.ID)
                .Where((d, p) => d.WorkDetailId != null && ids.Contains(d.WorkDetailId))
                .AnyAsync();
            if (inTrafficPlan) return true;

            // 2. Kiểm tra công việc thực thi sự cố
            var inWorkExecute = await _workExecuteRep.AsQueryable()
                .Where(u => u.WorkDetailId != null && ids.Contains(u.WorkDetailId))
                .AnyAsync();
            if (inWorkExecute) return true;

            // 3. Kiểm tra lịch sử thực thi sự cố
            var inWorkExecuteHistory = await _workExecuteHistoryRep.AsQueryable()
                .Where(u => u.WorkDetailId != null && ids.Contains(u.WorkDetailId))
                .AnyAsync();
            if (inWorkExecuteHistory) return true;

            return false;
        }
    }
}
