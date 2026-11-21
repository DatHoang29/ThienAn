namespace Modules.Media.Infrastructure.Config;

/// <summary>
/// Description: Cấu hình Media đọc từ bảng SysOpConfig (GroupCode = media_config).
/// Khóa theo quy ước:
///   media_provider_base_url, media_api_key, media_timeout_seconds,
///   media_cache_minutes, media_negative_cache_seconds, media_pending_cache_seconds,
///   media_{subsystem}_enabled | _endpoint,
///   media_incident_allowed_states | _picture_types.
/// Thiếu khóa nào thì dùng giá trị mặc định bên dưới.
/// Created date: 08/10/2026
/// </summary>
public sealed class MediaConfig
{
    private readonly IReadOnlyDictionary<string, string> _values;
    private readonly string? _itsBaseUrlFallback;

    /// <summary>Endpoint mặc định của ITS-Media theo phân hệ (khớp ItsMediaController).</summary>
    private static readonly Dictionary<string, string> DefaultEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        [MediaConst.Subsystem.Incident] = "/api/media/ItsMedia/GetIncident",
        [MediaConst.Subsystem.EventType] = "/api/media/ItsMedia/GetEventType",
        [MediaConst.Subsystem.EquipmentType] = "/api/media/ItsMedia/GetEquipmentType",
        [MediaConst.Subsystem.Vms] = "/api/media/ItsMedia/GetVms",
    };

    public MediaConfig(IReadOnlyDictionary<string, string> values, string? itsBaseUrlFallback)
    {
        _values = values;
        _itsBaseUrlFallback = itsBaseUrlFallback;
    }

    // ===================== Chung =====================

    /// <summary>Địa chỉ gốc site ITS. Trống thì lấy wp_config/baseUrl_its.</summary>
    public string ProviderBaseUrl
    {
        get
        {
            var value = Get("media_provider_base_url");
            return string.IsNullOrWhiteSpace(value) ? (_itsBaseUrlFallback ?? string.Empty).Trim() : value;
        }
    }

    public string ApiKey => Get("media_api_key");

    public int TimeoutSeconds => GetInt("media_timeout_seconds", 10, min: 1);

    /// <summary>[WP] Thời gian cache ảnh đã có (phút) trong BaseCacheService.</summary>
    public int CacheMinutes => GetInt("media_cache_minutes", 1440, min: 1);

    /// <summary>Thời gian nhớ tạm kết quả lỗi/không gọi được ITS (giây).</summary>
    public int NegativeCacheSeconds => GetInt("media_negative_cache_seconds", 30, min: 0);

    /// <summary>Thời gian nhớ tạm kết quả chưa sẵn sàng như PendingCapture (giây).</summary>
    public int PendingCacheSeconds => GetInt("media_pending_cache_seconds", 10, min: 0);

    // ===================== Theo phân hệ =====================

    public bool Enabled(string subsystem) => GetBool($"media_{subsystem}_enabled", true);

    public string Endpoint(string subsystem)
        => Get($"media_{subsystem}_endpoint", DefaultEndpoints.GetValueOrDefault(subsystem, string.Empty));

    // ===================== Riêng Incident =====================

    /// <summary>Các State của TmsIncident được phép xem ảnh trên WP (mặc định "3").</summary>
    public IReadOnlyList<string> IncidentAllowedStates => GetList("media_incident_allowed_states", "3");

    /// <summary>Các PictureType được phép (mặc định 3).</summary>
    public IReadOnlyList<int> IncidentPictureTypes
        => GetList("media_incident_picture_types", "3")
            .Select(x => int.TryParse(x, out var v) ? v : 0)
            .Where(v => v is >= 1 and <= 4)
            .ToList();


    // ===================== helper =====================

    private string Get(string code, string defaultValue = "")
        => _values.TryGetValue(code, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : defaultValue;

    private int GetInt(string code, int defaultValue, int min)
        => int.TryParse(Get(code), out var v) && v >= min ? v : defaultValue;

    private bool GetBool(string code, bool defaultValue)
    {
        var v = Get(code);
        if (v.Length == 0) return defaultValue;
        return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<string> GetList(string code, string defaultValue)
        => Get(code, defaultValue)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
