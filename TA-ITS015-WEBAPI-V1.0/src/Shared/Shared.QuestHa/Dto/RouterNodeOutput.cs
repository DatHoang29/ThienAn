namespace Shared.QuestHa.Dto;

/// <summary>
/// Author: KienLT
/// Description: Trạng thái một node theo góc nhìn router (đủ điều kiện đọc, breaker, Ready từ khi nào).
/// Created date: 09/10/2026
/// </summary>
internal sealed record RouterNodeOutput(
    string Node,
    int Priority,
    QuestNodeStateEnum State,
    // true = breaker cục bộ đang chặn node (sau lỗi kết nối).
    bool BreakerTripped,
    DateTime? ReadySinceUtc,
    bool FailbackEligible,
    // trạng thái đang lấy từ nguồn dự phòng (SQL journal không đáp ứng). CoveredUntilUtc = mốc SQL xác nhận
    // node đã nhận hết journal lần cuối; dữ liệu ACK sau mốc này có thể chưa có trên node.
    bool FromFallback = false,
    DateTime? CoveredUntilUtc = null);
