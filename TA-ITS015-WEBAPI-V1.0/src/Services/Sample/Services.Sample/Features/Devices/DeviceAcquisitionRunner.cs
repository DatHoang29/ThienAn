using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Services.Sample.Configuration;
using Services.Sample.Features.DataFlow.Messages;
using Services.Shared.Events;
using Services.Shared.Logging;
using Wolverine;

namespace Services.Sample.Features.Devices;

/// <summary>
/// Author: TAC Team
/// Description: Bộ điều khiển luồng thu thập thiết bị. Có thể Start/Stop theo yêu cầu (WPF)
/// hoặc được SampleServiceManager tự bật khi Worker StartAsync. Mỗi chu kỳ: acquire Device A + Device B,
/// tổng hợp, phát activity (qua MediatR) để UI hiển thị và publish ProcessDataCommand qua Wolverine
/// local queue để pipeline xử lý. Resolve IMessageBus lazily trong scope vì Wolverine bus là Scoped.
/// Created date: 17/07/2026
/// </summary>
public sealed class DeviceAcquisitionRunner(
    DeviceAggregationService aggregationService,
    ActivityPublisher events,
    IConfigManager configService,
    IServiceScopeFactory scopeFactory) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions PayloadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private bool _disposed;

    public bool IsRunning
    {
        get
        {
            lock (_sync)
                return _loop is { IsCompleted: false };
        }
    }

    public void Start()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_loop is { IsCompleted: false })
                return;

            _cts = new CancellationTokenSource();
            _loop = Task.Run(() => RunLoopAsync(_cts.Token));
        }

        LogManager.Info("Device acquisition started", kind: LogKind.Device);
        events.Publish("Start", "Bắt đầu thu thập dữ liệu thiết bị.", ActivityLevel.Success);
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        Task? loop;

        lock (_sync)
        {
            cts = _cts;
            loop = _loop;
            _cts = null;
            _loop = null;
        }

        if (cts is null || loop is null)
            return;

        cts.Cancel();
        try
        {
            await loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Dừng bình thường.
        }
        finally
        {
            cts.Dispose();
        }

        LogManager.Info("Device acquisition stopped", kind: LogKind.Device);
        events.Publish("Stop", "Đã dừng thu thập dữ liệu thiết bị.", ActivityLevel.Warning);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var devices = configService.Current.Devices;

            try
            {
                var aggregate = aggregationService.Acquire(devices.DeviceAId, devices.DeviceBId);

                foreach (var reading in aggregate.Readings)
                {
                    events.Publish(
                        "Acquire",
                        $"{reading.DeviceType}/{reading.ProtocolVersion} {reading.DeviceId}: "
                        + $"value={reading.Value}, status={reading.Status}");
                }

                events.Publish(
                    "Aggregate",
                    $"{aggregate.DeviceCount} thiết bị -> avg={aggregate.Average}, "
                    + $"min={aggregate.Min}, max={aggregate.Max}, status={aggregate.Status}");

                var payloadJson = JsonSerializer.Serialize(aggregate, PayloadOptions);
                var message = new ProcessDataCommand(
                    "devices",
                    aggregate.AggregateId,
                    aggregate.AggregatedAtUtc,
                    payloadJson);

                using (var scope = scopeFactory.CreateScope())
                {
                    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                    await bus.PublishAsync(message).ConfigureAwait(false);
                }
                events.Publish("Queue", $"Đưa aggregate {aggregate.AggregateId} vào hàng đợi xử lý.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogManager.Error(exception, kind: LogKind.Device);
                events.Publish("Acquire", $"Lỗi thu thập: {exception.Message}", ActivityLevel.Error);
            }

            await DelayAsync(devices.IntervalSeconds, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DelayAsync(int seconds, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, seconds)), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Dừng bình thường.
        }
    }
}
