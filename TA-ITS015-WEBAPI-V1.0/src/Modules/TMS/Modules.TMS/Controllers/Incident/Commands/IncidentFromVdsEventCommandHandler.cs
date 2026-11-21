using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Infrastructure.Services.Incident;

namespace Modules.TMS.Controllers.Incident.Commands
{
    /// <summary>
    /// Author: KienLT
    /// Description: Tạo sự cố từ bản tin VDS đã có (tab Sự kiện). Logic ở <see cref="TmsIncidentFromVdsEventService"/>.
    /// Created date: 23/10/2026
    /// </summary>
    public class IncidentFromVdsEventCommandHandler : IWolverineHandler
    {
        private readonly TmsIncidentFromVdsEventService _fromVdsEvent;

        public IncidentFromVdsEventCommandHandler(TmsIncidentFromVdsEventService fromVdsEvent)
        {
            _fromVdsEvent = fromVdsEvent;
        }

        public Task<IncidentFromVdsEventOutput> HandleAsync(CreateIncidentFromVdsEventInput command) =>
            _fromVdsEvent.CreateAsync(command);
    }
}
