using System.Text.Json.Serialization;

namespace ITS.FA.Core.Messaging;

/// <summary>
/// Bản tin sự cố trên <see cref="FaSubjects.Incident"/>. Subject dùng chung nên giữ đúng bộ tên camelCase bên nhận
/// đang đọc: gồm các trường của VdsIncidentPayload và các trường broadcast* của IncidentBroadcaster, nhờ đó FE vẫn
/// hiện thông báo như sự cố tạo từ web.
/// Thay gói { ActionType, Data, ListIncidentConfirm } của ITS008: bản cũ gửi cả danh sách, chuẩn mới FE tự tải lại.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public sealed record FaIncidentPayload
{
    [JsonPropertyName("incidentId")] public string? IncidentId { get; init; }
    [JsonPropertyName("code")] public string? Code { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("eventTypeId")] public string? EventTypeId { get; init; }
    [JsonPropertyName("eventTypeCode")] public string? EventTypeCode { get; init; }
    [JsonPropertyName("eventTypeName")] public string? EventTypeName { get; init; }
    [JsonPropertyName("eventGroup")] public string? EventGroup { get; init; }
    [JsonPropertyName("equipmentId")] public string? EquipmentId { get; init; }
    [JsonPropertyName("zoneId")] public string? ZoneId { get; init; }
    [JsonPropertyName("km")] public int? Km { get; init; }
    [JsonPropertyName("met")] public int? Met { get; init; }
    [JsonPropertyName("startDate")] public DateTime? StartDate { get; init; }
    [JsonPropertyName("state")] public string? State { get; init; }
    [JsonPropertyName("remark")] public string? Remark { get; init; }
    [JsonPropertyName("changeType")] public string ChangeType { get; init; } = "created";
    [JsonPropertyName("warningType")] public string? WarningType { get; init; }
    [JsonPropertyName("icon")] public string? Icon { get; init; }
    [JsonPropertyName("priority")] public int? Priority { get; init; }
    [JsonPropertyName("broadcastCode")] public string? BroadcastCode { get; init; }
    [JsonPropertyName("broadcastObject")] public string BroadcastObject { get; init; } = "Incident";
    [JsonPropertyName("broadcastMessage")] public FaBroadcastMessage? BroadcastMessage { get; init; }
    [JsonPropertyName("broadcastDateTime")] public long BroadcastDateTime { get; init; }
}

/// <summary>Cùng hình dạng broadcastMessage của IncidentBroadcaster (tên viết hoa chữ đầu là cố ý).</summary>
public sealed record FaBroadcastMessage(string Message, string? Account, string? Incident);

/// <summary>Bản tin trạng thái trên <see cref="FaSubjects.Data"/>. Cũ: { EquipmentType: "door", Data }.</summary>
public sealed record FaStatePayload
{
    /// <summary>Giá trị của trường <see cref="Type"/>.</summary>
    public const string TypeAlarm = "alarm";
    public const string TypeDoor = "door";

    [JsonPropertyName("type")] public required string Type { get; init; }
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("address")] public int? Address { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("deviceType")] public string? DeviceType { get; init; }
    [JsonPropertyName("km")] public int? Km { get; init; }
    [JsonPropertyName("met")] public int? Met { get; init; }
    [JsonPropertyName("location")] public string? Location { get; init; }
    [JsonPropertyName("isActive")] public bool IsActive { get; init; }
    [JsonPropertyName("incidentId")] public string? IncidentId { get; init; }
    [JsonPropertyName("time")] public DateTime Time { get; init; }
}
