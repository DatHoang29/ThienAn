using ShareDataWorker.Infrastructure.Services.DataInbound;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;

namespace ShareDataWorker.Infrastructure.Workers
{
    /// <summary>
    /// Background Worker của LUỒNG NHẬN: cứ 5 giây xử lý các gói tin Pending do API nhận lưu vào CSDL Inbound.
    /// Chạy song song, độc lập với DataPublicationWorker của luồng gửi.
    /// Author: HieuNV
    /// Created date: 07/09/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public class DataInboundWorker(
        DataInboundService dataInboundService,
        ILogger<DataInboundWorker> logger,
        IConfiguration configuration,
        DataWorkerConfigService workerConfig) : BackgroundService
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Vòng lặp nền xử lý gói nhận theo chu kỳ. Chưa khai ConnectionStrings:InboundConnection thì ghi cảnh báo và không chạy.
        /// Created date: 27/08/2026
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Nhường luồng ngay để không chặn quá trình khởi động của Host/Windows Service
            await Task.Yield();

            if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("InboundConnection")))
            {
                logger.LogWarning(
                    "⚠️ Chưa cấu hình ConnectionStrings:InboundConnection — DataInboundWorker không khởi động.");
                return;
            }

            logger.LogInformation("🚀 Khởi động DataInboundWorker (luồng nhận)...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await dataInboundService.ProcessPendingPackets(stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lỗi trong quá trình xử lý luồng nhận");
                }

                await workerConfig.Delay(o => o.InboundCheckIntervalSec, stoppingToken);
            }
        }
    }
}
