namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Tham số vá node đích từ node nguồn cho một hoặc nhiều bảng. <c>Apply</c> = false chỉ xem trước;
/// <c>Apply</c> = true chỉ hợp lệ ở WriteMode=Shadow với nguồn là SourceNode, chạy dưới khoá vận hành của journal,
/// chờ đích hết backlog (<c>MaxPending</c>) và WAL apply trong <c>Wait</c> trước khi vá.
/// Created date: 31/10/2026
/// </summary>
public sealed record NodePatchInput(string Source, string Target, IReadOnlyList<QuestTableSpec> Tables, DateTime FromUtc, DateTime ToUtc,
    bool Apply, TimeSpan Settle, int MaxWindows, int MaxRows, int MaxPending, TimeSpan Wait);
