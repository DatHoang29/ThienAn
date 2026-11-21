namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả <c>QuestHaManager.ImportAsync</c> (nạp ngược một thư mục ngày bằng <c>read_parquet</c>): file đã kiểm SHA-256,
/// số dòng của bảng trong khoảng ngày trước/sau khi nạp (DEDUP chống trùng nên nạp lại không tăng), số dòng theo manifest.
/// Created date: 02/11/2026
/// </summary>
public sealed record QuestImportOutput(
    string Folder,
    string Table,
    string Node,
    bool Apply,
    DateOnly DayLocal,
    DateTime FromUtc,
    DateTime ToUtc,
    long ManifestRows,
    long RowsBefore,
    long? RowsAfter,
    int Files,
    string? CopiedTo,
    string? Error);
