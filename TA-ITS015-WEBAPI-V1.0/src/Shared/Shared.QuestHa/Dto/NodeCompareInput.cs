namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Tham số so sánh một bảng giữa hai node trong cửa sổ <c>[FromUtc, ToUtc)</c>. <c>Source</c> thuộc Nodes hoặc
/// SourceNode, <c>Target</c> bắt buộc thuộc Nodes; <c>Settle</c> cắt phần quá mới, <c>MaxWindows</c> = trần số cửa sổ phút.
/// Created date: 31/10/2026
/// </summary>
public sealed record NodeCompareInput(string Source, string Target, QuestTableSpec Table, DateTime FromUtc, DateTime ToUtc,
    TimeSpan Settle, int MaxWindows);
