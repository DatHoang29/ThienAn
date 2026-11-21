using Microsoft.Extensions.Logging;
using Modules.CfgSystem.Core.Dto.ConfigData;
using Modules.TMS.Core.Dto.EventSnapshot;
using Shared.DTO.Constants.Application;
using Shared.Infrastructure.Services;
using Modules.TMS.Core.Entities;
using Modules.VDS.Controllers.Event.Dto;
using Modules.VDS.Core.Constants;
using Modules.VDS.Core.Entities;
using Modules.VDS.Infrastructure;
using Modules.VDS.Infrastructure.Services.Event.Rules;
using Modules.VDS.Infrastructure.Services.Persistence.Quest;
using Shared.Core.Utilities.Constants;
using Shared.DTO.Enums;
using Shared.Utility.Utilities;

// KienLT - 27/10/2026: dựng truy vấn trên client do GuardAsync cấp để hỗ trợ router HA.
namespace Modules.VDS.Infrastructure.Services.Event;

/// <summary>
/// Author: KienLT
/// Description: Tổng hợp snapshot sự kiện VDS, đọc danh mục qua bus và cache theo tenant/tài khoản.
/// Created date: 22/10/2026
/// </summary>
public sealed class VdsEventSnapshotService(
    VdsRepository<VdsTrafficData> traffic,
    BaseRepository<VdsConfig> settings,
    IMessageBus bus,
    BaseCacheService cache,
    ILogger<VdsEventSnapshotService> logger) : IScoped
{
    /// <summary>Trần số bản ghi thô đọc mỗi nhóm cửa sổ mỗi nhịp — chặn trường hợp bão bản tin.</summary>
    private const int MaxRawRows = 5000;

    /// <summary>Biên lệch đồng hồ khi lọc hành trình theo <c>CreateTime</c> (giờ server).</summary>
    private const int CreateTimeMarginSeconds = 300;

    private const string DirectionType = BaseEnums.SysConfigTypeCodeEnum.TmsDirectionConfigCode;

    private static readonly TimeSpan CatalogTtl = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Author: KienLT
    /// Description: Danh mục loại sự kiện, cửa sổ truy vấn và thông tin camera dùng để dựng snapshot.
    /// Created date: 22/10/2026
    /// </summary>
    private sealed record Catalog(
        Dictionary<string, VdsEventSnapshotRules.SnapshotType> Types,
        List<(int DisplaySeconds, string[] Codes)> Windows,
        VdsEventSnapshotRules.SnapshotType? JourneyType,
        Dictionary<string, CameraInfo> Cameras,
        Dictionary<(string Type, string Code), string?> Names);

    /// <summary>
    /// Author: KienLT
    /// Description: Tên, tuyến và lý trình của camera trong snapshot.
    /// Created date: 22/10/2026
    /// </summary>
    private sealed record CameraInfo(string? Name, string? Location, int? KmNumber, int? MetNumber);

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc và tổng hợp các sự kiện còn trong thời gian hiển thị theo bộ lọc.
    /// Created date: 22/10/2026
    /// </summary>
    public async Task<EventSnapshotOutput> SnapshotAsync(EventSnapshotInput input)
    {
        var now = DateTime.UtcNow;
        var catalog = await GetCatalogAsync();
        var items = new List<EventSnapshotItemOutput>();
        if (catalog.Types.Count > 0)
        {
            // Lọc vùng theo danh mục TmsZoneEquipment cho CẢ HAI nguồn: cột zoneId của bản thô chỉ là vùng chính lúc ghi.
            List<string>? zoneCameras = null;
            if (!string.IsNullOrWhiteSpace(input.ZoneId))
            {
                zoneCameras = (await bus.InvokeAsync<TmsEventSnapshotZoneOutput>(
                    new TmsEventSnapshotZoneInput { ZoneId = input.ZoneId })).EquipmentIds;
            }
            if (zoneCameras is not { Count: 0 })
            {
                items.AddRange(await ReadAidAsync(input, catalog, zoneCameras, now));
                items.AddRange(await ReadJourneysAsync(input, catalog, zoneCameras, now));
                FillCatalog(items, catalog);
            }
        }

        return new EventSnapshotOutput { ComputedAt = now, Items = VdsEventSnapshotRules.Finalize(items) };
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc danh mục có cache ngắn theo tenant và loại tài khoản.
    /// Created date: 22/10/2026
    /// </summary>
    private async Task<Catalog> GetCatalogAsync()
    {
        var user = App.User;
        var tenant = user?.FindFirst(ClaimConst.TenantId)?.Value;
        var key = $"{CacheConst.Vds.VdsEventSnapshot}:{tenant}|{user?.FindFirst(ClaimConst.AccountType)?.Value}";
        // Không có tenant thì không nhớ vì chưa xác định được phạm vi dữ liệu.
        var data = string.IsNullOrWhiteSpace(tenant) ? null : cache.Get<VdsEventCatalogOutput>(key);
        if (data == null)
        {
            data = await LoadCatalogAsync();
            if (!string.IsNullOrWhiteSpace(tenant)) cache.Set(key, data, CatalogTtl);
        }
        return BuildCatalog(data);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc các dòng danh mục để cache dưới dạng danh sách có thể tuần tự hóa.
    /// Created date: 22/10/2026
    /// </summary>
    private async Task<VdsEventCatalogOutput> LoadCatalogAsync()
    {
        var tms = await bus.InvokeAsync<TmsEventSnapshotCatalogOutput>(new TmsEventSnapshotCatalogInput());
        var configRows = (await settings.AsQueryable()
                .Where(x => x.IsDelete == null && x.Status == BaseEnums.StatusEnum.Enable
                    && (x.GroupCode == VdsConfigConst.Group.EventDisplay
                        || x.GroupCode == VdsConfigConst.Group.MappingEventType
                        || x.GroupCode == VdsConfigConst.Group.ConfigDefault && x.Code == VdsConfigConst.Code.EventDisplayDefault))
                .Select(x => new { x.GroupCode, x.Code, x.Value }).ToListAsync())
            .Select(x => new VdsEventSnapshotRules.DisplayRow(x.GroupCode, x.Code, x.Value))
            .ToList();

        var names = await bus.InvokeAsync<List<CfgEventSnapshotNameOutput>>(
            new CfgEventSnapshotNamesInput { Types = [VdsConfigConst.ZoneLocationTypeCode, DirectionType] });
        return new VdsEventCatalogOutput { Tms = tms, Display = configRows, Names = names };
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Dựng bảng tra cứu với bộ so sánh gốc sau khi đọc cache.
    /// Created date: 22/10/2026
    /// </summary>
    private static Catalog BuildCatalog(VdsEventCatalogOutput data)
    {
        var catalogRows = data.Tms.Types
            .Select(x => new VdsEventSnapshotRules.CatalogRow(x.Code, x.Name, x.Type, x.Url, x.WarningType, x.Priority))
            .ToList();
        var types = VdsEventSnapshotRules.BuildTypes(catalogRows, data.Display);
        var mappings = data.Display.Where(x => x.GroupCode == VdsConfigConst.Group.MappingEventType).ToList();
        var journeyCode = mappings.FirstOrDefault(x => string.Equals(x.Code?.Trim(), VdsJourneyEventConst.EventTypeName,
            StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
        // Tốc độ trung bình chỉ lên SSE khi loại ánh xạ của nó thuộc nhóm event (nhóm incident đã có hồ sơ sự cố).
        var journeyType = journeyCode is not null && types.TryGetValue(journeyCode, out var jt) ? jt : null;

        var cameras = data.Tms.Cameras
            .Where(x => !string.IsNullOrWhiteSpace(x.ID))
            .GroupBy(x => x.ID, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => new CameraInfo(x.First().Name, x.First().Location, x.First().KmNumber, x.First().MetNumber),
                StringComparer.OrdinalIgnoreCase);
        var names = data.Names
            .Where(x => !string.IsNullOrWhiteSpace(x.Code) && x.Type != null)
            .GroupBy(x => (x.Type!, x.Code!))
            .ToDictionary(x => x.Key, x => x.First().Name);

        return new Catalog(types, VdsEventSnapshotRules.QueryWindows(types, mappings.Select(x => x.Value)), journeyType, cameras, names);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Bản tin AID: mỗi nhóm thời lượng hiển thị một truy vấn với cận <c>detectTime</c> riêng.
    /// Created date: 22/10/2026
    /// </summary>
    private async Task<List<EventSnapshotItemOutput>> ReadAidAsync(
        EventSnapshotInput input, Catalog catalog, List<string>? zoneCameras, DateTime now)
    {
        var rows = new List<VdsTrafficData>();
        foreach (var (seconds, codes) in catalog.Windows)
        {
            // Cận dưới BẮT BUỘC: bảng phân vùng theo ngày, thiếu cận là quét toàn bảng.
            var from = now.AddSeconds(-seconds);
            var batch = await traffic.GuardAsync(questDb => questDb.Queryable<VdsTrafficData>()
                .Where(x => x.DetectTime >= from && x.IsDelete == null && codes.Contains(x.EventType!))
                .WhereIF(zoneCameras != null, x => zoneCameras!.Contains(x.EquipmentId!))
                .WhereIF(!string.IsNullOrWhiteSpace(input.EquipmentId), x => x.EquipmentId == input.EquipmentId)
                .WhereIF(!string.IsNullOrWhiteSpace(input.Direction), x => x.Direction == input.Direction)
                .OrderBy(x => x.DetectTime, OrderByType.Desc)
                .Take(MaxRawRows)
                .ToListAsync());
            if (batch.Count >= MaxRawRows)
                logger.LogWarning("[VdsEvent] Snapshot chạm trần {Max} bản tin cho cửa sổ {Seconds}s ({Codes}); nhóm cũ hơn bị cắt.",
                    MaxRawRows, seconds, string.Join(",", codes));
            rows.AddRange(batch);
        }
        return VdsEventSnapshotRules.GroupAid(rows, catalog.Types, now);
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Hành trình vượt tốc độ trung bình, kèm bản tin điểm B để FE tải ảnh.
    /// Created date: 22/10/2026
    /// </summary>
    private async Task<List<EventSnapshotItemOutput>> ReadJourneysAsync(
        EventSnapshotInput input, Catalog catalog, List<string>? zoneCameras, DateTime now)
    {
        if (catalog.JourneyType is not { } type) return [];

        // Cột thời gian hành trình là UTC do worker ghi (xem TimeServerUtil) — so thẳng với UtcNow.
        var from = now.AddSeconds(-type.DisplaySeconds);
        // CreateTime (giờ server, có index) luôn >= ToTime nên lọc thêm để không quét cả bảng mỗi nhịp.
        var createdFrom = BaseUtil.TimeServer.GetTimeServer(from.AddSeconds(-CreateTimeMarginSeconds))
                          ?? BaseUtil.TimeServer.GetTimeServer().AddSeconds(-type.DisplaySeconds - CreateTimeMarginSeconds);
        var rows = (await bus.InvokeAsync<TmsEventSnapshotJourneyOutput>(new TmsEventSnapshotJourneyInput
        {
            From = from, CreatedFrom = createdFrom, ZoneCameras = zoneCameras,
            EquipmentId = input.EquipmentId, Direction = input.Direction, MaxItems = VdsEventSnapshotRules.MaxItems
        })).Journeys;
        if (rows.Count == 0) return [];

        var sources = await ReadImageSourcesAsync(rows);
        return rows.Select(x => VdsEventSnapshotRules.JourneyItem(x, type, VdsEventSnapshotRules.MatchImageSource(x, sources))).ToList();
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Bản tin thô điểm B của các hành trình (cùng camera cuối, mốc trong JourneyTimeTolerance quanh ToTime).
    /// Created date: 22/10/2026
    /// </summary>
    private async Task<List<VdsTrafficData>> ReadImageSourcesAsync(List<TmsTrafficJourney> rows)
    {
        var cameras = rows.Select(x => x.ToEquipmentId).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var times = rows.Where(x => x.ToTime != null).Select(x => DateTime.SpecifyKind(x.ToTime!.Value, DateTimeKind.Utc)).ToList();
        if (cameras.Count == 0 || times.Count == 0) return [];
        var minTime = times.Min() - VdsEventSnapshotRules.JourneyTimeTolerance;
        var maxTime = times.Max() + VdsEventSnapshotRules.JourneyTimeTolerance;
        try
        {
            return await traffic.GuardAsync(questDb => questDb.Queryable<VdsTrafficData>()
                .Where(x => x.DetectTime >= minTime && x.DetectTime <= maxTime && x.IsDelete == null
                    && cameras.Contains(x.EquipmentId) && x.LicensePlate != null && x.LicensePlate != "")
                .Take(MaxRawRows)
                .ToListAsync());
        }
        catch (Exception ex)
        {
            // Ảnh chỉ là phụ: lỗi đọc nguồn ảnh không được làm mất sự kiện khỏi màn hình.
            logger.LogWarning(ex, "[VdsEvent] Không đọc được bản tin điểm B cho ảnh hành trình.");
            return [];
        }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Tên camera, vị trí (tuyến) và tên hướng theo danh mục — giống cách VDS đặt vị trí cho sự cố.
    /// Created date: 22/10/2026
    /// </summary>
    private static void FillCatalog(List<EventSnapshotItemOutput> items, Catalog catalog)
    {
        foreach (var item in items)
        {
            if (item.EquipmentId != null && catalog.Cameras.TryGetValue(item.EquipmentId, out var camera))
            {
                item.EquipmentName = camera.Name;
                item.Location = camera.Location;
                // Hành trình không mang km/met: lấy theo camera cuối như sự cố tốc độ trung bình.
                item.KmNumber ??= camera.KmNumber;
                item.MetNumber ??= camera.MetNumber;
            }
            if (item.FromEquipmentId != null && catalog.Cameras.TryGetValue(item.FromEquipmentId, out var fromCamera))
                item.FromEquipmentName = fromCamera.Name;
            if (item.Location != null && catalog.Names.TryGetValue((VdsConfigConst.ZoneLocationTypeCode, item.Location), out var location))
                item.LocationName = location;
            if (item.InfluenceScope != null && catalog.Names.TryGetValue((DirectionType, item.InfluenceScope), out var direction))
                item.InfluenceScopeName = direction;
        }
    }
}
