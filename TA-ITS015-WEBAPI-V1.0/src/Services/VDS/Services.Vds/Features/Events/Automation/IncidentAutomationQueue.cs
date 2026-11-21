using System.Threading.Channels;
using Services.Vds.Configuration;

namespace Services.Vds.Features.Events.Automation;

/// <summary>Một việc: nhờ TMS lập lịch kịch bản tự động cho sự cố vừa ghi.</summary>
/// <param name="IncidentId"><c>TmsIncident.ID</c>.</param>
/// <param name="Code">Mã sự cố — chỉ để ghi log cho dễ lần.</param>
/// <param name="Attempt">Số lần đã thử (0 = lần đầu).</param>
public sealed record IncidentAutomationJob(string IncidentId, string? Code, int Attempt = 0);

/// <summary>
/// Author: KienLT
/// Description: Hàng đợi CÓ GIỚI HẠN giữa pipeline và worker gọi API TMS.
///
/// <b>Bắt buộc phải tách hàng đợi.</b> Consumer của local queue dữ liệu chạy TUẦN TỰ, nên nếu
/// <c>IncidentPromoter</c> gọi HTTP ngay trong luồng xử lý thì một lượt API chậm hoặc timeout sẽ chặn toàn
/// bộ bản tin phía sau — tốc độ ghi dữ liệu giao thông hoá ra phụ thuộc vào độ khoẻ mạng của WebAPI.
///
/// Đầy thì BỎ việc mới và ĐẾM số đã bỏ, không chặn producer: sự cố đã nằm trong DB nên người trực vẫn xử
/// lý thủ công được, còn mất nhịp pipeline là mất dữ liệu đo đếm không lấy lại được.
/// Created date: 26/08/2026
/// </summary>
public sealed class IncidentAutomationQueue
{
    private readonly Channel<IncidentAutomationJob> _channel;
    private readonly ILogger<IncidentAutomationQueue> _logger;
    private readonly int _capacity;

    private long _dropped;
    private long _enqueued;

    public IncidentAutomationQueue(IVdsConfigManager config, ILogger<IncidentAutomationQueue> logger)
    {
        _logger = logger;
        _capacity = config.Current.TmsApi.EffectiveQueueCapacity;

        _channel = Channel.CreateBounded<IncidentAutomationJob>(new BoundedChannelOptions(_capacity)
        {
            // Wait + CHỈ dùng TryWrite: producer vẫn không bao giờ bị chặn (TryWrite trả false ngay khi
            // đầy) mà vẫn ĐẾM ĐƯỢC số việc bị bỏ. Nếu khai DropWrite thì TryWrite luôn trả true và việc bị
            // bỏ âm thầm — không còn cách nào biết đã mất bao nhiêu lần lập lịch.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public ChannelReader<IncidentAutomationJob> Reader => _channel.Reader;

    public long DroppedCount => Interlocked.Read(ref _dropped);

    public long EnqueuedCount => Interlocked.Read(ref _enqueued);

    /// <summary>
    /// Đẩy một sự cố vào hàng đợi. KHÔNG chờ, KHÔNG ném lỗi ra pipeline. Trả false khi bỏ (đầy/đã đóng).
    /// </summary>
    public bool Enqueue(string? incidentId, string? code, int attempt = 0)
    {
        if (string.IsNullOrWhiteSpace(incidentId)) return false;

        if (_channel.Writer.TryWrite(new IncidentAutomationJob(incidentId.Trim(), code, attempt)))
        {
            Interlocked.Increment(ref _enqueued);
            return true;
        }

        var total = Interlocked.Increment(ref _dropped);

        // Log THƯA DẦN: hàng đợi đầy nghĩa là WebAPI đang không phản hồi, mà lúc đó mỗi sự cố lại thêm một
        // dòng log — ghi hết sẽ chôn vùi chính thông tin cần đọc.
        if (total % 50 == 1)
        {
            _logger.LogWarning(
                "[TmsApi] Hàng đợi lập lịch tự động đầy, đã bỏ {Total} sự cố (trần {Capacity}). Kiểm tra " +
                "WebAPI có phản hồi không, hoặc tăng Service:TmsApi:QueueCapacity.",
                total, _capacity);
        }

        return false;
    }

    /// <summary>Đóng hàng đợi khi dừng service — worker xử lý hết việc còn lại rồi mới kết thúc.</summary>
    public void Complete() => _channel.Writer.TryComplete();
}
