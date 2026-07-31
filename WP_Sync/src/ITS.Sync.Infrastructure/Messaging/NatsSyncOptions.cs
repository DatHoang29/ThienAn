using System.Collections.Generic;

namespace ITS.Sync.Infrastructure.Messaging;

/// <summary>
/// Ánh xạ mục "Nats" trong cấu hình. Tầng NATS này nằm TRONG ITS.Sync.Infrastructure
/// (không tách project riêng), dùng CÙNG hạ tầng NATS thật của hệ thống.
/// </summary>
public sealed class NatsSyncOptions
{
    public const string SectionName = "Nats";

    /// <summary>Bật/tắt việc lắng nghe tín hiệu đồng bộ từ NATS.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Địa chỉ NATS. Hỗ trợ "nats://host:4222" và WebSocket "ws://host:12219" / "wss://...".
    /// </summary>
    public string Url { get; set; } = "ws://115.78.1.139:12219";

    /// <summary>Tên client hiển thị trên NATS server.</summary>
    public string ClientName { get; set; } = "ITS.Sync";

    /// <summary>Số giây chờ trước khi thử kết nối lại khi lỗi.</summary>
    public int RetryIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Danh sách subject cần lắng nghe. Mỗi subject có thể gắn cứng tên bảng cần đồng bộ
    /// (ví dụ "ta.its.data.incident" → "TmsIncident"), nhờ đó ta tái sử dụng luôn các subject
    /// nghiệp vụ đang có mà KHÔNG cần bên publish đổi payload.
    /// </summary>
    public List<NatsSyncSubscription> Subscriptions { get; set; } = new();

    /// <summary>
    /// Danh sách bảng được phép đồng bộ qua tín hiệu. Để rỗng = cho phép tất cả bảng đã đăng ký.
    /// </summary>
    public List<string> AllowedTables { get; set; } = new();

    // ===== Xác thực =====

    /// <summary>"JWT" (creds file), "NKEY", "TOKEN", "USERPASS" hoặc "NONE".</summary>
    public string AuthMode { get; set; } = "JWT";

    /// <summary>
    /// File .creds chứa cả JWT và Seed (giống camera.creds của FE).
    /// Đường dẫn tương đối được tính từ thư mục chạy ứng dụng.
    /// </summary>
    public string? CredsFile { get; set; } = "Configuration/camera.creds";

    /// <summary>JWT và Seed khai trực tiếp (dùng khi không có file creds).</summary>
    public string? Jwt { get; set; }
    public string? Seed { get; set; }

    /// <summary>NKey public (chế độ NKEY).</summary>
    public string? NKey { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
}

/// <summary>Một subject cần lắng nghe và bảng tương ứng cần đồng bộ.</summary>
public sealed class NatsSyncSubscription
{
    /// <summary>
    /// Subject NATS, ví dụ "ta.its.data.incident". Có thể dùng wildcard của NATS
    /// ("ta.its.refresh.>") khi tên bảng nằm ở token cuối.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Bảng cần đồng bộ khi nhận tín hiệu ở subject này (ví dụ "TmsIncident").
    /// Để trống thì tên bảng được suy ra từ payload hoặc token cuối của subject.
    /// </summary>
    public string? Table { get; set; }

    /// <summary>"jetstream" hoặc "pubsub" (mặc định "pubsub").</summary>
    public string Mode { get; set; } = "pubsub";

    /// <summary>Tên JetStream stream. Để trống thì tự tra theo Subject. Chỉ dùng khi Mode = "jetstream".</summary>
    public string? StreamName { get; set; }

    /// <summary>Tên consumer bền để không nhận lại tín hiệu cũ sau restart (chỉ dùng cho jetstream).</summary>
    public string? DurableName { get; set; }

    /// <summary>Queue group cho pubsub — nhiều instance thì mỗi message chỉ 1 instance xử lý.</summary>
    public string? QueueGroup { get; set; }

    /// <summary>
    /// Gộp nhiều tín hiệu dồn dập thành một lần đồng bộ (giây). 0 = chạy ngay mỗi tín hiệu.
    /// Hữu ích khi FE cập nhật liên tục làm bắn nhiều event.
    /// </summary>
    public int DebounceSeconds { get; set; } = 2;
}
