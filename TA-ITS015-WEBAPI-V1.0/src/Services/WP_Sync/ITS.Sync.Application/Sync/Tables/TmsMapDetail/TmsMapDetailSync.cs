using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsMapDetail (so sánh nội dung).</summary>
public sealed class TmsMapDetailSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsMapDetail";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsMapDetail>(TableName, TmsMapDetailColumns.Ignore, cancellationToken);
}
