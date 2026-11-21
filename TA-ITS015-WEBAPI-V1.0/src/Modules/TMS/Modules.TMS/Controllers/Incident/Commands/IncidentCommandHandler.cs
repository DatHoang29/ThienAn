using Modules.TMS.Infrastructure.Services.Incident.Configuration;
using FluentValidation;
using Modules.TMS.Infrastructure.Services.Incident.Mapping;
using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Modules.TMS.Infrastructure.Services.Incident.Classification;
using Modules.TMS.Infrastructure.Services.Incident.Messaging;
using Microsoft.Extensions.Logging;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.Incident;
using Modules.TMS.Infrastructure.Services.IncidentAutomation;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Incident.Commands
{
    /// <summary>
    /// Author: ThangNN
    /// Description: Handler xử lý CRUD cơ bản cho sự cố (Add / Update / Delete).
    /// Note: Logic chuyển trạng thái (workflow) nằm ở IncidentWorkflowCommandHandler.
    /// Created date: 19/08/2024
    /// </summary>
    public class IncidentCommandHandler : IWolverineHandler
    {
        private BaseRepository<TmsIncident> _incidentRepo;
        private readonly BaseRepository<TmsEventType> _eventTypes;
        private BaseCacheService _cache;
        private IncidentBroadcaster _broadcaster;
        private TmsIncidentAutomationService _automationService;
        private IncidentSnapshotQueue _snapshotQueue;
        private ILogger<IncidentCommandHandler> _logger;

        private IncidentGroupResolver _groupResolver;

        public IncidentCommandHandler(BaseRepository<TmsIncident> incidentRepo, BaseCacheService cache,
            IncidentBroadcaster broadcaster, TmsIncidentAutomationService automationService,
            IncidentSnapshotQueue snapshotQueue,
            ILogger<IncidentCommandHandler> logger, BaseRepository<TmsEventType> eventTypes, IncidentGroupResolver groupResolver)
        {
            this._incidentRepo = incidentRepo;
            _cache = cache;
            _broadcaster = broadcaster;
            _automationService = automationService;
            _snapshotQueue = snapshotQueue;
            _logger = logger;
            _eventTypes = eventTypes;
            _groupResolver = groupResolver;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Thêm mới một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<bool> HandleAsync(AddTmsIncidentInput command)
        {
            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã sự cố trước khi kiểm tra và lưu vào cơ sở dữ liệu

            if (await this._incidentRepo.IsAnyAsync(ti => ti.IsDelete == null && ti.Code == command.Code))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.CodeOrName);

            command.State = ((int)TmsIncidentStateEnum.Registration).ToString();
            command.StartDate = command.StartDate ?? Shared.Utility.Utilities.BaseUtil.TimeServer.GetTimeServer();
            command.CreatedBy = App.User.FindFirst(ClaimConst.Account)?.Value;
            command.CreatedDate = Shared.Utility.Utilities.BaseUtil.TimeServer.GetTimeServer();

            var incident = command.Adapt<TmsIncident>();
            var type = await _eventTypes.GetFirstAsync(x => x.ID == incident.EventTypeId && x.IsDelete == null);
            incident.EventGroup = IncidentEventGroupConst.Normalize(type?.Type)
                ?? throw Oops.Oh(BaseMsg.TmsIncident.Exception.EventGroupNotConfigured);
            if (incident.EventGroup == IncidentEventGroupConst.Event)
            {
                IncidentRecordingConst.ApplyToNewEvent(incident);
            }
            new Modules.TMS.Core.Validators.IncidentEntityValidator().ValidateAndThrow(incident);
            var insertedCount = await this._incidentRepo.InsertWithDiffLogAsync(incident);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncident); // ChuongLP: Xóa cache sau khi thêm mới dữ liệu

            // Thông báo cho frontend biết có một incident mới có trạng thái Registration
            if (insertedCount > 0)
            {
                // Author: SonTH - 24/07/2026 - Thử lập lịch automation nếu EventType này bật trigger Registration.
                // No-op nếu chưa có cấu hình tự động cho EventType (xem TryScheduleAsync).
                // Bọc try/catch: tự động hóa là chức năng phụ trợ, lỗi ở đây KHÔNG được làm hỏng việc tạo sự cố.
                //
                // PHẢI lập lịch TRƯỚC khi Notify: frontend nhận broadcast sẽ gọi ngay API lấy danh sách run
                // đang chờ. Nếu Notify trước, run chưa tồn tại lúc frontend truy vấn nên banner rỗng
                // (đúng nguyên nhân "phải F5 mới thấy").
                try
                {
                    await this._automationService.TryScheduleAsync(incident.ID, IncidentAutomationConst.TriggerState.Registration);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[IncidentCommandHandler] Lập lịch tự động xử lý sự cố {IncidentId} thất bại.", incident.ID);
                }

                _snapshotQueue.Enqueue(
                    incident.ID,
                    (int)TmsIncidentStateEnum.Registration);

                await _broadcaster.NotifyAsync(incident, (int)TmsIncidentStateEnum.Registration);
            }

            return insertedCount > 0;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Cập nhật một record
        /// Note: Không thực hiện update trạng thái state của incident
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<bool> HandleAsync(UpdateTmsIncidentInput command)
        {
            // TODO: Check authorization

            command.Code = command.Code?.Trim(); // ChuongLP - 02/04/2025 - Loại bỏ khoảng trắng ở đầu và cuối mã sự cố trước khi kiểm tra và lưu vào cơ sở dữ liệu

            if (await this._incidentRepo.IsAnyAsync(ti => ti.IsDelete == null && ti.Code == command.Code && ti.ID != command.ID))
                throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

            var incident = await this._incidentRepo.GetByIdAsync(command.ID)
                            ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            if (command.EventTypeId != null && !string.Equals(command.EventTypeId, incident.EventTypeId, StringComparison.Ordinal))
                throw Oops.Oh(BaseMsg.TmsIncident.Exception.EventTypeLocked);

            await this._groupResolver.PopulateGroupsAsync(new[] { incident });
            var createdDate = incident.CreatedDate;
            var createTime = incident.CreateTime;
            var group = incident.EventGroup;
            var originalState = incident.State;
            var originalType = incident.EventTypeId;
            IncidentUpdateMapping.Apply(command, incident);
            incident.CreatedDate = createdDate;
            incident.CreateTime = createTime;
            incident.EventGroup = group;
            incident.State = originalState;
            incident.EventTypeId = originalType;
            var updatedCount = await this._incidentRepo.UpdateWithDiffLogAsync(incident);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncident); // ChuongLP: Xóa cache sau khi cập nhật dữ liệu

            // Thông báo cho frontend biết có một incident mới có trạng thái Updated
            if (updatedCount > 0)
                await _broadcaster.NotifyAsync(incident, (int)TmsIncidentStateEnum.Updated);

            return updatedCount > 0;
        }

        /// <summary>
        /// Author: ThangNN
        /// Description: Xóa một record
        /// Created date: 09/08/2024
        /// </summary>
        public async Task<bool> HandleAsync(DeleteTmsIncidentInput command)
        {
            // TODO: Check authorization

            var incident = await this._incidentRepo.GetByIdAsync(command.ID);
            if (incident == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist);

            int deletedCount = await this._incidentRepo.SoftDeleteAsync<TmsIncident>(incident);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncident); //ChuongLP: Xóa cache sau khi xóa dữ liệu

            // Thông báo cho frontend biết có một incident đã được cập nhật trạng thái Deleted
            if (deletedCount > 0)
            {
                await _broadcaster.NotifyAsync(incident, (int)TmsIncidentStateEnum.Deleted);

                // Author: SonTH - 24/07/2026 - Incident bị xóa: hủy mọi automation run chưa kết thúc.
                try
                {
                    await this._automationService.CancelActiveRunsByIncidentAsync(incident.ID, "IncidentDeleted");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[IncidentCommandHandler] Hủy automation run của sự cố {IncidentId} thất bại.", incident.ID);
                }
            }

            return deletedCount > 0;
        }
    }
}
