namespace Shared.QuestHa.Infrastructure.Services.Export.Rules;

/// <summary>
/// Author: KienLT
/// Description: Chọn node để xuất: node PHỤ đang Ready (không phải node router đang ưu tiên đọc) để không tranh RAM/CPU với đường đọc;
/// chỉ một node Ready ⇒ dùng node đó; không node nào Ready ⇒ hoãn. Ready = trạng thái Ready, heartbeat còn mới, không tạm dừng, WAL không
/// suspend và node có <c>HttpUrl</c>. Coverage (fence/phục hồi/quarantine/cursor/WAL) chứng nhận riêng sau khi chọn.
/// Created date: 02/11/2026
/// </summary>
internal static class QuestExportNodeRules
{
    /// <summary>
    /// Node được chọn hoặc null. <paramref name="preferredNode"/> = node router đang đọc (null ⇒ node Ready có Priority nhỏ nhất,
    /// đúng cách router chọn node ưu tiên).
    /// </summary>
    public static string? Choose(IReadOnlyList<QuestNodeOptions> nodes, IReadOnlyList<NodeStatusOutput> statuses, string? preferredNode,
        DateTime nowUtc, TimeSpan heartbeatStaleAfter)
    {
        var ready = nodes.Where(n => !string.IsNullOrWhiteSpace(n.HttpUrl) && statuses.Any(s =>
                s.Node.Equals(n.Name, StringComparison.OrdinalIgnoreCase) && IsReady(s, nowUtc, heartbeatStaleAfter)))
            .OrderBy(n => n.Priority).ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase).ToList();
        if (ready.Count == 0) return null;
        var preferred = preferredNode ?? ready[0].Name;
        // Nhiều node phụ ⇒ lấy node kém ưu tiên nhất (xa đường đọc nhất).
        var secondary = ready.LastOrDefault(n => !n.Name.Equals(preferred, StringComparison.OrdinalIgnoreCase));
        return (secondary ?? ready[0]).Name;
    }

    /// <summary>Trạng thái replicator đủ điều kiện xuất.</summary>
    public static bool IsReady(NodeStatusOutput s, DateTime nowUtc, TimeSpan heartbeatStaleAfter) =>
        s.State == QuestNodeStateEnum.Ready && !s.Paused && !s.WalSuspended && nowUtc - s.HeartbeatUtc <= heartbeatStaleAfter;
}
