using Services.Vds.Shared.Persistence;
using Services.Vds.Configuration;

namespace Services.Vds.Catalog;

/// <summary>Một liên kết camera ↔ vùng, đọc từ <c>TmsZoneEquipment</c>.</summary>
public sealed record ZoneLink(string ZoneId, string EquipmentId, DateTime? CreateTime = null);

/// <summary>
/// Ngưỡng tốc độ khai trên vùng (<c>TmsZone.MinSpeed/MaxSpeed</c>, km/h). Từng giá trị có thể thiếu: giá trị
/// thiếu rơi về mặc định theo hướng (xem <see cref="SpeedRangeResolver"/>).
/// </summary>
public sealed record ZoneSpeedLimit(double? Min, double? Max);

/// <summary>
/// Hồ sơ tốc độ của MỘT vùng trong một lần nạp: ngưỡng khai trên vùng + hướng mặc định của vùng
/// (<c>TmsZone.LaneId</c>, đã bỏ "0"/rỗng). Gộp chung để bên đọc lấy cả hai từ CÙNG một phiên bản danh mục —
/// đọc hai map riêng giữa lúc nạp lại có thể ghép min của lần nạp mới với hướng của lần nạp cũ.
/// </summary>
/// <param name="Limit">Ngưỡng khai trên vùng; null = vùng không khai min lẫn max.</param>
/// <param name="Direction">Hướng mặc định của vùng; null = vùng không khai hướng cụ thể.</param>
public sealed record ZoneSpeedProfile(ZoneSpeedLimit? Limit, string? Direction);

/// <summary>
/// Author: KienLT
/// Description: Danh mục vùng của từng camera trong RAM (<c>EquipmentId → ZoneId[]</c>).
///
/// <b>Vì sao cần</b>: cột <c>zoneId</c> của bảng dữ liệu và toàn bộ phần trạng thái vùng đều tính theo
/// vùng, nhưng bản tin VDS chỉ mang <c>cameraIp</c>. Không có map này thì <c>zoneId</c> để rỗng và mọi
/// truy vấn theo vùng trả về rỗng.
///
/// <c>TmsZoneEquipment</c> là quan hệ n-n (không có UNIQUE) nên MỘT camera có thể thuộc NHIỀU vùng:
/// <see cref="ZonesOf"/> trả hết (dùng khi cập nhật trạng thái vùng), <see cref="PrimaryZoneOf"/> trả
/// vùng đầu tiên theo thứ tự tất định (dùng cho cột <c>zoneId</c> đơn).
///
/// Cùng mô hình với <see cref="CameraManager"/>: singleton, nạp lúc khởi động, tra RAM chứ không query
/// DB mỗi bản tin. Snapshot đổi bằng <see cref="Interlocked.Exchange{T}(ref T, T)"/> nên luồng đọc
/// không bị chặn khi nạp lại.
/// Created date: 26/08/2026
/// </summary>
public sealed class ZoneManager
{
    private static readonly IReadOnlyList<string> Empty = Array.Empty<string>();

    private readonly VdsCatalogRepository _catalog;
    private readonly IVdsConfigManager _config;
    private readonly CatalogSnapshotStore _snapshots;
    private readonly ILogger<ZoneManager> _logger;

    private Dictionary<string, IReadOnlyList<string>> _byEquipment = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Camera XEM ĐƯỢC của từng vùng: <c>ZoneId → EquipmentId[]</c> (module CCTV, đang bật).</summary>
    private Dictionary<string, IReadOnlyList<string>> _camerasByZone = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Chiều NGƯỢC của <c>_byEquipment</c>: <c>ZoneId → EquipmentId[]</c> của các thiết bị được nạp
    /// (camera VDS). Khác <c>_camerasByZone</c> — cái đó là camera CCTV xem được, cái này là thiết bị VDS
    /// sinh ra số liệu.
    /// </summary>
    private Dictionary<string, IReadOnlyList<string>> _equipmentByZone = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Hồ sơ tốc độ theo vùng (<c>ZoneId → ngưỡng + hướng</c>) của lần nạp gần nhất. BẤT BIẾN sau khi dựng,
    /// đổi bằng MỘT <see cref="Interlocked.Exchange{T}(ref T, T)"/> nên bên đọc luôn thấy trọn một phiên bản.
    /// Vùng không khai ngưỡng lẫn hướng thì không có khoá.
    /// </summary>
    private ZoneProfileSet _profiles = ZoneProfileSet.Empty;

