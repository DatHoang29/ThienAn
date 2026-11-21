using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.CfgSystem.Core.Entities;
using Modules.CfgSystem.Infrastructure;

namespace Modules.CfgSystem.Controllers.ConfigData.Queries;

/// <summary>
/// Author: KienLT
/// Description: Đọc danh mục mã → tên theo loại cấu hình trong module sở hữu để các module khác truy vấn qua bus.
/// Created date: 22/10/2026
/// </summary>
public sealed class CfgConfigDataNamesQueryHandler(BaseRepository<SysConfigData> configData) : IWolverineHandler
{
    /// <summary>
    /// Author: KienLT
    /// Description: Lấy danh mục chưa xóa theo mã loại (kể cả mục đang tắt), sắp theo <c>OrderNo</c>.
    /// Created date: 22/10/2026
    /// </summary>
    public Task<List<CfgConfigDataNameOutput>> HandleAsync(CfgConfigDataNamesInput input)
        => configData.AsQueryable()
            .InnerJoin<SysConfigType>((d, t) => d.ConfigTypeId == t.ID)
            .Where((d, t) => d.IsDelete == null && t.IsDelete == null && t.Code == input.TypeCode)
            .OrderBy((d, t) => d.OrderNo)
            .Select((d, t) => new CfgConfigDataNameOutput { Code = d.Code, Name = d.Name })
            .ToListAsync();
}
