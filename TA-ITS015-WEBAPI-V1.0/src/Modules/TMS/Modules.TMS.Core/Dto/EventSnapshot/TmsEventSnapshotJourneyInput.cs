namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Bộ lọc đọc hành trình vượt tốc độ trong cửa sổ hiển thị cho snapshot sự kiện VDS, gọi qua bus.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventSnapshotJourneyInput
{
    public DateTime From { get; set; }
    public DateTime CreatedFrom { get; set; }
    public List<string>? ZoneCameras { get; set; }
    public string? EquipmentId { get; set; }
    public string? Direction { get; set; }
    public int MaxItems { get; set; }
}
