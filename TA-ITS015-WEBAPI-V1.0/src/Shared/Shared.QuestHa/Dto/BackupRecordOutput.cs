namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một bản backup nguội trong QuestBackupCatalog (node, watermark Seq/thời điểm, đường dẫn, kích thước, đã kiểm).
/// Created date: 09/10/2026
/// </summary>
public sealed record BackupRecordOutput(
    Guid BackupId,
    string Node,
    long WatermarkSeq,
    DateTime WatermarkTsUtc,
    string Path,
    long SizeBytes,
    bool Verified,
    DateTime CreatedUtc)
{
    // init giữ tương thích constructor/deconstruct tám trường hiện có.
    public DateTime? RetiredUtc { get; init; }
}
