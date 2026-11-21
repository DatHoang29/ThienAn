using ITS.Barrier.Core.Messaging;

namespace ITS.Barrier.Worker.Messaging;

/// <summary>
/// Hosted service bọc BarrierControlNatsListener để chạy nền trong Worker service.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed class BarrierControlHostedService : BackgroundService
{
    private readonly BarrierControlNatsListener _listener;

    public BarrierControlHostedService(BarrierControlNatsListener listener)
    {
        _listener = listener;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener.Start();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Host đang dừng
        }

        await _listener.StopAsync().ConfigureAwait(false);
    }
}
