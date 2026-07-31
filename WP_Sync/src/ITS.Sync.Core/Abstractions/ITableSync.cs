using System.Threading;
using System.Threading.Tasks;
using ITS.Sync.Core.Models;

namespace ITS.Sync.Core.Abstractions;

/// <summary>
/// Đồng bộ MỘT bảng. Mỗi bảng có một lớp riêng, tự chứa toàn bộ logic của nó
/// (đọc nguồn, so khớp, ghi đích) để dễ đọc và dễ sửa riêng từng bảng.
/// </summary>
public interface ITableSync
{
    /// <summary>Tên bảng.</summary>
    string TableName { get; }

    /// <summary>Chạy đồng bộ bảng này một lần, trả về kết quả.</summary>
    Task<SyncResult> SyncAsync(CancellationToken cancellationToken = default);
}
