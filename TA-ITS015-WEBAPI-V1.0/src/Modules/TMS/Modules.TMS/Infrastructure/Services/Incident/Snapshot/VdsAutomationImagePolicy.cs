using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Helper;
using Modules.VDS.Core.Services.Media;
using Microsoft.Extensions.Logging;
using Modules.TMS.Core.Entities;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Shared.Utility.Utilities;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Author: KienLT
/// Description: Freezes policy per run and image choice per successful automation transition; never waits for full.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsAutomationImagePolicy(BaseRepository<VdsConfig> settings,
    VdsRegistrationImageStore store, TmsVdsRegistrationImageService registration, IncidentImageStorage images,
    ILogger<VdsAutomationImagePolicy> logger)
{
    public async Task<string> FreezeAsync(string configJson, TmsIncident incident)
    {
        var policy = new VdsImageReusePolicyOutput(incident.TenantId ?? "", incident.EventTypeId ?? "", 0);
        if (VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, 1) && !string.IsNullOrWhiteSpace(incident.TenantId)
            && !string.IsNullOrWhiteSpace(incident.EventTypeId))
        {
            try
            {
                // Config uses the readable TMS event code; the incident and frozen run retain their ID.
                var rows = await settings.Context.Queryable<VdsConfig>()
                    .InnerJoin<TmsEventType>((setting, eventType) => setting.Code == eventType.Code
                        && setting.TenantId == eventType.TenantId)
                    .Where((setting, eventType) => setting.TenantId == incident.TenantId
                        && setting.GroupCode == VdsConfigConst.Group.ImageReuse
                        && setting.IsDelete == null && setting.Status == StatusEnum.Enable
                        && eventType.ID == incident.EventTypeId && eventType.IsDelete == null)
                    .Select((setting, eventType) => setting).Take(2).ToListAsync();
                if (rows.Count == 1 && VdsImageReusePolicyOutput.ParseSeconds(rows[0].Value) is { } seconds)
                    policy = policy with { MaxAgeSeconds = seconds };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[VdsAutomationImage] Cannot freeze reuse setting for {IncidentId}; camera policy retained.", incident.ID);
            }
        }
        return VdsImageReusePolicyOutput.Freeze(configJson, policy);
    }

    public async Task<VdsAutomationImageDecisionOutput?> SelectAsync(TmsIncident incident, TmsIncidentAutomationRun run,
        int pictureType, CancellationToken ct)
    {
        var capture = IncidentSnapshotDispatcher.CaptureTime(incident, pictureType);
        var registered = IncidentSnapshotDispatcher.CaptureTime(incident, 1);
        if (!VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, 1) || pictureType is not (2 or 3)
            || capture is null || registered is null || string.IsNullOrWhiteSpace(incident.TenantId)
            || run.TenantId != incident.TenantId || run.IncidentId != incident.ID || run.EventTypeId != incident.EventTypeId)
            return null;
        var decision = new VdsAutomationImageDecisionOutput(incident.TenantId, incident.ID, run.ID, pictureType,
            capture.Value, registered.Value, "camera") { Reason = "ReuseNotEligible" };
        try
        {
            registration.EnsurePrivateStore();
            await using var held = await store.LockStageAsync(decision.TenantId, decision.IncidentId, pictureType, capture.Value, ct);
            var existing = await store.ReadStageAsync(decision.TenantId, decision.IncidentId, pictureType, capture.Value, ct);
            if (existing is not null) return existing.RunId == run.ID ? existing : decision.UseCamera("StageOwnedByOtherRun");
            var policy = VdsImageReusePolicyOutput.Read(run.ConfigSnapshotJson, incident.TenantId, incident.EventTypeId);
            if (policy?.CanReuse(true, incident.Source, pictureType, incident.TenantId, incident.EventTypeId,
                BaseUtil.TimeServer.ToUtc(registered.Value), BaseUtil.TimeServer.ToUtc(capture.Value)) == true)
            {
                var image = await store.ReadAsync(decision.TenantId, decision.IncidentId, ct);
                if (image is not null && (image.RegistrationTime - registered.Value).Duration() <= TimeSpan.FromMilliseconds(4)
                    && await images.RunAsync(() => Task.FromResult(
                        VdsBaseHelper.RegistrationImageFileHp.IsReady(image, images.Directory(registered.Value))), ct))
                {
                    var extension = Path.GetExtension(image.FileName!).ToLowerInvariant();
                    decision = decision with { ImageSource = "vds", Source = image.Source, ContentType = image.ContentType,
                        FileName = VdsBaseHelper.RegistrationImageFileHp.FileName(incident.ID, image.Source.EquipmentId, pictureType, extension),
                        Reason = "ReadyRegistrationWithinLimit" };
                }
                else decision = decision with { Reason = "RegistrationNotReady" };
            }
            return await store.SaveStageAsync(decision, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[VdsAutomationImage] Cannot persist image choice for {IncidentId}/{PictureType}; capture CCTV/RTSP now.", incident.ID, pictureType);
            // Keep the queued transition identity even when the shared state directory is unavailable.
            return decision.UseCamera("ChoicePersistenceFailed");
        }
    }
}
