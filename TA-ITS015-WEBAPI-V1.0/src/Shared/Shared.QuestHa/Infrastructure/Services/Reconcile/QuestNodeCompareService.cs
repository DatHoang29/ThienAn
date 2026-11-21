using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.QuestHa.Infrastructure.Services.Reconcile.Rules;

namespace Shared.QuestHa.Infrastructure.Services.Reconcile;

/// <summary>
/// Author: KienLT
/// Description: Đối soát và vá giữa HAI node QuestDB (nguồn ↔ đích), theo từng phút. Dùng ở chế độ Shadow: node chính ghi
/// trực tiếp, bản sao vào journal có thể mất (journal chết, dừng quá hạn…) mà <see cref="QuestReconcilerService"/> (so journal ↔ node)
/// không thấy. Vá = đưa lại các dòng thiếu/lệch của node nguồn vào journal; replicator giao sang các node, DEDUP UPSERT ghi đè
/// nên chạy lại an toàn về dữ liệu. Mặc định chạy thử (dry-run); có trần số dòng, số cửa sổ; mốc cuối bị cắt <c>now − settle</c>;
/// KHÔNG ghi journal khi đích còn backlog chưa giao (tránh nhân đôi mục khi chạy lặp).
/// Created date: 27/10/2026
/// </summary>
internal sealed class QuestNodeCompareService
{
    /// <summary>
    /// Chờ mặc định trước khi coi một dòng là "đáng lẽ đã nhân bản xong". Tính theo thời gian SỰ KIỆN (detectTime/fromTime) chứ không
    /// theo lúc nhận: dòng đến muộn/bù lịch sử có thể bị so khi bản sao chưa tới đích (chỉ gây ghi trùng cùng giá trị). Khi chạy định kỳ
    /// nên đặt settle ≥ vài phút.
    /// </summary>
    public static readonly TimeSpan DefaultSettle = TimeSpan.FromSeconds(30);

    /// <summary>Trần số cửa sổ phút mỗi lần chạy (7 ngày) — chặn gõ nhầm <c>from</c> (vd UnixEpoch ⇒ hàng chục triệu truy vấn).</summary>
    public const int DefaultMaxWindows = 7 * 24 * 60;

    /// <summary>Đích còn quá số dòng journal chưa giao này thì vá bị hoãn (cho phép trễ nhân bản bình thường của dòng sống).</summary>
    public const int DefaultMaxTargetPending = 1_000;

    private const int JournalChunk = 500;
    private const int MaxListedWindows = 100;
    private static readonly TimeSpan WindowSize = TimeSpan.FromMinutes(1);

