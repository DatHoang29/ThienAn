namespace Shared.QuestHa.Exceptions;

/// <summary>
/// Author: KienLT
/// Description: Đánh dấu lỗi kết nối node (để phân biệt với lỗi kết nối tới SQL journal).
/// Created date: 31/10/2026
/// </summary>
internal sealed class NodeConnectivityException : Exception
{
    public NodeConnectivityException(Exception inner) : base(inner.Message, inner) { }
}
