using SqlSugar;

namespace Shared.QuestHa.Infrastructure.Services.Journal.Statements;

/// <summary>
/// Author: KienLT
/// Description: Giữ đúng kiểu tham số ADO, đặc biệt datetime2 và giá trị NULL.
/// Created date: 31/10/2026
/// </summary>
internal static class QuestJournalParameter
{
    internal static SugarParameter P(string name, object? value)
    {
        return value switch
        {
            DateTime d => new SugarParameter(name, d, System.Data.DbType.DateTime2),
            null => new SugarParameter(name, DBNull.Value),
            _ => new SugarParameter(name, value),
        };
    }

    internal static string? Trim(string? s, int max) => s is null || s.Length <= max ? s : s[..max];
}
