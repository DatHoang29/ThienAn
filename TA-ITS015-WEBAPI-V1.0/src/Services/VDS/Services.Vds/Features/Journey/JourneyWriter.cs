using Services.Vds.Catalog;
using Services.Vds.Configuration;
using Services.Vds.Features.Events;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Ghi MỘT hành trình A→B đã ghép (<c>TmsTrafficJourney</c>) rồi nâng sự cố quá tốc độ trung bình
/// nếu đủ điều kiện. Tách khỏi <see cref="JourneyJob"/> ngày 29/09/2026 để ghép theo bản tin
/// (<see cref="JourneyArrivalProcessor"/>) và lượt quét đối soát ghi cùng một cách.
///
/// <para>Luỹ đẳng: hành trình theo <c>(SegmentId, biển, FromTime)</c>, sự cố theo <c>EventId</c> tất định —
/// hai đường cùng ghép một chuyến thì chỉ một dòng hành trình và một sự cố.</para>
/// Created date: 29/09/2026
/// </summary>
public sealed class JourneyWriter
{
    /// <summary>Chuyến kết thúc trước mốc này so với hiện tại được nâng sự cố ở chế độ phục hồi (không phát realtime).</summary>
    public static readonly TimeSpan RecoveryAge = TimeSpan.FromMinutes(5);

    private readonly JourneyRepository _journeys;
    private readonly IncidentPromoter _incidents;
    private readonly CameraManager _cameras;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<JourneyWriter> _logger;
    private readonly QuestJourneySightingReader _reader;
    private readonly SpeedRangeResolver _speeds;

    public JourneyWriter(JourneyRepository journeys, IncidentPromoter incidents, CameraManager cameras,
        IVdsConfigManager config, ILogger<JourneyWriter> logger, QuestJourneySightingReader reader,
        SpeedRangeResolver speeds)
    {
        _journeys = journeys;
        _incidents = incidents;
        _cameras = cameras;
        _config = config;
        _logger = logger;
        _reader = reader;
        _speeds = speeds;
    }

    /// <summary>
    /// Ghi hành trình và nâng sự cố. Camera cuối thiếu trong danh mục mà chuyến đủ điều kiện nâng sự cố thì NÉM
    /// để bên gọi giữ tiến độ/thử lại (thiếu km/vị trí thì không dựng được sự cố).
    /// </summary>
    public async Task<(bool JourneyCreated, bool IncidentCreated)> WriteAsync(
        JourneyCandidate c, CameraInfo? toCamera, DateTime nowUtc, CancellationToken ct)
    {
        var config = _config.Current;
        var entity = new TmsTrafficJourney
        {
            SegmentId = c.SegmentId,
            LicensePlate = c.LicensePlate,
            FromEquipmentId = c.FromEquipmentId,
            ToEquipmentId = c.ToEquipmentId,
            FromTime = c.FromTime,
            ToTime = c.ToTime,
            TravelSeconds = c.TravelSeconds,
            DistanceMeter = c.DistanceMeter,
            AvgSpeedKmh = c.AvgSpeedKmh,
            Type = c.VehicleSize,
            Direction = c.Direction,
            IsValid = c.IsValid,
            InvalidReason = c.InvalidReason,
        };

        // Nâng sự cố quá tốc độ trung bình — luật nằm ở JourneyOverSpeedRules.Decide, kể cả lý do bỏ qua.
        // Ngưỡng theo CAMERA hai đầu (vùng chính → mặc định hướng), lấy giá trị lớn nhất.
        var fromCamera = CameraOf(c.FromEquipmentId);
        var limit = JourneyOverSpeedRules.AllowedSpeedKmh(
            CameraMaxKmh(fromCamera, c.Direction),
            CameraMaxKmh(toCamera ?? CameraOf(c.ToEquipmentId), c.Direction));
        var decision = JourneyOverSpeedRules.Decide(c, limit, config.Events.Enabled);
        var eligible = !decision.IsSkip;
        // Lưu kết quả xét cho SSE sự kiện speedingAvg: tách khỏi cờ bật/tắt sự kiện vì đó là chuyện tạo hồ sơ.
        entity.SpeedLimitKmh = limit;
        entity.IsOverSpeed = !JourneyOverSpeedRules.Decide(c, limit, eventsEnabled: true).IsSkip;

        // Chỉ chờ retry khi thật sự còn hồ sơ phải tạo: loại nhóm event thì hành trình (kèm cờ) cứ lưu để lên SSE.
        if (toCamera is null && eligible
            && await _incidents.RecordsIncidentAsync(Modules.VDS.Core.Constants.VdsJourneyEventConst.EventTypeName, ct).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "[Journey] Đoạn {Segment} có camera cuối {Equipment} không trong danh mục camera VDS " +
                "⇒ không nâng được sự cố (thiếu km/vị trí).", c.SegmentId, c.ToEquipmentId);
            throw new InvalidOperationException("Journey camera missing; retain progress for retry.");
        }

