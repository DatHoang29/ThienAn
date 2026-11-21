using Modules.CfgSystem.Core.Refresh;
using Modules.CfgSystem.Infrastructure.Services.Refresh;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.IncidentAutomationConfig;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;

namespace Modules.TMS.Controllers.IncidentAutomationConfig.Commands
{
    /// <summary>
    /// Author: SonTH
    /// Description: Lớp nghiệp vụ xử lý lệnh ghi cho cấu hình tự động xử lý sự kiện/sự cố (TmsIncidentAutomationConfig)
    /// Created date: 24/07/2026
    /// </summary>
    public class IncidentAutomationConfigCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsIncidentAutomationConfig> _configRep;
        private BaseRepository<TmsEventType> _eventTypeRep;
        private BaseRepository<TmsTrafficPlans> _trafficPlanRep;
        private BaseCacheService _cache;
        private RefreshModifyService _refreshModify;

        public IncidentAutomationConfigCommandHandler(
            BaseRepository<TmsIncidentAutomationConfig> configRep,
            BaseRepository<TmsEventType> eventTypeRep,
            BaseRepository<TmsTrafficPlans> trafficPlanRep,
            BaseCacheService cache,
            RefreshModifyService refreshModify)
        {
            _configRep = configRep;
            _eventTypeRep = eventTypeRep;
            _trafficPlanRep = trafficPlanRep;
            _cache = cache;
            _refreshModify = refreshModify;
        }

