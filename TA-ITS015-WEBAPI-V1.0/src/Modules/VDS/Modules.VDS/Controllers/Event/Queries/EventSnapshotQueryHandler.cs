using Modules.VDS.Controllers.Event.Dto;
using Modules.VDS.Infrastructure.Services.Event;

namespace Modules.VDS.Controllers.Event.Queries;

/// <summary>
/// Author: KienLT
/// Description: Chuyển yêu cầu snapshot sự kiện VDS sang service tổng hợp.
/// Created date: 22/10/2026
/// </summary>
public sealed class EventSnapshotQueryHandler(VdsEventSnapshotService snapshots) : IWolverineHandler
{
    /// <summary>
    /// Author: KienLT
    /// Description: Trả snapshot sự kiện theo bộ lọc hiện tại.
    /// Created date: 22/10/2026
    /// </summary>
    public Task<EventSnapshotOutput> HandleAsync(EventSnapshotInput input)
        => snapshots.SnapshotAsync(input);
}
