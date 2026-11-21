using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Modules.TMS.Controllers.TrafficPlan.Dto;

public class PageTmsTrafficPlanInput : BasePageInput
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string EventTypeId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }
}

public class TrafficPlanInput
{

    public string Name { get; set; }
    public string Code { get; set; }
    public string EventTypeId { get; set; }
    public BaseEnums.StatusEnum? Status { get; set; }

}

public class AddTmsTrafficPlanInput : TmsTrafficPlans
{
}

public class UpdateTmsTrafficPlanInput : AddTmsTrafficPlanInput
{

}

public class DeleteTmsTrafficPlanInput : BaseIdInput
{

}

/// <summary>
/// Author: Ngọc Thắng
/// Description: DTO tham số lấy danh sách kịch bản hợp lệ
/// </summary>
public class TmsTrafficPlanValidInput { }