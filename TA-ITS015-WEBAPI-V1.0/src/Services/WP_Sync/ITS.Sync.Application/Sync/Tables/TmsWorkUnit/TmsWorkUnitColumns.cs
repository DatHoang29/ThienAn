namespace ITS.Sync.Application.Sync;

/// <summary>
/// Khai báo cột KHÔNG đồng bộ (loại trừ) cho bảng TmsWorkUnit.
/// Để trống nghĩa là đồng bộ toàn bộ cột nội dung.
/// </summary>
public static class TmsWorkUnitColumns
{
    /// <summary>Cột loại trừ khi đồng bộ.</summary>
    public static readonly string[] Ignore = [];
}
