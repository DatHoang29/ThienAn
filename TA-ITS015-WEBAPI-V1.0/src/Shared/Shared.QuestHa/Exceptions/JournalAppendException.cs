namespace Shared.QuestHa.Exceptions;

/// <summary>
/// Author: KienLT
/// Description: Journal không ghi được ⇒ CHƯA ACK. Caller phải coi event chưa được nhận.
/// Created date: 09/10/2026
/// </summary>
internal sealed class JournalAppendException : Exception
{
    /// <summary>Tạo lỗi append journal chưa ACK, giữ exception gốc.</summary>
    public JournalAppendException(string message, Exception inner) : base(message, inner) { }

    /// <summary>từ chối không do lỗi SQL (vd hàng đợi ghi đầy).</summary>
    public JournalAppendException(string message) : base(message) { }
}
