namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình router đọc có failover (cache trạng thái, heartbeat, failback, breaker, nguồn dự phòng).
/// Created date: 09/10/2026
/// </summary>
public sealed class RouterOptions
{
    // ngân sách từng node để cursor/WAL phủ head cố định, không đọc dữ liệu thiếu.
    /// <summary>Thời gian tối đa chờ cursor và WAL phủ head đã chốt trên mỗi node của đường đọc VDS.</summary>
    public TimeSpan CoverageWait { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Cache trạng thái node đọc từ QuestNodeStatus. Giá trị tham chiếu: 2 s.</summary>
    public TimeSpan StatusCacheTtl { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Heartbeat cũ hơn ngưỡng này ⇒ coi node Down (worker chết cũng không được tin).</summary>
    public TimeSpan HeartbeatStaleAfter { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Failback: node ưu tiên phải Ready liên tục ≥ ngưỡng này. Lab 30 s, production 5 phút.</summary>
    public TimeSpan FailbackStableFor { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Failback còn cần lượt đối soát gần nhất (sau khi node Ready) đạt.</summary>
    public bool RequireReconcileForFailback { get; set; } = true;

    /// <summary>Breaker cục bộ: lỗi kết nối ⇒ chặn node trong khoảng này rồi cho 1 lượt thăm dò.</summary>
    public TimeSpan BreakerCooldown { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Không có node Ready: true = đọc tạm node CatchingUp lag nhỏ nhất; false = ném lỗi.</summary>
    public bool AllowDegradedRead { get; set; }

    /// <summary>Số sự kiện đổi node giữ trong bộ nhớ.</summary>
    public int SwitchHistorySize { get; set; } = 50;

    /// <summary>ngân sách riêng cho 1 lượt làm mới trạng thái từ SQL, độc lập với token của request đọc.</summary>
    public TimeSpan StatusRefreshTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>khi SQL không đáp ứng, router tự thăm dò node theo chu kỳ này (không phụ thuộc SQL I/O của worker).</summary>
    public TimeSpan FallbackProbeInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// đọc dự phòng chỉ hợp lệ trong khoảng này kể từ <c>CoveredUntilUtc</c>; quá hạn ⇒ loại node. null = không giới hạn.
    /// </summary>
    public TimeSpan? FallbackMaxAge { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Bản sao nông để dựng snapshot tham số vận hành mới; không sửa bản đang được đọc.</summary>
    internal RouterOptions Clone() => (RouterOptions)MemberwiseClone();
}
