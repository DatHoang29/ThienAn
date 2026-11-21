using Module.CCTV.Controllers.CctvPTZ.Dto;
using System.Text.Json.Serialization;

namespace Module.CCTV.Infrastructure.Dto;

public class CctvDeviceConfig
{
    /// <summary>
    /// Danh sách preset cache để FE render + activate nhanh.
    /// Mặc định rỗng (thiết bị chưa tạo preset nào).
    /// </summary>
    [JsonPropertyName("presets")]
    public List<CctvPresetItem> Presets { get; set; } = new();

    // ── Để chỗ cho PTZ default (làm sau) ──────────────────────────────────────
    // [JsonPropertyName("ptz")] public PtzSpeedConfig? Ptz { get; set; }

    /// <summary>
    /// Cấu hình PTZ default riêng theo thiết bị.
    /// Dùng làm fallback khi OpConfig {vendor}_ptz_config KHÔNG có key.
    /// Null → thiết bị chưa cấu hình PTZ riêng.
    /// Author: ChuongLP
    /// </summary>
    [JsonPropertyName("ptz")]
    public PtzSpeedConfig? Ptz { get; set; }
}

public class CctvPresetItem
{
    [JsonPropertyName("presetId")]
    public string? PresetId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}