    private readonly IJournalStore _journal;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    /// <summary>Khởi tạo dịch vụ so/vá giữa hai node trên journal, đồng hồ và logger.</summary>
    public QuestNodeCompareService(IJournalStore journal, TimeProvider? clock = null, ILogger<QuestNodeCompareService>? logger = null)
    {
        _journal = journal;
        _clock = clock ?? TimeProvider.System;
        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Kết quả so một cửa sổ thời gian giữa node nguồn và đích (thiếu/khác/thừa).
    /// Created date: 27/10/2026
    /// </summary>
    private sealed class WindowResult(DateTime from, int sourceRows, int targetRows,
        List<QuestRow> missing, List<(QuestRow Source, QuestRow Target)> different, List<QuestRow> extra)
    {
        public DateTime From { get; } = from;
        public int SourceRows { get; } = sourceRows;
        public int TargetRows { get; } = targetRows;
        public List<QuestRow> Missing { get; } = missing;
        public List<(QuestRow Source, QuestRow Target)> Different { get; } = different;
        public List<QuestRow> Extra { get; } = extra;
        public bool Clean => Missing.Count == 0 && Different.Count == 0 && Extra.Count == 0;
    }

    /// <summary>
    /// Đối soát <paramref name="source"/> ↔ <paramref name="target"/> cho <paramref name="table"/> trong [<paramref name="fromUtc"/>,
    /// <paramref name="toUtc"/>). Chỉ đọc. Đầu vào Local/Unspecified được quy về UTC; <c>FromUtc</c> trong kết quả là đầu phút thật
    /// được so. <paramref name="settle"/> = null ⇒ <see cref="DefaultSettle"/> (âm ⇒ lỗi; <see cref="TimeSpan.Zero"/> chỉ cắt tại <c>now</c>).
    /// Vượt <paramref name="maxWindows"/> cửa sổ phút ⇒ <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public async Task<NodeCompareOutput> CompareAsync(
        QuestTableSpec table, IQuestNodeClient source, IQuestNodeClient target, DateTime fromUtc, DateTime toUtc,
        TimeSpan? settle = null, int sampleLimit = 20, int maxWindows = DefaultMaxWindows, CancellationToken ct = default)
    {
        var started = _clock.GetUtcNow().UtcDateTime;
        var (from, to) = Prepare(fromUtc, toUtc, settle, maxWindows, started);
        long srcRows = 0, tgtRows = 0, missing = 0, extra = 0, different = 0;
        var windows = 0;
        var badCount = 0;
        var bad = new List<DateTime>();
        var samples = new List<NodeCompareSampleOutput>();
        // Mẫu chia theo loại, tổng không vượt sampleLimit: loại đầu nhận phần dư của phép chia 3.
        var limit = Math.Max(0, sampleLimit);
        var caps = new[] { limit - 2 * (limit / 3), limit / 3, limit / 3 };
        var sampled = new int[3];

        await ScanAsync(table, source, target, from, to, ct, w =>
        {
            windows++;
            srcRows += w.SourceRows;
            tgtRows += w.TargetRows;
            missing += w.Missing.Count;
            extra += w.Extra.Count;
            different += w.Different.Count;
            if (w.Clean) return Task.FromResult(true);

            badCount++;
            if (bad.Count < MaxListedWindows) bad.Add(w.From);
            foreach (var r in w.Missing.Take(Math.Max(0, caps[0] - sampled[0])))
            {
                samples.Add(new("MissingInTarget", QuestPayloadHelper.KeyOf(table, r), Array.Empty<string>()));
                sampled[0]++;
            }

            foreach (var (s, t) in w.Different.Take(Math.Max(0, caps[1] - sampled[1])))
            {
                samples.Add(new("Different", QuestPayloadHelper.KeyOf(table, s), QuestNodeCompareRules.DescribeColumns(table, s, t)));
                sampled[1]++;
            }

            foreach (var r in w.Extra.Take(Math.Max(0, caps[2] - sampled[2])))
            {
                samples.Add(new("ExtraInTarget", QuestPayloadHelper.KeyOf(table, r), Array.Empty<string>()));
                sampled[2]++;
            }

            return Task.FromResult(true);
        }).ConfigureAwait(false);

        if (windows == 0)
            _logger.LogWarning("[QuestHa.Compare] {Table} {Source}→{Target}: khoảng so rỗng sau khi cắt settle — CHƯA so gì (Equal = false).",
                table.Name, source.Node, target.Node);

        var result = new NodeCompareOutput(table.Name, source.Node, target.Node, from, to, windows, srcRows, tgtRows,
            missing, extra, different, badCount, bad, samples, started, _clock.GetUtcNow().UtcDateTime);
        _logger.LogInformation("[QuestHa.Compare] {Table} {Source}→{Target}: {Windows} phút, thiếu {Missing}, lệch {Different}, thừa {Extra}.",
            table.Name, source.Node, target.Node, windows, missing, different, extra);
        return result;
    }

