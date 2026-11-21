using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.QuestHa.Infrastructure.Services.Cluster;

/// <summary>
/// Author: KienLT
/// Description: Facade cho dashboard/host: mọi thành phần Core của 1 cụm (journal + N node).
/// Created date: 09/10/2026
/// </summary>
internal sealed class QuestHaClusterService : IDisposable
{
    private readonly IDisposable? _owned;
    private readonly Dictionary<string, QuestReplicatorService> _replicators;
    private readonly Dictionary<string, IQuestNodeClient> _clients;

    /// <summary>Gom các thành phần dùng chung của một cluster QuestHa (journal, replicator, router, reconciler, backup).</summary>
    public QuestHaClusterService(
        IJournalStore store,
        IReadOnlyList<QuestReplicatorService> replicators,
        IReadOnlyList<IQuestNodeClient> clients,
        IReadOnlyList<QuestTableSpec> tables,
        QuestNodeRouterService router,
        QuestReconcilerService reconciler,
        QuestBackupService backup,
        QuestHaTimelineService timeline,
        QuestHaOptions options,
        IDisposable? owned = null)
    {
        _owned = owned;
        Store = store;
        Replicators = replicators;
        NodeClients = clients;
        Tables = tables;
        Router = router;
        Reconciler = reconciler;
        Backup = backup;
        Timeline = timeline;
        Options = options;
        _replicators = replicators.ToDictionary(r => r.Node, StringComparer.OrdinalIgnoreCase);
        _clients = clients.ToDictionary(c => c.Node, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Dừng vòng probe nền của nguồn dự phòng router (nếu có).</summary>
    public void Dispose() => _owned?.Dispose();

    public IJournalStore Store { get; }

    public IReadOnlyList<QuestReplicatorService> Replicators { get; }

    public IReadOnlyList<IQuestNodeClient> NodeClients { get; }

    public IReadOnlyList<QuestTableSpec> Tables { get; }

    public QuestNodeRouterService Router { get; }

    public QuestReconcilerService Reconciler { get; }

    public QuestBackupService Backup { get; }

    public QuestHaTimelineService Timeline { get; }

    public QuestHaOptions Options { get; }

    /// <summary>Replicator của node theo tên (ném lỗi nếu không có).</summary>
    public QuestReplicatorService Replicator(string node) =>
        _replicators.TryGetValue(node, out var r) ? r : throw new KeyNotFoundException($"Node '{node}' không tồn tại.");

    /// <summary>Client QuestDB của node theo tên (ném lỗi nếu không có).</summary>
    public IQuestNodeClient NodeClient(string node) =>
        _clients.TryGetValue(node, out var c) ? c : throw new KeyNotFoundException($"Node '{node}' không tồn tại.");

    /// <summary>Append rồi đánh thức mọi replicator (giảm trễ giao xuống dưới PollInterval).</summary>
    public async Task<JournalAppendOutput> AppendAndWakeAsync(QuestTableSpec table, object payload, CancellationToken ct = default)
    {
        var ack = await Store.AppendAsync(table, payload, ct);
        foreach (var r in Replicators) r.Wake();
        return ack;
    }
}
