namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Snapshot một lệnh SQL cho cổng đọc; quarantine nhỏ nhất đủ kiểm mọi head cố định.
/// Created date: 29/10/2026
/// </summary>
internal sealed record NodeReadCoverageOutput(long Generation, NodeRecoveryPhaseEnum Phase, bool ManualFence, long LastSeq, long? FirstQuarantinedSeq);
