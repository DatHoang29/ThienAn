using Services.Vds.Shared.Messaging;

namespace Services.Vds.Sources.Backfill;

/// <summary>
/// Author: KienLT
/// Description: Cầu nối giữa nguồn live và worker bù dữ liệu: nguồn báo "cần bù một lượt", worker tự quyết
/// định khi nào chạy.
///
/// <para><b>Vì sao tách ra (23/09/2026).</b> Trước đây <c>SseEventSource</c> gọi thẳng
/// <c>BackfillCoordinator.RunAsync</c> ngay trên đường kết nối, TRƯỚC khi đọc dòng live đầu tiên. Một lỗi
/// trong lượt bù vì vậy thoát ra tới vòng kết nối và dừng luôn đường live: đo trên máy thật, một bản tin của
/// camera chưa khai danh mục giữ worker đứng 2 ngày mà không ghi được bản tin mới nào. Live là dữ liệu đang
/// xảy ra — nó không được phép chờ việc bù quá khứ.</para>
///
/// <para>Dùng lại <see cref="PublishSignal"/> thay vì tự giữ semaphore: đó chính là nguyên thuỷ "có việc thì
/// đánh thức" của repo, và nhân bản thêm một bản nữa là chỗ dễ lệch nhau nhất.</para>
///
/// <para>Khoá của tín hiệu là TÊN TRIGGER, nên nhiều lượt yêu cầu cùng loại trong một cửa sổ chỉ tạo một
/// lượt bù — đúng điều ta muốn, vì mỗi lượt bù đã tự tính lại cửa sổ từ checkpoint.</para>
/// Created date: 23/09/2026
/// </summary>
public sealed class BackfillRequestTrigger : IDisposable
{
    private readonly PublishSignal _signal = new();
    private readonly ILogger<BackfillRequestTrigger> _logger;

    public BackfillRequestTrigger(ILogger<BackfillRequestTrigger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Đặt yêu cầu bù một lượt. Gọi từ đường kết nối live nên phải rẻ và KHÔNG ném.
    /// </summary>
    public void Request(BackfillTrigger trigger)
    {
        _signal.Mark(trigger.ToString());
        _logger.LogInformation(
            "[Backfill] Đã đặt yêu cầu bù ({Trigger}); đường live không chờ lượt bù này.", trigger);
    }

    /// <inheritdoc cref="PublishSignal.WaitAsync"/>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) => _signal.WaitAsync(timeout, ct);

    /// <summary>
    /// Lấy và XOÁ các yêu cầu đang chờ. Trigger không đọc được tên thì bỏ qua — tên là do chính
    /// <see cref="Request"/> ghi nên chỉ lệch được khi có ai đánh dấu bằng chuỗi tự do.
    /// </summary>
    public List<BackfillTrigger> DrainRequested()
    {
        var result = new List<BackfillTrigger>();
        foreach (var key in _signal.Drain())
        {
            if (Enum.TryParse<BackfillTrigger>(key, out var trigger)) result.Add(trigger);
        }

        return result;
    }

    public void Dispose() => _signal.Dispose();
}
