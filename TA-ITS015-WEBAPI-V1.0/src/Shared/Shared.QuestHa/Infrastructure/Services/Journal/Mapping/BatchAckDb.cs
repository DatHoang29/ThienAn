namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map ACK theo thứ tự trong lô append.
/// Created date: 31/10/2026
/// </summary>
internal sealed class BatchAckDb { public int Ord { get; set; } public long Seq { get; set; } public bool Superseded { get; set; } public DateTime CreatedUtc { get; set; } }

#pragma warning restore CS8618
