using ITS.VMS.Core.Messaging;
using Microsoft.Extensions.Hosting;

namespace ITS.VMS.Worker.Messaging;

/// <summary>
/// Hosted service bọc <see cref="VmsHeartbeatPublisher"/> (nằm ở Core) để chạy nền trong Worker.
/// Toàn bộ logic kết nối/phát heartbeat nằm ở Core nên dùng chung được với ITS.VMS.Wpf.
/// Author: TAC Team
/// Created date: 28/09/2026
/// </summary>
public sealed class VmsHeartbeatService : BackgroundService
{
    private readonly VmsHeartbeatPublisher _publisher;

    public VmsHeartbeatService(VmsHeartbeatPublisher publisher)
    {
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _publisher.Start();

        // Giữ hosted service sống cho tới khi host dừng.
        try { await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { /* host đang dừng */ }

        await _publisher.StopAsync().ConfigureAwait(false);
    }
}
