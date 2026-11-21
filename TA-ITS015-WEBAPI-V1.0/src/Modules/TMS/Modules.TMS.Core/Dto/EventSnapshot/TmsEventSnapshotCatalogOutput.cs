namespace Modules.TMS.Core.Dto.EventSnapshot;

/// <summary>
/// Author: KienLT
/// Description: Danh mục loại sự kiện đang bật và camera TMS chưa xoá cho snapshot sự kiện VDS.
/// Created date: 22/10/2026
/// </summary>
public sealed class TmsEventSnapshotCatalogOutput
{
    public List<TmsEventSnapshotTypeOutput> Types { get; set; } = [];
    public List<TmsEventSnapshotCameraOutput> Cameras { get; set; } = [];
}
