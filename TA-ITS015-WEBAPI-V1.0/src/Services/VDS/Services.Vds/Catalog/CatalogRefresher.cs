using Services.Vds.Features.Journey;
using Services.Vds.Features.Events;
using Services.Vds.Features.VehicleFilter;

namespace Services.Vds.Catalog;

/// <summary>
/// Các mảng dữ liệu nạp được từ CSDL. Dùng <c>[Flags]</c> để một lượt refresh gộp nhiều mảng mà không
/// phải viết nhiều lời gọi ở phía dùng.
/// </summary>
[Flags]
public enum CatalogParts
{
    None = 0,

    /// <summary>Danh mục camera VDS — <c>TmsEquipment</c>.</summary>
    Cameras = 1 << 0,

    /// <summary>Vùng + liên kết camera↔vùng — <c>TmsZone</c>/<c>TmsZoneEquipment</c>.</summary>
    Zones = 1 << 1,

    /// <summary>Đoạn A→B (khoảng cách) — <c>TmsTrafficSegment</c>.</summary>
    Segments = 1 << 2,

    /// <summary>Danh mục hiển thị (tên vị trí/hướng) — <c>SysConfigData</c>.</summary>
    ConfigData = 1 << 3,

    /// <summary>Mapping sự kiện VDS↔ITS và mapping cỡ xe — <c>VdsConfig</c>.</summary>
    Mappings = 1 << 4,

    /// <summary>Cấu hình vận hành, skip/dedup/display và nhóm đếm — <c>VdsConfig</c>.</summary>
    OpConfig = 1 << 5,

    /// <summary>Danh sách lọc xe (biển số).</summary>
    VehicleFilters = 1 << 6,

    /// <summary>Mọi mảng có thể thay đổi.</summary>
    All = Cameras | Zones | Segments | ConfigData | Mappings | OpConfig | VehicleFilters,
}

/// <summary>
/// Author: KienLT
/// Description: <b>NƠI DUY NHẤT</b> biết "mảng dữ liệu nào nạp bằng bộ nạp nào". Mọi đường refresh đều đi
/// qua đây: nạp lúc khởi động (<c>VdsServiceManager.InitAsync</c>), nạp lại theo chu kỳ
/// (<c>CatalogReloadJob</c>), và nạp lại theo yêu cầu NATS (<c>VdsRefreshSubscriber</c>).
///
/// <para><b>Vì sao gom lại</b>: trước đây ba nơi đó mỗi nơi tự viết chuỗi <c>LoadAsync</c> riêng — thêm một
/// mảng dữ liệu mới phải sửa đúng ba chỗ, quên một chỗ thì mảng đó không được nạp lại và lệch âm thầm.
/// Checklist V7 gọi đây là "hàm Refresh dùng chung".</para>
///
/// <para><b>try/catch THEO TỪNG mảng</b>: một mảng lỗi không được làm mất lượt nạp của các mảng còn lại.
/// Mỗi bộ nạp tự giữ dữ liệu đang dùng (last-known-good) khi lỗi hoặc khi truy vấn trả rỗng.</para>
///
/// <para><b>KHÔNG ném</b> (trừ huỷ theo token). Ngữ nghĩa fail-closed lúc KHỞI ĐỘNG — camera rỗng thì phải
/// chặn khởi động — thuộc về <c>VdsServiceManager.InitAsync</c>, không thuộc lớp này: giữa lúc chạy thì giữ
/// cache cũ mới là đúng.</para>
/// Created date: 04/09/2026
/// </summary>
public sealed class CatalogRefresher
{
    private readonly CameraManager _cameras;
    private readonly ZoneManager _zones;
    private readonly SegmentManager _segments;
    private readonly AutoSegmentManager _autoSegments;
    private readonly ConfigDataManager _configData;
    private readonly MappingManager _mappings;
    private readonly OpConfigManager _opConfig;
    private readonly VehicleFilterManager _vehicleFilters;
    private readonly ILogger<CatalogRefresher> _logger;
    private readonly IncidentTypeCache _incidentTypes;
    private readonly SpeedRangeResolver _speeds;

    public CatalogRefresher(
        CameraManager cameras,
        ZoneManager zones,
        SegmentManager segments,
        ConfigDataManager configData,
        MappingManager mappings,
        OpConfigManager opConfig,
        VehicleFilterManager vehicleFilters,
        ILogger<CatalogRefresher> logger,
        IncidentTypeCache incidentTypes,
        AutoSegmentManager autoSegments,
        SpeedRangeResolver speeds)
    {
        _cameras = cameras;
        _zones = zones;
        _segments = segments;
        _autoSegments = autoSegments;
        _configData = configData;
        _mappings = mappings;
        _opConfig = opConfig;
        _vehicleFilters = vehicleFilters;
        _logger = logger;
        _incidentTypes = incidentTypes;
        _speeds = speeds;
    }

