namespace ITS.Barrier.Core.Messaging;

/// <summary>
/// Cấu hình kết nối NATS cho dịch vụ điều khiển Barrier.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public class BarrierControlNatsOption
{
    public const string SectionName = "Nats";

    /// <summary>Bật/tắt việc lắng nghe kênh điều khiển.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Địa chỉ NATS.</summary>
    public string Url { get; set; } = "nats://127.0.0.1:4222";

    /// <summary>Tên client hiển thị trên NATS server.</summary>
    public string ClientName { get; set; } = "ITS_BARRIER_SERVICE";

    /// <summary>Số giây chờ trước khi thử kết nối lại khi lỗi.</summary>
    public int RetryIntervalSeconds { get; set; } = 5;

    /// <summary>Kênh điều khiển Barrier (dùng chung 2 chiều, phân biệt bằng field Type).</summary>
    public string ControlSubject { get; set; } = "ta.its.control.barrier";

    /// <summary>Trần thời gian (giây) cho MỖI thiết bị.</summary>
    public int DeviceTimeoutSeconds { get; set; } = 10;

    /// <summary>Số thiết bị xử lý đồng thời tối đa.</summary>
    public int MaxParallelDevice { get; set; } = 4;

    // ===== Xác thực =====

    /// <summary>"NONE", "JWT" (creds file), "NKEY", "TOKEN", hoặc "USERPASS".</summary>
    public string AuthMode { get; set; } = "NONE";

    /// <summary>File .creds chứa cả JWT và Seed (đường dẫn tương đối tính từ thư mục chạy exe).</summary>
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
