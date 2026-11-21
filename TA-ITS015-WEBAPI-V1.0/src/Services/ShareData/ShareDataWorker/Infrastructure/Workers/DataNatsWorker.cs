using ShareDataWorker.Infrastructure.Services.DataNats;

namespace ShareDataWorker.Infrastructure.Workers
{
    /// <summary>
    /// Description: Worker chạy ngầm lắng nghe tín hiệu NATS trigger khi có dữ liệu mới.
    ///              Toàn bộ logic nằm ở DataNatsService; worker chỉ giữ vòng đời đăng ký.
    /// Created date: 28/09/2026
    /// Modified date: 19/10/2026
    /// </summary>
    public class DataNatsWorker(
        DataNatsService NatsService,
        ILogger<DataNatsWorker> logger) : BackgroundService
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Đăng ký lắng nghe NATS rồi giữ tiến trình sống đến khi dịch vụ dừng.
        /// Created date: 18/09/2026
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("🚀 Khởi động DataNatsWorker (Lắng nghe sự kiện xuất dữ liệu NATS)...");

            try
            {
                await NatsService.InitSubscribe(stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }
    }
}
