using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.MapDetails.Dto;

public class PageTmsMapDetailsInput : BasePageInput
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string MapLocationId { get; set; }
}

public class MapDetailsInput
{

    public string Name { get; set; }
    public string Code { get; set; }
    public string MapLocationId { get; set; }
}

public class AddTmsMapDetailInput : TmsMapDetail
{
}

public class UpdateTmsMapDetailInput : AddTmsMapDetailInput
{

}

public class DeleteTmsMapDetailInput : BaseIdInput
{

}