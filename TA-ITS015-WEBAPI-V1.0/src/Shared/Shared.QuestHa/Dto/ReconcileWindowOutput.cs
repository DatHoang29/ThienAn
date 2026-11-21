namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Một cửa sổ thời gian trong lượt đối soát: số dòng/hash kỳ vọng từ journal và kết quả từng node.
/// Created date: 09/10/2026
/// </summary>
internal sealed record ReconcileWindowOutput(
    DateTime MinuteUtc,
    long JournalCount,
    string JournalHash,
    // Seq nhỏ nhất của các dòng journal trong phút (để phát lại).
    long MinSeq,
    long MaxSeq,
    IReadOnlyList<NodeWindowOutput> Nodes);
