namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map cursor một node (LastSeq, SafeSeq).
/// Created date: 31/10/2026
/// </summary>
internal sealed class CursorDb { public string Node { get; set; } public long LastSeq { get; set; } public long SafeSeq { get; set; } }

#pragma warning restore CS8618
