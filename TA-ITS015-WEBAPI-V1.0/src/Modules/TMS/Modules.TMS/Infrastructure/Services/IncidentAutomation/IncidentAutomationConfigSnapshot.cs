using Modules.TMS.Core.Entities;
using Modules.TMS.Core.Constants;
using Newtonsoft.Json;

namespace Modules.TMS.Infrastructure.Services.IncidentAutomation;

/// <summary>
/// Author: KienLT
/// Description: The scheduled run owns its execution, completion and reset policy.
/// Created date: 02/10/2026
/// </summary>
public static class IncidentAutomationConfigSnapshot
{
    /// <summary>Only legacy runs without a snapshot may fall back to the live configuration.</summary>
    public static TmsIncidentAutomationConfig? Read(TmsIncidentAutomationRun run)
    {
        if (string.IsNullOrWhiteSpace(run.ConfigSnapshotJson)) return null;

        var config = JsonConvert.DeserializeObject<TmsIncidentAutomationConfig>(run.ConfigSnapshotJson);
        if (config is null || config.ID != run.AutomationConfigId || config.TenantId != run.TenantId
            || config.EventTypeId != run.EventTypeId || config.ConfigVersion != run.ConfigVersion)
            throw new JsonSerializationException("Automation configuration snapshot does not match its run.");
        return config;
    }

    public static DateTime? MissingFinishAt(TmsIncidentAutomationRun run)
    {
        if (run.Status != IncidentAutomationConst.RunStatus.Active
            || run.AutoFinishAt is not null || run.ActivatedAt is null) return null;
        var config = Read(run);
        return config is { IsAutoFinishEnabled: true, AutoFinishAfterSeconds: > 0 }
            ? run.ActivatedAt.Value.AddSeconds(config.AutoFinishAfterSeconds.Value) : null;
    }
}
