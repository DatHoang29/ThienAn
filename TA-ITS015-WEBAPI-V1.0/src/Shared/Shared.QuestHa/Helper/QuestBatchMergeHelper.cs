using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Shared.QuestHa.Helper;

/// <summary>
/// Author: KienLT
/// Description: Gộp các dòng cùng DEDUP key trong 1 lô — giữ bản có Seq lớn nhất (bản cuối).
/// Created date: 09/10/2026
/// </summary>
internal class QuestBatchMergeHelper
{
    protected QuestBatchMergeHelper() { }

    /// <summary>Đầu vào theo thứ tự bất kỳ; đầu ra sắp theo Seq tăng dần, mỗi key 1 dòng.</summary>
    public static IReadOnlyList<QuestRow> MergeLastPerKey(QuestTableSpec spec, IEnumerable<QuestRow> rows)
    {
        var last = new Dictionary<string, QuestRow>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var key = QuestPayloadHelper.KeyOf(spec, row);
            if (!last.TryGetValue(key, out var cur) || row.Seq >= cur.Seq) last[key] = row;
        }

        return last.Values.OrderBy(r => r.Seq).ToList();
    }
}
