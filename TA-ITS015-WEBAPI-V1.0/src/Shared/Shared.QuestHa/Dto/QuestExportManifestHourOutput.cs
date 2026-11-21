namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một giờ trong manifest: khoảng UTC, file (null khi giờ không có dòng nào — không gọi <c>/exp</c>), số dòng, byte, SHA-256,
/// thời điểm xuất (UTC), head journal đã chứng nhận.
/// Created date: 02/11/2026
/// </summary>
internal sealed record QuestExportManifestHourOutput
{
    public int Hour { get; init; }
    public DateTime FromUtc { get; init; }
    public DateTime ToUtc { get; init; }
    public string? File { get; init; }
    public long Rows { get; init; }
    public long Bytes { get; init; }
    public string? Sha256 { get; init; }
    public DateTime ExportedUtc { get; init; }
    /// <summary>Head journal mà số dòng giờ này đã được chứng nhận tới (bằng WatermarkSeq của manifest khi bản đủ).</summary>
    public long? WatermarkSeq { get; init; }
}
