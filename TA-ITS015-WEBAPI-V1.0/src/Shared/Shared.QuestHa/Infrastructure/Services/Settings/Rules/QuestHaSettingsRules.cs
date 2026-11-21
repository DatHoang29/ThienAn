using System.Globalization;

namespace Shared.QuestHa.Infrastructure.Services.Settings.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật thuần cho tham số vận hành QuestDB HA: biên min/max, cặp Warn &lt; Critical, giờ <c>HH:mm</c>, slot lịch backup;
/// gộp gói đổi lúc chạy lên giá trị khởi động và liệt kê khoá đổi. Không I/O, không log.
/// Created date: 31/10/2026
/// </summary>
internal static class QuestHaSettingsRules
{
    private const string TimeFormat = "HH:mm";

    /// <summary>Kiểm thuộc tính đã khai; cặp ngưỡng chỉ so khi khai cả hai.</summary>
    public static IReadOnlyList<string> Validate(QuestHaRuntimeOptions r)
    {
        var errors = new List<string>();
        Range(errors, nameof(r.HealthCheckInterval), r.HealthCheckInterval, Sec(5), Hours(1));
        Range(errors, nameof(r.HealthHeartbeatStaleAfter), r.HealthHeartbeatStaleAfter, Sec(5), Hours(1));
        Range(errors, nameof(r.HealthLagWarnSeconds), r.HealthLagWarnSeconds, 1, 86_400);
        Range(errors, nameof(r.HealthLagCriticalSeconds), r.HealthLagCriticalSeconds, 1, 86_400);
        Range(errors, nameof(r.HealthBackupWarnAfter), r.HealthBackupWarnAfter, Hours(1), Days(30));
        Range(errors, nameof(r.HealthBackupCriticalAfter), r.HealthBackupCriticalAfter, Hours(1), Days(30));
        Range(errors, nameof(r.HealthReconcileWarnAfter), r.HealthReconcileWarnAfter, TimeSpan.FromMinutes(1), Days(7));
        Range(errors, nameof(r.HealthRepeatWarningEvery), r.HealthRepeatWarningEvery, TimeSpan.FromMinutes(1), Days(1));
        Range(errors, nameof(r.HealthMinAlertGap), r.HealthMinAlertGap, Sec(1), Hours(1));

        Range(errors, nameof(r.JournalInterval), r.JournalInterval, Sec(10), Days(1));
        Range(errors, nameof(r.JournalRetention), r.JournalRetention, Hours(1), Days(30));
        Range(errors, nameof(r.JournalProtectedBackupDays), r.JournalProtectedBackupDays, 1, 3650);
        // Lô nhỏ tránh leo thang khoá trên bảng Outbox.
        Range(errors, nameof(r.JournalBatchSize), r.JournalBatchSize, 1, 500);
        Range(errors, nameof(r.JournalMaxRowsPerRun), r.JournalMaxRowsPerRun, 1, 1_000_000);
        Range(errors, nameof(r.JournalMaxRunDuration), r.JournalMaxRunDuration, Sec(1), TimeSpan.FromMinutes(1));
        Range(errors, nameof(r.JournalAlertThrottle), r.JournalAlertThrottle, Sec(10), Days(1));
        Range(errors, nameof(r.JournalWarnPendingRows), r.JournalWarnPendingRows, 1L, 1_000_000_000_000L);
        Range(errors, nameof(r.JournalCriticalPendingRows), r.JournalCriticalPendingRows, 1L, 1_000_000_000_000L);
        Range(errors, nameof(r.JournalWarnPendingAge), r.JournalWarnPendingAge, Sec(1), Days(30));
        Range(errors, nameof(r.JournalCriticalPendingAge), r.JournalCriticalPendingAge, Sec(1), Days(30));
        Range(errors, nameof(r.JournalWarnJournalBytes), r.JournalWarnJournalBytes, 1L, 1_000_000_000_000_000L);
        Range(errors, nameof(r.JournalCriticalJournalBytes), r.JournalCriticalJournalBytes, 1L, 1_000_000_000_000_000L);

        if (r.BackupSchedule is { Length: > 0 } schedule && !IsTime(schedule))
            errors.Add($"{nameof(r.BackupSchedule)} phải là HH:mm hoặc rỗng (tắt lịch).");
        if (r.BackupWeeklyDay is { } day && !Enum.IsDefined(day))
            errors.Add($"{nameof(r.BackupWeeklyDay)} không hợp lệ.");
        if (r.BackupWindowStart is not null && !IsTime(r.BackupWindowStart))
            errors.Add($"{nameof(r.BackupWindowStart)} phải là HH:mm.");
        if (r.BackupWindowEnd is not null && !IsTime(r.BackupWindowEnd))
            errors.Add($"{nameof(r.BackupWindowEnd)} phải là HH:mm.");
        // Đầu > cuối là cửa sổ qua nửa đêm (hợp lệ); trùng nhau thì cửa sổ rỗng.
        if (r.BackupWindowStart is not null && r.BackupWindowStart == r.BackupWindowEnd)
            errors.Add($"{nameof(r.BackupWindowStart)} và {nameof(r.BackupWindowEnd)} không được trùng nhau.");
        Range(errors, nameof(r.BackupNodeOffset), r.BackupNodeOffset, TimeSpan.FromMinutes(1), Hours(23));
        Range(errors, nameof(r.BackupMaxAttempts), r.BackupMaxAttempts, 1, 10);
        Range(errors, nameof(r.BackupRetryDelay), r.BackupRetryDelay, Sec(10), Days(7));
        // Tối thiểu 7 daily + 4 weekly như BackupOptions.Validate: ít hơn thì mất điểm restore-replay đã cam kết.
        Range(errors, nameof(r.BackupDailyCopies), r.BackupDailyCopies, 7, 365);
        Range(errors, nameof(r.BackupWeeklyCopies), r.BackupWeeklyCopies, 4, 520);
        Range(errors, nameof(r.BackupManualCopies), r.BackupManualCopies, 1, 100);
        Range(errors, nameof(r.BackupMaxStoppedDuration), r.BackupMaxStoppedDuration, TimeSpan.FromMinutes(1), Days(1));
        Range(errors, nameof(r.BackupMinThroughputBytesPerSecond), r.BackupMinThroughputBytesPerSecond, 1L, 10_000_000_000_000L);
        Range(errors, nameof(r.BackupMinimumFreeBytes), r.BackupMinimumFreeBytes, 0L, 1_000_000_000_000_000L);

        // CoverageWait = 0 làm mọi lượt đọc báo không phủ ngay ⇒ không cho.
        Range(errors, nameof(r.RouterCoverageWait), r.RouterCoverageWait, Sec(1), TimeSpan.FromMinutes(1));
        Range(errors, nameof(r.RouterFailbackStableFor), r.RouterFailbackStableFor, Sec(1), Days(1));
        Range(errors, nameof(r.RouterBreakerCooldown), r.RouterBreakerCooldown, Sec(1), TimeSpan.FromMinutes(10));

        // Export: lịch luôn là giờ cụ thể (bật/tắt là Export:Enabled hạ tầng); giữ ≥ 7 ngày để không xoá bản vừa dùng làm căn cứ dọn journal.
        if (r.ExportSchedule is not null && !IsTime(r.ExportSchedule))
            errors.Add($"{nameof(r.ExportSchedule)} phải là HH:mm.");
        Range(errors, nameof(r.ExportRetentionDays), r.ExportRetentionDays, 7, 3650);
        Range(errors, nameof(r.ExportRecheckDays), r.ExportRecheckDays, 0, 6);
        Range(errors, nameof(r.ExportMaxAttempts), r.ExportMaxAttempts, 1, 20);
        Range(errors, nameof(r.ExportRetryDelay), r.ExportRetryDelay, TimeSpan.FromMinutes(1), Hours(6));
        Range(errors, nameof(r.ExportWarnAfter), r.ExportWarnAfter, Hours(1), Days(30));
        Range(errors, nameof(r.ExportCriticalAfter), r.ExportCriticalAfter, Hours(1), Days(30));

        Below(errors, nameof(r.HealthLagWarnSeconds), r.HealthLagWarnSeconds, nameof(r.HealthLagCriticalSeconds), r.HealthLagCriticalSeconds);
        Below(errors, nameof(r.HealthBackupWarnAfter), r.HealthBackupWarnAfter, nameof(r.HealthBackupCriticalAfter), r.HealthBackupCriticalAfter);
        Below(errors, nameof(r.JournalWarnPendingRows), r.JournalWarnPendingRows, nameof(r.JournalCriticalPendingRows), r.JournalCriticalPendingRows);
        Below(errors, nameof(r.JournalWarnPendingAge), r.JournalWarnPendingAge, nameof(r.JournalCriticalPendingAge), r.JournalCriticalPendingAge);
        Below(errors, nameof(r.JournalWarnJournalBytes), r.JournalWarnJournalBytes, nameof(r.JournalCriticalJournalBytes), r.JournalCriticalJournalBytes);
        Below(errors, nameof(r.ExportWarnAfter), r.ExportWarnAfter, nameof(r.ExportCriticalAfter), r.ExportCriticalAfter);
        return errors;
    }

