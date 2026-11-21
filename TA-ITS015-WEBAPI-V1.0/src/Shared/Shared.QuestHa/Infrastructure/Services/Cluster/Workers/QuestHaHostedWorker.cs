using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.QuestHa.Infrastructure.Services.Cluster.Workers;

/// <summary>
/// Author: KienLT
/// Description: Chạy mọi replicator + đối soát tự động (nếu <see cref="ReconcilerOptions.AutoInterval"/> có giá trị).
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestHaHostedWorker : BackgroundService
{
    private readonly QuestHaClusterService _cluster;
    private readonly ILogger<QuestHaHostedWorker> _logger;

    /// <summary>Khởi tạo worker nền của cluster.</summary>
    public QuestHaHostedWorker(QuestHaClusterService cluster, ILogger<QuestHaHostedWorker> logger)
    {
        _cluster = cluster;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tasks = _cluster.Replicators.Select(r => Task.Run(() => r.RunAsync(stoppingToken), stoppingToken)).ToList();
        if (_cluster.Options.Reconciler.AutoInterval is { } every && every > TimeSpan.Zero)
            tasks.Add(Task.Run(() => ReconcileLoopAsync(every, stoppingToken), stoppingToken));
        return Task.WhenAll(tasks);
    }

    private async Task ReconcileLoopAsync(TimeSpan every, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(every, ct);
                // chỉ công bố AND toàn bộ bảng một lần mỗi chu kỳ.
                await _cluster.Reconciler.ReconcileRecentAllAsync(_cluster.Tables, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // log/timeline chỉ loại lỗi, exception đầy đủ ở Debug.
                _logger.LogSafeWarning(ex, "[QuestHa] Đối soát tự động lỗi.");
                _cluster.Timeline.Publish("Reconciler", null, QuestHaEventKindEnum.Error, $"Đối soát tự động lỗi: {QuestErrorClassifierHelper.SafeDescribe(ex)}");
            }
        }
    }
}
