using Modules.VDS.Controllers.Dashboard.Dto;
using Modules.VDS.Infrastructure.Services.Dashboard;

namespace Modules.VDS.Controllers.Dashboard.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler dashboard VDS (trung tâm và theo vùng), tổng hợp từ QuestDB.
///              Toàn bộ nghiệp vụ nằm ở <see cref="VdsDashboardService"/>; handler chỉ là ranh giới CQRS.
/// Created date: 13/10/2026
/// </summary>
public class DashboardQueryHandler : IWolverineHandler
{
    private readonly VdsDashboardService _dashboard;

    public DashboardQueryHandler(VdsDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>Snapshot trung tâm theo camera và nhóm hướng cấu hình.</summary>
    public Task<DashboardCenterOutput> HandleAsync(DashboardCenterInput command)
        => _dashboard.CenterInfoAsync(command.Direction);

    /// <summary>Snapshot theo vùng.</summary>
    public Task<DashboardZoneOutput> HandleAsync(DashboardZoneInput command)
        => _dashboard.ZoneInfoAsync(command.ZoneId);
}