    /// <summary>
    /// Luật DUY NHẤT cho "bộ có hiệu lực": kiểm gói, gộp lên <paramref name="baseline"/>, kiểm bộ đã gộp. Manager dùng trước khi áp
    /// (<paramref name="candidate"/> = bộ sẽ áp khi không lỗi) và host dùng khi lưu cấu hình (<see cref="QuestHaRuntimeOptions.ValidateEffective"/>)
    /// nên không lưu được bộ mà Manager sẽ từ chối.
    /// </summary>
    public static IReadOnlyList<string> ValidateEffective(QuestHaOptions baseline, QuestHaRuntimeOptions r, out QuestHaOptions? candidate)
    {
        candidate = null;
        var errors = Validate(r);
        if (errors.Count > 0) return errors;
        var merged = Merge(baseline, r);
        errors = ValidateMerged(merged);
        if (errors.Count == 0) candidate = merged;
        return errors;
    }

    /// <summary>
    /// Kiểm bộ đã gộp: mọi tham số vận hành (kể cả lấy từ giá trị khởi động) qua <see cref="Validate"/>, rồi slot lịch backup
    /// của từng node (giờ + lệch node phải trong ngày và trong cửa sổ) — lịch sai thì worker sẽ không bao giờ chạy được.
    /// </summary>
    public static IReadOnlyList<string> ValidateMerged(QuestHaOptions candidate)
    {
        var errors = Validate(FromOptions(candidate)).ToList();
        var backup = candidate.Backup;
        if (errors.Count > 0 || string.IsNullOrWhiteSpace(backup.Schedule)) return errors;
        var start = Parse(backup.WindowStart);
        var end = Parse(backup.WindowEnd);
        var slot = Parse(backup.Schedule).ToTimeSpan();
        for (var i = 0; i < Math.Max(1, candidate.Nodes.Count); i++)
        {
            var time = slot + backup.NodeOffset * i;
            if (time >= TimeSpan.FromDays(1))
            {
                errors.Add($"Slot backup node thứ {i + 1} ({nameof(backup.Schedule)} + {nameof(backup.NodeOffset)}×{i}) vượt sang ngày sau.");
                break;
            }
            var t = TimeOnly.FromTimeSpan(time);
            if (!(start < end ? t >= start && t < end : t >= start || t < end))
            {
                errors.Add($"Slot backup node thứ {i + 1} ({t:HH\\:mm}) nằm ngoài cửa sổ {backup.WindowStart}–{backup.WindowEnd}.");
                break;
            }
        }
        return errors;
    }

