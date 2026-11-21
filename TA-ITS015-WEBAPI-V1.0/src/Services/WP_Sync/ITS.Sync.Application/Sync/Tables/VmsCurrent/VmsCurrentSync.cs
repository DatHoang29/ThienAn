using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using Modules.VMS.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Đồng bộ bảng VmsCurrent (so sánh nội dung). Cột loại trừ khai trong <see cref="VmsCurrentColumns"/>.
/// Tùy biến message log riêng cho bảng này qua tham số describe* của SyncSteps.
/// </summary>
public sealed class VmsCurrentSync(SyncSteps steps) : ITableSync
{
    public string TableName => "VmsCurrent";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<VmsCurrent>(
            TableName,
            VmsCurrentColumns.Ignore,
            cancellationToken);
}
