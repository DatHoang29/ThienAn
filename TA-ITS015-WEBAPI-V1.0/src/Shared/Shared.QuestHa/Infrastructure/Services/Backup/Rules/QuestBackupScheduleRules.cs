using System.Globalization;

namespace Shared.QuestHa.Infrastructure.Services.Backup.Rules;

/// <summary>
/// Author: KienLT
/// Description: Slot giờ server, lệch giờ mỗi node và cửa sổ kể cả qua nửa đêm.
/// Created date: 28/10/2026
/// </summary>
internal static class QuestBackupScheduleRules
{
    /// <summary>Kiểm giờ local thuộc cửa sổ vận hành.</summary>
    public static bool InWindow(DateTime local, BackupOptions options)
    {
        var time = TimeOnly.FromDateTime(local);
        var start = TimeOnly.ParseExact(options.WindowStart, "HH:mm", CultureInfo.InvariantCulture);
        var end = TimeOnly.ParseExact(options.WindowEnd, "HH:mm", CultureInfo.InvariantCulture);
        return start < end ? time >= start && time < end : time >= start || time < end;
    }

    /// <summary>Slot kế tiếp theo giờ local và offset node.</summary>
    public static DateTime Next(DateTime afterLocal, int nodeIndex, BackupOptions options)
    {
        options.Validate();
        if (string.IsNullOrWhiteSpace(options.Schedule)) throw new InvalidOperationException("Lịch backup trống.");
        var time = TimeOnly.ParseExact(options.Schedule, "HH:mm", CultureInfo.InvariantCulture).ToTimeSpan() + options.NodeOffset * nodeIndex;
        if (time >= TimeSpan.FromDays(1)) throw new ArgumentException("Slot node vượt ngày lịch.");
        for (var day = 0; day < 9; day++)
        {
            var candidate = afterLocal.Date.AddDays(day) + time;
            if (candidate > afterLocal && InWindow(candidate, options)) return candidate;
        }
        throw new ArgumentException("Slot backup nằm ngoài cửa sổ cho phép.");
    }

    /// <summary>Kiểm peer Ready không rào hoặc pause.</summary>
    public static bool OtherReady(string node, IReadOnlyList<NodeStatusOutput> statuses) =>
        statuses.Any(s => !s.Node.Equals(node, StringComparison.OrdinalIgnoreCase) &&
            s.State == QuestNodeStateEnum.Ready && !s.Paused && s.FenceReason is null && !s.WalSuspended);

    /// <summary>Slot đến hạn trong cùng cửa sổ, kể cả qua ngày.</summary>
    public static DateTime? Due(DateTime nowLocal, int nodeIndex, BackupOptions options)
    {
        var first = Next(nowLocal.Date.AddDays(-1).AddTicks(-1), nodeIndex, options);
        var second = Next(first, nodeIndex, options);
        return new[] { first, second }.Where(d => d <= nowLocal && WindowDate(d) == WindowDate(nowLocal)).Select(d => (DateTime?)d).Max();

        DateTime WindowDate(DateTime value)
        {
            var start = TimeOnly.ParseExact(options.WindowStart, "HH:mm", CultureInfo.InvariantCulture);
            var end = TimeOnly.ParseExact(options.WindowEnd, "HH:mm", CultureInfo.InvariantCulture);
            return start > end && TimeOnly.FromDateTime(value) < end ? value.Date.AddDays(-1) : value.Date;
        }
    }
}
