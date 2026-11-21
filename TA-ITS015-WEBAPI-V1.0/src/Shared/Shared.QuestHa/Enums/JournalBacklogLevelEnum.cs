using System.Text.Json.Serialization;

namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Mức cảnh báo backlog journal; không quyết định loại event.
/// Created date: 28/10/2026
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<JournalBacklogLevelEnum>))]
public enum JournalBacklogLevelEnum { Ok, Warn, Critical }
