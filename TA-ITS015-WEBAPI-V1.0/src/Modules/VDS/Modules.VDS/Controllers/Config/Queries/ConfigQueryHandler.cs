using Modules.CfgSystem.Core.Entities;
using Modules.VDS.Infrastructure.Services.Config;
using Modules.VDS.Core.Entities;
using Modules.VDS.Controllers.Config.Dto;
using Modules.VDS.Infrastructure;
using Shared.DTO.Enums;
using Shared.Infrastructure.Extensions;
using Shared.Utility.Utilities;
using Shared.Infrastructure.Persistence.SqlSugar;
using Modules.VDS.Core.Constants;
using Modules.VDS.Infrastructure.Services.Config.Rules;

namespace Modules.VDS.Controllers.Config.Queries;

/// <summary>
/// Author: KienLT
/// Description: Query handler đọc nhóm và danh sách cấu hình VDS.
/// Created date: 16/09/2026
/// </summary>
public class ConfigQueryHandler : IWolverineHandler
{
    private readonly BaseRepository<VdsConfig> _settings;
    private readonly BaseRepository<SysConfigData> _groupData;

    public ConfigQueryHandler(BaseRepository<VdsConfig> settings, BaseRepository<SysConfigData> groupData)
    {
        _settings = settings;
        _groupData = groupData;
    }

    /// <summary>Danh sách nhóm cấu hình VDS (dropdown).</summary>
    public async Task<List<VdsConfigGroupOutput>> HandleAsync(VdsConfigGroupInput command)
    {
        return await _groupData.AsQueryable()
            .InnerJoin<SysConfigType>((d, t) => d.ConfigTypeId == t.ID)
            .Where((d, t) => t.Code == VdsConfigConst.ConfigGroupTypeCode && t.IsDelete == null && d.IsDelete == null
                && t.Status == BaseEnums.StatusEnum.Enable && d.Status == BaseEnums.StatusEnum.Enable)
            .OrderBy((d, t) => d.OrderNo).OrderBy((d, t) => d.Code)
            .Select((d, t) => new VdsConfigGroupOutput { Code = d.Code!, Name = d.Name! }).ToListAsync();
    }

    /// <summary>
    /// Phân trang cấu hình VDS theo nhóm/keyword.
    ///
    /// <para>Output kế thừa entity nên <c>Select(..., true)</c> tự map đủ cột. Hai việc hậu xử lý bắt buộc:
    /// che <c>Value</c> của mã bí mật, và quy <c>CreateTime</c>/<c>UpdateTime</c> từ giờ server về UTC —
    /// trước đây do <c>[JsonConverter]</c> trên property của DTO làm, nay làm ở đây vì DTO không còn khai
    /// property nào.</para>
    /// </summary>
    public async Task<SqlSugarPagedList<VdsConfigOutput>> HandleAsync(PageVdsConfigInput command)
    {
        var result = await _settings.AsQueryable().Where(x => x.IsDelete == null)
            .WhereIF(!string.IsNullOrWhiteSpace(command.GroupCode), x => x.GroupCode == command.GroupCode)
            .WhereIF(!string.IsNullOrWhiteSpace(command.Keyword), x => x.Code.Contains(command.Keyword!) || x.Name!.Contains(command.Keyword!))
            .OrderBy(x => x.GroupCode).OrderBy(x => x.OrderNo).OrderBy(x => x.Code)
            .Select(x => new VdsConfigOutput(), true)
            .ToPagedListAsync(Math.Max(1, command.Page), Math.Clamp(command.PageSize, 1, 200));
        foreach (var row in result.Records)
        {
            // Che giá trị bí mật SAU khi phân trang: chỉ che chứ không loại dòng, để số tổng không lệch.
            if (VdsConfigRules.IsSecret(row.Code)) row.Value = null;
            row.CreateTime = BaseUtil.TimeServer.ToServerUtc(row.CreateTime);
            row.UpdateTime = BaseUtil.TimeServer.ToServerUtc(row.UpdateTime);
        }
        return result;
    }
}