    /// <summary>Một phiên bản danh mục hồ sơ tốc độ vùng; không sửa sau khi dựng.</summary>
    private sealed class ZoneProfileSet
    {
        public static readonly ZoneProfileSet Empty =
            new(new Dictionary<string, ZoneSpeedProfile>(StringComparer.OrdinalIgnoreCase));

        public ZoneProfileSet(IReadOnlyDictionary<string, ZoneSpeedProfile> byZone)
        {
            ByZone = byZone;
            SpeedCount = byZone.Values.Count(p => p.Limit is not null);
            DirectionCount = byZone.Values.Count(p => p.Direction is not null);
        }

        public IReadOnlyDictionary<string, ZoneSpeedProfile> ByZone { get; }
        public int SpeedCount { get; }
        public int DirectionCount { get; }
    }

    public ZoneManager(
        VdsCatalogRepository catalog,
        IVdsConfigManager config,
        CatalogSnapshotStore snapshots,
        ILogger<ZoneManager> logger)
    {
        _catalog = catalog;
        _config = config;
        _snapshots = snapshots;
        _logger = logger;
    }

    /// <summary>Số camera có ít nhất một vùng.</summary>
    public int Count => Volatile.Read(ref _byEquipment).Count;

    /// <summary>Số camera thuộc nhiều vùng ở lần nạp gần nhất — càng nhiều thì số liệu vùng càng khó đối chiếu.</summary>
    public int MultiZoneCount { get; private set; }

    /// <summary>Toàn bộ vùng của một camera; không có mapping ⇒ danh sách rỗng.</summary>
    public IReadOnlyList<string> ZonesOf(string? equipmentId)
    {
        if (string.IsNullOrWhiteSpace(equipmentId)) return Empty;
        return Volatile.Read(ref _byEquipment).TryGetValue(equipmentId.Trim(), out var zones)
            ? zones
            : Empty;
    }

    /// <summary>Vùng chính (đầu tiên theo thứ tự nạp tất định) — dùng cho cột <c>zoneId</c> đơn.</summary>
    public string? PrimaryZoneOf(string? equipmentId)
    {
        var zones = ZonesOf(equipmentId);
        return zones.Count == 0 ? null : zones[0];
    }

