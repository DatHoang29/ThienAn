using Modules.Media.Controllers.ItsMedia.Vms.Dto;
using Modules.Media.Infrastructure;
using Modules.Media.Infrastructure.Config;
using Modules.Media.Infrastructure.Helpers;
using Modules.Media.Infrastructure.Services;
using Modules.VMS.Core.Entities;

namespace Modules.Media.Controllers.ItsMedia.Vms.Queries;

/// <summary>
/// Description: [ITS] Đọc ảnh đang hiển thị của VMS (VmsCurrent.Url / DefaultUrl) trong wwwroot ITS.
/// Created date: 08/10/2026
/// </summary>
public class ItsMediaVmsQueryHandler
{
    private readonly BaseRepository<VmsCurrent> _vmsRep;
    private readonly MediaConfigService _configService;
    private readonly MediaFileReader _reader;

    public ItsMediaVmsQueryHandler(BaseRepository<VmsCurrent> vmsRep,
        MediaConfigService configService, MediaFileReader reader)
    {
        _vmsRep = vmsRep;
        _configService = configService;
        _reader = reader;
    }

    public async Task<ItsMediaVmsOutput> HandleAsync(ItsMediaVmsInput input, CancellationToken ct)
    {
        var field = MediaHelper.VmsField(input.Field);
        var output = new ItsMediaVmsOutput { ID = input.ID, Field = field };
        var config = await _configService.GetAsync(ct);
        if (!config.Enabled(MediaConst.Subsystem.Vms))
        {
            output.Status = MediaConst.Status.Disabled;
            return output;
        }

        var entity = await _vmsRep.AsQueryable()
            // Request ẩn danh (img src / WP gọi ITS) không có tenant -> bỏ filter toàn cục, tự lọc IsDelete.
            .ClearFilter()
            .Where(x => x.ID == input.ID && x.IsDelete == null)
            .FirstAsync(ct);
        if (entity == null) return output;

        output.Version = MediaHelper.Version(entity.UpdateTime, entity.CreateTime);
        var path = field == "DefaultUrl" ? entity.DefaultUrl : entity.Url;
        return await _reader.ReadAsync(path, output, ct);
    }
}