    /// <summary>Gộp gói đổi lúc chạy lên giá trị khởi động; thuộc tính null giữ giá trị khởi động. Không sửa <paramref name="baseline"/>.</summary>
    public static QuestHaOptions Merge(QuestHaOptions baseline, QuestHaRuntimeOptions r)
    {
        var next = baseline.Clone();
        var h = baseline.Health.Clone();
        h.CheckInterval = r.HealthCheckInterval ?? h.CheckInterval;
        h.HeartbeatStaleAfter = r.HealthHeartbeatStaleAfter ?? h.HeartbeatStaleAfter;
        h.LagWarnSeconds = r.HealthLagWarnSeconds ?? h.LagWarnSeconds;
        h.LagCriticalSeconds = r.HealthLagCriticalSeconds ?? h.LagCriticalSeconds;
        h.BackupWarnAfter = r.HealthBackupWarnAfter ?? h.BackupWarnAfter;
        h.BackupCriticalAfter = r.HealthBackupCriticalAfter ?? h.BackupCriticalAfter;
        h.BackupRequired = r.HealthBackupRequired ?? h.BackupRequired;
        h.ReconcileWarnAfter = r.HealthReconcileWarnAfter ?? h.ReconcileWarnAfter;
        h.RepeatWarningEvery = r.HealthRepeatWarningEvery ?? h.RepeatWarningEvery;
        h.MinAlertGap = r.HealthMinAlertGap ?? h.MinAlertGap;

        var j = baseline.JournalMaintenance.Clone();
        j.Interval = r.JournalInterval ?? j.Interval;
        j.Retention = r.JournalRetention ?? j.Retention;
        j.ProtectedBackupDays = r.JournalProtectedBackupDays ?? j.ProtectedBackupDays;
        j.BatchSize = r.JournalBatchSize ?? j.BatchSize;
        j.MaxRowsPerRun = r.JournalMaxRowsPerRun ?? j.MaxRowsPerRun;
        j.MaxRunDuration = r.JournalMaxRunDuration ?? j.MaxRunDuration;
        j.AlertThrottle = r.JournalAlertThrottle ?? j.AlertThrottle;
        j.WarnPendingRows = r.JournalWarnPendingRows ?? j.WarnPendingRows;
        j.CriticalPendingRows = r.JournalCriticalPendingRows ?? j.CriticalPendingRows;
        j.WarnPendingAge = r.JournalWarnPendingAge ?? j.WarnPendingAge;
        j.CriticalPendingAge = r.JournalCriticalPendingAge ?? j.CriticalPendingAge;
        j.WarnJournalBytes = r.JournalWarnJournalBytes ?? j.WarnJournalBytes;
        j.CriticalJournalBytes = r.JournalCriticalJournalBytes ?? j.CriticalJournalBytes;
        j.DryRun = r.JournalDryRun ?? j.DryRun;

        var b = baseline.Backup.Clone();
        b.Schedule = r.BackupSchedule ?? b.Schedule;
        b.WeeklyDay = r.BackupWeeklyDay ?? b.WeeklyDay;
        b.WindowStart = r.BackupWindowStart ?? b.WindowStart;
        b.WindowEnd = r.BackupWindowEnd ?? b.WindowEnd;
        b.NodeOffset = r.BackupNodeOffset ?? b.NodeOffset;
        b.MaxAttempts = r.BackupMaxAttempts ?? b.MaxAttempts;
        b.RetryDelay = r.BackupRetryDelay ?? b.RetryDelay;
        b.DailyCopies = r.BackupDailyCopies ?? b.DailyCopies;
        b.WeeklyCopies = r.BackupWeeklyCopies ?? b.WeeklyCopies;
        b.ManualCopies = r.BackupManualCopies ?? b.ManualCopies;
        b.MaxStoppedDuration = r.BackupMaxStoppedDuration ?? b.MaxStoppedDuration;
        b.MinThroughputBytesPerSecond = r.BackupMinThroughputBytesPerSecond ?? b.MinThroughputBytesPerSecond;
        b.MinimumFreeBytes = r.BackupMinimumFreeBytes ?? b.MinimumFreeBytes;
        b.DryRun = r.BackupDryRun ?? b.DryRun;

        var router = baseline.Router.Clone();
        router.CoverageWait = r.RouterCoverageWait ?? router.CoverageWait;
        router.FailbackStableFor = r.RouterFailbackStableFor ?? router.FailbackStableFor;
        router.BreakerCooldown = r.RouterBreakerCooldown ?? router.BreakerCooldown;

        var e = baseline.Export.Clone();
        e.Schedule = r.ExportSchedule ?? e.Schedule;
        e.RetentionDays = r.ExportRetentionDays ?? e.RetentionDays;
        e.RecheckDays = r.ExportRecheckDays ?? e.RecheckDays;
        e.MaxAttempts = r.ExportMaxAttempts ?? e.MaxAttempts;
        e.RetryDelay = r.ExportRetryDelay ?? e.RetryDelay;
        e.WarnAfter = r.ExportWarnAfter ?? e.WarnAfter;
        e.CriticalAfter = r.ExportCriticalAfter ?? e.CriticalAfter;
        e.DryRun = r.ExportDryRun ?? e.DryRun;

        next.Health = h;
        next.JournalMaintenance = j;
        next.Backup = b;
        next.Router = router;
        next.Export = e;
        return next;
    }

