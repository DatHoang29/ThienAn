using Modules.TMS.Core.Constants;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.Incident.Mapping;
using Newtonsoft.Json.Linq;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;

namespace Modules.TMS.Infrastructure.Services.Incident.Configuration;

/// <summary>Explicit TMS per-type policy. Old/disabled configurations never enroll existing incidents.</summary>
public sealed class EventCompletionPolicy(BaseRepository<TmsIncidentAutomationConfig> configs) : IScoped
{
    public const string DeadlineKey = "VdsEventDueV1";
    public const string ConfigKey = "VdsEventPolicyId";
    public const string DeadlineMarker = "\"VdsEventDueV1\":";
    public const string RegistrationReceipt = "VdsAutomationReceipt_register";

    public static bool IsEnabledFor(TmsIncident row, TmsIncidentAutomationConfig? config)
    {
        if (config == null || !config.IsUnclaimedAutoFinishEnabled || config.IsAutoExecuteEnabled
            || config.IsDelete != null || config.Status != BaseEnums.StatusEnum.Enable
            || config.UnclaimedAutoFinishFromUtc == null
            || config.UnclaimedAutoFinishAfterSeconds is not (> 0 and <= 604800)
            || !string.IsNullOrWhiteSpace(config.TrafficPlanId)
            || string.IsNullOrWhiteSpace(row.TenantId) || row.TenantId != config.TenantId
            || string.IsNullOrWhiteSpace(row.EventTypeId) || row.EventTypeId != config.EventTypeId) return false;
        if (!IncidentAutomationSourceConst.IsAllowed(config.AllowedSources, IncidentAutomationSourceConst.Vds)) return false;
        var createdUtc = IncidentRuntimeFields.DeadlineAfterCreation(row, 1,
            Shared.Utility.Utilities.BaseUtil.TimeServer.GetTimeServer(), DateTime.UtcNow)?.AddSeconds(-1);
        return createdUtc >= DateTime.SpecifyKind(config.UnclaimedAutoFinishFromUtc.Value, DateTimeKind.Utc)
            && row.Source == "vds" && (row.EventGroup == IncidentEventGroupConst.Event || row.EventGroup == IncidentEventGroupConst.Incident)
            && row.State == "1" && row.IsDelete == null && row.ValidatedDate == null
            && row.ApprovedDate == null && string.IsNullOrWhiteSpace(row.TrafficPlanId)
            && row.ProcessMode == IncidentConst.ProcessMode.Manual
            && !IncidentRecordingConst.IsDuplicate(row.Metadata)
            && row.Metadata?.Contains(IncidentRecordingConst.SuppressedAutomationMarker, StringComparison.Ordinal) != true;
    }

    public async Task<TmsIncidentAutomationConfig?> ResolveAsync(TmsIncident row)
    {
        var config = await configs.GetFirstAsync(x => x.TenantId == row.TenantId
            && x.EventTypeId == row.EventTypeId && x.IsDelete == null);
        return IsEnabledFor(row, config) ? config : null;
    }

    public async Task AddDeadlineAsync(TmsIncident row, JObject metadata)
    {
        if (metadata[DeadlineKey] != null) return;
        var config = await ResolveAsync(row);
        if (config == null) return;
        var due = IncidentRuntimeFields.DeadlineAfterCreation(row, config.UnclaimedAutoFinishAfterSeconds!.Value,
            Shared.Utility.Utilities.BaseUtil.TimeServer.GetTimeServer(), DateTime.UtcNow);
        if (due == null) return;
        metadata[DeadlineKey] = new DateTimeOffset(due.Value).ToUnixTimeSeconds();
        metadata[ConfigKey] = config.ID;
        if (metadata.ToString(Formatting.None).Length > EntityConst.Length512)
            throw new InvalidOperationException("Incident metadata cannot hold the completion policy.");
    }

    public static bool IsDue(string? metadata, DateTimeOffset now, string? configId = null)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return false;
        try
        {
            var json = JObject.Parse(metadata);
            return (string?)json[RegistrationReceipt] == "NoConfig"
                && !string.IsNullOrWhiteSpace((string?)json[ConfigKey])
                && (configId == null || (string?)json[ConfigKey] == configId)
                && json[DeadlineKey]?.Type == JTokenType.Integer
                && (long)json[DeadlineKey]! > 0 && (long)json[DeadlineKey]! <= now.ToUnixTimeSeconds();
        }
        catch (JsonException) { return false; }
        catch (OverflowException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
