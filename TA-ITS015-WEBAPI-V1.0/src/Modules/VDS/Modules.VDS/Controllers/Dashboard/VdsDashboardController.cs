using Modules.VDS.Controllers.Dashboard.Dto;

namespace Modules.VDS.Controllers.Dashboard;

/// <summary>
/// Author: KienLT
/// Description: API dashboard VDS: thông tin trung tâm và theo vùng, tổng hợp từ QuestDB.
///
/// <para><b>Từ 13/10/2026 đi qua message bus</b> như các controller khác của module:
/// <c>Queries/DashboardQueryHandler</c> delegate sang <c>VdsDashboardService</c>. Action giữ nguyên tham số
/// query string (<c>direction</c>, <c>zoneId</c>) và tự dựng message.</para>
/// Created date: 21/09/2026
/// </summary>
[ApiDescriptionSettings(GroupName, Order = 594)]
[ApiController]
public class VdsDashboardController : BaseController
{
    /// <summary>
    /// Get VDS dashboard center info
    /// </summary>
    /// <param name="direction">Chọn nhóm camera đã cấu hình theo hướng; bỏ trống là toàn tuyến.</param>
    /// <returns>Số liệu camera, hướng và các cửa sổ thời gian của trung tâm điều hành.</returns>
    [HttpGet("CenterInfo")]
    [DisplayName("Dashboard center info")]
    public async Task<DashboardCenterOutput> CenterInfo(string? direction)
    {
        return await MessBus.InvokeAsync<DashboardCenterOutput>(new DashboardCenterInput { Direction = direction });
    }

    /// <summary>
    /// Get VDS dashboard zone info
    /// </summary>
    /// <param name="zoneId">Lọc theo một vùng; bỏ trống là mọi vùng.</param>
    /// <returns>Số liệu theo vùng.</returns>
    [HttpGet("ZoneInfo")]
    [DisplayName("Dashboard zone info")]
    public async Task<DashboardZoneOutput> ZoneInfo(string? zoneId)
    {
        return await MessBus.InvokeAsync<DashboardZoneOutput>(new DashboardZoneInput { ZoneId = zoneId });
    }
}
