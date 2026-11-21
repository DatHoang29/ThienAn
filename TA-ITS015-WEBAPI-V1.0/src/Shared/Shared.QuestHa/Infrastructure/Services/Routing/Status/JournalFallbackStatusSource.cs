using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Infrastructure.Services.Routing.Status;

/// <summary>
/// Author: KienLT
/// Description: Nguồn trạng thái cho router khi journal (SQL) có thể chết .
/// SQL đọc được ⇒ trả đúng dữ liệu SQL và ghi nhớ MỐC PHỦ DỮ LIỆU từng node: mỗi lượt refresh đọc thêm đầu journal đã commit
/// (MaxSeq) tại thời điểm h (ghi ngay trước khi đọc); node được nâng CoveredUntilUtc = h CHỈ KHI status.LastSeq ≥ MaxSeq
/// (LastSeq là cận dưới dữ liệu node đã có ⇒ node có mọi Seq đã ACK trước h, bất kể hàng status ghi lúc nào).
/// LastSeq &lt; MaxSeq ⇒ ghi nhận "thiếu từ h". Không dùng LagSeq/heartbeat của hàng status để nâng mốc; đọc đầu journal lỗi ⇒ không nâng.
/// Mỗi request xử lý đúng một <see cref="StatusRefreshOutput"/>; coverage chỉ ghi khi generation lớn hơn generation đã ghi.
/// SQL không đáp ứng ⇒ node chỉ được coi Ready khi ĐỒNG THỜI:
/// (a) trạng thái replicator tại chỗ không phải Fenced/Down/CatchingUp/Unknown VÀ SQL lần cuối xác nhận Ready;
/// (b) lượt SQL thành công gần nhất cho thấy node không thiếu — nếu thiếu thì chỉ tin trong ReadyMaxLag kể từ lượt đó;
/// (c) có CoveredUntilUtc và chưa quá FallbackMaxAge;
/// (d) router tự thăm dò node (IQuestNodeClient): vòng nền probe theo FallbackProbeInterval suốt đợt dự phòng
///     (dừng khi SQL trở lại hoặc Dispose); nhịp sống = probe thành công gần nhất, KHÔNG phải HeartbeatUtc của replicator.
///     Nếu lúc chọn node mọi ứng viên đều stale, request chờ probe mới trong phần ngân sách StatusRefreshTimeout còn lại;
/// (e) breaker của router (áp ở bước chọn node).
/// Rủi ro chấp nhận: event ACK trong khoảng (lượt SQL thành công cuối, lúc SQL mất] (≤ StatusCacheTtl + PollInterval)
/// có thể chưa có trên node khi đọc dự phòng; chúng được giao khi SQL trở lại.
/// <c>readerOnly</c> (WebAPI) KHÔNG có dự phòng — SQL lỗi ⇒ trả rỗng, router báo hết node.
/// Created date: 11/10/2026
/// </summary>
internal sealed class JournalFallbackStatusSource : INodeStatusSource, IDisposable
{
    /// <summary>Hạn chờ probe do chính request khởi chạy / hạn tối thiểu khi mọi ứng viên stale (xem GetAsync).</summary>
    internal static readonly TimeSpan FirstProbeGrace = TimeSpan.FromMilliseconds(500);

    private readonly object _lock = new();
    private readonly SqlNodeStatusSource _primary;
    private readonly Func<IReadOnlyList<NodeStatusOutput>> _local;
    private readonly IReadOnlyList<IQuestNodeClient> _clients;
    private readonly QuestTableSpec _probeTable;
    private readonly RouterOptions _options;
    private readonly TimeSpan _readyMaxLag;
    private readonly TimeProvider _clock;
    private readonly bool _readerOnly;
    // Snapshot SQL cuối cùng đọc THÀNH CÔNG (khác cache của primary: không bị ghi đè khi lỗi).
    private IReadOnlyDictionary<string, NodeStatusOutput> _lastGood =
        new Dictionary<string, NodeStatusOutput>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _covered = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _missingSince = new(StringComparer.OrdinalIgnoreCase);
    private long _recordedGeneration;
    private long _stateGeneration;
    // Nhịp sống do router tự đo (probe thành công gần nhất).
    private readonly Dictionary<string, DateTime> _aliveUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _probeAttemptUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task> _probeInflight = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _disposeCts = new();
    private Task? _probeLoop;
    private bool _disposed;
    private volatile bool _usingFallback;

