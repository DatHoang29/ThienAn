namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một dòng catalog bản xuất Parquet (<c>dbo.QuestExportCatalog</c>): một bảng × một ngày giờ server.
/// <c>DayLocal</c> là ngày theo giờ server; <c>FromUtc</c>/<c>ToUtc</c> là khoảng UTC tương ứng của cột designated timestamp.
/// Created date: 02/11/2026
/// </summary>
public sealed record QuestExportRecordOutput(
    Guid ExportId,
    string TableName,
    DateOnly DayLocal,
    DateTime FromUtc,
    DateTime ToUtc,
    string Node,
    long WatermarkSeq,
    long Rows,
    long Bytes,
    string Path,
    bool Verified,
    DateTime CreatedUtc,
    DateTime? RetiredUtc);
