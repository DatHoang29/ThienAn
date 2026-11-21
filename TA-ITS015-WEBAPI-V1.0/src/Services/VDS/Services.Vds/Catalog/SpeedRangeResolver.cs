using Services.Vds.Configuration;
using Services.Vds.Shared.Domain;

namespace Services.Vds.Catalog;

/// <summary>Ngưỡng tốc độ đã phân giải lấy từ đâu — để log nói được vì sao camera nhận ngưỡng này.</summary>
public enum SpeedRangeSource
{
    /// <summary>Cả hai giá trị lấy từ mặc định theo hướng (<c>vds_route_{hướng}_min/max_speed</c>).</summary>
    Direction,

    /// <summary>Cả hai giá trị lấy từ vùng (<c>TmsZone.MinSpeed/MaxSpeed</c>).</summary>
    Zone,

    /// <summary>Vùng khai một giá trị, giá trị còn lại lấy mặc định theo hướng.</summary>
    ZoneWithDirectionFallback,
}

/// <param name="Range">Ngưỡng dùng được (<see cref="SpeedRange.IsValid"/>).</param>
/// <param name="ZoneId">Vùng đã tra; null khi camera không thuộc vùng nào.</param>
/// <param name="Source">Nguồn của hai giá trị.</param>
public sealed record ResolvedSpeedRange(SpeedRange Range, string? ZoneId, SpeedRangeSource Source)
{
    public override string ToString() => Source switch
    {
        SpeedRangeSource.Zone => $"{Range} (vùng {ZoneId})",
        SpeedRangeSource.ZoneWithDirectionFallback => $"{Range} (vùng {ZoneId} + mặc định hướng)",
        _ => $"{Range} (mặc định hướng)",
    };
}

/// <summary>
/// Author: KienLT
/// Description: NƠI DUY NHẤT quyết định ngưỡng tốc độ min/max của một camera VDS hoặc một vùng.
///
/// <para><b>Thứ tự ưu tiên, xét TỪNG giá trị:</b>
/// <list type="number">
/// <item>Giá trị khai trên vùng (<c>TmsZone.MinSpeed</c> / <c>MaxSpeed</c>).</item>
/// <item>Thiếu thì lấy mặc định theo hướng trong <c>VdsConfig</c> (<c>vds_route_{hướng}_min_speed</c> /
/// <c>_max_speed</c>). Hướng của VÙNG là <c>TmsZone.LaneId</c> (vùng không khai mới dùng hướng camera);
/// riêng <see cref="ForCamera"/> luôn dùng hướng của camera.</item>
/// </list>
/// Camera dùng VÙNG CHÍNH (<see cref="ZoneManager.PrimaryZoneOf"/>) vì một camera chỉ có một bộ ngưỡng làn
/// trên AID, trong khi quan hệ camera-vùng là n-n.</para>
///
/// <para>Ghép xong mà không hợp lệ (thiếu một vế, hoặc min &gt; max do vùng khai lệch với hướng) ⇒ null:
/// thà bỏ qua còn hơn đẩy xuống AID một ngưỡng tự chế.</para>
///
/// Dùng chung cho đồng bộ ngưỡng AID, đánh giá vùng chậm theo tốc độ và bộ mô phỏng của harness — ba nơi
/// này mà mỗi nơi tự tra thì sớm muộn lệch nhau.
/// Created date: 07/10/2026
/// </summary>
public sealed class SpeedRangeResolver
{
    private readonly ZoneManager _zones;
    private readonly IVdsConfigManager _config;

    public SpeedRangeResolver(ZoneManager zones, IVdsConfigManager config)
    {
        _zones = zones;
        _config = config;
    }

    /// <summary>
    /// Ngưỡng của camera theo vùng chính của nó; giá trị vùng không khai lấy mặc định theo hướng CỦA CAMERA
    /// (không theo <c>LaneId</c> của vùng): ngưỡng làn AID gắn với hướng camera đang đo.
    /// </summary>
    public ResolvedSpeedRange? ForCamera(CameraInfo camera)
    {
        var zoneId = ZoneOf(camera);
        return Combine(zoneId, _zones.ProfileOf(zoneId)?.Limit, DirectionRangeOf(camera.Direction));
    }

    /// <summary>Vùng quyết định ngưỡng của camera: vùng chính; camera không thuộc vùng nào ⇒ null.</summary>
    public string? ZoneOf(CameraInfo camera) => _zones.PrimaryZoneOf(camera.EquipmentId);

