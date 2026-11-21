using Modules.Media.Controllers.ItsMedia.EquipmentType.Dto;
using Modules.Media.Infrastructure;
using Modules.Media.Infrastructure.Config;
using Modules.Media.Infrastructure.Helpers;
using Modules.Media.Infrastructure.Services;
using Modules.TMS.Core.Entities;

namespace Modules.Media.Controllers.ItsMedia.EquipmentType.Queries;

/// <summary>
/// Description: [ITS] Đọc icon loại thiết bị (Default/Active/Inactive/Fault) trong wwwroot ITS.
/// Created date: 08/10/2026
/// </summary>
public class ItsMediaEquipmentTypeQueryHandler
{
    private readonly BaseRepository<TmsEquipmentType> _equipmentTypeRep;
    private readonly MediaConfigService _configService;
    private readonly MediaFileReader _reader;

    public ItsMediaEquipmentTypeQueryHandler(BaseRepository<TmsEquipmentType> equipmentTypeRep,
        MediaConfigService configService, MediaFileReader reader)
    {
        _equipmentTypeRep = equipmentTypeRep;
        _configService = configService;
        _reader = reader;
    }

    public async Task<ItsMediaEquipmentTypeOutput> HandleAsync(ItsMediaEquipmentTypeInput input, CancellationToken ct)
    {
        var field = MediaHelper.EquipmentTypeField(input.Field);
        var output = new ItsMediaEquipmentTypeOutput { ID = input.ID, Field = field };
        var config = await _configService.GetAsync(ct);
        if (!config.Enabled(MediaConst.Subsystem.EquipmentType))
        {
            output.Status = MediaConst.Status.Disabled;
            return output;
        }

        var entity = await _equipmentTypeRep.AsQueryable()
            // Request ẩn danh (img src / WP gọi ITS) không có tenant -> bỏ filter toàn cục, tự lọc IsDelete.
            .ClearFilter()
            .Where(x => x.ID == input.ID && x.IsDelete == null)
            .FirstAsync(ct);
        if (entity == null) return output;

        output.Version = MediaHelper.Version(entity.UpdateTime, entity.CreateTime);
        return await _reader.ReadAsync(MediaHelper.EquipmentTypePath(entity, field), output, ct);
    }
}
