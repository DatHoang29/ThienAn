using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Enums;
using ITS.Sync.Core.Models;
using Modules.TMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Đồng bộ bảng TmsWorkUnit (so sánh nội dung).
/// Update 07/10/2026: chế độ UPSERT-ONLY (MissingRowAction.Keep) — WP có màn quản lý đơn vị làm việc
/// (dự phòng khi service đồng bộ lỗi) nên đơn vị chỉ có ở WP được GIỮ NGUYÊN, không xóa.
/// Bản ghi lấy từ ITS10 bị sửa trên WP sẽ bị ghi đè lại khi ITS10 cập nhật.
/// </summary>
public sealed class TmsWorkUnitSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TmsWorkUnit";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TmsWorkUnit>(
            TableName, TmsWorkUnitColumns.Ignore, cancellationToken,
            missingRowAction: MissingRowAction.Keep);
}
