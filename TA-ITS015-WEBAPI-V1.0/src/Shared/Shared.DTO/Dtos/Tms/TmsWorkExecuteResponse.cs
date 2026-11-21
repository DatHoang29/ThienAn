using Shared.DTO.Dtos.Base;
using System;
using System.Collections.Generic;
namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin cơ bản TmsWorkExecute
    /// </summary>
    public class TmsWorkExecuteResponseBase : BaseEntityDto
    {
        //TmsWorkExecute {
        // ID varchar(64) pk
        // IncidentId varchar(64) null
        // TrafficPlanId varchar(64) null
        // WorkDetailId varchar(64) null
        // Step integer null
        // WorkName string(256) null
        // Description string(512) null
        // Remark string(512) null
        // ExecuteOrg varchar(64) null
        // ExecuteBy varchar(64) null
        // ExecuteDate datetime null
        // State varchar(16) null def(0)
        //}

        public string? IncidentId { get; set; }
        public string? TrafficPlanId { get; set; }
        public string? WorkDetailId { get; set; }
        public int? Step { get; set; }
        public string? WorkName { get; set; }
        public string? Description { get; set; }
        public string? Remark { get; set; }
        public string? ExecuteOrg { get; set; }
        public string? ExecuteBy { get; set; }
        public DateTime? ExecuteDate { get; set; }
        public string? State { get; set; }
        public string? WorkMode { get; set; }
        public string? ControlType { get; set; }
    }

    /// <summary>
    /// Author: Ngọc Thắng
    /// Description: DTO thông tin mở rộng TmsWorkExecute
    /// </summary>
    public class TmsWorkExecuteResponse : TmsWorkExecuteResponseBase
    {
        public string? StateName { get; set; }
        public string? ExecuteOrgName { get; set; }
        
        public List<object> WorkContactList { get; set; }
    }
}
