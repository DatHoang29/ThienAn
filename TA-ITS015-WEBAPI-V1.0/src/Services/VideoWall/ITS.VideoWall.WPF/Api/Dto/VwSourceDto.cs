using System.Text.Json.Serialization;

namespace Module.VideoWall.WPF.Api.Dto;

/// <summary>
/// Author: Đạt
/// Description: Nguồn tín hiệu vào (VwSource) — dữ liệu cho dropdown chọn nguồn của từng cửa sổ.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSourceDto : VwEntityDto
{
    /// <summary>Tên nguồn tín hiệu.</summary>
    public string? Name { get; set; }

    /// <summary>ID bộ điều khiển quản lý nguồn tín hiệu.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Loại nguồn tín hiệu (ví dụ: hdmi, ip_stream, camera...).</summary>
    public string? SourceType { get; set; }

    /// <summary>Loại tín hiệu (HDMI, DVI, RTSP, H.264...).</summary>
    public string? SignalType { get; set; }

    /// <summary>Đường dẫn luồng URL của nguồn tín hiệu IP Stream.</summary>
    public string? Url { get; set; }

    /// <summary>Cổng đầu vào vật lý hoặc logic — backend kiểu số (VwSource.InputPort int?).
    /// Cho đọc cả chuỗi để file lưu cục bộ cũ vẫn nạp được.</summary>
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? InputPort { get; set; }

    /// <summary>videoInputChannelID thật trên thiết bị — SyncSources nạp giá trị này từ thiết bị.</summary>
    public int? SignalNo { get; set; }

    /// <summary>Chế độ hoặc hình ảnh thu nhỏ (Thumbnail).</summary>
    public int? Thumbnail { get; set; }

    /// <summary>Độ phân giải tối đa hỗ trợ (ví dụ: 1920x1080, 3840x2160).</summary>
    public string? MaxResolution { get; set; }

    /// <summary>Tỷ lệ khung hình (ví dụ: 16:9, 4:3).</summary>
    public string? AspectRatio { get; set; }

    /// <summary>Trạng thái hoạt động của nguồn tín hiệu.</summary>
    public VwStatus? Status { get; set; }

    /// <summary>Trạng thái kết nối tín hiệu (Online/Offline).</summary>
    public VwStatus? SignalStatus { get; set; }

    /// <summary>Thứ tự sắp xếp hiển thị.</summary>
    public int? OrderNo { get; set; }

    /// <summary>Ghi chú bổ sung.</summary>
    public string? Remark { get; set; }

    /// <summary>ID kênh phát luồng StreamingChannelId trên thiết bị.</summary>
    public int? StreamingChannelId { get; set; }

    /// <summary>Tên hiển thị kèm loại tín hiệu dùng trên giao diện người dùng.</summary>
    [JsonIgnore]
    public string DisplayName =>
        SourceType == "ip_stream" || !string.IsNullOrWhiteSpace(Url) || (StreamingChannelId > 0)
            ? $"[IP] {(string.IsNullOrWhiteSpace(Name) ? Code : Name)}{(StreamingChannelId.HasValue && StreamingChannelId.Value > 0 ? $" (#{StreamingChannelId})" : $" ({SignalType ?? "RTSP"})")}"
            : SignalNo.HasValue
                ? $"[HDMI] {(string.IsNullOrWhiteSpace(Name) ? Code : Name)} (kênh {SignalNo})"
                : string.IsNullOrWhiteSpace(Name) ? Code ?? "" : Name;
}

/// <summary>
/// Author: Đạt
/// Description: Bộ lọc cho GET /api/videowall/vwsource/list.
/// Created date: 25/08/2026
/// </summary>
public sealed class VwSourceFilter
{
    /// <summary>ID bản ghi nguồn tín hiệu cần lọc.</summary>
    [JsonPropertyName("id")]
    public string? ID { get; set; }

    /// <summary>Tên nguồn tín hiệu cần lọc.</summary>
    public string? Name { get; set; }

    /// <summary>ID bộ điều khiển cần lọc.</summary>
    public string? ControllerId { get; set; }

    /// <summary>Loại nguồn tín hiệu cần lọc.</summary>
    public string? SourceType { get; set; }

    /// <summary>Loại tín hiệu cần lọc.</summary>
    public string? SignalType { get; set; }

    /// <summary>Cổng đầu vào cần lọc.</summary>
    public int? InputPort { get; set; }

    /// <summary>Trạng thái hoạt động cần lọc.</summary>
    public VwStatus? Status { get; set; }

    /// <summary>Trạng thái kết nối tín hiệu cần lọc.</summary>
    public VwStatus? SignalStatus { get; set; }
}
