namespace ITS.Toll.Core.Messaging;

/// <summary>
/// Gói tin trên kênh ta.its.control.toll. Phân biệt bằng <see cref="Type"/>:
///   - "request": BE gửi → dịch vụ nhận và xử lý.
///   - "ack"    : dịch vụ gửi ngay khi nhận được request hợp lệ (trước khi xử lý) → FE biết Worker online.
///   - "result" : dịch vụ gửi sau khi xử lý xong → FE hiển thị kết quả (dịch vụ bỏ qua khi tự nhận lại).
/// Author: ChuongLP
/// Created date: 14/09/2026
/// </summary>
public sealed class TollControlRequest
{
    /// <summary>Loại gói tin: "request" hoặc "result".</summary>
    public string? Type { get; set; }

    /// <summary>Mã truy vết yêu cầu (đối chiếu request/result).</summary>
    public string? RequestId { get; set; }

    /// <summary>Danh sách loại dữ liệu cần lấy (Master/Station/Lane/Shift/TransactionIn/TransactionOut/OutSummary/AccessLog).</summary>
    public List<string>? DataTypes { get; set; }

    /// <summary>Thời gian bắt đầu (đã là giờ local server do BE convert). Null với loại dữ liệu không cần khoảng thời gian.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Thời gian kết thúc (đã là giờ local server do BE convert).</summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Gói xác nhận dịch vụ phát ngay khi nhận request hợp lệ (Type = "ack"), trước khi bắt đầu xử lý.
/// FE dùng để phân biệt: không nhận ack → Worker offline/chưa khởi động;
/// nhận ack nhưng chờ lâu → Worker đang xử lý (dữ liệu lớn, TCS chậm).
/// </summary>
public sealed class TollControlAck
{
    public string Type { get; set; } = "ack";
    public string? RequestId { get; set; }
    /// <summary>Danh sách DataType đã nhận được (để FE xác nhận đúng yêu cầu của mình).</summary>
    public List<string> DataTypes { get; set; } = new();
    /// <summary>Thời điểm Worker nhận được request (giờ local server).</summary>
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
}

/// <summary>Kết quả tổng hợp dịch vụ phát lại (Type = "result") cho FE.</summary>
public sealed class TollControlResult
{
    public string Type { get; set; } = "result";

    public string? RequestId { get; set; }

    /// <summary>True khi TẤT CẢ loại dữ liệu đều lấy/lưu thành công.</summary>
    public bool Success { get; set; }

    /// <summary>Thông báo tổng hợp (đọc được cho người dùng).</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Thời điểm hoàn tất xử lý (giờ local server).</summary>
    public DateTime FinishedAt { get; set; } = DateTime.Now;

    /// <summary>Chi tiết kết quả theo từng loại dữ liệu.</summary>
    public List<TollControlResultItem> Items { get; set; } = new();

    // Tổng hợp số bản ghi qua tất cả loại dữ liệu.
    public int Received { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Failed { get; set; }
    public int Deleted { get; set; }
}

/// <summary>Kết quả cho một loại dữ liệu cụ thể.</summary>
public sealed class TollControlResultItem
{
    public string DataType { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Received { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Failed { get; set; }
    public int Deleted { get; set; }
}
