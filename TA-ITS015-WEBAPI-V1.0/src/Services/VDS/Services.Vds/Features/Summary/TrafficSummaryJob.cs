using Shared.Utility.Counting;
using Modules.TMS.Core.Entities;
using Services.Vds.Configuration;
using Services.Vds.Features.Counting;
using Shared.Utility.Utilities;

namespace Services.Vds.Features.Summary;

/// <summary>
/// Author: KienLT
/// Description: Tổng hợp số liệu lâu dài theo giờ/ngày/tháng vào <c>TmsTrafficSummary</c> trên SQL SERVER —
/// nguồn báo cáo DỰ PHÒNG khi QuestDB lỗi.
///
/// <para><b>Khác bản V3</b> (<c>ITS.VDS.Core</c>): V3 đọc từ bảng <c>TmsTrafficCount</c> 5 phút. V7 KHÔNG có
/// bảng đó — số đếm tính lại on-demand từ bảng thô QuestDB. Nên job này đọc thẳng slices từ
/// <see cref="QuestCountReader"/> rồi gộp lên GIỜ. Ngày/tháng thì gộp từ chính <c>TmsTrafficSummary</c>,
/// KHÔNG đụng QuestDB — đó là điều giữ cho báo cáo ngày/tháng vẫn chạy khi QuestDB chết.</para>
///
/// <para><b>Phạm vi lưu</b>: cả <c>equipment</c> và <c>zone</c>. Zone slices lấy sẵn từ cùng câu truy vấn
/// nên không tốn thêm lượt đọc; báo cáo theo vùng đọc trực tiếp, không phải gộp lại từ equipment.</para>
///
/// <para><b>Luỹ đẳng</b>: upsert theo khoá nghiệp vụ. Job lookback vài giờ mỗi lượt để bù khi một lượt
/// trước lỗi hoặc service vừa khởi động lại — chạy lại trên cùng giờ chỉ UPDATE, không sinh trùng.</para>
///
/// <para><b>QuestDB lỗi</b>: <see cref="QuestCountReader"/> đi qua cầu dao <c>QuestAvailabilityGate</c>;
/// khi QuestDB không tới được nó ném, job ghi log và bỏ lượt GIỜ đó. Mức ngày/tháng vẫn chạy vì chỉ đọc
/// SQL Server. Đúng tinh thần fallback: mất vài giờ gần nhất còn hơn hỏng cả báo cáo.</para>
/// Created date: 03/09/2026
/// </summary>
public sealed class TrafficSummaryJob
{
    /// <summary>Mã <c>PeriodType</c> — khớp cột <c>TmsTrafficSummary.PeriodType</c>.</summary>
    public const string PeriodHour = "hour";
    public const string PeriodDay = "day";
    public const string PeriodMonth = "month";

    private readonly QuestCountReader _counts;
    private readonly SqlTrafficSummaryRepository _summaries;
    private readonly IVdsConfigManager _config;
    private readonly ILogger<TrafficSummaryJob> _logger;
    private readonly SummaryProgressStore _progress;

