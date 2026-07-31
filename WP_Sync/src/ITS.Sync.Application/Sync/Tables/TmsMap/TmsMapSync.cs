using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsMap (so sánh nội dung).</summary>
public sealed class TmsMapSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsMap";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsMap>(TableName, TmsMapColumns.Ignore, cancellationToken);
}
