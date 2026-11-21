using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Helper;
using Modules.VDS.Core.Services.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modules.TMS.Core.Entities;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Shared.Utility.Utilities;
using Wolverine;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Author: KienLT
/// Description: Imports only VDS registration images. Automation reuse is selected separately without waiting.
///              14/10/2026: ảnh sự cố lưu ở <see cref="IncidentImageStorage"/> (share) thay vì wwwroot/tms/incident.
///              Ảnh VDS và ảnh sự cố phải cùng một share UNC: bước import đọc nguồn rồi ghi đích trong CÙNG một
///              lần đăng nhập share của thư mục VDS.
/// Created date: 02/10/2026
/// </summary>
public sealed class TmsVdsRegistrationImageService(
    BaseRepository<TmsIncident> incidents, BaseRepository<VdsConfig> settings,
    VdsRegistrationImageStore store, IncidentImageStorage images, IWebHostEnvironment environment, IConfiguration configuration,
    IMessageBus messageBus, ILogger<TmsVdsRegistrationImageService> logger) : IScoped
{
    public async Task CaptureAsync(TmsIncident incident, CancellationToken ct)
    {
        if (!VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, 1)) return;
        EnsurePrivateStore();
        var tenant = incident.TenantId;
        if (string.IsNullOrWhiteSpace(tenant)) throw new InvalidOperationException("VDS registration requires incident tenant.");
        var request = await store.ReadAsync(tenant, incident.ID, ct);
        if (request is null && long.TryParse(incident.EventId, out var eventId) && incident.StartDate is { } detected)
        {
            // KienLT - 23/10/2026: StartDate/CreatedDate là GIỜ SERVER (đọc từ DB ra Kind Unspecified) ⇒ phải dùng
            // ToServerUtc; ToUtc chỉ dán nhãn UTC nên tra ảnh gốc lệch 7 giờ và không bao giờ thấy.
            var source = await messageBus.InvokeAsync<VdsRegistrationImageSourceOutput?>(
                new VdsRegistrationSourceLookupInput(tenant, eventId, BaseUtil.TimeServer.ToServerUtc(detected)), ct);
            if (source is not null && IncidentSnapshotDispatcher.CaptureTime(incident, 1) is { } registration)
            {
                await store.RegisterAsync(tenant, incident.ID, registration, BaseUtil.TimeServer.ToServerUtc(registration), source, ct);
                request = await store.ReadAsync(tenant, incident.ID, ct);
            }
        }
        if (request is null)
        {
            logger.LogWarning("[VdsRegistrationImage] Incident {IncidentId} has no unambiguous raw image source; no camera fallback.", incident.ID);
            return;
        }
        await ProcessAsync(request, ct);
    }

    public async Task ProcessAsync(VdsRegistrationImageInput queued, CancellationToken ct)
    {
        EnsurePrivateStore();
        await using var held = await store.LockAsync(queued.TenantId, queued.IncidentId, ct);
        var request = await store.ReadAsync(queued.TenantId, queued.IncidentId, ct);
        if (request is null || request.Status != "WaitingForSource") return;
        var incident = await incidents.Context.Queryable<TmsIncident>()
            .Where(x => x.ID == request.IncidentId && x.TenantId == request.TenantId && x.IsDelete == null).FirstAsync(ct);
        var registration = incident is null ? null : IncidentSnapshotDispatcher.CaptureTime(incident, 1);
        if (incident is null || !VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, 1) || registration is null
            || (registration.Value - request.RegistrationTime).Duration() > TimeSpan.FromMilliseconds(4))
        {
            await store.SaveAsync(request with { Status = "MissingSource", Error = "IncidentRegistrationMismatch" }, ct);
            return;
        }
        if (DateTime.UtcNow >= request.WaitUntilUtc)
        {
            await store.SaveAsync(request with { Status = "MissingSource", Error = "WaitDeadlineExceeded" }, ct);
            logger.LogWarning("[VdsRegistrationImage] Full missing at deadline for incident {IncidentId}; no camera fallback.", incident.ID);
            return;
        }
        var setting = await settings.Context.Queryable<VdsConfig>().Where(x => x.TenantId == request.TenantId
            && x.IsDelete == null && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable
            && x.GroupCode == VdsConfigConst.Group.Config && x.Code == VdsConfigConst.Code.MediaStorageDirectory).FirstAsync(ct);
        var mediaRoot = setting?.Value ?? configuration["Vds:MediaStorageDirectory"]
            ?? configuration["Vds:EventImageDirectory"];
        if (string.IsNullOrWhiteSpace(mediaRoot))
        {
            logger.LogWarning("[VdsRegistrationImage] Media storage not configured for tenant {Tenant}.", request.TenantId);
            return;
        }
        var result = await store.WithConfiguredRootAccessAsync(mediaRoot, () => VdsBaseHelper.RegistrationImageFileHp.ImportAsync(request, mediaRoot,
            images.Directory(registration.Value), DateTime.UtcNow, ct), ct);
        if (result.Status == "Ready" && result.FileName is { } fileName)
            await images.RunAsync(() => VdsBaseHelper.RegistrationImageFileHp.PublishToIncidentSlotsAsync(
                Path.Combine(images.Directory(registration.Value), fileName), incident.ID,
                IncidentSnapshotDispatcher.EquipmentIds(incident.EquipmentId), 1, ct), ct);
        if (result != request) await store.SaveAsync(result, ct);
        if (result.Status == "Ready")
            logger.LogInformation("[VdsRegistrationImage] Imported raw full into registration image for incident {IncidentId}.", incident.ID);
    }

    /// <summary>
    /// Thư mục state (JSON yêu cầu/lựa chọn ảnh) phải riêng: không nằm trong wwwroot (bị phục vụ công khai) và
    /// không trùng/lồng với thư mục ảnh sự cố (vd TmsImage\IncidentImage và TmsImage\IncidentImageState là anh em).
    /// </summary>
    internal void EnsurePrivateStore()
    {
        if (IncidentImageStorage.Overlaps(store.Root, IncidentImageStorage.WebRoot(environment)))
            throw new InvalidOperationException("VdsRegistrationImages:StateDirectory must be outside wwwroot.");
        if (IncidentImageStorage.Overlaps(store.Root, images.Root))
            throw new InvalidOperationException("VdsRegistrationImages:StateDirectory must not overlap IncidentImages:RootDirectory.");
    }
}
