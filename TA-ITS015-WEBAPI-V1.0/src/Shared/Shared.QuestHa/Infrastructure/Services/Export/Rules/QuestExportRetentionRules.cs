namespace Shared.QuestHa.Infrastructure.Services.Export.Rules;

/// <summary>
/// Author: KienLT
/// Description: Chọn thư mục ngày hết hạn giữ: ngày giờ server cũ hơn RetentionDays so với hôm nay. KHÔNG BAO GIỜ chọn ngày Verified mới
/// nhất của mỗi bảng — bản đó là căn cứ "có backup" cho dọn journal/Health; mất nó thì purge bị chặn và mất điểm khôi phục cuối.
/// Created date: 02/11/2026
/// </summary>
internal static class QuestExportRetentionRules
{
    /// <summary>
    /// (bảng, ngày) hết hạn cùng các dòng catalog CHƯA retire của nó (retire TRƯỚC khi xoá thư mục). Ngày chỉ còn dòng đã retire vẫn được
    /// trả (danh sách rỗng) để thư mục còn sót sau lần xoá lỗi trước được dọn lại.
    /// </summary>
    public static IReadOnlyList<(string Table, DateOnly Day, IReadOnlyList<QuestExportRecordOutput> Records)> Expired(
        IReadOnlyList<QuestExportRecordOutput> records, DateOnly todayLocal, int retentionDays)
    {
        if (retentionDays < 1) throw new ArgumentOutOfRangeException(nameof(retentionDays));
        var cutoff = todayLocal.AddDays(-retentionDays);
        var protectedDays = records.Where(r => r.Verified && r.RetiredUtc is null)
            .GroupBy(r => r.TableName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(r => r.DayLocal), StringComparer.OrdinalIgnoreCase);
        return records.Where(r => r.DayLocal < cutoff && !(protectedDays.TryGetValue(r.TableName, out var keep) && keep == r.DayLocal))
            .GroupBy(r => (Table: r.TableName.ToUpperInvariant(), r.DayLocal))
            .Select(g => (g.First().TableName, g.Key.DayLocal, (IReadOnlyList<QuestExportRecordOutput>)g.Where(r => r.RetiredUtc is null).ToList()))
            .OrderBy(x => x.DayLocal).ToList();
    }

    /// <summary>
    /// Thư mục <paramref name="folder"/> nằm TRONG <paramref name="root"/> (không trùng root, không ra ngoài bằng ..). Reparse point/symlink
    /// và chứng từ sở hữu kiểm ở tầng I/O trước khi ghi/xoá.
    /// </summary>
    public static bool InsideRoot(string folder, string root)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        return full.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
