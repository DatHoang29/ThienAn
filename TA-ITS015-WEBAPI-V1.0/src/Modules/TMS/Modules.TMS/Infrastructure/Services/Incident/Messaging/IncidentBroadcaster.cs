using Modules.TMS.Infrastructure.Services.Incident.Configuration;
using Modules.TMS.Infrastructure.Services.Incident.Mapping;
using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.TMS.Core.Entities;
using Shared.Core.Utilities.Constants;
using Shared.Utility.Utilities;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident.Messaging
{
    /// <summary>
    /// Author: ThangNN (refactor)
    /// Description: Phát thông báo realtime về biến động trong phân hệ sự cố qua kênh Incident.
    /// </summary>
    public class IncidentBroadcaster(BaseRepository<TmsEventType> eventTypes) : IScoped
    {
        /// <summary>
        /// Map trạng thái sự cố -> key i18n của thông báo realtime.
        /// </summary>
        private static string GetBroadcastMessageKey(int state) => state switch
        {
            (int)TmsIncidentStateEnum.Registration => "lz.message.tmsIncident.registrationNotify",
            (int)TmsIncidentStateEnum.Verified => "lz.message.tmsIncident.verifiedNotify",
            (int)TmsIncidentStateEnum.InProgress => "lz.message.tmsIncident.inProgressNotify",
            (int)TmsIncidentStateEnum.Finished => "lz.message.tmsIncident.finishedNotify",
            (int)TmsIncidentStateEnum.Skipped => "lz.message.tmsIncident.skippedNotify",
            (int)TmsIncidentStateEnum.Canceled => "lz.message.tmsIncident.canceledNotify",
            (int)TmsIncidentStateEnum.Updated => "lz.message.tmsIncident.updatedNotify",
            _ => string.Empty
        };

        /// <summary>
        /// Thông báo frontend về biến động trạng thái của một sự cố (incident có thể null cho batch).
        /// </summary>
        /// <summary>
        /// Author: SonTH
        /// Description: actor (nếu truyền vào) dùng làm accountInfo hiển thị, tránh gọi App.User khi không có
        /// HttpContext (ví dụ khi được gọi từ luồng tự động chạy trong background worker).
        /// Null-safe với App.User: nếu không có HttpContext thì App.User có thể null, không được gọi trực tiếp
        /// FindFirst trên giá trị null.
        /// Created date: 24/07/2026
        /// </summary>
        public Task NotifyAsync(TmsIncident incident, int state, string? actor = null)
        {
            var broadcastMessage = GetBroadcastMessageKey(state);
            if (string.IsNullOrWhiteSpace(broadcastMessage))
                return Task.CompletedTask;

            var type = incident?.EventTypeId == null ? null : eventTypes.GetFirst(x => x.ID == incident.EventTypeId && x.IsDelete == null);
            if (incident != null)
            {
                incident.EventGroup ??= Modules.TMS.Core.Constants.IncidentEventGroupConst.Normalize(type?.Type);
            }
            var broadcastDateTime = BaseUtil.TimeServer.GetTimeServer().Ticks;
            var incidentName = incident?.Name ?? "Incident";
            var accountInfo = actor ?? $"{App.User?.FindFirst(ClaimConst.NickName)?.Value ?? App.User?.FindFirst(ClaimConst.OrgName)?.Value}";

            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.Incident, new
            {
                incidentId = incident?.ID,
                eventGroup = incident?.EventGroup,
                state = incident?.State,
                remark = incident?.Remark,
                warningType = type?.WarningType,
                priority = type?.Priority,
                icon = type?.Url,
                changeType = state == (int)TmsIncidentStateEnum.Registration ? "created" : "updated",
                zoneId = incident?.ZoneId,
                name = incident?.Name,
                eventTypeId = incident?.EventTypeId,
                broadcastCode = $"{incident?.ID ?? "Incident"}-{broadcastDateTime}-{state}",
                broadcastObject = IncidentBroadcastObjectEnum.Incident.ToString(),
                broadcastMessage = new { Message = broadcastMessage, Account = accountInfo, Incident = incidentName },
                broadcastDateTime
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Author: SonTH
        /// Description: Thông báo frontend về biến động của một lần TỰ ĐỘNG xử lý sự cố.
        ///              Dùng CHUNG kênh Incident với luồng thủ công để không phải thêm subject/kênh mới ở cả
        ///              backend lẫn frontend - frontend đã subscribe kênh này sẵn. Frontend phân biệt bằng
        ///              broadcastObject = "AutomationRun".
        ///
        ///              Mục đích: banner cảnh báo xuất hiện/biến mất tức thì thay vì phải chờ chu kỳ nạp lại
        ///              định kỳ (hoặc phải F5). Đặc biệt cần cho việc worker chạy nền chuyển
        ///              WarningPending -> Running: không có HttpContext nên bắt buộc truyền actor.
        /// Created date: 24/07/2026
        /// </summary>
        /// <param name="runId">ID của TmsIncidentAutomationRun</param>
        /// <param name="incidentId">ID sự cố tương ứng</param>
        /// <param name="runStatus">Trạng thái mới của run (IncidentAutomationConst.RunStatus)</param>
        /// <param name="actor">Người/hệ thống thực hiện; bắt buộc khi gọi từ background worker</param>
        public void NotifyAutomationRun(string runId, string? incidentId, string runStatus, string? actor = null)
        {
            if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(runStatus))
                return;

            var broadcastDateTime = BaseUtil.TimeServer.GetTimeServer().Ticks;
            var accountInfo = actor ?? $"{App.User?.FindFirst(ClaimConst.NickName)?.Value ?? App.User?.FindFirst(ClaimConst.OrgName)?.Value}";

            ItsDataTransporter.BroadcastData(ItsDataTransporterChannel.Incident, new
            {
                // broadcastCode phải đổi mỗi lần phát để frontend nhận biết là biến động mới.
                broadcastCode = $"{runId}-{broadcastDateTime}-{runStatus}",
                broadcastObject = IncidentBroadcastObjectEnum.AutomationRun.ToString(),
                broadcastMessage = new
                {
                    Message = "lz.message.tmsIncidentAutomation.runChangedNotify",
                    Account = accountInfo,
                    RunId = runId,
                    IncidentId = incidentId,
                    RunStatus = runStatus
                },
                broadcastDateTime
            });
        }
    }
}
