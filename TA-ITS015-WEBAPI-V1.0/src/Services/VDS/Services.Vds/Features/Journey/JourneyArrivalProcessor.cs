using Services.Vds.Catalog;
using Services.Vds.Configuration;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: Ghép hành trình A→B NGAY khi nhận một lượt xe có biển (thay cho chờ job chu kỳ). Một lượt xe ở
/// camera X được xét theo HAI chiều, mỗi chiều một hàm:
/// <list type="number">
/// <item><see cref="MatchAsEndAsync"/> — X là camera CUỐI: chọn đoạn bằng
/// <see cref="JourneySegmentResolver.ForArrival"/> (đoạn khai thắng đúng cặp trùng với đoạn tự sinh);
/// tìm lượt A cùng biển trước đó cho cả đoạn liền kề và đoạn dài.</item>
/// <item><see cref="MatchAsStartAsync"/> — X là camera ĐẦU: bắt ca lượt A đến SAU lượt B (bản tin camera đầu
/// trễ). Tìm lượt B cùng biển SAU nó mà chưa được ghép.</item>
/// </list>
///
/// <para>Cả hai đọc QuestDB các lượt CÙNG BIỂN (thô) ở A và B — vài dòng — rồi ghép bằng đúng
/// <see cref="JourneyMatcher.Match"/> của lượt quét đối soát. Lượt xe đang xét lấy từ bản tin, không chờ QuestDB
/// đọc lại được (WAL có độ trễ).</para>
///
/// <para><b>Một lượt B chỉ một hành trình</b>: <see cref="JourneyRepository"/> chặn thêm theo mốc kết thúc, nên
/// hai đường chọn hai lượt A khác nhau cho cùng lượt B thì chuyến ghi trước được giữ và không nâng sự cố lần hai.</para>
/// Created date: 29/09/2026
/// </summary>
public sealed class JourneyArrivalProcessor
{
    /// <summary>Sai lệch cho phép khi so mốc lượt đọc lại từ QuestDB với mốc trong bản tin.</summary>
    private static readonly TimeSpan MatchTolerance = TimeSpan.FromSeconds(1);

    private readonly JourneySegmentResolver _segments;
    private readonly CameraManager _cameras;
    private readonly QuestJourneySightingReader _reader;
    private readonly JourneyWriter _writer;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<JourneyArrivalProcessor> _logger;

    public JourneyArrivalProcessor(
        JourneySegmentResolver segments,
        CameraManager cameras,
        QuestJourneySightingReader reader,
        JourneyWriter writer,
        IVdsConfigManager config,
        ILogger<JourneyArrivalProcessor> logger)
    {
        _segments = segments;
        _cameras = cameras;
        _reader = reader;
        _writer = writer;
        _config = config;
        _logger = logger;
    }

    /// <summary>Xử lý một lượt xe ở camera X theo cả hai chiều. Trả số hành trình mới ghi.</summary>
    public async Task<int> ProcessAsync(JourneyArrival arrival, CancellationToken ct)
    {
        if (!_config.Current.Journey.Enabled) return 0;
        return await MatchAsEndAsync(arrival, ct).ConfigureAwait(false)
               + await MatchAsStartAsync(arrival, ct).ConfigureAwait(false);
    }

    /// <summary>X là camera CUỐI B: tìm lượt A cùng biển trong <c>[tB − Max − đệm, tB)</c>.</summary>
    private async Task<int> MatchAsEndAsync(JourneyArrival arrival, CancellationToken ct)
    {
        var segments = _segments.ForArrival(arrival.EquipmentId);
        if (segments.Count == 0) return 0;

        var padding = _config.Current.Journey.EffectiveLookbackPaddingSeconds;
        // Xét MỌI đoạn kết thúc ở B nhưng chỉ ghi ứng viên từ camera GẦN NHẤT có bắt được xe
        // (JourneyNearestRules) — xe đi 06 → 04 → 02 chỉ sinh 04→02 ở lượt B này, không sinh thêm đoạn dài 06→02.
        var candidates = new List<(SegmentInfo Segment, JourneyCandidate Candidate)>();
        foreach (var seg in segments)
        {
            ct.ThrowIfCancellationRequested();
            if (!IsActive(seg.FromEquipmentId)) continue;   // camera đầu không còn trong danh mục — giống JourneyJob

            var rows = await _reader.ReadPlateAsync(seg.FromEquipmentId, seg.ToEquipmentId, arrival.LicensePlate,
                arrival.DetectTimeUtc.AddSeconds(-(seg.MaxTravelSeconds + padding)),
                arrival.DetectTimeUtc + MatchTolerance, ct).ConfigureAwait(false);
            if (PickCandidate(seg, rows, arrival) is { } candidate) candidates.Add((seg, candidate));
        }
        var nearest = JourneyNearestRules.PickNearest(candidates.Select(x => x.Candidate));
        if (nearest is null) return 0;
        return await WriteAsync(candidates.First(x => ReferenceEquals(x.Candidate, nearest)).Segment, nearest, "cuối", ct).ConfigureAwait(false);
    }

