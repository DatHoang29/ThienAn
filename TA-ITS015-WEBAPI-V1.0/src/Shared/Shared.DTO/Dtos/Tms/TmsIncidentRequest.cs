using Shared.DTO.Dtos.Base.Request;
using System;

namespace Shared.DTO.Dtos.Tms
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham số lấy thông tin một TmsIncident
    /// </summary>
    public class TmsIncidentSingleRequest {
        public string? Id { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO tham sô lấy danh sách TmsIncident
    /// </summary>
    public class TmsIncidentListRequest : BasePageReqDto
    {
        public string? EventTypeId { get; set; }
        public string? Source { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}