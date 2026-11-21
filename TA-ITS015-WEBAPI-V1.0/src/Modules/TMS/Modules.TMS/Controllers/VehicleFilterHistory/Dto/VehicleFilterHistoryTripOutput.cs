using Shared.Utility.Serialization;

namespace Modules.TMS.Controllers.VehicleFilterHistory.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một CHUYẾN lọc xe: các lượt cùng biển đi tiến theo tuyến trong khoảng nghỉ, kèm lộ trình camera.
/// Created date: 23/10/2026
/// </summary>
public sealed class VehicleFilterHistoryTripOutput
{
    /// <summary>Mã chuyến; dòng cũ chưa có chuyến dùng ID của chính lượt đó.</summary>
    public string? TripId { get; set; }
    public string? LicensePlate { get; set; }
    public string? Type { get; set; }

    /// <summary>Lượt đầu chuyến (UTC).</summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? FromTime { get; set; }

    /// <summary>Lượt cuối chuyến (UTC).</summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? ToTime { get; set; }

    /// <summary>Số lượt khớp trong chuyến.</summary>
    public int HitCount { get; set; }

    public string? Direction { get; set; }
    public string? LocationId { get; set; }
    public string? ZoneId { get; set; }

    /// <summary>Lộ trình: camera (<c>TmsEquipment.ID</c>) theo thời gian, đã gộp lượt liền nhau cùng camera.</summary>
    public List<string> EquipmentIds { get; set; } = new();
}
