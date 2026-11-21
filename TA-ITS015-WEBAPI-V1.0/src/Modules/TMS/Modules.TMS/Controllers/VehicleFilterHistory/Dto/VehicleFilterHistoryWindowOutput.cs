using Shared.Utility.Serialization;

namespace Modules.TMS.Controllers.VehicleFilterHistory.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một dòng lịch sử đối chiếu biển số với danh sách lọc phương tiện.
/// Created date: 16/09/2026
/// </summary>
public sealed class VehicleFilterHistoryWindowOutput
{
    public string? Id { get; set; }
    public string? LicensePlate { get; set; }
    public string? Type { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(UtcDateTimeConverter))]
    public DateTime? DetectTime { get; set; }

    public string? Direction { get; set; }
    public string? ZoneId { get; set; }
    public string? LaneId { get; set; }
    public string? LocationId { get; set; }

    /// <summary>Camera bắt được xe (<c>TmsEquipment.ID</c>); dòng trước 23/10/2026 để trống.</summary>
    public string? EquipmentId { get; set; }

    /// <summary>Mã chuyến — các lượt cùng chuyến gộp một thẻ, dựng lộ trình A → B → C.</summary>
    public string? TripId { get; set; }
}
