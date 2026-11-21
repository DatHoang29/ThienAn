namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Lag so với journal (chỉ tính dòng đã commit — RowVer &lt; MIN_ACTIVE_ROWVERSION()).
/// Created date: 09/10/2026
/// </summary>
internal sealed record NodeLagOutput(long MaxSeq, long Pending, double OldestPendingAgeSeconds);
