using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsZoneStatus (so sánh nội dung).</summary>
public sealed class TmsZoneStatusSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsZoneStatus";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsZoneStatus>(TableName, TmsZoneStatusColumns.Ignore, cancellationToken);
}
