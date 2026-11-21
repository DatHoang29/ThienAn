using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;

namespace Modules.CfgSystem.Controllers.ConfigData.Queries;

/// <summary>
/// Author: KienLT
/// Description: Đọc tên danh mục phục vụ snapshot VDS trong module cấu hình.
/// Created date: 22/10/2026
/// </summary>
public sealed class CfgEventSnapshotNamesQueryHandler(BaseRepository<SysConfigData> configData) : IWolverineHandler
{
    /// <summary>
    /// Author: KienLT
    /// Description: Đọc tên theo nhóm, giữ bộ lọc xóa mềm của dữ liệu và loại danh mục.
    /// Created date: 22/10/2026
    /// </summary>
    public Task<List<CfgEventSnapshotNameOutput>> HandleAsync(CfgEventSnapshotNamesInput input)
        => configData.AsQueryable()
            .InnerJoin<SysConfigType>((d, t) => d.ConfigTypeId == t.ID)
            .Where((d, t) => d.IsDelete == null && t.IsDelete == null && input.Types.Contains(t.Code))
            .Select((d, t) => new CfgEventSnapshotNameOutput { Type = t.Code, Code = d.Code, Name = d.Name })
            .ToListAsync();
}
