namespace ITS.Sync.Core.Enums;

/// <summary>
/// Cách xử lý bản ghi CHỈ CÓ Ở ĐÍCH (ID không có/không còn ở nguồn).
/// </summary>
public enum MissingRowAction
{
    /// <summary>Giữ nguyên — không tác động (upsert-only).</summary>
    Keep = 0,

    /// <summary>Xóa CỨNG khỏi bảng đích (mirror tuyệt đối).</summary>
    HardDelete = 1
}
