namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map quarantine chưa resolve theo Seq/node.
/// Created date: 31/10/2026
/// </summary>
internal sealed class OpenQuarantineDb { public long Seq { get; set; } public string Node { get; set; } = ""; public int Attempts { get; set; } public bool Superseded { get; set; } }

#pragma warning restore CS8618
