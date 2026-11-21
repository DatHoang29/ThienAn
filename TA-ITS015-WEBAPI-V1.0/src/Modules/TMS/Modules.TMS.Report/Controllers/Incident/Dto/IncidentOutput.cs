using Shared.DTO.Dtos.Tms;

namespace Modules.TMS.Report.Controllers.Incident.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin chi tiết inciden sử dụng cho report
    /// Created date: 04/11/2024
    /// </summary>
    public class IncidentOutput : TmsIncidentResponse
    {
        public string? Idx { get; set; }
        public string? Zone { get; set; }
        public string? BeginDate { get; set; }
        public string? BeginTime { get; set; }
        public string? EndDate { get; set; }
        public string? EndTime { get; set; }
        public string? EventType { get; set; }
        public string? StateName { get; set; }
        public string? TrafficPlan { get; set; }
        public string? Position { get; set; }
        public string? Metadata { get; set; }
        public string? Information { get; set; }
        public string? InfluenceScope { get; set; }

        public List<TmsWorkExecuteResponse> WorkExecuteList { get; set; }
    }
}
