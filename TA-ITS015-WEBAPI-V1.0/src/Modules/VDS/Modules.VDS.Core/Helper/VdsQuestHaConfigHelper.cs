using System.Globalization;
using System.Text.RegularExpressions;
using Modules.VDS.Core.Constants;
using Shared.QuestHa.Options;
using Shared.Utility.Helpers;
using C = Modules.VDS.Core.Constants.VdsConfigConst.QuestHa;

namespace Modules.VDS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Đọc mã <c>VdsConfig</c> <c>vds_questha_*</c> thành <see cref="QuestHaRuntimeOptions"/>. Một nơi khai kiểu giá trị của
/// từng mã cho cả Worker (<c>OpConfigManager</c>) và WebAPI (<c>VdsConfigRules</c>, router đọc) để hai bên không hiểu lệch nhau.
/// Chỉ đọc ĐỊNH DẠNG; biên min/max và cặp Warn &lt; Critical do <see cref="QuestHaRuntimeOptions.Validate"/> của thư viện quyết định.
/// Thời lượng BẮT BUỘC hậu tố s/m/h/d (không hiểu số trần là phút như <see cref="DurationHelper"/>): gõ "30" khi có ý 30 giây là sai gấp 60 lần.
/// Created date: 31/10/2026
/// </summary>
public class VdsQuestHaConfigHelper
{
    /// <summary>
    /// Author: KienLT
    /// Description: Kiểu giá trị của một mã <c>vds_questha_*</c> — quyết định cách đọc chuỗi trong <c>VdsConfig.Value</c> và thông báo lỗi định dạng.
    /// Created date: 31/10/2026
    /// </summary>
    private enum ValueKind { Duration, Seconds, Flag, Int, Long, Time, Schedule, Day }

    /// <summary>
    /// Author: KienLT
    /// Description: Khai báo một mã: kiểu giá trị, cách gán vào và đọc ra thuộc tính tương ứng của <see cref="QuestHaRuntimeOptions"/>.
    /// Created date: 31/10/2026
    /// </summary>
    private sealed record Field(ValueKind Kind, Action<QuestHaRuntimeOptions, object> Set, Func<QuestHaRuntimeOptions, object?> Get);