        // Chi tiết cho Mô tả/Metadata sự cố: mã camera hai đầu, quãng đường, thời gian, ngưỡng đã xét.
        // Ngưỡng tối thiểu cho Metadata (token VMS {SpeedMin}) — dễ dãi nhất = nhỏ nhất của hai camera,
        // đối xứng với cách lấy max ở trên. Chỉ để hiển thị, không tham gia xét vi phạm.
        var minLimit = LenientMinKmh(
            CameraMinKmh(fromCamera, c.Direction),
            CameraMinKmh(toCamera ?? CameraOf(c.ToEquipmentId), c.Direction));
        var detail = new AverageSpeedDetail(CodeOf(c.FromEquipmentId), toCamera?.Code ?? CodeOf(c.ToEquipmentId),
            c.DistanceMeter, c.TravelSeconds, limit, c.FromTime, c.ToTime, minLimit);

        return await PersistAndPromoteAsync(
            async () =>
            {
                var stored = await _journeys.InsertIdempotentAsync(entity, ct).ConfigureAwait(false);
                // Luật camera gần nhất: bản tin camera giữa đến trễ ⇒ chuyến dài đã ghi trước bị xoá mềm. Sự cố (nếu chuyến
                // dài đã kịp nâng) KHÔNG tự đóng — ghi log để tra.
                if (stored.SupersededIds is { Count: > 0 } superseded)
                    _logger.LogInformation(
                        "[Journey] {Plate}: {Segment} gần hơn ⇒ xoá mềm {Count} hành trình xa hơn cùng lượt B ({Ids}).",
                        c.LicensePlate, c.SegmentId, superseded.Count, string.Join(", ", superseded));
                return stored;
            },
            eligible && toCamera is not null
                ? async () =>
                {
                    // Loại nhóm event không ghi hồ sơ: kiểm trước để khỏi đọc nguồn ảnh (và khỏi ném chờ retry khi
                    // bản ghi thô điểm B chưa thấy) cho một hồ sơ sẽ không được tạo.
                    if (!await _incidents.RecordsIncidentAsync(Modules.VDS.Core.Constants.VdsJourneyEventConst.EventTypeName, ct))
                        return new PromoteResult(false, null, VdsEventGate.EventOnly);
                    var source = await _reader.ReadImageSourceAsync(c.ToEquipmentId, c.ToTime, c.LicensePlate, ct)
                        ?? throw new InvalidOperationException("Raw B sighting is not yet visible; retry journey promotion with its image source.");
                    return await _incidents.PromoteAverageSpeedAsync(toCamera, c.SegmentId, c.LicensePlate, c.AvgSpeedKmh,
                        c.FromTime, c.ToTime, ct, recovery: c.ToTime < nowUtc - RecoveryAge,
                        detail: detail, registrationSource: source);
                }
                : null).ConfigureAwait(false);
    }

    /// <summary>Mã camera (VDS-01…) theo ID; không có trong danh mục thì trả chính ID.</summary>
    private string CodeOf(string equipmentId)
        => CameraOf(equipmentId)?.Code ?? equipmentId;

    /// <summary>Camera VDS theo ID thiết bị; không có trong danh mục ⇒ null.</summary>
    private CameraInfo? CameraOf(string? equipmentId)
        => string.IsNullOrWhiteSpace(equipmentId)
            ? null
            : _cameras.All.FirstOrDefault(x =>
                string.Equals(x.EquipmentId, equipmentId.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Ngưỡng max hiệu lực của MỘT camera: <see cref="SpeedRangeResolver.ForCamera"/> (vùng chính → mặc định
    /// hướng camera); không ghép được cặp hợp lệ thì lấy max mặc định theo hướng camera (camera không khai
    /// hướng ⇒ hướng của chuyến). Camera không có trong danh mục ⇒ null (bỏ camera đó).
    /// </summary>
    private double? CameraMaxKmh(CameraInfo? camera, string? journeyDirection)
    {
        if (camera is null) return null;
        var direction = string.IsNullOrWhiteSpace(camera.Direction) ? journeyDirection : camera.Direction;
        return _speeds.ForCamera(camera)?.Range.Max ?? _speeds.DirectionRangeOf(direction)?.Max;
    }

    /// <summary>Ngưỡng min hiệu lực của MỘT camera — cùng thứ tự phân giải với <see cref="CameraMaxKmh"/>.</summary>
    private double? CameraMinKmh(CameraInfo? camera, string? journeyDirection)
    {
        if (camera is null) return null;
        var direction = string.IsNullOrWhiteSpace(camera.Direction) ? journeyDirection : camera.Direction;
        return _speeds.ForCamera(camera)?.Range.Min ?? _speeds.DirectionRangeOf(direction)?.Min;
    }

    /// <summary>Giá trị nhỏ nhất trong các ngưỡng min dương, hữu hạn; không còn giá trị nào ⇒ null. Hàm thuần.</summary>
    internal static double? LenientMinKmh(params double?[] values)
    {
        double? result = null;
        foreach (var value in values)
        {
            if (value is not double v || !double.IsFinite(v) || v <= 0) continue;
            if (result is null || v < result) result = v;
        }
        return result;
    }

    public static async Task<(bool JourneyCreated, bool IncidentCreated)> PersistAndPromoteAsync(
        Func<Task<JourneyInsertResult>> persist, Func<Task<PromoteResult>>? promote)
    {
        var stored = await persist();
        // Lượt B đã được ghép với lượt A khác ⇒ chuyến này KHÔNG tồn tại, không nâng sự cố cho nó.
        if (!stored.Created && !stored.SameStart) return (false, false);
        // Existing journey does not prove that incident promotion completed.
        var incident = promote == null ? default : await promote();
        return (stored.Created, incident.IncidentCreated);
    }
}
