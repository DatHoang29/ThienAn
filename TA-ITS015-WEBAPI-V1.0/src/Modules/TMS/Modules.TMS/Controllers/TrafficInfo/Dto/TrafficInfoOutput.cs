namespace Modules.TMS.Controllers.TrafficInfo.Dto
{
    /// <summary>
    /// Output trả thông tin traffic info
    /// </summary>
    public class TrafficInfoOutput
    {

    }
}


/// <summary>
/// Output lấy thông tin danh sách sự cố trong vùng zone
/// </summary>
public class ListZoneIncidentOutput
{
    public string? EventGroup { get; set; }
    public List<string>? ListZoneId { get; set; }
    public string? EventTypeId { get; set; }
    public string? EventTypeName { get; set; }
    public string? IncidentId { get; set; }
    public string? TrafficPlanId { get; set; }
    public string? TrafficPlanName { get; set; }
    public DateTime StartDate { get; set; }
    public string? State { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public string? Direction { get; set; }
}
