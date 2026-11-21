
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.TrafficPlan.Dto;

public class TrafficPlanOutput : TmsTrafficPlans
{
    //Begin - Add by Ngọc Thắng
    public bool? HasWorkDetailMapped { get; set; } = false;
    //End - Add by Ngọc Thắng
}


public class PageTmsTrafficPlanOutput : TmsTrafficPlans
{
}