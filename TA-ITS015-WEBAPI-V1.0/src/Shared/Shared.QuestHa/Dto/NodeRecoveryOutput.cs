namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: trạng thái phục hồi lưu bền (QuestNodeStatus). <see cref="SqlNowUtc"/> = đồng hồ SQL lúc đọc — reconciler
/// lấy nó làm mốc "bắt đầu đọc node" để so với <see cref="CoveredUtc"/> (cùng đồng hồ SQL).
/// Created date: 13/10/2026
/// </summary>
internal sealed record NodeRecoveryOutput(
    long Generation,
    NodeRecoveryPhaseEnum Phase,
    DateTime? CoveredUtc,
    long? ReconciledGeneration,
    DateTime? LastReconcileUtc,
    bool? LastReconcileOk,
    bool ManualFence,
    DateTime SqlNowUtc);
