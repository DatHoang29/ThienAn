using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsIncident (so sánh nội dung).</summary>
public sealed class TmsIncidentSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsIncident";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsIncident>(TableName, TmsIncidentColumns.Ignore, cancellationToken);
}
