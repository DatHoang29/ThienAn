namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: pha phục hồi lưu bền của 1 node.
/// Created date: 13/10/2026
/// </summary>
internal enum NodeRecoveryPhaseEnum : byte
{
    None = 0,
    /// <summary>Đang restore (đã BeginRecovery, root có thể đang bị thay) ⇒ không giao, không đọc.</summary>
    Restoring = 1,
    /// <summary>Đã replay + WAL đủ; chờ một lượt đối soát đọc node SAU <see cref="NodeRecoveryOutput.CoveredUtc"/> và đúng thế hệ.</summary>
    AwaitReconcile = 2,
}
