namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Con trỏ của 1 node. <see cref="SafeSeq"/> = cursor tại thời điểm ≥ RewindOnReconnect trước.
/// Created date: 09/10/2026
/// </summary>
internal sealed record NodeCursorOutput(long LastSeq, long SafeSeq);
