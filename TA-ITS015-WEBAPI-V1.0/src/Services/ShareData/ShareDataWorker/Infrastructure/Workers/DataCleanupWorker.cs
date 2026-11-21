using ShareDataWorker.Infrastructure.Services.DataCleanup;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace ShareDataWorker.Infrastructure.Workers
{
    /// <summary>
    /// Description: Worker chạy ngầm dọn dẹp định kỳ dữ liệu lưu trữ (nhật ký Change Tracking ESH-16xx).
    /// Author: DatHQ
    /// Created date: 18/10/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public class DataCleanupWorker(
        DataCleanupService cleanupService,
        ILogger<DataCleanupWorker> logger,
        DataWorkerConfigService workerConfig) : BackgroundService
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Vòng lặp nền: gọi CleanTrackingLogs theo chu kỳ đến khi dịch vụ dừng.
        /// Created date: 18/09/2026
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("🚀 Khởi động DataCleanupWorker (Dọn dẹp nhật ký Change Tracking định kỳ)...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await cleanupService.CleanTrackingLogs(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "❌ Lỗi khi dọn dẹp nhật ký hạ tầng Change Tracking: {Message}", ex.Message);
                }

                await workerConfig.Delay(o => o.CleanupIntervalMin * 60, stoppingToken);
            }
        }
    }
}
