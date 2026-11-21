using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin cơ bản TmsTrafficPlan
    /// </summary>
    public class TmsTrafficPlanResponseBase : BaseEntityDto
    {
        //TmsTrafficPlan {
        //ID varchar(64) pk
        //Code string (64) null
        //Name string (64) null
        //EventTypeId varchar(64) null > TmsEventType.ID
        //Status integer null
        //Remark string (256) null
        //}

        public string Name { get; set; }
        public string EventTypeId { get; set; }
        public int? Status { get; set; }
        public string Remark { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin mở rộng TmsTrafficPlan
    /// </summary>
    public class TmsTrafficPlanResponse : TmsTrafficPlanResponseBase
    {
    }
}
