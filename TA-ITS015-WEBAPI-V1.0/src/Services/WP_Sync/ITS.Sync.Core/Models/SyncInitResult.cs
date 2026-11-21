namespace ITS.Sync.Core.Models;

/// <summary>
/// Kết quả bước Init: kiểm tra kết nối 2 DB (nguồn + đích) trước khi cho phép chạy đồng bộ.
/// </summary>
public sealed class SyncInitResult
{
    /// <summary>Kết nối DB nguồn OK?</summary>
    public bool SourceOk { get; init; }

    /// <summary>Lỗi kết nối nguồn (nếu có).</summary>
    public string? SourceError { get; init; }

    /// <summary>Kết nối DB đích OK?</summary>
    public bool TargetOk { get; init; }

    /// <summary>Lỗi kết nối đích (nếu có).</summary>
    public string? TargetError { get; init; }

    /// <summary>Sẵn sàng chạy khi CẢ nguồn và đích đều kết nối được.</summary>
    public bool Ok => SourceOk && TargetOk;
}
