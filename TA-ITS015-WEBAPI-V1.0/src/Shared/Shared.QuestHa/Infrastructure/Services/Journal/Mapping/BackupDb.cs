namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map một dòng QuestBackupCatalog.
/// Created date: 31/10/2026
/// </summary>
internal sealed class BackupDb
{
    // RetiredUtc NULL là chưa retire tường minh.
    public DateTime? RetiredUtc { get; set; }
    public Guid BackupId { get; set; }
    public string Node { get; set; }
    public long WatermarkSeq { get; set; }
    public DateTime WatermarkTs { get; set; }
    public string Path { get; set; }
    public long SizeBytes { get; set; }
    public bool Verified { get; set; }
    public DateTime CreatedUtc { get; set; }
}

#pragma warning restore CS8618
