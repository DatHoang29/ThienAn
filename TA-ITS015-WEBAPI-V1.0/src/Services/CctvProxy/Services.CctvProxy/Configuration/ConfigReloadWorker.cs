using Microsoft.Extensions.Hosting;
using Services.Shared.Logging;

namespace Services.CctvProxy.Configuration;

/// <summary>
/// Author: kien.le
/// Description: Reload cấu hình theo chu kỳ ConfigReloadSeconds (đổi tài khoản/địa chỉ NxVMS trên màn hình
/// "Cấu hình hoạt động" không cần restart service). Lần nạp đầu do CctvProxyServiceManager.InitAsync thực hiện.
/// Created date: 06/10/2026
/// </summary>
public sealed class ConfigReloadWorker(IConfigManager configService) : BackgroundService
{
    /// <inheritdoc/>
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
