namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Trạng thái node theo góc nhìn replicator/router. Màu dashboard: Ready 🟢, CatchingUp 🟡, Down 🔴, Fenced ⚫.
/// Created date: 09/10/2026
/// </summary>
public enum QuestNodeStateEnum
{
    Unknown = 0,
    /// <summary>Kết nối được, WAL ổn, pending cũ nhất ≤ ReadyMaxLag, worker không pause ⇒ được nhận đọc.</summary>
    Ready = 1,
    /// <summary>Kết nối được nhưng đang lag hoặc worker bị pause ⇒ KHÔNG nhận đọc.</summary>
    CatchingUp = 2,
    /// <summary>Lỗi kết nối (đang backoff).</summary>
    Down = 3,
    /// <summary>Bị rào: WAL suspend / bảng thiếu / đang backup-restore. Không giao, không nhận đọc.</summary>
    Fenced = 4,
}
