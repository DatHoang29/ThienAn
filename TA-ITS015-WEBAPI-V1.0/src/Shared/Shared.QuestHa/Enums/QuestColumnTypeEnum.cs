namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Kiểu cột QuestDB mà replicator biết chuyển từ payload JSON.
/// Created date: 09/10/2026
/// </summary>
public enum QuestColumnTypeEnum
{
    Timestamp,
    Long,
    Int,
    Double,
    Boolean,
    Symbol,
    Varchar,

    // thêm SHORT cho VdsTrafficStatistic.periodMinutes (thêm cuối, không đổi giá trị cũ).
    /// <summary>SHORT (16 bit) — QuestDB KHÔNG có NULL cho kiểu này: null ghi thành 0.</summary>
    Short,
}
