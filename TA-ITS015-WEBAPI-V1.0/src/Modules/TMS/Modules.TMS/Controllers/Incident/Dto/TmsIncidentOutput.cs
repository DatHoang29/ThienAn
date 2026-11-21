using Modules.TMS.Core.Entities;

namespace Modules.TMS.Controllers.Incident.Dto
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin sự cố
    /// Created date: 09/08/2024
    /// </summary>
    public class TmsIncidentOutput : TmsIncident
    {
        public string? EventTypeCode { get; set; }
        public string? EventTypeName { get; set; }
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: DTO thông tin sự cố
    /// Created date: 09/08/2024
    /// </summary>
    public class PageTmsIncidentOutput : TmsIncident
    {
        public string? EventTypeCode { get; set; }
        public string? EventTypeName { get; set; }
    }
}
