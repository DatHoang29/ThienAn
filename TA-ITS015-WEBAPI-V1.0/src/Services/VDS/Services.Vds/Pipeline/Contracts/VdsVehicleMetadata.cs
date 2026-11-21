using Newtonsoft.Json;

namespace Services.Vds.Pipeline.Contracts;

/// <summary>
/// Author: KienLT
/// Description: Metadata phương tiện cần cho bản ghi raw; các trường vendor khác vẫn được bỏ qua an toàn.
/// Kích thước trong payload v1.0 dùng mét. PlateColor là màu biển, dấu gạch ngang khi không nhận dạng được.
/// LaneSpeedLimitMinKmh là ngưỡng chạy chậm (km/h) lúc sinh sự kiện; nhận số hoặc chuỗi và đọc qua VdsParse.Int.
/// Created date: 25/08/2026
/// </summary>
public sealed class VdsVehicleMetadata
{
    [JsonProperty("lane_no.")]
    public string? LaneNo { get; set; }

    [JsonProperty("detection_region")]
    public string? DetectionRegion { get; set; }

    [JsonProperty("device_time")]
    public string? DeviceTime { get; set; }

    [JsonProperty("license_plate")]
    public string? LicensePlate { get; set; }

    [JsonProperty("speed")]
    public string? Speed { get; set; }

    [JsonProperty("vehicle_type")]
    public string? VehicleType { get; set; }

    [JsonProperty("vehicle_color")]
    public string? VehicleColor { get; set; }

    [JsonProperty("vendor_event_type")]
    public string? VendorEventType { get; set; }

    [JsonProperty("vehicle_length")]
    public string? VehicleLength { get; set; }

    [JsonProperty("length")]
    public string? Length { get; set; }

    [JsonProperty("vehicle_height")]
    public string? VehicleHeight { get; set; }

    [JsonProperty("height")]
    public string? Height { get; set; }

    [JsonProperty("vehicle_width")]
    public string? VehicleWidth { get; set; }

    [JsonProperty("width")]
    public string? Width { get; set; }

    [JsonProperty("plate_color")]
    public string? PlateColor { get; set; }

    [JsonProperty("lane_speed_limit_min_kmh")]
    public string? LaneSpeedLimitMinKmh { get; set; }

    [JsonIgnore]
    public string Lane => FirstNonBlank(LaneNo, DetectionRegion) ?? "1";

    [JsonIgnore]
    public string? VehicleLengthRaw => FirstNonBlank(VehicleLength, Length);

    [JsonIgnore]
    public string? VehicleHeightRaw => FirstNonBlank(VehicleHeight, Height);

    [JsonIgnore]
    public string? VehicleWidthRaw => FirstNonBlank(VehicleWidth, Width);

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var trimmed = value.Trim();
            if (trimmed != "-") return trimmed;
        }

        return null;
    }
}
