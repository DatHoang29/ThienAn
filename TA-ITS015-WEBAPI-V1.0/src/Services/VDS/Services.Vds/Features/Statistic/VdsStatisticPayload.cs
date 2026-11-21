using System.Text.Json.Serialization;

namespace Services.Vds.Features.Statistic;

/// <summary>
/// Author: KienLT
/// Description: Bản tin thống kê chu kỳ đẩy lên <see cref="VdsSubjects.Statistic"/>.
///
/// <b>Tên trường giữ nguyên bộ tên PascalCase của pipeline cũ</b> (kể cả <c>IpAddress</c> chứ không phải
/// <c>CameraIp</c>, và <c>SVehicleNumber</c> chứ không phải <c>ShortVehicle</c>): bản tin cũ sinh từ
/// object ẩn danh với property PascalCase nên đó chính là tên trên dây. Đổi tên ở đây là buộc bên nhận
/// phải sửa.
/// Created date: 26/08/2026
/// </summary>
public sealed record VdsStatisticPayload
{
    [JsonPropertyName("VendorEventId")]
    public long VendorEventId { get; init; }

    [JsonPropertyName("EquipmentId")]
    public string? EquipmentId { get; init; }

    [JsonPropertyName("IpAddress")]
    public string? IpAddress { get; init; }

    [JsonPropertyName("FromTime")]
    public DateTime FromTime { get; init; }

    [JsonPropertyName("ToTime")]
    public DateTime? ToTime { get; init; }

    [JsonPropertyName("PeriodMinutes")]
    public short? PeriodMinutes { get; init; }

    [JsonPropertyName("TotalVehicleNumber")]
    public int? TotalVehicleNumber { get; init; }

    [JsonPropertyName("AvgSpeed")]
    public double? AvgSpeed { get; init; }

    [JsonPropertyName("LaneCount")]
    public int LaneCount { get; init; }

    [JsonPropertyName("SVehicleNumber")]
    public int? SVehicleNumber { get; init; }

    [JsonPropertyName("MVehicleNumber")]
    public int? MVehicleNumber { get; init; }

    [JsonPropertyName("LVehicleNumber")]
    public int? LVehicleNumber { get; init; }

    [JsonPropertyName("OVehicleNumber")]
    public int? OVehicleNumber { get; init; }

    public static VdsStatisticPayload From(VdsTrafficStatistic entity, int laneCount) => new()
    {
        VendorEventId = entity.VendorEventId,
        EquipmentId = entity.EquipmentId,
        IpAddress = entity.CameraIp,
        FromTime = entity.FromTime,
        ToTime = entity.ToTime,
        PeriodMinutes = entity.PeriodMinutes,
        TotalVehicleNumber = entity.TotalVehicle,
        AvgSpeed = entity.AvgSpeed,
        LaneCount = laneCount,
        SVehicleNumber = entity.ShortVehicle,
        MVehicleNumber = entity.MediumVehicle,
        LVehicleNumber = entity.LongVehicle,
        OVehicleNumber = entity.OtherVehicle,
    };
}
