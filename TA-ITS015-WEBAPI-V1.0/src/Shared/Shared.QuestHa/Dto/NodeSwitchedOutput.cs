namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Sự kiện router đổi node đọc.
/// Created date: 09/10/2026
/// </summary>
internal sealed record NodeSwitchedOutput(DateTime Utc, string? From, string To, string Reason);
