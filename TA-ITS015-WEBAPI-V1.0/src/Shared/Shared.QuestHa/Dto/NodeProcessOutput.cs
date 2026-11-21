namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Trạng thái tiến trình QuestDB do <see cref="INodeControl"/> trả về.
/// Created date: 09/10/2026
/// </summary>
public sealed record NodeProcessOutput(bool IsRunning, int? Pid, bool HttpOk, string? Detail = null);
