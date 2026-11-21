using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.QuestHa.Infrastructure.Services.Export.Workers;

/// <summary>
/// Author: KienLT
/// Description: Nhịp lịch xuất Parquet trong host Writer (cùng chỗ backup lạnh/bảo trì). Export:Enabled = false (mặc định) hoặc
/// WriteMode=Direct ⇒ thoát ngay, không resolve SQL/cluster.
/// Created date: 02/11/2026
/// </summary>
internal sealed class QuestExportScheduleWorker(IServiceProvider services, IOptions<QuestHaOptions> options,
    ILogger<QuestExportScheduleWorker> logger, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = options.Value;
        if (!config.Export.Enabled || config.WriteMode == QuestWriteModeEnum.Direct || config.Nodes.Count == 0) return;
        var schedule = services.GetRequiredService<QuestExportScheduleService>();
        logger.LogInformation("[QuestHa.Export] Lịch xuất Parquet bật, ExportRoot={Root}.", config.Export.ExportRoot);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = clock.GetUtcNow();
                await schedule.RunDueAsync(TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Local).DateTime, now.UtcDateTime, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception error)
            {
                logger.LogError("[QuestHa.Export] Worker lịch xuất lỗi {ErrorType}.", error.GetType().Name);
            }
            try { await Task.Delay(config.Export.PollInterval, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        }
    }
}