    private static readonly Regex DurationPattern = new(@"^\d+\s*[smhd]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Giá trị lịch backup nghĩa là tắt lịch (VdsConfig không cho Value rỗng).</summary>
    public const string ScheduleOff = "off";

    private static readonly IReadOnlyDictionary<string, Field> Fields = new Dictionary<string, Field>(StringComparer.Ordinal)
    {
        [C.HealthCheckInterval] = new(ValueKind.Duration, (r, v) => r.HealthCheckInterval = (TimeSpan)v, r => r.HealthCheckInterval),
        [C.HealthHeartbeatStaleAfter] = new(ValueKind.Duration, (r, v) => r.HealthHeartbeatStaleAfter = (TimeSpan)v, r => r.HealthHeartbeatStaleAfter),
        [C.HealthLagWarnSeconds] = new(ValueKind.Seconds, (r, v) => r.HealthLagWarnSeconds = (double)v, r => r.HealthLagWarnSeconds),
        [C.HealthLagCriticalSeconds] = new(ValueKind.Seconds, (r, v) => r.HealthLagCriticalSeconds = (double)v, r => r.HealthLagCriticalSeconds),
        [C.HealthBackupWarnAfter] = new(ValueKind.Duration, (r, v) => r.HealthBackupWarnAfter = (TimeSpan)v, r => r.HealthBackupWarnAfter),
        [C.HealthBackupCriticalAfter] = new(ValueKind.Duration, (r, v) => r.HealthBackupCriticalAfter = (TimeSpan)v, r => r.HealthBackupCriticalAfter),
        [C.HealthBackupRequired] = new(ValueKind.Flag, (r, v) => r.HealthBackupRequired = (bool)v, r => r.HealthBackupRequired),
        [C.HealthReconcileWarnAfter] = new(ValueKind.Duration, (r, v) => r.HealthReconcileWarnAfter = (TimeSpan)v, r => r.HealthReconcileWarnAfter),
        [C.HealthRepeatWarningEvery] = new(ValueKind.Duration, (r, v) => r.HealthRepeatWarningEvery = (TimeSpan)v, r => r.HealthRepeatWarningEvery),
        [C.HealthMinAlertGap] = new(ValueKind.Duration, (r, v) => r.HealthMinAlertGap = (TimeSpan)v, r => r.HealthMinAlertGap),

        [C.JournalInterval] = new(ValueKind.Duration, (r, v) => r.JournalInterval = (TimeSpan)v, r => r.JournalInterval),
        [C.JournalRetention] = new(ValueKind.Duration, (r, v) => r.JournalRetention = (TimeSpan)v, r => r.JournalRetention),
        [C.JournalProtectedBackupDays] = new(ValueKind.Int, (r, v) => r.JournalProtectedBackupDays = (int)v, r => r.JournalProtectedBackupDays),
        [C.JournalBatchSize] = new(ValueKind.Int, (r, v) => r.JournalBatchSize = (int)v, r => r.JournalBatchSize),
        [C.JournalMaxRowsPerRun] = new(ValueKind.Int, (r, v) => r.JournalMaxRowsPerRun = (int)v, r => r.JournalMaxRowsPerRun),
        [C.JournalMaxRunDuration] = new(ValueKind.Duration, (r, v) => r.JournalMaxRunDuration = (TimeSpan)v, r => r.JournalMaxRunDuration),
        [C.JournalAlertThrottle] = new(ValueKind.Duration, (r, v) => r.JournalAlertThrottle = (TimeSpan)v, r => r.JournalAlertThrottle),
        [C.JournalWarnPendingRows] = new(ValueKind.Long, (r, v) => r.JournalWarnPendingRows = (long)v, r => r.JournalWarnPendingRows),
        [C.JournalCriticalPendingRows] = new(ValueKind.Long, (r, v) => r.JournalCriticalPendingRows = (long)v, r => r.JournalCriticalPendingRows),
        [C.JournalWarnPendingAge] = new(ValueKind.Duration, (r, v) => r.JournalWarnPendingAge = (TimeSpan)v, r => r.JournalWarnPendingAge),
        [C.JournalCriticalPendingAge] = new(ValueKind.Duration, (r, v) => r.JournalCriticalPendingAge = (TimeSpan)v, r => r.JournalCriticalPendingAge),
        [C.JournalWarnBytes] = new(ValueKind.Long, (r, v) => r.JournalWarnJournalBytes = (long)v, r => r.JournalWarnJournalBytes),
        [C.JournalCriticalBytes] = new(ValueKind.Long, (r, v) => r.JournalCriticalJournalBytes = (long)v, r => r.JournalCriticalJournalBytes),
        [C.JournalDryRun] = new(ValueKind.Flag, (r, v) => r.JournalDryRun = (bool)v, r => r.JournalDryRun),

        [C.BackupSchedule] = new(ValueKind.Schedule, (r, v) => r.BackupSchedule = (string)v, r => r.BackupSchedule),
        [C.BackupWeeklyDay] = new(ValueKind.Day, (r, v) => r.BackupWeeklyDay = (DayOfWeek)v, r => r.BackupWeeklyDay),
        [C.BackupWindowStart] = new(ValueKind.Time, (r, v) => r.BackupWindowStart = (string)v, r => r.BackupWindowStart),
        [C.BackupWindowEnd] = new(ValueKind.Time, (r, v) => r.BackupWindowEnd = (string)v, r => r.BackupWindowEnd),
        [C.BackupNodeOffset] = new(ValueKind.Duration, (r, v) => r.BackupNodeOffset = (TimeSpan)v, r => r.BackupNodeOffset),
        [C.BackupMaxAttempts] = new(ValueKind.Int, (r, v) => r.BackupMaxAttempts = (int)v, r => r.BackupMaxAttempts),
        [C.BackupRetryDelay] = new(ValueKind.Duration, (r, v) => r.BackupRetryDelay = (TimeSpan)v, r => r.BackupRetryDelay),
        [C.BackupDailyCopies] = new(ValueKind.Int, (r, v) => r.BackupDailyCopies = (int)v, r => r.BackupDailyCopies),
        [C.BackupWeeklyCopies] = new(ValueKind.Int, (r, v) => r.BackupWeeklyCopies = (int)v, r => r.BackupWeeklyCopies),
        [C.BackupManualCopies] = new(ValueKind.Int, (r, v) => r.BackupManualCopies = (int)v, r => r.BackupManualCopies),
        [C.BackupMaxStoppedDuration] = new(ValueKind.Duration, (r, v) => r.BackupMaxStoppedDuration = (TimeSpan)v, r => r.BackupMaxStoppedDuration),
        [C.BackupMinThroughputBytesPerSecond] = new(ValueKind.Long, (r, v) => r.BackupMinThroughputBytesPerSecond = (long)v, r => r.BackupMinThroughputBytesPerSecond),
        [C.BackupMinimumFreeBytes] = new(ValueKind.Long, (r, v) => r.BackupMinimumFreeBytes = (long)v, r => r.BackupMinimumFreeBytes),
        [C.BackupDryRun] = new(ValueKind.Flag, (r, v) => r.BackupDryRun = (bool)v, r => r.BackupDryRun),

        [C.RouterCoverageWait] = new(ValueKind.Duration, (r, v) => r.RouterCoverageWait = (TimeSpan)v, r => r.RouterCoverageWait),
        [C.RouterFailbackStableFor] = new(ValueKind.Duration, (r, v) => r.RouterFailbackStableFor = (TimeSpan)v, r => r.RouterFailbackStableFor),
        [C.RouterBreakerCooldown] = new(ValueKind.Duration, (r, v) => r.RouterBreakerCooldown = (TimeSpan)v, r => r.RouterBreakerCooldown),

        [C.ExportSchedule] = new(ValueKind.Time, (r, v) => r.ExportSchedule = (string)v, r => r.ExportSchedule),
        [C.ExportRetentionDays] = new(ValueKind.Int, (r, v) => r.ExportRetentionDays = (int)v, r => r.ExportRetentionDays),
        [C.ExportRecheckDays] = new(ValueKind.Int, (r, v) => r.ExportRecheckDays = (int)v, r => r.ExportRecheckDays),
        [C.ExportMaxAttempts] = new(ValueKind.Int, (r, v) => r.ExportMaxAttempts = (int)v, r => r.ExportMaxAttempts),
        [C.ExportRetryDelay] = new(ValueKind.Duration, (r, v) => r.ExportRetryDelay = (TimeSpan)v, r => r.ExportRetryDelay),
        [C.ExportWarnAfter] = new(ValueKind.Duration, (r, v) => r.ExportWarnAfter = (TimeSpan)v, r => r.ExportWarnAfter),
        [C.ExportCriticalAfter] = new(ValueKind.Duration, (r, v) => r.ExportCriticalAfter = (TimeSpan)v, r => r.ExportCriticalAfter),
        [C.ExportDryRun] = new(ValueKind.Flag, (r, v) => r.ExportDryRun = (bool)v, r => r.ExportDryRun),
    };

    /// <summary>Cặp ngưỡng (Warn, Critical) và cặp cửa sổ backup (đầu, cuối) phải so với nhau khi lưu.</summary>
    private static readonly (string First, string Second)[] Pairs =
    [
        (C.HealthLagWarnSeconds, C.HealthLagCriticalSeconds),
        (C.HealthBackupWarnAfter, C.HealthBackupCriticalAfter),
        (C.JournalWarnPendingRows, C.JournalCriticalPendingRows),
        (C.JournalWarnPendingAge, C.JournalCriticalPendingAge),
        (C.JournalWarnBytes, C.JournalCriticalBytes),
        (C.ExportWarnAfter, C.ExportCriticalAfter),
        (C.BackupWindowStart, C.BackupWindowEnd),
    ];

    /// <summary>Các cặp phải so với nhau (Warn ⇄ Critical, đầu ⇄ cuối cửa sổ backup).</summary>
    public static IReadOnlyList<(string First, string Second)> PairCodes => Pairs;

    /// <summary>Mọi mã được hỗ trợ.</summary>
    public static IReadOnlyCollection<string> Codes => (IReadOnlyCollection<string>)Fields.Keys;

    /// <summary>Mã có tiền tố <c>vds_questha_</c> (kể cả mã chưa hỗ trợ).</summary>
    public static bool IsQuestHaCode(string? code) => code?.StartsWith(C.Prefix, StringComparison.Ordinal) == true;

    /// <summary>Mã có trong danh mục hỗ trợ.</summary>
    public static bool IsKnownCode(string? code) => code is not null && Fields.ContainsKey(code);

    /// <summary>Mã là thời lượng (bắt buộc hậu tố s/m/h/d).</summary>
    public static bool IsDurationCode(string? code) =>
        code is not null && Fields.TryGetValue(code, out var f) && f.Kind is ValueKind.Duration or ValueKind.Seconds;

    /// <summary>Giá trị có dạng số nguyên + hậu tố s/m/h/d.</summary>
    public static bool HasDurationSuffix(string? raw) => DurationPattern.IsMatch(raw?.Trim() ?? "");

    /// <summary>Mã còn lại của cặp phải so (Warn ⇄ Critical, đầu ⇄ cuối cửa sổ); <c>null</c> nếu mã không thuộc cặp nào.</summary>
    public static string? PeerCode(string? code)
    {
        foreach (var (first, second) in Pairs)
        {
            if (code == first) return second;
            if (code == second) return first;
        }
        return null;
    }

    /// <summary>
    /// Đọc một giá trị vào <paramref name="target"/>. Trả <c>null</c> khi đọc được; ngược lại là lý do (tiếng Việt, cho log).
    /// Không kiểm biên — gọi <see cref="QuestHaRuntimeOptions.Validate"/> sau khi đọc.
    /// </summary>
    public static string? TryRead(QuestHaRuntimeOptions target, string code, string? raw)
    {
        if (!Fields.TryGetValue(code, out var field)) return "mã không được hỗ trợ";
        var text = (raw ?? "").Trim();
        object? value = field.Kind switch
        {
            ValueKind.Duration => ReadSeconds(text) is int s ? TimeSpan.FromSeconds(s) : null,
            ValueKind.Seconds => ReadSeconds(text) is int s ? (double)s : null,
            ValueKind.Flag => text.ToLowerInvariant() switch
            {
                "1" or "true" or "on" or "yes" => true,
                "0" or "false" or "off" or "no" => false,
                _ => null,
            },
            ValueKind.Int => int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var i) ? i : null,
            ValueKind.Long => long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l) ? l : null,
            ValueKind.Time => IsTime(text) ? text : null,
            ValueKind.Schedule => text.Length == 0 || text.Equals(ScheduleOff, StringComparison.OrdinalIgnoreCase) ? "" : IsTime(text) ? text : null,
            // Chỉ nhận tên ngày (Sunday…): số trần dễ lệch quy ước 0=Chủ nhật hay 1=Thứ hai.
            ValueKind.Day => !text.Any(char.IsDigit) && Enum.TryParse<DayOfWeek>(text, true, out var d) && Enum.IsDefined(d) ? d : null,
            _ => null,
        };
        if (value is null)
            return field.Kind switch
            {
                ValueKind.Duration or ValueKind.Seconds when !HasDurationSuffix(text) => "thời lượng phải có hậu tố s/m/h/d (vd 30s, 5m)",
                ValueKind.Duration or ValueKind.Seconds => "thời lượng vượt 30 ngày",
                ValueKind.Flag => "phải là 1/0",
                ValueKind.Int or ValueKind.Long => "phải là số nguyên",
                ValueKind.Time => "phải là giờ HH:mm",
                ValueKind.Schedule => "phải là giờ HH:mm hoặc off",
                _ => "phải là tên ngày trong tuần (Sunday…Saturday)",
            };
        field.Set(target, value);
        return null;
    }

    /// <summary>
    /// Dựng gói từ các dòng <c>VdsConfig</c> (mã → giá trị, mã khác tiền tố bị bỏ qua). Mã thiếu ⇒ thuộc tính null (giá trị appsettings).
    /// Mỗi mã lạ/giá trị sai/ngoài biên thêm một dòng vào <paramref name="errors"/>; bên gọi phải từ chối cả gói khi có lỗi.
    /// </summary>
    public static QuestHaRuntimeOptions Build(IReadOnlyDictionary<string, string?> byCode, ICollection<string> errors)
    {
        var runtime = new QuestHaRuntimeOptions();
        foreach (var (code, raw) in byCode)
        {
            if (!IsQuestHaCode(code)) continue;
            if (TryRead(runtime, code, raw) is { } reason) errors.Add($"{code} = '{raw}': {reason}");
        }
        foreach (var error in runtime.Validate()) errors.Add(error);
        return runtime;
    }

    /// <summary>
    /// Cặp <paramref name="first"/>/<paramref name="second"/> trong bộ <paramref name="effective"/> vi phạm luật cặp của thư viện
    /// (Warn ≥ Critical, đầu = cuối cửa sổ). Chỉ chép hai giá trị sang gói rỗng rồi gọi <see cref="QuestHaRuntimeOptions.Validate"/> — không tự viết lại luật.
    /// </summary>
    public static bool PairViolated(QuestHaRuntimeOptions effective, string first, string second)
    {
        if (!Fields.TryGetValue(first, out var a) || !Fields.TryGetValue(second, out var b)) return false;
        if (a.Get(effective) is not { } va || b.Get(effective) is not { } vb) return false;
        var pair = new QuestHaRuntimeOptions();
        a.Set(pair, va);
        b.Set(pair, vb);
        return pair.Validate().Count > 0;
    }

    /// <summary>
    /// Giá trị khởi động dùng khi kiểm bộ hiệu lực lúc lưu: mặc định trong code (trùng section <c>QuestHa</c> của appsettings Worker) và
    /// <paramref name="nodeCount"/> node (tối thiểu 2 — cụm HA luôn có node chính + node phụ) để kiểm slot lịch backup của từng node.
    /// </summary>
    public static QuestHaOptions SaveBaseline(int nodeCount) => new()
    {
        Nodes = Enumerable.Range(1, Math.Max(2, nodeCount)).Select(i => new QuestNodeOptions { Name = "node" + i }).ToList(),
    };

    /// <summary>
    /// Tenant sở hữu tham số QuestDB HA của host: <paramref name="configured"/> (khoá <see cref="C.SettingsTenantConfigKey"/>) nếu có,
    /// không thì <paramref name="hostTenant"/> (Worker: <c>Service:TenantId</c>), cuối cùng <see cref="C.DefaultSettingsTenantId"/>.
    /// </summary>
    public static string SettingsTenant(string? configured, string? hostTenant = null) =>
        !string.IsNullOrWhiteSpace(configured) ? configured.Trim()
        : !string.IsNullOrWhiteSpace(hostTenant) ? hostTenant.Trim()
        : C.DefaultSettingsTenantId;

    private static int? ReadSeconds(string text) => HasDurationSuffix(text) ? BaseHelper.DurationHp.TryParseSeconds(text) : null;

    private static bool IsTime(string text) =>
        TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
}
