using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.QuestHa.Infrastructure.Services.Journal.Maintenance.Workers;

/// <summary>
/// Author: KienLT
/// Description: Lịch chỉ đăng ký cùng replicator; Direct/tắt/không node không khởi tạo kết nối SQL.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestJournalMaintenanceWorker(IServiceProvider services, IOptions<QuestHaOptions> options,
    ILogger<QuestJournalMaintenanceWorker> logger, TimeProvider? clock = null) : BackgroundService
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private DateTime? _lastBlockedUtc;
    private string? _lastBlockedReason;

    /// <summary>Đo backlog và dọn định kỳ; lỗi một lượt không làm dừng replicator/pipeline append.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        var maintenance = config.JournalMaintenance;
        if (!maintenance.Enabled || config.WriteMode == QuestWriteModeEnum.Direct || config.Nodes.Count == 0) return;
        try { maintenance.Validate(); }
        catch (ArgumentException)
        {
            logger.LogError("[QuestHa] JournalMaintenance không hợp lệ; lịch bảo trì không chạy.");
            return;
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            // Chụp chính sách một lần mỗi vòng (DryRun, giới hạn, throttle, chu kỳ chờ); đổi lúc chạy có hiệu lực từ vòng sau.
            var current = maintenance;
            try
            {
                var service = services.GetRequiredService<QuestJournalMaintenanceService>();
                current = service.Maintenance;
                // Lỗi đo không ngăn dọn an toàn; purge tự kiểm lại mọi điều kiện trong SQL.
                try { await service.GetBacklogAsync(stoppingToken).ConfigureAwait(false); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                { logger.LogWarning("[QuestHa] Đo backlog lỗi: {ErrorType}", ex.GetType().Name); }
                var result = await service.PurgeAsync(current.DryRun, current, stoppingToken).ConfigureAwait(false);
                ReportPurge(result, current.AlertThrottle);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex)
            { logger.LogWarning("[QuestHa] Bảo trì journal lỗi: {ErrorType}", ex.GetType().Name); }
            try { await Task.Delay(current.Interval, stoppingToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    /// <summary>Lý do dừng/chặn là Warning, lặp cùng lý do giãn theo AlertThrottle.</summary>
    private void ReportPurge(JournalPurgeOutput result, TimeSpan alertThrottle)
    {
        const string message = "[QuestHa] Purge dryRun={DryRun}, rows={Rows}, maxSeq={MaxSeq}, cutoff={Cutoff}, reason={Reason}";
        if (result.Reason is null)
        {
            _lastBlockedReason = null;
            logger.LogInformation(message, result.DryRun, result.Rows, result.Boundary?.MaxSeq,
                result.Boundary?.RetentionBeforeUtc, result.Reason);
            return;
        }
        var now = _clock.GetUtcNow().UtcDateTime;
        if (_lastBlockedReason == result.Reason && _lastBlockedUtc is { } last && now - last < alertThrottle)
            return;
        _lastBlockedReason = result.Reason;
        _lastBlockedUtc = now;
        logger.LogWarning(message, result.DryRun, result.Rows, result.Boundary?.MaxSeq,
            result.Boundary?.RetentionBeforeUtc, result.Reason);
    }
}
