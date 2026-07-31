namespace ITS.Sync.Core.Models;

/// <summary>
/// Một thay đổi cụ thể trong chu kỳ đồng bộ (dùng để log chi tiết).
/// </summary>
public sealed class SyncChange
{
    /// <summary>Loại thay đổi: "Thêm" / "Sửa" / "Xóa".</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Khóa (ID) của bản ghi bị ảnh hưởng.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Mô tả chi tiết (ví dụ các field đã đổi: 'cũ' -> 'mới').</summary>
    public string? Detail { get; init; }
}
