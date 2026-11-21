using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.Map.Dto;

public class PageMapInput : BasePageInput
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Mode { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }


}

public class MapInput
{
    public string Code { get; set; }

    public string Name { get; set; }

    public string Type { get; set; }
    public string Mode { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }

}

public class AddMapInput : TmsMap
{
    public bool IsCopyLocationConfig { get; set; } = false;
    public string OldMapId { get; set; }
}

public class UpdateMapInput : AddMapInput
{

}

public class DeleteMapInput : BaseIdInput
{

}