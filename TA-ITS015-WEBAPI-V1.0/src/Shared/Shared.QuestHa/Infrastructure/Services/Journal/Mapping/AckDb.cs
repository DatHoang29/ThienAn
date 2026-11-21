namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map ACK một dòng append (Seq, Superseded, CreatedUtc).
/// Created date: 31/10/2026
/// </summary>
internal sealed class AckDb { public long Seq { get; set; } public bool Superseded { get; set; } public DateTime CreatedUtc { get; set; } }

#pragma warning restore CS8618
