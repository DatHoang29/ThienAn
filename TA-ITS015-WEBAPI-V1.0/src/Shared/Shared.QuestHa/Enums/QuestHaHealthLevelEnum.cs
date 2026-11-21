namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Mức sức khoẻ của QuestDB HA: Ok &lt; Warn &lt; Critical; mức tổng = mức cao nhất trong các lý do.
/// Created date: 28/10/2026
/// </summary>
public enum QuestHaHealthLevelEnum
{
    Ok = 0,

    /// <summary>Cần chú ý nhưng chưa mất bảo đảm (lag, đối soát cũ, backup hơi cũ…).</summary>
    Warn = 1,

    /// <summary>Mất bảo đảm hoặc sắp mất: node chết/rào, WAL suspend, mất heartbeat, không có backup, journal không đọc được…</summary>
    Critical = 2,
}