    /// <summary>Khởi tạo nguồn trạng thái SQL có dự phòng; readerOnly = host chỉ đọc (không có dự phòng khi SQL lỗi).</summary>
    public JournalFallbackStatusSource(
        SqlNodeStatusSource primary,
        Func<IReadOnlyList<NodeStatusOutput>> localSnapshots,
        IReadOnlyList<IQuestNodeClient> clients,
        QuestTableSpec probeTable,
        RouterOptions options,
        TimeSpan readyMaxLag,
        TimeProvider? clock = null, bool readerOnly = false)
    {
        _primary = primary;
        _local = localSnapshots;
        _clients = clients;
        _probeTable = probeTable;
        _options = options;
        _readyMaxLag = readyMaxLag;
        _clock = clock ?? TimeProvider.System;
        _readerOnly = readerOnly;
    }

    /// <summary>true = đang dùng nguồn dự phòng vì SQL không đáp ứng.</summary>
    public bool UsingFallback => _usingFallback;

    /// <inheritdoc />
    public DateTime? CoveredUntil(string node)
    {
        lock (_lock) return _covered.TryGetValue(node, out var c) ? c : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        try { _disposeCts.Cancel(); } catch (ObjectDisposedException) { }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, NodeStatusOutput>> GetAsync(CancellationToken ct = default)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        var r = await _primary.GetResultAsync(ct); // MỘT object nhất quán cho cả request
        if (r.Succeeded)
        {
            lock (_lock)
            {
                RecordSqlReadLocked(r);
                if (r.Generation >= _stateGeneration)
                {
                    _stateGeneration = r.Generation;
                    _usingFallback = false;
                }
            }

            return r.Statuses;
        }

        // host chỉ đọc (WebAPI) không có replicator tại chỗ; last-good SQL + probe sống chỉ cho biết
        // node còn sống, KHÔNG chứng nhận dữ liệu. SQL lỗi ⇒ không node nào đủ điều kiện ⇒ router báo hết node (503),
        // không dựng Ready từ last-good. Host ghi (Worker) giữ nguyên luật dự phòng.
        if (_readerOnly)
        {
            lock (_lock)
            {
                if (r.Generation >= _stateGeneration)
                {
                    _stateGeneration = r.Generation;
                    _usingFallback = true;
                }
            }

            return new Dictionary<string, NodeStatusOutput>(StringComparer.OrdinalIgnoreCase);
        }

        // probe chạy trên thread riêng, NGOÀI lock (client chặn đồng bộ có thể giữ
        // lock suốt lúc connect). Để quyết định của request vẫn dựa trên kết quả probe như trước, request CHỜ (ngoài lock),
        // G = FirstProbeGrace (≤ chu kỳ probe):
        //  mọi ứng viên Ready đều stale ⇒ mọi probe đang chạy (luật cũ), hạn = max(ngân sách còn lại, G) — lượt SQL
        //    lỗi/treo có thể đã ăn hết ngân sách (vd. ngay khi vừa sang dự phòng);
        //  - ngược lại chỉ các probe do CHÍNH request này khởi chạy, hạn G. Probe của request khác / vòng nền không bị chờ
        //    ⇒ node chết (probe treo) chỉ làm chậm ≤ G đúng request khởi chạy probe của nó, mỗi FallbackProbeInterval một lần;
        //    các request khác dùng ngay node còn tươi.
        var own = new List<Task>();
        lock (_lock)
        {
            if (r.Generation >= _stateGeneration)
            {
                _stateGeneration = r.Generation;
                _usingFallback = true;
            }

            EnsureProbeLoopLocked();
            own.AddRange(StartDueProbesLocked());
        }

        var result = Build();
        var grace = FirstProbeGrace < _options.FallbackProbeInterval ? FirstProbeGrace : _options.FallbackProbeInterval;
        IEnumerable<Task> waitFor = own;
        var limit = grace;
        lock (_lock)
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            var ready = result.Values.Where(s => s.State == QuestNodeStateEnum.Ready).ToList();
            if (ready.Count > 0 && ready.All(s => now - s.HeartbeatUtc > _options.HeartbeatStaleAfter))
            {
                own.AddRange(StartDueProbesLocked());
                waitFor = own.Concat(_probeInflight.Values).ToList();
                var remainingBudget = _options.StatusRefreshTimeout - System.Diagnostics.Stopwatch.GetElapsedTime(started);
                if (remainingBudget > limit) limit = remainingBudget;
            }
        }

