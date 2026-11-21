namespace Services.Shared.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Cấu hình heartbeat cho một dịch vụ tầng Services. Bind từ section "Heartbeat" trong
/// DataTransporter.json. HeartbeatPublisher đọc config này để publish định kỳ 1 gói { data = "ok" }
/// lên subject "ta.its.heartbeat.{phân hệ}" giúp FE giám sát dịch vụ Online/Offline.
/// Nếu <see cref="Enabled"/> = false hoặc <see cref="Subject"/> rỗng thì publisher không chạy (no-op).
/// Created date: 24/09/2026
/// </summary>
public class HeartbeatOptions
{
    /// <summary>Tên section trong file cấu hình.</summary>
    public const string SectionName = "Heartbeat";

    /// <summary>Bật/tắt heartbeat. Mặc định false (opt-in từng dịch vụ).</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Subject NATS để publish, ví dụ "ta.its.heartbeat.vms". Rỗng = không chạy.</summary>
    public string? Subject { get; set; }

    /// <summary>Chu kỳ gửi (giây). Mặc định 30s.</summary>
    public int IntervalSeconds { get; set; } = 30;

    /// <summary>Giá trị trường "data" trong gói tin. Mặc định "ok".</summary>
    public string Data { get; set; } = "ok";
}
