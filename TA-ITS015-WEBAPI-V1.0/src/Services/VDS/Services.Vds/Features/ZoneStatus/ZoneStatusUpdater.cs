using System.Collections.Concurrent;
using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Configuration;
using Services.Vds.Catalog;
using Shared.Utility.Utilities;

namespace Services.Vds.Features.ZoneStatus;

/// <summary>
/// Author: KienLT
/// Description: Nối máy trạng thái vùng với danh mục vùng, DB và NATS. Máy trạng thái nằm ở
/// <see cref="ZoneStatusTracker"/> (thuần RAM); class này chỉ điều phối và chịu trách nhiệm KHÔNG để lỗi
/// lan ra pipeline.
///
/// <para><b>Ba quyết định định hình tính năng:</b>
/// <list type="bullet">
/// <item>Lấy mẫu ở tầng dữ liệu THÔ, không phải tầng đếm. Tầng đếm lọc theo biển số, nên ban đêm hoặc
/// camera đọc biển kém sẽ mất phần lớn mẫu — trong khi tình trạng vùng là thứ cần liên tục nhất.</item>
/// <item>Cập nhật MỌI vùng chứa camera: quan hệ camera ↔ vùng là n-n, chỉ lấy vùng chính sẽ bỏ sót.</item>
/// <item><b>BỎ QUA bản tin bù lịch sử</b>: replay quá khứ không được lái trạng thái hiện tại. Đây là chỗ
/// duy nhất trong pipeline mà cờ bù lịch sử mang nghĩa nghiệp vụ thật.</item>
/// <item>Ngưỡng tối thiểu dùng cho đánh giá chậm theo tốc độ lấy THEO TỪNG VÙNG qua
/// <see cref="SpeedRangeResolver"/> (vùng → mặc định theo hướng của camera), không dùng ngưỡng cố định.</item>
/// </list></para>
/// Created date: 26/08/2026
/// </summary>
public sealed class ZoneStatusUpdater
{
    private readonly ZoneManager _zones;
    private readonly ZoneStatusTracker _tracker;
    private readonly ZoneStatusRepository _repo;
    private readonly VdsPublisher _publisher;
    private readonly IVdsConfigManager _config;
    private readonly SpeedRangeResolver _speeds;
    private readonly ILogger<ZoneStatusUpdater> _logger;

    /// <summary>
    /// Khoá ghi THEO TỪNG VÙNG. Bảng trạng thái thuộc bên TMS nên VDS không được thêm UNIQUE trên vùng;
    /// thay vào đó chặn ở tầng ứng dụng để hai luồng (bản tin và quét theo giờ) không cùng lúc thấy "vùng
    /// chưa có dòng" rồi cùng chèn hai dòng cho một vùng.
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _writeGates =
        new(StringComparer.OrdinalIgnoreCase);

    private bool _warnedInactive;
    private bool _warnedNoSlowCode;

    public ZoneStatusUpdater(
        ZoneManager zones,
        ZoneStatusTracker tracker,
        ZoneStatusRepository repo,
        VdsPublisher publisher,
        IVdsConfigManager config,
        SpeedRangeResolver speeds,
        ILogger<ZoneStatusUpdater> logger)
    {
        _zones = zones;
        _tracker = tracker;
        _repo = repo;
        _publisher = publisher;
        _config = config;
        _speeds = speeds;
        _logger = logger;
    }

