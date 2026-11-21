namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map số đo pending theo node.
/// Created date: 31/10/2026
/// </summary>
internal sealed class NodeBacklogDb
{
    public string Node { get; set; }
    public bool CursorExists { get; set; }
    public long LastSeq { get; set; }
    public long SafeSeq { get; set; }
    public long PendingRows { get; set; }
    public DateTime? OldestPendingCreatedUtc { get; set; }
    public double AgeSeconds { get; set; }
}

#pragma warning restore CS8618
