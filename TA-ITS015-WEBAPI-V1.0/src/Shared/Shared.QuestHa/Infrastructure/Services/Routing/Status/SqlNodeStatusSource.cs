using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shared.QuestHa.Infrastructure.Services.Routing.Status;

/// <summary>
/// Author: KienLT
/// Description: Đọc QuestNodeStatus có cache (cache 2 s). Lượt làm mới SQL chạy với token RIÊNG (timeout
/// <c>StatusRefreshTimeout</c>), không gắn vào token của request đọc:
/// request bị huỷ ⇒ ném <see cref="OperationCanceledException"/> nhưng refresh vẫn tự kết thúc và ghi nhận
/// đúng thành công/lỗi cho request sau; refresh timeout/huỷ luôn là LỖI (không bao giờ cập nhật như thành công).
/// Khi lần đọc gần nhất đã lỗi, request KHÔNG chờ SQL nữa (refresh chạy nền) ⇒ nguồn dự phòng dùng được ngay.
/// Mỗi lượt đọc cả status lẫn đầu journal đã commit (<see cref="IJournalStore.GetLagAsync"/>) trong CÙNG ngân sách;
/// kết quả gói vào <see cref="StatusRefreshOutput"/>.
/// Created date: 09/10/2026
/// </summary>
internal sealed class SqlNodeStatusSource : INodeStatusSource
{
    private readonly IJournalStore _store;
    private readonly TimeSpan _ttl;
    private readonly TimeSpan _refreshTimeout;
    private readonly TimeProvider _clock;
    private readonly object _lock = new();
    private StatusRefreshOutput _result = new(0,
        new Dictionary<string, NodeStatusOutput>(StringComparer.OrdinalIgnoreCase), true, null, null);
    private DateTime _attemptUtc = DateTime.MinValue;
    private Task? _inflight;
    // thao tác store THẬT của lượt refresh gần nhất (không phải wrapper có hạn). Lượt refresh hết hạn vẫn publish
    // lỗi đúng ngân sách, nhưng KHÔNG mở lượt mới khi thao tác cũ còn chạy (store chặn đồng bộ / bỏ qua token) ⇒ tối đa
    // 2 thao tác store đang bay, không tích luỹ thread theo số lượt TTL.
    private Task? _ops;

    /// <summary>Kết quả publish gần nhất (đọc nguyên tử một object).</summary>
    public StatusRefreshOutput Current => Volatile.Read(ref _result);

    /// <summary>false = lần đọc SQL gần nhất lỗi/timeout (cache đang là bản cũ). true = đọc được (hoặc chưa đọc lần nào).</summary>
    public bool LastFetchSucceeded => Current.Succeeded;

    /// <summary>Khởi tạo nguồn trạng thái đọc QuestNodeStatus có cache TTL và hạn refresh.</summary>
    public SqlNodeStatusSource(IJournalStore store, TimeSpan ttl, TimeProvider? clock = null, TimeSpan? refreshTimeout = null)
    {
        _store = store;
        _ttl = ttl;
        _refreshTimeout = refreshTimeout ?? TimeSpan.FromSeconds(2);
        _clock = clock ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, NodeStatusOutput>> GetAsync(CancellationToken ct = default) =>
        (await GetResultAsync(ct)).Statuses;

    /// <summary>Lấy MỘT kết quả refresh nhất quán (chờ lượt đang chạy nếu SQL đang được coi là khoẻ).</summary>
    public async Task<StatusRefreshOutput> GetResultAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Task? wait = null;
        lock (_lock)
        {
            if (_inflight is null && _clock.GetUtcNow().UtcDateTime - _attemptUtc >= _ttl)
            {
                if (_ops is null || _ops.IsCompleted)
                {
                    _inflight = Task.Run(RefreshAsync, CancellationToken.None);
                }
                else if (_result.Succeeded)
                {
                    // quá TTL mà thao tác store cũ vẫn treo ⇒ không mở lượt mới; KHÔNG để kết quả thành công cũ
                    // được dùng mãi — publish lỗi (giữ statuses cũ) để nguồn dự phòng quyết định ngay.
                    var prev = _result;
                    Volatile.Write(ref _result, new StatusRefreshOutput(prev.Generation + 1, prev.Statuses, false, null, null));
                }
            }
            // SQL đang được coi là khoẻ ⇒ chờ lượt làm mới (tối đa _refreshTimeout vì refresh tự giới hạn).
            // SQL đã lỗi ⇒ không chờ: trả kết quả cũ + cờ lỗi để nguồn dự phòng quyết định ngay.
            if (_inflight is not null && _result.Succeeded) wait = _inflight;
        }

        if (wait is not null) await wait.WaitAsync(ct);
        return Current;
    }

