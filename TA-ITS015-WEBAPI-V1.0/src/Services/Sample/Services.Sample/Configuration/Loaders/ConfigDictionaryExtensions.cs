namespace Services.Sample.Configuration.Loaders;

/// <summary>
/// Author: TAC Team
/// Description: Helper parse giá trị string từ dictionary config (Key/Value row) sang typed primitive,
/// fallback an toàn khi thiếu/lỗi parse.
/// Created date: 17/07/2026
/// </summary>
internal static class ConfigDictionaryExtensions
{
    public static string GetString(
        this IReadOnlyDictionary<string, string> values,
        string key,
        string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;

    public static int GetInt32(
        this IReadOnlyDictionary<string, string> values,
        string key,
        int fallback) =>
        values.TryGetValue(key, out var value) && int.TryParse(value, out var number)
            ? number
            : fallback;

    public static bool GetBoolean(
        this IReadOnlyDictionary<string, string> values,
        string key,
        bool fallback) =>
        values.TryGetValue(key, out var value) && bool.TryParse(value, out var flag)
            ? flag
            : fallback;
}