    /// <summary>
    /// Hướng dùng để lấy mặc định cho vùng: <c>TmsZone.LaneId</c>; vùng không khai hướng mới dùng
    /// <paramref name="fallbackDirection"/> (thường là hướng camera).
    /// </summary>
    public string? DirectionOfZone(string? zoneId, string? fallbackDirection)
        => DirectionOf(_zones.ProfileOf(zoneId), fallbackDirection);

    /// <summary>
    /// Ngưỡng đủ cặp của một vùng; giá trị vùng không khai lấy mặc định theo hướng của vùng
    /// (<see cref="DirectionOfZone"/>). Đọc hồ sơ vùng MỘT lần ⇒ ngưỡng và hướng cùng một phiên bản danh mục.
    /// </summary>
    public ResolvedSpeedRange? ForZone(string? zoneId, string? fallbackDirection)
    {
        var profile = _zones.ProfileOf(zoneId);
        return Combine(zoneId, profile?.Limit, DirectionRangeOf(DirectionOf(profile, fallbackDirection)));
    }

    /// <summary>
    /// CHỈ ngưỡng tối thiểu của vùng (km/h): <c>zone.Min ?? mặc định hướng.Min</c>. Khác <see cref="ForZone"/>,
    /// KHÔNG đòi vùng khai đủ cặp: vùng chỉ khai min, hoặc min vùng ghép với max hướng ra min &gt; max, vẫn có
    /// ngưỡng để đánh giá chậm. Riêng mặc định theo hướng thì chỉ có khi CẶP hướng hợp lệ — OpConfig loại hướng
    /// thiếu max hoặc min &gt; max trước khi vào <c>SpeedRangeByDirection</c> (quy ước: mặc định hướng luôn khai
    /// đủ cặp). Không có giá trị dương nào ⇒ null. Đọc hồ sơ vùng MỘT lần như <see cref="ForZone"/>.
    /// </summary>
    public int? MinSpeedOfZone(string? zoneId, string? fallbackDirection)
    {
        var profile = _zones.ProfileOf(zoneId);
        return ToKmh(profile?.Limit?.Min)
               ?? PositiveOrNull(DirectionRangeOf(DirectionOf(profile, fallbackDirection))?.Min);
    }

    /// <summary>Mặc định theo hướng đang hiệu lực; hướng rỗng, chưa khai hoặc khai sai ⇒ null.</summary>
    public SpeedRange? DirectionRangeOf(string? directionId)
    {
        if (string.IsNullOrWhiteSpace(directionId)) return null;
        return _config.Current.SpeedRangeByDirection.TryGetValue(directionId.Trim(), out var range) && range.IsValid
            ? range
            : null;
    }

    private static string? DirectionOf(ZoneSpeedProfile? profile, string? fallbackDirection)
        => profile?.Direction ?? fallbackDirection;

    private static int? PositiveOrNull(int? value) => value is > 0 ? value : null;

    /// <summary>Ghép ngưỡng vùng với mặc định hướng theo từng giá trị. Hàm thuần.</summary>
    public static ResolvedSpeedRange? Combine(string? zoneId, ZoneSpeedLimit? zone, SpeedRange? direction)
    {
        var zoneMin = ToKmh(zone?.Min);
        var zoneMax = ToKmh(zone?.Max);

        var min = zoneMin ?? direction?.Min;
        var max = zoneMax ?? direction?.Max;
        if (min is null || max is null) return null;

        var range = new SpeedRange(min.Value, max.Value);
        if (!range.IsValid) return null;

        var source = (zoneMin, zoneMax) switch
        {
            (not null, not null) => SpeedRangeSource.Zone,
            (null, null) => SpeedRangeSource.Direction,
            _ => SpeedRangeSource.ZoneWithDirectionFallback,
        };

        return new ResolvedSpeedRange(range, zoneId?.Trim(), source);
    }

    /// <summary>
    /// Cột vùng là số thực, ngưỡng làn AID là số nguyên km/h ⇒ làm tròn. Không dương ⇒ coi như chưa khai.
    /// </summary>
    private static int? ToKmh(double? value)
    {
        if (value is not double v || v <= 0) return null;
        var rounded = (int)Math.Round(v, MidpointRounding.AwayFromZero);
        return rounded > 0 ? rounded : null;
    }
}
