using System.Globalization;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Modules.VDS.Core.Entities;
using static Shared.DTO.Enums.BaseEnums;

namespace Modules.TMS.Controllers.Zone.Queries;

public sealed class ZoneSpeedConfigQueryHandler : IWolverineHandler
{
    private readonly BaseRepository<TmsZone> _zones;
    private readonly BaseRepository<TmsZoneEquipment> _links;
    private readonly BaseRepository<TmsEquipment> _equipment;
    private readonly BaseRepository<VdsConfig> _settings;

    public ZoneSpeedConfigQueryHandler(BaseRepository<TmsZone> zones,
        BaseRepository<TmsZoneEquipment> links, BaseRepository<TmsEquipment> equipment,
        BaseRepository<VdsConfig> settings)
    {
        _zones = zones;
        _links = links;
        _equipment = equipment;
        _settings = settings;
    }

    public async Task<ZoneSpeedConfigListOutput> HandleAsync(SpeedConfigListInput input)
    {
        // Cùng repository/tenant và điều kiện VdsConfig với TrafficDataQueryHandler.
        var configRows = await _settings.AsQueryable()
            .Where(c => c.IsDelete == null && c.Status == StatusEnum.Enable
                        && (c.GroupCode == "vds_config" || c.GroupCode == "vds_list_equipment"))
            .ToListAsync();
        var zones = await _zones.AsQueryable().Where(z => z.IsDelete == null)
            .OrderBy(z => z.Code)
            .Select(z => new ZoneSpeedConfigOutput
            {
                Id = z.ID, Pid = z.Pid, Code = z.Code, Name = z.Name, Location = z.Location,
                LaneId = z.LaneId, Status = z.Status, FromKmNumber = z.FromKmNumber,
                FromMetNumber = z.FromMetNumber, ToKmNumber = z.ToKmNumber, ToMetNumber = z.ToMetNumber,
                MinSpeed = z.MinSpeed, MaxSpeed = z.MaxSpeed
            }).ToListAsync();
        var vdsIds = configRows.Where(c => c.GroupCode == "vds_list_equipment" && !string.IsNullOrWhiteSpace(c.Code))
            .Select(c => c.Code!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (vdsIds.Count == 0) return BuildOutput(configRows, zones, [], []);

        var links = await _links.AsQueryable()
            .Where(l => l.IsDelete == null && vdsIds.Contains(l.EquipmentId!))
            .Select(l => new ZoneSpeedConfigLink
            {
                ZoneId = l.ZoneId, EquipmentId = l.EquipmentId, CreateTime = (DateTime?)l.CreateTime
            }).ToListAsync();
        // Không lấy credential/config của thiết bị vào DTO hay kết quả trả về.
        var cameras = await _equipment.AsQueryable()
            .Where(e => e.IsDelete == null && vdsIds.Contains(e.ID))
            .Select(e => new TmsEquipment
            {
                ID = e.ID, Code = e.Code, Name = e.Name, KmNumber = e.KmNumber,
                MetNumber = e.MetNumber, DirectionId = e.DirectionId
            }).ToListAsync();
        return BuildOutput(configRows, zones, links, cameras);
    }

    internal static ZoneSpeedConfigListOutput BuildOutput(IEnumerable<VdsConfig> configRows,
        List<ZoneSpeedConfigOutput> zones, IEnumerable<ZoneSpeedConfigLink> links, IEnumerable<TmsEquipment> cameras)
    {
        var cameraById = cameras.ToDictionary(c => c.ID.Trim(), StringComparer.OrdinalIgnoreCase);
        var byZone = new Dictionary<string, List<ZoneVdsCameraOutput>>(StringComparer.OrdinalIgnoreCase);
        // Đúng ZoneManager: CreateTime tăng, null cuối, rồi ZoneId (không phân biệt hoa/thường).
        // Xét mọi liên kết chưa xoá, kể cả liên kết đến zone không còn trong danh sách trả về.
        var equipmentLinks = links.Where(l => !string.IsNullOrWhiteSpace(l.ZoneId)
                                              && !string.IsNullOrWhiteSpace(l.EquipmentId))
            .GroupBy(l => l.EquipmentId!.Trim(), StringComparer.OrdinalIgnoreCase);
        foreach (var group in equipmentLinks)
        {
            if (!cameraById.TryGetValue(group.Key, out var camera)) continue;
            var zoneIds = group.OrderBy(l => l.CreateTime ?? DateTime.MaxValue)
                .ThenBy(l => l.ZoneId, StringComparer.OrdinalIgnoreCase)
                .Select(l => l.ZoneId!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var zoneId in zoneIds)
            {
                if (!byZone.TryGetValue(zoneId, out var list)) byZone[zoneId] = list = [];
                list.Add(new ZoneVdsCameraOutput
                {
                    Id = camera.ID, Code = camera.Code, Name = camera.Name, KmNumber = camera.KmNumber,
                    MetNumber = camera.MetNumber, DirectionId = camera.DirectionId?.ToString(CultureInfo.InvariantCulture),
                    IsPrimary = string.Equals(zoneId, zoneIds[0], StringComparison.OrdinalIgnoreCase)
                });
            }
        }
        foreach (var zone in zones)
            zone.VdsCameras = byZone.TryGetValue(zone.Id.Trim(), out var list)
                ? list.OrderBy(c => c.Code, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Id, StringComparer.OrdinalIgnoreCase).ToList()
                : [];
        return new ZoneSpeedConfigListOutput { DirectionDefaults = ParseDirectionDefaults(configRows), Zones = zones };
    }

    internal static List<ZoneDirectionDefaultOutput> ParseDirectionDefaults(IEnumerable<VdsConfig> rows)
    {
        const string prefix = "vds_route_", minSuffix = "_min_speed", maxSuffix = "_max_speed";
        var mins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var maxs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Where(r => r.GroupCode == "vds_config"))
        {
            var code = (row.Code ?? "").Trim().ToLowerInvariant();
            if (!code.StartsWith(prefix, StringComparison.Ordinal)) continue;
            var isMin = code.EndsWith(minSuffix, StringComparison.Ordinal);
            var isMax = code.EndsWith(maxSuffix, StringComparison.Ordinal);
            if (!isMin && !isMax) continue;
            var direction = code[prefix.Length..^(isMin ? minSuffix.Length : maxSuffix.Length)];
            if (direction.Length == 0 || !int.TryParse((row.Value ?? "").Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var speed)) continue;
            (isMin ? mins : maxs)[direction] = speed;
        }
        return mins.Where(p => p.Value > 0 && maxs.TryGetValue(p.Key, out var max) && max > 0 && p.Value <= max)
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => new ZoneDirectionDefaultOutput { Direction = p.Key, MinSpeed = p.Value, MaxSpeed = maxs[p.Key] }).ToList();
    }

    /// <summary>
    /// Liên kết vùng–camera đọc từ DB. EntityTenant khai CreateTime không nullable; projection nullable giữ đúng NULL
    /// thật trong DB để xếp cuối.
    /// </summary>
    internal sealed class ZoneSpeedConfigLink
    {
        public string? ZoneId { get; set; }
        public string? EquipmentId { get; set; }
        public DateTime? CreateTime { get; set; }
    }
}
