using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.ZoneEquipment.Dto;

public class PageZoneEquipmentInput : BasePageInput
{
    public string? ZoneId { get; set; }
    public string? EquipmentId { get; set; }
    public string? Type { get; set; }
    public string? LaneId { get; set; }
}
