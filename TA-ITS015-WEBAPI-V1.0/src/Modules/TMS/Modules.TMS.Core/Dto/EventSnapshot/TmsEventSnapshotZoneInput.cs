namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Yêu cầu đọc danh sách thiết bị của một vùng cho snapshot sự kiện VDS, gọi qua bus.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventSnapshotZoneInput
{
    public string ZoneId { get; set; } = string.Empty;
}
