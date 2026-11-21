namespace Shared.QuestHa.Exceptions;

/// <summary>
/// Author: KienLT
/// Description: Lỗi ở journal (SQL) — KHÔNG được coi là node Down.
/// Created date: 31/10/2026
/// </summary>
internal sealed class JournalException : Exception
{
    public JournalException(Exception inner) : base(inner.Message, inner) { }
}