    /// <summary>
    /// Vá <paramref name="target"/> từ <paramref name="source"/>: đưa lại vào journal các dòng thiếu hoặc khác của nguồn. Không đụng:
    /// dòng chỉ có ở đích; dòng đích có version cao hơn (bảng có cột version); dòng nguồn mà replicator sẽ từ chối
    /// (<c>Unrepairable</c>). <paramref name="dryRun"/> mặc định true: chỉ đếm. Khi ghi thật mà đích còn quá <paramref name="maxTargetPending"/>
    /// dòng journal chưa giao (hoặc journal không có con trỏ của đích) thì <b>hoãn</b> — vẫn đếm ứng viên nhưng không ghi — để chạy lặp
    /// không nhân đôi mục journal; chạy lại sau khi replicator giao xong. Dừng khi tổng ứng viên chạm <paramref name="maxRows"/> (<c>Capped</c>).
    /// Lỗi giữa chừng được log kèm số dòng đã vào journal (idempotent nên chạy lại là đủ).
    /// </summary>
    public async Task<NodeRepairOutput> RepairAsync(
        QuestTableSpec table, IQuestNodeClient source, IQuestNodeClient target, DateTime fromUtc, DateTime toUtc,
        bool dryRun = true, int maxRows = 100_000, TimeSpan? settle = null, int maxTargetPending = DefaultMaxTargetPending,
        int maxWindows = DefaultMaxWindows, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRows, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(maxTargetPending);
        var started = _clock.GetUtcNow().UtcDateTime;
        var (from, to) = Prepare(fromUtc, toUtc, settle, maxWindows, started);

        string? deferReason = null;
        if (!dryRun)
        {
            var stats = await _journal.GetStatsAsync(ct).ConfigureAwait(false);
            if (!stats.PendingByNode.TryGetValue(target.Node, out var pending))
                deferReason = $"journal chưa có con trỏ của đích {target.Node} (không có replicator giao)";
            else if (pending > maxTargetPending)
                deferReason = $"đích {target.Node} còn {pending} dòng journal chưa giao (> {maxTargetPending}); chờ giao xong rồi chạy lại";
        }

        var write = !dryRun && deferReason is null;
        long candidates = 0, journaled = 0, extra = 0, targetNewer = 0, unrepairable = 0;
        var capped = false;

        try
        {
            await ScanAsync(table, source, target, from, to, ct, async w =>
            {
                extra += w.Extra.Count;
                var rows = new List<(QuestRow Row, string Payload)>(w.Missing.Count + w.Different.Count);
                foreach (var r in w.Missing) Collect(r);
                foreach (var (s, t) in w.Different)
                {
                    if (IsTargetNewer(table, s, t)) { targetNewer++; continue; }
                    Collect(s);
                }

                if (rows.Count == 0) return true;

                var room = maxRows - candidates;
                if (rows.Count > room)
                {
                    rows = rows.Take((int)room).ToList();
                    capped = true;
                }

                candidates += rows.Count;
                if (write)
                {
                    foreach (var chunk in rows.Chunk(JournalChunk))
                    {
                        var entries = chunk.Select(x => new JournalAppendInput(
                            table.Name, QuestPayloadHelper.KeyOf(table, x.Row), (DateTime)x.Row.Values[table.DesignatedIndex]!,
                            x.Payload, table.SchemaVer, QuestPayloadHelper.VersionOf(table, x.Row))).ToList();
                        await _journal.AppendRawBatchAsync(entries, ct).ConfigureAwait(false);
                        journaled += entries.Count;
                    }
                }

                return !capped;

                void Collect(QuestRow r)
                {
                    if (TryBuildPayload(table, r, out var payload)) rows.Add((r, payload));
                    else unrepairable++;
                }
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) // huỷ chủ động không phải sự cố, khỏi cảnh báo
        {
            // giữ số đã journal và chỉ log provider text ở Debug.
            _logger.LogSafeWarning(ex, "[QuestHa.Compare] Vá {Table} {Source}→{Target} dừng giữa chừng: ứng viên {Candidates}, đã đưa vào journal {Journaled}.",
                table.Name, source.Node, target.Node, candidates, journaled);
            throw;
        }

        if (deferReason is not null)
            _logger.LogWarning("[QuestHa.Compare] Vá {Table} {Source}→{Target} HOÃN: {Reason}.", table.Name, source.Node, target.Node, deferReason);
        if (unrepairable > 0)
            _logger.LogWarning("[QuestHa.Compare] {Table}: {Count} dòng nguồn không thể đưa vào journal (replicator sẽ từ chối) — cần xử lý tay.", table.Name, unrepairable);
        _logger.LogInformation("[QuestHa.Compare] Vá {Table} {Source}→{Target} dryRun={DryRun}: ứng viên {Candidates}, đã đưa vào journal {Journaled}, đích mới hơn {TargetNewer}, capped={Capped}.",
            table.Name, source.Node, target.Node, dryRun, candidates, journaled, targetNewer, capped);
        return new NodeRepairOutput(table.Name, source.Node, target.Node, from, to, dryRun, candidates, journaled, extra, targetNewer,
            unrepairable, capped, deferReason is not null, deferReason, started, _clock.GetUtcNow().UtcDateTime);
    }

    // Quy đầu vào về UTC, kiểm tham số và đếm cửa sổ TRƯỚC khi chạm node. Trả [đầu phút thật, mốc cuối đã cắt settle).
    private static (DateTime From, DateTime To) Prepare(DateTime fromUtc, DateTime toUtc, TimeSpan? settle, int maxWindows, DateTime now)
    {
        var s = settle ?? DefaultSettle;
        ArgumentOutOfRangeException.ThrowIfLessThan(s, TimeSpan.Zero, nameof(settle));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxWindows, 1);

        var f = QuestPayloadHelper.NormalizeTs(fromUtc);
        var from = new DateTime(f.Ticks - f.Ticks % WindowSize.Ticks, DateTimeKind.Utc);
        var to = QuestNodeCompareRules.EffectiveTo(QuestPayloadHelper.NormalizeTs(toUtc), now, s);
        if (to > from)
        {
            var windows = (to - from + WindowSize - TimeSpan.FromTicks(1)).Ticks / WindowSize.Ticks;
            if (windows > maxWindows)
                throw new ArgumentOutOfRangeException(nameof(toUtc),
                    $"Khoảng so cần {windows} cửa sổ phút, vượt trần {maxWindows}; thu hẹp khoảng hoặc tăng maxWindows.");
        }

        return (from, to);
    }

    // Bảng có cột version: đích có version CAO hơn nguồn thì không vá (journal chỉ so version với mục trong journal, không với đích).
    private static bool IsTargetNewer(QuestTableSpec table, QuestRow source, QuestRow target) =>
        table.VersionIndex >= 0
        && QuestPayloadHelper.VersionOf(table, target) is { } tv
        && QuestPayloadHelper.VersionOf(table, source) is { } sv
        && tv > sv;

    // Dòng chỉ vá được nếu replicator nhận lại: EventKey vừa cột journal, payload qua được ToRow (không NUL, đúng kiểu…).
    private static bool TryBuildPayload(QuestTableSpec table, QuestRow row, out string payload)
    {
        payload = QuestPayloadHelper.ToPayload(table, row);
        if (QuestPayloadHelper.KeyOf(table, row).Length > QuestJournalService.EventKeyMaxLength) return false;
        try
        {
            QuestPayloadHelper.ToRow(table, 0, payload);
            return true;
        }
        catch (PayloadConversionException)
        {
            return false;
        }
    }

    // Duyệt từng cửa sổ phút (đọc nguồn rồi đích, so sánh); callback trả false để dừng. Mỗi lần chỉ giữ MỘT phút trong RAM.
    private async Task ScanAsync(QuestTableSpec table, IQuestNodeClient source, IQuestNodeClient target, DateTime fromUtc, DateTime toUtc,
        CancellationToken ct, Func<WindowResult, Task<bool>> onWindow)
    {
        foreach (var (from, to) in QuestNodeCompareRules.Windows(fromUtc, toUtc, WindowSize))
        {
            ct.ThrowIfCancellationRequested();
            // Đọc NGUỒN trước ĐÍCH: dòng mới vào nguồn rồi tới đích giữa hai lần đọc chỉ hiện là "thừa ở đích" (không vá), không
            // thành "thiếu ở đích" giả. Đọc ngược lại sẽ sinh ứng viên vá không cần thiết.
            var s = await source.ReadRangeAsync(table, from, to, ct).ConfigureAwait(false);
            var t = await target.ReadRangeAsync(table, from, to, ct).ConfigureAwait(false);
            var (missing, different, extra) = QuestNodeCompareRules.Diff(table, s, t);
            if (!await onWindow(new WindowResult(from, s.Count, t.Count, missing, different, extra)).ConfigureAwait(false)) return;
        }
    }
}
