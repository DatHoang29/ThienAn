namespace Modules.TMS.Controllers.TrafficInfo.Dto;


/// <summary>
/// Input lấy thông tin traffic info
/// </summary>
public class TrafficInfoInput
{
    public List<string> ListZoneId { get; set; }
    public string EventTypeId { get; set; }
    public string EventTypeName { get; set; }
    public string IncidentId { get; set; }
    public string TrafficPlanId { get; set; }
    public string TrafficPlanName { get; set; }
    public DateTime StartDate { get; set; }
    public string State { get; set; }
}

/// <summary>
/// Input lấy thông tin danh sách sự cố trong vùng zone
/// </summary>
public class ListZoneIncidentInput
{

}