        /// <summary>
        /// Thêm mới cấu hình. Chống trùng theo EventTypeId (một EventType chỉ có một cấu hình active).
        /// Author: SonTH
        /// Date: 2026-07-24
        /// </summary>
        public async Task HandleAsync(AddIncidentAutomationConfigInput command)
        {
            await ValidateBusinessAsync(command, null);

            command.ConfigVersion = 1;
            command.UnclaimedAutoFinishFromUtc = command.IsUnclaimedAutoFinishEnabled ? DateTime.UtcNow : null;
            command.CreateUId = null;
            await _configRep.InsertWithDiffLogAsync(command.Adapt<TmsIncidentAutomationConfig>());
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentAutomationConfig);
            await NotifyVdsAsync(command.EventTypeId, "add");
        }

        /// <summary>
        /// Cập nhật cấu hình. Chống trùng theo EventTypeId, loại trừ chính nó.
        /// Tăng ConfigVersion mỗi khi cập nhật để automation run snapshot đúng phiên bản đã dùng.
        /// Author: SonTH
        /// Date: 2026-07-24
        /// </summary>
        public async Task HandleAsync(UpdateIncidentAutomationConfigInput command)
        {
            var entity = await _configRep.GetByIdAsync(command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await ValidateBusinessAsync(command, command.ID);

            // The client cannot backdate activation and accidentally close operational history.
            if (command.IsUnclaimedAutoFinishEnabled && (!entity.IsUnclaimedAutoFinishEnabled
                || entity.EventTypeId != command.EventTypeId
                || (entity.Status != BaseEnums.StatusEnum.Enable && command.Status == BaseEnums.StatusEnum.Enable)))
                entity.UnclaimedAutoFinishFromUtc = DateTime.UtcNow;
            if (!command.IsUnclaimedAutoFinishEnabled) entity.UnclaimedAutoFinishFromUtc = null;
            entity.IsUnclaimedAutoFinishEnabled = command.IsUnclaimedAutoFinishEnabled;
            entity.UnclaimedAutoFinishAfterSeconds = command.UnclaimedAutoFinishAfterSeconds;

            entity.EventTypeId = command.EventTypeId;
            entity.IsAutoExecuteEnabled = command.IsAutoExecuteEnabled;
            entity.AllowedSources = command.AllowedSources;
            entity.TriggerState = command.TriggerState;
            entity.AutoVerifyIncident = command.AutoVerifyIncident;
            entity.WarningDelaySeconds = command.WarningDelaySeconds;
            entity.TrafficPlanId = command.TrafficPlanId;
            entity.IsAutoFinishEnabled = command.IsAutoFinishEnabled;
            entity.AutoFinishAfterSeconds = command.AutoFinishAfterSeconds;
            entity.FailurePolicy = command.FailurePolicy;
            entity.ResetPolicy = command.ResetPolicy;
            entity.Status = command.Status;
            entity.Remark = command.Remark;
            entity.ConfigVersion += 1;

            await _configRep.UpdateWithDiffLogAsync(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentAutomationConfig);
            await NotifyVdsAsync(entity.ID, "update");
        }

        /// <summary>
        /// Xóa cấu hình tự động (soft-delete, đồng bộ theo convention EntityTenant).
        /// Author: SonTH
        /// Date: 2026-07-24
        /// </summary>
        public async Task HandleAsync(DeleteIncidentAutomationConfigInput command)
        {
            var entity = await _configRep.GetFirstAsync(u => u.ID == command.ID)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            await _configRep.SoftDeleteAsync<TmsIncidentAutomationConfig>(entity);
            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncidentAutomationConfig);
            await NotifyVdsAsync(entity.ID, "delete");
        }

        /// <summary>
        /// Author: KienLT
        /// Description: VDS service suy danh sách loại sự kiện cần gửi sang TMS (lập lịch tự động / tự kết thúc) từ
        ///              chính bảng cấu hình này, nên lưu xong phải báo VDS nạp lại. Mã refresh do route
        ///              'catalog:automationconfig' trong SysConfigData quyết định (2 = VdsConfig, có nạp OpConfig);
        ///              thiếu route thì VDS vẫn nhận ở lượt nạp định kỳ.
        /// Created date: 17/10/2026
        /// </summary>
        private Task NotifyVdsAsync(string? key, string action)
            => _refreshModify.NotifyAsync(new RefreshModifyRequest("catalog", "automationconfig", key, action));

        /// <summary>
        /// Validate nghiệp vụ chung cho Add/Update: EventType tồn tại + enable, unique theo EventTypeId,
        /// TrafficPlan (nếu có) cùng EventTypeId + Enable + ExecutionMode Automatic/Both,
        /// TriggerState hợp lệ, AutoVerifyIncident bắt buộc nếu trigger từ Registration,
        /// AutoFinishAfterSeconds bắt buộc > 0 nếu bật auto-finish.
        /// Author: SonTH
        /// Date: 2026-07-24
        /// </summary>
        private async Task ValidateBusinessAsync(TmsIncidentAutomationConfig command, string? excludeId)
        {
            if (await _configRep.IsAnyAsync(u => u.EventTypeId == command.EventTypeId && (excludeId == null || u.ID != excludeId)))
                throw Oops.Oh(BaseMsg.TmsIncidentAutomationConfig.Exception.Duplicate);

            var eventType = await _eventTypeRep.GetFirstAsync(u => u.ID == command.EventTypeId && u.IsDelete == null);
            if (eventType == null || eventType.Status != BaseEnums.StatusEnum.Enable)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist, BaseMsg.TmsIncidentAutomationConfig.Entity.EventTypeId);

            if (command.IsUnclaimedAutoFinishEnabled)
            {
                if (IncidentEventGroupConst.Normalize(eventType.Type) is not (IncidentEventGroupConst.Event or IncidentEventGroupConst.Incident))
                    throw Oops.Oh(BaseMsg.TmsIncidentAutomationConfig.Exception.UnclaimedFinishGroupInvalid);
                if (command.IsAutoExecuteEnabled || !string.IsNullOrWhiteSpace(command.TrafficPlanId))
                    throw Oops.Oh(BaseMsg.TmsIncidentAutomationConfig.Exception.UnclaimedFinishPlanConflict);
                if (command.UnclaimedAutoFinishAfterSeconds is not (> 0 and <= 604800))
                    throw Oops.Oh(BaseMsg.TmsIncidentAutomationConfig.Exception.UnclaimedFinishSecondsRange, 1, 604800);
                if (!IncidentAutomationSourceConst.IsAllowed(command.AllowedSources, IncidentAutomationSourceConst.Vds))
                    throw Oops.Oh(BaseMsg.TmsIncidentAutomationConfig.Exception.UnclaimedFinishSourceRequiresVds);
            }

            if (!string.IsNullOrWhiteSpace(command.TrafficPlanId))
            {
                var plan = await _trafficPlanRep.GetFirstAsync(u => u.ID == command.TrafficPlanId && u.IsDelete == null);
                var isValidPlan = plan != null
                    && plan.Status == BaseEnums.StatusEnum.Enable
                    && plan.EventTypeId == command.EventTypeId
                    && (plan.ExecutionMode == TrafficPlanConst.ExecutionMode.Automatic || plan.ExecutionMode == TrafficPlanConst.ExecutionMode.Both);

                if (!isValidPlan)
                    throw Oops.Oh(BaseMsg.TmsIncidentAutomationConfig.Exception.TrafficPlanInvalid);
            }

            var isRegistrationTrigger = command.TriggerState == IncidentAutomationConst.TriggerState.Registration;
            if (!command.IsUnclaimedAutoFinishEnabled && isRegistrationTrigger && !command.AutoVerifyIncident)
                throw Oops.Oh(BaseMsg.TmsIncidentAutomationConfig.Validation.AutoVerifyRequiredForRegistrationTrigger);

            if (command.IsAutoFinishEnabled && (command.AutoFinishAfterSeconds == null || command.AutoFinishAfterSeconds <= 0))
                throw Oops.Oh(BaseMsg.TmsIncidentAutomationConfig.Validation.AutoFinishAfterSecondsRequired);
        }
    }
}
