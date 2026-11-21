using Services.Vds.Catalog;
using Services.Vds.Configuration;
using Modules.VDS.Core.Constants;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Thông tin một đoạn A→B (khoảng cách khai tay). <c>record</c> nên bất biến: đã vào cache
/// thì không ai sửa được tại chỗ.
/// Created date: 04/09/2026
/// </summary>
/// <param name="SegmentId"><c>TmsTrafficSegment.ID</c>.</param>
/// <param name="FromEquipmentId">Camera điểm A (<c>TmsEquipment.ID</c>).</param>
/// <param name="ToEquipmentId">Camera điểm B (<c>TmsEquipment.ID</c>).</param>
/// <param name="DistanceMeter">Khoảng cách khai tay (mét) — mẫu số của tốc độ trung bình.</param>
/// <param name="MinTravelSeconds">Biên dưới hợp lệ của thời gian di chuyển (giây).</param>
/// <param name="MaxTravelSeconds">Biên trên hợp lệ của thời gian di chuyển (giây).</param>
/// <param name="IsAuto">
/// <c>true</c> = đoạn TỰ SINH từ km/tuyến/hướng (<see cref="AutoSegmentManager"/>), <c>SegmentId</c> là mã tất
/// định dạng <c>S:VDS-01&gt;VDS-03</c>; <c>false</c> = đoạn KHAI trong <c>TmsTrafficSegment</c>.
/// </param>
public sealed record SegmentInfo(
    string SegmentId, string? Name,
    string FromEquipmentId, string ToEquipmentId,
    string? Direction, int DistanceMeter,
    int MinTravelSeconds, int MaxTravelSeconds, bool IsAuto = false);

/// <summary>
/// Author: KienLT
/// Description: Chỉ mục đoạn theo CAMERA CUỐI, dựng một lần mỗi lượt nạp. Đổi nguyên khối (một tham chiếu)
/// để luồng đọc không bao giờ thấy danh sách mới đi cùng chỉ mục cũ.
/// Created date: 29/09/2026
/// </summary>
public sealed class SegmentIndex
{
    public static readonly SegmentIndex Empty = new(Array.Empty<SegmentInfo>());

    private readonly Dictionary<string, IReadOnlyList<SegmentInfo>> _byTo;
    private readonly Dictionary<string, IReadOnlyList<SegmentInfo>> _byFrom;

    public SegmentIndex(IReadOnlyList<SegmentInfo> all)
    {
        All = all;
        _byTo = all.GroupBy(s => s.ToEquipmentId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SegmentInfo>)g.ToList(), StringComparer.OrdinalIgnoreCase);
        _byFrom = all.GroupBy(s => s.FromEquipmentId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SegmentInfo>)g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<SegmentInfo> All { get; }

    /// <summary>Các đoạn có camera cuối là <paramref name="equipmentId"/>; rỗng nếu không có.</summary>
    public IReadOnlyList<SegmentInfo> EndingAt(string? equipmentId)
        => equipmentId is not null && _byTo.TryGetValue(equipmentId, out var list) ? list : Array.Empty<SegmentInfo>();

    /// <summary>Các đoạn có camera ĐẦU là <paramref name="equipmentId"/>; rỗng nếu không có.</summary>
    public IReadOnlyList<SegmentInfo> StartingAt(string? equipmentId)
        => equipmentId is not null && _byFrom.TryGetValue(equipmentId, out var list) ? list : Array.Empty<SegmentInfo>();
}

/// <summary>
/// Author: KienLT
/// Description: Danh mục đoạn A→B giữ trong RAM (singleton). Nguồn là <c>TmsTrafficSegment</c> trên SQL
/// Server, đọc qua <see cref="VdsCatalogRepository.QuerySegmentsAsync"/>.
///
/// <para>Chỉ <see cref="JourneyJob"/> dùng danh mục này — việc ghép hành trình so trực tiếp
/// <c>EquipmentId</c> với <c>FromEquipmentId</c>/<c>ToEquipmentId</c>. Một camera có thể thuộc NHIỀU đoạn.</para>
///
/// <para>Đoạn có <c>DistanceMeter &lt;= 0</c> bị loại: nó là mẫu số của tốc độ trung bình, không dương thì
/// không tính được.</para>
///
/// Nạp lại an toàn kiểu last-known-good: kết quả rỗng hoặc lỗi truy vấn thì GIỮ danh mục đang dùng,
/// không xoá trắng — giống <see cref="CameraManager"/>.
/// Created date: 04/09/2026
/// </summary>
public sealed class SegmentManager
{
    private readonly VdsCatalogRepository _catalog;
    private readonly IVdsConfigManager _config;
    private readonly CatalogSnapshotStore _snapshots;
    private readonly ILogger<SegmentManager> _logger;

    private volatile SegmentIndex _index = SegmentIndex.Empty;
    private (int MinSeconds, int MaxSeconds) _defaults =
        (VdsJourneyTravelConst.CodeMinSeconds, VdsJourneyTravelConst.CodeMaxSeconds);

    public SegmentManager(
        VdsCatalogRepository catalog,
        IVdsConfigManager config,
        CatalogSnapshotStore snapshots,
        ILogger<SegmentManager> logger)
    {
        _catalog = catalog;
        _config = config;
        _snapshots = snapshots;
        _logger = logger;
    }

    public int Count => _index.All.Count;

    /// <summary>Toàn bộ đoạn đang hoạt động.</summary>
    public IReadOnlyList<SegmentInfo> All => _index.All;

    /// <summary>Đoạn KHAI có camera cuối là <paramref name="equipmentId"/> — tra RAM, không I/O.</summary>
    public IReadOnlyList<SegmentInfo> EndingAt(string? equipmentId) => _index.EndingAt(equipmentId);

