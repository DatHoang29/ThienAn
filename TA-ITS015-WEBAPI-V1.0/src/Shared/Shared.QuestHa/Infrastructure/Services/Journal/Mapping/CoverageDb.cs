namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map snapshot đọc một câu SQL, không payload.
/// Created date: 31/10/2026
/// </summary>
internal sealed class CoverageDb
{
    public long Generation { get; set; }
    public int Phase { get; set; }
    public bool ManualFence { get; set; }
    public long LastSeq { get; set; }
    public long? FirstQuarantinedSeq { get; set; }
}

#pragma warning restore CS8618
