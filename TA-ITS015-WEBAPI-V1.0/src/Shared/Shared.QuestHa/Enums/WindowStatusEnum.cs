namespace Shared.QuestHa.Enums;

/// <summary>
/// Author: KienLT
/// Description: Kết quả đối soát 1 phút cho 1 node.
/// Created date: 09/10/2026
/// </summary>
internal enum WindowStatusEnum
{
    Ok,
    /// <summary>Node chưa giao tới các Seq của phút này (cursor còn sau) ⇒ chưa kết luận.</summary>
    Pending,
    Mismatch,
    /// <summary>Không đọc được node.</summary>
    Unreachable,
}
