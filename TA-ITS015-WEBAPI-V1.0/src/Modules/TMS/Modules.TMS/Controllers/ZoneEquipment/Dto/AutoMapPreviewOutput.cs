namespace Modules.TMS.Controllers.ZoneEquipment.Dto;

public class AutoMapPreviewOutput
{
    public string? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public string? ZoneCode { get; set; }
    public string? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentCode { get; set; }
    public int? KmNumber { get; set; }
    public int? MetNumber { get; set; }
    public string? LaneId { get; set; }
    public string? MatchReason { get; set; }
}
