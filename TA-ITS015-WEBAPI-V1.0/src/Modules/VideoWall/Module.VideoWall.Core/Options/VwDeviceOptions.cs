namespace Module.VideoWall.Core.Options
{
    /// <summary>
    /// Cấu hình phân hệ Video Wall — section "VideoWall". Dùng chung cho cả BE (Module.VideoWall) lẫn
    /// Worker (ITS.VideoWall), nhưng nguồn cấu hình khác nhau: Worker đọc thật từ appsettings.json của
    /// chính nó; BE (TAC_WebAPI) hiện KHÔNG có file cấu hình nào chứa section "VideoWall", nên luôn dùng
    /// giá trị mặc định trong code.
    /// </summary>
    public class VwDeviceOptions
    {
        public const string SectionName = "VideoWall";

        /// <summary>
        /// Tạm bỏ qua kiểm tra vùng lưới (Tầng 3) khi đang đấu nối thiết bị. CHỈ dùng ở Dev/Test —
        /// VwPermissionService từ chối cờ này khi ứng dụng chạy ở môi trường Production.
        /// Mặc định: false.
        /// </summary>
        public bool BypassPermission { get; set; } = false;

        /// <summary>
        /// BE tự ghi kịch bản đang phát (VwController.ActiveSceneId, cờ VwScene.ActiveScene) và bắn NATS
        /// "ta.its.data.videowall.scene" cho FE NGAY khi kích hoạt — như luồng cũ trước 29/09/2026 —
        /// thay vì chờ Worker ghi sau khi thiết bị chuyển thành công.
        /// Dùng để test BE + FE khi CHƯA có thiết bị. Mặc định: true (BE chưa có section "VideoWall").
        /// Khi đã có thiết bị và Worker: đặt false để chỉ Worker ghi trạng thái thật của tường.
        /// </summary>
        public bool ApplySceneOnServer { get; set; } = true;

        public VwDeviceConnectionOptions Device { get; set; } = new();
    }

    /// <summary>
    /// Thông tin kết nối tới bộ điều khiển vật lý. Các giá trị Ip, Port, Scheme, Account, Password
    /// dùng làm fallback khi bản ghi VwController chưa cấu hình.
    /// </summary>
    public class VwDeviceConnectionOptions
    {
        /// <summary>IP hoặc hostname thiết bị. Có thể kèm port dạng "10.10.9.236:8080".</summary>
        public string? Ip { get; set; }

        /// <summary>Port ISAPI. Bỏ trống dùng VwDeviceProfile.DefaultPort (80).</summary>
        public int? Port { get; set; }

        /// <summary>
        /// "http" hoặc "https". Bỏ trống dùng VwDeviceProfile.Scheme ("http").
        /// Lưu ý: https đòi hỏi thiết bị có chứng chỉ thật được cài trên máy chủ.
        /// Không bỏ qua kiểm tra chứng chỉ (DangerousAcceptAnyServerCertificateValidator).
        /// </summary>
        public string? Scheme { get; set; }

        public string? Account { get; set; }

        public string? Password { get; set; }

        /// <summary>
        /// Số hiệu tường ISAPI. Bỏ trống để tự dò tường có wallBindOutputStatus = "bound" —
        /// tường số 1 trên thiết bị thường là sandbox chưa gắn màn hình nên không mặc định là 1.
        /// </summary>
        public int? WallNo { get; set; }

        public int TimeoutSeconds { get; set; } = 8;

        /// <summary>
        /// Số lần thử lại tối đa khi gặp lỗi tạm thời (transient error / network timeout / 5xx).
        /// Mặc định 3 lần. Đặt 1 để không retry.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Độ trễ cơ sở (mili-giây) cho cơ chế exponential backoff khi retry.
        /// Mặc định 500ms.
        /// </summary>
        public int RetryBaseDelayMs { get; set; } = 500;

        /// <summary>
        /// Thời gian tối đa (giây) chờ Worker phản hồi telemetry xác nhận kích hoạt kịch bản
        /// trước khi fallback broadcast với cờ unconfirmed = true. Mặc định 8 giây.
        /// </summary>
        public int DeviceAckTimeoutSeconds { get; set; } = 8;

        /// <summary>
        /// Thời gian tối đa (giây) chờ phản hồi NATS request-reply cho các thao tác thiết bị thông thường.
        /// Mặc định 15 giây.
        /// </summary>
        public int NatsRequestTimeoutSeconds { get; set; } = 15;

        /// <summary>
        /// Thời gian tối đa (giây) chờ phản hồi NATS cho thao tác dựng kịch bản (DeviceSetupScene).
        /// Mặc định 60 giây.
        /// </summary>
        public int NatsSetupSceneTimeoutSeconds { get; set; } = 60;

        /// <summary>
        /// Thời gian tối đa (giây) chờ phản hồi NATS cho lệnh chuyển tiếp passthrough (DeviceSetupSendPassthrough).
        /// Mặc định 30 giây.
        /// </summary>
        public int NatsPassthroughTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Chu kỳ (giây) giữa mỗi lần Heartbeat định kỳ thăm dò trạng thái thiết bị (ScreenState / SignalStatus).
        /// Mặc định 20 giây (khoảng «10-30s» đề xuất từ họp 11/09/2026).
        /// </summary>
        public int HeartbeatIntervalSeconds { get; set; } = 20;

        /// <summary>
        /// Cấu hình profile phần cứng/nghiệp vụ cho model thiết bị.
        /// Bỏ trống (null hoặc <= 0) để dùng giá trị mặc định của profile.
        /// </summary>
        public VwDeviceProfileOptions Profile { get; set; } = new();
    }

    /// <summary>
    /// Các tham số profile model thiết bị có thể override qua section "VideoWall". Dùng chung cho cả BE
    /// (Module.VideoWall) lẫn Worker (ITS.VideoWall), nhưng nguồn cấu hình khác nhau: Worker đọc thật từ
    /// appsettings.json của chính nó; BE (TAC_WebAPI) hiện KHÔNG có file cấu hình nào chứa section "VideoWall",
    /// nên luôn dùng giá trị mặc định trong code.
    /// Toàn bộ các trường đều là nullable; null hoặc &lt;= 0 nghĩa là dùng giá trị mặc định từ code.
    /// </summary>
    public class VwDeviceProfileOptions
    {
        public string? Model { get; set; }
        public int? PanelWidthPx { get; set; }
        public int? PanelHeightPx { get; set; }
        public int? BaseOutputSize { get; set; }
        public int? MaxConsecutiveFailures { get; set; }
        public int? BlockMinutes { get; set; }
    }
}
