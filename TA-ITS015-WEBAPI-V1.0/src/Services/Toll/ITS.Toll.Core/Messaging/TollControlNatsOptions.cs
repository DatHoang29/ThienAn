namespace ITS.Toll.Core.Messaging;

/// <summary>
/// Ánh xạ mục "Nats" trong appsettings — cấu hình lắng nghe/gửi tín hiệu lấy dữ liệu thu phí thủ công.
/// Dùng CÙNG hạ tầng NATS thật của hệ thống (giống ITS.Sync / BE DataTransporter).
/// Dùng chung cho cả ITS.Toll.Worker và ITS.Toll.Wpf.
/// Author: ChuongLP
/// Created date: 14/09/2026
/// </summary>
public sealed class TollControlNatsOptions
{
    public const string SectionName = "Nats";

    /// <summary>Bật/tắt việc lắng nghe tín hiệu điều khiển từ NATS.</summary>
    public bool Enabled { get; set; }

    /// <summary>Địa chỉ NATS. Hỗ trợ "tls://host:port", "nats://host:4222" và WebSocket "ws://host:12219" / "wss://...".</summary>
    public string Url { get; set; } = "ws://115.78.1.139:12219";

    /// <summary>Bỏ qua xác thực chứng chỉ TLS (hữu ích cho self-signed certificate hoặc dịch vụ thứ 3). Mặc định true khi dùng TLS.</summary>
    public bool InsecureSkipVerify { get; set; } = true;

    /// <summary>File chứng chỉ CA (nếu có, để trống nếu dùng InsecureSkipVerify).</summary>
    public string? CaFile { get; set; }

    /// <summary>File client cert khi xác thực mTLS.</summary>
    public string? TlsCertFile { get; set; }

    /// <summary>File client key khi xác thực mTLS.</summary>
    public string? TlsKeyFile { get; set; }

    /// <summary>Tên client hiển thị trên NATS server.</summary>
    public string ClientName { get; set; } = "ITS_TOLL_SERVICE";

    /// <summary>Số giây chờ trước khi thử kết nối lại khi lỗi.</summary>
    public int RetryIntervalSeconds { get; set; } = 5;

    /// <summary>Kênh điều khiển lấy dữ liệu thu phí thủ công (dùng chung 2 chiều, phân biệt bằng field Type).</summary>
    public string ControlSubject { get; set; } = "ta.its.control.toll";

    /// <summary>Số giờ lùi lại mặc định khi loại dữ liệu cần khoảng thời gian nhưng FE không gửi from/to.</summary>
    public int DefaultLookbackHours { get; set; } = 24;

    // ===== Heartbeat (giám sát dịch vụ) =====

    /// <summary>Bật/tắt gửi heartbeat định kỳ để FE giám sát dịch vụ Online/Offline.</summary>
    public bool HeartbeatEnabled { get; set; } = true;

    /// <summary>Subject phát heartbeat. Mặc định "ta.its.heartbeat.toll".</summary>
    public string HeartbeatSubject { get; set; } = "ta.its.heartbeat.toll";

    /// <summary>Chu kỳ gửi heartbeat (giây). Mặc định 30s.</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    // ===== Xác thực =====

    /// <summary>"JWT" (creds file), "NKEY", "TOKEN", "USERPASS" hoặc "NONE".</summary>
    public string AuthMode { get; set; } = "JWT";

    /// <summary>File .creds chứa cả JWT và Seed (đường dẫn tương đối tính từ thư mục chạy ứng dụng).</summary>
    public string? CredsFile { get; set; } = "Configuration/nats.creds";

    /// <summary>JWT và Seed khai trực tiếp (dùng khi không có file creds).</summary>
    public string? Jwt { get; set; }
    public string? Seed { get; set; }

    /// <summary>NKey public (chế độ NKEY).</summary>
    public string? NKey { get; set; }

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
}
