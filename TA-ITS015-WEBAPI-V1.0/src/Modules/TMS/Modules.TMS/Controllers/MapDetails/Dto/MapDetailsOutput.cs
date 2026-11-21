
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.MapDetails.Dto;

public class MapDetailsOutput : TmsMapDetail
{
    public string GeoJsonContent { get; set; } // New property to hold file content

}


public class PageTmsMapDetailsOutput : TmsMapDetail
{
}