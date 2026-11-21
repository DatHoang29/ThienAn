using ITS.Toll.Core.Messaging;
using Microsoft.Extensions.Hosting;

namespace ITS.Toll.Worker.Messaging;

/// <summary>
/// Hosted service bọc <see cref="TollControlNatsListener"/> (nằm ở Core) để chạy nền trong Worker.
/// Toàn bộ logic kết nối/lắng nghe/xử lý nằm ở Core nên dùng chung được với ITS.Toll.Wpf.
/// Author: ChuongLP
/// Created date: 14/09/2026
/// </summary>
public sealed class TollControlListener : BackgroundService
{
    private readonly TollControlNatsListener _listener;

    public TollControlListener(TollControlNatsListener listener)
    {
        _listener = listener;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener.Start();

        // Giữ hosted service sống cho tới khi host dừng.
        try { await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { /* host đang dừng */ }

        await _listener.StopAsync().ConfigureAwait(false);
    }
}
