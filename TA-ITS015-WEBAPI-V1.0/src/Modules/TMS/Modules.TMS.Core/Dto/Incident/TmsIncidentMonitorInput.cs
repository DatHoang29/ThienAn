namespace Modules.TMS.Core.Dto.Incident;

/// <summary>Unpaged main-screen list with bounded time windows.</summary>
public class TmsIncidentMonitorInput : TmsIncidentInput
{
    public bool IncludeEvents { get; set; } = true;
    public bool IncludeIncidents { get; set; } = true;
    public string? Field { get; set; }
    public string? Order { get; set; }
}
