namespace Module.CCTV.Controllers.CctvPTZ.Dto
{
    public class CctvActionOutput
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>Vị trí PTZ hiện tại (absolute).</summary>
    public class CctvPtzPositionOutput
    {
        public decimal Pan { get; set; }
        public decimal Tilt { get; set; }
        public decimal Focus { get; set; }
        public decimal Zoom { get; set; }
    }

    /// <summary>
    /// Giới hạn phần cứng PTZ trả về cho FE.
    /// Gồm 2 nhóm rõ ràng:
    ///   - Absolute: pan, tilt, fov, focus — dùng khi gọi /ptz/positions
    ///   - Speed:    panSpeed, tiltSpeed, zoomSpeed — dùng khi gọi /ptz/move
    /// Author: ChuongLP
    /// Date: 2026-06-10
    /// </summary>
    public class CctvPtzLimitsOutput
    {
        // ── Absolute position limits ───────────────────────────────────────────
        public decimal MinPan { get; set; }
        public decimal MaxPan { get; set; }
        public decimal MinTilt { get; set; }
        public decimal MaxTilt { get; set; }
        public decimal MinFov { get; set; }
        public decimal MaxFov { get; set; }
        public decimal MinFocus { get; set; }
        public decimal MaxFocus { get; set; }

        // ── Continuous speed limits ────────────────────────────────────────────
        public decimal MinPanSpeed { get; set; }
        public decimal MaxPanSpeed { get; set; }
        public decimal MinTiltSpeed { get; set; }
        public decimal MaxTiltSpeed { get; set; }
        public decimal MinZoomSpeed { get; set; }
        public decimal MaxZoomSpeed { get; set; }
    }

    public class CctvPtzPresetOutput
    {
        public string? PresetId { get; set; }
        public string? Name { get; set; }
    }

    public class CctvPtzTourOutput
    {
        public string? TourId { get; set; }
        public string? Name { get; set; }
    }
}