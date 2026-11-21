using Modules.Media.Controllers.MediaPublic.EventType.Dto;
using Modules.Media.Core.Dto.EventType;
using Modules.Media.Infrastructure;
using Modules.Media.Infrastructure.Config;
using Modules.Media.Infrastructure.Helpers;
using Modules.Media.Infrastructure.Services;
using Modules.TMS.Core.Entities;

namespace Modules.Media.Controllers.MediaPublic.EventType.Queries;

/// <summary>
/// Description: [WP] Icon loại sự kiện: kiểm tra trên DB WP -> cache theo UpdateTime -> gọi ITS-Media.
/// Created date: 08/10/2026
/// </summary>
public class MediaPublicEventTypeQueryHandler
{
    private readonly BaseRepository<TmsEventType> _eventTypeRep;
    private readonly MediaConfigService _configService;
    private readonly MediaPublicGateway _gateway;

    public MediaPublicEventTypeQueryHandler(BaseRepository<TmsEventType> eventTypeRep,
        MediaConfigService configService, MediaPublicGateway gateway)
    {
        _eventTypeRep = eventTypeRep;
        _configService = configService;
        _gateway = gateway;
    }

    public async Task<MediaPublicEventTypeOutput> HandleAsync(MediaPublicEventTypeInput input, CancellationToken ct)
    {
        var config = await _configService.GetAsync(ct);
        if (!config.Enabled(MediaConst.Subsystem.EventType))
            return new MediaPublicEventTypeOutput { ID = input.ID, Status = MediaConst.Status.Disabled };

        var entity = await _eventTypeRep.AsQueryable()
            // Request ẩn danh (img src / WP gọi ITS) không có tenant -> bỏ filter toàn cục, tự lọc IsDelete.
            .ClearFilter()
            .Where(x => x.ID == input.ID && x.IsDelete == null)
            .FirstAsync(ct);
        if (entity == null) return new MediaPublicEventTypeOutput { ID = input.ID };

        var version = MediaHelper.Version(entity.UpdateTime, entity.CreateTime);

        // Không cần gọi ITS khi chắc chắn không có file / là URL ngoài.
        if (string.IsNullOrWhiteSpace(entity.Url))
            return new MediaPublicEventTypeOutput { ID = input.ID, Version = version, Status = MediaConst.Status.MissingSource };
        if (MediaHelper.IsExternal(entity.Url))
            return new MediaPublicEventTypeOutput { ID = input.ID, Version = version, Status = MediaConst.Status.External, Url = entity.Url.Trim() };

        return await _gateway.GetAsync<MediaPublicEventTypeOutput>(
            config,
            MediaConst.Subsystem.EventType,
            key: input.ID,
            version,
            new MediaEventTypeInput { ID = input.ID },
            canCache: o => o.Exists,
            fallback: status => new MediaPublicEventTypeOutput { ID = input.ID, Version = version, Status = status },
            ct);
    }
}
