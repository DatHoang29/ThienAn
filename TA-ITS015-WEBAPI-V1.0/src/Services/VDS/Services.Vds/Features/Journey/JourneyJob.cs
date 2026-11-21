using Services.Vds.Catalog;
using Services.Vds.Configuration;
using Shared.Utility.Utilities;

namespace Services.Vds.Features.Journey;

/// <summary>
/// Author: KienLT
/// Description: LƯỢT QUÉT ĐỐI SOÁT ghép hành trình A→B theo chu kỳ (<c>Journey:IntervalSeconds</c>).
///
/// <para><b>Từ 29/09/2026 đường chính là ghép THEO BẢN TIN</b> (<see cref="JourneyArrivalProcessor"/>): xe tới
/// camera cuối là tính ngay. Job này chỉ còn BÙ những chuyến đường chính bỏ sót:</para>
/// <list type="bullet">
/// <item>bản tin camera cuối đến TRƯỚC bản tin camera đầu (SSE hai camera trễ khác nhau, hoặc backfill);</item>
/// <item>hàng đợi ghép theo bản tin đầy / mất khi restart;</item>
/// <item>QuestDB lỗi đúng lúc xe tới camera cuối;</item>
/// <item>biển số hai camera ghi khác định dạng (đường chính lọc biển THÔ, job này chuẩn hoá trong bộ nhớ).</item>
/// </list>
///
/// <para>Quét cả đoạn KHAI lẫn đoạn TỰ SINH theo đúng luật chọn của <see cref="JourneySegmentResolver"/>. Cửa
/// sổ quét = MaxTravelSeconds + đệm; ghi luỹ đẳng (<see cref="JourneyWriter"/>) nên trùng với đường chính
/// không sinh dòng thứ hai.</para>
///
/// <para><b>Chỉ nâng sự cố từ hành trình HỢP LỆ</b> (thời gian di chuyển trong biên đoạn) — fail-closed.</para>
/// Created date: 04/09/2026
/// </summary>
public sealed class JourneyJob
{
    private readonly JourneyProgressStore _progress;
    private readonly JourneySegmentResolver _segments;
    private readonly CameraManager _cameras;
    private readonly QuestJourneySightingReader _reader;
    private readonly JourneyWriter _writer;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<JourneyJob> _logger;

    public JourneyJob(
        JourneySegmentResolver segments,
        CameraManager cameras,
        QuestJourneySightingReader reader,
        JourneyWriter writer,
        IVdsConfigManager config,
        ILogger<JourneyJob> logger, JourneyProgressStore? progress = null)
    {
        _progress = progress ?? new JourneyProgressStore(config);
        _segments = segments;
        _cameras = cameras;
        _reader = reader;
        _writer = writer;
        _config = config;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var config = _config.Current;
        var options = config.Journey;
        if (!options.Enabled) return;

        var segments = _segments.AllEffective();
        if (segments.Count == 0) return;

        // Mọi mốc đọc QuestDB theo UTC (detectTime là UTC). now theo UTC để khớp.
        var nowUtc = DateTime.UtcNow;
        var written = 0;
        var incidents = 0;

        foreach (var seg in segments)
        {
            ct.ThrowIfCancellationRequested();
            var activeCameraIds = _cameras.All.Select(c => c.EquipmentId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!activeCameraIds.Contains(seg.FromEquipmentId) || !activeCameraIds.Contains(seg.ToEquipmentId)) continue;

            // Biên thời gian của đoạn trên DB quyết định cửa sổ quét. Giá trị mặc định từ VdsConfig
            // đã được SegmentManager dùng khi đoạn thiếu biên; appsettings không được cắt ngắn ở đây.
            var lookback = TimeSpan.FromSeconds(seg.MaxTravelSeconds)
                + TimeSpan.FromSeconds(options.EffectiveReconcilePaddingSeconds);

            var progress = _progress.Read(seg.SegmentId, nowUtc - lookback);
            if (progress.From > nowUtc) throw new InvalidDataException("Journey cursor is in the future.");
            var through = progress.From;
            for (var batch = 0; batch < 24 && through < nowUtc; batch++)
            {
                var endUtc = through.AddHours(1) < nowUtc ? through.AddHours(1) : nowUtc;
                var fromUtc = through - lookback;

                List<JourneySighting> rows;
                try
                {
                    rows = await _reader
                        .ReadAsync(seg.FromEquipmentId, seg.ToEquipmentId, fromUtc, endUtc, ct)
                        .ConfigureAwait(false);
                }
                catch (QuestDbUnavailableException)
                {
                    // QuestDB đang bị cầu dao chặn: bỏ lượt đoạn này, thử lại lượt job sau.
                    _logger.LogWarning("[Journey] QuestDB không sẵn sàng; bỏ đoạn {Segment} lượt này.", seg.SegmentId);
                    break;
                }

                var candidates = JourneyMatcher.Match(seg, rows);


                CameraInfo? toCamera = _cameras.All
                    .FirstOrDefault(c => string.Equals(c.EquipmentId, seg.ToEquipmentId, StringComparison.OrdinalIgnoreCase));

                foreach (var c in candidates.Where(c => c.ToTime >= through.AddSeconds(-options.EffectiveReconcilePaddingSeconds) && c.ToTime < endUtc))
                {
                    var outcome = await _writer.WriteAsync(c, toCamera, nowUtc, ct).ConfigureAwait(false);
                    if (outcome.JourneyCreated) written++;
                    if (outcome.IncidentCreated) incidents++;
                }
                if (!_progress.Commit(seg.SegmentId, endUtc, progress.Revision)) break;
                through = endUtc;
            }
        }

        if (written > 0 || incidents > 0)
            _logger.LogInformation(
                "[Journey] Ghép {Written} hành trình A→B, nâng {Incidents} sự cố quá tốc độ trung bình.",
                written, incidents);
    }
}
