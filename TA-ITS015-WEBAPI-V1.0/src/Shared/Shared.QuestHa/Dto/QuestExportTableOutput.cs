namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả xuất một bảng trong một ngày: thư mục, tổng dòng/byte, từng giờ, đã ghi catalog Verified hay chưa.
/// Created date: 02/11/2026
/// </summary>
public sealed record QuestExportTableOutput(
    string Table,
    string Folder,
    long Rows,
    long Bytes,
    bool Verified,
    Guid? ExportId,
    IReadOnlyList<QuestExportHourOutput> Hours,
    string? Error = null);
