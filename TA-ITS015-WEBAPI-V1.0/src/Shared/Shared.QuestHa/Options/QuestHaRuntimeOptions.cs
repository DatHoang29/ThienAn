namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Tham số VẬN HÀNH của QuestDB HA đổi được lúc chạy (ngưỡng giám sát, bảo trì journal, lịch/giữ bản backup, router đọc).
/// Host đọc từ cấu hình động (VDS: <c>VdsConfig</c> mã <c>vds_questha_*</c>) rồi chuyển cho <see cref="QuestHaManager.ApplySettings"/>.
/// Mọi thuộc tính nullable: null = không khai ⇒ dùng giá trị khởi động (appsettings section <c>QuestHa</c> hoặc mặc định trong code).
/// Không chứa tham số hạ tầng (kết nối, Nodes, WriteMode/ReadMode, Enabled, thư mục, NodeControls, AdminPipeName) — đổi những thứ đó phải restart.
/// Created date: 31/10/2026
/// </summary>
public sealed class QuestHaRuntimeOptions
{
    // Health
    /// <summary>Chu kỳ worker đánh giá sức khoẻ.</summary>
    public TimeSpan? HealthCheckInterval { get; set; }
    /// <summary>Không thấy heartbeat replicator quá lâu ⇒ Critical.</summary>
    public TimeSpan? HealthHeartbeatStaleAfter { get; set; }
    /// <summary>Tuổi pending cũ nhất (giây) ≥ ngưỡng ⇒ Warn; phải nhỏ hơn ngưỡng Critical.</summary>
    public double? HealthLagWarnSeconds { get; set; }
    /// <summary>Tuổi pending cũ nhất (giây) ≥ ngưỡng ⇒ Critical.</summary>
    public double? HealthLagCriticalSeconds { get; set; }
    /// <summary>Backup Verified cũ hơn ⇒ Warn; phải nhỏ hơn ngưỡng Critical.</summary>
    public TimeSpan? HealthBackupWarnAfter { get; set; }
    /// <summary>Backup Verified cũ hơn ⇒ Critical.</summary>
    public TimeSpan? HealthBackupCriticalAfter { get; set; }
    /// <summary>Chưa có backup nào của node = Critical.</summary>
    public bool? HealthBackupRequired { get; set; }
    /// <summary>Lần đối soát gần nhất cũ hơn ⇒ Warn.</summary>
    public TimeSpan? HealthReconcileWarnAfter { get; set; }
    /// <summary>Trạng thái xấu kéo dài ⇒ nhắc lại cảnh báo sau khoảng này (tăng mức vẫn báo ngay).</summary>
    public TimeSpan? HealthRepeatWarningEvery { get; set; }
    /// <summary>Khoảng nghỉ tối thiểu giữa hai cảnh báo khi mức không tăng (chống nhảy qua lại quanh ngưỡng).</summary>
    public TimeSpan? HealthMinAlertGap { get; set; }

    // JournalMaintenance
    /// <summary>Chu kỳ purge journal.</summary>
    public TimeSpan? JournalInterval { get; set; }
    /// <summary>Tuổi tối thiểu của dòng đã giao trước khi được purge.</summary>
    public TimeSpan? JournalRetention { get; set; }
    /// <summary>Số ngày bản backup Verified còn được bảo vệ quyền restore-replay.</summary>
    public int? JournalProtectedBackupDays { get; set; }
    /// <summary>Số dòng mỗi lô DELETE (nhỏ để tránh leo thang khoá).</summary>
    public int? JournalBatchSize { get; set; }
    /// <summary>Trần số dòng mỗi lượt purge.</summary>
    public int? JournalMaxRowsPerRun { get; set; }
    /// <summary>Trần thời gian mỗi lượt purge.</summary>
    public TimeSpan? JournalMaxRunDuration { get; set; }
    /// <summary>Giãn cách lặp lại cùng một cảnh báo journal.</summary>
    public TimeSpan? JournalAlertThrottle { get; set; }
    /// <summary>Pending mỗi node ≥ ngưỡng ⇒ Warn.</summary>
    public long? JournalWarnPendingRows { get; set; }
    /// <summary>Pending mỗi node ≥ ngưỡng ⇒ Critical.</summary>
    public long? JournalCriticalPendingRows { get; set; }
    /// <summary>Tuổi pending cũ nhất ≥ ngưỡng ⇒ Warn.</summary>
    public TimeSpan? JournalWarnPendingAge { get; set; }
    /// <summary>Tuổi pending cũ nhất ≥ ngưỡng ⇒ Critical.</summary>
    public TimeSpan? JournalCriticalPendingAge { get; set; }
    /// <summary>Dung lượng journal (byte) ≥ ngưỡng ⇒ Warn.</summary>
    public long? JournalWarnJournalBytes { get; set; }
    /// <summary>Dung lượng journal (byte) ≥ ngưỡng ⇒ Critical.</summary>
    public long? JournalCriticalJournalBytes { get; set; }
    /// <summary>true ⇒ purge nền chỉ đếm, không xoá.</summary>
    public bool? JournalDryRun { get; set; }

