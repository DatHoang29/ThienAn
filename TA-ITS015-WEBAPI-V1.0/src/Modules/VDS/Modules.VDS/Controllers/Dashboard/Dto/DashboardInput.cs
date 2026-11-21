namespace Modules.VDS.Controllers.Dashboard.Dto;

/// <summary>
/// Author: KienLT
/// Description: Message lấy snapshot dashboard trung tâm. Controller giữ query string <c>direction</c> như cũ
/// và tự dựng message này.
/// Created date: 13/10/2026
/// </summary>
public class DashboardCenterInput
{
    /// <summary>Chọn nhóm camera đã cấu hình theo hướng; bỏ trống là toàn tuyến.</summary>
    public string? Direction { get; set; }
}

/// <summary>
/// Author: KienLT
/// Description: Message lấy snapshot dashboard theo vùng. Controller giữ query string <c>zoneId</c> như cũ
/// và tự dựng message này.
/// Created date: 13/10/2026
/// </summary>
public class DashboardZoneInput
{
    /// <summary>Lọc theo một vùng; bỏ trống là mọi vùng.</summary>
    public string? ZoneId { get; set; }
}
