using ITS.VMS.Core.Messaging;

namespace ITS.VMS.Worker.Messaging;

/// <summary>
/// Hosted service bọc <see cref="VmsControlNatsListener"/> (nằm ở Core) để chạy nền trong Worker.
/// Toàn bộ logic kết nối/lắng nghe/thực thi nằm ở Core nên sau này bọc lại bằng form WPF vẫn dùng được.
/// Author: ChuongLP
/// Created date: 22/09/2026
/// </summary>
public sealed class VmsControlListener : BackgroundService
{
    private readonly VmsControlNatsListener _listener;

    public VmsControlListener(VmsControlNatsListener listener)
    {
        _listener = listener;
    }

    /// <summary>
    /// Hàm xử lý Hosted service, gọi <see cref="VmsControlNatsListener.Start"/> để bắt đầu lắng nghe kênh NATS.
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener.Start(); // Bắt đầu lắng nghe kênh NATS, chạy nền trong Core.

        // Giữ hosted service sống cho tới khi host dừng.
        try { await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { /* host đang dừng */ }

        await _listener.StopAsync().ConfigureAwait(false);  // Dừng lắng nghe kênh NATS khi host dừng.
    }
}