    // Backup
    /// <summary>Giờ backup tự động <c>HH:mm</c> theo giờ server; chuỗi rỗng = không chạy lịch.</summary>
    public string? BackupSchedule { get; set; }
    /// <summary>Ngày trong tuần của bản weekly.</summary>
    public DayOfWeek? BackupWeeklyDay { get; set; }
    /// <summary>Đầu cửa sổ cho phép dừng node <c>HH:mm</c> (qua nửa đêm khi đầu &gt; cuối).</summary>
    public string? BackupWindowStart { get; set; }
    /// <summary>Cuối cửa sổ cho phép dừng node <c>HH:mm</c>; không được trùng đầu cửa sổ.</summary>
    public string? BackupWindowEnd { get; set; }
    /// <summary>Lệch giờ giữa các node trong cùng lịch.</summary>
    public TimeSpan? BackupNodeOffset { get; set; }
    /// <summary>Số lần thử mỗi slot.</summary>
    public int? BackupMaxAttempts { get; set; }
    /// <summary>Chờ giữa hai lần thử.</summary>
    public TimeSpan? BackupRetryDelay { get; set; }
    /// <summary>Số bản daily giữ lại (≥ 7).</summary>
    public int? BackupDailyCopies { get; set; }
    /// <summary>Số bản weekly giữ lại (≥ 4).</summary>
    public int? BackupWeeklyCopies { get; set; }
    /// <summary>Số bản backup tay giữ lại.</summary>
    public int? BackupManualCopies { get; set; }
    /// <summary>Ngân sách thời gian node được dừng cho một bản backup.</summary>
    public TimeSpan? BackupMaxStoppedDuration { get; set; }
    /// <summary>Thông lượng tối thiểu (byte/giây) dùng dự toán trước khi dừng node.</summary>
    public long? BackupMinThroughputBytesPerSecond { get; set; }
    /// <summary>Dung lượng trống tối thiểu (byte) của thư mục backup.</summary>
    public long? BackupMinimumFreeBytes { get; set; }
    /// <summary>true ⇒ lịch chỉ ghi timeline, không dừng node/chép/xoá file.</summary>
    public bool? BackupDryRun { get; set; }

    // Router
    /// <summary>Ngân sách chờ cursor/WAL của node phủ head đã chốt trên đường đọc.</summary>
    public TimeSpan? RouterCoverageWait { get; set; }
    /// <summary>Node ưu tiên phải Ready liên tục bấy lâu mới failback.</summary>
    public TimeSpan? RouterFailbackStableFor { get; set; }
    /// <summary>Breaker chặn node sau lỗi kết nối trong khoảng này.</summary>
    public TimeSpan? RouterBreakerCooldown { get; set; }

    // Export Parquet
    /// <summary>Giờ xuất hằng ngày <c>HH:mm</c> theo giờ server (xuất trọn ngày hôm trước).</summary>
    public string? ExportSchedule { get; set; }
    /// <summary>Số ngày giữ thư mục xuất (bản Verified mới nhất mỗi bảng luôn giữ).</summary>
    public int? ExportRetentionDays { get; set; }
    /// <summary>Số ngày trước ngày vừa xuất được kiểm lại để bắt dữ liệu đến muộn (0 = không kiểm).</summary>
    public int? ExportRecheckDays { get; set; }
    /// <summary>Số lần thử tối đa trong ngày.</summary>
    public int? ExportMaxAttempts { get; set; }
    /// <summary>Chờ giữa hai lần thử.</summary>
    public TimeSpan? ExportRetryDelay { get; set; }
    /// <summary>Ngày mới nhất đã xuất cách đây lâu hơn ⇒ Warn; phải nhỏ hơn ngưỡng Critical.</summary>
    public TimeSpan? ExportWarnAfter { get; set; }
    /// <summary>Ngày mới nhất đã xuất cách đây lâu hơn ⇒ Critical.</summary>
    public TimeSpan? ExportCriticalAfter { get; set; }
    /// <summary>true ⇒ lịch chỉ xem trước, không ghi file/catalog, không xoá.</summary>
    public bool? ExportDryRun { get; set; }

    /// <summary>
    /// Kiểm các thuộc tính ĐÃ khai (biên min/max, định dạng <c>HH:mm</c>, cặp Warn &lt; Critical khi khai cả hai). Trả danh sách lỗi
    /// (rỗng = hợp lệ), không ném. Kiểm đầy đủ sau khi gộp với giá trị khởi động nằm ở <see cref="QuestHaManager.ApplySettings"/>.
    /// </summary>
    public IReadOnlyList<string> Validate() => Infrastructure.Services.Settings.Rules.QuestHaSettingsRules.Validate(this);

    /// <summary>
    /// Kiểm ĐÚNG luật <see cref="QuestHaManager.ApplySettings(QuestHaRuntimeOptions)"/> dùng: gói, gộp lên <paramref name="baseline"/>
    /// (thuộc tính null ⇒ giá trị của baseline), rồi bộ đã gộp (cặp Warn &lt; Critical với giá trị mặc định, slot lịch backup của
    /// từng node trong <c>baseline.Nodes</c> phải trong ngày và trong cửa sổ). Host gọi trước khi lưu cấu hình. Rỗng = hợp lệ; không ném.
    /// </summary>
    public IReadOnlyList<string> ValidateEffective(QuestHaOptions baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return Infrastructure.Services.Settings.Rules.QuestHaSettingsRules.ValidateEffective(baseline, this, out _);
    }

    /// <summary>Bộ có hiệu lực đủ mọi thuộc tính: gói này gộp lên <paramref name="baseline"/>. Không kiểm, không sửa baseline.</summary>
    public QuestHaRuntimeOptions Effective(QuestHaOptions baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        return Infrastructure.Services.Settings.Rules.QuestHaSettingsRules.FromOptions(
            Infrastructure.Services.Settings.Rules.QuestHaSettingsRules.Merge(baseline, this));
    }
}
