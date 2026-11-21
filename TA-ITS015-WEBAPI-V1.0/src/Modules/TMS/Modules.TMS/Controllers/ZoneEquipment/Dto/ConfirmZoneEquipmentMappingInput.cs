namespace Modules.TMS.Controllers.ZoneEquipment.Dto;

public class ConfirmZoneEquipmentMappingInput
{
    public List<AddZoneEquipmentInput> Mappings { get; set; } = new();
    public string? ZoneId { get; set; }
    public bool ClearExistingMappings { get; set; } = false;
}
