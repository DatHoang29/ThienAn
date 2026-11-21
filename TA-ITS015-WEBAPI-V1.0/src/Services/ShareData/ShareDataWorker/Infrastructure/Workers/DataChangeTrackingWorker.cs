using ShareDataWorker.Infrastructure.Services.DataChangeTracking;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace ShareDataWorker.Infrastructure.Workers
{
    /// <summary>
    /// Description: Worker chạy ngầm giám sát SQL Server Change Tracking (Polling 1s/lần).
    ///              Toàn bộ logic nằm ở DataChangeTrackingService; worker chỉ giữ vòng lặp và nhịp quét.
    /// Created date: 28/09/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public class DataChangeTrackingWorker(
        DataChangeTrackingService ChangeTrackingService,
        ILogger<DataChangeTrackingWorker> logger,
        DataWorkerConfigService dataWorkerConfig) : BackgroundService
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Vòng lặp nền: gọi PollChanges theo chu kỳ đến khi dịch vụ dừng.
        /// Created date: 18/09/2026
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("🚀 Khởi động DataChangeTrackingWorker (Giám sát SQL Server Change Tracking cho {Count} bảng)...", ChangeTrackingService.TrackedTables.Count);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ChangeTrackingService.PollChanges(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "❌ Lỗi trong chu kỳ polling Change Tracking: {Message}", ex.Message);
                }

                await dataWorkerConfig.Delay(o => o.TrackingPollIntervalSec, stoppingToken);
            }
        }
    }
}
