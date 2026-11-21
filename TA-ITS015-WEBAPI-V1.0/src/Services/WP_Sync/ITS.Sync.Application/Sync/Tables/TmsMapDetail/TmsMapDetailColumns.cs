namespace ITS.Sync.Application.Sync;

/// <summary>
/// Khai báo cột KHÔNG đồng bộ (loại trừ) cho bảng TmsMapDetail.
/// Để trống nghĩa là đồng bộ toàn bộ cột nội dung.
/// </summary>
public static class TmsMapDetailColumns
{
    /// <summary>Cột loại trừ khi đồng bộ.</summary>
    public static readonly string[] Ignore = [];
}
