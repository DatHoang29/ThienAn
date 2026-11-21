using Modules.TMS.Controllers.IncidentAutomationRun.Dto;
using Modules.TMS.Core.Dto.IncidentAutomationRun;
using Modules.TMS.Infrastructure.Services.IncidentAutomation;

namespace Modules.TMS.Controllers.IncidentAutomationRun.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler đọc lần chạy tự động xử lý sự cố (run gần nhất theo sự cố, danh sách run
///              đang hoạt động cho banner). Đọc qua <see cref="TmsIncidentAutomationService"/>.
/// Created date: 13/10/2026
/// </summary>
public class IncidentAutomationRunQueryHandler : IWolverineHandler
{
    private TmsIncidentAutomationService _automationService;

    public IncidentAutomationRunQueryHandler(TmsIncidentAutomationService automationService)
    {
        this._automationService = automationService;
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Lấy run gần nhất theo Incident (để FE hiển thị countdown/trạng thái).
    /// </summary>
    public async Task<IncidentAutomationRunOutput?> HandleAsync(GetIncidentAutomationRunByIncidentInput command)
    {
        var run = await this._automationService.GetLatestByIncidentAsync(command.IncidentId);
        return run?.Adapt<IncidentAutomationRunOutput>();
    }

    /// <summary>
    /// Author: SonTH
    /// Description: Danh sách run đang chờ thực thi / chờ tự kết thúc (kèm thông tin sự cố),
    ///              dùng cho banner cảnh báo hiển thị trên mọi màn hình.
    /// </summary>
    public async Task<List<IncidentAutomationRunViewOutput>> HandleAsync(GetActiveIncidentAutomationRunListInput command)
    {
        return await this._automationService.GetActiveRunViewsAsync();
    }
}
