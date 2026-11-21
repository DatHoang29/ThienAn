using Shared.QuestHa.Helper;
using Shared.QuestHa.Infrastructure.Services.Journal.Schema;

namespace Shared.QuestHa.Infrastructure.Services.Reconcile.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật thuần của đối soát node nguồn ↔ node đích (không I/O): chia cửa sổ phút, cắt mốc cuối theo độ trễ chờ,
/// so hai tập dòng theo khoá DEDUP + fingerprint mọi cột.
/// Created date: 27/10/2026
/// </summary>
internal static class QuestNodeCompareRules
{
    /// <summary>Mốc cuối hiệu lực = min(<paramref name="toUtc"/>, now − settle): dòng quá mới có thể còn đang trên đường nhân bản.</summary>
    public static DateTime EffectiveTo(DateTime toUtc, DateTime nowUtc, TimeSpan settle)
    {
        var limit = nowUtc - settle;
        return toUtc < limit ? toUtc : limit;
    }

    /// <summary>Các cửa sổ <c>[đầu, cuối)</c> cỡ <paramref name="size"/>; <paramref name="fromUtc"/> làm tròn xuống, cửa sổ cuối cắt theo <paramref name="toUtc"/>.</summary>
    public static IEnumerable<(DateTime From, DateTime To)> Windows(DateTime fromUtc, DateTime toUtc, TimeSpan size)
    {
        var start = new DateTime(fromUtc.Ticks - fromUtc.Ticks % size.Ticks, DateTimeKind.Utc);
        for (var w = start; w < toUtc; w += size)
        {
            var end = w + size;
            yield return (w, end < toUtc ? end : toUtc);
        }
    }

    /// <summary>
    /// So hai tập dòng của cùng một cửa sổ. <c>Missing</c>: có ở nguồn, không có ở đích; <c>Different</c>: cùng khoá, khác
    /// fingerprint; <c>Extra</c>: có ở đích, không có ở nguồn. Giả định khoá DEDUP duy nhất trong mỗi node (QuestDB bảo đảm sau khi
    /// WAL apply); khoá lặp ở nguồn chỉ tính một lần, khoá lặp ở đích thì dòng cuối thắng.
    /// </summary>
    public static (List<QuestRow> Missing, List<(QuestRow Source, QuestRow Target)> Different, List<QuestRow> Extra) Diff(
        QuestTableSpec spec, IReadOnlyList<QuestRow> source, IReadOnlyList<QuestRow> target)
    {
        var targetByKey = new Dictionary<string, QuestRow>(target.Count, StringComparer.Ordinal);
        foreach (var r in target) targetByKey[QuestPayloadHelper.KeyOf(spec, r)] = r;

        var missing = new List<QuestRow>();
        var different = new List<(QuestRow, QuestRow)>();
        var sourceKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in source)
        {
            var key = QuestPayloadHelper.KeyOf(spec, s);
            if (!sourceKeys.Add(key)) continue; // khoá lặp ở nguồn: không đếm/vá nhân đôi
            if (!targetByKey.TryGetValue(key, out var t)) { missing.Add(s); continue; }
            if (Fingerprint(spec, s) != Fingerprint(spec, t)) different.Add((s, t));
        }

        var extra = targetByKey.Where(kv => !sourceKeys.Contains(kv.Key)).Select(kv => kv.Value).ToList();
        return (missing, different, extra);
    }

    /// <summary>
    /// Fingerprint để so node ↔ node: mọi cột TRỪ cột <c>FromJournalSeq</c> — cột đó do replicator điền theo Seq của journal nên
    /// dòng vá (được đưa lại vào journal, Seq mới) luôn khác nguồn ở cột này dù dữ liệu giống hệt. Khác
    /// <see cref="QuestPayloadHelper.FingerprintOf"/> (đối soát journal ↔ node, CÓ tính journalSeq).
    /// </summary>
    public static string Fingerprint(QuestTableSpec spec, QuestRow row)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < spec.Columns.Count; i++)
        {
            if (spec.Columns[i].FromJournalSeq) continue;
            if (sb.Length > 0) sb.Append('\u001f');
            sb.Append(QuestPayloadHelper.Canon(row.Values[i]));
        }

        return sb.ToString();
    }

    /// <summary>Danh sách cột lệch giữa hai dòng cùng khoá, dạng <c>cột: nguồn=… đích=…</c> (bỏ cột journalSeq).</summary>
    public static List<string> DescribeColumns(QuestTableSpec spec, QuestRow source, QuestRow target)
    {
        var cols = new List<string>();
        for (var i = 0; i < spec.Columns.Count; i++)
        {
            if (spec.Columns[i].FromJournalSeq) continue;
            var a = QuestPayloadHelper.Canon(source.Values[i]);
            var b = QuestPayloadHelper.Canon(target.Values[i]);
            if (a != b) cols.Add($"{spec.Columns[i].Name}: nguồn={a} đích={b}");
        }

        return cols;
    }
}
