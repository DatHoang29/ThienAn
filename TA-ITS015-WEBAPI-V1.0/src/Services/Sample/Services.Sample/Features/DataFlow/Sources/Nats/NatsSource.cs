using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Sample.Configuration;
using Services.Sample.Features.DataFlow.Mapping;
using Services.Shared.Logging;
using Services.Shared.Messaging;
using Wolverine;

namespace Services.Sample.Features.DataFlow.Sources.Nats;

/// <summary>
/// Author: TAC Team
/// Description: Subscribe NATS input subject (lấy từ Service:natsInputSubject) và publish ProcessDataCommand
/// qua Wolverine local queue. Kết nối + streams lo bởi TransportManager (đọc DataTransporter.json).
/// Resolve IMessageBus lazily trong scope vì Wolverine bus là Scoped (không inject trực tiếp vào Singleton).
/// Created date: 16/07/2026
/// </summary>
public sealed class NatsSource(
    IConfigManager configService,
    TransportManager transport,
    InputMapper mapper,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private string? _subscribedSubject;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var inputSubject = configService.Current.NatsInputSubject;
        if (string.IsNullOrWhiteSpace(inputSubject))
            return; // Chưa cấu hình input subject — bỏ qua.

        await transport.ConnectAsync(stoppingToken);
        if (!transport.IsConnected)
        {
            LogManager.Log(LogLevel.Warning, "Transport not connected; NatsSource skipped", kind: LogKind.Nats);
            return;
        }

        _subscribedSubject = inputSubject;
        await transport.SubscribeAsync(
            inputSubject,
            json => HandleMessageAsync(json, stoppingToken),
            stoppingToken);

        LogManager.Info($"Registered NATS input subject {inputSubject}", kind: LogKind.Nats);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            if (_subscribedSubject is not null)
                transport.Unsubscribe(_subscribedSubject);
        }
    }

    private async Task HandleMessageAsync(string json, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(mapper.FromNats(json));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            LogManager.Error(exception, $"Invalid NATS input on {_subscribedSubject}", kind: LogKind.Nats);
        }
    }
}
