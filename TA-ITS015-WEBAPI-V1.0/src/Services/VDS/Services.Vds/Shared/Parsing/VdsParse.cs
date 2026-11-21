using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Services.Vds.Shared.Parsing;

/// <summary>
/// Author: KienLT
/// Description: Các phép parse thuần dùng chung cho payload VDS; không phụ thuộc culture của máy chạy.
/// Created date: 25/08/2026
/// </summary>
public static partial class VdsParse
{
    /// <summary>Giữ các trường UTC trong JSON ở <see cref="DateTimeKind.Utc"/>.</summary>
    public static readonly JsonSerializerSettings JsonSettings = new()
    {
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        DateParseHandling = DateParseHandling.DateTime,
        NullValueHandling = NullValueHandling.Ignore,
    };

    [GeneratedRegex(@"-?\d+")]
    private static partial Regex IntRegex();

    /// <summary><c>"37km/h"</c> → 37; rỗng hoặc không có số → null.</summary>
    public static int? SpeedKmh(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var match = IntRegex().Match(value);
        return match.Success
               && int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    public static int? Int(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    public static double? Double(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    /// <summary>
    /// Giờ THIẾT BỊ trong payload (<c>device_time</c>/<c>start_time</c>/<c>end_time</c>) — chuỗi không
    /// kèm timezone nên trả về <see cref="DateTimeKind.Unspecified"/>.
    ///
    /// <b>Không dùng làm mốc thời gian tuyệt đối</b>: mọi cột thời gian ghi xuống đều là UTC, còn giá trị
    /// này là giờ treo trên tường của thiết bị. Chỉ dùng để tính KHOẢNG (end − start).
    /// </summary>
    public static DateTime? DeviceTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        string[] formats =
        {
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss",
        };

        return DateTime.TryParseExact(
            value.Trim(), formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? parsed
            : null;
    }
}
