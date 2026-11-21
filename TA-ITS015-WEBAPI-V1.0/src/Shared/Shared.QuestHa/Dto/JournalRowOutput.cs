namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một dòng QuestOutbox.
/// Created date: 09/10/2026
/// </summary>
internal sealed record JournalRowOutput(
    long Seq,
    string TargetTable,
    string EventKey,
    DateTime DesignatedTsUtc,
    string Payload,
    int SchemaVer,
    long? Version,
    bool Superseded,
    DateTime CreatedUtc);
