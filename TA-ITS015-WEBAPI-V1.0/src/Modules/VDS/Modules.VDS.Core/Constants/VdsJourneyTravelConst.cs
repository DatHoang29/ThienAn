using Shared.Utility.Helpers;

namespace Modules.VDS.Core.Constants;

/// <summary>
/// Biên thời gian dự phòng cho đoạn A→B. Giá trị trên TmsTrafficSegment luôn được ưu tiên;
/// hai khoá vds_config_default chỉ thay thế trường bị thiếu hoặc không hợp lệ.
/// </summary>
public static class VdsJourneyTravelConst
{
    public const int CodeMinSeconds = 1;
    public const int CodeMaxSeconds = 3600;
    public const int MaxSupportedSeconds = 86400;

    public static int? ParseSeconds(string? raw)
    {
        var seconds = BaseHelper.DurationHp.TryParseSeconds(raw);
        return seconds is >= CodeMinSeconds and <= MaxSupportedSeconds ? seconds : null;
    }

    public static (int MinSeconds, int MaxSeconds) Resolve(string? minRaw, string? maxRaw)
    {
        var min = ParseSeconds(minRaw) ?? CodeMinSeconds;
        var max = ParseSeconds(maxRaw) ?? CodeMaxSeconds;
        return min <= max ? (min, max) : (CodeMinSeconds, CodeMaxSeconds);
    }
}
