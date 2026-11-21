using System.Threading.Channels;
using Services.Vds.Configuration;
using Services.Vds.Pipeline.Contracts;

namespace Services.Vds.Features.Media;

/// <summary>
/// Một việc tải media — đủ thông tin để dựng đường dẫn đích và tải file.
/// </summary>
/// <param name="EventType">
/// Loại bản tin ĐÃ CHUẨN HOÁ: quyết định ảnh nằm ở nhánh lượt xe hay nhánh sự cố.
/// </param>
public sealed record VdsMediaJob(
    long VendorEventId, string? CameraIp, DateTime DetectTime, string? EventType, string? Kind, string Url);

/// <summary>
/// Author: KienLT
/// Description: Hàng đợi media CÓ GIỚI HẠN giữa pipeline và bộ tải nền.
///
/// Pipeline chỉ đẩy việc vào đây (không chờ, không tải) nên tốc độ nhận bản tin không phụ thuộc tốc độ tải
/// ảnh — điều kiện bắt buộc khi thiết bị đẩy vài chục bản tin mỗi giây qua một queue tuần tự.
///
/// Đầy thì BỎ việc mới và ĐẾM số đã bỏ: dữ liệu nghiệp vụ trong DB quan trọng hơn ảnh minh hoạ.
/// Created date: 26/08/2026
/// </summary>
public sealed class VdsMediaQueue
{
    private readonly Channel<VdsMediaJob> _channel;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<VdsMediaQueue> _logger;
    private readonly int _capacity;

    private long _dropped;
    private long _enqueued;

    public VdsMediaQueue(IVdsConfigManager config, ILogger<VdsMediaQueue> logger)
    {
        _config = config;
        _logger = logger;
        _capacity = config.Current.Media.EffectiveQueueCapacity;

        _channel = Channel.CreateBounded<VdsMediaJob>(new BoundedChannelOptions(_capacity)
        {
            // Wait + CHỈ dùng TryWrite: producer không bao giờ bị chặn (TryWrite trả false ngay khi đầy) mà
            // vẫn ĐẾM ĐƯỢC số việc bị bỏ.
            //
            // Bản cũ khai DropWrite rồi đếm ở nhánh else — với DropWrite thì TryWrite LUÔN trả true nên nhánh
            // đó không bao giờ chạy: ảnh bị bỏ âm thầm và bộ đếm luôn bằng 0.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });
    }

    public ChannelReader<VdsMediaJob> Reader => _channel.Reader;

    public long DroppedCount => Interlocked.Read(ref _dropped);

    public long EnqueuedCount => Interlocked.Read(ref _enqueued);

    /// <summary>
    /// Đẩy toàn bộ media của một bản tin vào hàng đợi. KHÔNG chờ, KHÔNG ném lỗi ra pipeline.
    /// </summary>
    /// <returns>Số việc đã nhận.</returns>
    public int Enqueue(
        long vendorEventId,
        string? cameraIp,
        DateTime detectTime,
        string? eventType,
        IReadOnlyList<VdsMediaItem>? media)
    {
        var options = _config.Current.Media;
        if (!options.Enabled) return 0;
        if (media is null || media.Count == 0) return 0;

        var accepted = 0;

        foreach (var item in media)
        {
            if (string.IsNullOrWhiteSpace(item.Url)) continue;
            if (!options.AcceptsKind(item.Kind)) continue;

            var job = new VdsMediaJob(vendorEventId, cameraIp, detectTime, eventType, item.Kind, item.Url!);

            if (_channel.Writer.TryWrite(job))
            {
                accepted++;
                Interlocked.Increment(ref _enqueued);
                continue;
            }

            // Đầy (hoặc đã đóng lúc dừng service) ⇒ bỏ ảnh, giữ nhịp pipeline. Log THƯA DẦN vì lúc đầy thì
            // mỗi bản tin đều sinh một dòng và sẽ chôn vùi chính thông tin cần đọc.
            var total = Interlocked.Increment(ref _dropped);
            if (total % 100 == 1)
            {
                _logger.LogWarning(
                    "[Media] Hàng đợi đầy, đã bỏ {Total} media (trần {Capacity}). Tăng " +
                    "Service:Media:QueueCapacity hoặc WorkerCount nếu tình trạng này kéo dài.",
                    total, _capacity);
            }
        }

        return accepted;
    }

    /// <summary>Đóng hàng đợi khi dừng service — worker tải hết việc còn lại rồi mới kết thúc.</summary>
    public void Complete() => _channel.Writer.TryComplete();
}
