namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Trạng thái 1 node (replicator ghi vào QuestNodeStatus; router/dashboard đọc).
/// Created date: 09/10/2026
/// </summary>
internal sealed record NodeStatusOutput
{
    public required string Node { get; init; }
    public QuestNodeStateEnum State { get; init; }
    public bool Paused { get; init; }
    public string? FenceReason { get; init; }
    public bool WalSuspended { get; init; }
    /// <summary>Cursor = Seq cuối đã giao xong cho node.</summary>
    public long LastSeq { get; init; }
    /// <summary>Seq lớn nhất journal đang thấy được (đã commit).</summary>
    public long MaxSeq { get; init; }
    public long LagSeq { get; init; }
    /// <summary>Tuổi dòng pending cũ nhất (0 nếu bắt kịp).</summary>
    public double LagSeconds { get; init; }
    public DateTime HeartbeatUtc { get; init; }
    /// <summary>Thời điểm bắt đầu Ready liên tục (null nếu không Ready).</summary>
    public DateTime? ReadySinceUtc { get; init; }
    public DateTime? LastDeliveredUtc { get; init; }
    public string? LastError { get; init; }
    public DateTime? LastErrorUtc { get; init; }
    public DateTime? LastReconcileUtc { get; init; }
    public bool? LastReconcileOk { get; init; }
    /// <summary>Đếm trong tiến trình (không persist): số dòng đã ghi, số lần rewind, số dòng quarantine.</summary>
    public long DeliveredTotal { get; init; }
    public long RewindCount { get; init; }
    public long QuarantinedTotal { get; init; }
}