    /// <summary>
    /// Hồ sơ tốc độ (ngưỡng + hướng) của vùng từ MỘT phiên bản danh mục; vùng không khai gì (hoặc chưa nạp)
    /// ⇒ null. Bên cần cả ngưỡng lẫn hướng PHẢI dùng hàm này (một lần đọc), không ghép
    /// <see cref="SpeedLimitOf"/> với <see cref="DirectionOf"/> — hai lần đọc có thể ra hai phiên bản.
    /// </summary>
    public ZoneSpeedProfile? ProfileOf(string? zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId)) return null;
        return Volatile.Read(ref _profiles).ByZone.TryGetValue(zoneId.Trim(), out var profile) ? profile : null;
    }

    /// <summary>Ngưỡng tốc độ khai trên vùng; vùng không khai gì (hoặc chưa nạp) ⇒ null.</summary>
    public ZoneSpeedLimit? SpeedLimitOf(string? zoneId) => ProfileOf(zoneId)?.Limit;

    /// <summary>Số vùng có khai ngưỡng tốc độ riêng ở lần nạp gần nhất.</summary>
    public int ZoneSpeedCount => Volatile.Read(ref _profiles).SpeedCount;

    /// <summary>Hướng mặc định của vùng (<c>TmsZone.LaneId</c>); vùng không khai (hoặc chưa nạp) ⇒ null.</summary>
    public string? DirectionOf(string? zoneId) => ProfileOf(zoneId)?.Direction;

    /// <summary>Camera XEM ĐƯỢC của một vùng; vùng không có camera nào ⇒ danh sách rỗng.</summary>
    public IReadOnlyList<string> CamerasOfZone(string? zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId)) return Empty;
        return Volatile.Read(ref _camerasByZone).TryGetValue(zoneId.Trim(), out var cameras)
            ? cameras
            : Empty;
    }

    /// <summary>
    /// MỌI vùng có thiết bị VDS, kèm danh sách thiết bị của vùng. Dùng để dựng bản tin lưu lượng theo vùng
    /// mà không phải quét lại danh mục camera mỗi lượt phát.
    ///
    /// Đây là thiết bị SINH SỐ LIỆU, không phải camera xem được (<see cref="CamerasOfZone"/>).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> EquipmentByZone =>
        Volatile.Read(ref _equipmentByZone);

    /// <summary>Thiết bị VDS của một vùng; vùng không có thiết bị nào ⇒ danh sách rỗng.</summary>
    public IReadOnlyList<string> EquipmentOfZone(string? zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId)) return Empty;
        return Volatile.Read(ref _equipmentByZone).TryGetValue(zoneId.Trim(), out var equipment)
            ? equipment
            : Empty;
    }

    /// <summary>
    /// Camera XEM ĐƯỢC "che" một thiết bị: gom camera module CCTV của MỌI vùng mà thiết bị đó thuộc về,
    /// loại trùng và giữ thứ tự tất định.
    ///
    /// Camera VDS phát hiện sự cố nhưng không stream được, nên sự cố phải trỏ sang các camera cùng vùng
    /// mà FE mở hình được. Thiết bị VDS không bao giờ nằm trong danh sách này vì nó thuộc module VDS.
    /// </summary>
    public IReadOnlyList<string> ViewableCamerasOf(string? equipmentId)
    {
        var zones = ZonesOf(equipmentId);
        if (zones.Count == 0) return Empty;

        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var zoneId in zones)
        {
            foreach (var camera in CamerasOfZone(zoneId))
            {
                if (seen.Add(camera)) result.Add(camera);
            }
        }

        return result.Count == 0 ? Empty : result;
    }

    /// <summary>
    /// Nạp lại toàn bộ danh mục vùng.
    ///
    /// Lỗi được NÉM RA cho bên gọi quyết định, giống <see cref="CameraManager.LoadAsync"/>: lúc khởi
    /// động thì thiếu vùng là thiếu <c>zoneId</c> cho mọi dòng sắp ghi, còn nạp lại giữa lúc chạy thì
    /// giữ snapshot cũ mới đúng. Hai tình huống khác nhau nên không tự nuốt lỗi ở đây.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        // Có ĐƯỜNG LÙI như danh mục camera: DB lỗi thì dùng snapshot lần nạp gần nhất thay vì chặn khởi động.
        var fromSnapshot = false;
        List<VdsCatalogRepository.ZoneLinkRow> rows;

        try
        {
            rows = await _catalog.QueryZoneLinksAsync(ct);
            if (rows.Count > 0) _snapshots.TrySave(CatalogSnapshotStore.ZoneLinksName, rows);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("ZoneManager");
            var cached = _snapshots.TryLoad<List<VdsCatalogRepository.ZoneLinkRow>>(
                CatalogSnapshotStore.ZoneLinksName);
            if (cached is null or { Count: 0 }) throw;

            _logger.LogError(ex,
                "[Zone] Không đọc được liên kết camera-vùng từ DB ⇒ DÙNG SNAPSHOT {Count} liên kết của lần " +
                "nạp gần nhất. Dữ liệu có thể đã CŨ.", cached.Count);

            rows = cached;
            fromSnapshot = true;
        }

        var links = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ZoneId) && !string.IsNullOrWhiteSpace(r.EquipmentId))
            .Select(r => new ZoneLink(r.ZoneId!, r.EquipmentId!, r.CreateTime))
            .ToList();

        // Giữ thứ tự nạp (CreateTime rồi ZoneId) để "vùng chính" không nhảy giữa các lần nạp — nếu
        // zoneId đổi theo từng lần restart thì số liệu theo vùng không thể đối chiếu được.
        var map = links
            .GroupBy(l => l.EquipmentId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .OrderBy(l => l.CreateTime ?? DateTime.MaxValue)
                    .ThenBy(l => l.ZoneId, StringComparer.OrdinalIgnoreCase)
                    .Select(l => l.ZoneId.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        MultiZoneCount = map.Count(kv => kv.Value.Count > 1);

        // Chiều ngược, tính MỘT lần ở đây thay vì quét danh mục mỗi lượt phát bản tin theo vùng.
        var byZone = links
            .GroupBy(l => l.ZoneId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g
                    .OrderBy(l => l.CreateTime ?? DateTime.MaxValue)
                    .ThenBy(l => l.EquipmentId, StringComparer.OrdinalIgnoreCase)
                    .Select(l => l.EquipmentId.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        // Kết quả RỖNG mà đang có map ⇒ GIỮ map cũ. Cùng lý do với danh mục camera: một lượt truy vấn trả
        // rỗng giữa lúc chạy sẽ làm cột vùng của mọi dòng ghi sau đó trống, và mọi truy vấn theo vùng hụt số.
        // Lần nạp ĐẦU vẫn cho phép rỗng (vùng không phải điều kiện để ghi dữ liệu thô).
        if (map.Count == 0 && Volatile.Read(ref _byEquipment).Count > 0)
        {
            _logger.LogError(
                "[Zone] Truy vấn liên kết camera-vùng trả về rỗng ⇒ GIỮ {Count} camera có vùng đang dùng.",
                Volatile.Read(ref _byEquipment).Count);
            return;
        }

        Interlocked.Exchange(ref _byEquipment, map);
        Interlocked.Exchange(ref _equipmentByZone, byZone);

        await LoadViewableCamerasAsync(ct);
        await LoadZoneSpeedsAsync(ct);

        _logger.LogInformation(
            "[Zone] Nạp {Cameras} camera có vùng ({Links} liên kết){Source}; {Multi} camera thuộc nhiều vùng.",
            map.Count, links.Count, fromSnapshot ? " TỪ SNAPSHOT (DB lỗi)" : "", MultiZoneCount);

        if (map.Count == 0)
        {
            _logger.LogWarning(
                "[Zone] TmsZoneEquipment không có liên kết camera-vùng nào ⇒ cột zoneId sẽ để rỗng và " +
                "mọi truy vấn theo vùng trả về rỗng.");
        }
    }

    /// <summary>
    /// Nạp camera XEM ĐƯỢC theo vùng. Lỗi ⇒ GIỮ map đang dùng và chỉ cảnh báo: mất map này chỉ làm sự cố
    /// không trỏ được sang camera stream được, không được làm hỏng cả danh mục vùng vừa nạp xong.
    /// </summary>
    private async Task LoadViewableCamerasAsync(CancellationToken ct)
    {
        var moduleId = _config.Current.Catalog.CctvModuleId;
        if (string.IsNullOrWhiteSpace(moduleId)) return;

        try
        {
            var rows = await _catalog.QueryZoneCamerasByModuleAsync(moduleId, ct);
            if (rows.Count > 0) _snapshots.TrySave(CatalogSnapshotStore.ZoneCamerasName, rows);
            ApplyViewableCameras(rows, fromSnapshot: false, moduleId);
        }
        catch (Exception ex)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("ZoneManager");
            // Đường lùi: dùng snapshot nếu có, vì mất map này làm sự cố không trỏ được sang camera xem được.
            var cached = _snapshots.TryLoad<List<VdsCatalogRepository.ZoneCameraRow>>(
                CatalogSnapshotStore.ZoneCamerasName);

            if (cached is { Count: > 0 })
            {
                _logger.LogError(ex,
                    "[Zone] Không nạp được camera xem được từ DB ⇒ DÙNG SNAPSHOT {Count} dòng của lần nạp " +
                    "gần nhất.", cached.Count);
                ApplyViewableCameras(cached, fromSnapshot: true, moduleId);
                return;
            }

            _logger.LogWarning(ex,
                "[Zone] Không nạp được camera xem được của vùng; giữ danh sách đang dùng ({Count} vùng).",
                Volatile.Read(ref _camerasByZone).Count);
        }
    }

    /// <summary>
    /// Nạp ngưỡng tốc độ và hướng (<c>LaneId</c>) của MỌI vùng. Khác liên kết camera-vùng, kết quả RỖNG là hợp
    /// lệ và được áp: nghĩa là không có vùng nào, mọi camera dùng mặc định theo hướng của camera. Đọc DB lỗi ⇒
    /// snapshot, không có snapshot thì GIỮ map đang dùng — mất ngưỡng vùng chỉ làm rơi về mặc định hướng, không
    /// chặn pipeline. Snapshot cũ (chưa có <c>LaneId</c>) vẫn đọc được, chỉ thiếu hướng của vùng.
    /// </summary>
    private async Task LoadZoneSpeedsAsync(CancellationToken ct)
    {
        List<VdsCatalogRepository.ZoneSpeedRow> rows;
        var fromSnapshot = false;

        try
        {
            rows = await _catalog.QueryZoneSpeedsAsync(ct);
            _snapshots.TrySave(CatalogSnapshotStore.ZoneSpeedsName, rows);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("ZoneManager");
            var cached = _snapshots.TryLoad<List<VdsCatalogRepository.ZoneSpeedRow>>(
                CatalogSnapshotStore.ZoneSpeedsName);

            if (cached is null)
            {
                _logger.LogWarning(ex,
                    "[Zone] Không nạp được ngưỡng tốc độ của vùng; giữ ngưỡng đang dùng ({Count} vùng).",
                    Volatile.Read(ref _profiles).SpeedCount);
                return;
            }

            _logger.LogError(ex,
                "[Zone] Không nạp được ngưỡng tốc độ của vùng từ DB ⇒ DÙNG SNAPSHOT {Count} vùng của lần nạp " +
                "gần nhất.", cached.Count);
            rows = cached;
            fromSnapshot = true;
        }

        var byZone = new Dictionary<string, ZoneSpeedProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ZoneId)) continue;
            var zoneId = row.ZoneId.Trim();

            // Hướng của vùng ghi nhận độc lập với min/max: vùng không khai ngưỡng vẫn cần hướng để lấy mặc định.
            // "0" = "Tất cả hướng" (direction_config_type; IncidentPositionHelper cũng coi "0" là mọi hướng) ⇒ KHÔNG
            // phải một hướng cụ thể, không có vds_route_0_*: coi như vùng không khai hướng, rơi về hướng camera.
            // Dev 08/10/2026 có 17 vùng LaneId = "0"; ghi thẳng thì cả 17 mất mặc định theo hướng.
            var laneId = row.LaneId?.Trim();
            var direction = !string.IsNullOrEmpty(laneId) && laneId != "0" ? laneId : null;

            // Số không dương là "chưa khai" chứ không phải ngưỡng 0 km/h.
            var min = row.MinSpeed is > 0 ? row.MinSpeed : null;
            var max = row.MaxSpeed is > 0 ? row.MaxSpeed : null;
            var limit = min is null && max is null ? null : new ZoneSpeedLimit(min, max);

            if (limit is null && direction is null) continue;
            byZone[zoneId] = new ZoneSpeedProfile(limit, direction);
        }

        // MỘT lần đổi cho cả ngưỡng lẫn hướng: bên đọc (SpeedRangeResolver) luôn thấy trọn lần nạp cũ hoặc
        // trọn lần nạp mới, không bao giờ min của lần này ghép với hướng của lần kia.
        var profiles = new ZoneProfileSet(byZone);
        Interlocked.Exchange(ref _profiles, profiles);
        _logger.LogInformation(
            "[Zone] Ngưỡng tốc độ theo vùng: {Count} vùng khai riêng, {Directions}/{Zones} vùng khai hướng " +
            "(LaneId){Source}; giá trị thiếu dùng mặc định theo hướng.",
            profiles.SpeedCount, profiles.DirectionCount, rows.Count, fromSnapshot ? " TỪ SNAPSHOT (DB lỗi)" : "");
    }

    /// <summary>
    /// Dựng map <c>ZoneId → EquipmentId[]</c> của camera xem được rồi đổi snapshot. Tách riêng để đường DB
    /// và đường snapshot dùng CHUNG một phép biến đổi — hai bản sao logic sẽ trôi lệch theo thời gian.
    /// </summary>
    private void ApplyViewableCameras(
        List<VdsCatalogRepository.ZoneCameraRow> rows, bool fromSnapshot, string moduleId)
    {
        var byZone = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ZoneId) && !string.IsNullOrWhiteSpace(r.EquipmentId))
            .GroupBy(r => r.ZoneId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                // Sắp theo Code để danh sách thiết bị ghi vào sự cố tất định qua các lần nạp.
                g => (IReadOnlyList<string>)g
                    .OrderBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
                    .Select(r => r.EquipmentId!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        Interlocked.Exchange(ref _camerasByZone, byZone);
        _logger.LogInformation(
            "[Zone] Camera xem được (module CCTV): {Cameras} thiết bị / {Zones} vùng{Source}.",
            rows.Count, byZone.Count, fromSnapshot ? " TỪ SNAPSHOT (DB lỗi)" : "");

        if (byZone.Count == 0)
        {
            _logger.LogWarning(
                "[Zone] Không có camera nào thuộc module CCTV ({Module}) được gán vùng ⇒ sự cố sẽ để " +
                "trống thiết bị xem được. Kiểm tra Catalog:CctvModuleId và TmsZoneEquipment.",
                moduleId);
        }
    }
}
