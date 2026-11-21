using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.ZoneEquipment.Dto;

public class ZoneEquipmentOutput : TmsZoneEquipment
{
    public string? ZoneName { get; set; }
    public string? ZoneCode { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentCode { get; set; }
    public int? EquipmentKmNumber { get; set; }
    public int? EquipmentMetNumber { get; set; }
    public string? EquipmentTypeName { get; set; }
}
