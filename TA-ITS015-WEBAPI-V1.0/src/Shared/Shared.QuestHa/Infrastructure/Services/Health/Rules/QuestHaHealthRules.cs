namespace Shared.QuestHa.Infrastructure.Services.Health.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật thuần đánh giá sức khoẻ QuestDB HA từ trạng thái node trong journal, số đo journal và danh mục backup.
/// Không I/O, không đọc giờ hệ thống (giờ truyền vào) nên test trực tiếp.
/// Created date: 28/10/2026
/// </summary>
internal static class QuestHaHealthRules
{
    /// <summary>Mức cao nhất của một danh sách lý do (Ok khi rỗng).</summary>
    public static QuestHaHealthLevelEnum Worst(IEnumerable<QuestHaHealthReasonOutput> reasons) =>
        reasons.Aggregate(QuestHaHealthLevelEnum.Ok, (a, r) => r.Level > a ? r.Level : a);

    /// <summary>Kết quả khi không đọc được journal: không có số đo nào đáng tin ⇒ Critical, KHÔNG kèm thông điệp lỗi (có thể chứa chuỗi kết nối).</summary>
    public static QuestHaHealthOutput JournalUnreachable(DateTime nowUtc, string errorType) =>
        new(QuestHaHealthLevelEnum.Critical, nowUtc, 0, 0, 0, Array.Empty<NodeHealthOutput>(),
            [new(QuestHaHealthLevelEnum.Critical, "journal.unreachable", $"Không đọc được journal ({errorType}).", null)]);

