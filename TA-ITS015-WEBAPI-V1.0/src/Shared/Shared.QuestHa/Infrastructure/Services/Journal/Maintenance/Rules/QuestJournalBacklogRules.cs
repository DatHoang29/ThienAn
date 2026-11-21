namespace Shared.QuestHa.Infrastructure.Services.Journal.Maintenance.Rules;

/// <summary>
/// Author: KienLT
/// Description: Phân mức thuần theo ngưỡng bao gồm biên; pending xét từng node, không cộng trùng nghĩa vụ giao.
/// Created date: 28/10/2026
/// </summary>
internal static class QuestJournalBacklogRules
{
    /// <summary>Trả số đo kèm mức lớn nhất và mọi lý do; quarantine mở tối thiểu Warn.</summary>
    public static JournalBacklogOutput Evaluate(JournalBacklogOutput measurement, JournalMaintenanceOptions options)
    {
        options.Validate();
        var level = JournalBacklogLevelEnum.Ok;
        var reasons = new List<string>();
        void Check(bool critical, bool warn, string reason)
        {
            if (!critical && !warn) return;
            var next = critical ? JournalBacklogLevelEnum.Critical : JournalBacklogLevelEnum.Warn;
            if (next > level) level = next;
            reasons.Add($"{next}: {reason}");
        }
        foreach (var n in measurement.Nodes)
        {
            Check(n.PendingRows >= options.CriticalPendingRows, n.PendingRows >= options.WarnPendingRows, $"{n.Node}: PendingRows={n.PendingRowsDisplay}");
            Check(n.OldestPendingAge >= options.CriticalPendingAge, n.OldestPendingAge >= options.WarnPendingAge, $"{n.Node}: OldestPendingAge={n.OldestPendingAge}");
            Check(false, !n.CursorExists, $"{n.Node}: MissingCursor");
        }
        Check(measurement.JournalBytes >= options.CriticalJournalBytes, measurement.JournalBytes >= options.WarnJournalBytes,
            $"JournalBytes={measurement.JournalBytes}");
        Check(false, measurement.OpenQuarantineCount > 0, $"OpenQuarantine={measurement.OpenQuarantineCount}");
        return measurement with { Level = level, Reasons = reasons };
    }
}
