using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>Đồng bộ bảng TmsEquipmentType (so sánh nội dung).</summary>
public sealed class TmsEquipmentTypeSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsEquipmentType";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsEquipmentType>(TableName, TmsEquipmentTypeColumns.Ignore, cancellationToken);
}
