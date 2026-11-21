namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Pending thực tế của node được giao; thiếu cursor được coi như LastSeq=0, không tạo cursor giả.
/// Created date: 28/10/2026
/// </summary>
public sealed record JournalNodeBacklogOutput(string Node, bool CursorExists, long LastSeq, long SafeSeq,
    long PendingRows, DateTime? OldestPendingCreatedUtc, TimeSpan OldestPendingAge)
{
    /// <summary>Đã đếm tới trần; PendingRows là cận dưới, không phải tổng chính xác.</summary>
    public bool PendingRowsLowerBound { get; init; }

    /// <summary>Số hiển thị có dấu ≥ khi phép đếm dừng ở trần.</summary>
    public string PendingRowsDisplay => (PendingRowsLowerBound ? "≥" : "") + PendingRows.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
