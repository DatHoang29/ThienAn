using Microsoft.Extensions.Hosting;
using Services.Vds.Configuration;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Worker đọc <see cref="JourneyArrivalQueue"/> và ghép hành trình theo từng lượt xe
/// (<see cref="JourneyArrivalProcessor"/>). Một lượt lỗi chỉ ghi log rồi đi tiếp: dữ liệu thô đã nằm trong
/// QuestDB nên lượt quét đối soát bù được, còn dừng worker là mất cả những chuyến phía sau.
/// Created date: 29/09/2026
/// </summary>
public sealed class JourneyArrivalWorker : BackgroundService
{
    private readonly JourneyArrivalQueue _queue;
    private readonly JourneyArrivalProcessor _processor;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<JourneyArrivalWorker> _logger;
    private long _questUnavailable;

    public JourneyArrivalWorker(JourneyArrivalQueue queue, JourneyArrivalProcessor processor,
        IVdsConfigManager config, ILogger<JourneyArrivalWorker> logger)
    {
        _queue = queue;
        _processor = processor;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _config.Current.Journey;
        if (!options.Enabled || !options.IsArrivalTrigger)
        {
            _logger.LogInformation("[Journey] Ghép theo bản tin đang tắt (Enabled={Enabled}, Trigger={Trigger}).",
                options.Enabled, options.Trigger);
            return;
        }

        _logger.LogInformation("[Journey] Ghép theo bản tin: bật; lượt quét đối soát mỗi {Interval}s.",
            options.EffectiveIntervalSeconds);

        try
        {
            await foreach (var arrival in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                // Chờ tới hạn của CHÍNH mục này (hàng đợi FIFO nên độ chờ không cộng dồn): lượt ở camera kia và dòng
                // QuestDB kịp hiện ra trước khi ghép.
                var due = arrival.EnqueuedUtc.AddSeconds(_config.Current.Journey.EffectiveArrivalDelaySeconds) - DateTime.UtcNow;
                if (due > TimeSpan.Zero) await Task.Delay(due, stoppingToken).ConfigureAwait(false);
                try
                {
                    await _processor.ProcessAsync(arrival, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (QuestDbUnavailableException)
                {
                    // Cầu dao QuestDB đang mở: lượt quét đối soát sẽ bù. Log thưa để không ngập log.
                    if (Interlocked.Increment(ref _questUnavailable) % 100 == 1)
                        _logger.LogWarning("[Journey] QuestDB không sẵn sàng; bỏ {Count} lượt ghép theo bản tin, chờ đối soát.",
                            Interlocked.Read(ref _questUnavailable));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Journey] Lỗi ghép theo bản tin camera {Equipment} lúc {At:O}; chờ đối soát.",
                        arrival.EquipmentId, arrival.DetectTimeUtc);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // dừng service
        }
    }
}
