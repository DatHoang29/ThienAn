namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Xem trước/kết quả gỡ cursor node đã ngừng nhận journal; không thay đổi dữ liệu node.
/// Created date: 28/10/2026
/// </summary>
public sealed record JournalCursorRetireOutput(bool DryRun, string Node, bool CursorExists,
    long? LastSeq, long? SafeSeq, bool Retired, string? Reason);
