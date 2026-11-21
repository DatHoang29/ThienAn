using Services.Vds.Shared.Domain;
using Services.Vds.Shared.Messaging;
using Services.Vds.Shared.Parsing;
using Services.Vds.Configuration;
using Services.Vds.Pipeline.Contracts;
using Services.Vds.Catalog;

namespace Services.Vds.Features.Statistic;

/// <summary>
/// Author: KienLT
/// Description: Bản tin thống kê chu kỳ (<c>type = Statistic</c>) → bảng <c>VdsTrafficStatistic</c>.
///
/// <b>Gộp mọi làn về MỘT dòng</b> (<c>lane</c> rỗng): lưu lượng cộng dồn, tốc độ trung bình có TRỌNG SỐ
/// theo số xe, các chỉ số còn lại lấy trung bình. Bảng có <c>lane</c> trong khoá DEDUP nên tách làn về
/// sau không phải đổi lược đồ — nhưng tách hay không là quyết định riêng, không trộn vào phase này.
///
/// <b>Mốc thời gian lấy từ UTC của sự kiện, KHÔNG lấy từ <c>start_time</c>/<c>end_time</c></b>: hai
/// trường đó là giờ treo trên tường của thiết bị, không kèm timezone. Ghi chúng vào cột UTC sẽ làm bảng
/// thống kê lệch múi giờ so với bảng dữ liệu thô. Hai trường đó chỉ dùng để suy ĐỘ DÀI chu kỳ khi thiết
/// bị không khai <c>periodMinutes</c>.
/// Created date: 26/08/2026
/// </summary>
public sealed class StatisticHandler
{
    private readonly QuestTrafficStatisticRepository _repository;
    private readonly VdsPublisher _publisher;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<StatisticHandler> _logger;

