namespace Services.Vds.Features.Events;

/// <summary>Deploy 20260925_VdsIncidentDispatch.sql before this worker version.</summary>
internal static class IncidentDispatchProcedures
{
    public const string ReadPending = "dbo.Proc_VdsIncident_ReadPendingDispatch";
    public const string Expire = "dbo.Proc_VdsIncident_ExpireDispatch";
    public const string Complete = "dbo.Proc_VdsIncident_CompleteDispatch";
}

public sealed class IncidentDispatchResult
{
    public int AffectedRows { get; set; }
}
