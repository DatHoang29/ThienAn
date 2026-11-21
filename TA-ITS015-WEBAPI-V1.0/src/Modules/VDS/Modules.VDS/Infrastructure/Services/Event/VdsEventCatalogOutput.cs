using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.TMS.Core.Dto.EventSnapshot;
using Modules.VDS.Infrastructure.Services.Event.Rules;

namespace Modules.VDS.Infrastructure.Services.Event;

/// <summary>
/// Author: KienLT
/// Description: Dữ liệu danh mục snapshot lưu cache, không chứa khóa tuple hoặc bộ so sánh dictionary.
/// Created date: 22/10/2026
/// </summary>
public sealed class VdsEventCatalogOutput
{
    public TmsEventSnapshotCatalogOutput Tms { get; set; } = new();
    public List<VdsEventSnapshotRules.DisplayRow> Display { get; set; } = [];
    public List<CfgEventSnapshotNameOutput> Names { get; set; } = [];
}
