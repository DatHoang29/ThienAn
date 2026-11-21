using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.QuestHa.Infrastructure.Services.Backup.Workers;

/// <summary>
/// Author: KienLT
/// Description: Lịch backup chỉ ở host replicator; tắt không resolve SQL/cluster.
/// Created date: 28/10/2026
/// </summary>
internal sealed class QuestBackupScheduleWorker(IServiceProvider services, IOptions<QuestHaOptions> options,
    ILogger<QuestBackupScheduleWorker> logger, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        // Enabled/WriteMode/Nodes là hạ tầng: tắt thì không chạy. Lịch (Schedule rỗng = tắt) đổi được lúc chạy nên vẫn giữ vòng chờ.
        if (!config.Backup.Enabled || config.WriteMode == QuestWriteModeEnum.Direct || config.Nodes.Count == 0) return;
        var settings = services.GetService<QuestHaSettingsService>();
        // Không có snapshot vận hành (test/host tối giản) thì lịch cố định như cấu hình khởi động: rỗng ⇒ không chạy.
        if (settings is null && string.IsNullOrWhiteSpace(config.Backup.Schedule)) return;
        string? rejected = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            var backup = (settings?.Current ?? config).Backup;
            var valid = !string.IsNullOrWhiteSpace(backup.Schedule) && ScheduleValid(config, backup, ref rejected);
            if (!valid && settings is null) return;
            if (valid)
            {
                try { await services.GetRequiredService<QuestBackupScheduleService>().RunDueAsync(clock.GetUtcNow().UtcDateTime.ToLocalTime(), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception error)
                {
                    logger.LogError("[QuestHa] Backup worker lỗi {ErrorType}.", error.GetType().Name);
                    services.GetRequiredService<QuestHaTimelineService>().Publish("BackupSchedule", null, QuestHaEventKindEnum.Error, "Lượt backup lỗi: " + error.GetType().Name);
                }
            }
            try { await Task.Delay(config.Backup.PollInterval, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }

    // Báo lỗi một lần cho mỗi lịch sai (không lặp mỗi nhịp poll); lịch hợp lệ trở lại thì chạy tiếp.
    private bool ScheduleValid(QuestHaOptions config, BackupOptions backup, ref string? rejected)
    {
        try
        {
            backup.Validate();
            for (var i = 0; i < config.Nodes.Count; i++) _ = QuestBackupScheduleRules.Next(clock.GetUtcNow().UtcDateTime.ToLocalTime(), i, backup);
            rejected = null;
            return true;
        }
        catch (ArgumentException)
        {
            var key = backup.Schedule + "|" + backup.WindowStart + "|" + backup.WindowEnd + "|" + backup.NodeOffset;
            if (rejected != key)
            {
                rejected = key;
                logger.LogError("[QuestHa] Lịch backup không hợp lệ; không chạy.");
                services.GetRequiredService<QuestHaTimelineService>().Publish("BackupSchedule", null, QuestHaEventKindEnum.Error, "Cấu hình lịch backup không hợp lệ; không chạy.");
            }
            return false;
        }
    }
}
