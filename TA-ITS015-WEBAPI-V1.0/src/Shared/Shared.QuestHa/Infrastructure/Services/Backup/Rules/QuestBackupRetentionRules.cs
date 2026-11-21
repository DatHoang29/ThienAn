namespace Shared.QuestHa.Infrastructure.Services.Backup.Rules;

/// <summary>
/// Author: KienLT
/// Description: Giữ 7 daily cộng 4 weekly mỗi node và bản Verified mới nhất.
/// Created date: 28/10/2026
/// </summary>
internal static class QuestBackupRetentionRules
{
    /// <summary>Giữ ngày/tuần local phân biệt; bản gọi tay có quota riêng, không đẩy mất lịch sử lịch.</summary>
    public static HashSet<Guid> Keep(IReadOnlyList<BackupRecordOutput> backups, int daily, int weekly, Func<BackupRecordOutput, bool> isWeekly,
        Func<BackupRecordOutput, bool>? isManual = null, int manual = 7)
    {
        // snapshot phân loại đúng một lần; lỗi I/O phải huỷ retention trước khi xoá.
        var kinds = backups.ToDictionary(b => b.BackupId, b => (Weekly: isWeekly(b), Manual: isManual?.Invoke(b) == true));
        var keep = new HashSet<Guid>();
        foreach (var group in backups.GroupBy(b => b.Node, StringComparer.OrdinalIgnoreCase))
        {
            var ordered = group.OrderByDescending(b => b.CreatedUtc).ThenByDescending(b => b.BackupId.ToString("D"), StringComparer.Ordinal).ToArray();
            keep.UnionWith(ordered.Where(b => b.Verified && !kinds[b.BackupId].Manual && !kinds[b.BackupId].Weekly)
                .GroupBy(b => b.CreatedUtc.ToLocalTime().Date).Take(daily).Select(g => g.First().BackupId));
            keep.UnionWith(ordered.Where(b => b.Verified && !kinds[b.BackupId].Manual && kinds[b.BackupId].Weekly)
                .GroupBy(b => Week(b.CreatedUtc.ToLocalTime().Date)).Take(weekly).Select(g => g.First().BackupId));
            keep.UnionWith(ordered.Where(b => b.Verified && kinds[b.BackupId].Manual).Take(manual).Select(b => b.BackupId));
            if (ordered.FirstOrDefault(b => b.Verified) is { } latest) keep.Add(latest.BackupId);
        }
        return keep;
    }

    private static DateTime Week(DateTime day) => day.AddDays(-((int)day.DayOfWeek + 6) % 7);
}
