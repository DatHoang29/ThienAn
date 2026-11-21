namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Chính sách bảo trì journal; không loại pending hoặc từ chối ACK khi quá ngưỡng.
/// Created date: 28/10/2026
/// </summary>
public sealed class JournalMaintenanceOptions
{
    public bool Enabled { get; set; } = true;
    public bool DryRun { get; set; }
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
    // Ngắn (3 ngày) vì dòng chỉ được xoá khi đã nằm trong bản xuất Parquet hoặc backup lạnh Verified; journal ~1,7 KB/dòng.
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(3);
    // cửa sổ chung giữa retired hiệu lực và SQL purge, không đếm bản sao.
    /// <summary>Backup Verified gần đây giữ quyền restore-replay; mới nhất mỗi node còn nghĩa vụ journal luôn được bảo vệ.</summary>
    public int ProtectedBackupDays { get; set; } = 8;
    /// <summary>Chấp nhận mất khả năng restore-replay từ backup chưa có trong catalog; mặc định cấm.</summary>
    public bool AllowPurgeWithoutVerifiedBackup { get; set; }

    /// <summary>
    /// Export Parquet bật (<c>QuestHa:Export:Enabled</c>, gán lúc đăng ký): dòng đã nằm trong bản xuất Verified được tính như đã có backup.
    /// Không bind từ cấu hình.
    /// </summary>
    internal bool ExportCountsAsBackup { get; set; }
    public int BatchSize { get; set; } = 200;
    public int MaxRowsPerRun { get; set; } = 2_000;
    public TimeSpan MaxRunDuration { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan AlertThrottle { get; set; } = TimeSpan.FromMinutes(5);
    public long WarnPendingRows { get; set; } = 100_000;
    public long CriticalPendingRows { get; set; } = 1_000_000;
    public TimeSpan WarnPendingAge { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan CriticalPendingAge { get; set; } = TimeSpan.FromMinutes(5);
    public long WarnJournalBytes { get; set; } = 10L * 1024 * 1024 * 1024;
    public long CriticalJournalBytes { get; set; } = 50L * 1024 * 1024 * 1024;

    /// <summary>Từ chối cấu hình sai trước khi truy cập SQL; lô nhỏ tránh leo thang khoá.</summary>
    public void Validate()
    {
        if (Interval <= TimeSpan.Zero || Interval > TimeSpan.FromDays(30) || Retention <= TimeSpan.Zero || Retention > TimeSpan.FromDays(36500) ||
            ProtectedBackupDays is < 1 or > 36500 || BatchSize is < 1 or > 500 || MaxRowsPerRun < 1 || MaxRunDuration <= TimeSpan.Zero ||
            MaxRunDuration > TimeSpan.FromMinutes(1) || AlertThrottle <= TimeSpan.Zero ||
            WarnPendingRows < 1 || CriticalPendingRows < WarnPendingRows ||
            WarnPendingAge <= TimeSpan.Zero || CriticalPendingAge < WarnPendingAge ||
            WarnJournalBytes < 1 || CriticalJournalBytes < WarnJournalBytes)
            throw new ArgumentException("QuestHa:JournalMaintenance có giới hạn hoặc ngưỡng không hợp lệ.");
    }

    /// <summary>Bản sao nông để dựng snapshot tham số vận hành mới; không sửa bản đang được đọc.</summary>
    internal JournalMaintenanceOptions Clone() => (JournalMaintenanceOptions)MemberwiseClone();
}