    /// <summary>
    /// Đánh giá toàn bộ. <paramref name="statuses"/> = QuestNodeStatus từng node; <paramref name="backups"/> = QuestBackupCatalog.
    /// Mỗi node trong <paramref name="statuses"/> được kiểm backup riêng.
    /// </summary>
    public static QuestHaHealthOutput Evaluate(
        DateTime nowUtc,
        IReadOnlyList<string> configuredNodes,
        IReadOnlyList<NodeStatusOutput> allStatuses,
        long totalRows,
        int openQuarantine,
        IReadOnlyList<BackupRecordOutput> backups,
        QuestHaHealthOptions options,
        IReadOnlyList<QuestExportRecordOutput>? exports = null,
        ExportOptions? export = null,
        IReadOnlyList<string>? exportTables = null)
    {
        // Export bật: bản xuất chỉ thay backup lạnh cho BackupRequired khi MỌI bảng đã đăng ký có bản Verified đủ mới
        // (không quá CriticalAfter); bảng ngoài cấu hình không tính. Bảng thiếu được nêu tên trong backup.none.
        var exportEnabled = export is { Enabled: true };
        var uncovered = exportEnabled
            ? UncoveredExportTables(nowUtc, exports ?? Array.Empty<QuestExportRecordOutput>(), exportTables ?? Array.Empty<string>(), export!)
            : Array.Empty<string>();
        var exportCoversAll = exportEnabled && exports is not null && exportTables is { Count: > 0 } && uncovered.Count == 0;
        var reasons = new List<QuestHaHealthReasonOutput>();

        // Tập node lấy từ CẤU HÌNH (không từ các dòng status còn sót): node cấu hình mà chưa từng báo cáo = Critical;
        // dòng status của node đã bỏ khỏi cấu hình bị bỏ qua (chỉ Warn) thay vì làm Critical vĩnh viễn.
        var configured = new HashSet<string>(configuredNodes, StringComparer.OrdinalIgnoreCase);
        var statuses = configured.Count == 0 ? allStatuses : allStatuses.Where(s => configured.Contains(s.Node)).ToList();
        var nodes = new List<NodeHealthOutput>(statuses.Count);

        if (configured.Count == 0 && statuses.Count == 0)
            reasons.Add(new(QuestHaHealthLevelEnum.Critical, "nodes.none", "Journal chưa có trạng thái node nào (replicator chưa chạy?).", null));
        foreach (var missing in configured.Where(c => !allStatuses.Any(s => string.Equals(s.Node, c, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            reasons.Add(new(QuestHaHealthLevelEnum.Critical, "node.noStatus", $"Node {missing}: chưa có trạng thái trong journal (replicator chưa báo cáo).", missing));
        if (configured.Count > 0)
            foreach (var extra in allStatuses.Where(s => !configured.Contains(s.Node)).Select(s => s.Node).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                reasons.Add(new(QuestHaHealthLevelEnum.Warn, "node.unconfigured", $"Node {extra}: có trạng thái trong journal nhưng không nằm trong cấu hình (bỏ qua).", extra));

        foreach (var s in statuses.OrderBy(x => x.Node, StringComparer.OrdinalIgnoreCase))
        {
            var before = reasons.Count;
            var heartbeatAge = Math.Max(0, (nowUtc - s.HeartbeatUtc).TotalSeconds);
            EvaluateNode(nowUtc, s, heartbeatAge, options, reasons);
            var newest = backups.Where(b => string.Equals(b.Node, s.Node, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.CreatedUtc).FirstOrDefault();
            EvaluateBackup(nowUtc, s.Node, newest, backups, options, reasons, exportCoversAll, exportEnabled ? uncovered : null);

            var level = Worst(reasons.Skip(before));
            nodes.Add(new NodeHealthOutput(s.Node, s.State, level, s.LagSeq, s.LagSeconds, Math.Round(heartbeatAge, 1), s.WalSuspended,
                s.LastReconcileUtc, s.LastReconcileOk, newest?.CreatedUtc, newest?.Verified));
        }

        if (exportEnabled) EvaluateExport(nowUtc, exports ?? Array.Empty<QuestExportRecordOutput>(), exportTables ?? Array.Empty<string>(), export!, reasons);

        if (openQuarantine > 0)
            reasons.Add(new(QuestHaHealthLevelEnum.Warn, "journal.quarantine",
                $"{openQuarantine} dòng trong quarantine cần xử lý (lỗi dữ liệu, không tự hết).", null));

        return new QuestHaHealthOutput(Worst(reasons), nowUtc, allStatuses.Count == 0 ? 0 : allStatuses.Max(s => s.MaxSeq), totalRows, openQuarantine, nodes, reasons);
    }

    private static void EvaluateNode(DateTime now, NodeStatusOutput s, double heartbeatAge, QuestHaHealthOptions o, List<QuestHaHealthReasonOutput> reasons)
    {
        void Add(QuestHaHealthLevelEnum level, string code, string message) => reasons.Add(new(level, code, message, s.Node));

        if (heartbeatAge > o.HeartbeatStaleAfter.TotalSeconds)
            Add(QuestHaHealthLevelEnum.Critical, "node.heartbeat", $"Node {s.Node}: replicator không có nhịp sống {heartbeatAge:0} s (> {o.HeartbeatStaleAfter.TotalSeconds:0} s).");

        switch (s.State)
        {
            case QuestNodeStateEnum.Down:
                Add(QuestHaHealthLevelEnum.Critical, "node.down", $"Node {s.Node}: mất kết nối QuestDB.");
                break;
            case QuestNodeStateEnum.Fenced:
                Add(QuestHaHealthLevelEnum.Critical, "node.fenced", $"Node {s.Node}: bị rào{(string.IsNullOrWhiteSpace(s.FenceReason) ? "" : $" — {s.FenceReason}")}.");
                break;
            case QuestNodeStateEnum.CatchingUp:
                Add(QuestHaHealthLevelEnum.Warn, "node.catchingUp", $"Node {s.Node}: đang bắt kịp ({s.LagSeq} dòng, {s.LagSeconds:0} s).");
                break;
            case QuestNodeStateEnum.Unknown:
                Add(QuestHaHealthLevelEnum.Warn, "node.unknown", $"Node {s.Node}: chưa rõ trạng thái.");
                break;
        }

        if (s.WalSuspended)
            Add(QuestHaHealthLevelEnum.Critical, "node.walSuspended", $"Node {s.Node}: WAL bị SUSPEND (không ghi được).");
        if (s.Paused)
            Add(QuestHaHealthLevelEnum.Warn, "node.paused", $"Node {s.Node}: replicator đang tạm dừng.");

        if (s.LagSeconds >= o.LagCriticalSeconds)
            Add(QuestHaHealthLevelEnum.Critical, "node.lag", $"Node {s.Node}: dòng pending cũ nhất {s.LagSeconds:0} s (≥ {o.LagCriticalSeconds:0} s).");
        else if (s.LagSeconds >= o.LagWarnSeconds)
            Add(QuestHaHealthLevelEnum.Warn, "node.lag", $"Node {s.Node}: dòng pending cũ nhất {s.LagSeconds:0} s (≥ {o.LagWarnSeconds:0} s).");

        if (s.LastReconcileOk == false)
            Add(QuestHaHealthLevelEnum.Warn, "node.reconcile", $"Node {s.Node}: lần đối soát gần nhất KHÔNG đạt.");
        else if (s.LastReconcileUtc is { } at && now - at > o.ReconcileWarnAfter)
            Add(QuestHaHealthLevelEnum.Warn, "node.reconcileStale", $"Node {s.Node}: lần đối soát cuối cách đây {(now - at).TotalMinutes:0} phút.");
    }

    private static void EvaluateBackup(DateTime now, string node, BackupRecordOutput? newest, IReadOnlyList<BackupRecordOutput> all,
        QuestHaHealthOptions o, List<QuestHaHealthReasonOutput> reasons, bool exportCoversAll = false, IReadOnlyList<string>? uncovered = null)
    {
        if (newest is null)
        {
            if (o.BackupRequired && !exportCoversAll)
                reasons.Add(new(QuestHaHealthLevelEnum.Critical, "backup.none", uncovered is null
                    ? $"Node {node}: chưa có bản backup nào."
                    : $"Node {node}: chưa có backup lạnh và bản xuất Parquet chưa phủ đủ bảng — thiếu/quá hạn: {(uncovered.Count == 0 ? "(chưa đăng ký bảng nào)" :string.Join(", ", uncovered))}.", node));
            return;
        }

        // Tuổi tính trên bản ĐÃ XÁC MINH mới nhất: bản chưa xác minh không phải là bảo đảm khôi phục.
        var verified = all.Where(b => string.Equals(b.Node, node, StringComparison.OrdinalIgnoreCase) && b.Verified)
            .OrderByDescending(b => b.CreatedUtc).FirstOrDefault();
        if (verified is null)
        {
            reasons.Add(new(QuestHaHealthLevelEnum.Critical, "backup.unverified", $"Node {node}: có backup nhưng chưa bản nào được xác minh.", node));
            return;
        }

        var age = now - verified.CreatedUtc;
        if (age > o.BackupCriticalAfter)
            reasons.Add(new(QuestHaHealthLevelEnum.Critical, "backup.stale", $"Node {node}: backup xác minh mới nhất cách đây {age.TotalHours:0} giờ (> {o.BackupCriticalAfter.TotalHours:0} giờ).", node));
        else if (age > o.BackupWarnAfter)
            reasons.Add(new(QuestHaHealthLevelEnum.Warn, "backup.old", $"Node {node}: backup xác minh mới nhất cách đây {age.TotalHours:0} giờ (> {o.BackupWarnAfter.TotalHours:0} giờ).", node));
    }

    /// <summary>
    /// Độ mới của bản xuất từng bảng: lấy ngày giờ server mới nhất đã có bản Verified còn hiệu lực, tuổi = lúc LẦN ĐẦU xuất ngày đó
    /// (bản xuất lại do dữ liệu đến muộn không làm "trẻ" lại). Không có bản nào ⇒ Warn; quá WarnAfter ⇒ Warn; quá CriticalAfter ⇒ Critical.
    /// </summary>
    public static void EvaluateExport(DateTime now, IReadOnlyList<QuestExportRecordOutput> exports, IReadOnlyList<string> tables,
        ExportOptions o, List<QuestHaHealthReasonOutput> reasons)
    {
        var names = tables.Count > 0 ? tables : exports.Select(e => e.TableName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (names.Count == 0)
        {
            reasons.Add(new(QuestHaHealthLevelEnum.Warn, "export.none", "Export Parquet đã bật nhưng chưa có bản xuất Verified nào.", null));
            return;
        }
        foreach (var table in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
        {
            if (NewestExport(now, exports, table) is not { } newest)
            {
                reasons.Add(new(QuestHaHealthLevelEnum.Warn, "export.none", $"Bảng {table}: chưa có bản xuất Parquet Verified.", null));
                continue;
            }
            var (day, age) = newest;
            if (age > o.CriticalAfter)
                reasons.Add(new(QuestHaHealthLevelEnum.Critical, "export.stale", $"Bảng {table}: ngày xuất mới nhất {day:yyyy-MM-dd} xuất cách đây {age.TotalHours:0} giờ (> {o.CriticalAfter.TotalHours:0} giờ).", null));
            else if (age > o.WarnAfter)
                reasons.Add(new(QuestHaHealthLevelEnum.Warn, "export.old", $"Bảng {table}: ngày xuất mới nhất {day:yyyy-MM-dd} xuất cách đây {age.TotalHours:0} giờ (> {o.WarnAfter.TotalHours:0} giờ).", null));
        }
    }

    /// <summary>
    /// Bảng đã đăng ký mà bản xuất KHÔNG thay được backup lạnh: chưa có bản Verified còn hiệu lực, hoặc ngày mới nhất xuất quá
    /// CriticalAfter. Rỗng khi mọi bảng đều đủ (danh sách bảng rỗng cũng trả rỗng — người gọi tự coi là chưa phủ).
    /// </summary>
    public static IReadOnlyList<string> UncoveredExportTables(DateTime now, IReadOnlyList<QuestExportRecordOutput> exports,
        IReadOnlyList<string> tables, ExportOptions o) =>
        tables.Where(t => NewestExport(now, exports, t) is not { } x || x.Age > o.CriticalAfter)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();

    // Ngày giờ server mới nhất có bản Verified còn hiệu lực của bảng; tuổi = lúc LẦN ĐẦU xuất ngày đó (xuất lại không làm "trẻ" lại).
    private static (DateOnly Day, TimeSpan Age)? NewestExport(DateTime now, IReadOnlyList<QuestExportRecordOutput> exports, string table)
    {
        var mine = exports.Where(e => e.Verified && string.Equals(e.TableName, table, StringComparison.OrdinalIgnoreCase)).ToList();
        var live = mine.Where(e => e.RetiredUtc is null).ToList();
        if (live.Count == 0) return null;
        var day = live.Max(e => e.DayLocal);
        return (day, now - mine.Where(e => e.DayLocal == day).Min(e => e.CreatedUtc));
    }
}
