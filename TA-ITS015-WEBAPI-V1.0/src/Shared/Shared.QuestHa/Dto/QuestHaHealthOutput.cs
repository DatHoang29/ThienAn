namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Ảnh chụp sức khoẻ QuestDB HA: mức tổng, từng node, số đo journal và các lý do xuống mức.
/// Created date: 28/10/2026
/// </summary>
public sealed record QuestHaHealthOutput(
    QuestHaHealthLevelEnum Level,
    DateTime CheckedUtc,
    long JournalMaxSeq,
    long JournalRows,
    int OpenQuarantine,
    IReadOnlyList<NodeHealthOutput> Nodes,
    IReadOnlyList<QuestHaHealthReasonOutput> Reasons);