    /// <summary>Đọc đủ bộ tham số vận hành đang có trong <paramref name="o"/> (mọi thuộc tính khác null).</summary>
    public static QuestHaRuntimeOptions FromOptions(QuestHaOptions o) => new()
    {
        HealthCheckInterval = o.Health.CheckInterval,
        HealthHeartbeatStaleAfter = o.Health.HeartbeatStaleAfter,
        HealthLagWarnSeconds = o.Health.LagWarnSeconds,
        HealthLagCriticalSeconds = o.Health.LagCriticalSeconds,
        HealthBackupWarnAfter = o.Health.BackupWarnAfter,
        HealthBackupCriticalAfter = o.Health.BackupCriticalAfter,
        HealthBackupRequired = o.Health.BackupRequired,
        HealthReconcileWarnAfter = o.Health.ReconcileWarnAfter,
        HealthRepeatWarningEvery = o.Health.RepeatWarningEvery,
        HealthMinAlertGap = o.Health.MinAlertGap,
        JournalInterval = o.JournalMaintenance.Interval,
        JournalRetention = o.JournalMaintenance.Retention,
        JournalProtectedBackupDays = o.JournalMaintenance.ProtectedBackupDays,
        JournalBatchSize = o.JournalMaintenance.BatchSize,
        JournalMaxRowsPerRun = o.JournalMaintenance.MaxRowsPerRun,
        JournalMaxRunDuration = o.JournalMaintenance.MaxRunDuration,
        JournalAlertThrottle = o.JournalMaintenance.AlertThrottle,
        JournalWarnPendingRows = o.JournalMaintenance.WarnPendingRows,
        JournalCriticalPendingRows = o.JournalMaintenance.CriticalPendingRows,
        JournalWarnPendingAge = o.JournalMaintenance.WarnPendingAge,
        JournalCriticalPendingAge = o.JournalMaintenance.CriticalPendingAge,
        JournalWarnJournalBytes = o.JournalMaintenance.WarnJournalBytes,
        JournalCriticalJournalBytes = o.JournalMaintenance.CriticalJournalBytes,
        JournalDryRun = o.JournalMaintenance.DryRun,
        BackupSchedule = o.Backup.Schedule ?? "",
        BackupWeeklyDay = o.Backup.WeeklyDay,
        BackupWindowStart = o.Backup.WindowStart,
        BackupWindowEnd = o.Backup.WindowEnd,
        BackupNodeOffset = o.Backup.NodeOffset,
        BackupMaxAttempts = o.Backup.MaxAttempts,
        BackupRetryDelay = o.Backup.RetryDelay,
        BackupDailyCopies = o.Backup.DailyCopies,
        BackupWeeklyCopies = o.Backup.WeeklyCopies,
        BackupManualCopies = o.Backup.ManualCopies,
        BackupMaxStoppedDuration = o.Backup.MaxStoppedDuration,
        BackupMinThroughputBytesPerSecond = o.Backup.MinThroughputBytesPerSecond,
        BackupMinimumFreeBytes = o.Backup.MinimumFreeBytes,
        BackupDryRun = o.Backup.DryRun,
        RouterCoverageWait = o.Router.CoverageWait,
        RouterFailbackStableFor = o.Router.FailbackStableFor,
        RouterBreakerCooldown = o.Router.BreakerCooldown,
        ExportSchedule = o.Export.Schedule,
        ExportRetentionDays = o.Export.RetentionDays,
        ExportRecheckDays = o.Export.RecheckDays,
        ExportMaxAttempts = o.Export.MaxAttempts,
        ExportRetryDelay = o.Export.RetryDelay,
        ExportWarnAfter = o.Export.WarnAfter,
        ExportCriticalAfter = o.Export.CriticalAfter,
        ExportDryRun = o.Export.DryRun,
    };

