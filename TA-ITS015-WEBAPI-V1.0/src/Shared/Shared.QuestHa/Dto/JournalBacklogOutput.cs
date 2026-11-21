namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Số đo SQL và kết quả đánh giá; TotalRows là metadata xấp xỉ, JournalBytes gồm mọi index/LOB.
/// Created date: 28/10/2026
/// </summary>
public sealed record JournalBacklogOutput(DateTime MeasuredUtc, long TotalRows, long JournalBytes,
    long OpenQuarantineCount, IReadOnlyList<JournalNodeBacklogOutput> Nodes,
    JournalBacklogLevelEnum Level, IReadOnlyList<string> Reasons);
