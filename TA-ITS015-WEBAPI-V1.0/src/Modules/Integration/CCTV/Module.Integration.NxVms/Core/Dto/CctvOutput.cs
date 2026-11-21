using System.Text.Json.Serialization;
namespace Module.Integration.CCTV.Core.Dto
{
    /// <summary>
    /// DTO output chung — prefix "Vms" để tránh xung đột tên với tầng Vms Controller.
    /// </summary>

    public class VmsDeviceOutput
    {
        public string? DeviceId { get; set; }
        public string? Name { get; set; }
        public string? Ip { get; set; }
        public string? DeviceState { get; set; }
        public string? Url { get; set; }
    }

    public class VmsActionOutput
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>Vị trí PTZ hiện tại của camera.</summary>
    public class VmsPtzPositionOutput
    {
        public decimal Pan { get; set; }
        public decimal Tilt { get; set; }
        public decimal Focus { get; set; }
        public decimal Zoom { get; set; }
    }

    /// <summary>Giới hạn pan/tilt/zoom của camera.</summary>
    public class VmsPtzLimitsOutput
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

    /// <summary>Thông tin một preset PTZ.</summary>
    public class VmsPtzPresetOutput
    {
        public string PresetId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Thông tin một tour PTZ.</summary>
    public class VmsPtzTourOutput
    {
        public string TourId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }


    /// <summary>
    /// Kết quả tạo preset — mang theo presetId + name do Vms sinh ra,
    /// để handler ghi vào Config TmsEquipment.
    /// Khác VmsActionOutput ở chỗ có thêm dữ liệu định danh.
    /// Author: ChuongLP
    /// Date: 2026-06-13
    /// </summary>
    public class VmsPresetAddOutput
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? PresetId { get; set; }
        public string? Name { get; set; }
    }


    /// <summary>Một đoạn footage đã ghi (đã convert về Unix ms).</summary>
    public class VmsFootageOutput
    {
        public string? DeviceId { get; set; }
        public long StartTimeMs { get; set; }
        public long DurationMs { get; set; }
    }
}