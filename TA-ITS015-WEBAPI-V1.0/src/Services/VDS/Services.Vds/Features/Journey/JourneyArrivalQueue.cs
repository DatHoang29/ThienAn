using System.Threading.Channels;
using Services.Vds.Configuration;

namespace Services.Vds.Features.Journey;

/// <summary>Một lượt xe CÓ BIỂN vừa ghi xong — ứng viên là camera cuối của một hay nhiều đoạn A→B.</summary>
/// <param name="EquipmentId">Camera vừa ghi nhận (ứng viên camera cuối B).</param>
/// <param name="LicensePlate">Biển THÔ đúng như bản tin (khớp cột QuestDB).</param>
/// <param name="DetectTimeUtc">Thời điểm phát hiện (UTC) — mốc kết thúc hành trình.</param>
/// <param name="VehicleSize">Cỡ xe của lượt này (dự phòng khi lượt A không có).</param>
/// <param name="EnqueuedUtc">Lúc đẩy vào hàng đợi — worker chờ tới <c>EnqueuedUtc + ArrivalDelaySeconds</c>.</param>
public sealed record JourneyArrival(string EquipmentId, string LicensePlate, DateTime DetectTimeUtc, string? VehicleSize = null,
    DateTime EnqueuedUtc = default);

/// <summary>
/// Author: KienLT
/// Description: Hàng đợi CÓ GIỚI HẠN giữa pipeline nhận bản tin và <see cref="JourneyArrivalWorker"/>.
///
/// <para><b>Bắt buộc tách hàng đợi</b> (cùng lý do với <c>IncidentAutomationQueue</c>): consumer
/// <c>vds-data</c> chạy TUẦN TỰ, ghép hành trình ngay trong luồng đó thì một truy vấn QuestDB chậm sẽ chặn
/// mọi bản tin phía sau.</para>
///
/// <para>Đầy thì BỎ và ĐẾM, không chặn producer. Tín hiệu bị bỏ không mất chuyến: lượt quét đối soát
/// (<see cref="JourneyJob"/>) sẽ ghép lại từ QuestDB.</para>
/// Created date: 29/09/2026
/// </summary>
public sealed class JourneyArrivalQueue
{
    private readonly Channel<JourneyArrival> _channel;
    private readonly ILogger<JourneyArrivalQueue> _logger;
    private readonly int _capacity;
    private long _dropped;

    public JourneyArrivalQueue(IVdsConfigManager config, ILogger<JourneyArrivalQueue> logger)
    {
        _logger = logger;
        _capacity = config.Current.Journey.EffectiveArrivalQueueCapacity;
        _channel = Channel.CreateBounded<JourneyArrival>(new BoundedChannelOptions(_capacity)
        {
            // Wait + CHỈ dùng TryWrite: producer không bao giờ bị chặn mà vẫn đếm được số bị bỏ.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public ChannelReader<JourneyArrival> Reader => _channel.Reader;

    public long DroppedCount => Interlocked.Read(ref _dropped);

    /// <summary>Đẩy một lượt xe. KHÔNG chờ, KHÔNG ném. Trả false khi bỏ (không biển / đầy / đã đóng).</summary>
    public bool Enqueue(string? equipmentId, string? licensePlate, DateTime detectTimeUtc, string? vehicleSize = null)
    {
        if (string.IsNullOrWhiteSpace(equipmentId) || string.IsNullOrWhiteSpace(licensePlate)) return false;
        if (_channel.Writer.TryWrite(new JourneyArrival(equipmentId, licensePlate, detectTimeUtc, vehicleSize, DateTime.UtcNow))) return true;

        var total = Interlocked.Increment(ref _dropped);
        if (total % 100 == 1)
            _logger.LogWarning(
                "[Journey] Hàng đợi ghép theo bản tin đầy, đã bỏ {Total} lượt (trần {Capacity}); lượt quét đối soát sẽ bù. " +
                "Kiểm tra QuestDB có chậm không, hoặc tăng Service:Journey:ArrivalQueueCapacity.", total, _capacity);
        return false;
    }

    public void Complete() => _channel.Writer.TryComplete();
}
