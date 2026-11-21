namespace Shared.QuestHa.Exceptions;

/// <summary>
/// Author: KienLT
/// Description: Không còn node nào được phép đọc.
/// Created date: 09/10/2026
/// </summary>
internal sealed class NoQuestNodeAvailableException : Exception
{
    /// <summary>Tạo lỗi không còn node QuestDB đủ điều kiện để đọc.</summary>
    public NoQuestNodeAvailableException(string message, Exception? inner = null) : base(message, inner) { }
}
