using Modules.Media.Controllers.ItsMedia.EventType.Dto;
using Modules.Media.Infrastructure;
using Modules.Media.Infrastructure.Config;
using Modules.Media.Infrastructure.Helpers;
using Modules.Media.Infrastructure.Services;
using Modules.TMS.Core.Entities;

namespace Modules.Media.Controllers.ItsMedia.EventType.Queries;

/// <summary>
/// Description: [ITS] Đọc icon loại sự kiện (TmsEventType.Url) trong wwwroot ITS.
/// Created date: 08/10/2026
/// </summary>
public class ItsMediaEventTypeQueryHandler
{
    private readonly BaseRepository<TmsEventType> _eventTypeRep;
    private readonly MediaConfigService _configService;
    private readonly MediaFileReader _reader;

    public ItsMediaEventTypeQueryHandler(BaseRepository<TmsEventType> eventTypeRep,
        MediaConfigService configService, MediaFileReader reader)
    {
        _eventTypeRep = eventTypeRep;
        _configService = configService;
        _reader = reader;
    }

    public async Task<ItsMediaEventTypeOutput> HandleAsync(ItsMediaEventTypeInput input, CancellationToken ct)
    {
        var output = new ItsMediaEventTypeOutput { ID = input.ID };
        var config = await _configService.GetAsync(ct);
        if (!config.Enabled(MediaConst.Subsystem.EventType))
        {
            output.Status = MediaConst.Status.Disabled;
            return output;
        }

        var entity = await _eventTypeRep.AsQueryable()
            // Request ẩn danh (img src / WP gọi ITS) không có tenant -> bỏ filter toàn cục, tự lọc IsDelete.
            .ClearFilter()
            .Where(x => x.ID == input.ID && x.IsDelete == null)
            .FirstAsync(ct);
        if (entity == null) return output;

        output.Version = MediaHelper.Version(entity.UpdateTime, entity.CreateTime);
        return await _reader.ReadAsync(entity.Url, output, ct);
    }
}
