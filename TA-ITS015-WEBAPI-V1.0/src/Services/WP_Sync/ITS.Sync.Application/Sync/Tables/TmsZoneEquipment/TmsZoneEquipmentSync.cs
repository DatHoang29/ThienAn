using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsZoneEquipment (so sánh nội dung).</summary>
public sealed class TmsZoneEquipmentSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsZoneEquipment";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsZoneEquipment>(TableName, TmsZoneEquipmentColumns.Ignore, cancellationToken);
}
