namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map một dòng QuestOutbox.
/// Created date: 31/10/2026
/// </summary>
internal sealed class OutboxDb
{
    public long Seq { get; set; }
    public string TargetTable { get; set; }
    public string EventKey { get; set; }
    public DateTime DesignatedTs { get; set; }
    public string Payload { get; set; }
    public int SchemaVer { get; set; }
    public long? Version { get; set; }
    public bool Superseded { get; set; }
    public DateTime CreatedUtc { get; set; }
}

#pragma warning restore CS8618
