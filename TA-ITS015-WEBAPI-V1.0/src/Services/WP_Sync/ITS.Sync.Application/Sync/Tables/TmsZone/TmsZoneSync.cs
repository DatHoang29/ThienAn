using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsZone (so sánh nội dung).</summary>
public sealed class TmsZoneSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsZone";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsZone>(TableName, TmsZoneColumns.Ignore, cancellationToken);
}