    /// <summary>
    /// Ghi nhận một bản tin đã lưu dữ liệu thô. <paramref name="isBackfill"/> = true ⇒ bỏ qua hoàn toàn.
    ///
    /// KHÔNG BAO GIỜ ném lỗi: trạng thái vùng là số liệu phụ, không được làm đứt luồng ghi dữ liệu chính.
    /// </summary>
    /// <param name="camera">Camera đã tra từ danh mục — hướng của nó là mặc định khi vùng không khai ngưỡng.</param>
    public async Task ObserveAsync(VdsTrafficData row, CameraInfo camera, bool isBackfill, CancellationToken ct)
    {
        if (isBackfill) return;

        var cfg = _config.Current.ZoneStatus;
        if (!EnsureActive(cfg)) return;

        try
        {
            var zones = _zones.ZonesOf(row.EquipmentId);
            if (zones.Count == 0) return;

            var eventKind = EventKindOf(row.AidEventType ?? row.EventType);
            var speed = row.Speed;

            // Bản tin không có tốc độ và cũng không phải sự kiện nghẽn/chậm thì không có gì để ghi nhận.
            if (speed is null && eventKind == ZoneEventKind.None) return;

            var now = BaseUtil.TimeServer.GetTimeServer();

            // row.DetectTime là UTC (VdsTrafficDataMapper.ResolveDetectTime luôn trả UTC), còn now là GIỜ
            // HỆ THỐNG. Truyền thẳng UTC vào tracker thì (now - detectTime) luôn lệch đúng số giờ của múi
            // giờ máy chủ — với UTC+7 là 7 tiếng, nên SampleMaxAgeSeconds (mặc định 120s) loại SẠCH mọi mẫu
            // và trạng thái vùng KHÔNG BAO GIỜ được ghi, không có lỗi nào để lần ra.
            // Cùng loại bẫy mà IncidentPromoter đã ghi chú khi dựng StartDate.
            var detectLocal = BaseUtil.TimeServer.GetTimeServer(row.DetectTime);

            foreach (var zoneId in zones)
            {
                // Ngưỡng tối thiểu của CHÍNH vùng này (camera thuộc nhiều vùng thì mỗi vùng một ngưỡng), thiếu
                // thì mặc định theo hướng của vùng (LaneId), vùng không khai hướng mới dùng hướng camera. Chỉ cần
                // min, không đòi đủ cặp min/max. Chỉ cần khi bật đánh giá chậm theo tốc độ.
                var minSpeed = cfg.SpeedSlow.Enabled
                    ? _speeds.MinSpeedOfZone(zoneId, camera.Direction)
                    : null;

                var snapshot = _tracker.Observe(
                    zoneId, detectLocal, speed, eventKind, minSpeed, now, cfg, camera.Direction);
                if (snapshot is ZoneStatusSnapshot due)
                    await WriteAsync(due, cfg, row.EquipmentId, speed, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "[ZoneStatus] Ghi nhận thất bại cho camera {Equipment}; bỏ qua bản tin này.", row.EquipmentId);
        }
    }

    /// <summary>
    /// Quét theo giờ: hạ chốt sự kiện (nghẽn/chậm) đã hết hạn và trượt cửa sổ tốc độ.
    /// </summary>
    /// <returns>Số vùng đã ghi.</returns>
    public async Task<int> SweepAsync(CancellationToken ct)
    {
        var cfg = _config.Current.ZoneStatus;
        if (!EnsureActive(cfg)) return 0;

        var written = 0;
        // Tra LẠI V_min theo danh mục hiện tại: danh mục vùng/hướng vừa nạp lại phải có hiệu lực cả với vùng
        // không còn bản tin mới (Observe là nơi duy nhất tra ngưỡng trước đây).
        foreach (var snapshot in _tracker.Sweep(
                     BaseUtil.TimeServer.GetTimeServer(), cfg, _speeds.MinSpeedOfZone))
        {
            // Thay đổi này do THỜI GIAN, không camera nào gây ra ⇒ để trống thiết bị và tốc độ mẫu.
            if (await WriteAsync(snapshot, cfg, equipmentId: null, sampleSpeed: null, ct).ConfigureAwait(false))
                written++;
        }

        return written;
    }

    /// <summary>
    /// ÁP trạng thái một vùng rồi ghi ngay — dùng cho harness và thao tác vận hành đặt tay.
    /// </summary>
    /// <returns>true nếu có ghi DB.</returns>
    public async Task<bool> ForceConditionAsync(
        string zoneId, ZoneCondition condition, string? equipmentId, CancellationToken ct)
    {
        var cfg = _config.Current.ZoneStatus;
        if (string.IsNullOrWhiteSpace(zoneId) || !EnsureActive(cfg)) return false;

        var snapshot = _tracker.ForceCondition(
            zoneId, condition, BaseUtil.TimeServer.GetTimeServer(), cfg);

        return await WriteAsync(snapshot, cfg, equipmentId, sampleSpeed: null, ct).ConfigureAwait(false);
    }

    /// <summary>Huỷ chốt sự kiện <c>Congestion</c> của một vùng ngay lập tức (không chờ TTL).</summary>
    /// <returns>true nếu có ghi DB (trạng thái/số liệu đổi, hoặc tới hạn ghi giữ nhịp).</returns>
    public Task<bool> CancelCongestionEventAsync(string zoneId, CancellationToken ct)
        => CancelEventAsync(zoneId, ZoneEventKind.Congestion, ct);

    /// <summary>
    /// Huỷ chốt sự kiện <c>Slow Moving</c> của một vùng ngay lập tức. Bậc chậm theo TỐC ĐỘ (nếu bật) giữ
    /// nguyên — xe vẫn chạy chậm thì vùng vẫn chậm.
    /// </summary>
    /// <returns>true nếu có ghi DB (trạng thái/số liệu đổi, hoặc tới hạn ghi giữ nhịp).</returns>
    public Task<bool> CancelSlowEventAsync(string zoneId, CancellationToken ct)
        => CancelEventAsync(zoneId, ZoneEventKind.Slow, ct);

    private async Task<bool> CancelEventAsync(string zoneId, ZoneEventKind kind, CancellationToken ct)
    {
        var cfg = _config.Current.ZoneStatus;
        if (string.IsNullOrWhiteSpace(zoneId) || !EnsureActive(cfg)) return false;

        if (_tracker.ClearEvent(zoneId, kind, BaseUtil.TimeServer.GetTimeServer(), cfg)
            is not ZoneStatusSnapshot due)
        {
            return false;
        }

        return await WriteAsync(due, cfg, equipmentId: null, sampleSpeed: null, ct).ConfigureAwait(false);
    }

    /// <summary>Loại bản tin ⇒ loại sự kiện của trạng thái vùng. Nhận cả tên v1.0 lẫn v1.3 qua Canonical.</summary>
    private static ZoneEventKind EventKindOf(string? eventType)
    {
        var canonical = VdsEventTypes.Canonical(eventType);
        if (string.Equals(canonical, VdsEventTypes.Congestion, StringComparison.OrdinalIgnoreCase))
            return ZoneEventKind.Congestion;
        if (string.Equals(canonical, VdsEventTypes.SlowMoving, StringComparison.OrdinalIgnoreCase))
            return ZoneEventKind.Slow;
        return ZoneEventKind.None;
    }

    private async Task<bool> WriteAsync(
        ZoneStatusSnapshot snapshot,
        ZoneStatusOptions cfg,
        string? equipmentId,
        double? sampleSpeed,
        CancellationToken ct)
    {
        var condition = ConditionCode(snapshot.Condition, cfg);

        var gate = _writeGates.GetOrAdd(snapshot.ZoneId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _repo
                .UpsertStatusAsync(
                    snapshot.ZoneId, snapshot.AverageSpeedText, condition, snapshot.UpdateTime, ct)
                .ConfigureAwait(false);

            // Lượt giữ nhịp lặp lại theo nhịp quét cho mọi vùng đang chậm/nghẽn ⇒ Debug, tránh ngập log.
            _logger.Log(snapshot.KeepAlive ? LogLevel.Debug : LogLevel.Information,
                "[ZoneStatus] vùng={Zone} tình trạng={Condition} tốcĐộTB={Avg} giữNhịp={KeepAlive}",
                snapshot.ZoneId, condition, snapshot.AverageSpeedText ?? "-", snapshot.KeepAlive);

            // Zone status is persisted for module SSE; no NATS broadcast.
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Máy trạng thái ĐÃ đánh dấu "đã ghi" trước khi tới đây, nên phải xoá dấu: không xoá thì lần đổi
            // trạng thái này mất hẳn — các lượt sau thấy "không đổi so với lần ghi trước" nên im lặng và DB
            // đứng ở giá trị cũ cho tới lần đổi tiếp theo.
            _tracker.InvalidateWriteMark(snapshot.ZoneId);

            _logger.LogError(ex, "[ZoneStatus] Ghi trạng thái vùng {Zone} thất bại; sẽ ghi lại ở lượt sau.",
                snapshot.ZoneId);
            return false;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Mã chuỗi ghi vào cột tình trạng cho từng bậc.
    ///
    /// Bậc CHÙNG chưa khai mã ⇒ lùi về mã THÔNG kèm cảnh báo MỘT lần: thà hiển thị thiếu một bậc còn hơn ghi
    /// chuỗi rỗng vào cột mà FE đang đọc để đổ màu.
    /// </summary>
    private string ConditionCode(ZoneCondition condition, ZoneStatusOptions cfg)
    {
        switch (condition)
        {
            case ZoneCondition.Congested:
                return cfg.CongestedCondition.Trim();

            case ZoneCondition.Slow when !string.IsNullOrWhiteSpace(cfg.SlowCondition):
                return cfg.SlowCondition.Trim();

            case ZoneCondition.Slow:
                if (!_warnedNoSlowCode)
                {
                    _warnedNoSlowCode = true;
                    _logger.LogWarning(
                        "[ZoneStatus] Chưa khai Service:ZoneStatus:SlowCondition ⇒ bậc CHÙNG ghi bằng mã thông " +
                        "'{Normal}'. Lấy đúng mã FE dùng: SELECT DISTINCT Condition FROM TmsZoneStatus.",
                        cfg.NormalCondition.Trim());
                }

                return cfg.NormalCondition.Trim();

            default:
                return cfg.NormalCondition.Trim();
        }
    }

    /// <summary>
    /// Có ghi được không. Chưa khai đủ bộ mã thông/nghẽn ⇒ TẮT và cảnh báo MỘT lần, không ghi giá
    /// trị bịa vào bảng FE đang dùng.
    /// </summary>
    private bool EnsureActive(ZoneStatusOptions cfg)
    {
        if (cfg.Enabled && cfg.IsUsable) return true;

        if (!_warnedInactive)
        {
            _warnedInactive = true;

            if (!cfg.Enabled)
            {
                _logger.LogInformation(
                    "[ZoneStatus] Service:ZoneStatus:Enabled = false ⇒ không cập nhật trạng thái vùng.");
            }
            else
            {
                _logger.LogError(
                    "[ZoneStatus] Chưa khai Service:ZoneStatus:NormalCondition / CongestedCondition ⇒ KHÔNG cập nhật " +
                    "trạng thái vùng. Lấy đúng bộ mã FE đang dùng: " +
                    "SELECT DISTINCT Condition FROM TmsZoneStatus.");
            }
        }

        return false;
    }
}
