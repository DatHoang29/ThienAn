namespace Services.CctvProxy.Configuration.Options;

/// <summary>
/// Author: kien.le
/// Description: Cấu hình hành vi proxy (section "Service:Proxy").
/// Created date: 06/10/2026
/// </summary>
public sealed class ProxyOptions
{
    /// <summary>
    /// Origin FE được phép gọi (CORS + kiểm tra Origin khi bắt tay WebSocket).
    /// Rỗng = cho phép mọi origin (ticket vẫn bắt buộc).
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = [];

    /// <summary>Chu kỳ keep-alive (giây) cho WebSocket 2 phía.</summary>
    public int WebSocketKeepAliveSeconds { get; set; } = 20;

    /// <summary>Timeout (giây) khi kết nối tới NxVMS (bắt tay WS / nhận header HTTP).</summary>
    public int ConnectTimeoutSeconds { get; set; } = 15;

    /// <summary>Số giây chờ trước khi thử login lại sau khi login NxVMS thất bại (tránh spam login).</summary>
    public int LoginRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Thời gian (giây) dùng chung một ảnh snapshot live cho cùng camera + kích thước.
    /// Nhiều người xem cùng camera chỉ gọi NxVMS 1 lần mỗi chu kỳ. 0 = không cache.
    /// </summary>
    public int SnapshotCacheSeconds { get; set; } = 3;
}
