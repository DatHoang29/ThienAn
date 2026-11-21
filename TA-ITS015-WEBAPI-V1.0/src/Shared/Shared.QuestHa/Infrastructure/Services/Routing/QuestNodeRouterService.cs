using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Infrastructure.Services.Routing;

/// <summary>
/// Author: KienLT
/// Description: Router đọc có failover.
/// <list type="bullet">
/// <item>Node "đủ điều kiện" = trạng thái Ready (QuestNodeStatus, heartbeat còn tươi) VÀ breaker cục bộ không chặn.</item>
/// <item><b>Dính</b> node hiện tại; chỉ đổi khi node đó hết đủ điều kiện hoặc lỗi kết nối.</item>
/// <item>Lỗi kết nối trong request ⇒ trip breaker node đó, thử lại ĐÚNG 1 lần trên node đủ điều kiện kế tiếp.
///   Lỗi khác (SQL sai…) ném thẳng, không retry.</item>
/// <item><b>Failback</b> về node ưu tiên hơn chỉ khi router QUAN SÁT thấy nó đủ điều kiện liên tục ≥
///   FailbackStableFor VÀ lượt đối soát gần nhất (diễn ra SAU mốc bắt đầu Ready đó) đạt.</item>
/// </list>
/// "Ready liên tục" đo bằng đồng hồ của router (không tin ReadySinceUtc ghi từ máy khác) ⇒ không phụ thuộc lệch giờ.
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestNodeRouterService
{
    private readonly object _sync = new();
    private readonly IReadOnlyList<QuestNodeOptions> _nodes;
    private readonly Dictionary<string, QuestNodeBreaker> _breakers;
    private readonly Dictionary<string, DateTime?> _eligibleSince;
    private readonly Func<string, CancellationToken, Task<QuestNodeLease>> _leaseFactory;
    private readonly INodeStatusSource _status;
    private readonly RouterOptions _options;
    // CoverageWait/FailbackStableFor/BreakerCooldown đọc từ snapshot vận hành mỗi lượt chọn/lỗi; phần còn lại là cấu hình khởi động.
    private readonly Func<RouterOptions> _runtime;
    private readonly TimeProvider _clock;
    private readonly QuestHaTimelineService? _timeline;
    private readonly ILogger _logger;
    // callback tới đúng client/node, dùng chung hạn mức xoá pool với replicator cùng host.
    private readonly Action<string>? _resetConnectionPool;
    // callback có lý do, vẫn giữ overload callback cũ cho host/test hiện có.
    private readonly Action<string, string>? _resetConnectionPoolWithReason;
    private readonly LinkedList<NodeSwitchedOutput> _switches = new();
    private IReadOnlyDictionary<string, NodeStatusOutput> _lastStatuses =
        new Dictionary<string, NodeStatusOutput>(StringComparer.OrdinalIgnoreCase);

    private string? _current;
    private long _requests;
    private long _failures;
    private long _retries;

    // hai overload nhận callback vô hiệu pool đúng client/node của host.
    /// <summary>Lease dựng đồng bộ (test, client giả) — không mở kết nối trước.</summary>
    public QuestNodeRouterService(
        IReadOnlyList<QuestNodeOptions> nodes,
        Func<string, QuestNodeLease> leaseFactory,
        INodeStatusSource status,
        RouterOptions options,
        TimeProvider? clock = null,
        QuestHaTimelineService? timeline = null,
        ILogger<QuestNodeRouterService>? logger = null,
        Action<string>? resetConnectionPool = null, Action<string, string>? resetConnectionPoolWithReason = null,
        Func<RouterOptions>? runtime = null)
        : this(nodes, (node, _) => Task.FromResult(leaseFactory(node)), status, options, clock, timeline, logger, resetConnectionPool, resetConnectionPoolWithReason, runtime)
    {
    }

    /// <summary>
    /// lease mở kết nối bất đồng bộ (<see cref="IQuestNodeClient.OpenLeaseAsync"/>) — lỗi mở tính là lỗi
    /// kết nối của node đó (trip breaker, thử node kế tiếp) như lỗi trong op.
    /// </summary>
    public QuestNodeRouterService(
        IReadOnlyList<QuestNodeOptions> nodes,
        Func<string, CancellationToken, Task<QuestNodeLease>> leaseFactory,
        INodeStatusSource status,
        RouterOptions options,
        TimeProvider? clock = null,
        QuestHaTimelineService? timeline = null,
        ILogger<QuestNodeRouterService>? logger = null,
        Action<string>? resetConnectionPool = null, Action<string, string>? resetConnectionPoolWithReason = null,
        Func<RouterOptions>? runtime = null)
    {
        if (nodes.Count == 0) throw new ArgumentException("At least one node is required.", nameof(nodes));
        _nodes = nodes.OrderBy(n => n.Priority).ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase).ToList();
        _breakers = _nodes.ToDictionary(n => n.Name, _ => new QuestNodeBreaker(), StringComparer.OrdinalIgnoreCase);
        _eligibleSince = _nodes.ToDictionary(n => n.Name, _ => (DateTime?)null, StringComparer.OrdinalIgnoreCase);
        _leaseFactory = leaseFactory;
        _status = status;
        _options = options;
        _runtime = runtime ?? (() => _options);
        _clock = clock ?? TimeProvider.System;
        _timeline = timeline;
        _logger = logger ?? (ILogger)NullLogger.Instance;
        _resetConnectionPool = resetConnectionPool;
        _resetConnectionPoolWithReason = resetConnectionPoolWithReason;
    }

    /// <summary>Đổi node đọc (gồm cả lần chọn đầu tiên, From = null).</summary>
    public event EventHandler<NodeSwitchedOutput>? NodeSwitched;

    public string? CurrentNode
    {
        get { lock (_sync) return _current; }
    }

    /// <summary>
    /// Chạy <paramref name="op"/> trên node đang chọn. Lỗi kết nối ⇒ thử lại 1 lần trên node khác.
    /// Không còn node đủ điều kiện ⇒ <see cref="NoQuestNodeAvailableException"/>.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<QuestNodeLease, CancellationToken, Task<T>> op, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _requests);
        var statuses = await _status.GetAsync(ct);

        var first = Choose(statuses, exclude: null);
        if (first is null)
        {
            Interlocked.Increment(ref _failures);
            throw new NoQuestNodeAvailableException("Không có node QuestDB nào Ready để đọc.");
        }

        try
        {
            return await RunOn(first, op, ct);
        }
        // node thiếu coverage vẫn sống; thử node khác nhưng không trip breaker trong RunOn.
        catch (Exception ex) when ((QuestErrorClassifierHelper.IsConnectivity(ex) || ex is QuestNodeNotCoveredException) && !ct.IsCancellationRequested)
        {
            var second = Choose(statuses, exclude: first, reason: ex is QuestNodeNotCoveredException ? $"{first} chưa phủ dữ liệu" : $"{first} lỗi kết nối");
            if (second is null)
            {
                Interlocked.Increment(ref _failures);
                // giữ nghĩa thiếu coverage khi không còn node khác để thử.
                if (ex is QuestNodeNotCoveredException) throw;
                throw new NoQuestNodeAvailableException($"Node {first} lỗi kết nối và không còn node Ready khác.", ex);
            }

            Interlocked.Increment(ref _retries);
            try
            {
                return await RunOn(second, op, ct);
            }
            catch
            {
                Interlocked.Increment(ref _failures);
                throw;
            }
        }
        catch
        {
            Interlocked.Increment(ref _failures);
            throw;
        }
    }

    private async Task<T> RunOn<T>(string node, Func<QuestNodeLease, CancellationToken, Task<T>> op, CancellationToken ct)
    {
        var breaker = _breakers[node];
        if (!breaker.TryEnter(_clock.GetUtcNow().UtcDateTime))
            throw new TimeoutException($"Breaker của node {node} đang chặn."); // ⇒ coi như lỗi kết nối, thử node khác

        try
        {
            T result;
            await using (var lease = await _leaseFactory(node, ct))
                result = await op(lease, ct);
            breaker.ReportSuccess();
            return result;
        }
        catch (Exception ex) when (QuestErrorClassifierHelper.IsConnectivity(ex))
        {
            var cooldown = _runtime().BreakerCooldown;
            breaker.ReportConnectivityFailure(_clock.GetUtcNow().UtcDateTime, cooldown);
            // bỏ socket cũ còn nằm trong pool; lỗi thật vẫn trip/retry theo luật cũ.
            ResetPool(node, "router lỗi kết nối");
            lock (_sync) _eligibleSince[node] = null;
            // cảnh báo không chứa SQL/PG provider text hoặc chuỗi kết nối.
            _logger.LogSafeWarning(ex, "[QuestHa.Router] Node {Node} lỗi kết nối, breaker trip {Cooldown}s.", node, cooldown.TotalSeconds);
            throw;
        }
        catch
        {
            breaker.ReportUnrelatedFailure();
            throw;
        }
    }

    // lựa chọn có side effect vô hiệu pool, không còn là hàm thuần.
    /// <summary>
    /// Chọn node có khoá; vô hiệu pool khi phát hiện đổi kết nối. <paramref name="exclude"/> = node vừa lỗi trong request này.
    /// Internal để test state machine.
    /// </summary>
    internal string? Choose(IReadOnlyDictionary<string, NodeStatusOutput> statuses, string? exclude, string? reason = null)
    {
        NodeSwitchedOutput? switched = null;
        string? pick;
        // Một ngưỡng failback cho cả lượt chọn.
        var stableFor = _runtime().FailbackStableFor;
        lock (_sync)
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            foreach (var n in _nodes)
            {
                var ok = IsEligible(n.Name, statuses, now);
                // reader có thể ở tiến trình khác jrepl; tự xoá pool khi thấy Down/stale,
                // đổi mốc Ready lưu SQL/lượt rewind cục bộ hoặc vừa đủ điều kiện lại (kể cả bỏ lỡ heartbeat Down).
                statuses.TryGetValue(n.Name, out var status);
                _lastStatuses.TryGetValue(n.Name, out var previous);
                if (status is not null &&
                    (status.State == QuestNodeStateEnum.Down || !IsFresh(status, now)
                     || previous is not null && (status.RewindCount != previous.RewindCount
                         || status.ReadySinceUtc is not null && status.ReadySinceUtc != previous.ReadySinceUtc)
                     || ok && previous is not null && _eligibleSince[n.Name] is null))
                    ResetPool(n.Name, status.State == QuestNodeStateEnum.Down ? "router thấy Down"
                        : !IsFresh(status, now) ? "router heartbeat cũ"
                        : status.ReadySinceUtc is not null && status.ReadySinceUtc != previous?.ReadySinceUtc ? "ReadySince đổi"
                        : status.RewindCount != previous?.RewindCount ? "RewindCount đổi" : "router reconnect/đủ điều kiện trở lại");
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    if (string.Equals(n.Name, exclude, StringComparison.OrdinalIgnoreCase))
                        _logger.LogDebug("[QuestHa.Router] Loại {Node}: đã thử trong request này ({Reason}).", n.Name, reason);
                    else if (!ok)
                        _logger.LogDebug("[QuestHa.Router] Loại {Node}: {Reason}.", n.Name, DescribeState(n.Name, statuses, now));
                }
                if (!ok) _eligibleSince[n.Name] = null;
                else _eligibleSince[n.Name] ??= now;
            }
            _lastStatuses = statuses;

            var current = _current;
            var currentOk = current is not null
                            && !string.Equals(current, exclude, StringComparison.OrdinalIgnoreCase)
                            && _eligibleSince[current] is not null;

            if (currentOk)
            {
                pick = current;
                // Failback: node ưu tiên hơn current, đủ điều kiện ổn định + đối soát đạt.
                var curPrio = PriorityOf(current!);
                var better = _nodes.FirstOrDefault(n => n.Priority < curPrio
                                                        && !string.Equals(n.Name, exclude, StringComparison.OrdinalIgnoreCase)
                                                        && IsFailbackEligible(n.Name, statuses, now, stableFor));
                if (better is not null)
                {
                    pick = better.Name;
                    reason ??= $"failback: {better.Name} Ready ≥ {stableFor.TotalSeconds:0}s và đối soát đạt";
                }
            }
            else
            {
                pick = _nodes
                    .Where(n => !string.Equals(n.Name, exclude, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(n => _eligibleSince[n.Name] is not null)?.Name;

                if (pick is null && _options.AllowDegradedRead)
                {
                    pick = _nodes
                        .Where(n => !string.Equals(n.Name, exclude, StringComparison.OrdinalIgnoreCase)
                                    && !_breakers[n.Name].IsBlocked(now)
                                    && statuses.TryGetValue(n.Name, out var s)
                                    && s.State == QuestNodeStateEnum.CatchingUp
                                    && IsFresh(s, now))
                        .OrderBy(n => statuses[n.Name].LagSeq)
                        .FirstOrDefault()?.Name;
                    if (pick is not null) reason ??= $"degraded: không có node Ready, đọc tạm {pick} (CatchingUp)";
                }

                if (reason is null && current is not null)
                    reason = $"{current} hết Ready ({DescribeState(current, statuses, now)})";
            }

            if (pick is not null && !string.Equals(pick, current, StringComparison.OrdinalIgnoreCase))
            {
                switched = new NodeSwitchedOutput(now, current, pick, reason ?? "chọn node đầu tiên");
                _current = pick;
                _switches.AddFirst(switched);
                while (_switches.Count > Math.Max(1, _options.SwitchHistorySize)) _switches.RemoveLast();
            }
        }

        if (switched is not null)
        {
            _logger.LogInformation("[QuestHa.Router] {From} → {To}: {Reason}", switched.From ?? "-", switched.To, switched.Reason);
            _timeline?.Publish("Router", switched.To, QuestHaEventKindEnum.RouterSwitched,
                $"Reader: {switched.From ?? "-"} → {switched.To} ({switched.Reason})");
            try { NodeSwitched?.Invoke(this, switched); } catch { /* listener lỗi không ảnh hưởng đọc */ }
        }

        return pick;
    }

    private int PriorityOf(string node) => _nodes.First(n => string.Equals(n.Name, node, StringComparison.OrdinalIgnoreCase)).Priority;

    // lý do là nhãn cố định, không đọc chuỗi kết nối hoặc thông điệp provider.
    private void ResetPool(string node, string reason)
    {
        if (_resetConnectionPoolWithReason is not null) _resetConnectionPoolWithReason(node, reason);
        else _resetConnectionPool?.Invoke(node);
    }

    private bool IsFresh(NodeStatusOutput s, DateTime now) => now - s.HeartbeatUtc <= _options.HeartbeatStaleAfter;

    private bool IsEligible(string node, IReadOnlyDictionary<string, NodeStatusOutput> statuses, DateTime now) =>
        statuses.TryGetValue(node, out var s)
        && s.State == QuestNodeStateEnum.Ready
        && IsFresh(s, now)
        && !_breakers[node].IsBlocked(now);

    private bool IsFailbackEligible(string node, IReadOnlyDictionary<string, NodeStatusOutput> statuses, DateTime now, TimeSpan stableFor)
    {
        var since = _eligibleSince[node];
        if (since is null || now - since.Value < stableFor)
        {
            // giải thích ứng viên Ready chưa được failback, chỉ dựng log khi Debug bật.
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("[QuestHa.Router] Loại failback {Node}: Ready chưa ổn định đủ {StableSeconds}s.", node, stableFor.TotalSeconds);
            return false;
        }
        if (!_options.RequireReconcileForFailback) return true;
        var reconciled = statuses.TryGetValue(node, out var s)
               && s.LastReconcileOk == true
               && s.LastReconcileUtc is not null
               && s.LastReconcileUtc.Value >= since.Value;
        if (!reconciled && _logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("[QuestHa.Router] Loại failback {Node}: chưa có đối soát OK sau mốc Ready.", node);
        return reconciled;
    }

    private string DescribeState(string node, IReadOnlyDictionary<string, NodeStatusOutput> statuses, DateTime now)
    {
        if (_breakers[node].IsBlocked(now)) return "breaker trip";
        if (!statuses.TryGetValue(node, out var s)) return "chưa có trạng thái";
        if (!IsFresh(s, now)) return "heartbeat quá cũ";
        return s.State.ToString();
    }

    /// <summary>Ảnh chụp router cho dashboard (không chờ I/O).</summary>
    public RouterSnapshotOutput GetSnapshot()
    {
        // đọc nguồn dự phòng (khoá _lock riêng) TRƯỚC khi vào _sync ⇒ router không lồng khoá của nguồn trạng thái.
        var usingFallback = _status.UsingFallback;
        var covered = usingFallback
            ? _nodes.ToDictionary(n => n.Name, n => _status.CoveredUntil(n.Name), StringComparer.OrdinalIgnoreCase)
            : null;
        var stableFor = _runtime().FailbackStableFor;
        lock (_sync)
        {
            var now = _clock.GetUtcNow().UtcDateTime;
            var views = _nodes.Select(n =>
            {
                _lastStatuses.TryGetValue(n.Name, out var s);
                var state = s is null ? QuestNodeStateEnum.Unknown : (IsFresh(s, now) ? s.State : QuestNodeStateEnum.Down);
                return new RouterNodeOutput(n.Name, n.Priority, state, _breakers[n.Name].IsBlocked(now),
                    _eligibleSince[n.Name], IsFailbackEligible(n.Name, _lastStatuses, now, stableFor),
                    usingFallback, covered?[n.Name]);
            }).ToList();

            return new RouterSnapshotOutput(_current, _nodes[0].Name, views, _switches.ToList(),
                Interlocked.Read(ref _requests), Interlocked.Read(ref _failures), Interlocked.Read(ref _retries));
        }
    }
}
