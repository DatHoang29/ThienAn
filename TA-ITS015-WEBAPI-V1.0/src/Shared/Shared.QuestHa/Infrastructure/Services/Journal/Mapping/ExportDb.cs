namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map một dòng QuestExportCatalog.
/// Created date: 02/11/2026
/// </summary>
internal sealed class ExportDb
{
    public Guid ExportId { get; set; }
    public string TableName { get; set; }
    public DateTime DayLocal { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public string Node { get; set; }
    public long WatermarkSeq { get; set; }
    public long RowTotal { get; set; }
    public long Bytes { get; set; }
    public string Path { get; set; }
    public bool Verified { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? RetiredUtc { get; set; }
}

#pragma warning restore CS8618
