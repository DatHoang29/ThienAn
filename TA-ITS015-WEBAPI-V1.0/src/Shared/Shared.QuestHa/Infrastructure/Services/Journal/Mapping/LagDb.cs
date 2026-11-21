namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map đầu journal và pending/tuổi pending của một cursor.
/// Created date: 31/10/2026
/// </summary>
internal sealed class LagDb { public long MaxSeq { get; set; } public long Pending { get; set; } public double AgeSeconds { get; set; } }

#pragma warning restore CS8618
