using System.Globalization;
using System.Text.Json;
using Modules.TMS.Core.Constants;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Dựng Metadata (JSON) và Description của sự cố từ một bản tin VDS.
///              Dùng chung cho sự cố VDS tự tạo (Services.Vds IncidentPromoter) và sự cố người vận hành
///              tạo từ sự kiện cũ (TmsIncidentFromVdsEventService) để hai đường cho cùng nội dung.
///              Nhận giá trị trần thay vì entity VDS vì Modules.TMS.Core không tham chiếu Modules.VDS.Core.
/// Created date: 23/10/2026
/// </summary>
public static class IncidentVdsDetailHelper
{
    /// <summary>Metadata JSON; giữ AID, bỏ dần trường phụ nếu vượt giới hạn cột (512).</summary>
    public static string? Metadata(string? aidEventType, int? speedMin, int? speedMax, string? plate, double? speed,
        int? lengthMm, string? vehicleType, string? vehicleColor, string? vehicleSize, string? lane)
    {
        var data = new Dictionary<string, object>(StringComparer.Ordinal);
        var aid = aidEventType;
        if (!string.IsNullOrWhiteSpace(aid)) data[IncidentMetadataConst.AidEventType] = aid;
        if (speedMin is int min) data[IncidentMetadataConst.SpeedMin] = min;
        if (speedMax is int max) data[IncidentMetadataConst.SpeedMax] = max;

        if (!string.IsNullOrWhiteSpace(plate)) data[IncidentMetadataConst.VehiclePlate] = plate;
        if (speed is double value && double.IsFinite(value) && value >= 0) data[IncidentMetadataConst.VehicleSpeed] = value;
        if (lengthMm is int mm && mm > 0) data[IncidentMetadataConst.VehicleLength] = mm / 1000d;
        if (!string.IsNullOrWhiteSpace(vehicleType)) data[IncidentMetadataConst.VehicleType] = vehicleType;
        if (!string.IsNullOrWhiteSpace(vehicleColor)) data[IncidentMetadataConst.VehicleColor] = vehicleColor;
        if (!string.IsNullOrWhiteSpace(vehicleSize)) data[IncidentMetadataConst.VehicleSize] = vehicleSize;
        if (!string.IsNullOrWhiteSpace(lane)) data[IncidentMetadataConst.Lane] = lane;

        while (JsonSerializer.Serialize(data).Length > EntityConst.Length512 && data.Count > 1)
            data.Remove(data.Keys.Last());
        while (JsonSerializer.Serialize(data).Length > EntityConst.Length512 && !string.IsNullOrEmpty(aid))
        {
            aid = aid[..^1];
            data[IncidentMetadataConst.AidEventType] = aid;
        }
        return data.Count == 0 ? null : JsonSerializer.Serialize(data);
    }

    /// <summary>Mô tả ngắn: biển số, tốc độ, làn (bỏ phần không có).</summary>
    public static string? Description(string? plate, double? speed, string? lane)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(plate)) parts.Add($"Biển số {plate}");
        if (speed is double value && double.IsFinite(value) && value >= 0)
            parts.Add($"tốc độ {value.ToString("0.##", CultureInfo.GetCultureInfo("vi-VN"))} km/h");
        if (!string.IsNullOrWhiteSpace(lane)) parts.Add($"làn {lane}");
        return parts.Count == 0 ? null : string.Join(", ", parts);
    }
}
