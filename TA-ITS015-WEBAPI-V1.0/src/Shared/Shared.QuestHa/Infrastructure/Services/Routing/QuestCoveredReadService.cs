using System.Collections.Concurrent;
using System.Data.Common;
using System.Net.Sockets;
using SqlSugar;

namespace Shared.QuestHa.Infrastructure.Services.Routing;

/// <summary>
/// Author: KienLT
/// Description: Đường đọc có CHỨNG NHẬN COVERAGE dùng chung cho Worker và WebAPI. Mỗi request:
/// (1) chốt head journal MỘT lần (không đuổi theo append mới);
/// (2) router chọn node/lease; trên node đó chờ có giới hạn (RouterOptions.CoverageWait) tới khi cursor ≥ head, không
///     ManualFence, RecoveryPhase=None, không quarantine mở ≤ head, rồi WAL mọi bảng apply tới mốc chốt ở probe đầu;
/// (3) đọc lại coverage MỚI (bỏ cache) sau WAL — đổi thế hệ/fence/phase/cursor/quarantine ⇒ không chứng nhận.
/// Node chưa phủ ⇒ router thử node khác; hết node ⇒ NotCovered. Journal SQL lỗi ⇒ JournalUnavailable — KHÔNG dùng
/// last-good/probe sống để thay chứng nhận. Coverage của cùng node được chia sẻ tối đa 50 ms giữa request đồng thời.
/// Created date: 30/10/2026
/// </summary>
internal sealed class QuestCoveredReadService
{
    private readonly QuestNodeRouterService _router;
    private readonly IJournalStore _store;
    private readonly RouterOptions _options;
    private readonly Func<RouterOptions> _runtime;
    private readonly ConcurrentDictionary<string, CoverageCache> _coverage = new(StringComparer.OrdinalIgnoreCase);
    private readonly QuestNodeCoverageService _waiter;

    /// <summary>Dựng đường đọc có coverage trên router/journal/client/bảng của cùng cluster.</summary>
    public QuestCoveredReadService(QuestNodeRouterService router, IJournalStore store,
        IReadOnlyList<IQuestNodeClient> clients, IReadOnlyList<QuestTableSpec> tables, RouterOptions? options = null,
        Func<RouterOptions>? runtime = null)
    {
        _router = router;
        _store = store;
        _options = options ?? new RouterOptions();
        // runtime = snapshot vận hành: CoverageWait chụp một lần mỗi lượt chờ của request.
        _runtime = runtime ?? (() => _options);
        _waiter = new QuestNodeCoverageService(clients, tables);
    }

    /// <summary>
    /// Chạy <paramref name="work"/> trên lease của node đã chứng nhận phủ head chốt đầu request.
    /// Ném <see cref="QuestHaUnavailableException"/> (journal lỗi / không phủ / hết node);
    /// <see cref="OperationCanceledException"/> khi <paramref name="ct"/> bị huỷ. Lỗi khác của <paramref name="work"/> ném nguyên.
    /// </summary>
    public async Task<T> ReadAsync<T>(Func<ISqlSugarClient, Task<T>> work, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            // Head cố định; không đuổi theo các append mới trong khi chờ replicator.
            var head = (await ReadJournalAsync(() => _store.GetLagAsync(0, ct)).ConfigureAwait(false)).MaxSeq;
            return await _router.ExecuteAsync(async (lease, token) =>
            {
                await WaitCoveredAsync(lease.Node, head, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                lease.Db.Ado.CancellationToken = token;
                return await work(lease.Db).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
        catch (QuestHaUnavailableException) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }
        catch (NoQuestNodeAvailableException)
        {
            throw new QuestHaUnavailableException(QuestHaUnavailableException.NoNodeCode, TimeSpan.FromSeconds(5), 1);
        }
        catch (QuestNodeNotCoveredException)
        {
            // Thiếu coverage không chứng minh lỗi kết nối; giữ mã/RetryAfter, không báo một lỗi giả.
            throw new QuestHaUnavailableException(QuestHaUnavailableException.NotCoveredCode, TimeSpan.FromSeconds(1), 0);
        }
    }

    // Cơ chế chờ coverage dùng chung với bản xuất Parquet; ngân sách = CoverageWait chụp một lần mỗi lượt chờ.
    private Task WaitCoveredAsync(string node, long head, CancellationToken ct) =>
        _waiter.WaitCoveredAsync(node, head, _runtime().CoverageWait, (n, fresh, token) => CoverageAsync(n, token, fresh), ct);

    private async Task<NodeReadCoverageOutput> CoverageAsync(string node, CancellationToken ct, bool fresh = false)
    {
        // Request đồng thời cùng node chia sẻ một lượt đọc/vòng; tối đa 50 ms từ lúc bắt đầu đọc.
        var cache = _coverage.GetOrAdd(node, _ => new CoverageCache());
        await cache.Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var now = Environment.TickCount64;
            if (!fresh && cache.Value is not null && now - cache.Started < 50) return cache.Value;
            cache.Value = null; // lỗi không được để snapshot cũ thành fallback
            cache.Value = await ReadJournalAsync(() => _store.ReadCoverageAsync(node, ct)).ConfigureAwait(false);
            cache.Started = now;
            return cache.Value;
        }
        finally { cache.Gate.Release(); }
    }

    private static async Task<T> ReadJournalAsync<T>(Func<Task<T>> read)
    {
        try { return await read().ConfigureAwait(false); }
        // Chỉ bọc I/O journal, trước khi router có thể hiểu nhầm lỗi SQL là lỗi node (không gắn inner exception).
        // I/O journal thắng deadline coverage trùng lúc; huỷ request được xử lý ở ReadAsync.
        catch (Exception ex) when (ex is SqlSugarException or DbException or TimeoutException or IOException or SocketException or OperationCanceledException)
        {
            throw new QuestHaUnavailableException(QuestHaUnavailableException.JournalUnavailableCode, TimeSpan.FromSeconds(5), 1);
        }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Khoá/cache coverage theo node; số mục bị chặn bởi tập node cấu hình.
    /// Created date: 29/10/2026
    /// </summary>
    private sealed class CoverageCache
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public long Started;
        public NodeReadCoverageOutput? Value;
    }
}
