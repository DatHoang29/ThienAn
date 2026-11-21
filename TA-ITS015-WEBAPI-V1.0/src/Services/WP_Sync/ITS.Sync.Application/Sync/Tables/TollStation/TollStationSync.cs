using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Enums;
using ITS.Sync.Core.Models;
using Modules.TOLL.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Đồng bộ bảng TollStation (trạm thu phí ETC — tra tên trạm vào/ra) — UPSERT-ONLY (MissingRowAction.Keep).
/// Update 07/10/2026: WP có màn quản lý trạm (dự phòng khi service đồng bộ lỗi) nên trạm chỉ có ở WP được GIỮ NGUYÊN.
/// Author: HieuNV
/// Created date: 07/10/2026
/// </summary>
public sealed class TollStationSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TollStation";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TollStation>(
            TableName,
            TollStationColumns.Ignore,
            cancellationToken,
            describeInsert: src => $"Thêm trạm StationId={src.StationId}, Name={src.Name}",
            missingRowAction: MissingRowAction.Keep);
}
