namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Loại sự kiện timeline QuestHa (đổi node, fence, quarantine, backup/restore, lỗi…).
/// Created date: 09/10/2026
/// </summary>
internal enum QuestHaEventKindEnum
{
    Info,
    NodeStateChanged,
    RouterSwitched,
    CursorRewound,
    CursorReset,
    Quarantined,
    ReconcileMismatch,
    ReconcileOk,
    BackupStarted,
    BackupFinished,
    RestoreStarted,
    RestoreFinished,
    Error,
}
