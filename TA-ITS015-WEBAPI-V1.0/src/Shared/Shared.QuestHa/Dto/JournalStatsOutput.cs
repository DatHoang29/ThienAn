namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Tổng quan journal cho dashboard.
/// Created date: 09/10/2026
/// </summary>
internal sealed record JournalStatsOutput(
    long MaxSeq,
    long TotalRows,
    int QuarantineCount,
    IReadOnlyDictionary<string, long> CursorByNode,
    IReadOnlyDictionary<string, long> PendingByNode);
