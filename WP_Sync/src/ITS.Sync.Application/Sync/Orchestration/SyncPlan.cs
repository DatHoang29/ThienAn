using System.Collections.Generic;
using ITS.Sync.Core.Models;

namespace ITS.Sync.Application.Sync;

/// <summary>
/// Kế hoạch đồng bộ một bảng sau bước so sánh (Diff): danh sách cần Thêm/Sửa/Xóa
/// và số bản ghi Không đổi, kèm log chi tiết từng thay đổi.
/// </summary>
public sealed class SyncPlan<T>
{
    /// <summary>Bản ghi có ở nguồn, chưa có ở đích → INSERT.</summary>
    public List<T> ToInsert { get; } = new();

    /// <summary>Bản ghi có ở cả hai nhưng khác nội dung → UPDATE.</summary>
    public List<T> ToUpdate { get; } = new();

    /// <summary>ID bản ghi còn ở đích nhưng không còn ở nguồn → DELETE (xóa cứng).</summary>
    public List<string> ToDeleteIds { get; } = new();

    /// <summary>Số bản ghi trùng khớp hoàn toàn (không cần ghi).</summary>
    public int Unchanged { get; set; }

    /// <summary>Chi tiết từng thay đổi để log ra UI.</summary>
    public List<SyncChange> Changes { get; } = new();
}
