using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Infrastructure.Services.Reconcile;

/// <summary>
/// Author: KienLT
/// Description: Đối soát journal ↔ từng node theo PHÚT (môi trường lab rút chu kỳ từ giờ xuống phút).
///
/// <para>Kỳ vọng CHUNG cho mọi node = với mỗi DEDUP key, dòng journal (chưa Superseded, không quarantine ở TẤT CẢ
/// node) có Seq lớn nhất. Quarantine chỉ ở một số node KHÔNG miễn dòng đó cho node ⇒ node thiếu là Missing, NodeOk=false
/// dòng thấy đúng trên node thì quarantine của node đó được resolve tự động. So <c>count</c> + hash SHA-256 trên (key, fingerprint mọi cột, gồm
/// journalSeq). Phút có dòng Seq &gt; cursor của node ⇒ <see cref="WindowStatusEnum.Pending"/> (chưa kết luận).</para>
///
/// <para>Lệch (Missing/Different) ⇒ có thể <see cref="ReplayWindowAsync"/> (hạ cursor về MinSeq − 1 của phút).
/// Dòng thừa (Extra — có ở node, không có trong journal, vd INSERT tay) không sửa được bằng phát lại ⇒ chỉ
/// báo, không tự phát lại (tránh vòng lặp vô hạn). Lab: tính hash phía client; production nên tính phía
/// server theo giờ để không kéo dữ liệu về.</para>
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestReconcilerService
{
    private readonly IJournalStore _store;
    private readonly IReadOnlyList<IQuestNodeClient> _nodes;
    private readonly ReconcilerOptions _options;
    private readonly TimeProvider _clock;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ReconcileReportOutput? _last;
    // cờ per-node là AND mọi bảng, kèm bằng chứng thế hệ/mốc đọc của từng bảng.
    private readonly string[] _tableNames;
    private readonly Dictionary<string, Dictionary<string, ReconcileEvidence>> _results = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Khởi tạo bộ đối soát journal ↔ node cho các client/bảng của cluster.</summary>
    public QuestReconcilerService(
        IJournalStore store,
        IEnumerable<IQuestNodeClient> nodes,
        ReconcilerOptions options,
        TimeProvider? clock = null,
        QuestHaTimelineService? timeline = null,
        ILogger<QuestReconcilerService>? logger = null,
        IReadOnlyList<QuestTableSpec>? tables = null)
    {
        _store = store;
        _nodes = nodes.ToList();
        _options = options;
        _clock = clock ?? TimeProvider.System;
        _timeline = timeline;
        _logger = logger ?? (ILogger)NullLogger.Instance;
        _tableNames = tables?.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
    }

    public ReconcileReportOutput? LastReport => Volatile.Read(ref _last);

    /// <summary>
    /// Đối soát mọi bảng trong một chu kỳ; chỉ ghi một kết quả AND mỗi node sau khi hoàn tất toàn bộ bảng.
    /// Trả report từng bảng (bảng lỗi không có report); <c>NodeOk</c> của report đã AND với chứng nhận tổng hợp đã lưu.
    /// </summary>
    // Kết quả từng bảng cần giữ NodeOk tổng hợp để không chứng nhận node khi còn bảng lỗi.
    public async Task<IReadOnlyList<ReconcileReportOutput>> ReconcileRecentAllAsync(IReadOnlyList<QuestTableSpec> tables, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var reports = new List<ReconcileReportOutput>(tables.Count);
            // Không dùng chứng nhận của chu kỳ cũ để kết luận chu kỳ mới.
            _results.Clear();
            foreach (var table in tables) _results[table.Name] = new(StringComparer.OrdinalIgnoreCase);
            var end = _clock.GetUtcNow().UtcDateTime - _options.SettleDelay;
            var from = Minute(end - _options.Lookback);
            var to = Minute(end);
            if (from >= to) return reports;
            foreach (var table in tables)
            {
                try
                {
                    var report = await RunAsync(table, from, to, ct, persist: false);
                    Volatile.Write(ref _last, report);
                    reports.Add(report);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    // Bảng lỗi giữ bằng chứng rỗng ⇒ cả chu kỳ không được chứng nhận.
                    _logger.LogSafeWarning(ex, "[QuestHa.Reconciler] Đối soát bảng {Table} lỗi.", table.Name);
                    _timeline?.Publish("Reconciler", null, QuestHaEventKindEnum.ReconcileMismatch,
                        $"Đối soát bảng {table.Name} lỗi — không chứng nhận chu kỳ");
                }
            }
            var stored = await PersistResultsAsync(ct);
            return reports.Select(r => r with { NodeOk = r.NodeOk.ToDictionary(kv => kv.Key, kv => kv.Value && stored.GetValueOrDefault(kv.Key), StringComparer.OrdinalIgnoreCase) }).ToList();
        }
        finally { _gate.Release(); }
    }

    /// <summary>Đối soát cửa sổ mặc định [now − Lookback, now − SettleDelay); bỏ lượt rỗng sau làm tròn phút.</summary>
    public Task<ReconcileReportOutput> ReconcileRecentAsync(QuestTableSpec table, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var now = _clock.GetUtcNow().UtcDateTime;
        var end = now - _options.SettleDelay;
        var from = Minute(end - _options.Lookback);
        var to = Minute(end);
        if (from >= to)
        {
            // Chỉ trả report cục bộ chưa chứng nhận; giữ nguyên LastReport và kết quả đã persist.
            var nodeOk = _nodes.ToDictionary(n => n.Node, _ => false, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(new ReconcileReportOutput(table.Name, from, to, now, now,
                Array.Empty<ReconcileWindowOutput>(), 0, nodeOk, 0));
        }

        return ReconcileAsync(table, from, to, ct);
    }

    /// <summary>Đối soát journal ↔ các node trong khoảng [<paramref name="fromUtc"/>, <paramref name="toUtc"/>): so số dòng + hash theo cửa sổ, ghi kết quả từng node.</summary>
    public async Task<ReconcileReportOutput> ReconcileAsync(QuestTableSpec table, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var from = Minute(fromUtc);
        var to = Minute(toUtc);
        if (from >= to)
            throw new ArgumentException("Cửa sổ đối soát phải có ít nhất một phút sau khi làm tròn.", nameof(toUtc));

        await _gate.WaitAsync(ct);
        try
        {
            _results[table.Name] = new(StringComparer.OrdinalIgnoreCase);
            var report = await RunAsync(table, from, to, ct);
            Volatile.Write(ref _last, report);
            return report;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // lỗi đọc journal cũng vô hiệu chứng nhận cũ của bảng này.
            await PersistResultsAsync(ct);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static DateTime Minute(DateTime d)
    {
        var u = QuestPayloadHelper.NormalizeTs(d);
        return new DateTime(u.Ticks - u.Ticks % TimeSpan.TicksPerMinute, DateTimeKind.Utc);
    }

    private async Task<ReconcileReportOutput> RunAsync(QuestTableSpec table, DateTime from, DateTime to, CancellationToken ct, bool persist = true)
    {
        var started = _clock.GetUtcNow().UtcDateTime;
        // chốt Seq đã commit trước cửa sổ, tránh bản đến muộn gây Extra giả.
        var head = (await _store.GetLagAsync(0, ct)).MaxSeq;
        var journalRows = await _store.ReadWindowAsync(table.Name, from, to, head, ct);

        // Payload → row (dòng hỏng bị loại: không node nào có được nó).
        var converted = new List<(QuestRow Row, DateTime Minute)>(journalRows.Count);
        foreach (var j in journalRows)
        {
            try
            {
                var row = QuestPayloadHelper.ToRow(table, j.Seq, j.Payload);
                converted.Add((row, Minute((DateTime)row.Values[table.DesignatedIndex]!)));
            }
            catch (PayloadConversionException)
            {
            }
        }

        var minSeq = journalRows.Count == 0 ? 0 : journalRows.Min(r => r.Seq);
        var maxSeq = journalRows.Count == 0 ? 0 : journalRows.Max(r => r.Seq);

        // quarantine chưa resolved của mọi node trong cửa sổ Seq. Chỉ cần khi journal có dòng
        // (cửa sổ rỗng không đụng store quarantine).
        IReadOnlyList<OpenQuarantineOutput> openList = Array.Empty<OpenQuarantineOutput>();
        string? quarantineError = null;
        if (journalRows.Count > 0)
        {
            try { openList = await _store.GetOpenQuarantineAsync(minSeq, maxSeq, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested) { quarantineError = QuestErrorClassifierHelper.SafeDescribe(ex); }
        }

        // Seq → (node → thế hệ Attempts đã đọc). Thế hệ dùng để resolve có điều kiện.
        var openQ = openList.GroupBy(e => e.Seq).ToDictionary(g => g.Key,
            g => g.GroupBy(e => e.Node, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First().Attempts, StringComparer.OrdinalIgnoreCase));

        // Dữ liệu từng node.
        var perNode = new Dictionary<string, NodeData>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in _nodes)
        {
            var data = new NodeData();
            try
            {
                // thế hệ + mốc đồng hồ SQL được chốt TRƯỚC khi đọc node; persist chỉ hợp lệ nếu chúng còn nguyên.
                data.Recovery = await _store.GetRecoveryAsync(node.Node, ct);
                data.Cursor = (await _store.GetCursorAsync(node.Node, ct)).LastSeq;
                var wait = System.Diagnostics.Stopwatch.StartNew();
                while (data.Cursor < head && wait.Elapsed < _options.WalApplyWait)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(50, Math.Max(1, (_options.WalApplyWait - wait.Elapsed).TotalMilliseconds))), ct);
                    data.Cursor = (await _store.GetCursorAsync(node.Node, ct)).LastSeq;
                }
                if (quarantineError is not null) throw new InvalidOperationException(quarantineError);
                // WAL chưa apply hết ⇒ không được đọc/chứng nhận như đã đủ.
                // cursor đã đọc ở trên, TRƯỚC probe đầu của helper; row ≤ cursor đã nằm trong WAL
                // lúc đó nên mốc sequencer chốt ở probe đầu bao chúng. Không đòi WAL của append sau head rỗng.
                if (!await node.WaitWalAppliedAsync(table, _options.WalApplyWait, ct))
                    throw new TimeoutException($"WAL của {node.Node} chưa apply hết trong {_options.WalApplyWait.TotalSeconds:0.#}s.");
                var rows = await node.ReadRangeAsync(table, from, to, ct);
                // Node đã upsert bản mới sau H0: không thể phục dựng bản cũ, bỏ key đó ở cả hai phía.
                data.AfterSnapshotKeys = rows.Where(r => r.Seq > head).Select(r => QuestPayloadHelper.KeyOf(table, r)).ToHashSet(StringComparer.Ordinal);
                // VDS không lưu journalSeq (đọc node trả Seq=0); tra journal SAU đọc từng node.
                // Key sau H0, kể cả UPSERT key cũ trong bucket mở, không thể so với ảnh trước H0 ở cả hai phía.
                if (!table.Columns.Any(c => c.FromJournalSeq))
                {
                    var current = await _store.ReadWindowAsync(table.Name, from, to, ct);
                    foreach (var late in current.Where(j => j.Seq > head))
                        data.AfterSnapshotKeys.Add(QuestPayloadHelper.KeyOf(table, QuestPayloadHelper.ToRow(table, late.Seq, late.Payload)));
                }
                data.Rows = rows.Where(r => r.Seq <= head && !data.AfterSnapshotKeys.Contains(QuestPayloadHelper.KeyOf(table, r))).ToArray();
                data.ActualByKey = data.Rows
                    .GroupBy(r => QuestPayloadHelper.KeyOf(table, r))
                    .ToDictionary(g => g.Key, g => QuestPayloadHelper.FingerprintOf(table, g.First()), StringComparer.Ordinal);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                data.Error = QuestErrorClassifierHelper.SafeDescribe(ex);
            }

            perNode[node.Node] = data;
        }

        // resolve quarantine khi dòng đã có ĐÚNG (key + fingerprint gồm journalSeq) trên node. Làm TRƯỚC khi dựng
        // tập kỳ vọng để Seq toàn cục vừa xuất hiện đúng trên một node được tính ngay trong lượt này.
        // effectiveQ = quarantine còn mở sau resolve (resolve lỗi ⇒ vẫn mở, không làm hỏng report).
        var convertedBySeq = converted.ToDictionary(c => c.Row.Seq, c => c.Row);
        var effectiveQ = new Dictionary<long, HashSet<string>>();
        var resolved = 0;
        foreach (var (seq, set) in openQ)
        {
            var open = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            convertedBySeq.TryGetValue(seq, out var qrow); // vắng = khác bảng/cửa sổ hoặc payload hỏng
            var key = qrow is null ? null : QuestPayloadHelper.KeyOf(table, qrow);
            var fp = qrow is null ? null : QuestPayloadHelper.FingerprintOf(table, qrow);
            foreach (var node in _nodes)
            {
                if (!set.TryGetValue(node.Node, out var attempts)) continue;
                var actualByKey = perNode[node.Node].ActualByKey;
                if (key is not null && actualByKey is not null && actualByKey.TryGetValue(key, out var afp)
                    && string.Equals(afp, fp, StringComparison.Ordinal))
                {
                    try
                    {
                        if (await _store.ResolveQuarantineAsync(seq, node.Node, attempts, "reconciler", ct))
                        {
                            resolved++;
                            continue;
                        }

                        // Đã bị quarantine lại / resolve bởi bên khác: bằng chứng cũ không còn hiệu lực.
                        _timeline?.Publish("Reconciler", node.Node, QuestHaEventKindEnum.ReconcileMismatch,
                            $"{node.Node}: quarantine Seq {seq} đổi thế hệ trong lúc đối soát — không resolve, không chứng nhận");
                    }
                    catch (Exception ex) when (!ct.IsCancellationRequested)
                    {
                        _logger.LogSafeWarning(ex, "[QuestHa.Reconciler] Không resolve được quarantine Seq {Seq} của {Node}.", seq, node.Node);
                        _timeline?.Publish("Reconciler", node.Node, QuestHaEventKindEnum.ReconcileMismatch,
                            $"{node.Node}: không resolve được quarantine Seq {seq} — {QuestErrorClassifierHelper.SafeDescribe(ex)}");
                    }
                }

                open.Add(node.Node);
            }

            if (open.Count > 0) effectiveQ[seq] = open;
        }

        // Chỉ Seq quarantine ở TẤT CẢ node (payload hỏng toàn cục, ngoại lệ L10) mới bị loại khỏi tập kỳ vọng chung.
        var globalQ = effectiveQ.Where(kv => _nodes.All(n => kv.Value.Contains(n.Node))).Select(kv => kv.Key).ToHashSet();
        var expectedRows = converted.Where(c => !globalQ.Contains(c.Row.Seq)).ToList();

        // Gom theo phút.
        var minutes = new SortedSet<DateTime>(converted.Select(c => c.Minute));
        foreach (var d in perNode.Values.Where(d => d.Rows is not null))
            foreach (var r in d.Rows!) minutes.Add(Minute((DateTime)r.Values[table.DesignatedIndex]!));

        var mismatched = new List<ReconcileWindowOutput>();
        var winnerSeqs = new HashSet<long>();
        // Kết quả đọc phải được xét cả khi journal và các node đọc được đều rỗng.
        var nodeOk = _nodes.ToDictionary(n => n.Node, n => perNode[n.Node].Rows is not null && perNode[n.Node].Cursor >= head, StringComparer.OrdinalIgnoreCase);
        // Phút chẩn đoán kết nối, không phải Missing/Extra hay phút có dữ liệu.
        var diagnosticMinute = minutes.Count == 0 && nodeOk.Values.Any(ok => !ok);
        if (diagnosticMinute) minutes.Add(from);

        foreach (var minute in minutes)
        {
            var jInMinute = expectedRows.Where(c => c.Minute == minute).Select(c => c.Row).ToList();
            var journalExpected = LastPerKey(table, jInMinute);
            foreach (var w in QuestBatchMergeHelper.MergeLastPerKey(table, jInMinute)) winnerSeqs.Add(w.Seq);
            var results = new List<NodeWindowOutput>();
            var wMin = jInMinute.Count == 0 ? 0 : jInMinute.Min(r => r.Seq);
            var wMax = jInMinute.Count == 0 ? 0 : jInMinute.Max(r => r.Seq);

            foreach (var node in _nodes)
            {
                var d = perNode[node.Node];
                if (d.Rows is null)
                {
                    results.Add(new NodeWindowOutput(node.Node, 0, "", WindowStatusEnum.Unreachable, 0, 0, 0));
                    nodeOk[node.Node] = false;
                    continue;
                }

                // Tập kỳ vọng CHUNG: quarantine riêng của node không miễn dòng đó cho node.
                var expected = journalExpected.Where(kv => !d.AfterSnapshotKeys.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
                var actual = d.Rows
                    .Where(r => Minute((DateTime)r.Values[table.DesignatedIndex]!) == minute)
                    .GroupBy(r => QuestPayloadHelper.KeyOf(table, r))
                    .ToDictionary(g => g.Key, g => QuestPayloadHelper.FingerprintOf(table, g.First()), StringComparer.Ordinal);

                int missing = 0, different = 0;
                foreach (var (key, fp) in expected)
                {
                    if (!actual.TryGetValue(key, out var afp)) missing++;
                    else if (!string.Equals(fp, afp, StringComparison.Ordinal)) different++;
                }

                var extra = actual.Keys.Count(k => !expected.ContainsKey(k));
                var hash = QuestPayloadHelper.AggregateHash(actual.Select(kv => (kv.Key, kv.Value)));
                WindowStatusEnum status;
                if (d.Cursor < head) status = WindowStatusEnum.Pending; // không chứng nhận trước khi node tới H0, kể cả dữ liệu trùng
                else if (missing == 0 && different == 0 && extra == 0) status = WindowStatusEnum.Ok;
                else status = WindowStatusEnum.Mismatch;

                if (status != WindowStatusEnum.Ok) nodeOk[node.Node] = false;
                results.Add(new NodeWindowOutput(node.Node, actual.Count, hash, status, missing, extra, different));
            }

            if (results.Any(r => r.Status != WindowStatusEnum.Ok))
            {
                mismatched.Add(new ReconcileWindowOutput(minute, journalExpected.Count,
                    QuestPayloadHelper.AggregateHash(journalExpected.Select(kv => (kv.Key, kv.Value))), wMin, wMax, results));
            }
        }

        // Quarantine MỞ riêng của node = Seq còn thắng ở node này mà node chưa có đúng (không tính Seq toàn cục).
        var openCount = _nodes.ToDictionary(n => n.Node, n => effectiveQ.Count(kv =>
            !globalQ.Contains(kv.Key) && winnerSeqs.Contains(kv.Key) && kv.Value.Contains(n.Node)), StringComparer.OrdinalIgnoreCase);
        foreach (var (node, count) in openCount)
            if (count > 0) nodeOk[node] = false;

        // chứng nhận không được quên quarantine ở cửa sổ khác. Trước khi persist true, đọc lại quarantine mở
        // TOÀN dải (sau resolve ⇒ thấy cả quarantine mới/đổi thế hệ). Seq quarantine ở mọi node: không chặn (L10).
        // Superseded: canonical mới thay thế ⇒ không chặn, resolve "superseded" đúng thế hệ.
        var outside = _nodes.ToDictionary(n => n.Node, _ => 0, StringComparer.OrdinalIgnoreCase);
        if (nodeOk.Values.Any(v => v))
        {
            IReadOnlyList<OpenQuarantineOutput> all = Array.Empty<OpenQuarantineOutput>();
            string? fullError = null;
            try { all = await _store.GetOpenQuarantineAsync(0, long.MaxValue, ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested) { fullError = QuestErrorClassifierHelper.SafeDescribe(ex); }

            if (fullError is not null)
            {
                foreach (var n in _nodes) nodeOk[n.Node] = false;
                _timeline?.Publish("Reconciler", null, QuestHaEventKindEnum.ReconcileMismatch,
                    $"Không đọc được quarantine toàn dải — không chứng nhận node: {fullError}");
            }
            else
            {
                foreach (var e in all.Where(e => e.Superseded))
                {
                    try { await _store.ResolveQuarantineAsync(e.Seq, e.Node, e.Attempts, "superseded", ct); }
                    catch (Exception ex) when (!ct.IsCancellationRequested)
                    {
                        _logger.LogSafeWarning(ex, "[QuestHa.Reconciler] Không resolve được quarantine superseded Seq {Seq} của {Node}.", e.Seq, e.Node);
                    }
                }

                var allNodesQ = all.GroupBy(e => e.Seq)
                    .Where(g => _nodes.All(n => g.Any(e => string.Equals(e.Node, n.Node, StringComparison.OrdinalIgnoreCase))))
                    .Select(g => g.Key).ToHashSet();
                foreach (var e in all.Where(e => !e.Superseded && !allNodesQ.Contains(e.Seq)))
                {
                    var node = _nodes.FirstOrDefault(n => string.Equals(n.Node, e.Node, StringComparison.OrdinalIgnoreCase));
                    if (node is null) continue;
                    nodeOk[node.Node] = false;
                    if (!(winnerSeqs.Contains(e.Seq) && effectiveQ.ContainsKey(e.Seq))) outside[node.Node]++;
                }
            }
        }

        // (membership): store định nghĩa "quarantine ở mọi node" bằng tập dòng QuestNodeStatus; GetRecoveryAsync tạo dòng đó và dòng
        // không bao giờ bị xoá. Nếu GetRecovery của BẤT KỲ node cấu hình nào lỗi, tập đó có thể thiếu ⇒ không thể phân biệt quarantine
        // riêng với toàn cục ⇒ fail-closed: không node nào được chứng nhận true trong lượt này (false vẫn được persist theo thế hệ).
        if (perNode.Values.Any(d => d.Recovery is null))
        {
            foreach (var n in _nodes) nodeOk[n.Node] = false;
            _timeline?.Publish("Reconciler", null, QuestHaEventKindEnum.ReconcileMismatch,
                "Membership chưa đủ (không lấy được thế hệ phục hồi của một node cấu hình) — không chứng nhận node nào");
        }

        // bảng OK không thể ghi đè lỗi bảng khác; chu kỳ nền chỉ persist sau bảng cuối.
        _results[table.Name] = nodeOk.ToDictionary(kv => kv.Key,
            kv => new ReconcileEvidence(kv.Value, perNode[kv.Key].Recovery), StringComparer.OrdinalIgnoreCase);
        if (persist)
        {
            var stored = await PersistResultsAsync(ct);
            // nêu lý do khi bảng này sạch nhưng chứng nhận tổng hợp bị từ chối vì thiếu bằng chứng bảng khác
            // (gọi đơn lẻ từng bảng: bảng đầu luôn false, bảng cuối mới true — fail-closed đúng thiết kế, không phải lệch dữ liệu).
            var missingTables = MissingEvidenceTables();
            if (missingTables.Length > 0 && nodeOk.Any(kv => kv.Value && !stored[kv.Key]))
            {
                var note = $"Đối soát {table.Name}: bảng sạch nhưng chưa đủ bằng chứng các bảng [{string.Join(", ", missingTables)}] — chưa cấp chứng nhận tổng hợp (dùng ReconcileRecentAllAsync)";
                _logger.LogInformation("[QuestHa.Reconciler] {Note}", note);
                _timeline?.Publish("Reconciler", null, QuestHaEventKindEnum.ReconcileMismatch, note);
            }
            foreach (var node in nodeOk.Keys.ToList()) nodeOk[node] &= stored[node];
        }

        var report = new ReconcileReportOutput(table.Name, from, to, started, _clock.GetUtcNow().UtcDateTime, mismatched, minutes.Count, nodeOk, journalRows.Count,
            openCount);

        var summary = string.Join(", ", nodeOk.Select(kv => $"{kv.Key}={(kv.Value ? "OK" : "LỆCH/CHỜ")}"));
        _timeline?.Publish("Reconciler", null,
            nodeOk.Values.All(v => v) ? QuestHaEventKindEnum.ReconcileOk : QuestHaEventKindEnum.ReconcileMismatch,
            $"Đối soát {table.Name} {from:HH:mm}–{to:HH:mm}Z: {minutes.Count} phút, {journalRows.Count} dòng journal; {summary}" +
            (openCount.Values.Any(c => c > 0) ? "; quarantine mở: " + string.Join(", ", openCount.Where(kv => kv.Value > 0).Select(kv => $"{kv.Key}={kv.Value}")) : "") +
            (outside.Values.Any(c => c > 0) ? "; quarantine mở ngoài cửa sổ chặn chứng nhận: " + string.Join(", ", outside.Where(kv => kv.Value > 0).Select(kv => $"{kv.Key}={kv.Value} Seq")) : "") +
            (resolved > 0 ? $"; resolve {resolved} quarantine" : "") +
            (globalQ.Count > 0 ? $"; {globalQ.Count} Seq quarantine toàn cục (loại khỏi kỳ vọng)" : "") +
            (diagnosticMinute ? "; phút chẩn đoán Unreachable (không có dữ liệu)" : "") +
            // cửa sổ rỗng không chứng minh coverage head nằm ngoài cửa sổ.
            (journalRows.Count == 0 && nodeOk.Values.Any(ok => !ok) ? "; journal rỗng trong cửa sổ; chưa chứng nhận coverage H0/WAL/quarantine/thế hệ" : "") +
            (mismatched.Count > 0 ? $"; lệch {mismatched.Count} phút" : ""));

        return report;
    }

    /// <summary>Bảng bắt buộc của chu kỳ mà chưa có bằng chứng cho mọi node (chưa chạy hoặc bị lỗi).</summary>
    private string[] MissingEvidenceTables()
    {
        var required = _tableNames.Length == 0 ? _results.Keys.ToArray() : _tableNames;
        return required.Where(t => !_results.TryGetValue(t, out var rows) || _nodes.Any(n => !rows.ContainsKey(n.Node))).ToArray();
    }

    private async Task<Dictionary<string, bool>> PersistResultsAsync(CancellationToken ct)
    {
        var result = _nodes.ToDictionary(n => n.Node, _ => false, StringComparer.OrdinalIgnoreCase);
        var required = _tableNames.Length == 0 ? _results.Keys.ToArray() : _tableNames;
        foreach (var node in result.Keys.ToList())
        {
            var evidence = required.Select(t => _results.TryGetValue(t, out var rows) && rows.TryGetValue(node, out var e) ? e : null).ToArray();
            var observed = evidence.Where(e => e?.Recovery is not null).Select(e => e!.Recovery!).ToArray();
            var rec = observed.OrderByDescending(r => r.Generation).FirstOrDefault();
            var ok = evidence.Length > 0 && evidence.All(e => e is { Ok: true, Recovery: not null }) &&
                observed.All(r => r.Generation == rec!.Generation);
            try
            {
                // Chỉ đọc thế hệ mới để hạ cờ false, tuyệt đối không dùng nó chứng nhận dữ liệu đã đọc trước đó.
                rec ??= await _store.GetRecoveryAsync(node, ct);
                var readStarted = observed.Length == 0 ? rec.SqlNowUtc : observed.Min(r => r.SqlNowUtc);
                var stored = await _store.WriteReconcileResultAsync(node, ok, rec.Generation, readStarted, ct);
                result[node] = ok && stored == true;
                if (ok && stored != true)
                    _timeline?.Publish("Reconciler", node, QuestHaEventKindEnum.ReconcileMismatch,
                        $"{node}: store từ chối chứng nhận tổng hợp — thế hệ/mốc đọc đổi hoặc quarantine mở");
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogSafeWarning(ex, "[QuestHa.Reconciler] Không ghi được kết quả đối soát {Node}.", node);
            }
        }
        return result;
    }

    /// <summary>key → fingerprint của dòng có Seq lớn nhất.</summary>
    private static Dictionary<string, string> LastPerKey(QuestTableSpec table, IEnumerable<QuestRow> rows) =>
        QuestBatchMergeHelper.MergeLastPerKey(table, rows)
            .ToDictionary(r => QuestPayloadHelper.KeyOf(table, r), r => QuestPayloadHelper.FingerprintOf(table, r), StringComparer.Ordinal);

    /// <summary>
    /// Phát lại cửa sổ: hạ cursor của node về (Seq nhỏ nhất của các dòng journal có ts trong [from, to)) − 1.
    /// Không bao giờ nâng cursor. Trả về cursor sau khi hạ.
    /// </summary>
    public async Task<long> ReplayWindowAsync(string node, QuestTableSpec table, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var rows = await _store.ReadWindowAsync(table.Name, fromUtc, toUtc, ct);
        var cursorBefore = (await _store.GetCursorAsync(node, ct)).LastSeq;
        if (rows.Count == 0) return cursorBefore;

        var target = rows.Min(r => r.Seq) - 1;
        var after = await _store.LowerCursorAsync(node, target, "reconciler", ct);
        _timeline?.Publish("Reconciler", node, QuestHaEventKindEnum.CursorReset,
            $"Phát lại {table.Name} {fromUtc:HH:mm:ss}–{toUtc:HH:mm:ss}Z cho {node}: cursor {cursorBefore} → {after}");
        return after;
    }

    /// <summary>Tự phát lại các phút Mismatch có Missing/Different (Extra thì chỉ báo).</summary>
    public async Task<int> ReplayMismatchesAsync(QuestTableSpec table, ReconcileReportOutput report, CancellationToken ct = default)
    {
        var n = 0;
        foreach (var w in report.MismatchedWindows)
        {
            foreach (var r in w.Nodes.Where(r => r.Status == WindowStatusEnum.Mismatch && (r.Missing > 0 || r.Different > 0)))
            {
                await ReplayWindowAsync(r.Node, table, w.MinuteUtc, w.MinuteUtc.AddMinutes(1), ct);
                n++;
            }
        }

        return n;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Bằng chứng đối soát một bảng trên một node; giữ mốc SQL để không chứng nhận nhầm thế hệ.
    /// Created date: 28/10/2026
    /// </summary>
    private sealed record ReconcileEvidence(bool Ok, NodeRecoveryOutput? Recovery);

    /// <summary>
    /// Author: KienLT
    /// Description: Dữ liệu đọc từ một node trong một lượt đối soát (recovery, cursor, dòng, khoá sau snapshot, lỗi).
    /// Created date: 09/10/2026
    /// </summary>
    private sealed class NodeData
    {
        public NodeRecoveryOutput? Recovery;
        public long Cursor;
        public IReadOnlyList<QuestRow>? Rows;
        public Dictionary<string, string>? ActualByKey;
        public HashSet<string> AfterSnapshotKeys = new(StringComparer.Ordinal);
        public string? Error;
    }
}
