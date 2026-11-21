using Modules.VDS.Core.Dto;
using Modules.VDS.Core.Helper;
using Modules.VDS.Core.Services.Media;
using Modules.TMS.Infrastructure.Services.Incident.Snapshot;
using Modules.TMS.Core.Dto.Incident;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Modules.TMS.Infrastructure.Services.Incident;
using Wolverine;
using Microsoft.Extensions.Logging;
using JsonException = System.Text.Json.JsonException;

namespace Modules.TMS.Controllers.Incident.Queries;

/// <summary>
/// Đọc các ảnh camera đã lưu theo sự cố và mốc trạng thái.
/// KienLT - 14/10/2026: ảnh đọc từ <see cref="IncidentImageStorage"/> (share cấu hình) thay vì wwwroot/tms/incident,
/// chỉ trả Base64 — share không phục vụ file tĩnh nên không còn <c>Url</c>.
/// </summary>
public sealed class IncidentImageQueryHandler : IWolverineHandler
{
    private const long MaxStageImageBytes = 10 * 1024 * 1024;

    private readonly BaseRepository<TmsIncident> _incidentRepo;
    private readonly IncidentImageStorage _images;
    private readonly VdsRegistrationImageStore _vdsImages;
    private readonly BaseRepository<TmsEquipment> _equipment;
    private readonly ILogger<IncidentImageQueryHandler> _logger;

    public IncidentImageQueryHandler(
        BaseRepository<TmsIncident> incidentRepo,
        IncidentImageStorage images, VdsRegistrationImageStore vdsImages, BaseRepository<TmsEquipment> equipment,
        ILogger<IncidentImageQueryHandler> logger)
    {
        _incidentRepo = incidentRepo;
        _images = images;
        _vdsImages = vdsImages;
        _equipment = equipment;
        _logger = logger;
    }

