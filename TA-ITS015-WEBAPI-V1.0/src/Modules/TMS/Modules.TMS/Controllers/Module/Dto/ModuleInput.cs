using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.Module.Dto;

public class PageTmsModuleInput : BasePageInput
{
    public string KeyName { get; set; }
    public string Name { get; set; }
    public string PluginId { get; set; }

    public BaseEnums.StatusEnum? Status { get; set; }
}

public class ModuleInput
{

    public string Name { get; set; }
    public string KeyName { get; set; }
    public string PluginId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

public class AddTmsModuleInput : TmsModule
{
}

public class UpdateTmsModuleInput : AddTmsModuleInput
{

}

public class DeleteTmsModuleInput : BaseIdInput
{

}