using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.TOLL.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Đồng bộ bảng TollMasterData (danh mục ETC gộp (loại xe, loại vé, nhóm giá...) — tra tên loại xe cho chi tiết bảng giá) — mirror một chiều ITS10 → WP.
/// Author: HieuNV
/// Created date: 07/10/2026
/// </summary>
public sealed class TollMasterDataSync(SyncSteps steps) : ITableSync
{
    public string TableName => "TollMasterData";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<TollMasterData>(
            TableName,
            TollMasterDataColumns.Ignore,
            cancellationToken,
            describeInsert: src => $"Thêm danh mục {src.DataType} SourceId={src.SourceId}, Name={src.Name}",
            describeDelete: tgt => $"Gỡ danh mục {tgt.DataType} SourceId={tgt.SourceId} (không còn ở nguồn)");
}
