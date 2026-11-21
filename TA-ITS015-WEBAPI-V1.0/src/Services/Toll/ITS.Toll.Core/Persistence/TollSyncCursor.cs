namespace ITS.Toll.Core.Persistence;

/// <summary>
/// Con trỏ đồng bộ (điểm resume) cho MỘT DataType cần khoảng ngày. Lưu ra file để sống sót qua restart.
/// Author: ChuongLP
/// Created date: 09/09/2026
/// </summary>
public sealed class TollSyncCursor
{
    /// <summary>Loại dữ liệu (TransactionIn/TransactionOut/AccessLog...).</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Đã đồng bộ (và lưu DB thành công) tới mốc thời gian này — chính là ToDate của lượt thành công gần nhất.</summary>
    public DateTime? Watermark { get; set; }

    /// <summary>Thời điểm chạy gần nhất (dù thành công hay lỗi).</summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>Trạng thái lượt gần nhất: Success/Failed/Running.</summary>
    public string? LastStatus { get; set; }

    /// <summary>Thông điệp lỗi lượt gần nhất (nếu có).</summary>
    public string? LastError { get; set; }

    /// <summary>Ngày (yyyy-MM-dd) đang đếm số lần chạy.</summary>
    public string? RunDate { get; set; }

    /// <summary>Số lần chạy thành công trong RunDate.</summary>
    public int RunsToday { get; set; }

    public int LastReceived { get; set; }
    public int LastInserted { get; set; }
    public int LastUpdated { get; set; }
    public int LastUnchanged { get; set; }
    public int LastFailed { get; set; }
}

/// <summary>Một dòng nhật ký đồng bộ ghi vào file history theo ngày (JSONL).</summary>
public sealed class TollSyncHistoryEntry
{
    public DateTime At { get; set; } = DateTime.Now;
    public string DataType { get; set; } = string.Empty;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Received { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Failed { get; set; }
    public int Deleted { get; set; }
    public long DurationMs { get; set; }
    public int HttpStatus { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
    public string? Url { get; set; }
    public string? RequestId { get; set; }
}
