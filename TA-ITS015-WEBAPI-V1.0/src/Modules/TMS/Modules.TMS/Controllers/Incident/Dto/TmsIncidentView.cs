using Modules.TMS.Controllers.WorkExecute.Dto;

namespace Modules.TMS.Controllers.Incident.Dto
{
    public class TmsIncidentView
    {
        public TmsIncidentOutput Incident { get; set; }
        public List<TmsWorkExecuteOutput> WorkExecuteList { get; set; }
    }
}