    public StatisticHandler(
        QuestTrafficStatisticRepository repository,
        VdsPublisher publisher,
        IVdsConfigManager config,
        ILogger<StatisticHandler> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Ghi một bản tin thống kê rồi publish. Lỗi ghi KHÔNG bị bắt ở đây: Wolverine phải thấy để retry,
    /// và DEDUP làm lần ghi lại an toàn.
    /// </summary>
    /// <param name="isBackfill">
    /// Frame bù lịch sử. VẪN GHI (bù dữ liệu là mục đích của nó) nhưng KHÔNG publish: bản tin thống kê là
    /// số liệu của MỘT chu kỳ đã qua, phát lại thì bên nhận tưởng thiết bị vừa gửi chu kỳ mới và bảng điều
    /// khiển nhảy về số của vài giờ trước.
    /// </param>
    public async Task<VdsTrafficStatistic> HandleAsync(
        VdsSseEnvelope envelope,
        VdsStatisticMetadata? metadata,
        CameraInfo camera,
        string? zoneId,
        DateTime occurredUtc,
        bool isBackfill = false,
        CancellationToken ct = default)
    {
        var lanes = metadata?.DataList ?? new List<VdsStatisticLaneItem>();
        var periodMinutes = ResolvePeriodMinutes(metadata);

        // Bản tin thống kê được phát ở CUỐI chu kỳ ⇒ occurredUtc là mốc kết thúc. fromTime là designated
        // timestamp và nằm trong khoá DEDUP nên phải tất định: suy từ occurredUtc trừ độ dài chu kỳ.
        var toUtc = occurredUtc;
        var fromUtc = periodMinutes is short minutes && minutes > 0
            ? toUtc.AddMinutes(-minutes)
            : toUtc;

        var entity = new VdsTrafficStatistic
        {
            FromTime = fromUtc,
            ToTime = toUtc,
            PeriodMinutes = periodMinutes,
            VendorEventId = envelope.Id,
            CameraIp = camera.CameraIp,
            EquipmentId = camera.EquipmentId,
            Direction = camera.Direction,
            ZoneId = zoneId,

            // Rỗng = dòng này gộp mọi làn. Không dùng null: lane nằm trong khoá DEDUP, để null thì hai
            // lần ghi cùng bản tin có thể không khớp khoá và sinh dòng thứ hai.
            Lane = string.Empty,

            TotalVehicle = SumOrNull(lanes, lane => VdsParse.Int(lane.VehicleNum)),

            // Không cấp ⇒ NULL thay vì 0, để phân biệt "không có số liệu" với "không có xe cỡ đó".
            ShortVehicle = SumOrNull(lanes, lane => lane.SmallVehicleCount),
            MediumVehicle = SumOrNull(lanes, lane => lane.MediumVehicleCount),
            LongVehicle = SumOrNull(lanes, lane => lane.LargeVehicleCount),
            OtherVehicle = SumOrNull(lanes, lane => lane.UnclassifiedVehicleCount),

            AvgSpeed = WeightedAvgSpeed(lanes),
            SpaceOccupy = Avg(lanes, lane => VdsParse.Double(lane.SpaceOccupyRation)),
            TimeOccupy = Avg(lanes, lane => VdsParse.Double(lane.TimeOccupyRation)),
            SpaceHeadway = Avg(lanes, lane => VdsParse.Double(lane.SpaceHeadway)),
            TimeHeadway = Avg(lanes, lane => VdsParse.Double(lane.TimeHeadway)),
            FlowVph = SumOrNull(lanes, lane => lane.Ext?.FlowVph),
            DensityVpkm = Avg(lanes, lane => lane.Ext?.DensityVpkm),
            SpaceMeanKmh = Avg(lanes, lane => lane.Ext?.SpaceMeanKmh),
            KmNumber = camera.Km,
            MetNumber = camera.Met,
        };

        WarnIfPeriodMismatch(entity, metadata);
        WarnIfSizeCountsMismatch(lanes, envelope.Id);

        await _repository.InsertAsync(entity, ct).ConfigureAwait(false);

        // Statistic snapshots are served by module SSE after persistence.

        _logger.LogInformation(
            "[Statistic] event={EventId} cam={Camera} lanes={Lanes} vehicles={Total} " +
            "(S={Short} M={Medium} L={Long} O={Other}) avg={Avg} period={Period}m.",
            entity.VendorEventId, camera.Label, lanes.Count, entity.TotalVehicle,
            entity.ShortVehicle, entity.MediumVehicle, entity.LongVehicle, entity.OtherVehicle,
            entity.AvgSpeed, entity.PeriodMinutes);

        return entity;
    }

    /// <summary>
    /// Độ dài chu kỳ: ưu tiên <c>periodMinutes</c>; thiếu thì suy từ khoảng <c>end_time − start_time</c>
    /// (hai giờ thiết bị cùng múi nên hiệu của chúng vẫn đúng dù giá trị tuyệt đối không dùng được).
    /// </summary>
    private static short? ResolvePeriodMinutes(VdsStatisticMetadata? metadata)
    {
        if (metadata?.PeriodMinutes is int declared && declared is > 0 and <= short.MaxValue)
            return (short)declared;

        var start = VdsParse.DeviceTime(metadata?.StartTime);
        var end = VdsParse.DeviceTime(metadata?.EndTime);
        if (start is null || end is null) return null;

        var minutes = Math.Round((end.Value - start.Value).TotalMinutes, MidpointRounding.AwayFromZero);
        return minutes is > 0 and <= short.MaxValue ? (short)minutes : null;
    }

    /// <summary>
    /// Cảnh báo khi chu kỳ thiết bị gửi không khớp chu kỳ kỳ vọng và cũng không phải ước số của nó.
    /// CHỈ cảnh báo, không sửa số: số đã ghi phải đúng những gì nhận được để còn đối soát được.
    /// </summary>
    private void WarnIfPeriodMismatch(VdsTrafficStatistic entity, VdsStatisticMetadata? metadata)
    {
        var expected = _config.Current.Statistic.ExpectedPeriodMinutes;
        if (expected <= 0) return;

        if (entity.PeriodMinutes is not short minutes || minutes <= 0)
        {
            _logger.LogWarning(
                "[Statistic] event={EventId} không xác định được chu kỳ (periodMinutes={Period}, " +
                "start={Start}, end={End}) ⇒ fromTime bằng toTime, số liệu gom theo chu kỳ sẽ lệch.",
                entity.VendorEventId, metadata?.PeriodMinutes, metadata?.StartTime, metadata?.EndTime);
            return;
        }

        var isDivisor = expected % minutes == 0;
        if (minutes != expected && !isDivisor)
        {
            _logger.LogWarning(
                "[Statistic] event={EventId} chu kỳ {Minutes} phút không khớp chu kỳ kỳ vọng {Expected} " +
                "phút và không phải ước số ⇒ số liệu gom theo chu kỳ sẽ lệch.",
                entity.VendorEventId, minutes, expected);
        }
    }

    /// <summary>
    /// Kiểm bất biến của đặc tả: <c>small + medium + large</c> LUÔN bằng <c>vehicle_num</c> của cùng phần
    /// tử. Đây là phép kiểm rẻ nhất để phát hiện nhà cung cấp đổi ngữ nghĩa mà không thông báo.
    /// Chỉ kiểm khi bản tin có đủ ba trường; payload cũ không có thì bỏ qua, không cảnh báo vô ích.
    /// </summary>
    private void WarnIfSizeCountsMismatch(List<VdsStatisticLaneItem> lanes, long eventId)
    {
        foreach (var lane in lanes)
        {
            if (!lane.HasSizeCounts) continue;
            if (VdsParse.Int(lane.VehicleNum) is not int total) continue;
            if (lane.SizeCountsTotal == total) continue;

            _logger.LogWarning(
                "[Statistic] event={EventId} làn {Lane}: small+medium+large = {Sum} nhưng vehicle_num = " +
                "{Total} (đặc tả cam kết hai số bằng nhau). Vẫn lưu đúng số nhận được.",
                eventId, lane.Lane, lane.SizeCountsTotal, total);
        }
    }

    /// <summary>
    /// Tốc độ trung bình có TRỌNG SỐ theo số xe từng làn.
    ///
    /// Làn khai RÕ <c>vehicle_num = 0</c> bị bỏ hẳn: thiết bị gửi kèm <c>average_speed = 0</c> cho làn
    /// không có xe, mà 0 ở đây nghĩa là "không có dữ liệu", không phải "xe chạy 0 km/h". Tính cả làn rỗng
    /// thì một làn 2 xe chạy 80 km/h cạnh ba làn rỗng cho ra 32 km/h — thấp hơn thực tế một nửa.
    ///
    /// Trọng số 1 chỉ dành cho làn KHÔNG khai số xe mà vẫn cấp tốc độ: lúc đó không biết lưu lượng nên
    /// coi mỗi làn một phiếu. Không làn nào có xe ⇒ NULL, không phải 0.
    /// </summary>
    internal static double? WeightedAvgSpeed(List<VdsStatisticLaneItem> lanes)
    {
        double sum = 0;
        double weight = 0;

        foreach (var lane in lanes)
        {
            if (VdsParse.Double(lane.AverageSpeed) is not double speed) continue;

            var vehicles = VdsParse.Int(lane.VehicleNum);
            if (vehicles is <= 0) continue;

            var laneWeight = vehicles ?? 1;
            sum += speed * laneWeight;
            weight += laneWeight;
        }

        return weight > 0 ? sum / weight : null;
    }

    private static double? Avg(
        List<VdsStatisticLaneItem> lanes, Func<VdsStatisticLaneItem, double?> selector)
    {
        var values = lanes.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return values.Count > 0 ? values.Average() : null;
    }

    /// <summary>
    /// Tổng các làn, trả NULL khi KHÔNG làn nào có giá trị: null = thiết bị không cấp, 0 = có số liệu và
    /// bằng 0. Phân biệt hai cái này là điều kiện để về sau biết số liệu hụt do thiết bị hay do đường ghi.
    /// </summary>
    private static int? SumOrNull(
        List<VdsStatisticLaneItem> lanes, Func<VdsStatisticLaneItem, int?> selector)
    {
        var values = lanes.Select(selector).Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return values.Count > 0 ? values.Sum() : null;
    }
}
