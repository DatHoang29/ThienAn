namespace Modules.TMS.Infrastructure.Services.Zone.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật thuần khi xoá zone: xoá zone cha thì xoá luôn MỌI zone con cháu. Trước đây chỉ xoá đúng dòng
/// được chọn ⇒ zone con còn sống với Pid trỏ tới cha đã xoá: cây GetTree (dựng từ gốc "0") giấu chúng, nhưng danh
/// sách phẳng (tab Tốc độ, service VDS) vẫn thấy và vẫn dùng.
/// Created date: 26/10/2026
/// </summary>
internal static class TmsZoneDeleteRules
{
    /// <summary>
    /// Các zone được chọn + mọi con cháu (đi theo Pid trong <paramref name="zones"/>). Chống vòng lặp Pid; giữ thứ
    /// tự: zone được chọn trước, con cháu sau. So sánh ID không phân biệt hoa/thường.
    /// </summary>
    internal static List<string> WithDescendants(IEnumerable<string> selectedIds, IEnumerable<(string Id, string? Pid)> zones)
    {
        var children = zones
            .Where(z => !string.IsNullOrWhiteSpace(z.Id) && !string.IsNullOrWhiteSpace(z.Pid))
            .GroupBy(z => z.Pid!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(z => z.Id.Trim()).ToList(), StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        var queue = new Queue<string>();
        foreach (var id in selectedIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
            if (seen.Add(id)) { result.Add(id); queue.Enqueue(id); }

        while (queue.Count > 0)
        {
            if (!children.TryGetValue(queue.Dequeue(), out var kids)) continue;
            foreach (var kid in kids)
                if (seen.Add(kid)) { result.Add(kid); queue.Enqueue(kid); }
        }
        return result;
    }
}
