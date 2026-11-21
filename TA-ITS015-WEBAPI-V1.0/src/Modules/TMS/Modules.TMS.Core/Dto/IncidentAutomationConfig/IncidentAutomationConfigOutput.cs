using Modules.TMS.Core.Entities;

namespace Modules.TMS.Core.Dto.IncidentAutomationConfig;

/// <summary>
/// Author: SonTH
/// Description: Output item danh sách cấu hình tự động (kèm tên loại sự kiện + tên kịch bản để hiển thị grid)
/// Created date: 24/07/2026
/// </summary>
public class IncidentAutomationConfigOutput : TmsIncidentAutomationConfig
{
    /// <summary>
    /// Tên loại sự kiện (TmsEventType.Name)
    /// </summary>
    public string? EventTypeName { get; set; }

    /// <summary>
    /// Tên kịch bản tự động (TmsTrafficPlan.Name)
    /// </summary>
    public string? TrafficPlanName { get; set; }
}

/// <summary>
/// Author: SonTH
/// Description: Output phân trang cấu hình tự động
/// Created date: 24/07/2026
/// </summary>
public class PageIncidentAutomationConfigOutput : IncidentAutomationConfigOutput
{
}
