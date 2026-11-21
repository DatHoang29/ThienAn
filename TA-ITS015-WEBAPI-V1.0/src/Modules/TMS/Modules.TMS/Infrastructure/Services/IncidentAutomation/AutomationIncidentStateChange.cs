namespace Modules.TMS.Infrastructure.Services.IncidentAutomation;

/// <summary>
/// Author: KienLT
/// Description: Internal message only. HTTP state inputs cannot supply automation image context.
/// Created date: 02/10/2026
/// </summary>
public sealed record AutomationIncidentStateChange(string RunId, string TenantId, string IncidentId, int State);
