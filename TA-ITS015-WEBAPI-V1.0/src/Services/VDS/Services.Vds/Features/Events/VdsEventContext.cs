using Services.Vds.Shared.Domain;
using Services.Vds.Configuration;
using Services.Vds.Pipeline.Contracts;
using Services.Vds.Catalog;

namespace Services.Vds.Features.Events;

/// <summary>
/// Author: KienLT
/// Description: Ngữ cảnh xử lý một bản tin, truyền cho <see cref="VdsEventRules.Decide"/>.
///
/// Chỉ mang đúng những gì luật cần, không mang cả cấu hình service: luật sự kiện là hàm thuần, không nên
/// thấy tham số DB hay media.
/// Created date: 26/08/2026
/// </summary>
/// <param name="Row">Bản ghi dữ liệu thô đã map — nguồn của loại bản tin, tốc độ, hướng, biển số, làn.</param>
/// <param name="Camera">Camera đã tra từ danh mục — nguồn của vị trí và hướng tuyến.</param>
/// <param name="Events">Tham số sự kiện đang hiệu lực.</param>
/// <param name="SpeedRanges">
/// Ngưỡng theo hướng tuyến, giữ cho các caller hiện tại. Rule API v1.8 tin phân loại
/// Speeding/Slow Moving của AID và không dùng ngưỡng này để loại bản tin.
/// </param>
public sealed record VdsEventContext(
    VdsSseEnvelope Envelope,
    VdsVehicleMetadata? Metadata,
    VdsTrafficData Row,
    CameraInfo Camera,
    EventOptions Events,
    IReadOnlyDictionary<string, SpeedRange> SpeedRanges)
{
    /// <summary>Ngưỡng tốc độ của một hướng; hướng rỗng, chưa khai, hoặc khai sai ⇒ null.</summary>
    public SpeedRange? SpeedRangeOf(string? directionId)
    {
        if (string.IsNullOrWhiteSpace(directionId)) return null;
        if (!SpeedRanges.TryGetValue(directionId.Trim(), out var range)) return null;

        // Khai nửa vời (min > max, số không dương) ⇒ coi như chưa khai: thà không báo vi phạm còn hơn báo
        // theo ngưỡng bịa.
        return range.IsValid ? range : null;
    }
}

/// <summary>
/// Ba dạng kết quả của luật sự kiện: bỏ qua / là sự kiện / là sự kiện và đề nghị nâng sự cố. Giá trị THUẦN.
/// </summary>
public sealed record EventDecision
{
    public bool IsSkip { get; private init; }

    public string? SkipReason { get; private init; }

    /// <summary>
    /// Slug ổn định của điều kiện, khớp <c>TmsEventType.Code</c> đã seed trong danh mục. Cố ý KHÔNG đổi
    /// theo tên loại để giữ liên tục với dữ liệu cũ.
    /// </summary>
    public string ConditionCode { get; private init; } = string.Empty;

    public bool Promote { get; private init; }

    /// <summary>Định danh phụ để tính khoá chống trùng (biển số, làn, chiều vi phạm...).</summary>
    public string? SubKey { get; init; }

    public static EventDecision Skip(string reason) => new() { IsSkip = true, SkipReason = reason };

    public static EventDecision Event(string conditionCode) => new() { ConditionCode = conditionCode };

    public EventDecision PromoteToIncident() => this with { Promote = true };
}
