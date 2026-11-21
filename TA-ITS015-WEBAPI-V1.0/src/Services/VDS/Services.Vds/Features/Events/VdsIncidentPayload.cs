using System.Text.Json.Serialization;

namespace Services.Vds.Features.Events;

/// <summary>
/// Author: KienLT
/// Description: Bản tin sự cố đẩy lên <see cref="VdsSubjects.Incident"/> — subject dùng CHUNG cho mọi nguồn
/// sự cố của hệ thống, nên hình dạng phải giữ đúng bộ tên camelCase mà bên nhận đang đọc.
///
/// Chỉ phát khi thực sự TẠO MỚI sự cố. Bản tin trùng trong cửa sổ không phát: bên nhận sẽ hiểu mỗi bản tin
/// là một sự cố mới và tạo báo động trùng cho người trực.
/// Created date: 26/08/2026
/// </summary>
public sealed record VdsIncidentPayload
{
    [JsonPropertyName("incidentId")]
    public string? IncidentId { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("eventTypeCode")] public string? EventTypeCode { get; init; }
    [JsonPropertyName("eventTypeName")] public string? EventTypeName { get; init; }

    [JsonPropertyName("eventTypeId")]
    public string? EventTypeId { get; init; }

    /// <summary>Danh sách thiết bị XEM ĐƯỢC, ngăn cách bằng dấu phẩy.</summary>
    [JsonPropertyName("equipmentId")]
    public string? EquipmentId { get; init; }

    [JsonPropertyName("km")]
    public int? Km { get; init; }

    [JsonPropertyName("met")]
    public int? Met { get; init; }

    [JsonPropertyName("startDate")]
    public DateTime? StartDate { get; init; }

    [JsonPropertyName("state")] public string? State { get; init; }
    [JsonPropertyName("remark")] public string? Remark { get; init; }
    [JsonPropertyName("eventGroup")] public string? EventGroup { get; init; }
    [JsonPropertyName("changeType")] public string ChangeType { get; init; } = "created";
    [JsonPropertyName("warningType")] public string? WarningType { get; init; }
    [JsonPropertyName("icon")] public string? Icon { get; init; }
    [JsonPropertyName("priority")] public int? Priority { get; init; }
    [JsonPropertyName("zoneId")] public string? ZoneId { get; init; }

    public static VdsIncidentPayload From(TmsIncident incident) => new()
    {
        IncidentId = incident.ID,
        EventGroup = incident.EventGroup,
        State = incident.State,
        Remark = incident.Remark,
        ZoneId = incident.ZoneId,
        Code = incident.Code,
        Name = incident.Name,
        EventTypeId = incident.EventTypeId,
        EquipmentId = incident.EquipmentId,
        Km = incident.KmNumber,
        Met = incident.MetNumber,
        StartDate = incident.StartDate,
    };
}
