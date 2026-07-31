using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsWorkUnit (so sánh nội dung).</summary>
public sealed class TmsWorkUnitSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsWorkUnit";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsWorkUnit>(TableName, TmsWorkUnitColumns.Ignore, cancellationToken);
}
