namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả một lượt giờ của bản xuất: khoảng UTC, số dòng (đếm PG = số dòng trong metadata file), byte, SHA-256.
/// <c>Status</c>: Exported (vừa xuất), Kept (giữ file cũ vì khớp), Preview (chỉ xem trước), Failed.
/// Created date: 02/11/2026
/// </summary>
public sealed record QuestExportHourOutput(
    int Hour,
    DateTime FromUtc,
    DateTime ToUtc,
    long Rows,
    long Bytes,
    string? Sha256,
    string Status,
    string? Error = null);
