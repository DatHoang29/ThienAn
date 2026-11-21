using Modules.TMS.Controllers.IncidentAutomationRun.Dto;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Dto.IncidentAutomationRun;
using Modules.TMS.Infrastructure.Services.Incident.Messaging;
using Modules.TMS.Infrastructure.Services.IncidentAutomation;
using Modules.TMS.Constants;

namespace Modules.TMS.Controllers.IncidentAutomationRun.Commands;

/// <summary>
/// Author: KienLT
/// Description: Command handler điều khiển lần chạy tự động xử lý sự cố: lập lịch cho sự cố đã tồn tại,
///              chạy ngay, hủy, chuyển thủ công. Toàn bộ nghiệp vụ vẫn ở <see cref="TmsIncidentAutomationService"/>;
///              handler chỉ chuẩn hoá input, phát realtime và dựng output.
///
///              14/10/2026: BE KHÔNG chặn theo nhóm tài khoản (bỏ 403 thêm ngày 28/09/2026). Việc ẩn/hiện thao
///              tác do FE quyết định theo cờ <c>Automation.CanOperate</c> trả trong danh sách sự cố.
/// Created date: 13/10/2026
/// </summary>
public class IncidentAutomationRunCommandHandler : IWolverineHandler
{
    private TmsIncidentAutomationService _automationService;
    private IncidentBroadcaster _broadcaster;

    public IncidentAutomationRunCommandHandler(
        TmsIncidentAutomationService automationService, IncidentBroadcaster broadcaster)
    {
        this._automationService = automationService;
        this._broadcaster = broadcaster;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Lập lịch tự động cho một sự cố ĐÃ TỒN TẠI — đường vào cho hệ thống ngoài tự ghi
    ///              <c>TmsIncident</c> (hiện là service VDS) rồi nhờ TMS xử lý tiếp.
    ///
    ///              KHÔNG ném lỗi khi không lập lịch được: trả về <c>Scheduled = false</c> kèm
    ///              <c>ReasonCode</c> để bên gọi biết vì sao và KHÔNG thử lại vô ích. Chỉ những lý do
    ///              thực sự tạm thời (<c>UnexpectedError</c>, <c>InsertFailed</c>) mới đáng gọi lại.
    ///
    ///              Phát bản tin realtime khi đã lập lịch để banner cảnh báo hiện ngay — sự cố do hệ
    ///              thống ngoài ghi trực tiếp vào DB không đi qua <c>IncidentBroadcaster</c> nên nếu
    ///              không phát ở đây thì người trực phải chờ nạp lại danh sách mới thấy.
    /// Created date: 12/08/2026
    /// </summary>
    public async Task<ScheduleIncidentAutomationOutput> HandleAsync(ScheduleIncidentAutomationInput command)
    {
        var incidentId = command?.IncidentId?.Trim();
        var triggerState = string.IsNullOrWhiteSpace(command?.TriggerState)
            ? IncidentAutomationConst.TriggerState.Registration
            : command!.TriggerState!.Trim();

        var (run, reasonCode) = command?.OncePerIncident == true
            ? await this._automationService.TryScheduleOnceWithReasonAsync(incidentId!, triggerState)
            : await this._automationService.TryScheduleWithReasonAsync(incidentId!, triggerState);

        if (run != null)
            this._broadcaster.NotifyAutomationRun(
                run.ID,
                run.IncidentId,
                run.Status,
                string.IsNullOrWhiteSpace(command?.Actor)
                    ? IncidentAutomationConst.SystemActor
                    : command!.Actor!.Trim());

        return new ScheduleIncidentAutomationOutput
        {
            IncidentId = incidentId,
            Scheduled = run != null,
            ReasonCode = reasonCode,
            RunId = run?.ID,
            RunStatus = run?.Status,
            ExecuteDueAt = run?.ExecuteDueAt
        };
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Bỏ qua countdown còn lại, chạy ngay lần tự động này.
    /// </summary>
    public async Task<bool> HandleAsync(ExecuteIncidentAutomationRunNowInput command)
    {
        return await this._automationService.ExecuteNowAsync(command.RunId);
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Hủy lần tự động (đang cảnh báo hoặc đang chờ auto-finish).
    /// </summary>
    public async Task<bool> HandleAsync(CancelIncidentAutomationRunInput command)
    {
        return await this._automationService.CancelAsync(command.RunId, command.Reason);
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Chuyển sự cố sang xử lý thủ công: hủy lần tự động nhưng giữ nguyên trạng thái
    ///              hiện tại của sự cố để người vận hành tiếp tục theo luồng thủ công.
    /// </summary>
    public async Task<bool> HandleAsync(ConvertIncidentAutomationRunToManualInput command)
    {
        return await this._automationService.CancelAsync(
            command.RunId,
            command.Reason ?? IncidentAutomationMessageConst.ConvertToManualReason);
    }
}
