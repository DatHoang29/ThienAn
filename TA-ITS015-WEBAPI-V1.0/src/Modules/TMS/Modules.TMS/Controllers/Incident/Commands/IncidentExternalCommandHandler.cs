using Modules.TMS.Infrastructure.Services.Incident.Configuration;
using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Modules.TMS.Infrastructure.Services.Incident.Messaging;
using Microsoft.Extensions.Logging;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Exceptions;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.Incident;
using Modules.TMS.Infrastructure.Services.IncidentAutomation;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Services;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Incident.Commands
{
    /// <summary>
    /// Author: SonTH
    /// Description: Handler cho HỆ THỐNG NGOÀI tạo sự cố (VDS, báo cháy, trung tâm đối tác, dịch vụ đồng bộ).
    ///
    ///              Cách dùng từ dịch vụ khác: tham chiếu Modules.TMS + Modules.TMS.Core, cấu hình Wolverine,
    ///              rồi dispatch qua mediator:
    ///                  var result = await MessBus.InvokeAsync&lt;AddIncidentExternalOutput&gt;(input);
    ///              Wolverine tự phát hiện handler này nhờ [assembly: WolverineModule] trong Modules.TMS.
    ///              Host hiện dùng DurabilityMode.MediatorOnly nên handler chạy TRONG tiến trình gọi, không
    ///              qua message broker - dịch vụ gọi phải tự cấu hình SqlSugar, cache và ItsDataTransporter.
    ///
    ///              VÌ SAO CẦN HANDLER RIÊNG thay vì dùng lại IncidentCommandHandler:
    ///              1. Handler thủ công gán CreatedBy từ App.User mà không kiểm null, sẽ nổ ở tiến trình nền.
    ///                 Ở đây actor truyền tường minh qua Input.
    ///              2. Trùng mã sự cố ở luồng thủ công là LỖI, còn ở đường tích hợp là dấu hiệu gọi lại sau
    ///                 timeout - phải trả về thành công kèm ID sự cố cũ, nếu ném lỗi thì hệ thống ngoài hiểu
    ///                 sai là thất bại rồi gọi lại mãi.
    ///              3. Trả về luôn kết quả lập lịch tự động để phía gọi biết sự cố có được xử lý tự động không.
    ///
    ///              Tự động hóa vẫn là luồng PHỤ: nếu lập lịch lỗi thì sự cố vẫn được tạo thành công và
    ///              quay về luồng xử lý thủ công.
    /// Created date: 24/07/2026
    /// </summary>
    public class IncidentExternalCommandHandler : IWolverineHandler
    {
        /// <summary>Tiền tố mã sự cố, giữ giống hàm sinh mã ở giao diện (editIncident.vue).</summary>
        private const string IncidentCodePrefix = "ICC";

        private BaseRepository<TmsIncident> _incidentRepo;
        private BaseRepository<TmsEventType> _eventTypeRepo;
        private BaseCacheService _cache;
        private IncidentBroadcaster _broadcaster;
        private TmsIncidentAutomationService _automationService;
        private IncidentSnapshotQueue _snapshotQueue;
        private ILogger<IncidentExternalCommandHandler> _logger;

        public IncidentExternalCommandHandler(
            BaseRepository<TmsIncident> incidentRepo,
            BaseRepository<TmsEventType> eventTypeRepo,
            BaseCacheService cache,
            IncidentBroadcaster broadcaster,
            TmsIncidentAutomationService automationService,
            IncidentSnapshotQueue snapshotQueue,
            ILogger<IncidentExternalCommandHandler> logger)
        {
            _incidentRepo = incidentRepo;
            _eventTypeRepo = eventTypeRepo;
            _cache = cache;
            _broadcaster = broadcaster;
            _automationService = automationService;
            _snapshotQueue = snapshotQueue;
            _logger = logger;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Tạo sự cố từ hệ thống ngoài rồi thử lập lịch tự động xử lý.
        /// Created date: 24/07/2026
        /// </summary>
        public async Task<AddIncidentExternalOutput> HandleAsync(AddIncidentExternalInput command)
        {
            if (command == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotMatch);

            if (string.IsNullOrWhiteSpace(command.EventTypeId))
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist, BaseMsg.BaseEntity.Type);

            // StartDate bắt buộc: mã sinh tự động chứa mốc thời gian này tới từng giây, nhờ đó lần gọi lại
            // sinh ra đúng mã cũ và được nhận diện là trùng. Mặc định "giờ hiện tại" sẽ phá cơ chế đó.
            if (command.StartDate == null)
                throw Oops.Oh(BaseMsg.BaseException.DataNotExist, BaseMsg.BaseEntity.StartTime);

            var eventType = await _eventTypeRepo.GetFirstAsync(u => u.ID == command.EventTypeId && u.IsDelete == null)
                ?? throw Oops.Oh(BaseMsg.BaseException.DataNotExist, BaseMsg.BaseEntity.Type);

            var code = string.IsNullOrWhiteSpace(command.Code)
                ? BuildIncidentCode(command, eventType.Code)
                : command.Code.Trim();

            // Trùng mã => coi như lần gọi lại, trả về sự cố đã có. Xem giải thích ở phần chú thích class.
            var existing = await _incidentRepo.GetFirstAsync(u => u.IsDelete == null && u.Code == code);
            if (existing != null)
            {
                _logger.LogInformation(
                    "[IncidentExternalCommandHandler] Sự cố mã {Code} đã tồn tại ({IncidentId}), bỏ qua yêu cầu tạo trùng từ nguồn {Source}.",
                    code, existing.ID, command.Source);

                return new AddIncidentExternalOutput
                {
                    IncidentId = existing.ID,
                    Code = existing.Code,
                    AlreadyExists = true,
                    AutomationScheduled = false,
                    AutomationReasonCode = IncidentAutomationConst.ScheduleReason.DuplicateRequest
                };
            }

            var incident = new TmsIncident
            {
                Code = code,
                Name = string.IsNullOrWhiteSpace(command.Name) ? BuildIncidentName(command, eventType.Name) : command.Name.Trim(),
                EventTypeId = command.EventTypeId,
                Source = command.Source,
                StartDate = command.StartDate,
                Location = command.Location,
                InfluenceScope = command.InfluenceScope,
                KmNumber = command.KmNumber,
                MetNumber = command.MetNumber,
                ZoneId = command.ZoneId,
                EquipmentId = command.EquipmentId,
                Description = command.Description,
                InjuredNumber = command.InjuredNumber,
                VehicleNumber = command.VehicleNumber,
                Remark = command.Remark,
                State = ((int)TmsIncidentStateEnum.Registration).ToString(),
                // Không gán ProcessMode: entity đã mặc định "manual". Dấu tự động chỉ được đặt khi
                // tự động hóa thực sự áp kịch bản (xem TmsIncidentAutomationService.ExecuteAsync).
                CreatedBy = command.Actor,
                CreatedDate = Shared.Utility.Utilities.BaseUtil.TimeServer.GetTimeServer()
            };

            incident.EventGroup = IncidentEventGroupConst.Normalize(eventType.Type)
                ?? throw Oops.Oh(BaseMsg.TmsIncident.Exception.EventGroupNotConfigured);
            if (incident.EventGroup == IncidentEventGroupConst.Event)
            {
                IncidentRecordingConst.ApplyToNewEvent(incident);
            }
            var insertedCount = await _incidentRepo.InsertWithDiffLogAsync(incident);
            if (insertedCount == 0)
                throw Oops.Oh(BaseMsg.BaseException.ServerError);

            _cache.RemoveByPrefixKey(CacheConst.Tms.TmsIncident);

            var result = new AddIncidentExternalOutput
            {
                IncidentId = incident.ID,
                Code = incident.Code,
                AlreadyExists = false
            };

            // Lập lịch tự động TRƯỚC khi phát broadcast: frontend nhận broadcast sẽ nạp lại ngay danh sách
            // sự cố kèm nhánh tự động; nếu phát trước thì run chưa tồn tại và banner sẽ rỗng.
            // Bọc try/catch: tự động hóa là lớp bổ trợ, lỗi ở đây không được làm hỏng việc tạo sự cố.
            try
            {
                var (run, reasonCode) = await _automationService.TryScheduleWithReasonAsync(
                    incident.ID, IncidentAutomationConst.TriggerState.Registration);

                result.AutomationScheduled = run != null;
                result.AutomationReasonCode = reasonCode;
                result.AutomationExecuteDueAt = run?.ExecuteDueAt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[IncidentExternalCommandHandler] Lập lịch tự động cho sự cố {IncidentId} thất bại.", incident.ID);
                result.AutomationReasonCode = IncidentAutomationConst.ScheduleReason.UnexpectedError;
            }

            _snapshotQueue.Enqueue(
                incident.ID,
                (int)TmsIncidentStateEnum.Registration);

            // Actor tường minh: handler này có thể chạy ở tiến trình nền nên App.User là null.
            await _broadcaster.NotifyAsync(incident, (int)TmsIncidentStateEnum.Registration, command.Actor);

            return result;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Sinh mã sự cố theo đúng cấu trúc hàm generateCodeAndName ở giao diện
        ///              (views/tms/incident/component/editIncident.vue):
        ///                  ICC_{mã loại sự kiện}_{vị trí}_{hướng}_{km}_{mét}_{ddMMyyyyHHmmss}
        ///              Đoạn nào thiếu dữ liệu thì bỏ qua, giống giao diện.
        ///
        ///              Location và InfluenceScope đã chính là MÃ cấu hình được lưu trong TmsIncident, nên
        ///              không cần gọi sang phân hệ cấu hình - chỉ mã loại sự kiện phải tra từ TmsEventType.
        /// Created date: 24/07/2026
        /// </summary>
        private static string BuildIncidentCode(AddIncidentExternalInput command, string? eventTypeCode)
        {
            var parts = new List<string> { IncidentCodePrefix };

            if (!string.IsNullOrWhiteSpace(eventTypeCode)) parts.Add(eventTypeCode);
            if (!string.IsNullOrWhiteSpace(command.Location)) parts.Add(command.Location);
            if (!string.IsNullOrWhiteSpace(command.InfluenceScope)) parts.Add(command.InfluenceScope);

            parts.Add((command.KmNumber ?? 0).ToString());
            parts.Add((command.MetNumber ?? 0).ToString());
            parts.Add(command.StartDate!.Value.ToString("ddMMyyyyHHmmss"));

            return string.Join("_", parts);
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Sinh tên sự cố bản NGẮN: {tên loại sự kiện} Km{km}+{mét} {dd-MM-yyyy HH:mm:ss}.
        ///              Giao diện còn ghép thêm tên vị trí và tên hướng, nhưng hai tên đó nằm trong dữ liệu
        ///              cấu hình ở phân hệ CfgSystem; gọi sang đó chỉ để sinh một chuỗi hiển thị là không
        ///              đáng thêm phụ thuộc. Phía tích hợp muốn tên đầy đủ thì tự truyền Name.
        /// Created date: 24/07/2026
        /// </summary>
        private static string BuildIncidentName(AddIncidentExternalInput command, string? eventTypeName)
        {
            var km = command.KmNumber ?? 0;
            var met = command.MetNumber ?? 0;

            return $"{eventTypeName} Km{km}+{met} {command.StartDate!.Value:dd-MM-yyyy HH:mm:ss}".Trim();
        }
    }
}
