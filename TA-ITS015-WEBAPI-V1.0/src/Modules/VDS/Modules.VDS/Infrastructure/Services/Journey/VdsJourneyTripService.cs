using Modules.VDS.Controllers.Journey.Dto;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Modules.VDS.Infrastructure.Services.Journey.Rules;
using Modules.VDS.Infrastructure.Services.Segment;
using Modules.VDS.Infrastructure.Services.Segment.Rules;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.VDS.Infrastructure.Services.Journey;

/// <summary>
/// Author: KienLT
/// Description: Đọc topology và cấu hình để gom hành trình thành chuyến, sau đó phân trang.
/// Created date: 22/10/2026
/// </summary>
public sealed class VdsJourneyTripService(
    BaseRepository<VdsConfig> config,
    VdsSegmentGenerateService segments) : IScoped
{
    /// <summary>
    /// Author: KienLT
    /// Description: Gom toàn bộ hành trình đã lọc thành chuyến trước khi phân trang.
    /// Created date: 22/10/2026
    /// </summary>
    public async Task<SqlSugarPagedList<JourneyTripOutput>> BuildAsync(List<PageJourneyOutput> rows, JourneyInput filter)
    {
        var topology = await segments.TopologyAsync();
        var cameras = new Dictionary<string, VdsJourneyTripRules.CameraPoint>(StringComparer.Ordinal);
        foreach (var camera in topology.Cameras.Where(c => !string.IsNullOrWhiteSpace(c.Id)))
            cameras.TryAdd(camera.Id.Trim().ToLowerInvariant(), new VdsJourneyTripRules.CameraPoint(camera.Route, camera.Pos));
        var turns = topology.Segments.Where(s => s.Kind == VdsSegmentTopologyRules.PathKind.Turn && !string.IsNullOrWhiteSpace(s.Id)).Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var gapRaw = await config.AsQueryable()
            .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable
                && x.GroupCode == VdsConfigConst.Group.ConfigDefault && x.Code == VdsConfigConst.Code.JourneyTripGap)
            .Select(x => x.Value).FirstAsync();

        var trips = VdsJourneyTripRules.Build(rows, cameras, turns, VdsJourneyTripRules.GapSeconds(gapRaw));
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 20 : filter.PageSize;
        return new SqlSugarPagedList<JourneyTripOutput>
        {
            Current = page,
            Size = pageSize,
            Total = trips.Count,
            TotalPages = (int)Math.Ceiling(trips.Count / (double)pageSize),
            HasPrevPage = page > 1,
            HasNextPage = page * pageSize < trips.Count,
            Records = trips.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
        };
    }

}
