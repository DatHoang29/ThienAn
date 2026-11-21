using Shared.Utility.Counting;

namespace Modules.VDS.Infrastructure.Services.Dashboard;

/// <summary>
/// Author: KienLT
/// Description: Chính sách đếm của dashboard VDS (chu kỳ, loại bản tin được đếm, phạm vi camera).
/// Created date: 25/09/2026
/// </summary>
public sealed class DashboardPolicy : ICountQueryFilter, CountSliceFolder.IEquipmentScope
{
    public int PeriodMinutes { get; set; } = 5;
    public List<string> CountedEventTypes { get; set; } = ["Normal"];
    public bool CountsAllEventTypes { get; set; }
    public bool RequirePlate { get; set; }
    public List<string> CountingEquipmentIds { get; set; } = [];
    public string? ZoneId { get; set; }
    public string? Direction { get; set; }
    public string? EquipmentId { get; set; }
    public IReadOnlyList<string> EffectiveEventTypes => CountedEventTypes;
    public bool CountAllEventTypes => CountsAllEventTypes;
    public bool IsCountingEquipment(string? id) => CountingEquipmentIds.Count == 0 || CountingEquipmentIds.Contains(id ?? "", StringComparer.OrdinalIgnoreCase);
}
