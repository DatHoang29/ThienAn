using Microsoft.Extensions.Hosting;
using Services.Vds.Configuration;
using Services.Vds.Pipeline.State;

namespace Services.Vds.Sources.Backfill;

/// <summary>
/// Author: KienLT
/// Description: Worker chạy MỌI lượt bù dữ liệu lịch sử — cả lượt theo yêu cầu của nguồn live
/// (<see cref="BackfillTrigger.Startup"/>, <see cref="BackfillTrigger.Reconnect"/>) lẫn lượt tự quét lại
/// khoảng còn dở (<see cref="BackfillTrigger.Recovery"/>).
///
/// <para><b>Đây là chỗ DUY NHẤT gọi <see cref="BackfillCoordinator"/> trên đường chạy.</b> Từ 23/09/2026
/// <c>SseEventSource</c> không tự chạy lượt bù nữa mà chỉ đặt yêu cầu qua
/// <see cref="BackfillRequestTrigger"/>. Trước đó lượt bù nằm ngay trên đường kết nối: một bản tin của camera
/// chưa khai danh mục làm lượt bù ném lỗi, lỗi thoát ra vòng kết nối và dừng cả đường live — đo được 2 ngày
/// worker không ghi được bản tin mới nào. Tách ra đây thì lỗi của việc bù quá khứ chỉ ảnh hưởng việc bù.</para>
///
/// <para>Vòng lặp chờ theo tín hiệu VÀ có hạn chờ: có yêu cầu thì chạy ngay (không phải đợi hết chu kỳ), vắng
/// yêu cầu thì vẫn thức theo nhịp để nhặt khoảng pending do đường live đánh dấu.</para>
/// Created date: 26/08/2026
/// </summary>
public sealed class LiveGapRecoveryWorker(
    BackfillCoordinator backfill,
    BackfillRequestTrigger request,
    IngestionCheckpointStore checkpoint,
    IVdsConfigManager config,
    ILogger<LiveGapRecoveryWorker> logger) : BackgroundService
{
    /// <summary>Nhịp thức dậy khi không có yêu cầu nào — giữ nguyên chu kỳ quét gap của bản cũ.</summary>
    private static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await request.WaitAsync(IdlePollInterval, stoppingToken).ConfigureAwait(false);

                // Gộp mọi yêu cầu đang chờ thành MỘT lượt: mỗi lượt bù tự tính lại cửa sổ từ checkpoint nên
                // chạy hai lượt liền nhau cho hai yêu cầu chỉ lặp lại đúng một việc.
                var requested = request.DrainRequested();
                var trigger = requested.Count > 0
                    ? requested[0]
                    : BackfillTrigger.Recovery;

                // Lượt Recovery chỉ có việc khi đường live đã đánh dấu một khoảng dở dang.
                if (trigger == BackfillTrigger.Recovery
                    && checkpoint.Current.PendingBackfillFromUtc == null) continue;
                if (!config.Current.Backfill.Enabled) continue;

                var result = await backfill.RunAsync(trigger, stoppingToken).ConfigureAwait(false);
                if (!result.WasRun)
                    logger.LogDebug("[Backfill] Bỏ qua {Trigger}: {Reason}.", trigger, result.SkipReason);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                // Lỗi ở đây KHÔNG còn chạm tới đường live. Giữ marker để lượt sau bù lại; khoảng pending vẫn
                // nằm trên đĩa nên không mất dấu vết phần chưa bù.
                logger.LogWarning(ex, "[Backfill] Lượt bù thất bại; giữ marker để thử lại.");
            }
        }
    }
}
