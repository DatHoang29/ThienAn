namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map một dòng QuestQuarantine.
/// Created date: 31/10/2026
/// </summary>
internal sealed class QuarantineDb
{
    public long Seq { get; set; }
    public string Node { get; set; }
    public string Error { get; set; }
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public int Attempts { get; set; }
    public DateTime? ResolvedUtc { get; set; }
}

#pragma warning restore CS8618
