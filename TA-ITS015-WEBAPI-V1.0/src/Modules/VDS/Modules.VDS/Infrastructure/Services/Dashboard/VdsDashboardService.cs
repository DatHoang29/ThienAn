using Modules.VDS.Core.Helper;
using Shared.Utility.Utilities;
using Microsoft.Extensions.Configuration;
using Modules.CfgSystem.Core.Entities;
using Modules.VDS.Core.Entities;
using Shared.Utility.Counting;
using Modules.TMS.Core.Entities;
using Modules.VDS.Infrastructure.Services.TrafficCount;
using Modules.VDS.Controllers.Dashboard.Dto;
using Shared.Utility.Helpers;
using Modules.VDS.Core.Exceptions;
using Modules.VDS.Core.Constants;

namespace Modules.VDS.Infrastructure.Services.Dashboard;

public sealed class VdsDashboardService(VdsTrafficCountService counts, BaseRepository<TmsEquipment> cameras,
    BaseRepository<TmsZoneEquipment> links, BaseRepository<VdsConfig> settings,
    BaseRepository<SysConfigData> configData, IConfiguration configuration) : IScoped
{
    /// <summary>Snapshot trung tâm; nhánh false luôn tạo DashboardCenterOutput.</summary>
    public async Task<DashboardCenterOutput> CenterInfoAsync(string? direction)
        => (DashboardCenterOutput)await SnapshotAsync(false, direction: direction);

    /// <summary>Snapshot vùng; nhánh true luôn tạo DashboardZoneOutput.</summary>
    public async Task<DashboardZoneOutput> ZoneInfoAsync(string? zoneId)
        => (DashboardZoneOutput)await SnapshotAsync(true, zoneId);

    private async Task<DashboardSnapshotOutput> SnapshotAsync(bool byZone, string? zoneId = null, string? equipmentId = null, string? direction = null)
    {
        var now = DateTime.UtcNow;
        var policy = configuration.GetSection("Vds:Counting").Get<DashboardPolicy>() ?? new();
        policy.ZoneId = zoneId; policy.EquipmentId = equipmentId;
        // Direction on a center snapshot selects a configured group, not every camera in the physical direction.
        policy.Direction = byZone ? direction : null;
        var period = TrafficCountMath.NormalizePeriod(policy.PeriodMinutes);
        if (period > 60 || 60 % period != 0) throw Oops.Oh(BaseMsg.Vds.Exception.CountPeriodInvalid);
        var last1h = TrafficCountMath.SlidingWindow(now, TimeSpan.FromHours(1), period).From;
        var last24h = TrafficCountMath.SlidingWindow(now, TimeSpan.FromHours(24), period).From;
        var today = BaseUtil.TimeServer.GetLocalDayUtcRange(now).FromUtc;
        var end = TrafficCountMath.FloorPeriod(now, period).AddMinutes(period);
        var slices = await counts.ReadSlicesAsync(policy, period, byZone ? TrafficCountScopes.Zone : TrafficCountScopes.Equipment, new[] { last24h, today }.Min(), end);
        var catalog = await ReadCamerasAsync();
        var ids = catalog.Select(x => x.ID).ToArray();
        var zoneLinks = await links.AsQueryable().Where(x => x.IsDelete == null && ids.Contains(x.EquipmentId!)).ToListAsync();
        var labels = await configData.AsQueryable().InnerJoin<SysConfigType>((d,t) => d.ConfigTypeId == t.ID)
            .Where((d,t) => d.IsDelete == null && t.IsDelete == null && t.Code == "vehicle_type")
            .Select((d,t) => d).ToListAsync();
        string? Name(string code) => labels.FirstOrDefault(x => x.Code == code)?.Name;
        EChartHelper.EChartResponse Chart(List<TrafficCountHour> hours) => TrafficCountChartBuilder.Build(hours, Name);
        DashboardCountOutput Block(TrafficCountBlock b) => new() { Short = b.Short, Medium = b.Medium, Long = b.Long, Other = b.Other,
            Total = b.Total, AvgSpeed = b.AvgSpeed, MinSpeed = b.MinSpeed, MaxSpeed = b.MaxSpeed };
        List<DashboardHourlyOutput> Flow(List<TrafficCountHour> hours) => hours.Select(h => new DashboardHourlyOutput {
            Hour = BaseUtil.TimeServer.ToUtc(h.Hour), Short = h.Block.Short, Medium = h.Block.Medium, Long = h.Block.Long,
            Other = h.Block.Other, Total = h.Block.Total, AvgSpeed = h.Block.AvgSpeed, Origin = "db"
        }).ToList();
        var window = new DashboardWindowsOutput {
            Last1h = new DashboardIntervalOutput { From = last1h, To = now },
            Last24h = new DashboardIntervalOutput { From = last24h, To = now },
            Today = new DashboardIntervalOutput { From = today, To = now } };

        if (byZone)
        {
            var zones = zoneLinks.Where(x => !string.IsNullOrWhiteSpace(x.ZoneId) && (zoneId == null || x.ZoneId == zoneId))
                .GroupBy(x => x.ZoneId!).OrderBy(x => x.Key).Select(g => {
                    var own = slices.Where(s => s.Scope.ScopeId == g.Key).ToList();
                    var hours = TrafficCountMath.ByHour(own, today, now);
                    var latest = own.Where(x => x.AvgSpeed != null).OrderByDescending(x => x.From).FirstOrDefault();
                    return new DashboardZoneRowOutput { ZoneId = g.Key, EquipmentIds = g.Select(x => x.EquipmentId).Distinct().ToList(),
                        Last1h = Block(TrafficCountMath.Sum(own, last1h, end)), Last24h = Block(TrafficCountMath.Sum(own, last24h, end)),
                        Today = Block(TrafficCountMath.Sum(own, today, end)),
                        Speed = new DashboardSpeedOutput { Latest = latest.AvgSpeed, LatestAt = latest.AvgSpeed == null ? (DateTime?)null : latest.From },
                        FlowByHour = Flow(hours), Chart = Chart(hours) };
                }).ToList();
            return new DashboardZoneOutput { Schema = "vds.traffic/v2", Mode = "snapshot", ComputedAt = now, Source = "VdsTrafficData",
                CountedEventTypes = policy.EffectiveEventTypes.ToList(), CountsAllEventTypes = policy.CountAllEventTypes,
                CountsUnknownPlate = !policy.RequirePlate, PeriodMinutes = period, Window = window, Zones = zones };
        }
        var configuredGroups = await settings.AsQueryable().Where(x => x.IsDelete == null && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable && x.GroupCode == VdsConfigConst.Group.Config
            && x.Code != null && x.Code.StartsWith(VdsConfigConst.Pattern.CountDirectionEquipmentPrefix)).ToListAsync();
        var groups = configuredGroups.Select(x => new {
            key = x.Code![VdsConfigConst.Pattern.CountDirectionEquipmentPrefix.Length..], name = x.Name,
            ids = (x.Value ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray()
        }).Where(x => direction == null || x.key == direction).ToList();
        var speedConfigs = await settings.AsQueryable().Where(x => x.IsDelete == null
            && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable && x.GroupCode == VdsConfigConst.Group.Config
            && x.Code != null && x.Code.StartsWith(VdsConfigConst.Pattern.SpeedDirectionEquipmentPrefix)).ToListAsync();
        var cameraRows = catalog.Where(c => equipmentId == null || c.ID == equipmentId).Select(c => {
            var own = slices.Where(s => s.Scope.ScopeId == c.ID).ToList();
            var hours = TrafficCountMath.ByHour(own, today, now);
            var latest = own.Where(x => x.AvgSpeed != null).OrderByDescending(x => x.From).FirstOrDefault();
            return new DashboardCameraOutput { EquipmentId = c.ID, Code = c.Code, Name = c.Name, Km = c.KmNumber, Met = c.MetNumber, IpAddress = c.Ip,
                ZoneId = zoneLinks.FirstOrDefault(x => x.EquipmentId == c.ID)?.ZoneId,
                DirectionId = c.DirectionId?.ToString(), DirectionKey = groups.FirstOrDefault(g => g.ids.Contains(c.ID))?.key,
                Last1h = Block(TrafficCountMath.Sum(own, last1h, end)), Last24h = Block(TrafficCountMath.Sum(own, last24h, end)),
                Today = Block(TrafficCountMath.Sum(own, today, end)),
                Speed = new DashboardSpeedOutput { Latest = latest.AvgSpeed, LatestAt = latest.AvgSpeed == null ? (DateTime?)null : latest.From },
                FlowByHour = Flow(hours), Chart = Chart(hours) };
        }).ToList();
        var directions = new List<DashboardDirectionOutput>();
        var speedFrom = now.AddHours(-1);
        var topologyRows = await settings.AsQueryable().Where(x => x.IsDelete == null
            && x.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable && x.GroupCode == VdsConfigConst.Group.Segment).ToListAsync();
        var topology = VdsBaseHelper.SegmentTopologyHp.Plan(catalog.Select(c => new VdsBaseHelper.SegmentTopologyHp.TopoCamera(c.ID, c.Code ?? c.ID,
                c.Location, c.DirectionId, c.KmNumber, c.MetNumber)),
            topologyRows.Select(r => new VdsBaseHelper.SegmentTopologyHp.TopoConfigRow(r.GroupCode, r.Code, r.Value)));
        // Đoạn A→B đang bật trong TmsTrafficSegment — nguồn khoảng cách mà worker dùng khi ghi hành trình
        // (VdsCatalogRepository.QuerySegmentsAsync), nên phải được ưu tiên trước con số tự tính từ lý trình.
        var segmentRows = await settings.Context.Queryable<TmsTrafficSegment>()
            .Where(x => x.IsDelete == null && x.Status == TmsTrafficSegment.StatusEnabled && x.DistanceMeter > 0
                && x.FromEquipmentId != null && x.ToEquipmentId != null)
            .Select(x => new { x.FromEquipmentId, x.ToEquipmentId, x.Direction, x.DistanceMeter, x.TenantId }).ToListAsync();
        foreach (var g in groups) {
            var own = slices.Where(s => g.ids.Contains(s.Scope.ScopeId)).ToList();
            var hours = TrafficCountMath.ByHour(own, today, now);
            var speedRows = speedConfigs.Where(x => x.Code == VdsConfigConst.Pattern.SpeedDirectionEquipmentPrefix + g.key).ToList();
            var speedIds = speedRows.Count == 1
                ? (speedRows[0].Value ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : Array.Empty<string>();
            var journeys = new List<TrafficDirectionJourney>();
            // Khoảng cách kỳ vọng: ưu tiên đoạn đã khai (kể cả khai tay lệch lý trình, vì hành trình được ghi
            // theo đúng số đó); chưa có đoạn nào thì mới rơi về khoảng cách tự tính từ topology.
            int? expectedDistance = null;
            if (speedIds.Length == 2)
            {
                expectedDistance = segmentRows.FirstOrDefault(s => s.TenantId == speedRows[0].TenantId && s.Direction == g.key
                    && string.Equals(s.FromEquipmentId, speedIds[0], StringComparison.OrdinalIgnoreCase)
                    && string.Equals(s.ToEquipmentId, speedIds[1], StringComparison.OrdinalIgnoreCase))?.DistanceMeter;
                if (expectedDistance == null && !topology.HasErrors)
                    expectedDistance = topology.Segments.FirstOrDefault(p => p.FromId == speedIds[0] && p.ToId == speedIds[1]
                        && p.Direction.ToString() == g.key)?.DistanceMeter;
            }
            if (expectedDistance is > 0)
            {
                var fromId = speedIds[0]; var toId = speedIds[1];
                var speedTenant = speedRows[0].TenantId;
                journeys = await settings.Context.Queryable<TmsTrafficJourney>()
                    .Where(x => x.IsDelete == null && x.TenantId == speedTenant && x.IsValid == true
                        && x.Direction == g.key && x.FromEquipmentId == fromId && x.ToEquipmentId == toId
                        && x.DistanceMeter == expectedDistance && x.TravelSeconds > 0
                        && x.ToTime >= speedFrom && x.ToTime < now && x.AvgSpeedKmh != null && x.AvgSpeedKmh >= 0)
                    .Select(x => new TrafficDirectionJourney { FromEquipmentId = x.FromEquipmentId,
                        ToEquipmentId = x.ToEquipmentId, Direction = x.Direction, ToTime = x.ToTime,
                        AvgSpeedKmh = x.AvgSpeedKmh, IsValid = x.IsValid,
                        DistanceMeter = x.DistanceMeter, TravelSeconds = x.TravelSeconds }).ToListAsync();
            }
            var speed = expectedDistance is > 0
                ? TrafficDirectionSpeed.Calculate(journeys, speedIds, g.key, speedFrom, now, expectedDistance)
                : (Average: (double?)null, LatestAt: (DateTime?)null);
            directions.Add(new DashboardDirectionOutput { DirectionKey = g.key, Name = g.name, EquipmentIds = g.ids,
                Last1h = Block(TrafficCountMath.Sum(own, last1h, end)), Last24h = Block(TrafficCountMath.Sum(own, last24h, end)),
                Today = Block(TrafficCountMath.Sum(own, today, end)),
                Speed = new DashboardDirectionSpeedOutput { Latest = speed.Average, LatestAt = speed.LatestAt,
                    LatestEquipmentId = speed.Average.HasValue ? speedIds[1] : null,
                    Last1hAvgSpeed = speed.Average, EquipmentIds = speedIds },
                FlowByHour = Flow(hours), Chart = Chart(hours) });
        }
        return new DashboardCenterOutput { Schema = "vds.count/v1", Mode = "snapshot", ComputedAt = now, Source = "VdsTrafficData",
            CountedEventTypes = policy.EffectiveEventTypes.ToList(), CountsAllEventTypes = policy.CountAllEventTypes,
            CountsUnknownPlate = !policy.RequirePlate, PeriodMinutes = period, Window = window, Cameras = cameraRows, Directions = directions };
    }

    private async Task<List<TmsEquipment>> ReadCamerasAsync()
    {
        var type = configuration["Vds:VdsEquipmentTypeId"];
        var configRows = await settings.AsQueryable().Where(d => d.IsDelete == null
            && d.Status == Shared.DTO.Enums.BaseEnums.StatusEnum.Enable && d.GroupCode == VdsConfigConst.Group.ListEquipment)
            .Select(d => new { d.Code }).ToListAsync();
        var configured = configRows.Select(x => x.Code).ToList();
        if (configured.Count == 0 && string.IsNullOrWhiteSpace(type))
            throw Oops.Oh(BaseMsg.Vds.Exception.EquipmentCatalogNotConfigured);
        return await cameras.AsQueryable().Where(x => x.IsDelete == null && x.Ip != null && x.Ip != "")
            .WhereIF(configured.Count > 0, x => configured.Contains(x.ID))
            .WhereIF(!string.IsNullOrWhiteSpace(type), x => x.EquipmentTypeId == type)
            .OrderBy(x => x.KmNumber).OrderBy(x => x.ID).ToListAsync();
    }
}