    /// <summary>Đoạn KHAI có camera đầu là <paramref name="equipmentId"/> — tra RAM, không I/O.</summary>
    public IReadOnlyList<SegmentInfo> StartingAt(string? equipmentId) => _index.StartingAt(equipmentId);

    /// <summary>Biên thời gian mặc định (vds_config_default) của lượt nạp gần nhất — đoạn tự sinh dùng chung.</summary>
    public (int MinSeconds, int MaxSeconds) TravelDefaults => _defaults;

    /// <summary>
    /// Nạp lại danh mục đoạn từ SQL Server. Không NÉM: đoạn thiếu chỉ ảnh hưởng phần hành trình, không
    /// ảnh hưởng dữ liệu thô — nên khác <see cref="CameraManager.LoadAsync"/> (camera thiếu thì chặn khởi
    /// động), ở đây luôn giữ cache cũ khi lỗi.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (!_config.Current.Journey.Enabled)
        {
            _index = SegmentIndex.Empty;
            return;
        }

        // Biên trên từng đoạn lấy từ TmsTrafficSegment. VdsConfig chỉ cấp giá trị dự phòng cho ô trống.
        var defaults = await LoadDefaultsAsync(ct).ConfigureAwait(false);
        List<VdsCatalogRepository.SegmentRow> rows;
        try
        {
            rows = await _catalog.QuerySegmentsAsync(ct).ConfigureAwait(false);
            _snapshots.TrySave(CatalogSnapshotStore.SegmentsName, rows);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("SegmentManager");
            // Đường lùi: snapshot lần nạp gần nhất. Không có thì giữ cache RAM như trước.
            var cached = _snapshots.TryLoad<List<VdsCatalogRepository.SegmentRow>>(
                CatalogSnapshotStore.SegmentsName);

            if (cached is null)
            {
                _logger.LogWarning(ex, "[Segment] Không nạp được danh mục đoạn; giữ {Count} đoạn đang dùng.",
                    _index.All.Count);
                return;
            }

            _logger.LogError(ex,
                "[Segment] Không nạp được danh mục đoạn từ DB ⇒ DÙNG SNAPSHOT {Count} đoạn của lần nạp gần nhất.",
                cached.Count);
            rows = cached;
        }

        var segments = new List<SegmentInfo>();
        var dropped = 0;
        foreach (var r in rows)
        {
            var distance = r.DistanceMeter ?? 0;
            if (distance <= 0)
            {
                CatalogRefreshAudit.Failed("SegmentDistanceInvalid");
                dropped++;
                continue;
            }

            var min = r.MinTravelSeconds is >= VdsJourneyTravelConst.CodeMinSeconds
                and <= VdsJourneyTravelConst.MaxSupportedSeconds
                ? r.MinTravelSeconds.Value : defaults.MinSeconds;
            var max = r.MaxTravelSeconds is >= VdsJourneyTravelConst.CodeMinSeconds
                and <= VdsJourneyTravelConst.MaxSupportedSeconds
                ? r.MaxTravelSeconds.Value : defaults.MaxSeconds;
            if (min > max)
            {
                CatalogRefreshAudit.Failed("SegmentTravelBoundsInvalid");
                _logger.LogWarning("[Segment] Bỏ đoạn {Segment}: MinTravelSeconds {Min}s > MaxTravelSeconds {Max}s.",
                    r.Id, min, max);
                continue;
            }

            segments.Add(new SegmentInfo(
                SegmentId: r.Id,
                Name: r.Name,
                FromEquipmentId: r.FromEquipmentId!,
                ToEquipmentId: r.ToEquipmentId!,
                Direction: r.Direction,
                DistanceMeter: distance,
                MinTravelSeconds: min,
                MaxTravelSeconds: max));
        }

        if (dropped > 0)
            _logger.LogWarning("[Segment] Bỏ {Count} đoạn vì DistanceMeter <= 0.", dropped);

        _index = new SegmentIndex(segments);
        _logger.LogInformation("[Segment] Nạp {Count} đoạn A→B.", segments.Count);
    }

    private async Task<(int MinSeconds, int MaxSeconds)> LoadDefaultsAsync(CancellationToken ct)
    {
        try
        {
            var rows = await _catalog.QueryOpConfigAsync(VdsConfigConst.Group.ConfigDefault, ct)
                .ConfigureAwait(false);
            var minRaw = rows.FirstOrDefault(r => string.Equals(r.Code,
                VdsConfigConst.Code.JourneyMinTravelDefault, StringComparison.OrdinalIgnoreCase))?.Value;
            var maxRaw = rows.FirstOrDefault(r => string.Equals(r.Code,
                VdsConfigConst.Code.JourneyMaxTravelDefault, StringComparison.OrdinalIgnoreCase))?.Value;
            var min = VdsJourneyTravelConst.ParseSeconds(minRaw);
            var max = VdsJourneyTravelConst.ParseSeconds(maxRaw);
            if ((minRaw is not null && min is null)
                || (maxRaw is not null && max is null)
                || (min ?? VdsJourneyTravelConst.CodeMinSeconds)
                    > (max ?? VdsJourneyTravelConst.CodeMaxSeconds))
                _logger.LogWarning("[Segment] Biên thời gian mặc định trong VdsConfig không hợp lệ; dùng giá trị dự phòng của code.");
            _defaults = VdsJourneyTravelConst.Resolve(minRaw, maxRaw);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Services.Vds.Catalog.CatalogRefreshAudit.Failed("SegmentManager");
            _logger.LogWarning(ex, "[Segment] Không đọc được biên mặc định từ VdsConfig; giữ giá trị đã nạp.");
        }

        return _defaults;
    }
}
