using Modules.Media.Controllers.MediaPublic.Vms.Dto;
using Modules.Media.Core.Dto.Vms;
using Modules.Media.Infrastructure;
using Modules.Media.Infrastructure.Config;
using Modules.Media.Infrastructure.Helpers;
using Modules.Media.Infrastructure.Services;
using Modules.VMS.Core.Entities;

namespace Modules.Media.Controllers.MediaPublic.Vms.Queries;

/// <summary>
/// Description: [WP] Ảnh VMS: kiểm tra trên DB WP -> cache theo UpdateTime -> gọi ITS-Media.
/// Created date: 08/10/2026
/// </summary>
public class MediaPublicVmsQueryHandler
{
    private readonly BaseRepository<VmsCurrent> _vmsRep;
    private readonly MediaConfigService _configService;
    private readonly MediaPublicGateway _gateway;

    public MediaPublicVmsQueryHandler(BaseRepository<VmsCurrent> vmsRep,
        MediaConfigService configService, MediaPublicGateway gateway)
    {
        _vmsRep = vmsRep;
        _configService = configService;
        _gateway = gateway;
    }

    public async Task<MediaPublicVmsOutput> HandleAsync(MediaPublicVmsInput input, CancellationToken ct)
    {
        var field = MediaHelper.VmsField(input.Field);
        var config = await _configService.GetAsync(ct);
        if (!config.Enabled(MediaConst.Subsystem.Vms))
            return new MediaPublicVmsOutput { ID = input.ID, Field = field, Status = MediaConst.Status.Disabled };

        var entity = await _vmsRep.AsQueryable()
            // Request ẩn danh (img src / WP gọi ITS) không có tenant -> bỏ filter toàn cục, tự lọc IsDelete.
            .ClearFilter()
            .Where(x => x.ID == input.ID && x.IsDelete == null)
            .FirstAsync(ct);
        if (entity == null) return new MediaPublicVmsOutput { ID = input.ID, Field = field };

        var version = MediaHelper.Version(entity.UpdateTime, entity.CreateTime);
        var path = field == "DefaultUrl" ? entity.DefaultUrl : entity.Url;

        if (string.IsNullOrWhiteSpace(path))
            return new MediaPublicVmsOutput { ID = input.ID, Field = field, Version = version, Status = MediaConst.Status.MissingSource };
        if (MediaHelper.IsExternal(path))
            return new MediaPublicVmsOutput { ID = input.ID, Field = field, Version = version, Status = MediaConst.Status.External, Url = path.Trim() };

        return await _gateway.GetAsync<MediaPublicVmsOutput>(
            config,
            MediaConst.Subsystem.Vms,
            key: $"{input.ID}:{field}",
            version,
            new MediaVmsInput { ID = input.ID, Field = field },
            canCache: o => o.Exists,
            fallback: status => new MediaPublicVmsOutput { ID = input.ID, Field = field, Version = version, Status = status },
            ct);
    }
}
