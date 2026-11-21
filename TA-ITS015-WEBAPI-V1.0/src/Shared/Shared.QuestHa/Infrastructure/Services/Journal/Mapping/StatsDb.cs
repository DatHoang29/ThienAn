namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map thống kê journal (MaxSeq, tổng dòng, số quarantine).
/// Created date: 31/10/2026
/// </summary>
internal sealed class StatsDb { public long MaxSeq { get; set; } public long TotalRows { get; set; } public int QuarantineCount { get; set; } }

#pragma warning restore CS8618
