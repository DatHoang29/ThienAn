using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Xử lý chụp ảnh sự cố ở nền. Mỗi yêu cầu dùng một DI scope mới để repository và
/// Wolverine message bus không phụ thuộc vòng đời của request đã cập nhật sự cố.
/// </summary>
public sealed class IncidentSnapshotWorker : BackgroundService
{
    private const int ConcurrentWorkers = 2;

    private readonly IncidentSnapshotQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IncidentSnapshotWorker> _logger;

    public IncidentSnapshotWorker(
        IncidentSnapshotQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<IncidentSnapshotWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[IncidentSnapshotWorker][START] Khởi động {WorkerCount} luồng xử lý ảnh sự cố.",
            ConcurrentWorkers);

        // KienLT - 17/10/2026: MỖI luồng chạy trong ExecutionContext riêng. Trước đây hai luồng (và các worker nền
        // khác) thừa hưởng chung một connection SqlSugarScope ⇒ "The connection is closed" khi chụp ảnh song song.
        var workers = Enumerable.Range(1, ConcurrentWorkers)
            .Select(workerId => Shared.Utility.Threading.IsolatedFlow.Run(() => ProcessQueueAsync(workerId, stoppingToken)))
            .ToList();

        return Task.WhenAll(workers);
    }

    private async Task ProcessQueueAsync(int workerId, CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var request in _queue.ReadAllAsync(stoppingToken))
            {
                var stopwatch = Stopwatch.StartNew();
                var queueDelay = DateTimeOffset.UtcNow - request.EnqueuedAt;
                _logger.LogInformation(
                    "[IncidentSnapshotWorker][DEQUEUED] Worker {WorkerId} bắt đầu chụp sự cố {IncidentId}, state {State}, chờ queue {QueueDelayMs} ms.",
                    workerId,
                    request.IncidentId,
                    request.State,
                    Math.Max(0, queueDelay.TotalMilliseconds));

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IncidentSnapshotDispatcher>();
                    await dispatcher.CaptureAsync(request.IncidentId, request.State, stoppingToken, request.AutomationImage);

                    _logger.LogInformation(
                        "[IncidentSnapshotWorker][COMPLETED] Worker {WorkerId} đã xử lý yêu cầu chụp sự cố {IncidentId}, state {State} sau {ElapsedMs} ms.",
                        workerId,
                        request.IncidentId,
                        request.State,
                        stopwatch.ElapsedMilliseconds);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[IncidentSnapshotWorker][FAILED] Worker {WorkerId} lỗi khi chụp sự cố {IncidentId}, state {State} sau {ElapsedMs} ms.",
                        workerId,
                        request.IncidentId,
                        request.State,
                        stopwatch.ElapsedMilliseconds);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Dừng bình thường cùng host.
        }
    }
}
