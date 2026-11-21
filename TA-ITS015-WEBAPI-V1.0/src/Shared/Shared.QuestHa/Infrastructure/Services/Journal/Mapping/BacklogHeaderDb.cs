namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Metadata outbox SQL, không đọc payload.
/// Created date: 31/10/2026
/// </summary>
internal sealed class BacklogHeaderDb
{
    public DateTime MeasuredUtc { get; set; }
    public long TotalRows { get; set; }
    public long JournalBytes { get; set; }
    public long OpenQuarantineCount { get; set; }
}

#pragma warning restore CS8618
