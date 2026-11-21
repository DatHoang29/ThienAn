namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Sức khoẻ một node QuestDB theo góc nhìn journal (liveness/readiness, độ trễ, WAL, đối soát, backup).
/// <c>HeartbeatAgeSeconds</c> = tuổi nhịp sống của replicator (không phải của QuestDB).
/// Created date: 28/10/2026
/// </summary>
public sealed record NodeHealthOutput(
    string Node,
    QuestNodeStateEnum State,
    QuestHaHealthLevelEnum Level,
    long LagSeq,
    double LagSeconds,
    double HeartbeatAgeSeconds,
    bool WalSuspended,
    DateTime? LastReconcileUtc,
    bool? LastReconcileOk,
    DateTime? LastBackupUtc,
    bool? LastBackupVerified);
