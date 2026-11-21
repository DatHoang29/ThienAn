using ITS.Sync.Infrastructure.Messaging;

namespace ITS.Sync.Worker;

/// <summary>
/// Hosted service bọc <see cref="SyncHeartbeatPublisher"/> (nằm ở Infrastructure) để chạy nền trong Worker.
/// Chạy độc lập với <see cref="SyncBackgroundService"/>: tiến trình còn sống là còn phát heartbeat,
/// kể cả khi đang chờ kết nối DB.
/// </summary>
public sealed class SyncHeartbeatService(SyncHeartbeatPublisher publisher) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        publisher.Start();

        // Giữ hosted service sống cho tới khi host dừng.
        try { await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { /* host đang dừng */ }

        await publisher.StopAsync().ConfigureAwait(false);
    }
}
