namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Nội dung <c>manifest.json</c> của một thư mục phiên bản xuất Parquet. Ghi SAU CÙNG (file tạm + flush + đổi tên): phiên bản
/// không có manifest là bản dở. <c>ExportId</c> trùng khoá dòng catalog của chính phiên bản này (đối chiếu manifest ↔ catalog khi phục hồi).
/// Thời điểm đều là UTC; <c>DayLocal</c> theo giờ server, <c>TimeZone</c>/<c>UtcOffset</c> ghi múi giờ đã dùng.
/// Created date: 02/11/2026
/// </summary>
internal sealed record QuestExportManifestOutput
{
    public int Version { get; init; } = 2;
    public Guid? ExportId { get; init; }
    public required string Table { get; init; }
    public required string DesignatedColumn { get; init; }
    public required string DayLocal { get; init; }
    public required string TimeZone { get; init; }
    public required string UtcOffset { get; init; }
    public DateTime FromUtc { get; init; }
    public DateTime ToUtc { get; init; }
    public required string Node { get; init; }
    public long WatermarkSeq { get; init; }
    public string Format { get; init; } = "parquet";
    public string Codec { get; init; } = "zstd";
    public long Rows { get; init; }
    public long Bytes { get; init; }
    public DateTime CreatedUtc { get; init; }
    public required IReadOnlyList<QuestExportManifestHourOutput> Hours { get; init; }
}
