namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả lượt dọn có trần; Rows là ứng viên dry-run hoặc số đã commit, Boundary là mốc lô cuối.
/// Created date: 28/10/2026
/// </summary>
public sealed record JournalPurgeOutput(bool DryRun, long Rows, int Batches, bool Capped,
    JournalPurgeBoundaryOutput? Boundary, string? Reason);
