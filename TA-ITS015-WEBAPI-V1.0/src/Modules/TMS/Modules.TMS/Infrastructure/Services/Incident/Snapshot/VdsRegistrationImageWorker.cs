using Modules.VDS.Core.Services.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Author: KienLT
/// Description: Bounded file checks recover delayed downloads and service/API restarts without camera requests.
/// Created date: 02/10/2026
/// </summary>
public sealed class VdsRegistrationImageWorker(VdsRegistrationImageStore store,
    IServiceScopeFactory scopeFactory, ILogger<VdsRegistrationImageWorker> logger) : BackgroundService
{
    // KienLT - 17/10/2026: chạy vòng quét trong ExecutionContext riêng để không dùng chung connection SqlSugarScope
    // với các worker nền khác (xem IsolatedFlow).
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Shared.Utility.Threading.IsolatedFlow.Run(() => ScanLoopAsync(stoppingToken));

    private async Task ScanLoopAsync(CancellationToken stoppingToken)
    {
        string? after = null;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(store.Options.EffectivePollSeconds));
        try
        {
            do
            {
                try
                {
                    foreach (var path in await store.PendingBatchAsync(after, stoppingToken))
                    {
                        after = path;
                        try
                        {
                            var request = await store.ReadPendingFileAsync(path, stoppingToken);
                            if (request is null) continue;
                            using var scope = scopeFactory.CreateScope();
                            await scope.ServiceProvider.GetRequiredService<TmsVdsRegistrationImageService>().ProcessAsync(request, stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                        catch (Exception ex) { logger.LogError(ex, "[VdsRegistrationImage] Pending image processing failed; retry on next scan."); }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { logger.LogError(ex, "[VdsRegistrationImage] Cannot scan registration image state directory."); }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
