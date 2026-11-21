using System.Text.Json.Serialization;

namespace Services.Vds.Features.VehicleFilter;

/// <summary>
/// Author: KienLT
/// Description: Bản tin cảnh báo biển số khớp danh sách đen/trắng, đẩy lên <see cref="VdsSubjects.Filter"/>.
/// Giao diện dựng nội dung thông báo từ <see cref="Plate"/> + <see cref="FilterType"/> +
/// <see cref="Remark"/>.
///
/// <para>
/// Tên trường đặt tường minh để KHÔNG lẫn với bản tin lượt xe: ở subject đó <c>size</c> mới là cỡ xe và
/// không có trường <c>type</c> nào. Ở đây <see cref="FilterType"/> là loại DANH SÁCH (đen/trắng), còn
/// <see cref="Size"/> mới là cỡ xe.
/// </para>
///
/// <para>
/// <b>Hai trường LÀN có ý nghĩa khác nhau</b>, giữ cả hai để giao diện không phải đoán:
/// <see cref="Lane"/> là số làn do thiết bị cấp trong bản tin; <see cref="LaneId"/> là khoá danh mục làn của
/// camera (chính giá trị ghi vào cột làn của bảng lịch sử).
/// </para>
///
/// <see cref="Location"/> là MÃ TUYẾN của camera trong danh mục, không phải toạ độ.
/// Created date: 26/08/2026
/// </summary>
public sealed record VdsFilterPayload
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "vehicle_filter";

    /// <summary>Id dòng lịch sử vừa ghi; null khi ghi lịch sử thất bại mà vẫn phải báo.</summary>
    [JsonPropertyName("historyId")]
    public string? HistoryId { get; init; }

    /// <summary>Chuyến của lượt (cùng biển, đi tiến theo tuyến, trong khoảng nghỉ) — giao diện gộp một thẻ theo mã này.</summary>
    [JsonPropertyName("tripId")]
    public string? TripId { get; init; }

    /// <summary>Id dòng danh sách đã khớp.</summary>
    [JsonPropertyName("filterId")]
    public string? FilterId { get; init; }

    /// <summary>Biển số NGUYÊN VĂN của lượt xe (không phải bản chuẩn hoá).</summary>
    [JsonPropertyName("plate")]
    public string? Plate { get; init; }

    /// <summary>Loại danh sách: đen/trắng theo mã web đang dùng.</summary>
    [JsonPropertyName("filterType")]
    public string? FilterType { get; init; }

    [JsonPropertyName("alarm")]
    public string? Alarm { get; init; }

    /// <summary>
    /// true ⇒ giao diện hiện thông báo (cờ cảnh báo bật và đã qua cửa chống dội). false ⇒ chỉ là một dòng lịch
    /// sử mới cho bảng hoạt động gần đây, KHÔNG hiện thông báo. Bản tin cũ không có trường này coi như true.
    /// </summary>
    [JsonPropertyName("notify")]
    public bool Notify { get; init; } = true;

    [JsonPropertyName("remark")]
    public string? Remark { get; init; }

    [JsonPropertyName("equipmentId")]
    public string? EquipmentId { get; init; }

    [JsonPropertyName("equipmentName")]
    public string? EquipmentName { get; init; }

    /// <summary>Lý trình của camera bắt được xe (Km + mét) — giao diện hiện vị trí mà không phải tra thêm.</summary>
    [JsonPropertyName("km")]
    public int? Km { get; init; }

    [JsonPropertyName("met")]
    public int? Met { get; init; }

    [JsonPropertyName("zone")]
    public string? Zone { get; init; }

    [JsonPropertyName("direction")]
    public string? Direction { get; init; }

    /// <summary>Số làn do thiết bị cấp trong bản tin.</summary>
    [JsonPropertyName("lane")]
    public string? Lane { get; init; }

    /// <summary>Khoá danh mục làn của camera.</summary>
    [JsonPropertyName("laneId")]
    public string? LaneId { get; init; }

    /// <summary>Mã tuyến của camera trong danh mục.</summary>
    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("speed")]
    public double? Speed { get; init; }

    /// <summary>Cỡ xe suy từ chiều dài.</summary>
    [JsonPropertyName("size")]
    public string? Size { get; init; }

    [JsonPropertyName("detectTime")]
    public DateTime DetectTime { get; init; }
}
