using SqlSugar;

namespace Services.Sample.Features.DataFlow.Persistence;

/// <summary>
/// Author: TAC Team
/// Description: Entity dữ liệu mẫu sau khi xử lý.
/// Created date: 16/07/2026
/// </summary>
[SugarTable("ServiceData")]
public sealed class DataEntity
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 50)]
    public string Source { get; set; } = string.Empty;

    [SugarColumn(Length = 200)]
    public string MessageId { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public string DataJson { get; set; } = string.Empty;

    public DateTime CreateTime { get; set; }
}
