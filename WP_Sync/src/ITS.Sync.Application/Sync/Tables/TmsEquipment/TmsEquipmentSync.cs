using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Đồng bộ bảng TmsEquipment (so sánh nội dung). Cột loại trừ khai trong <see cref="TmsEquipmentColumns"/>.
/// Tùy biến message log riêng cho bảng này qua tham số describe* của SyncSteps.
/// </summary>
public sealed class TmsEquipmentSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsEquipment";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsEquipment>(
            TableName,
            TmsEquipmentColumns.Ignore,
            cancellationToken,
            describeInsert: src => $"Thêm thiết bị mới — Code={src.Code}",
            describeDelete: tgt => $"Gỡ thiết bị Code={tgt.Code} (không còn ở nguồn)");
}
