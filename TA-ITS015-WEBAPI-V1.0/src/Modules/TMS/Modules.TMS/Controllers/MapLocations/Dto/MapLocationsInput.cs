using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.MapLocations.Dto;

public class PageTmsMapLocationsInput : BasePageInput
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string MapId { get; set; }
}

public class MapLocationsInput
{

    public string Name { get; set; }
    public string Code { get; set; }
    public string MapId { get; set; }
}

public class AddTmsMapLocationInput : TmsMapLocation
{
}

public class UpdateTmsMapLocationInput : AddTmsMapLocationInput
{

}

public class DeleteTmsMapLocationInput : BaseIdInput
{

}

public class MapLocationBasedOnMapIdInput
{
    public string MapId { get; set; }
}