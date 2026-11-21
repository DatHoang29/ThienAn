using System.Text.Json.Serialization;

namespace Services.Vds.Features.ZoneStatus;

/// <summary>
/// Author: KienLT
/// Description: Bản tin trạng thái vùng đẩy lên <see cref="VdsSubjects.Zone"/>.
///
/// <para>
/// Giữ nguyên bộ tên của bản cũ để bên nhận không phải sửa. Trong đó có ba trường CÙNG diễn tả một bậc,
/// cố tình dư:
/// <list type="bullet">
/// <item><see cref="Congested"/> — cờ hai mức, dành cho bên nhận viết từ thời chỉ có nghẽn/không nghẽn;</item>
/// <item><see cref="Slow"/> — cờ bậc chùng, thêm sau;</item>
/// <item><see cref="Level"/> — tên bậc, KHÔNG phụ thuộc bộ mã trong cấu hình, dùng cho bên nhận mới.</item>
/// </list>
/// Bên nhận mới chỉ nên đọc <see cref="Level"/>; hai cờ kia là tương thích ngược.
/// </para>
///
/// <see cref="Condition"/> là đúng chuỗi đã ghi vào DB, còn <see cref="Level"/> là tên bậc nội bộ — hai thứ
/// khác nhau: bộ mã DB do người vận hành khai theo giá trị FE đang dùng và có thể khác nhau giữa các hệ.
/// Created date: 26/08/2026
/// </summary>
public sealed record VdsZonePayload
{
    /// <summary>
    /// Giữ <c>kind="zone"</c> dù đã có subject riêng: bên nhận cũ gộp hai luồng trên một subject và phân
    /// biệt bằng trường này, và bản tin nằm trong log vẫn tự mô tả được.
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "zone";

    [JsonPropertyName("zoneId")]
    public string? ZoneId { get; init; }

    /// <summary>Camera gây ra thay đổi; null khi thay đổi do quét theo giờ (không camera nào "gây ra").</summary>
    [JsonPropertyName("equipmentId")]
    public string? EquipmentId { get; init; }

    /// <summary>Đúng chuỗi đã ghi vào cột tình trạng của bảng trạng thái vùng.</summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; init; }

    /// <summary>Tên bậc nội bộ: <c>normal</c> / <c>slow</c> / <c>congested</c>.</summary>
    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("congested")]
    public bool Congested { get; init; }

    [JsonPropertyName("slow")]
    public bool Slow { get; init; }

    /// <summary>Tốc độ trung bình cửa sổ trượt của VÙNG.</summary>
    [JsonPropertyName("avgSpeed")]
    public double? AvgSpeed { get; init; }

    /// <summary>Tốc độ của lượt xe vừa quan sát — khác <see cref="AvgSpeed"/>; null khi do quét theo giờ.</summary>
    [JsonPropertyName("speed")]
    public double? Speed { get; init; }

    [JsonPropertyName("updateTime")]
    public DateTime UpdateTime { get; init; }
}
