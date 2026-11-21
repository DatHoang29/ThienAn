using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.EventType.Dto;

public class PageEventTypeInput : BasePageInput
{

    public string Name { get; set; }

    public string Type { get; set; }
    public string WarningType { get; set; }
    public string ModuleId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

public class EventTypeInput
{

    public string Name { get; set; }
    public string Type { get; set; }
    public string WarningType { get; set; }
    public string ModuleId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }

}

public class AddEventTypeInput : TmsEventType
{
}

public class UpdateEventTypeInput : AddEventTypeInput
{

}

public class DeleteEventTypeInput : BaseIdInput
{

}