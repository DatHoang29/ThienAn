using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.WorkDetail.Dto;

public class PageWorkDetailInput : BasePageInput
{
    public string ID { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string WorkType { get; set; }
    public string ControlType { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }

}

public class WorkDetailInput
{
    public string ID { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string WorkType { get; set; }
    public string ControlType { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

public class AddWorkDetailInput : TmsWorkDetail
{
}

public class UpdateWorkDetailInput : AddWorkDetailInput
{

}

public class DeleteWorkDetailInput : BaseIdInput
{

}