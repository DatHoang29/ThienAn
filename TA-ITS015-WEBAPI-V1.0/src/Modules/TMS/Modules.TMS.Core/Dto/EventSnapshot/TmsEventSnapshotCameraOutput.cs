namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Camera TMS (tên, tuyến, lý trình) dùng để gắn thông tin cho snapshot sự kiện VDS.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventSnapshotCameraOutput
{
    public string ID { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Location { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
}
