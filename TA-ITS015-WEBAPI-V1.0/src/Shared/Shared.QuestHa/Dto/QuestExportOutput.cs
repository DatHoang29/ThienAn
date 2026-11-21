namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả <c>QuestHaManager.ExportAsync</c>. Apply = false là xem trước: node được chọn, số dòng dự kiến từng giờ,
/// dung lượng ước (theo số đo lab) và dung lượng trống của ExportRoot. <c>DeferredReason</c> khác null ⇒ lượt không chạy/không xong
/// (không node Ready, chưa phủ head, thiếu đĩa, lỗi xuất…) — lịch sẽ thử lại.
/// Created date: 02/11/2026
/// </summary>
public sealed record QuestExportOutput(
    DateOnly DayLocal,
    bool Apply,
    string TimeZone,
    DateTime FromUtc,
    DateTime ToUtc,
    string? Node,
    long? WatermarkSeq,
    long EstimatedBytes,
    long? FreeBytes,
    IReadOnlyList<QuestExportTableOutput> Tables,
    string? DeferredReason)
{
    /// <summary>Mọi bảng đã xuất và ghi catalog Verified (chỉ khi Apply).</summary>
    public bool Completed => Apply && DeferredReason is null && Tables.Count > 0 && Tables.All(t => t.Verified);
}
