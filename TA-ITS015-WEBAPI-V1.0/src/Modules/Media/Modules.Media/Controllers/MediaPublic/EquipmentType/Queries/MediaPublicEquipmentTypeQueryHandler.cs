using Modules.Media.Controllers.MediaPublic.EquipmentType.Dto;
using Modules.Media.Core.Dto.EquipmentType;
using Modules.Media.Infrastructure;
using Modules.Media.Infrastructure.Config;
using Modules.Media.Infrastructure.Helpers;
using Modules.Media.Infrastructure.Services;
using Modules.TMS.Core.Entities;

namespace Modules.Media.Controllers.MediaPublic.EquipmentType.Queries;

/// <summary>
/// Description: [WP] Icon loại thiết bị: kiểm tra trên DB WP -> cache theo UpdateTime -> gọi ITS-Media.
/// Created date: 08/10/2026
/// </summary>
public class MediaPublicEquipmentTypeQueryHandler
{
    private readonly BaseRepository<TmsEquipmentType> _equipmentTypeRep;
    private readonly MediaConfigService _configService;
    private readonly MediaPublicGateway _gateway;

    public MediaPublicEquipmentTypeQueryHandler(BaseRepository<TmsEquipmentType> equipmentTypeRep,
        MediaConfigService configService, MediaPublicGateway gateway)
    {
        _equipmentTypeRep = equipmentTypeRep;
        _configService = configService;
        _gateway = gateway;
    }

    public async Task<MediaPublicEquipmentTypeOutput> HandleAsync(MediaPublicEquipmentTypeInput input, CancellationToken ct)
    {
        var field = MediaHelper.EquipmentTypeField(input.Field);
        var config = await _configService.GetAsync(ct);
        if (!config.Enabled(MediaConst.Subsystem.EquipmentType))
            return new MediaPublicEquipmentTypeOutput { ID = input.ID, Field = field, Status = MediaConst.Status.Disabled };

        var entity = await _equipmentTypeRep.AsQueryable()
            // Request ẩn danh (img src / WP gọi ITS) không có tenant -> bỏ filter toàn cục, tự lọc IsDelete.
            .ClearFilter()
            .Where(x => x.ID == input.ID && x.IsDelete == null)
            .FirstAsync(ct);
        if (entity == null) return new MediaPublicEquipmentTypeOutput { ID = input.ID, Field = field };

        var version = MediaHelper.Version(entity.UpdateTime, entity.CreateTime);
        var path = MediaHelper.EquipmentTypePath(entity, field);

        if (string.IsNullOrWhiteSpace(path))
            return new MediaPublicEquipmentTypeOutput { ID = input.ID, Field = field, Version = version, Status = MediaConst.Status.MissingSource };
        if (MediaHelper.IsExternal(path))
            return new MediaPublicEquipmentTypeOutput { ID = input.ID, Field = field, Version = version, Status = MediaConst.Status.External, Url = path.Trim() };

        return await _gateway.GetAsync<MediaPublicEquipmentTypeOutput>(
            config,
            MediaConst.Subsystem.EquipmentType,
            key: $"{input.ID}:{field}",
            version,
            new MediaEquipmentTypeInput { ID = input.ID, Field = field },
            canCache: o => o.Exists,
            fallback: status => new MediaPublicEquipmentTypeOutput { ID = input.ID, Field = field, Version = version, Status = status },
            ct);
    }
}
