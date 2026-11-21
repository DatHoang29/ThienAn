namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Báo cáo một lượt đối soát journal ↔ các node (các cửa sổ, kết quả đạt/không, thời điểm).
/// Created date: 09/10/2026
/// </summary>
internal sealed record ReconcileReportOutput(
    string Table,
    DateTime FromUtc,
    DateTime ToUtc,
    DateTime StartedUtc,
    DateTime FinishedUtc,
    // Chỉ các phút có ít nhất 1 node khác Ok (để gọn); tổng số phút ở WindowCount.
    IReadOnlyList<ReconcileWindowOutput> MismatchedWindows,
    int WindowCount,
    // Node → true nếu mọi phút Ok.
    IReadOnlyDictionary<string, bool> NodeOk,
    long JournalRows,
    // Node → số Seq trong cửa sổ đang quarantine CHỈ ở node đó (node khác đã có). Khác 0 ⇒ NodeOk=false.
    IReadOnlyDictionary<string, int>? OpenQuarantine = null);
