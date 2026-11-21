namespace Shared.QuestHa.Infrastructure.Services.Journal.Mapping;

#pragma warning disable CS8618 // SqlSugar gán cột sau khi khởi tạo.

/// <summary>
/// Author: KienLT
/// Description: Map kết quả rewind cursor (cũ/mới).
/// Created date: 31/10/2026
/// </summary>
internal sealed class RewindDb { public long OldSeq { get; set; } public long NewSeq { get; set; } }

#pragma warning restore CS8618
