namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả đối soát hai node QuestDB (nguồn ↔ đích) cho một bảng trong một khoảng thời gian, theo từng phút.
/// <c>MissingInTarget</c> = có ở nguồn, thiếu ở đích; <c>ExtraInTarget</c> = có ở đích, không có ở nguồn (không vá được);
/// <c>Different</c> = cùng khoá nhưng khác giá trị. <c>ToUtc</c> là mốc ĐÃ cắt bớt theo độ trễ chờ (settle).
/// Created date: 27/10/2026
/// </summary>
public sealed record NodeCompareOutput(
    string Table,
    string Source,
    string Target,
    DateTime FromUtc,
    DateTime ToUtc,
    int WindowCount,
    long SourceRows,
    long TargetRows,
    long MissingInTarget,
    long ExtraInTarget,
    long Different,
    int MismatchedWindowCount,
    IReadOnlyList<DateTime> MismatchedWindows,
    IReadOnlyList<NodeCompareSampleOutput> Samples,
    DateTime StartedUtc,
    DateTime FinishedUtc)
{
    /// <summary>
    /// true khi hai node giống hệt trong khoảng đã so. Một lần so KHÔNG có cửa sổ nào (khoảng rỗng / bị cắt hết bởi settle) là
    /// "chưa so gì" nên KHÔNG tính Equal — tránh báo OK giả.
    /// </summary>
    public bool Equal => WindowCount > 0 && MissingInTarget == 0 && ExtraInTarget == 0 && Different == 0;
}
