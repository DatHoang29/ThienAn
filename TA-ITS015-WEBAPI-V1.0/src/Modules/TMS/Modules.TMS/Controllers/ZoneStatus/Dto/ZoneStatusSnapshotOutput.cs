namespace Modules.TMS.Controllers.ZoneStatus.Dto;

/// <summary>
/// Author: KienLT
/// Description: Snapshot trạng thái vùng kèm ngưỡng quá hạn.
/// Created date: 08/09/2026
/// </summary>
public class ZoneStatusSnapshotOutput
{
    public DateTime ComputedAt { get; set; }
    public int StaleAfterSeconds { get; set; }
    public List<ZoneStatusSnapshotRow> Zones { get; set; } = new();
}

/// <summary>
/// Author: KienLT
/// Description: Dữ liệu SQL vùng; AverageSpeed giữ kiểu chuỗi, UpdateTime giữ giờ local hiện hữu.
/// Created date: 08/09/2026
/// </summary>
public class ZoneStatusSnapshotRow
{
    public string? ZoneId { get; set; }
    public string? Condition { get; set; }
    public string? AverageSpeed { get; set; }
    public DateTime? UpdateTime { get; set; }
    public bool Stale { get; set; }
}
