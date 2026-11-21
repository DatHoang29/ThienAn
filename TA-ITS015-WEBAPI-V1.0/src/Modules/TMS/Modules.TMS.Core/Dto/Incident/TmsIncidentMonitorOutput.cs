using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Core.Dto.Incident;

/// <summary>
/// Author: KienLT
/// Description: Danh sách sự cố trên màn hình giám sát và số lượng theo trạng thái.
/// Created date: 19/10/2026
/// </summary>
public class TmsIncidentMonitorOutput
{
    public List<PageTmsIncidentOutput> Items { get; set; } = [];
    public TmsIncidentPageStatisticsOutput Statistics { get; set; } = new();

    public static TmsIncidentMonitorOutput From(List<PageTmsIncidentOutput> rows) => new()
    {
        Items = rows,
        Statistics = new()
        {
            Total = rows.Count,
            New = rows.Count(x => x.State == ((int)TmsIncidentStateEnum.Registration).ToString()),
            Executing = rows.Count(x => (x.State == ((int)TmsIncidentStateEnum.Verified).ToString() || x.State == ((int)TmsIncidentStateEnum.InProgress).ToString()))
        }
    };
}
