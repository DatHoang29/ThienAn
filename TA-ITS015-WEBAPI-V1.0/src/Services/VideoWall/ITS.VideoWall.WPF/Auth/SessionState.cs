using System.Text.Json;

namespace Module.VideoWall.WPF.Auth;

/// <summary>
/// Author: Đạt
/// Description: Giữ phiên đăng nhập cho toàn ứng dụng (access token + refresh token).
/// Đọc/ghi từ nhiều luồng (UI thread và JwtAuthHandler) nên mọi truy cập đều qua lock.
/// Created date: 25/08/2026
/// </summary>
public sealed class SessionState
{
    /// <summary>Giá trị đặc biệt backend trả ở header access-token khi token đã bị vô hiệu.</summary>
    public const string InvalidTokenMarker = "invalid_token";

    private readonly object _gate = new();

    private string? _token;
    private string? _refreshToken;
    private DateTime? _refreshTokenExpiryTime;
    private string? _username;

    public string? Token
    {
        get
        {
            lock (_gate)
                return _token;
        }
    }

    public string? RefreshToken
    {
        get
        {
            lock (_gate)
                return _refreshToken;
        }
    }

    public string? Username
    {
        get
        {
            lock (_gate)
                return _username;
        }
    }

    /// <summary>Hạn của refresh token do backend cấp — hiển thị cho người vận hành biết khi nào phải đăng nhập lại.</summary>
    public DateTime? RefreshTokenExpiryTime
    {
        get
        {
            lock (_gate)
                return _refreshTokenExpiryTime;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            lock (_gate)
                return !string.IsNullOrWhiteSpace(_token);
        }
    }

    /// <summary>
    /// Access token đã quá hạn theo claim "exp"? Dùng để quyết định có gửi kèm header
    /// X-Authorization (refresh token) như interceptor của web Vue hay không.
    /// </summary>
    public bool IsAccessTokenExpired
    {
        get
        {
            var expiry = ReadExpiry(Token);
            return expiry is not null && DateTime.UtcNow >= expiry.Value;
        }
    }

    public void SetTokens(string? token, string? refreshToken, DateTime? refreshTokenExpiryTime = null, string? username = null)
    {
        lock (_gate)
        {
            _token = token;
            _refreshToken = refreshToken;
            _refreshTokenExpiryTime = refreshTokenExpiryTime ?? _refreshTokenExpiryTime;
            _username = username ?? _username;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _token = null;
            _refreshToken = null;
            _refreshTokenExpiryTime = null;
        }
    }

    /// <summary>
    /// Đọc claim "exp" của JWT (giây Unix) — tương đương decryptJWT + getJWTDate bên axios-utils.ts.
    /// Trả null khi token rỗng hoặc không giải mã được, để phía gọi coi như "chưa biết hạn".
    /// </summary>
    public static DateTime? ReadExpiry(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var segments = token.Split('.');
        if (segments.Length < 2)
            return null;

        try
        {
            var payload = Convert.FromBase64String(PadBase64Url(segments[1]));
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("exp", out var exp))
                return null;

            // BẮT BUỘC kiểm ValueKind trước: TryGetInt64 NÉM InvalidOperationException khi phần tử
            // không phải Number (chứ không trả false). Hàm này chạy ở MỌI request qua JwtAuthHandler
            // nên để lọt exception ở đây là gãy toàn bộ lời gọi API.
            if (exp.ValueKind != JsonValueKind.Number || !exp.TryGetInt64(out var seconds))
                return null;

            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }
        catch (Exception exception)
            when (exception is FormatException or JsonException or ArgumentOutOfRangeException)
        {
            // ArgumentOutOfRangeException: exp nằm ngoài khoảng DateTimeOffset biểu diễn được.
            return null;
        }
    }

    private static string PadBase64Url(string segment)
    {
        var normalized = segment.Replace('-', '+').Replace('_', '/');
        return (normalized.Length % 4) switch
        {
            2 => normalized + "==",
            3 => normalized + "=",
            _ => normalized,
        };
    }
}
