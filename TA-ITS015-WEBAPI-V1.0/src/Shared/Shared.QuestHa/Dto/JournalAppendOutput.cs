namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: ACK của journal: chỉ trả về SAU KHI transaction SQL đã commit.
/// Created date: 09/10/2026
/// </summary>
internal sealed record JournalAppendOutput(
    long Seq,
    string TargetTable,
    string EventKey,
    DateTime DesignatedTsUtc,
    long? Version,
    bool Superseded,
    DateTime CommittedUtc);
