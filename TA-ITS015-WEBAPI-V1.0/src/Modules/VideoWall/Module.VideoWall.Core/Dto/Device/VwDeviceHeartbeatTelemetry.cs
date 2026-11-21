using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.Device
{
    /// <summary>
    /// Description: Payload telemetry phát lên NATS khi Heartbeat phát hiện thay đổi trạng thái
    ///              màn hình (ScreenState) hoặc nguồn tín hiệu (SignalStatus) của một bộ điều khiển.
    ///              Chỉ chứa các mục thực sự thay đổi (delta), không phát lại toàn bộ mỗi chu kỳ.
    /// Created date: 12/09/2026
    /// </summary>
    public class VwDeviceHeartbeatTelemetry
    {
        public string ControllerId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<VwScreenStateChangeDto> Screens { get; set; } = [];
        public List<VwSourceSignalChangeDto> Sources { get; set; } = [];
    }

    /// <summary>
    /// Description: Delta thay đổi ScreenState của một màn hình trong telemetry heartbeat.
    /// Created date: 12/09/2026
    /// </summary>
    public class VwScreenStateChangeDto
    {
        public string ScreenId { get; set; } = string.Empty;
        public string? ScreenState { get; set; }
    }

    /// <summary>
    /// Description: Delta thay đổi SignalStatus của một nguồn tín hiệu trong telemetry heartbeat.
    /// Created date: 12/09/2026
    /// </summary>
    public class VwSourceSignalChangeDto
    {
        public string SourceId { get; set; } = string.Empty;
        public string? SignalStatus { get; set; }
    }
}
