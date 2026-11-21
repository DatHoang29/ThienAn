using Modules.VDS.Core.Dto;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Author: KienLT
/// Description: Một yêu cầu chụp ảnh sự cố trong hàng đợi IncidentSnapshotQueue.
/// Created date: 25/09/2026
/// </summary>
internal sealed record IncidentSnapshotRequest(
    string IncidentId,
    int State,
    DateTimeOffset EnqueuedAt,
    VdsAutomationImageDecisionOutput? AutomationImage = null);
