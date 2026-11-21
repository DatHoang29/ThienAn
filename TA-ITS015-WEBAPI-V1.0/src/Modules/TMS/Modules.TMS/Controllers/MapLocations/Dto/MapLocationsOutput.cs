
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.MapLocations.Dto;

public class MapLocationsOutput : TmsMapLocation
{
    public string GeoJsonContent { get; set; } // New property to hold file content

}


public class PageTmsMapLocationsOutput : TmsMapLocation
{
}