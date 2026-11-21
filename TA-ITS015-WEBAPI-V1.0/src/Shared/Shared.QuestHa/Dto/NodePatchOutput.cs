namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả vá của một bảng: <c>Repair</c> khi đã chạy vá/xem trước; <c>DeferredReason</c> khi dừng trước vá
/// (<c>TargetPendingOrNoCursor</c>, <c>TargetWalNotApplied</c>) — các bảng sau không chạy.
/// Created date: 31/10/2026
/// </summary>
public sealed record NodePatchOutput(string Table, NodeRepairOutput? Repair, string? DeferredReason);
