using Microsoft.Extensions.Hosting;
using Services.Vds.Configuration;

namespace Services.Vds.Features.Media;

/// <summary>
/// Author: KienLT
/// Description: Bộ tải media chạy NỀN: một số worker cố định đọc <see cref="VdsMediaQueue"/> và tải tuần tự,
/// nên số lượt tải song song bị chặn ở đúng số worker dù pipeline nhận vài chục bản tin mỗi giây.
///
/// Mô hình hàng đợi + worker thay cho lối "mỗi bản tin tự sinh một task tải rồi bỏ đó": lối đó không giới hạn
/// số tải song song, lỗi không ai quan sát, và token của frame bị huỷ sẽ cắt ngang việc tải.
///
/// Khi dừng service: đóng hàng đợi rồi cho worker tải hết việc còn lại trong hạn drain; quá hạn thì huỷ.
/// Created date: 26/08/2026
/// </summary>
public sealed class VdsMediaDownloader : BackgroundService
{
    private readonly VdsMediaQueue _queue;
    private readonly VdsMediaStorage _storage;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VdsMediaDownloader> _logger;

    public VdsMediaDownloader(
        VdsMediaQueue queue,
        VdsMediaStorage storage,
        IVdsConfigManager config,
        ILogger<VdsMediaDownloader> logger)
    {
        _queue = queue;
        _storage = storage;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _config.Current.Media;
        if (!options.Enabled)
        {
            _logger.LogInformation("[Media] Service:Media:Enabled = false ⇒ không chạy worker tải media.");
            return;
        }

        var workerCount = options.EffectiveWorkerCount;

        // Token RIÊNG cho worker: lúc host yêu cầu dừng, stoppingToken đã bị huỷ nhưng ta còn muốn tải hết
        // việc trong hàng đợi. Dùng chung token thì lượt tải đang bay bị cắt ngay.
        using var drainCts = new CancellationTokenSource();
        var workers = Enumerable.Range(0, workerCount)
            .Select(index => WorkerAsync(index, drainCts.Token))
            .ToArray();

        _logger.LogInformation(
            "[Media] {Count} worker tải media, trần hàng đợi {Capacity}, thư mục gốc {Root}.",
            workerCount, options.EffectiveQueueCapacity, options.ResolveRoot());

        try { await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { /* host yêu cầu dừng */ }

        _queue.Complete();

        var drainTimeout = TimeSpan.FromSeconds(_config.Current.Media.EffectiveShutdownDrainSeconds);
        var all = Task.WhenAll(workers);
        if (await Task.WhenAny(all, Task.Delay(drainTimeout)).ConfigureAwait(false) != all)
        {
            _logger.LogWarning("[Media] Còn việc tải chưa xong sau {Timeout}s khi dừng; huỷ phần còn lại.",
                drainTimeout.TotalSeconds);
            drainCts.Cancel();
        }

        try { await all.ConfigureAwait(false); }
        catch (OperationCanceledException) { }

        _logger.LogInformation("[Media] Dừng worker. Đã nhận {Enqueued} việc, bỏ {Dropped} việc do hàng đợi đầy.",
            _queue.EnqueuedCount, _queue.DroppedCount);
    }

    private async Task WorkerAsync(int index, CancellationToken ct)
    {
        try
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                try
                {
                    await _storage.SaveOneAsync(job, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // SaveOneAsync đã tự log mọi lỗi tải; chặn thêm ở đây để một việc lỗi ngoài dự kiến không
                    // giết cả worker và làm mất hẳn một phần năng lực tải.
                    _logger.LogWarning(ex,
                        "[Media] Worker {Index} gặp lỗi ngoài dự kiến; tiếp tục việc sau.", index);
                }
            }
        }
        catch (OperationCanceledException) { /* dừng service */ }
    }
}
