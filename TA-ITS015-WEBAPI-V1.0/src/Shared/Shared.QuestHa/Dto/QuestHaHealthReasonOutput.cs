namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một lý do làm sức khoẻ QuestDB HA xuống Warn/Critical. <c>Code</c> ổn định (cho cảnh báo/dashboard), <c>Message</c> cho người đọc.
/// Created date: 28/10/2026
/// </summary>
public sealed record QuestHaHealthReasonOutput(QuestHaHealthLevelEnum Level, string Code, string Message, string? Node);
