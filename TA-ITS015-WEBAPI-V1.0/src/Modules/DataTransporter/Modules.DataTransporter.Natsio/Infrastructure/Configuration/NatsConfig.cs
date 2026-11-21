namespace Modules.DataTransporter.Natsio.Infrastructure.Configuration;

public class NatsRuntimeConfig
{
    public string? Url { get; set; }
    public string? ClientName { get; set; }
    public string? AuthMode { get; set; }

    // NKEY auth
    public string? NKey { get; set; }
    public string? Seed { get; set; }
    public string? SeedFile { get; set; }

    // Username/Password auth
    public string? Username { get; set; }
    public string? Password { get; set; }

    // TLS auth
    public string? TlsCertFile { get; set; }
    public string? TlsKeyFile { get; set; }
    public string? CaFile { get; set; }

    /// <summary>
    /// Bỏ qua xác thực chứng chỉ TLS (hữu ích cho self-signed certificate). Mặc định true khi dùng TLS
    /// </summary>
    public bool InsecureSkipVerify { get; set; } = true;

    // JWT auth
    public string? Jwt { get; set; }
    public string? JwtFile { get; set; }
    public string? UserSeed { get; set; }
    public string? UserSeedFile { get; set; }

    // Token auth
    public string? Token { get; set; }

    /// <summary>
    /// Thời gian giữa các lần retry kết nối (giây). Mặc định 5
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 5;
}

/// <summary>
/// Cấu hình JetStream stream
/// </summary>
public class NatsStreamConfig
{
    public string Name { get; set; } = string.Empty;
    public string Subjects { get; set; } = string.Empty;

    /// <summary>
    /// Có tạo stream khi khởi tạo không. false = chỉ dùng stream đã tồn tại. Mặc định false
    /// </summary>
    public bool InitStream { get; set; } = false;

    /// <summary>
    /// "file" hoặc "memory"
    /// </summary>
    public string Storage { get; set; } = "file";

    /// <summary>
    /// Thời gian giữ data. Hậu tố: s=giây, h=giờ, d=ngày. VD "30d", "5h", "60s". Không hậu tố = ngày. null = không giới hạn
    /// </summary>
    public string? MaxAge { get; set; }

    /// <summary>
    /// Danh sách subjects thuộc stream này
    /// </summary>
    public List<NatsSubjectConfig>? SubjectsList { get; set; }
}

/// <summary>
/// Cấu hình từng subject
/// </summary>
public class NatsSubjectConfig
{
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Tên stream chứa subject (dùng cho JetStream consume)
    /// </summary>
    public string? StreamName { get; set; }

    /// <summary>
    /// "jetstream" hoặc "pubsub". Mặc định "pubsub"
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Tên consumer cho JetStream (chỉ cần khi Mode = "jetstream")
    /// </summary>
    public string? ConsumerName { get; set; }
}
