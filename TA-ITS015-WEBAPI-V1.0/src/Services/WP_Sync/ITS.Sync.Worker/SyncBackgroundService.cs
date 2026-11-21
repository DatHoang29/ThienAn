using ITS.Sync.Core.Abstractions;
using ITS.Sync.Core.Models;
using ITS.Sync.Infrastructure.Messaging;

namespace ITS.Sync.Worker;

/// <summary>
/// Host chạy nền (Windows Service): dùng CHUNG <see cref="ISyncController"/> với WPF,
/// KHÔNG viết lại logic đồng bộ. Chạy định kỳ theo cấu hình "Sync:IntervalSeconds".
/// </summary>
public sealed class SyncBackgroundService(
    ISyncController controller,
    NatsSyncListener natsListener,
    IConfiguration configuration,
    ILogger<SyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue<int?>("Sync:IntervalSeconds") ?? 20;
        if (intervalSeconds < 1) intervalSeconds = 1;
        var interval = TimeSpan.FromSeconds(intervalSeconds);

        // Init: kiểm tra kết nối 2 DB. Nếu lỗi thì thử lại mỗi chu kỳ cho tới khi OK (hoặc bị dừng).
        while (!stoppingToken.IsCancellationRequested)
        {
            var init = await controller.InitAsync(stoppingToken);
            if (init.Ok)
            {
                logger.LogInformation("Init OK - kết nối nguồn & đích bình thường.");
                break;
            }

            logger.LogError("Init THẤT BẠI - Nguồn: {Source} | Đích: {Target}. Thử lại sau {Seconds}s.",
                init.SourceOk ? "OK" : init.SourceError,
                init.TargetOk ? "OK" : init.TargetError,
                intervalSeconds);

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }

        if (stoppingToken.IsCancellationRequested) return;

        // Báo cáo/lỗi qua IProgress giống WPF, nhưng ở đây chỉ ghi log.
        var onCycle = new Progress<SyncRunReport>(report =>
            logger.LogInformation(
                "Chu kỳ xong: +{Inserted} ~{Updated} -{Deleted}{Failed}",
                report.TotalInserted, report.TotalUpdated, report.TotalDeleted,
                report.AllSucceeded ? "" : " (có bảng LỖI)"));

        var onError = new Progress<Exception>(ex =>
            logger.LogError(ex, "Lỗi khi đồng bộ: {Message}", ex.Message));

        // Lắng nghe tín hiệu NATS để đồng bộ MỘT bảng lẻ (song song với vòng lặp định kỳ).
        // Các lần chạy được tuần tự hóa trong SyncController nên không ghi chồng lấn.
        var onNatsStatus = new Progress<string>(s => logger.LogInformation("NATS: {Status}", s));
        natsListener.Start(onCycle, onError, onNatsStatus);

        logger.LogInformation("SyncBackgroundService START - đồng bộ định kỳ mỗi {Seconds} giây.", intervalSeconds);

        // Vòng lặp dùng cùng đường điều khiển RunOnceAsync với WPF (tôn trọng stoppingToken).
        while (!stoppingToken.IsCancellationRequested)
        {
            await controller.RunOnceAsync(onCycle, onError, stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        natsListener.Stop();
        logger.LogInformation("SyncBackgroundService STOP - đã dừng.");
    }
}
