namespace Modules.Media.Core.Dto;

/// <summary>
/// Description: Phần chung của mọi kết quả ảnh trả về từ ITS-Media / WP-MediaPublic.
/// Created date: 08/10/2026
/// </summary>
public class MediaBaseOutput
{
    /// <summary>Có nội dung ảnh (Base64) hay không.</summary>
    public bool Exists { get; set; }

    /// <summary>Ready | PendingCapture | MissingSource | NotFound | External | Disabled | Unavailable.</summary>
    public string Status { get; set; } = "NotFound";

    public string ContentType { get; set; } = "image/jpeg";

    /// <summary>Nội dung ảnh dạng Base64 (rỗng khi không có ảnh).</summary>
    public string Base64 { get; set; } = string.Empty;

    /// <summary>Chỉ có giá trị khi Status = External (URL ngoài, FE dùng thẳng).</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Phiên bản để cache (UpdateTime.Ticks hoặc CaptureTime.Ticks).</summary>
    public string Version { get; set; } = string.Empty;
}
