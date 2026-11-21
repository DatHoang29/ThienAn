using Modules.VDS.Core.Constants;
using Shared.Utility.Counting;
namespace Modules.VDS.Controllers.TrafficSummary.Dto;

/// <summary>
/// Author: KienLT
/// Description: DTO đầu vào VDS, giữ nguyên contract và validation hiện hữu.
/// Created date: 08/09/2026
/// </summary>
public class TrafficSummaryInput : VdsReadInput
{
    public string PeriodType { get; set; } = VdsSummaryPeriodConst.Hour;
    public string ScopeType { get; set; } = TrafficCountScopes.Equipment;
    public string? ScopeId { get; set; }
}