        if (waitFor.Any())
        {
            var remaining = limit;
            if (remaining > TimeSpan.Zero)
            {
                try { await Task.WhenAll(waitFor.Distinct()).WaitAsync(remaining, ct); }
                catch (TimeoutException) { /* hết hạn chờ: dùng nhịp sống hiện có */ }
            }

            ct.ThrowIfCancellationRequested();
            result = Build();
        }

        return result;
    }

    private Dictionary<string, NodeStatusOutput> Build()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        // snapshot replicator (khoá _sync của worker) lấy NGOÀI _lock ⇒ _lock không lồng khoá nào khác.
        var locals = _local();
        lock (_lock)
        {
            // WebAPI không sở hữu replicator; chỉ dùng xác nhận SQL đã lưu,
            // vẫn bắt buộc coverage, tuổi tối đa và probe sống như host ghi. Khởi động lạnh không có fallback.
            // reader-only không còn tới Build (GetAsync trả rỗng khi SQL lỗi); bỏ nhánh last-good.
            var result = new Dictionary<string, NodeStatusOutput>(StringComparer.OrdinalIgnoreCase);
            foreach (var local in locals)
            {
                _lastGood.TryGetValue(local.Node, out var confirmed);
                var state = local.State;
                if (state == QuestNodeStateEnum.Ready && confirmed?.State != QuestNodeStateEnum.Ready)
                    state = confirmed?.State is { } s && s != QuestNodeStateEnum.Ready ? s : QuestNodeStateEnum.Unknown;

                if (state == QuestNodeStateEnum.Ready)
                {
                    if (_missingSince.TryGetValue(local.Node, out var missing) && now - missing > _readyMaxLag)
                        state = QuestNodeStateEnum.CatchingUp; // thiếu dữ liệu đã ACK quá ngưỡng ⇒ không đọc
                    else if (!_covered.TryGetValue(local.Node, out var covered)
                             || (_options.FallbackMaxAge is { } maxAge && now - covered > maxAge))
                        state = QuestNodeStateEnum.Unknown; // chưa có / quá hạn mốc phủ dữ liệu
                }

                result[local.Node] = local with
                {
                    State = state,
                    HeartbeatUtc = _aliveUtc.TryGetValue(local.Node, out var alive) ? alive : DateTime.MinValue,
                    LastReconcileOk = confirmed?.LastReconcileOk,
                    LastReconcileUtc = confirmed?.LastReconcileUtc,
                };
            }

            return result;
        }
    }

    /// <summary>
    /// Ghi nhớ mốc phủ dữ liệu từ MỘT kết quả refresh thành công. Gọi trong lock. Chỉ ghi khi generation lớn hơn
    /// generation đã ghi (request cũ không ghi đè phiên bản mới; phiên bản mới không bị bỏ qua).
    /// </summary>
    private void RecordSqlReadLocked(StatusRefreshOutput r)
    {
        if (r.Generation <= _recordedGeneration) return;
        _recordedGeneration = r.Generation;
        _lastGood = r.Statuses;
        if (r.HeadReadUtc is not { } h || r.HeadSeq is not { } head)
        {
            // Không có đầu journal hợp lệ ⇒ KHÔNG nâng coverage và KHÔNG xoá mốc thiếu; nhưng status vừa đọc báo thiếu
            // vẫn là bằng chứng: ghi mốc thiếu = min(mốc cũ, thời điểm refresh − LagSeconds).
            if (!r.Succeeded) return;
            foreach (var s in r.Statuses.Values)
            {
                if (s.LastSeq >= s.MaxSeq && s.LagSeq <= 0) continue;
                var since = r.RefreshUtc - TimeSpan.FromSeconds(Math.Max(0, s.LagSeconds));
                if (!_missingSince.TryGetValue(s.Node, out var old) || since < old) _missingSince[s.Node] = since;
            }

            return;
        }
        foreach (var s in r.Statuses.Values)
        {
            if (s.LastSeq >= head)
            {
                _covered[s.Node] = h;
                _missingSince.Remove(s.Node);
            }
            else
            {
                _missingSince[s.Node] = h; // giữ mốc phủ cũ; thiếu tính từ h
            }
        }
    }

    /// <summary>
    /// Gọi trong lock. KHÔNG gọi client trong lock — <see cref="IQuestNodeClient.ProbeAsync"/> có thể chặn
    /// ĐỒNG BỘ (SqlSugar/Npgsql Open). Mỗi probe chạy NGOÀI lock trên thread riêng (<see cref="QuestOffThreadHelper.Run"/>, không bao
    /// giờ inline, không chiếm thread pool lúc chặn) và được đăng ký vào <c>_probeInflight</c> NGAY trong lock — trước khi
    /// probe kịp kết thúc (phần kết thúc của probe cũng cần lock) ⇒ vẫn đúng một probe/node in-flight.
    /// Trả các probe vừa khởi chạy (request chờ chúng ngoài lock, xem GetAsync).
    /// </summary>
    private List<Task> StartDueProbesLocked()
    {
        var started = new List<Task>();
        if (_disposed) return started;
        var now = _clock.GetUtcNow().UtcDateTime;
        foreach (var c in _clients)
        {
            var node = c.Node;
            if (!_probeInflight.ContainsKey(node)
                && (!_probeAttemptUtc.TryGetValue(node, out var last) || now - last >= _options.FallbackProbeInterval))
            {
                _probeAttemptUtc[node] = now;
                var client = c;
                var task = QuestOffThreadHelper.Run(() => ProbeAsync(client, now));
                _probeInflight[node] = task;
                started.Add(task);
            }
        }

        return started;
    }

    /// <summary>Gọi trong lock. Vòng nền probe suốt đợt dự phòng; dừng khi SQL trở lại hoặc Dispose.</summary>
    private void EnsureProbeLoopLocked()
    {
        if (_probeLoop is not null || _disposed) return;
        _probeLoop = Task.Run(ProbeLoopAsync, CancellationToken.None);
    }

    private async Task ProbeLoopAsync()
    {
        var ct = _disposeCts.Token;
        var delay = _options.FallbackProbeInterval > TimeSpan.FromMilliseconds(10)
            ? _options.FallbackProbeInterval : TimeSpan.FromMilliseconds(10);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                lock (_lock)
                {
                    if (!_usingFallback) break;
                    StartDueProbesLocked();
                }

                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* vòng nền không được chết vì lỗi lạ: bị bỏ, request kế tiếp dựng lại */ }
        finally
        {
            lock (_lock)
            {
                _probeLoop = null;
                if (_usingFallback && !_disposed) EnsureProbeLoopLocked(); // SQL lại mất ngay lúc vòng đang thoát
            }
        }
    }

    /// <param name="client">Client của node cần probe.</param>
    /// <param name="startedUtc">
    /// Thời điểm khởi chạy (theo _clock). probe thành công gia hạn nhịp sống tới mốc NÀY, không tới lúc kết thúc —
    /// probe nay chạy trên thread riêng, có thể kết thúc muộn hơn (sau khi đồng hồ đã trôi); mốc khởi chạy là cận dưới
    /// thận trọng của thời điểm node còn sống và không phụ thuộc lịch chạy thread.
    /// </param>
    private async Task ProbeAsync(IQuestNodeClient client, DateTime startedUtc)
    {
        var ok = false;
        Task<NodeProbeOutput>? probe = null;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
            cts.CancelAfter(_options.FallbackProbeInterval);
            probe = client.ProbeAsync(_probeTable, cts.Token);
            await Task.WhenAny(probe, Task.Delay(Timeout.Infinite, cts.Token)).ConfigureAwait(false);
            // client chặn đồng bộ có thể trả về SAU hạn (WhenAny chỉ chạy khi lời gọi đã trả) ⇒ quá hạn = không tính.
            if (probe.IsCompleted && !cts.IsCancellationRequested)
            {
                var r = await probe.ConfigureAwait(false);
                ok = r.TableExists;
            }
            else
            {
                _ = probe.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            }
        }
        catch (Exception)
        {
            // node không tới được / probe lỗi ⇒ không gia hạn nhịp sống
        }

        lock (_lock)
        {
            if (ok && (!_aliveUtc.TryGetValue(client.Node, out var prev) || startedUtc > prev)) _aliveUtc[client.Node] = startedUtc;
            _probeInflight.Remove(client.Node);
        }
    }
}
