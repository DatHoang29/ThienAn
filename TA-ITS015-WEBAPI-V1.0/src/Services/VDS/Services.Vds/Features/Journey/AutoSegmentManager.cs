using Modules.VDS.Core.Helper;
using System.Globalization;
using Modules.VDS.Core.Constants;
using Services.Vds.Catalog;
using Services.Vds.Configuration;
using Modules.VDS.Core.Dto;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Danh mục đoạn A→B TỰ SINH giữ trong RAM (singleton), tính từ km + tuyến + hướng của camera và
/// cấu hình hình học trong <c>VdsConfig</c> (nhóm <c>vds_segment</c>: <c>vds_km_dir_…</c>, <c>vds_junction_…</c>, <c>vds_turn_…</c>).
/// Luật tính dùng CHUNG với WebAPI (<see cref="VdsBaseHelper.SegmentTopologyHp"/> ở Modules.VDS.Core), nên đoạn worker tự
/// sinh và đoạn nút "Sinh đoạn" trên web ra cùng một kết quả.
///
/// <para><b>Khi nào tính lại:</b> mỗi lượt nạp camera, đoạn hoặc VdsConfig (<see cref="CatalogRefresher"/>)
/// — tức là sửa km camera, sửa cấu hình tuyến/nút trên web rồi gửi refresh là có hiệu lực, không cần restart.</para>
///
/// <para><b>Cache ra file:</b> mỗi lượt tính thành công ghi <c>state/catalog/catalog-auto-segments.json</c>. Đọc
/// VdsConfig lỗi (SQL Server chết) thì nạp lại file này; không có file thì giữ bản đang dùng trong RAM.</para>
///
/// <para><b>Mã đoạn:</b> <c>SegmentId</c> là mã tất định <c>S:VDS-01&gt;VDS-03</c> / <c>T:J01_1:VDS-04&gt;VDS-08</c>,
/// nên khoá hành trình và <c>EventId</c> sự cố quá tốc độ trung bình ổn định qua các lần restart.</para>
/// Created date: 29/09/2026
/// </summary>
public sealed class AutoSegmentManager
{
    private static readonly string[] TopologyGroups =
        { VdsConfigConst.Group.Segment };

    private readonly VdsCatalogRepository _catalog;
    private readonly CameraManager _cameras;
    private readonly SegmentManager _segments;
    private readonly IVdsConfigManager _config;
    private readonly CatalogSnapshotStore _snapshots;
    private readonly ILogger<AutoSegmentManager> _logger;

    private volatile SegmentIndex _index = SegmentIndex.Empty;

    public AutoSegmentManager(
        VdsCatalogRepository catalog,
        CameraManager cameras,
        SegmentManager segments,
        IVdsConfigManager config,
        CatalogSnapshotStore snapshots,
        ILogger<AutoSegmentManager> logger)
    {
        _catalog = catalog;
        _cameras = cameras;
        _segments = segments;
        _config = config;
        _snapshots = snapshots;
        _logger = logger;
    }

    public int Count => _index.All.Count;

    /// <summary>Toàn bộ đoạn tự sinh đang dùng.</summary>
    public IReadOnlyList<SegmentInfo> All => _index.All;

    /// <summary>Đoạn TỰ SINH có camera cuối là <paramref name="equipmentId"/> — tra RAM, không I/O.</summary>
    public IReadOnlyList<SegmentInfo> EndingAt(string? equipmentId) => _index.EndingAt(equipmentId);

    /// <summary>Đoạn TỰ SINH có camera đầu là <paramref name="equipmentId"/> — tra RAM, không I/O.</summary>
    public IReadOnlyList<SegmentInfo> StartingAt(string? equipmentId) => _index.StartingAt(equipmentId);

    /// <summary>
    /// Tính lại danh mục. Không NÉM (trừ huỷ): lỗi chỉ làm giữ bản cũ, không ảnh hưởng dữ liệu thô.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (!_config.Current.Journey.Enabled)
        {
            _index = SegmentIndex.Empty;
            return;
        }

