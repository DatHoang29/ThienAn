namespace Modules.DataTransporter.Natsio.Infrastructure.Configuration;

/// <summary>
/// Parser cho config values dùng chung giữa NatsEventHandler và NatsClient
/// </summary>
public static class NatsConfigParser
{
    /// <summary>
    /// Parse MaxAge string. Hậu tố: s=giây, h=giờ, d=ngày. VD "30d", "5h", "60s". Không hậu tố = ngày. null = không giới hạn
    /// </summary>
    public static TimeSpan? ResolveMaxAge(string? maxAge)
    {
        if (string.IsNullOrWhiteSpace(maxAge)) return null;
        var s = maxAge.Trim().ToLower();
        var suffix = s[^1];
        var numPart = suffix is 's' or 'h' or 'd' ? s[..^1] : s;
        if (!double.TryParse(numPart, out var val)) return null;
        return suffix switch
        {
            's' => TimeSpan.FromSeconds(val),
            'h' => TimeSpan.FromHours(val),
            'd' => TimeSpan.FromDays(val),
            _ => TimeSpan.FromDays(val)
        };
    }
}
