using Shared.DTO.Dtos.Base;
using System;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin cơ bản TmsIncident
    /// </summary>
    public class TmsIncidentResponseBase : BaseEntityDto
    {
        //TmsIncident {
            //ID varchar(64) pk
            //Code string(64) null
            //Name string(64) null
            //EventTypeId varchar(64) null > TmsEventType.ID
            //StartDate datetime null
            //Source string(16) null def(0)
            //Location string(16) null def(0)
            //ZoneId varchar(1024) null >* TmsZone.ID
            //KmNumber integer null
            //MetNumber integer null
            //InfluenceScope string(16) null def(0)
            //InjuredNumber integer null
            //VehicleNumber integer null
            //CreatedBy varchar(64) null > SysUser.Username
            //CreatedDate datetime null
            //ValidatedBy varchar(64) null > SysUser.Username
            //ValidatedDate datetime null
            //ApprovedBy varchar(64) > SysUser.Username
            //ApprovedDate datetime null
            //FinishedBy varchar(64) null > SysUser.Username
            //FinishedDate datetime null
            //TrafficPlanId varchar(64) null > TmsTrafficPlan.ID
            //EquipmentId varchar(1024) null >* TmsEquipment.ID
            //State varchar(16) null def(1)
            //Description string(512) null
            //Remark string(512) null
        //}

        public string? Name { get; set; }
        public string? EventTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public string? Source { get; set; }
        public string? Location { get; set; }
        public string? ZoneId { get; set; }
        public int? KmNumber { get; set; }
        public int? MetNumber { get; set; }
        public string? InfluenceScope { get; set; }
        public int? InjuredNumber { get; set; }
        public int? VehicleNumber { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ValidatedBy { get; set; }
        public DateTime? ValidatedDate { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? FinishedBy { get; set; }
        public DateTime? FinishedDate { get; set; }
        public string? TrafficPlanId { get; set; }
        public string? EquipmentId { get; set; }
        public string? State { get; set; }
        public string? Description { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin mở rộng TmsIncident
    /// </summary>
    public class TmsIncidentResponse : TmsIncidentResponseBase
    {
        public string? EventTypeName { get; set; }
    }
}
