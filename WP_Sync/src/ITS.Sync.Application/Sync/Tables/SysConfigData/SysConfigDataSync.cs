using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Enums;
using ITS.Sync.Core.Models;
using Modules.CfgSystem.Core.Entities;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Đồng bộ bảng SysConfigData theo chế độ UPSERT-ONLY (một chiều nguồn → đích):
/// - Nguồn có bản ghi mới → THÊM vào đích.
/// - Cùng ID nhưng UpdateTime của nguồn khác đích → SỬA (đích lấy giá trị nguồn).
/// - Bản ghi CHỈ có ở đích → GIỮ NGUYÊN (không xóa).
/// - Nguồn xóa MỀM (IsDelete) → lan sang đích qua bước SỬA.
/// Tham chiếu SysConfigType.ID qua ConfigTypeId nên đăng ký/chạy sau SysConfigType.
/// </summary>
public sealed class SysConfigDataSync(SyncSteps steps) : ITableSync
{
    public string TableName => "SysConfigData";

    public Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default)
        => steps.ContentCompareAsync<SysConfigData>(
            TableName, ct: cancellationToken, missingRowAction: MissingRowAction.Keep);
}
