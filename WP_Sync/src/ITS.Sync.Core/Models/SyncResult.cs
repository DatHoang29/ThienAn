using System;
using System.Collections.Generic;

namespace ITS.Sync.Core.Models;

/// <summary>
/// Kết quả đồng bộ MỘT bảng trong một chu kỳ: số bản ghi thêm/sửa/xóa/không đổi + thời lượng.
/// </summary>
public sealed class SyncResult
{
    /// <summary>Tên bảng đồng bộ.</summary>
    public string TableName { get; set; } = string.Empty;

    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Deleted { get; set; }
    public int Unchanged { get; set; }

    /// <summary>Chi tiết từng thay đổi (thêm/sửa/xóa) để log ra cho người dùng.</summary>
    public List<SyncChange> Changes { get; } = new();

    /// <summary>Thời gian chạy bảng này.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Chạy thành công hay không.</summary>
    public bool Success { get; set; }

    /// <summary>Thông báo lỗi nếu thất bại.</summary>
    public string? Error { get; set; }
}
