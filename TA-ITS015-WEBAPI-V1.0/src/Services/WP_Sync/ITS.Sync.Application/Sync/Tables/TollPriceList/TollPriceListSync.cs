using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Enums;
using ITS.Sync.Core.Models;
using Modules.TOLL.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Đồng bộ bảng TollPriceList (bảng giá (header) — module WP đọc/ghi trực tiếp) theo chế độ UPSERT-ONLY:
/// - Nguồn (ITS10) có bản ghi mới → THÊM; cùng ID khác UpdateTime → SỬA (đích lấy giá trị nguồn).
/// - Bản ghi CHỈ có ở đích (WP nhập tay khi service đồng bộ lỗi) → GIỮ NGUYÊN, không xóa.
/// - Bản ghi đồng bộ bị sửa/xóa mềm trên WP → UpdateTime lệch nguồn → bị ghi đè lại theo nguồn.
/// Author: HieuNV
/// Created date: 07/10/2026
/// </summary>
public sealed class TollPriceListSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TollPriceList";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TollPriceList>(
            TableName,
            TollPriceListColumns.Ignore,
            cancellationToken,
            describeInsert: src => $"Thêm bảng giá SourceId={src.SourceId}, Name={src.Name}",
            missingRowAction: MissingRowAction.Keep);
}