    /// <summary>X là camera ĐẦU A: tìm lượt B cùng biển trong <c>(tA, tA + Max]</c> (lượt A đến sau lượt B).</summary>
    private async Task<int> MatchAsStartAsync(JourneyArrival arrival, CancellationToken ct)
    {
        var segments = _segments.ForDeparture(arrival.EquipmentId);
        if (segments.Count == 0) return 0;

        var padding = _config.Current.Journey.EffectiveLookbackPaddingSeconds;
        var written = 0;
        foreach (var seg in segments)
        {
            ct.ThrowIfCancellationRequested();
            if (!IsActive(seg.ToEquipmentId)) continue;

            // Đọc cả khoảng TRƯỚC tA để matcher thấy lượt B cũ đã tiêu thụ lượt A cũ, giữ đúng luật "mỗi A một B".
            var rows = await _reader.ReadPlateAsync(seg.FromEquipmentId, seg.ToEquipmentId, arrival.LicensePlate,
                arrival.DetectTimeUtc.AddSeconds(-(seg.MaxTravelSeconds + padding)),
                arrival.DetectTimeUtc.AddSeconds(seg.MaxTravelSeconds) + MatchTolerance, ct).ConfigureAwait(false);
            written += await WriteAsync(seg, PickDepartureCandidate(seg, rows, arrival), "đầu", ct).ConfigureAwait(false);
        }
        return written;
    }

    private async Task<int> WriteAsync(SegmentInfo seg, JourneyCandidate? candidate, string role, CancellationToken ct)
    {
        if (candidate is null) return 0;
        var toCamera = _cameras.All.FirstOrDefault(c =>
            string.Equals(c.EquipmentId, seg.ToEquipmentId, StringComparison.OrdinalIgnoreCase));
        var outcome = await _writer.WriteAsync(candidate, toCamera, DateTime.UtcNow, ct).ConfigureAwait(false);
        if (!outcome.JourneyCreated) return 0;
        _logger.LogDebug(
            "[Journey] Ghép theo bản tin (camera {Role}) {Segment}{Kind}: {From:O} → {To:O}, {Speed:0.#} km/h, hợp lệ={Valid}.",
            role, seg.SegmentId, seg.IsAuto ? " (tự sinh)" : "", candidate.FromTime, candidate.ToTime,
            candidate.AvgSpeedKmh, candidate.IsValid);
        return 1;
    }

    private bool IsActive(string equipmentId)
        => _cameras.All.Any(c => string.Equals(c.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Chuyến KẾT THÚC ở chính lượt B này. Hàm thuần — kiểm được không cần QuestDB.
    ///
    /// <para>Lượt B lấy từ bản tin; dòng B trùng mốc (nếu QuestDB đã thấy) bị bỏ để không ghép hai lần. Các lượt B
    /// CŨ hơn vẫn giữ để matcher tiêu thụ lượt A của chúng (mỗi A một B). <c>FromTime</c> lấy từ dòng A trong
    /// QuestDB như lượt quét đối soát ⇒ cùng khoá hành trình / <c>EventId</c>.</para>
    /// </summary>
    public static JourneyCandidate? PickCandidate(SegmentInfo seg, IReadOnlyList<JourneySighting> rows, JourneyArrival arrival)
    {
        var sightings = rows
            .Where(r => !(IsCamera(r, seg.ToEquipmentId) && Near(r.DetectTime, arrival.DetectTimeUtc)))
            .Where(r => r.DetectTime < arrival.DetectTimeUtc)
            .Append(new JourneySighting(seg.ToEquipmentId, arrival.LicensePlate, arrival.DetectTimeUtc, arrival.VehicleSize))
            .ToList();

        return JourneyMatcher.Match(seg, sightings)
            .FirstOrDefault(c => c.ToTime == arrival.DetectTimeUtc);
    }

    /// <summary>
    /// Chuyến BẮT ĐẦU ở chính lượt A này (lượt A đến sau lượt B). Hàm thuần.
    ///
    /// <para>Ưu tiên mốc của dòng A trong QuestDB (nếu đã thấy), không thì dùng mốc bản tin. Chỉ nhận chuyến mà
    /// lượt A này là lượt A GẦN NHẤT trước lượt B — đúng luật matcher; nếu có lượt A khác sau nó thì chuyến thuộc về
    /// lượt A kia.</para>
    /// </summary>
    public static JourneyCandidate? PickDepartureCandidate(SegmentInfo seg, IReadOnlyList<JourneySighting> rows, JourneyArrival arrival)
    {
        // FromTime nằm trong EventId và khoá hành trình, mà chuỗi thời gian "O" phụ thuộc DateTimeKind (xem
        // VdsJourneyEventConst.EventId). Vì vậy ƯU TIÊN mốc của chính dòng A trong QuestDB (như lượt quét đối soát);
        // chưa thấy dòng đó thì dùng mốc bản tin nhưng gán cùng Kind với các dòng QuestDB đang đọc.
        var stored = rows.Where(r => IsCamera(r, seg.FromEquipmentId) && Near(r.DetectTime, arrival.DetectTimeUtc))
            .OrderBy(r => (r.DetectTime - arrival.DetectTimeUtc).Duration()).Cast<JourneySighting?>().FirstOrDefault();
        DateTime start;
        List<JourneySighting> sightings;
        if (stored is { } a)
        {
            start = a.DetectTime;
            sightings = rows.ToList();
        }
        else
        {
            var kind = rows.Count > 0 ? rows[0].DetectTime.Kind : DateTimeKind.Utc;
            start = DateTime.SpecifyKind(arrival.DetectTimeUtc, kind);
            sightings = rows.Append(new JourneySighting(seg.FromEquipmentId, arrival.LicensePlate, start, arrival.VehicleSize)).ToList();
        }

        return JourneyMatcher.Match(seg, sightings)
            .FirstOrDefault(c => c.FromTime == start);
    }

    private static bool IsCamera(JourneySighting r, string equipmentId)
        => string.Equals(r.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase);

    private static bool Near(DateTime a, DateTime b) => (a - b).Duration() <= MatchTolerance;
}
