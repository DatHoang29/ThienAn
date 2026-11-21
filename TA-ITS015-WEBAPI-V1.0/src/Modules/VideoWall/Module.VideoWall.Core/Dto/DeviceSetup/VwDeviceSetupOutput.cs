namespace Module.VideoWall.Core.Dto.DeviceSetup
{
    /// <summary>
    /// Một bước trong kịch bản đấu nối. Giữ nguyên XML gửi/nhận để đọc lại được khi thất bại —
    /// thông báo lỗi ISAPI nằm ở subStatusCode chứ không phải statusString.
    /// </summary>
    public class VwSetupSceneStep
    {
        public int Order { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Method { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;

        public bool Success { get; set; }

        /// <summary>Bước bị bỏ qua do cấu hình (DryRun, ResetWindows tắt...) — không tính là lỗi.</summary>
        public bool Skipped { get; set; }

        public int? HttpStatus { get; set; }

        public string? Message { get; set; }

        public long DurationMs { get; set; }

        public string? RequestXml { get; set; }

        public string? ResponseXml { get; set; }
    }

    /// <summary>Kết quả đẩy một cửa sổ của kịch bản xuống thiết bị.</summary>
    public class VwSetupWindowResult
    {
        public string? WindowSceneId { get; set; }

        public string? Label { get; set; }

        public string? SourceId { get; set; }

        public int? SignalNo { get; set; }

        /// <summary>Toạ độ tuyệt đối trên tường (pixel) lấy từ VwWindowScene.</summary>
        public int WallX { get; set; }

        public int WallY { get; set; }

        public int WallW { get; set; }

        public int WallH { get; set; }

        /// <summary>Toạ độ cục bộ uniformCoordinate đã quy đổi, chính là Rect gửi xuống thiết bị.</summary>
        public int LocalX { get; set; }

        public int LocalY { get; set; }

        public int LocalW { get; set; }

        public int LocalH { get; set; }

        public string? DeviceWindowId { get; set; }

        public bool Success { get; set; }

        public string? Message { get; set; }
    }

    /// <summary>Một tường logic đọc từ GET /ISAPI/DisplayDev/VideoWall.</summary>
    public class VwWallSummary
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? BindOutputStatus { get; set; }

        /// <summary>Tường được kịch bản chọn để thao tác.</summary>
        public bool Selected { get; set; }
    }

    /// <summary>Đối chiếu một giá trị đang lưu trong DB với giá trị thật đọc từ thiết bị.</summary>
    public class VwDeviceMismatch
    {
        public string? EntityId { get; set; }

        public string? EntityName { get; set; }

        public string? Field { get; set; }

        public string? DbValue { get; set; }

        public string? DeviceValue { get; set; }

        public string? Note { get; set; }
    }

    /// <summary>
    /// Kết quả đối chiếu/đồng bộ ngược kịch bản đang chạy thật trên thiết bị về CSDL.
    /// </summary>
    public class VwSyncActiveSceneOutput
    {
        public string? ControllerId { get; set; }

        public int? WallNo { get; set; }

        /// <summary>SceneId đang chạy thật trên thiết bị đọc được từ ISAPI.</summary>
        public int? DeviceActiveSceneId { get; set; }

        /// <summary>SceneId đang lưu trong CSDL trước khi đồng bộ.</summary>
        public string? DbActiveSceneId { get; set; }

        /// <summary>Trạng thái đồng bộ (khớp hoặc đã cập nhật xong).</summary>
        public bool Synced { get; set; }

        /// <summary>Có sự khác biệt giữa thiết bị và CSDL trước khi đồng bộ.</summary>
        public bool HasMismatch { get; set; }

        public VwDeviceMismatch? Mismatch { get; set; }

        public string? MatchedSceneId { get; set; }

        public string? MatchedSceneName { get; set; }

        public string? Message { get; set; }

        public List<VwWallSummary> Walls { get; set; } = [];

        public List<VwSetupSceneStep> Steps { get; set; } = [];
    }

    /// <summary>
    /// Kết quả tiếp nhận lệnh đấu nối thiết bị theo cơ chế Fire-and-Forget.
    /// </summary>
    public class VwDeviceSetupAcceptedOutput
    {
        public bool Accepted { get; set; } = true;
        public string MessageId { get; set; } = string.Empty;
    }
}
