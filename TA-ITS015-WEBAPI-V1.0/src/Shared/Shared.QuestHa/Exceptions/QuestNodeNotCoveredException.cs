namespace Shared.QuestHa.Exceptions;

/// <summary>
/// Author: KienLT
/// Description: Node còn sống nhưng chưa phủ mốc dữ liệu cần đọc; router thử node khác, không trip breaker kết nối.
/// Created date: 27/10/2026
/// </summary>
internal sealed class QuestNodeNotCoveredException : Exception
{
    /// <summary>Báo chưa đủ dữ liệu cho lượt đọc hiện tại.</summary>
    public QuestNodeNotCoveredException() : base("QuestDB node has not applied the required journal watermark.") { }
}
