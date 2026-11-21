namespace ITS.FA.Core.Models;

/// <summary>
/// Loại điểm trong FaObjectData.TypeObject theo CODE chạy thật của dịch vụ cũ (Its.FireAlarm.Bussiness):
/// 1 = đầu báo, 2 = nút ấn. Các giá trị khác của enum cũ trùng nhau và không được code nào dùng
/// (comment DB ghi 1 = nút ấn, ngược code) — cần xác nhận trước khi thêm giá trị.
/// </summary>
public enum FaDeviceType
{
    Detector = 1,
    Button = 2,
}

/// <summary>Tham số của một lần báo/hết báo. Cũ: AlarmActivedParams.</summary>
public sealed class FaAlarmEvent
{
    public FaObjectData? Point { get; init; }

    public FaFireAlarmConfig? Door { get; init; }

    public bool IsActive { get; init; }
}

/// <summary>Sự cố vừa tạo cùng loại sự kiện đã tra được (null khi chưa khai EventTypeId).</summary>
public sealed record FaCreatedIncident(TmsIncident Incident, TmsEventType? EventType);
