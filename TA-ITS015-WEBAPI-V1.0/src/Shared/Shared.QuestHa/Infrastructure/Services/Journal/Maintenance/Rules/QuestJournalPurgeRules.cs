namespace Shared.QuestHa.Infrastructure.Services.Journal.Maintenance.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật thuần tính mốc dọn, giữ cả cửa sổ rewind và mọi backup Verified còn trong catalog.
/// Created date: 28/10/2026
/// </summary>
internal static class QuestJournalPurgeRules
{
    /// <summary>
    /// Thiếu node/cursor/backup thì cấm xoá; lấy MIN(SafeSeq, LastSeq, mọi watermark Verified).
    /// <paramref name="exportCountsAsBackup"/> (Export bật): bản xuất Verified cũng là "có backup"; mốc chỉ theo cursor và luật từng
    /// dòng (<see cref="IsCovered"/>) quyết định — dòng chưa nằm trong backup lạnh lẫn bản xuất thì giữ.
    /// <paramref name="allowWithoutVerifiedBackup"/> chỉ có nghĩa khi Export tắt: khi Export bật, cờ này KHÔNG tạo coverage giả (mốc lạnh
    /// vẫn −1 nếu không có backup lạnh), mỗi dòng vẫn phải nằm trong bản xuất Verified hoặc backup lạnh Verified.
    /// </summary>
    public static JournalPurgeBoundaryOutput Calculate(IReadOnlyList<NodeCursorOutput?> cursors, DateTime sqlNowUtc,
        TimeSpan retention, long? oldestVerifiedWatermark, bool allowWithoutVerifiedBackup = false,
        bool exportCountsAsBackup = false, bool hasVerifiedExport = false)
    {
        if (retention <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(retention));
        var cutoff = sqlNowUtc - retention;
        if (cursors.Count == 0) return new(null, cutoff, "NoAssignedNodes");
        if (cursors.Any(c => c is null)) return new(null, cutoff, "MissingAssignedNodeCursor");
        var anyBackup = oldestVerifiedWatermark is not null || exportCountsAsBackup && hasVerifiedExport;
        if (!anyBackup && (exportCountsAsBackup || !allowWithoutVerifiedBackup))
            return new(null, cutoff, "NoVerifiedBackup: restore-replay chưa được bảo vệ");
        var safe = cursors.Min(c => Math.Min(c!.LastSeq, c.SafeSeq));
        if (safe < 0) return new(null, cutoff, "InvalidNegativeBoundary");
        if (!exportCountsAsBackup)
        {
            var max = Math.Min(safe, oldestVerifiedWatermark ?? long.MaxValue);
            return max < 0 ? new(null, cutoff, "InvalidNegativeBoundary") : new(max, cutoff, null);
        }
        // Không có backup lạnh ⇒ −1: chỉ dòng được bản xuất Verified bao mới xoá (cờ allow không nới điều kiện này).
        return new(safe, cutoff, null, oldestVerifiedWatermark ?? -1);
    }

    /// <summary>Quarantine mở luôn chặn; dòng đúng biên retention vẫn được giữ.</summary>
    public static bool IsEligible(long seq, DateTime createdUtc, bool openQuarantine, JournalPurgeBoundaryOutput boundary) =>
        boundary.MaxSeq is { } max && seq <= max && createdUtc < boundary.RetentionBeforeUtc && !openQuarantine;

    /// <summary>
    /// Luật từng dòng khi Export được tính như backup (SQL <c>PurgeReadyRowsExport</c> làm đúng điều này): dòng chỉ được xoá khi đã nằm
    /// trong backup lạnh Verified (Seq ≤ <see cref="JournalPurgeBoundaryOutput.ColdBackupMaxSeq"/>) HOẶC trong bản xuất Verified chưa retire
    /// của cùng bảng có khoảng UTC [FromUtc, ToUtc) chứa DesignatedTs (= ngày giờ server của dòng) và WatermarkSeq ≥ Seq.
    /// Dòng của ngày chưa xuất (hôm nay) hoặc đến muộn sau watermark ⇒ giữ.
    /// </summary>
    public static bool IsCovered(long seq, string table, DateTime designatedTsUtc, JournalPurgeBoundaryOutput boundary,
        IReadOnlyList<QuestExportRecordOutput> exports)
    {
        if (boundary.ColdBackupMaxSeq is not { } cold) return true;
        if (seq <= cold) return true;
        return exports.Any(e => e.Verified && e.RetiredUtc is null && string.Equals(e.TableName, table, StringComparison.OrdinalIgnoreCase) &&
            designatedTsUtc >= e.FromUtc && designatedTsUtc < e.ToUtc && e.WatermarkSeq >= seq);
    }
}