        List<VdsBaseHelper.SegmentTopologyHp.TopoConfigRow> config;
        try
        {
            config = new List<VdsBaseHelper.SegmentTopologyHp.TopoConfigRow>();
            foreach (var group in TopologyGroups)
            {
                var rows = await _catalog.QueryOpConfigAsync(group, ct).ConfigureAwait(false);
                config.AddRange(rows.Select(r => new VdsBaseHelper.SegmentTopologyHp.TopoConfigRow(group, r.Code, r.Value)));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            CatalogRefreshAudit.Failed("AutoSegmentManager");
            var cached = _snapshots.TryLoad<List<SegmentInfo>>(CatalogSnapshotStore.AutoSegmentsName);
            if (cached is null)
            {
                _logger.LogWarning(ex, "[AutoSegment] Không đọc được cấu hình tuyến; giữ {Count} đoạn tự sinh đang dùng.",
                    _index.All.Count);
                return;
            }

            _index = new SegmentIndex(cached);
            _logger.LogError(ex,
                "[AutoSegment] Không đọc được cấu hình tuyến từ DB ⇒ DÙNG SNAPSHOT {Count} đoạn tự sinh của lần tính gần nhất.",
                cached.Count);
            return;
        }

        var cameras = _cameras.All
            .Select(c => new VdsBaseHelper.SegmentTopologyHp.TopoCamera(c.EquipmentId, c.Code ?? c.Label, c.Location,
                int.TryParse(c.Direction, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dir) ? dir : null,
                c.Km, c.Met))
            .ToList();

        VdsBaseHelper.SegmentTopologyHp.TopoPlan plan;
        try
        {
            plan = VdsBaseHelper.SegmentTopologyHp.Plan(cameras, config);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Plan không được ném với cấu hình sai; tới đây là lỗi lập trình — xử lý như cấu hình lỗi.
            plan = new VdsBaseHelper.SegmentTopologyHp.TopoPlan(new(), new(),
                new() { VdsSegmentTopologyMessageOutput.RawText($"Lỗi tính đoạn: {ex.GetType().Name}: {ex.Message}") });
        }
        // Thông báo của Plan là mã + tham số; worker ghi log bằng mẫu tiếng Việt dùng chung với resource vi-VN.
        foreach (var warning in plan.Warnings)
            _logger.LogWarning("[AutoSegment] {Warning}", VdsBaseHelper.SegmentTopologyHp.FormatVi(warning));

        // Lỗi CẤU HÌNH ⇒ kết quả có thể thiếu đoạn (JSON nút hỏng làm mất cả nhóm đoạn rẽ). Không thay cache đang
        // chạy bằng kết quả thiếu: giữ bản trong RAM, RAM rỗng (vừa khởi động) thì lấy file của lần tính tốt gần nhất.
        if (plan.HasErrors)
        {
            CatalogRefreshAudit.Failed("AutoSegmentConfigInvalid");
            foreach (var error in plan.Errors)
                _logger.LogError("[AutoSegment] Cấu hình tuyến lỗi: {Error}", VdsBaseHelper.SegmentTopologyHp.FormatVi(error));
            if (_index.All.Count == 0
                && _snapshots.TryLoad<List<SegmentInfo>>(CatalogSnapshotStore.AutoSegmentsName) is { } lastGood)
                _index = new SegmentIndex(lastGood);
            _logger.LogError("[AutoSegment] Không áp kết quả tính mới vì cấu hình lỗi; giữ {Count} đoạn tự sinh của lần tính tốt gần nhất.",
                _index.All.Count);
            return;
        }

        var maxDefault = _segments.TravelDefaults.MaxSeconds;
        var segments = plan.Segments.Select(p =>
        {
            var min = plan.MinTravelSeconds(p.DistanceMeter);
            return new SegmentInfo(
                SegmentId: p.Code,
                Name: p.Turn is null ? $"{p.FromCode} → {p.ToCode}" : $"{p.FromCode} → {p.ToCode} ({p.Turn})",
                FromEquipmentId: p.FromId,
                ToEquipmentId: p.ToId,
                Direction: p.Direction.ToString(CultureInfo.InvariantCulture),
                DistanceMeter: p.DistanceMeter,
                MinTravelSeconds: min,
                MaxTravelSeconds: Math.Max(min, maxDefault),
                IsAuto: true);
        }).ToList();

        // Last-known-good: tính ra RỖNG mà đang có đoạn ⇒ nghi dữ liệu nạp dở (camera chưa lên), giữ bản cũ.
        if (segments.Count == 0 && _index.All.Count > 0)
        {
            _logger.LogWarning("[AutoSegment] Tính ra 0 đoạn tự sinh; giữ {Count} đoạn đang dùng.", _index.All.Count);
            return;
        }

        _index = new SegmentIndex(segments);
        _snapshots.TrySave(CatalogSnapshotStore.AutoSegmentsName, segments);
        _logger.LogInformation("[AutoSegment] Tính {Count} đoạn tự sinh từ {Cameras} camera: {List}.",
            segments.Count, cameras.Count, string.Join(", ", segments.Select(s => $"{s.SegmentId}={s.DistanceMeter}m")));
    }
}