    /// <summary>Danh sách <c>Tên: cũ → mới</c> của tham số khác nhau giữa hai bộ (các tham số này không có bí mật).</summary>
    public static IReadOnlyList<string> Diff(QuestHaRuntimeOptions before, QuestHaRuntimeOptions after) =>
        typeof(QuestHaRuntimeOptions).GetProperties()
            .Select(p => (p.Name, Old: Format(p.GetValue(before)), New: Format(p.GetValue(after))))
            .Where(x => x.Old != x.New)
            .Select(x => $"{x.Name}: {x.Old} → {x.New}")
            .ToList();

    private static string Format(object? value) => value switch
    {
        null => "(mặc định)",
        string s => s.Length == 0 ? "(rỗng)" : s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static TimeSpan Sec(int s) => TimeSpan.FromSeconds(s);
    private static TimeSpan Hours(int h) => TimeSpan.FromHours(h);
    private static TimeSpan Days(int d) => TimeSpan.FromDays(d);

    private static bool IsTime(string value) =>
        TimeOnly.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static TimeOnly Parse(string value) => TimeOnly.ParseExact(value, TimeFormat, CultureInfo.InvariantCulture);

    private static void Range<T>(List<string> errors, string name, T? value, T min, T max) where T : struct, IComparable<T>
    {
        if (value is { } v && (v.CompareTo(min) < 0 || v.CompareTo(max) > 0))
            errors.Add($"{name} = {Format(v)} ngoài biên [{Format(min)}, {Format(max)}].");
    }

    private static void Below<T>(List<string> errors, string warnName, T? warn, string criticalName, T? critical) where T : struct, IComparable<T>
    {
        if (warn is { } w && critical is { } c && w.CompareTo(c) >= 0)
            errors.Add($"{warnName} ({Format(w)}) phải nhỏ hơn {criticalName} ({Format(c)}).");
    }
}
