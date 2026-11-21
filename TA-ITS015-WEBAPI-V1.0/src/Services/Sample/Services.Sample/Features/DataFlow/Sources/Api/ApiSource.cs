using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services.Sample.Configuration;
using Services.Sample.Features.DataFlow.Mapping;
using Services.Shared.Logging;
using Wolverine;

namespace Services.Sample.Features.DataFlow.Sources.Api;

/// <summary>
/// Author: TAC Team
/// Description: Poll dữ liệu từ HTTP API và publish ProcessDataCommand qua Wolverine local queue.
/// Resolve IMessageBus lazily trong scope vì Wolverine bus là Scoped (không inject trực tiếp vào Singleton).
/// Created date: 16/07/2026
/// </summary>
public sealed class ApiSource(
    IHttpClientFactory httpClientFactory,
    IConfigManager configService,
    InputMapper mapper,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = configService.Current.Api;
            if (!options.Enabled)
            {
                await DelayAsync(5, stoppingToken);
                continue;
            }

            try
            {
                using var timeout = CancellationTokenSource
                    .CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

                var client = httpClientFactory.CreateClient("InputApi");
                var json = await client.GetStringAsync(options.Url, timeout.Token);
                using var scope = scopeFactory.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                await bus.PublishAsync(mapper.FromApi(json));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogManager.Error(exception, $"API polling failed for {options.Url}", kind: LogKind.Data);
            }

            await DelayAsync(options.IntervalSeconds, stoppingToken);
        }
    }

    private static async Task DelayAsync(int seconds, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, seconds)), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }
}
