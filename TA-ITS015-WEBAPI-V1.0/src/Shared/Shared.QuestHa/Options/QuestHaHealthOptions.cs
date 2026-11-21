namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Ngưỡng và chu kỳ giám sát QuestDB HA. Mặc định theo (đồng bộ ≤ 5 s p95) và (backup daily).
/// Created date: 28/10/2026
/// </summary>
public sealed class QuestHaHealthOptions
{
    /// <summary>Chu kỳ worker đánh giá sức khoẻ.</summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Lặp lại cảnh báo khi trạng thái xấu kéo dài (đổi mức thì báo ngay).</summary>
    public TimeSpan RepeatWarningEvery { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Không thấy heartbeat của replicator quá lâu ⇒ Critical (replicator chết / mất kết nối journal).</summary>
    public TimeSpan HeartbeatStaleAfter { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Tuổi dòng pending cũ nhất của node ≥ ngưỡng ⇒ Warn (5 s).</summary>
    public double LagWarnSeconds { get; set; } = 10;

    /// <summary>Tuổi dòng pending cũ nhất ≥ ngưỡng ⇒ Critical.</summary>
    public double LagCriticalSeconds { get; set; } = 60;

    /// <summary>Lần đối soát gần nhất cũ hơn ngưỡng ⇒ Warn.</summary>
    public TimeSpan ReconcileWarnAfter { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Có bản backup đã xác minh mới hơn ngưỡng này mới coi là ổn (backup daily ⇒ 36 h).</summary>
    public TimeSpan BackupWarnAfter { get; set; } = TimeSpan.FromHours(36);

    /// <summary>Backup cũ hơn ngưỡng ⇒ Critical (mất hơn 2 chu kỳ daily).</summary>
    public TimeSpan BackupCriticalAfter { get; set; } = TimeSpan.FromHours(72);

    /// <summary>Khoảng nghỉ tối thiểu giữa hai cảnh báo khi mức KHÔNG tăng (chống nhảy qua lại quanh ngưỡng). Tăng mức luôn báo ngay.</summary>
    public TimeSpan MinAlertGap { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>true ⇒ chưa có bản backup nào của một node = Critical. Tắt khi chưa triển khai backup.</summary>
    public bool BackupRequired { get; set; } = true;

    /// <summary>Bản sao nông để dựng snapshot tham số vận hành mới; không sửa bản đang được đọc.</summary>
    internal QuestHaHealthOptions Clone() => (QuestHaHealthOptions)MemberwiseClone();
}
