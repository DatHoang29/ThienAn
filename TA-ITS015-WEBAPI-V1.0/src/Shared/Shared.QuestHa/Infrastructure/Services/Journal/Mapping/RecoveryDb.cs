namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map cột phục hồi/rào của QuestNodeStatus kèm giờ SQL.
/// Created date: 31/10/2026
/// </summary>
internal sealed class RecoveryDb
{
    public long RecoveryGeneration { get; set; }
    public byte RecoveryPhase { get; set; }
    public DateTime? RecoveryCoveredUtc { get; set; }
    public long? ReconciledGeneration { get; set; }
    public DateTime? LastReconcileUtc { get; set; }
    public bool? LastReconcileOk { get; set; }
    public bool ManualFence { get; set; }
    public DateTime SqlNowUtc { get; set; }
}

#pragma warning restore CS8618
