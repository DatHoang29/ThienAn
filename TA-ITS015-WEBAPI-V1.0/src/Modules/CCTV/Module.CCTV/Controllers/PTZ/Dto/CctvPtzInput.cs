using Module.CCTV.Controllers.CctvGetDevice;

namespace Module.CCTV.Controllers.CctvPTZ.Dto
{


    // ── Input ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Zoom liên tục (nhấn giữ → dừng khi thả).
    /// Gọi /ptz/move với panSpeed=0, tiltSpeed=0, chỉ zoomSpeed có giá trị.
    /// Author: ChuongLP
    /// Date: 2026-06-10
    /// </summary>
    public class CctvPtzZoomInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public ZoomDirection Direction { get; set; }
        public string? Vendor { get; set; }
        public decimal? Speed { get; set; }
        public bool IsHold { get; set; }   // ← true = đè giữ; false = nhích 1 nấc
    }

    /// <summary>
    /// Dừng mọi di chuyển PTZ (pan/tilt/zoom).
    /// Author: ChuongLP
    /// Date: 2026-06-10
    /// </summary>
    public class CctvPtzStopInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? Vendor { get; set; }
    }

    /// <summary>
    /// Di chuyển PTZ đến tọa độ tuyệt đối.
    /// Gọi /ptz/positions — khác với /ptz/move (continuous).
    /// Author: ChuongLP
    /// Date: 2026-06-10
    /// </summary>
    public class CctvPtzPositionInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }

        /// <summary>Góc ngang tuyệt đối: -1 (tận trái) → 1 (tận phải).</summary>
        public decimal Pan { get; set; }

        /// <summary>Góc dọc tuyệt đối: -1 (tận xuống) → 1 (tận lên).</summary>
        public decimal Tilt { get; set; }

        /// <summary>Field of View (zoom): 0 (zoom tối đa) → 1 (wide nhất).</summary>
        public decimal Zoom { get; set; }
        public string? Vendor { get; set; }
    }

    // ── Preset & Tour ─────────────────────────────────────────────────────────────

    public class CctvActivatePresetInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? PresetId { get; set; }
        public decimal? Speed { get; set; }
        public string? Vendor { get; set; }
    }

    public class CctvActivateTourInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? TourId { get; set; }
        public string? Vendor { get; set; }
    }

    // ── Query inputs ──────────────────────────────────────────────────────────────

    public class CctvGetPtzPositionInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? Vendor { get; set; }
    }

    public class CctvGetPtzLimitsInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? Vendor { get; set; }
    }

    // ── Preset & Tour ──────────────────────────────────────────────────────────────
    public class CctvAddPresetInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? PresetName { get; set; }
        public string? Vendor { get; set; }
    }

    public class CctvUpdatePresetInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? PresetId { get; set; }
        public string? PresetName { get; set; }
        public string? Vendor { get; set; }
    
        
    }

    public class CctvGetPresetInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? Vendor { get; set; }
    }

    public class CctvDeletePresetInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? PresetId { get; set; }
        public string? Vendor { get; set; }
    }

    public class CctvGetToursInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public string? Vendor { get; set; }
    }

    public class PtzSpeedConfig
    {
        public decimal Pan { get; set; }
        public decimal Speed { get; set; }
        public decimal Tilt{ get; set; }
        public decimal Focus { get; set; }
        public decimal Zoom { get; set; }
        /// <summary>
        /// Sau mỗi lệnh MovePtz/ZoomPtz, nếu FE không gọi Stop hoặc không gọi lại Move/Zoom,
        /// BE sẽ tự Stop sau thời gian này.
        /// Khuyến nghị: 2500 - 5000ms.
        /// </summary>
        public int ContinuousMoveAutoStopMs { get; set; }
        public int ZoomNudgeMs { get; set; }
        /// <summary>
        /// Thời gian nhích mặc định cho NudgePtz.
        /// Khuyến nghị: 150 - 300ms.
        /// </summary>
        public int NudgeDurationMs { get; set; }
    }

    /// <summary>
    /// Nhích PTZ một bước ngắn theo 1 trong 8 hướng.
    /// Dùng cho thao tác click nhẹ/tap: BE tự Move → Delay → Stop để tránh FE lệch timer
    /// hoặc mất lệnh Stop do mạng/browser.
    /// Author: ChuongLP
    /// Date: 2026-06-11
    /// </summary>
    public class CctvPtzNudgeInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public PtzDirection Direction { get; set; }
        public string? Vendor { get; set; }
        public decimal? Speed { get; set; }
        public bool IsHold { get; set; }
    }

    public class CctvPtzFocusInput
    {
        public string? DeviceId { get; set; }
        /// <summary>IP thiết bị (ưu tiên hơn DeviceId nếu có). BE tự quy đổi sang DeviceId để gọi NxVMS.</summary>
        public string? Ip { get; set; }
        public FocusDirection Direction { get; set; }
        public string? Vendor { get; set; }
        public decimal? Speed { get; set; }
    }

}
