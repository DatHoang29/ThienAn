using static Shared.DTO.Enums.BaseEnums;
using System.Linq.Expressions;
using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;

namespace Modules.TMS.Infrastructure.Services.Incident.Classification;

/// <summary>Events keep their display window independently from the canceled incident lifecycle.</summary>
public static class IncidentDisplayRules
{
    public static Expression<Func<TmsIncident, bool>> Window(DateTime openFrom, DateTime closedFrom)
        => ti => ti.StartDate != null
            && (ti.Metadata == null || !ti.Metadata.Contains(IncidentRecordingConst.DuplicateMarker))
            && (((ti.State == "1" || ti.State == "2" || ti.State == "3") && ti.StartDate.Value.Date >= openFrom.Date)
                || ((ti.State == "0" || ti.State == "4" || ti.State == "5") && ti.StartDate.Value.Date >= closedFrom.Date));
}
