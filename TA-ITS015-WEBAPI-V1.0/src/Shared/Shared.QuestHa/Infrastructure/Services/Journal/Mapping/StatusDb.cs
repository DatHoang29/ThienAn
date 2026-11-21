namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map một dòng QuestNodeStatus kèm tuổi heartbeat/đối soát.
/// Created date: 31/10/2026
/// </summary>
internal sealed class StatusDb
{
    public string Node { get; set; }
    public string State { get; set; }
    public bool Paused { get; set; }
    public string? FenceReason { get; set; }
    public bool WalSuspended { get; set; }
    public long LastSeq { get; set; }
    public long MaxSeq { get; set; }
    public long LagSeq { get; set; }
    public double LagSeconds { get; set; }
    public DateTime? ReadySinceUtc { get; set; }
    public DateTime? LastDeliveredUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime? LastErrorUtc { get; set; }
    public bool? LastReconcileOk { get; set; }
    public long? HeartbeatAgeMs { get; set; }
    public long? ReconcileAgeMs { get; set; }
}

#pragma warning restore CS8618