    public async Task<List<TmsIncidentImageOutput>> HandleAsync(
        TmsIncidentImageInput input,
        CancellationToken cancellationToken = default)
    {
        var incident = await _incidentRepo.GetByIdAsync(input.ID);
        if (incident == null)
            return [];

        var captureTime = IncidentSnapshotDispatcher.CaptureTime(incident, input.PictureType);
        if (captureTime == null)
            return [];

        var directory = _images.Directory(captureTime.Value);
        var incidentPart = IncidentSnapshotDispatcher.SafeFilePart(incident.ID);
        var vdsRegistration = VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, input.PictureType);
        if (vdsRegistration && !string.IsNullOrWhiteSpace(incident.TenantId))
        {
            var request = await _vdsImages.ReadAsync(incident.TenantId, incident.ID, cancellationToken);
            if (request is not null)
            {
                var camera = await _equipment.Context.Queryable<TmsEquipment>()
                    .Where(x => x.ID == request.Source.EquipmentId && x.TenantId == incident.TenantId).FirstAsync(cancellationToken);
                var output = new TmsIncidentImageOutput
                {
                    IncidentId = incident.ID, EquipmentId = request.Source.EquipmentId, EquipmentName = camera?.Name ?? camera?.Code,
                    KmNumber = camera?.KmNumber, MetNumber = camera?.MetNumber,
                    PictureType = 1, CaptureTime = captureTime.Value, ImageCapturedAt = request.Source.DetectTimeUtc,
                    ContentType = request.ContentType ?? "image/jpeg", ImageSource = "vds",
                    Status = request.Status, WaitUntilUtc = DateTime.SpecifyKind(request.WaitUntilUtc, DateTimeKind.Utc)
                };
                if (Math.Abs((request.RegistrationTime - captureTime.Value).TotalMilliseconds) > 4
                    || (request.Status == "WaitingForSource" && DateTime.UtcNow >= request.WaitUntilUtc))
                    output.Status = "MissingSource";
                if (output.Status == "Ready" && request.FileName is { } name && Path.GetFileName(name) == name
                    && VdsBaseHelper.MediaPathHp.ImageTypes.ContainsKey(Path.GetExtension(name))
                    && name == VdsBaseHelper.RegistrationImageFileHp.FileName(request, Path.GetExtension(name)))
                {
                    var bytes = await ReadFileAsync(Path.Combine(directory, name), maxBytes: null, cancellationToken);
                    if (bytes is not null)
                    {
                        output.Base64 = Convert.ToBase64String(bytes);
                        output.Exists = true;
                    }
                    else output.Status = "MissingSource";
                }
                else if (output.Status == "Ready") output.Status = "MissingSource";
                return InIncidentSlots(incident, output);
            }
        }
        VdsAutomationImageDecisionOutput? decision = null;
        if (input.PictureType is 2 or 3 && VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, 1)
            && !string.IsNullOrWhiteSpace(incident.TenantId))
        {
            try
            {
                decision = await _vdsImages.ReadStageAsync(incident.TenantId, incident.ID, input.PictureType,
                    captureTime.Value, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                _logger.LogWarning(ex, "[VdsAutomationImage] Cannot read stage choice for {IncidentId}/{PictureType}; preserve camera image lookup.", incident.ID, input.PictureType);
            }
            var registered = IncidentSnapshotDispatcher.CaptureTime(incident, 1);
            if (decision is { ImageSource: "vds", Source: not null } && registered is not null
                && (decision.RegistrationTime - registered.Value).Duration() <= TimeSpan.FromMilliseconds(4)
                && VdsBaseHelper.MediaPathHp.IsVdsRegistration(incident.Source, 1))
            {
                var camera = await _equipment.Context.Queryable<TmsEquipment>()
                    .Where(x => x.ID == decision.Source.EquipmentId && x.TenantId == incident.TenantId).FirstAsync(cancellationToken);
                var output = new TmsIncidentImageOutput
                {
                    IncidentId = incident.ID, EquipmentId = decision.Source.EquipmentId, EquipmentName = camera?.Name ?? camera?.Code,
                    KmNumber = camera?.KmNumber, MetNumber = camera?.MetNumber,
                    PictureType = input.PictureType, CaptureTime = captureTime.Value, ImageCapturedAt = decision.Source.DetectTimeUtc,
                    ContentType = decision.ContentType ?? "image/jpeg", ImageSource = "vds",
                    Status = decision.Status == "Selected" ? "PendingCapture" : decision.Status
                };
                if (decision.Status is "Ready" or "Selected" && decision.FileName is { } name && Path.GetFileName(name) == name)
                {
                    var extension = Path.GetExtension(name).ToLowerInvariant();
                    if (VdsBaseHelper.MediaPathHp.ImageTypes.TryGetValue(extension, out var mime) && mime == decision.ContentType
                        && name == VdsBaseHelper.RegistrationImageFileHp.FileName(incident.ID, decision.Source.EquipmentId, input.PictureType, extension))
                    {
                        var bytes = await ReadFileAsync(Path.Combine(directory, name), MaxStageImageBytes, cancellationToken);
                        if (bytes is not null)
                        {
                            output.Base64 = Convert.ToBase64String(bytes);
                            output.Exists = true;
                            output.Status = "Ready";
                        }
                    }
                    if (!output.Exists && decision.Status == "Ready") output.Status = "MissingSource";
                }
                return InIncidentSlots(incident, output);
            }
        }
        var suffix = $"_{input.PictureType}.jpeg";
        var equipmentBySafeId = IncidentSnapshotDispatcher.EquipmentIds(incident.EquipmentId)
            .GroupBy(IncidentSnapshotDispatcher.SafeFilePart, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        // Liệt kê + đọc toàn bộ ảnh camera của mốc này trong MỘT lần truy cập share.
        var cameraFiles = await _images.RunAsync(async () =>
        {
            var found = new List<(string FileName, byte[] Bytes)>();
            if (!Directory.Exists(directory))
                return found;
            var files = Directory.GetFiles(directory, $"{incidentPart}_*{suffix}", SearchOption.TopDirectoryOnly);
            foreach (var filePath in files.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName.StartsWith(incidentPart + "_vds_", StringComparison.OrdinalIgnoreCase))
                    continue; // Orphan VDS files must never masquerade as a CCTV image after fallback.
                found.Add((fileName, await File.ReadAllBytesAsync(filePath, cancellationToken)));
            }
            return found;
        }, cancellationToken);

        var outputs = new List<TmsIncidentImageOutput>();
        foreach (var (fileName, bytes) in cameraFiles)
        {
            var equipmentPartLength = fileName.Length - incidentPart.Length - suffix.Length - 1;
            if (equipmentPartLength <= 0)
                continue;

            var safeEquipmentId = fileName.Substring(incidentPart.Length + 1, equipmentPartLength);
            var equipmentId = equipmentBySafeId.GetValueOrDefault(safeEquipmentId, safeEquipmentId);

            outputs.Add(new TmsIncidentImageOutput
            {
                IncidentId = incident.ID,
                EquipmentId = equipmentId,
                PictureType = input.PictureType,
                CaptureTime = captureTime.Value,
                Exists = true,
                Base64 = Convert.ToBase64String(bytes)
            });
        }

        // Poll only an explicitly selected camera capture; missing cameras after a completed
        // or failed attempt are terminal, not evidence that capture is still running.
        if (decision is { ImageSource: "camera", Status: "Selected" })
            foreach (var id in equipmentBySafeId.Values.Where(id => outputs.All(image => image.EquipmentId != id)))
                outputs.Add(new TmsIncidentImageOutput
                {
                    IncidentId = incident.ID, EquipmentId = id, PictureType = input.PictureType,
                    CaptureTime = captureTime.Value, Exists = false, Status = "PendingCapture"
                });

        // Legacy registration files remain readable; no automatic replacement of historical images.
        if (vdsRegistration && outputs.Count == 0)
            outputs.Add(new TmsIncidentImageOutput
            {
                IncidentId = incident.ID, PictureType = 1, CaptureTime = captureTime.Value,
                ImageSource = "vds", Status = "MissingSource", Exists = false
            });
        return outputs;
    }

    /// <summary>
    /// Đọc một file ảnh trong quyền truy cập share; không có file (hoặc vượt <paramref name="maxBytes"/>, rỗng khi có giới hạn)
    /// thì trả null.
    /// </summary>
    private Task<byte[]?> ReadFileAsync(string path, long? maxBytes, CancellationToken cancellationToken)
        => _images.RunAsync(async () =>
        {
            var file = new FileInfo(path);
            if (!file.Exists) return null;
            if (maxBytes is { } limit && (file.Length <= 0 || file.Length > limit)) return null;
            return await File.ReadAllBytesAsync(file.FullName, cancellationToken);
        }, cancellationToken);

    /// <summary>
    /// VDS evidence replaces the image of every incident camera (PTZ/CCTV) for that stage.
    /// Legacy evidence remains readable without writing camera slots during a query.
    /// </summary>
    private static List<TmsIncidentImageOutput> InIncidentSlots(TmsIncident incident, TmsIncidentImageOutput image)
    {
        var equipmentIds = IncidentSnapshotDispatcher.EquipmentIds(incident.EquipmentId);
        if (equipmentIds.Count == 0) return [image];
        return equipmentIds.Select(id =>
        {
            return new TmsIncidentImageOutput
            {
                IncidentId = image.IncidentId, EquipmentId = id, EquipmentName = image.EquipmentName,
                KmNumber = image.KmNumber, MetNumber = image.MetNumber,
                PictureType = image.PictureType, CaptureTime = image.CaptureTime, Exists = image.Exists,
                Base64 = image.Base64, ContentType = image.ContentType, ImageSource = image.ImageSource,
                ImageCapturedAt = image.ImageCapturedAt, Status = image.Status, WaitUntilUtc = image.WaitUntilUtc
            };
        }).ToList();
    }
}
