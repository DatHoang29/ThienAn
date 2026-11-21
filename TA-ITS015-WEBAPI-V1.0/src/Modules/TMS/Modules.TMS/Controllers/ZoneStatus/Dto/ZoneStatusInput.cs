using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.ZoneStatus.Dto;

public class ZoneStatusInput
{

}

public class AddZoneStatusInput : TmsZoneStatus
{
}

public class UpdateZoneStatusInput : AddZoneStatusInput
{

}

public class TmsZoneStatusIdListInput
{
    public string? ZoneIdList { get; set; }
}

