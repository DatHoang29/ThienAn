using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsEventType (so sánh nội dung).</summary>
public sealed class TmsEventTypeSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsEventType";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsEventType>(TableName, TmsEventTypeColumns.Ignore, cancellationToken);
}
