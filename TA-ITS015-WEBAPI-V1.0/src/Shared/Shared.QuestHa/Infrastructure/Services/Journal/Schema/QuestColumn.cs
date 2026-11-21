namespace Shared.QuestHa.Infrastructure.Services.Journal.Schema;

/// <summary>
/// Author: KienLT
/// Description: Một cột bảng QuestDB.
/// <paramref name="IsDedupKey"/>: thuộc <c>DEDUP UPSERT KEYS</c> (designated ts BẮT BUỘC là key).
/// <paramref name="FromJournalSeq"/>: replicator tự điền Seq của journal (payload không cần có).
/// Created date: 09/10/2026
/// </summary>
public sealed record QuestColumn(
    string Name,
    QuestColumnTypeEnum Type,
    bool IsDedupKey = false,
    bool IsDesignated = false,
    bool FromJournalSeq = false);
