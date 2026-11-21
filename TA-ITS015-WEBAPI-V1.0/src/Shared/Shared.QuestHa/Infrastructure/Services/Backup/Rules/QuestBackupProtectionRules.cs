namespace Shared.QuestHa.Infrastructure.Services.Backup.Rules;

/// <summary>
/// Author: KienLT
/// Description: Tập bảo vệ journal, theo từng node, không bỏ bản mới nhất.
/// Created date: 28/10/2026
/// </summary>
internal static class QuestBackupProtectionRules
{
    /// <summary>Suy retire theo tuổi, luôn bảo vệ bản Verified mới nhất của từng node.</summary>
    public static IReadOnlyList<BackupRecordOutput> Apply(IReadOnlyList<BackupRecordOutput> backups, DateTime nowUtc, int days)
    {
        var latest = backups.Where(b => b.Verified).GroupBy(b => b.Node, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(b => b.CreatedUtc).ThenByDescending(b => b.BackupId.ToString("D"), StringComparer.Ordinal).First().BackupId).ToHashSet();
        return backups.Select(b => latest.Contains(b.BackupId) ? b with { RetiredUtc = null } :
            b.Verified && b.RetiredUtc is null && b.CreatedUtc < nowUtc.AddDays(-days)
                ? b with { RetiredUtc = b.CreatedUtc.AddDays(days) } : b).ToArray();
    }

    /// <summary>Mốc thấp nhất của tập bảo vệ trên các node còn nghĩa vụ journal.</summary>
    public static long? MinimumWatermark(IReadOnlyList<BackupRecordOutput> backups, DateTime nowUtc, int days, IReadOnlySet<string>? activeNodes = null) =>
        Apply(backups, nowUtc, days).Where(b => b.Verified && b.RetiredUtc is null && (activeNodes is null || activeNodes.Contains(b.Node)))
            .Select(b => (long?)b.WatermarkSeq).Min();
}
