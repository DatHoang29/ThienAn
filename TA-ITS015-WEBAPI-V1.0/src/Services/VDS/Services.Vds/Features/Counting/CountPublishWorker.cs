using Microsoft.Extensions.Hosting;
using Services.Vds.Shared.Messaging;
using Services.Vds.Configuration;
using Services.Vds.Catalog;

namespace Services.Vds.Features.Counting;

/// <summary>
/// Author: KienLT
/// Description: Phát số đếm xe lên NATS.
///
/// Nhịp phát là kết quả của ba tham số, mỗi tham số chặn một kiểu hỏng khác nhau:
/// <list type="bullet">
/// <item><c>CountPublishDebounceMs</c> — gom các lượt xe sát nhau thành một lần phát.</item>
/// <item><c>CountMinPublishIntervalSeconds</c> — trần tần suất. Mỗi lần phát là một truy vấn gộp 24h nên
/// không thể phát theo từng lượt xe như bộ đếm RAM trước đây.</item>
/// <item><c>CountRefreshIntervalSeconds</c> — vẫn phát khi vắng xe, để cửa sổ trượt và chuỗi theo giờ tiếp
/// tục tiến (nếu không thì lúc không có xe, bên nhận thấy số đứng im ở mốc cũ).</item>
/// </list>
///
/// Lượt phát đầu sau khi khởi động luôn là <c>snapshot</c> để bên nhận có nền đầy đủ trước khi nhận
/// <c>delta</c>. Lượt định kỳ cũng là snapshot vì nó không gắn với camera nào cụ thể.
/// Created date: 26/08/2026
/// </summary>
public sealed class CountPublishWorker : BackgroundService
{
    private readonly CountPublishTrigger _trigger;
    private readonly CountPayloadBuilder _builder;
    private readonly VdsPublisher _publisher;
    private readonly CameraManager _cameras;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<CountPublishWorker> _logger;

    private DateTime _lastPublishUtc = DateTime.MinValue;
    private bool _snapshotSent;

    public CountPublishWorker(
        CountPublishTrigger trigger,
        CountPayloadBuilder builder,
        VdsPublisher publisher,
        CameraManager cameras,
        IVdsConfigManager config,
        ILogger<CountPublishWorker> logger)
    {
        _trigger = trigger;
        _builder = builder;
        _publisher = publisher;
        _cameras = cameras;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var runtime = _config.Current;
            var publish = runtime.Publish;

            if (!publish.PublishCount || !runtime.QuestEnabled)
            {
                // Không tắt hẳn vòng lặp: cả hai điều kiện đổi được lúc chạy.
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                continue;
            }

            var refreshSeconds = Math.Max(0, publish.CountRefreshIntervalSeconds);
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

            if (triggered && publish.CountPublishDebounceMs > 0)
            {
                // Chờ thêm cửa sổ gom để các lượt xe sát nhau vào cùng một lần phát.
                try
                {
                    await Task.Delay(publish.CountPublishDebounceMs, stoppingToken).ConfigureAwait(false);
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

        // Trần tần suất: thay đổi vẫn nằm trong tập _changed của lượt sau nên không mất số, chỉ hoãn.
        var minInterval = TimeSpan.FromSeconds(Math.Max(0, publish.CountMinPublishIntervalSeconds));
        var sinceLast = DateTime.UtcNow - _lastPublishUtc;
        if (_snapshotSent && sinceLast < minInterval)
        {
            foreach (var id in changed) _trigger.Notify(id);
            return;
        }

        // Chưa có danh mục camera thì payload sẽ rỗng nghĩa; chờ danh mục nạp xong.
        if (_cameras.Count == 0) return;

        // Không có gì đổi và cũng không phải lượt định kỳ đầu tiên ⇒ vẫn phát snapshot để cửa sổ trượt
        // tiến, nhưng đó là đường định kỳ chứ không phải đường dữ liệu mới.
        var sendDelta = _snapshotSent && triggered && changed.Count > 0;

        try
        {
            var nowUtc = DateTime.UtcNow;
            var payload = sendDelta
                ? await _builder.BuildDeltaAsync(nowUtc, changed, ct).ConfigureAwait(false)
                : await _builder.BuildAsync(nowUtc, ct).ConfigureAwait(false);

            var sent = await _publisher
                .SendAsync(VdsSubjects.Count, payload, ct)
                .ConfigureAwait(false);

            _lastPublishUtc = nowUtc;
            if (!sendDelta && sent) _snapshotSent = true;

            _logger.LogDebug(
                "[Count] Đã phát {Mode} ({Changed} camera đổi), sent={Sent}.",
                sendDelta ? "delta" : "snapshot", changed.Count, sent);
        }
        catch (OperationCanceledException)
        {
            // Host đang dừng: trả lại thay đổi cho lượt sau (nếu còn) rồi thoát êm.
            foreach (var id in changed) _trigger.Notify(id);
        }
        catch (Exception ex)
        {
            // Truy vấn đếm lỗi (QuestDB xuống, bảng chưa có) KHÔNG được làm chết worker: lần sau thử lại.
            // Trả lại tập camera đã đổi để lượt sau vẫn phát đúng phạm vi.
            foreach (var id in changed) _trigger.Notify(id);
            _logger.LogError(ex, "[Count] Không dựng/phát được số đếm; sẽ thử lại ở lượt sau.");

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
