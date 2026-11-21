using Microsoft.Extensions.Hosting;
using Services.Shared.Logging;

namespace Services.Sample.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Reload các runtime setting an toàn theo chu kỳ cấu hình.
/// Created date: 16/07/2026
/// </summary>
public sealed class ConfigReloadWorker(IConfigManager configService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var seconds = configService.Current.ConfigReloadSeconds;
            if (seconds <= 0)
                return;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
                await configService.ReloadAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogManager.Error(exception, kind: LogKind.Config);
            }
        }
    }
}
