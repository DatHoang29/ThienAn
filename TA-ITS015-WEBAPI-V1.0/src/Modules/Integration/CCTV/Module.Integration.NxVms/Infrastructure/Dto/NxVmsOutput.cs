using System.Text.Json.Serialization;

namespace Module.Integration.NxVms.Infrastructure.Dto
{
    public class NxVmsDeviceOutput
    {
        /// <summary>ID thiết bị do NxVMS cấp, dạng GUID.</summary>
        [JsonPropertyName("id")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// URL kết nối thiết bị, ví dụ: "rtsp://192.168.1.100:554/...".
        /// IP sẽ được trích xuất từ field này bởi NxVmsApiService.
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// IP được gán sau khi parse từ Url.
        /// Không có trong JSON gốc, được set thủ công trong GetDevicesAsync.
        /// </summary>
        public string? Ip { get; set; }

        [JsonPropertyName("deviceType")]
        public string? DeviceType { get; set; }

        /// <summary>Trạng thái: "Online" | "Offline" | "Unauthorized"</summary>
        [JsonPropertyName("status")]
        public string? DeviceState { get; set; }
    }

    public class NxVmsPtzPositionResponse
    {
        [JsonPropertyName("pan")] public decimal Pan { get; set; }
        [JsonPropertyName("tilt")] public decimal Tilt { get; set; }
        [JsonPropertyName("focus")] public decimal Focus { get; set; }
        [JsonPropertyName("zoom")] public decimal Zoom { get; set; }
    }

    /// <summary>
    /// Helper: map một object { "min": x, "max": y } trong JSON của NxVMS.
    /// Dùng cho tất cả các field limits (pan, tilt, fov, panSpeed...).
    /// Author: ChuongLP
    /// Date: 2026-06-10
    /// </summary>
    public class NxVmsPtzRange
    {
        [JsonPropertyName("min")] public decimal Min { get; set; }
        [JsonPropertyName("max")] public decimal Max { get; set; }
    }

    /// <summary>
    /// Map đúng cấu trúc JSON lồng nhau mà NxVMS trả về từ endpoint /ptz/limits:
    /// {
    ///   "pan":          { "min": -1, "max": 1 },
    ///   "tilt":         { "min": -1, "max": 1 },
    ///   "fov":          { "min":  0, "max": 1 },
    ///   "focus":        { "min":  0, "max": 1 },
    ///   "rotation":     { "min":  0, "max": 360 },
    ///   "panSpeed":     { "min": -1, "max": 1 },
    ///   "tiltSpeed":    { "min": -1, "max": 1 },
    ///   "zoomSpeed":    { "min": -1, "max": 1 },
    ///   "rotationSpeed":{ "min": -1, "max": 1 },
    ///   "focusSpeed":   { "min": -1, "max": 1 }
    /// }
    /// Author: ChuongLP
    /// Date: 2026-06-10
    /// </summary>
    public class NxVmsPtzLimitsResponse
    {
        // ── Absolute position limits ───────────────────────────────────────────
        [JsonPropertyName("pan")] public NxVmsPtzRange? Pan { get; set; }
        [JsonPropertyName("tilt")] public NxVmsPtzRange? Tilt { get; set; }
        [JsonPropertyName("fov")] public NxVmsPtzRange? Fov { get; set; }
        [JsonPropertyName("focus")] public NxVmsPtzRange? Focus { get; set; }
        [JsonPropertyName("rotation")] public NxVmsPtzRange? Rotation { get; set; }

        // ── Continuous speed limits ────────────────────────────────────────────
        [JsonPropertyName("panSpeed")] public NxVmsPtzRange? PanSpeed { get; set; }
        [JsonPropertyName("tiltSpeed")] public NxVmsPtzRange? TiltSpeed { get; set; }
        [JsonPropertyName("zoomSpeed")] public NxVmsPtzRange? ZoomSpeed { get; set; }
        [JsonPropertyName("rotationSpeed")] public NxVmsPtzRange? RotationSpeed { get; set; }
        [JsonPropertyName("focusSpeed")] public NxVmsPtzRange? FocusSpeed { get; set; }
    }

    public class NxVmsPtzPresetResponse
    {
        [JsonPropertyName("id")] public string? PresetId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public class NxVmsPtzTourResponse
    {
        [JsonPropertyName("id")] public string? TourId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    public class NxVmsErrorResponse
    {
        [JsonPropertyName("errorString")] public string? ErrorString { get; set; }
    }

    /// <summary>
    /// Response NxVMS sau khi POST tạo preset: { id, deviceId, name }.
    /// Chỉ cần id + name để ghi vào Config TmsEquipment.
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    public class NxVmsPtzPresetAddResponse
    {
        [JsonPropertyName("id")] public string? PresetId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    /// <summary>
    /// Một chunk footage thô từ NxVMS: GET /rest/v4/devices/{id}/footage.
    /// JSON hãng trả camelCase → map sang PascalCase.
    /// Author: ChuongLP
    /// Date: 2026-06-17
    /// </summary>
    public class NxVmsFootageResponse
    {
        [JsonPropertyName("deviceId")]
        public string? DeviceId { get; set; }

        [JsonPropertyName("startTimeMs")]
        public long StartTimeMs { get; set; }

        [JsonPropertyName("durationMs")]
        public long DurationMs { get; set; }
    }
}