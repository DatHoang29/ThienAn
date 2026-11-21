namespace Shared.QuestHa.Infrastructure.Services.Replication.Rules;

/// <summary>
/// Author: KienLT
/// Description: Đánh giá tuổi mốc WAL theo từng bảng; không cần writer đuổi kịp sequencer đang ghi liên tục.
/// Created date: 29/10/2026
/// </summary>
internal static class QuestWalProgressRules
{
    // giữ hữu hạn mốc; không trẻ hoá backlog khi writer vừa phủ một mốc đã quá hạn.
    private const int MaxCheckpoints = 64;

    /// <summary>
    /// Author: KienLT
    /// Description: Mốc sequencer đã quan sát cùng tuổi sớm nhất của phần WAL chờ áp.
    /// Created date: 29/10/2026
    /// </summary>
    internal readonly record struct Checkpoint(long Txn, DateTime Since);

    /// <summary>
    /// Author: KienLT
    /// Description: Các mốc chờ writer phủ, kèm bằng chứng đã áp của thế hệ kết nối hiện tại.
    /// Created date: 29/10/2026
    /// </summary>
    internal sealed record Progress(long Writer, long Sequencer, IReadOnlyList<Checkpoint> Pending,
        DateTime ProbedAt, bool Confirmed, bool Valid);

    /// <summary>Nhận mẫu mới bằng đồng hồ caller; transaction lùi hoặc bảng lỗi làm mất bằng chứng cũ.</summary>
    internal static Progress Observe(Progress? previous, NodeProbeOutput probe, DateTime now, DateTime? writtenSince)
    {
        var valid = probe.TableExists && !probe.WalSuspended && probe.WriterTxn >= 0 && probe.SequencerTxn >= 0;
        if (previous is not { Valid: true } || probe.WriterTxn < previous.Writer || probe.SequencerTxn < previous.Sequencer)
            previous = null;

        var applied = probe.WriterTxn >= probe.SequencerTxn;
        var reached = previous is not null && probe.WriterTxn > previous.Writer && previous.Pending.Any(p => p.Txn <= probe.WriterTxn);
        var pending = previous?.Pending.Where(p => p.Txn > probe.WriterTxn).ToList() ?? new List<Checkpoint>();
        if (!applied && (pending.Count == 0 || pending[^1].Txn < probe.SequencerTxn))
        {
            var since = previous is null || previous.Writer >= previous.Sequencer ? writtenSince ?? now : now;
            pending.Add(new(probe.SequencerTxn, since));
        }
        if (pending.Count > MaxCheckpoints)
        {
            // Gộp hai mốc già nhất: giữ tuổi cũ, đòi writer phủ mốc cao hơn; chỉ có thể chặn bảo thủ.
            pending[1] = pending[1] with { Since = pending[0].Since };
            pending.RemoveAt(0);
        }
        return new(probe.WriterTxn, probe.SequencerTxn, pending, now,
            valid && (previous?.Confirmed == true || applied || reached), valid);
    }

    /// <summary>Chỉ sẵn sàng khi đã thấy apply, probe còn mới và mốc WAL đang chờ chưa quá ngưỡng.</summary>
    internal static bool IsReady(Progress progress, DateTime now, TimeSpan maxLag) =>
        progress.Valid && progress.Confirmed && now - progress.ProbedAt <= maxLag &&
        (progress.Pending.Count == 0 || now - progress.Pending[0].Since <= maxLag);
}
