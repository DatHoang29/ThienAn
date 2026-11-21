using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Infrastructure.Services.Backup;

/// <summary>
/// Author: KienLT
/// Description: tuần tự trong host replicator, retry có trần từng slot.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestBackupScheduleService(IJournalStore store, QuestBackupService backup, QuestHaOptions config,
    QuestHaTimelineService timeline, ILogger<QuestBackupScheduleService>? logger = null, QuestHaSettingsService? settings = null)
{
    private readonly ILogger _logger = logger ?? (ILogger)NullLogger.Instance;
    private readonly Dictionary<string, (int Attempts, DateTime Next, bool Done)> _attempts = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Thử các slot đến hạn; peer chưa Ready chỉ chờ, không tiêu hao quota thử backup.</summary>
    public async Task RunDueAsync(DateTime nowLocal, CancellationToken ct)
    {
        // Lịch, cửa sổ, số lần thử, DryRun chụp một lần mỗi tick từ snapshot vận hành; Enabled là hạ tầng (khởi động).
        var options = (settings?.Current ?? config).Backup;
        if (!config.Backup.Enabled || config.WriteMode == QuestWriteModeEnum.Direct || config.Nodes.Count == 0 || string.IsNullOrWhiteSpace(options.Schedule)) return;
        options.Validate();
        if (!QuestBackupScheduleRules.InWindow(nowLocal, options) || backup.IsBusy || !await _gate.WaitAsync(0, ct)) return;
        try
        {
            var records = await store.ListBackupsAsync(ct);
            // bản gọi tay không được làm lịch nghĩ rằng daily/weekly của slot đã xong.
            var manual = records.ToDictionary(b => b.BackupId, QuestBackupStorageService.IsManual);
            var nodes = config.Nodes.Select(n => n.Name).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var old in _attempts.Where(kv => kv.Value.Next.Date < nowLocal.Date.AddDays(-1)).Select(kv => kv.Key).ToArray()) _attempts.Remove(old);
            for (var i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (QuestBackupScheduleRules.Due(nowLocal, i, options) is not { } due ||
                    records.Any(b => b.Node.Equals(node, StringComparison.OrdinalIgnoreCase) && b.Verified && !manual[b.BackupId] &&
                        b.CreatedUtc >= due.ToUniversalTime() && Directory.Exists(b.Path))) continue;
                var key = node + ":" + due.ToString("yyyyMMddHHmm");
                var state = _attempts.GetValueOrDefault(key);
                if (state.Done || state.Attempts >= options.MaxAttempts || nowLocal < state.Next) continue;
                try
                {
                    if (backup.IsBusy) continue;
                    // peer lag/recovery là điều kiện chờ bình thường, không phải lần backup thất bại.
                    try { await backup.EnsurePeerReadyAsync(node, ct); }
                    catch (InvalidOperationException)
                    {
                        _attempts[key] = (state.Attempts, nowLocal + options.RetryDelay, false);
                        continue;
                    }
                    _attempts[key] = (state.Attempts + 1, nowLocal + options.RetryDelay, false);
                    if (options.DryRun)
                        timeline.Publish("BackupSchedule", node, QuestHaEventKindEnum.BackupStarted, $"Dry-run backup slot {due:O}; không dừng node, không chép/xoá file.");
                    else
                        await backup.BackupColdAsync(node, ct, requirePeerReady: true, weekly: due.DayOfWeek == options.WeeklyDay, enforceWindow: true, manual: false);
                    _attempts[key] = (state.Attempts + 1, nowLocal, true);
                    await backup.RetainAsync(options.DryRun, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (InvalidOperationException error) when (error.Data.Contains("QuestHa.Backup.WaitForPeer") && error.Data["QuestHa.Backup.StopAttempted"] is not true)
                {
                    // kể cả peer đổi trạng thái ở probe cuối cũng không trừ lượt.
                    _attempts[key] = (state.Attempts, nowLocal + options.RetryDelay, false);
                }
                catch (Exception error)
                {
                    var attempts = state.Attempts + (error.Data["QuestHa.Backup.StopAttempted"] is true ? 1 : 0);
                    _attempts[key] = (attempts, nowLocal + options.RetryDelay, false);
                    _logger.LogError("[QuestHa] Backup lịch {Node} lỗi {ErrorType}, lần {Attempt}/{Max}.", node, error.GetType().Name, attempts, options.MaxAttempts);
                    timeline.Publish("BackupSchedule", node, QuestHaEventKindEnum.Error, $"Backup lịch lỗi {error.GetType().Name}, lần {attempts}/{options.MaxAttempts}.");
                }
            }
        }
        finally { _gate.Release(); }
    }
}
