namespace Shared.QuestHa.Infrastructure.Services.Node.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật chờ WAL theo MỐC CỐ ĐỊNH: probe đầu chốt target = sequencerTxn của chính probe đó; các probe sau
/// chỉ cần writerTxn ≥ target. Không đòi WAL của các append đến sau target cũng rỗng — dưới tải ghi liên tục, so với
/// sequencerTxn mới của từng probe có thể không bao giờ đúng dù mọi transaction cũ đã apply.
/// Txn lùi so với probe đầu (node restart/restore), bảng mất hoặc WAL suspended ⇒ không chứng nhận.
/// Created date: 30/10/2026
/// </summary>
internal static class QuestWalWaitRules
{
    /// <summary>
    /// Author: KienLT
    /// Description: Mốc chốt ở probe đầu: target sequencer và writer/sequencer ban đầu để phát hiện txn lùi.
    /// Created date: 30/10/2026
    /// </summary>
    internal readonly record struct Baseline(long Target, long FirstWriter, long FirstSequencer);

    /// <summary>
    /// Author: KienLT
    /// Description: Kết quả xét một probe khi chờ WAL.
    /// Created date: 30/10/2026
    /// </summary>
    internal enum Decision
    {
        /// <summary>writerTxn đã phủ target.</summary>
        Applied,
        /// <summary>Chưa phủ, probe tiếp nếu còn ngân sách.</summary>
        Pending,
        /// <summary>Bảng mất/suspended/txn âm hoặc lùi — dừng, không chứng nhận.</summary>
        Invalid,
    }

    /// <summary>
    /// Xét một probe. <paramref name="baseline"/> null = probe đầu: chốt target từ probe này (trả trong
    /// <paramref name="next"/>). Probe sau so với mốc đã chốt; txn nhỏ hơn mốc đầu ⇒ <see cref="Decision.Invalid"/>.
    /// </summary>
    internal static Decision Evaluate(Baseline? baseline, NodeProbeOutput probe, out Baseline next)
    {
        next = baseline ?? new Baseline(probe.SequencerTxn, probe.WriterTxn, probe.SequencerTxn);
        if (!probe.TableExists || probe.WalSuspended || probe.WriterTxn < 0 || probe.SequencerTxn < 0)
            return Decision.Invalid;
        if (baseline is { } b && (probe.WriterTxn < b.FirstWriter || probe.SequencerTxn < b.FirstSequencer))
            return Decision.Invalid;
        return probe.WriterTxn >= next.Target ? Decision.Applied : Decision.Pending;
    }
}
