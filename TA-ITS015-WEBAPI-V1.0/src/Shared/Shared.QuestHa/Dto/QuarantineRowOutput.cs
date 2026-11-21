namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một dòng QuestQuarantine: Seq lỗi dữ liệu trên một node, lỗi, số lần thử, thời điểm xử lý.
/// Created date: 09/10/2026
/// </summary>
internal sealed record QuarantineRowOutput(
    long Seq,
    string Node,
    string Error,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc,
    int Attempts,
    DateTime? ResolvedUtc = null);
