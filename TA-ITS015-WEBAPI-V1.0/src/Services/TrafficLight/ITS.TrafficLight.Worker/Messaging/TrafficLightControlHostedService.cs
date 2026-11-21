using ITS.TrafficLight.Core.Messaging;

namespace ITS.TrafficLight.Worker.Messaging;

/// <summary>
/// Hosted service bọc TrafficLightControlNatsListener để chạy nền trong Worker service.
/// Author: ChuongLP
/// Date: 05/10/2026
/// </summary>
public sealed class TrafficLightControlHostedService : BackgroundService
{
    private readonly TrafficLightControlNatsListener _listener;

    public TrafficLightControlHostedService(TrafficLightControlNatsListener listener)
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
