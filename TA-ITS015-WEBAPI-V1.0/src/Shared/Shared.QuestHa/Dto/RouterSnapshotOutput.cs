namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Ảnh chụp router cho dashboard: node đang đọc, số request/lỗi/retry, lịch sử đổi node.
/// Created date: 09/10/2026
/// </summary>
internal sealed record RouterSnapshotOutput(
    string? CurrentNode,
    string? PreferredNode,
    IReadOnlyList<RouterNodeOutput> Nodes,
    IReadOnlyList<NodeSwitchedOutput> RecentSwitches,
    long Requests,
    long Failures,
    long Retries);
