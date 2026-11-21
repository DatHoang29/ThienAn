namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Danh sách mã thiết bị thuộc vùng cho snapshot sự kiện VDS.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventSnapshotZoneOutput
{
    public List<string> EquipmentIds { get; set; } = [];
}
