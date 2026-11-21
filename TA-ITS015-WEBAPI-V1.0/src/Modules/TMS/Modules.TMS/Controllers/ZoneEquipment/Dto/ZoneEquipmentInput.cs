using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.ZoneEquipment.Dto;

public class ZoneEquipmentInput
{
    public string? ZoneId { get; set; }
    public string? EquipmentId { get; set; }
    public string? Type { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
    public string? LaneId { get; set; }
}
