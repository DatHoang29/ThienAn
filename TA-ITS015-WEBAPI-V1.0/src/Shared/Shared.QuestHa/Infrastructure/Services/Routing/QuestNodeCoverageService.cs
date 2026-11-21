namespace Shared.QuestHa.Infrastructure.Services.Routing;

/// <summary>
/// Author: KienLT
/// Description: Chứng nhận COVERAGE một node so với head journal đã chốt — một cơ chế dùng chung cho đường đọc có coverage
/// (<see cref="QuestCoveredReadService"/>) và bản xuất Parquet: chờ có giới hạn tới khi cursor ≥ head, không ManualFence,
/// RecoveryPhase=None, không quarantine mở ≤ head, rồi WAL mọi bảng apply tới mốc chốt ở probe đầu; đọc lại coverage MỚI sau WAL —
/// đổi thế hệ/fence/phase/cursor/quarantine ⇒ không chứng nhận. Không phủ trong ngân sách ⇒ <see cref="QuestNodeNotCoveredException"/>.
/// Created date: 02/11/2026
/// </summary>
internal sealed class QuestNodeCoverageService
{
    private readonly IReadOnlyList<IQuestNodeClient> _clients;
    private readonly IReadOnlyList<QuestTableSpec> _tables;

    /// <summary>Dựng trên client node và bảng của cùng cluster.</summary>
    public QuestNodeCoverageService(IReadOnlyList<IQuestNodeClient> clients, IReadOnlyList<QuestTableSpec> tables)
    {
        _clients = clients;
        _tables = tables;
    }

    /// <summary>
    /// Chờ <paramref name="node"/> phủ <paramref name="head"/> trong <paramref name="budget"/>. <paramref name="coverage"/> đọc trạng thái
    /// phục hồi/cursor/quarantine của node (tham số thứ hai = bỏ cache). Hết ngân sách / fence / phục hồi / quarantine ≤ head ⇒
    /// <see cref="QuestNodeNotCoveredException"/>; huỷ <paramref name="ct"/> ⇒ <see cref="OperationCanceledException"/>.
    /// </summary>
    public async Task WaitCoveredAsync(string node, long head, TimeSpan budget,
        Func<string, bool, CancellationToken, Task<NodeReadCoverageOutput>> coverage, CancellationToken ct)
    {
        if (budget <= TimeSpan.Zero) throw new QuestNodeNotCoveredException();
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(budget);
        var token = limit.Token;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var recovery = await coverage(node, false, token).ConfigureAwait(false);
                if (recovery.ManualFence || recovery.Phase != NodeRecoveryPhaseEnum.None)
                    throw new QuestNodeNotCoveredException();
                if (recovery.LastSeq >= head)
                {
                    if (recovery.FirstQuarantinedSeq is { } first && first <= head)
                        throw new QuestNodeNotCoveredException();
                    var client = _clients.Single(c => c.Node.Equals(node, StringComparison.OrdinalIgnoreCase));
                    var walApplied = true;
                    foreach (var table in _tables)
                    {
                        // cursor ≥ head đã đọc TRƯỚC probe đầu ⇒ mọi row ≤ head đã ở WAL lúc đó, mốc sequencer chốt ở
                        // probe đầu bao chúng. Chờ trong cả ngân sách (token chặn tổng thời gian); không chia lượt ngắn vì mỗi
                        // lượt mới lại chốt mốc mới và có thể không bao giờ kịp khi WAL apply trễ đều.
                        if (!await client.WaitWalAppliedAsync(table, budget, token).ConfigureAwait(false))
                        {
                            walApplied = false;
                            break;
                        }
                    }
                    if (walApplied)
                    {
                        // Không bỏ qua fence/rewind/quarantine xuất hiện trong lúc chờ WAL: chốt sau WAL luôn đọc mới.
                        var finalRecovery = await coverage(node, true, token).ConfigureAwait(false);
                        if (finalRecovery.Generation != recovery.Generation || finalRecovery.ManualFence ||
                            finalRecovery.Phase != NodeRecoveryPhaseEnum.None || finalRecovery.LastSeq < head ||
                            finalRecovery.FirstQuarantinedSeq is { } finalFirst && finalFirst <= head)
                            throw new QuestNodeNotCoveredException();
                        token.ThrowIfCancellationRequested();
                        return;
                    }
                }
                await Task.Delay(TimeSpan.FromMilliseconds(50), token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new QuestNodeNotCoveredException();
        }
    }
}