    private static async Task<T> Bounded<T>(Task<T> task, CancellationToken budget)
    {
        // Store không tôn trọng token vẫn không giữ được router quá ngân sách.
        await Task.WhenAny(task, Task.Delay(Timeout.Infinite, budget)).ConfigureAwait(false);
        if (!task.IsCompleted)
        {
            _ = task.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
            throw new TimeoutException("Đọc journal vượt StatusRefreshTimeout.");
        }

        return await task.ConfigureAwait(false);
    }

    private async Task RefreshAsync()
    {
        using var cts = new CancellationTokenSource(_refreshTimeout);
        Task<IReadOnlyList<NodeStatusOutput>>? statusTask = null;
        Task<NodeLagOutput>? headTask = null;
        DateTime? headAt = null;
        try
        {
            try
            {
                // store có thể chặn ĐỒNG BỘ trước await đầu (SqlSugar CheckConnection → Open) ⇒ chạy trên thread riêng để
                // Bounded thực sự giới hạn lượt refresh trong StatusRefreshTimeout (request đang chờ _inflight không bị kéo dài).
                var statusOp = QuestOffThreadHelper.Run(() => _store.ReadStatusesAsync(cts.Token));
                headAt = _clock.GetUtcNow().UtcDateTime; // ngay TRƯỚC khi đọc đầu journal: mọi ACK trước mốc này đều ≤ MaxSeq đọc được sau đó
                var headOp = QuestOffThreadHelper.Run(() => _store.GetLagAsync(0, cts.Token));
                var ops = Task.WhenAll(statusOp, headOp);
                _ = ops.ContinueWith(t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted); // lỗi muộn được quan sát
                lock (_lock) _ops = ops;
                statusTask = Bounded(statusOp, cts.Token);
                headTask = Bounded(headOp, cts.Token);
            }
            catch (Exception)
            {
                // store ném đồng bộ: coi như lượt lỗi (task chưa tạo = null)
            }

            var pending = new List<Task>();
            if (statusTask is not null) pending.Add(statusTask);
            if (headTask is not null) pending.Add(headTask);
            try { await Task.WhenAll(pending).ConfigureAwait(false); } catch { /* kiểm từng task bên dưới */ }

            var prev = Current;
            var refreshUtc = _clock.GetUtcNow().UtcDateTime;
            if (statusTask is { Status: TaskStatus.RanToCompletion })
            {
                var map = statusTask.Result.ToDictionary(r => r.Node, StringComparer.OrdinalIgnoreCase);
                var head = headTask is { Status: TaskStatus.RanToCompletion } ? headTask.Result : null;
                // Đọc đầu journal lỗi ⇒ statuses vẫn dùng được cho trạng thái nhưng KHÔNG chứng nhận coverage.
                Volatile.Write(ref _result, new StatusRefreshOutput(prev.Generation + 1, map, true,
                    head is null ? null : headAt, head?.MaxSeq, refreshUtc));
            }
            else
            {
                // Journal (SQL) chết/chậm/refresh bị huỷ: giữ statuses cũ và đánh dấu lỗi.
                Volatile.Write(ref _result, new StatusRefreshOutput(prev.Generation + 1, prev.Statuses, false, null, null));
            }
        }
        finally
        {
            lock (_lock)
            {
                _attemptUtc = _clock.GetUtcNow().UtcDateTime;
                _inflight = null;
            }
        }
    }
}
