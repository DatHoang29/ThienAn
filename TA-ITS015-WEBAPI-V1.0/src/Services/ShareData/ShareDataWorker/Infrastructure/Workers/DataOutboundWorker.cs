using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace ShareDataWorker.Infrastructure.Workers
{
    /// <summary>
    /// Background Worker xử lý trích xuất & xuất bản dữ liệu định kỳ (Periodic)
    /// Author: Đạt
    /// Created date: 14/08/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public class DataOutboundWorker(
        DataOutboundService DataOutboundService,
        ILogger<DataOutboundWorker> logger,
        DataWorkerConfigService dataWorkerConfig) : BackgroundService
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Vòng lặp nền: gọi ProcessSubscriptions theo chu kỳ đến khi dịch vụ dừng.
        /// Created date: 14/08/2026
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("🚀 Khởi động DataOutboundWorker (Định kỳ)...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DataOutboundService.ProcessSubscriptions(stoppingToken);
                }
                catch (ShareDataException ex)
                {
                    logger.LogError(ex, "❌ [{AlertCode}] Lỗi kết xuất dừng batch: {Message}", ex.AlertCode, ex.Message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Lỗi không xác định dừng batch Periodic Export: {Message}", ex.Message);
                }

                await dataWorkerConfig.Delay(o => o.OutboundCheckIntervalSec, stoppingToken);
            }
        }
    }
}