    /// <summary>
    /// Phát sau mỗi lượt nạp lại (khởi động, chu kỳ, lệnh refresh NATS), khi dữ liệu đã vào RAM và TRƯỚC lúc
    /// đẩy ngưỡng xuống AID. Dành cho bên hiển thị (WPF) cập nhật lại màn hình; handler chạy trên luồng nền
    /// và lỗi của nó không được làm hỏng lượt nạp.
    /// </summary>
    public event Action<CatalogParts>? Reloaded;

    private void RaiseReloaded(CatalogParts parts)
    {
        var handlers = Reloaded;
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<CatalogParts>>())
        {
            try { handler(parts); }
            catch (Exception ex) { _logger.LogWarning(ex, "[Refresh] Handler Reloaded lỗi; bỏ qua."); }
        }
    }

    /// <summary>
    /// Nạp lại các mảng được chọn. Thứ tự cố định (camera → vùng → đoạn → danh mục hiển thị → mapping →
    /// cấu hình vận hành → danh sách biển) để log đọc được và để mảng phụ thuộc nạp sau mảng nó dựa vào.
    /// </summary>
    public async Task RefreshAsync(CatalogParts parts, CancellationToken ct = default)
    {
        if (parts == CatalogParts.None) return;

        if (parts.HasFlag(CatalogParts.Cameras))
            await SafeAsync("camera", () => _cameras.LoadAsync(ct)).ConfigureAwait(false);

        if (parts.HasFlag(CatalogParts.Zones))
            await SafeAsync("vùng", () => _zones.LoadAsync(ct)).ConfigureAwait(false);

        if (parts.HasFlag(CatalogParts.Segments))
            await SafeAsync("đoạn A→B", () => _segments.LoadAsync(ct)).ConfigureAwait(false);

        // Đoạn tự sinh phụ thuộc camera (km/tuyến/hướng), biên mặc định của đoạn và cấu hình tuyến trong
        // VdsConfig ⇒ tính lại khi một trong ba mảng đó được nạp. Đặt SAU camera và đoạn.
        if ((parts & (CatalogParts.Cameras | CatalogParts.Segments | CatalogParts.OpConfig)) != 0)
            await SafeAsync("đoạn A→B tự sinh", () => _autoSegments.LoadAsync(ct)).ConfigureAwait(false);

        if (parts.HasFlag(CatalogParts.ConfigData))
            await SafeAsync("danh mục hiển thị", () => _configData.LoadAsync(ct)).ConfigureAwait(false);

        if (parts.HasFlag(CatalogParts.Mappings))
            await SafeAsync("mapping (sự kiện + cỡ xe)", () => _mappings.LoadAsync(ct)).ConfigureAwait(false);

        // Refresh 1/0 and periodic catalog reload refresh both successful and missing lookups.
        // Also invalidate on loader failure: TmsEventType may have changed independently.
        if ((parts & (CatalogParts.ConfigData | CatalogParts.Mappings)) != 0)
            _incidentTypes.Invalidate();

        if (parts.HasFlag(CatalogParts.OpConfig))
            await SafeAsync("cấu hình vận hành", () => _opConfig.LoadAsync(ct)).ConfigureAwait(false);

        // Báo bên quan sát (harness WPF) TRƯỚC khi đẩy AID: đẩy AID chờ HTTP từng camera (timeout 10s/camera),
        // giao diện không nên phải chờ tới lúc đó mới thấy ngưỡng/vùng mới.
        RaiseReloaded(parts);

        // Ngưỡng làn AID = ngưỡng vùng chính của camera, thiếu thì mặc định theo hướng ⇒ đẩy lại khi camera,
        // vùng (TmsZone.MinSpeed/MaxSpeed) hoặc mặc định hướng (VdsConfig) đổi. Chạy NỀN, không await: máy chủ
        // AID chậm/không tới được từng làm khởi động chờ 10s × số camera trước khi mở ingestion.
        if ((parts & (CatalogParts.Cameras | CatalogParts.Zones | CatalogParts.OpConfig)) != 0)
            _opConfig.RequestLaneSpeedPush(() => _cameras.All, _speeds);

        if (parts.HasFlag(CatalogParts.VehicleFilters))
            await SafeAsync("danh sách lọc xe", () => _vehicleFilters.LoadAsync(ct)).ConfigureAwait(false);
    }

    /// <summary>
    /// Nạp các mảng KHÔNG chặn khởi động, dùng trong <c>InitAsync</c> sau khi camera/vùng đã nạp với ngữ
    /// nghĩa fail-closed riêng. Tách ra để <c>InitAsync</c> không phải liệt kê lại từng bộ nạp.
    /// </summary>
    public Task RefreshSoftPartsAsync(CancellationToken ct = default) =>
        RefreshAsync(
            CatalogParts.Segments | CatalogParts.ConfigData | CatalogParts.Mappings
            | CatalogParts.OpConfig | CatalogParts.VehicleFilters, ct);

    private async Task SafeAsync(string label, Func<Task> load)
    {
        try
        {
            await load().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("CatalogRefresher");
            _logger.LogWarning(ex, "[Refresh] Không nạp lại được {Label}; giữ dữ liệu đang dùng.", label);
        }
    }
}
