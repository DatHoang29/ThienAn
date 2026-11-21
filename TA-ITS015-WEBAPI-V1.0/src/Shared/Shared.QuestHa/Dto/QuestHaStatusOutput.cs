namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Ảnh chụp trạng thái QuestDB HA cho vận hành: backlog journal (cursor/pending từng node) và sức khoẻ
/// (trạng thái node, lag, quarantine, backup, lý do xuống mức).
/// Created date: 31/10/2026
/// </summary>
public sealed record QuestHaStatusOutput(JournalBacklogOutput Backlog, QuestHaHealthOutput Health);
