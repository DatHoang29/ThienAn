namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Append thô — dùng cho nút "dữ liệu hỏng" (L10): payload không qua kiểm tra kiểu.
/// Created date: 09/10/2026
/// </summary>
internal sealed record JournalAppendInput(
    string TargetTable,
    string EventKey,
    DateTime DesignatedTsUtc,
    string Payload,
    int SchemaVer = 1,
    long? Version = null);
