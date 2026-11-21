namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Kết quả vá node đích từ node nguồn: <c>Candidates</c> = dòng thiếu/lệch vá được; <c>Journaled</c> = số dòng
/// đã đưa lại vào journal (0 khi <c>DryRun</c> hoặc <c>Deferred</c>); replicator sẽ giao chúng sang các node (DEDUP ghi đè nên
/// chạy lại an toàn). <c>Capped</c> = dừng vì chạm trần số dòng — chạy lại sau khi replicator giao xong để vá tiếp.
/// <c>ExtraInTarget</c> (chỉ có ở đích) không vá. <c>TargetNewer</c> = bảng có cột version và đích có version CAO hơn nguồn —
/// không vá (giữ bất biến "version cao nhất thắng"). <c>Unrepairable</c> = dòng nguồn không thể vào journal (replicator sẽ từ
/// chối: giá trị không hợp lệ, khoá quá dài) — không vá, cần xử lý tay. <c>Deferred</c> = đích còn backlog journal chưa giao
/// (hoặc không có con trỏ) nên KHÔNG ghi journal để khỏi nhân đôi mục (<c>DeferReason</c> nêu lý do); <c>Candidates</c> vẫn được đếm.
/// Created date: 27/10/2026
/// </summary>
public sealed record NodeRepairOutput(
    string Table,
    string Source,
    string Target,
    DateTime FromUtc,
    DateTime ToUtc,
    bool DryRun,
    long Candidates,
    long Journaled,
    long ExtraInTarget,
    long TargetNewer,
    long Unrepairable,
    bool Capped,
    bool Deferred,
    string? DeferReason,
    DateTime StartedUtc,
    DateTime FinishedUtc);
