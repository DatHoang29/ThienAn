using Modules.Media.Controllers.MediaPublic.Incident.Dto;
using Modules.Media.Core.Dto.Incident;
using Modules.Media.Infrastructure;
using Modules.Media.Infrastructure.Config;
using Modules.Media.Infrastructure.Helpers;
using Modules.Media.Infrastructure.Services;
using Modules.TMS.Core.Entities;

namespace Modules.Media.Controllers.MediaPublic.Incident.Queries;

/// <summary>
/// Description: [WP] Ảnh camera của sự cố.
///  1) Sự cố phải có trong DB WP và State thuộc media_incident_allowed_states (mặc định 3),
///     PictureType thuộc media_incident_picture_types (mặc định 3) — không đạt trả [].
///  2) Cache (BaseCacheService) theo {id}:{pictureType}:{CaptureTime}; chỉ cache dài khi MỌI ảnh đã Ready.
///  3) Chưa có cache -> gọi ITS-Media (single-flight).
/// Created date: 08/10/2026
/// </summary>
public class MediaPublicIncidentQueryHandler
{
    private readonly BaseRepository<TmsIncident> _incidentRep;
    private readonly MediaConfigService _configService;
    private readonly MediaPublicGateway _gateway;

    public MediaPublicIncidentQueryHandler(BaseRepository<TmsIncident> incidentRep,
        MediaConfigService configService, MediaPublicGateway gateway)
    {
        _incidentRep = incidentRep;
        _configService = configService;
        _gateway = gateway;
    }

    public async Task<List<MediaPublicIncidentOutput>> HandleAsync(MediaPublicIncidentInput input, CancellationToken ct)
    {
        var config = await _configService.GetAsync(ct);
        if (!config.Enabled(MediaConst.Subsystem.Incident)) return [];
        if (!config.IncidentPictureTypes.Contains(input.PictureType)) return [];

        var incident = await _incidentRep.AsQueryable()
            // Request ẩn danh (img src / WP gọi ITS) không có tenant -> bỏ filter toàn cục, tự lọc IsDelete.
            .ClearFilter()
            .Where(x => x.ID == input.ID && x.IsDelete == null)
            .FirstAsync(ct);
        if (incident == null || !config.IncidentAllowedStates.Contains(incident.State ?? string.Empty)) return [];

        var captureTime = MediaHelper.IncidentCaptureTime(incident, input.PictureType);
        if (captureTime == null) return [];

        var query = new MediaIncidentInput { ID = input.ID, PictureType = input.PictureType };

        return await _gateway.GetAsync<List<MediaPublicIncidentOutput>>(
            config,
            MediaConst.Subsystem.Incident,
            key: $"{input.ID}:{input.PictureType}",
            version: captureTime.Value.Ticks.ToString(),
            query,
            canCache: images => images.Count > 0
                                && images.All(i => i.Exists && i.Status == MediaConst.Status.Ready),
            fallback: _ => [],
            ct);
    }
}
