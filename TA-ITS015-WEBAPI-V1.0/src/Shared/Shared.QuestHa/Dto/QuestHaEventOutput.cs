namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một dòng timeline dashboard.
/// Created date: 09/10/2026
/// </summary>
internal sealed record QuestHaEventOutput(DateTime Utc, string Source, string? Node, QuestHaEventKindEnum Kind, string Message);