    public TrafficSummaryJob(
        QuestCountReader counts,
        SqlTrafficSummaryRepository summaries,
        IVdsConfigManager config,
        ILogger<TrafficSummaryJob> logger, SummaryProgressStore progress)
    {
        _counts = counts;
        _summaries = summaries;
        _config = config;
        _logger = logger;
        _progress = progress;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (!_config.Current.Summary.Enabled) return;
        var now = DateTime.UtcNow;
        var end = TrafficCountMath.FloorHour(now);
        var original = _progress.Current;
        var progress = original;
        var initial = end.AddHours(-Math.Max(_config.Current.Summary.EffectiveHourLookbackHours, _config.Current.Backfill.EffectiveMaxHours));
        var from = progress.HourThrough ?? initial;
        var overlap = end.AddHours(-_config.Current.Summary.EffectiveHourLookbackHours);
        if (from > overlap) from = overlap;
        var dayFrom = SummaryCalendar.DayStartUtc(from);
        var monthFrom = SummaryCalendar.MonthStartUtc(from);
        if (progress.DayThrough is DateTime d && d < dayFrom) dayFrom = d;
        if (progress.MonthThrough is DateTime m && m < monthFrom) monthFrom = m;
        progress = progress with { DayThrough = dayFrom, MonthThrough = monthFrom };
        // Bounded catch-up. Do not advance past a failed hour, including an empty successful hour.
        for (var hour = from; hour < end && hour < from.AddHours(48); hour = hour.AddHours(1))
        {
            try
            {
                await SummarizeHourAsync(hour, hour.AddHours(1), now, ct);
                progress = progress with { HourThrough = hour.AddHours(1) };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "[Summary] Giữ cursor tại giờ lỗi {Hour:O}.", hour); progress = progress with { HourThrough = hour }; break; }
        }
        try
        {
            var completeDay = SummaryCalendar.DayStartUtc(progress.HourThrough ?? from);
            for (var day = dayFrom; day < completeDay; day = SummaryCalendar.NextDayUtc(day))
            {
                var to = SummaryCalendar.NextDayUtc(day);
                await SummarizeFromLowerAsync(PeriodDay, PeriodHour, day, to, now, ct);
                progress = progress with { DayThrough = to };
            }
            var completeMonth = SummaryCalendar.MonthStartUtc(progress.DayThrough ?? dayFrom);
            for (var month = monthFrom; month < completeMonth; month = SummaryCalendar.NextMonthUtc(month))
            {
                var to = SummaryCalendar.NextMonthUtc(month);
                await SummarizeFromLowerAsync(PeriodMonth, PeriodDay, month, to, now, ct);
                progress = progress with { MonthThrough = to };
            }
        }
        finally
        {
            // A concurrent history repair may rewind progress; never overwrite that revision.
            _progress.Commit(progress, original.Revision);
        }
    }
    /// <summary>Đọc slices equipment + zone của một giờ từ QuestDB, gộp lên giờ, upsert.</summary>
    private async Task SummarizeHourAsync(DateTime hourFrom, DateTime hourTo, DateTime now, CancellationToken ct)
    {
        var equipmentSlices = await _counts.ReadEquipmentSlicesAsync(hourFrom, hourTo, ct).ConfigureAwait(false);
        var zoneSlices = await _counts.ReadZoneSlicesAsync(hourFrom, hourTo, ct).ConfigureAwait(false);

        var rows = BuildHourlySummaries(equipmentSlices, TrafficCountScopes.Equipment, hourFrom, hourTo, now)
            .Concat(BuildHourlySummaries(zoneSlices, TrafficCountScopes.Zone, hourFrom, hourTo, now))
            .ToList();

        await _summaries.ReplacePeriodAsync(rows, PeriodHour, hourFrom, hourTo, ct);

        _logger.LogInformation(
            "[Summary] hour {From:yyyy-MM-dd HH:mm}: {Count} scope (equipment {Eq}, zone {Zn}).",
            hourFrom, rows.Count,
            rows.Count(r => r.ScopeType == TrafficCountScopes.Equipment),
            rows.Count(r => r.ScopeType == TrafficCountScopes.Zone));
    }

    /// <summary>Gộp từ mức thấp hơn (hour→day, day→month) — chỉ đọc/ghi SQL Server.</summary>
    private async Task SummarizeFromLowerAsync(
        string periodType, string lowerPeriodType, DateTime from, DateTime to, DateTime now, CancellationToken ct)
    {
        var lower = await _summaries.QueryRangeAsync(lowerPeriodType, from, to, ct).ConfigureAwait(false);
        var rows = FoldSummaries(lower, periodType, from, to, now);

        await _summaries.ReplacePeriodAsync(rows, periodType, from, to, ct);

        _logger.LogInformation(
            "[Summary] {Period} {From:yyyy-MM-dd}: {Count} scope gộp từ {Lower}.",
            periodType, from, rows.Count, lowerPeriodType);
    }

    // ==================== Phần gộp THUẦN — không I/O, test trực tiếp ====================

    /// <summary>
    /// Gộp các ô đếm của một giờ về bản ghi summary theo scope. Mỗi <c>(ScopeId, Direction)</c> ⇒ một
    /// bản ghi. Trọng số tốc độ là <c>SpeedSampleCount</c> của từng ô (số lượt xe CÓ tốc độ).
    /// </summary>
    public static List<TmsTrafficSummary> BuildHourlySummaries(
        IEnumerable<TrafficCountSlice> slices, string scopeType, DateTime from, DateTime to, DateTime computedAt)
    {
        return slices
            // Chỉ lấy ô đúng scope và nằm trong giờ này (QuestCountReader đã trả đúng scope, lọc thêm cho chắc).
            .Where(s => string.Equals(s.Scope.ScopeType, scopeType, StringComparison.OrdinalIgnoreCase))
            .Where(s => s.From >= from && s.From < to)
            .GroupBy(s => (s.Scope.ScopeId, Direction: NullIfBlank(s.Scope.Direction)))
            .Select(g => BuildOne(scopeType, g.Key.ScopeId, g.Key.Direction, from, to, g.ToList(), computedAt))
            .ToList();
    }

    /// <summary>Gộp các bản ghi summary mức dưới lên mức trên theo <c>(ScopeType, ScopeId, Direction)</c>.</summary>
    public static List<TmsTrafficSummary> FoldSummaries(
        IEnumerable<TmsTrafficSummary> rows, string periodType, DateTime from, DateTime to, DateTime computedAt)
    {
        return rows
            .GroupBy(r => (
                ScopeType: r.ScopeType ?? "",
                ScopeId: r.ScopeId ?? "",
                Direction: NullIfBlank(r.Direction)))
            .Select(g => FoldOne(g.Key.ScopeType, g.Key.ScopeId, g.Key.Direction, periodType, from, to, g.ToList(), computedAt))
            .ToList();
    }

    /// <summary>Một bản ghi summary từ các ô đếm (slice) của cùng một scope trong một giờ.</summary>
    private static TmsTrafficSummary BuildOne(
        string scopeType, string scopeId, string? direction, DateTime from, DateTime to,
        List<TrafficCountSlice> items, DateTime computedAt)
    {
        var s = items.Sum(x => x.Short);
        var m = items.Sum(x => x.Medium);
        var l = items.Sum(x => x.Long);
        var other = items.Sum(x => x.Other);

        return new TmsTrafficSummary
        {
            PeriodType = PeriodHour,
            FromTime = from,
            ToTime = to,
            ScopeType = scopeType,
            ScopeId = scopeId,
            Direction = direction,
            SVehicleNumber = s,
            MVehicleNumber = m,
            LVehicleNumber = l,
            OtherVehicleNumber = other,
            // Tốc độ chung và theo cỡ xe đều dùng số mẫu tốc độ tương ứng làm trọng số.
            AvgSpeed = TrafficCountMath.WeightedAverage(items.Select(x => (x.AvgSpeed, x.SpeedSampleCount))),
            MinSpeed = MinOf(items.Select(x => x.MinSpeed)),
            MaxSpeed = MaxOf(items.Select(x => x.MaxSpeed)),
            AvgSpeedS = TrafficCountMath.WeightedAverage(items.Select(x => (x.ShortSpeed.Average, x.ShortSpeed.Count))),
            SampleCountS = items.Sum(x => x.ShortSpeed.Count),
            AvgSpeedM = TrafficCountMath.WeightedAverage(items.Select(x => (x.MediumSpeed.Average, x.MediumSpeed.Count))),
            SampleCountM = items.Sum(x => x.MediumSpeed.Count),
            AvgSpeedL = TrafficCountMath.WeightedAverage(items.Select(x => (x.LongSpeed.Average, x.LongSpeed.Count))),
            SampleCountL = items.Sum(x => x.LongSpeed.Count),
            AvgSpeedOther = TrafficCountMath.WeightedAverage(items.Select(x => (x.OtherSpeed.Average, x.OtherSpeed.Count))),
            SampleCountOther = items.Sum(x => x.OtherSpeed.Count),
            SampleCount = items.Sum(x => x.SpeedSampleCount),
            ComputedAt = computedAt,
        };
    }

    /// <summary>Một bản ghi summary từ các bản ghi summary mức dưới của cùng một scope.</summary>
    private static TmsTrafficSummary FoldOne(
        string scopeType, string scopeId, string? direction, string periodType, DateTime from, DateTime to,
        List<TmsTrafficSummary> items, DateTime computedAt)
    {
        return new TmsTrafficSummary
        {
            PeriodType = periodType,
            FromTime = from,
            ToTime = to,
            ScopeType = scopeType,
            ScopeId = scopeId,
            Direction = direction,
            SVehicleNumber = items.Sum(x => x.SVehicleNumber ?? 0),
            MVehicleNumber = items.Sum(x => x.MVehicleNumber ?? 0),
            LVehicleNumber = items.Sum(x => x.LVehicleNumber ?? 0),
            OtherVehicleNumber = items.Sum(x => x.OtherVehicleNumber ?? 0),
            AvgSpeed = TrafficCountMath.WeightedAverage(items.Select(x => (x.AvgSpeed, x.SampleCount ?? 0))),
            MinSpeed = MinOf(items.Select(x => x.MinSpeed)),
            MaxSpeed = MaxOf(items.Select(x => x.MaxSpeed)),
            AvgSpeedS = SizeAverage(items.Select(x => (x.AvgSpeedS, x.SampleCountS))),
            SampleCountS = items.All(x => x.SampleCountS.HasValue) ? items.Sum(x => x.SampleCountS ?? 0) : null,
            AvgSpeedM = SizeAverage(items.Select(x => (x.AvgSpeedM, x.SampleCountM))),
            SampleCountM = items.All(x => x.SampleCountM.HasValue) ? items.Sum(x => x.SampleCountM ?? 0) : null,
            AvgSpeedL = SizeAverage(items.Select(x => (x.AvgSpeedL, x.SampleCountL))),
            SampleCountL = items.All(x => x.SampleCountL.HasValue) ? items.Sum(x => x.SampleCountL ?? 0) : null,
            AvgSpeedOther = SizeAverage(items.Select(x => (x.AvgSpeedOther, x.SampleCountOther))),
            SampleCountOther = items.All(x => x.SampleCountOther.HasValue) ? items.Sum(x => x.SampleCountOther ?? 0) : null,
            SampleCount = items.Sum(x => x.SampleCount ?? 0),
            ComputedAt = computedAt,
        };
    }

    private static double? SizeAverage(IEnumerable<(double? Speed, int? Count)> values)
    {
        var rows = values.ToList();
        return rows.Any(x => x.Count == null) ? null : TrafficCountMath.WeightedAverage(rows.Select(x => (x.Speed, x.Count!.Value)));
    }

    private static double? MinOf(IEnumerable<double?> values)
    {
        double? min = null;
        foreach (var v in values)
            if (v is double d) min = min is null || d < min ? d : min;
        return TrafficCountMath.Round(min);
    }

    private static double? MaxOf(IEnumerable<double?> values)
    {
        double? max = null;
        foreach (var v in values)
            if (v is double d) max = max is null || d > max ? d : max;
        return TrafficCountMath.Round(max);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
