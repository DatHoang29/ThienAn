namespace ITS.Toll.Core.Models;

/// <summary>
/// Kết quả MỘT lần gọi API thu phí: dùng để hiển thị và ghi log trên form.
/// Author: ChuongLP
/// Created date: 24/08/2026
/// </summary>
public sealed class TollApiCallResult
{
    /// <summary>Tên API đã gọi.</summary>
    public string EndpointName { get; set; } = string.Empty;

    /// <summary>Loại dữ liệu (TransactionIn/Out/OutSummary/Master...).</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>URL đầy đủ (kèm query) đã gọi.</summary>
    public string RequestUrl { get; set; } = string.Empty;

    /// <summary>Thành công (HTTP 2xx và không có exception).</summary>
    public bool Success { get; set; }

    /// <summary>Mã HTTP trả về (0 nếu chưa nhận được phản hồi).</summary>
    public int StatusCode { get; set; }

    /// <summary>Thời lượng gọi (mili-giây).</summary>
    public long DurationMs { get; set; }

    /// <summary>Kiểu nội dung (Content-Type) trả về.</summary>
    public string? ContentType { get; set; }

    /// <summary>Nội dung thô trả về.</summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>Nội dung đã format đẹp (nếu là JSON hợp lệ), để hiển thị.</summary>
    public string PrettyContent { get; set; } = string.Empty;

    /// <summary>Số bản ghi ước lượng nhận về (nếu JSON là mảng hoặc chứa mảng data/result).</summary>
    public int? RecordCount { get; set; }

    /// <summary>Thông báo lỗi (nếu có).</summary>
    public string? Error { get; set; }

    /// <summary>Thời điểm gọi.</summary>
    public DateTime CalledAt { get; set; } = DateTime.Now;
}
