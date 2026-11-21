using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TOLL.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Đồng bộ bảng TollRoad (tuyến đường ETC (cặp trạm vào/ra) — chi tiết bảng giá tham chiếu qua RoadSourceId) — mirror một chiều ITS10 → WP.
/// Author: HieuNV
/// Created date: 07/10/2026
/// </summary>
public sealed class TollRoadSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TollRoad";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TollRoad>(
            TableName,
            TollRoadColumns.Ignore,
            cancellationToken,
            describeInsert: src => $"Thêm tuyến SourceId={src.SourceId}, Name={src.Name}",
            describeDelete: tgt => $"Gỡ tuyến SourceId={tgt.SourceId} (không còn ở nguồn)");
}
