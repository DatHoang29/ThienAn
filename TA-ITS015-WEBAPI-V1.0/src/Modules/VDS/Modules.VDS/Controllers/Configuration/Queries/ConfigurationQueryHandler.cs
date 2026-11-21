using Modules.VDS.Infrastructure.Services.Config;
using Modules.VDS.Core.Entities;
using Modules.VDS.Controllers.Configuration.Dto;
using Modules.VDS.Infrastructure;
using Shared.Infrastructure.Extensions;
using Modules.VDS.Core.Constants;
using Modules.VDS.Infrastructure.Services.Config.Rules;

namespace Modules.VDS.Controllers.Configuration.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler đọc danh sách tham số cấu hình VDS (ẩn giá trị secret).
/// Created date: 16/09/2026
/// </summary>
public class ConfigurationQueryHandler : IWolverineHandler
{
    private readonly BaseRepository<VdsConfig> _repo;

    public ConfigurationQueryHandler(BaseRepository<VdsConfig> repo)
    {
        _repo = repo;
    }

    /// <summary>Danh sách tham số vận hành (nhóm <c>vds_config</c>), giá trị bí mật được che.</summary>
    public async Task<List<VdsConfigurationOutput>> HandleAsync(VdsConfigurationListInput command)
    {
        var rows = await _repo.AsQueryable().Where(x => x.IsDelete == null && x.GroupCode == VdsConfigConst.Group.Config)
            .OrderBy(x => x.Code)
            .Select(x => new VdsConfigurationOutput
            {
                ID = x.ID, Code = x.Code, Name = x.Name, Value = x.Value,
                OrderNo = x.OrderNo, Remark = x.Remark, UpdateTime = x.UpdateTime
            })
            .ToListAsync();
        foreach (var row in rows) if (VdsConfigRules.IsSecret(row.Code)) row.Value = null;
        return rows;
    }
}
