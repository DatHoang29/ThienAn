namespace ITS.VMS.Core.Messaging
{
    /// <summary>
    /// Ánh xạ mục "Nats" trong appsettings — cấu hình lắng nghe kênh điều khiển VMS và thực thi ra thiết bị.
    /// Dùng CÙNG hạ tầng NATS thật của hệ thống (giống ITS.Toll.Core / BE DataTransporter).
    /// Author: ChuongLP
    /// Created date: 22/09/2026
    /// </summary>
    public class VmsControlNatsOption
    {
        public const string SectionName = "Nats";

        /// <summary>Bật/tắt việc lắng nghe kênh điều khiển.</summary>
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
        public string ClientName { get; set; } = "ITS_VMS_SERVICE";

        /// <summary>Số giây chờ trước khi thử kết nối lại khi lỗi.</summary>
        public int RetryIntervalSeconds { get; set; } = 5;

        /// <summary>Kênh điều khiển VMS (dùng chung 2 chiều, phân biệt bằng field Type).</summary>
        public string ControlSubject { get; set; } = "ta.its.control.vms";

        /// <summary>
        /// Trần thời gian (giây) cho MỖI thiết bị. Quá hạn coi như thất bại thiết bị đó,
        /// không giữ cả gói. Nên nhỏ hơn timeout phía BE để BE luôn nhận được result.
        /// </summary>
        public int DeviceTimeoutSeconds { get; set; } = 25;

        /// <summary>Số thiết bị xử lý đồng thời tối đa (SDK native nặng, không nên để quá cao).</summary>
        public int MaxParallelDevice { get; set; } = 8;

        // ===== Heartbeat (giám sát dịch vụ) =====

        /// <summary>Bật/tắt gửi heartbeat định kỳ để FE giám sát dịch vụ Online/Offline.</summary>
        public bool HeartbeatEnabled { get; set; } = true;

        /// <summary>Subject phát heartbeat. Mặc định "ta.its.heartbeat.vms".</summary>
        public string HeartbeatSubject { get; set; } = "ta.its.heartbeat.vms";

        /// <summary>Chu kỳ gửi heartbeat (giây). Mặc định 30s.</summary>
        public int HeartbeatIntervalSeconds { get; set; } = 30;

        // ===== Xử lý file ảnh =====

        /// <summary>
        /// Thư mục gốc lưu trữ ảnh khi dịch vụ CHUNG máy chủ với BE (map đường dẫn tương đối BE gửi sang).
        /// Bỏ trống nếu khác máy — lúc đó BE phải gửi HTTP URL.
        /// </summary>
        public string? StorageBasePath { get; set; }

        /// <summary>Thư mục tạm để tải ảnh về khi Url là HTTP (tương đối tính từ thư mục chạy exe).</summary>
        public string? TempImageDir { get; set; } = "temp/vms_image";

        /// <summary>Số giờ giữ file ảnh tạm; cũ hơn sẽ bị dọn khi khởi động.</summary>
        public int TempImageRetainHours { get; set; } = 24;

        // ===== Driver thiết bị =====

        /// <summary>
        /// Thư mục chứa DLL driver theo hãng (Modules.Integration.VMS.Chainzone.dll ...).
        /// Tương đối tính từ thư mục chạy exe; bỏ trống = ngay cạnh exe.
        /// </summary>
        public string? VendorDllPath { get; set; }

        // ===== Xác thực =====

        /// <summary>"JWT" (creds file), "NKEY", "TOKEN", "USERPASS" hoặc "NONE".</summary>
        public string AuthMode { get; set; } = "JWT";

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
}
