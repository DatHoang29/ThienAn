namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Một loại sự kiện TMS đang bật trong danh mục snapshot sự kiện VDS.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventSnapshotTypeOutput
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Url { get; set; }
    public string? WarningType { get; set; }
    public int? Priority { get; set; }
}
