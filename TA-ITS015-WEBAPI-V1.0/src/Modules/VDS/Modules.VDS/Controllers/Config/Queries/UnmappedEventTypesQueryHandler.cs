using Modules.TMS.Core.Constants;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Modules.VDS.Controllers.Config.Dto;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Modules.VDS.Infrastructure;
using Shared.DTO.Enums;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Controllers.Config.Queries;

/// <summary>
/// Author: KienLT
/// Description: Đọc QuestDB theo phân vùng thời gian, loại các AID đã có ánh xạ hiện hành.
/// Created date: 19/10/2026
/// </summary>
public sealed class UnmappedEventTypesQueryHandler(
    VdsRepository<VdsTrafficData> traffic,
    BaseRepository<VdsConfig> config) : IWolverineHandler
{
    /// <summary>
    /// Author: KienLT
    /// Description: Không sửa lịch sử unknown khi người dùng bổ sung ánh xạ.
    /// Created date: 19/10/2026
    /// </summary>
    public async Task<List<UnmappedEventTypeOutput>> HandleAsync(UnmappedEventTypesInput input)
    {
        var from = DateTime.UtcNow.AddDays(-Math.Clamp(input.Days, 1, 90));
        var rows = await traffic.GuardAsync(questDb => questDb.Queryable<VdsTrafficData>()
            .Where(x => x.DetectTime >= from && x.EventType == IncidentEventTypeConst.Unknown && x.IsDelete == null && x.AidEventType != null)
            .GroupBy(x => x.AidEventType)
            .Select(x => new UnmappedEventTypeOutput
            {
                AidEventType = x.AidEventType,
                Count = SqlFunc.AggregateCount(x.AidEventType),
                LastSeen = SqlFunc.AggregateMax(x.DetectTime)
            }).ToListAsync());
        var mapped = await config.AsQueryable()
            .Where(x => x.GroupCode == VdsConfigConst.Group.MappingEventType && x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable)
            .Select(x => x.Code).ToListAsync();
        var codes = new HashSet<string>(mapped.OfType<string>(), StringComparer.OrdinalIgnoreCase);
        return rows.Where(x => !string.IsNullOrWhiteSpace(x.AidEventType) && !codes.Contains(x.AidEventType))
            .OrderByDescending(x => x.LastSeen).ToList();
    }
}
