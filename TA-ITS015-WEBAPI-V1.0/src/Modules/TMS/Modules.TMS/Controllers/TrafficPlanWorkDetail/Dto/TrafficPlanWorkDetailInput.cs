using Modules.TMS.Core.Entities;
using Shared.Core.Utilities;

namespace Modules.TMS.Controllers.TrafficPlanWorkDetail.Dto;

public class PageTmsTrafficPlanWorkDetailInput : BasePageInput
{
    public string TrafficPlanId { get; set; }
    public string WorkDetailId { get; set; }
    public int Step { get; set; }


}

public class TrafficPlanWorkDetailInput
{
    public int Step { get; set; }

    public string TrafficPlanId { get; set; }
    public string WorkDetailId { get; set; }

}

public class AddTmsTrafficPlanWorkDetailInput : TmsTrafficPlanWorkDetails
{
}

public class UpdateTmsTrafficPlanWorkDetailInput : AddTmsTrafficPlanWorkDetailInput
{

}

public class DeleteTmsTrafficPlanWorkDetailInput : BaseIdInput
{

}