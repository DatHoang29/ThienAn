using Microsoft.Extensions.Hosting;
using Services.Vds.Catalog;
using Services.Vds.Configuration;
using Services.Vds.Shared.Messaging;

namespace Services.Vds.Features.ZoneTraffic;

/// <summary>
/// Author: KienLT
/// Description: Phát lưu lượng theo VÙNG lên <see cref="VdsSubjects.Traffic"/>.
///
/// Cùng mô hình nhịp với <see cref="Counting.CountPublishWorker"/> và cùng lý do: mỗi lượt phát là một
/// truy vấn gộp trên QuestDB nên không thể phát theo từng lượt xe.
/// <list type="bullet">
/// <item><c>TrafficPublishDebounceMs</c> — gom các lượt xe sát nhau thành một lần phát.</item>
/// <item><c>TrafficMinPublishIntervalSeconds</c> — trần tần suất.</item>
/// <item><c>TrafficRefreshIntervalSeconds</c> — vẫn phát khi vắng xe để cửa sổ trượt và chuỗi theo giờ tiếp
/// tục tiến.</item>
/// </list>
///
/// Lượt phát đầu sau khi khởi động luôn là <c>snapshot</c> để bên nhận có nền đầy đủ trước khi nhận
/// <c>delta</c>. Lượt định kỳ cũng là snapshot vì nó không gắn với vùng nào cụ thể.
/// Created date: 27/08/2026
/// </summary>
public sealed class ZoneTrafficPublishWorker : BackgroundService
{
    private readonly ZoneTrafficPublishTrigger _trigger;
    private readonly ZoneTrafficPayloadBuilder _builder;
    private readonly VdsPublisher _publisher;
    private readonly ZoneManager _zones;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<ZoneTrafficPublishWorker> _logger;

    private DateTime _lastPublishUtc = DateTime.MinValue;
    private bool _snapshotSent;

    public ZoneTrafficPublishWorker(
        ZoneTrafficPublishTrigger trigger,
        ZoneTrafficPayloadBuilder builder,
        VdsPublisher publisher,
        ZoneManager zones,
        IVdsConfigManager config,
        ILogger<ZoneTrafficPublishWorker> logger)
    {
        _trigger = trigger;
        _builder = builder;
        _publisher = publisher;
        _zones = zones;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var runtime = _config.Current;
            var publish = runtime.Publish;

            if (!publish.PublishTraffic || !runtime.QuestEnabled)
            {
                // Không tắt hẳn vòng lặp: cả hai điều kiện đổi được lúc chạy.
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                continue;
            }

            var refreshSeconds = Math.Max(0, publish.TrafficRefreshIntervalSeconds);
            var waitTimeout = refreshSeconds > 0
                ? TimeSpan.FromSeconds(refreshSeconds)
                : Timeout.InfiniteTimeSpan;

            bool triggered;
            try
            {
                triggered = await _trigger.WaitAsync(waitTimeout, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (triggered && publish.TrafficPublishDebounceMs > 0)
            {
                try
                {
                    await Task.Delay(publish.TrafficPublishDebounceMs, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            await PublishOnceAsync(triggered, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task PublishOnceAsync(bool triggered, CancellationToken ct)
    {
        var publish = _config.Current.Publish;
        var changed = _trigger.DrainChanged();

        // Trần tần suất: thay đổi vẫn nằm trong tập của lượt sau nên không mất số, chỉ hoãn.
        var minInterval = TimeSpan.FromSeconds(Math.Max(0, publish.TrafficMinPublishIntervalSeconds));
        var sinceLast = DateTime.UtcNow - _lastPublishUtc;
        if (_snapshotSent && sinceLast < minInterval)
        {
            foreach (var id in changed) _trigger.Notify(id);
            return;
        }

        // Chưa nạp xong danh mục vùng thì payload rỗng nghĩa; chờ lượt sau.
        if (_zones.EquipmentByZone.Count == 0) return;

        var sendDelta = _snapshotSent && triggered && changed.Count > 0;

        try
        {
            var nowUtc = DateTime.UtcNow;
            var payload = sendDelta
                ? await _builder.BuildDeltaAsync(nowUtc, changed, ct).ConfigureAwait(false)
                : await _builder.BuildAsync(nowUtc, ct).ConfigureAwait(false);

            var sent = await _publisher
                .SendAsync(VdsSubjects.Traffic, payload, ct)
                .ConfigureAwait(false);

            _lastPublishUtc = nowUtc;
            if (!sendDelta && sent) _snapshotSent = true;

            _logger.LogDebug(
                "[ZoneTraffic] Đã phát {Mode} ({Changed} vùng đổi), sent={Sent}.",
                sendDelta ? "delta" : "snapshot", changed.Count, sent);
        }
        catch (OperationCanceledException)
        {
            // Host đang dừng: trả lại thay đổi cho lượt sau (nếu còn) rồi thoát êm.
            foreach (var id in changed) _trigger.Notify(id);
        }
        catch (Exception ex)
        {
            // Truy vấn lỗi (QuestDB xuống, bảng chưa có) KHÔNG được làm chết worker: lần sau thử lại.
            foreach (var id in changed) _trigger.Notify(id);
            _logger.LogError(ex, "[ZoneTraffic] Không dựng/phát được lưu lượng theo vùng; thử lại lượt sau.");

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
