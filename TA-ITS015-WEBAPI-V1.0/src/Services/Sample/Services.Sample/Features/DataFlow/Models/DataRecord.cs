namespace Services.Sample.Features.DataFlow.Models;

/// <summary>
/// Author: TAC Team
/// Description: Model nội bộ sau khi payload đã được chuẩn hóa.
/// Created date: 16/07/2026
/// </summary>
public sealed record DataRecord(
    string Source,
    string MessageId,
    DateTime OccurredAt,
    string NormalizedJson);
