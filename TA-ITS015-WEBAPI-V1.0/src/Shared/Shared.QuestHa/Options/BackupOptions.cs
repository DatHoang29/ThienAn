// giờ vận hành không phụ thuộc culture của tài khoản service.
using System.Globalization;

namespace Shared.QuestHa.Options;

/// <summary>
/// Author: KienLT
/// Description: Cấu hình backup/restore nguội (thư mục lưu, thời gian chờ dừng/khởi động node).
/// Created date: 09/10/2026
/// </summary>
public sealed class BackupOptions
{
    // mặc định không chạy nền; giờ lịch theo timezone của server.
    public bool Enabled { get; set; }
    public bool DryRun { get; set; } = true;
    public string Schedule { get; set; } = "";
    public DayOfWeek WeeklyDay { get; set; } = DayOfWeek.Sunday;
    public string WindowStart { get; set; } = "01:00";
    public string WindowEnd { get; set; } = "05:00";
    public TimeSpan NodeOffset { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public int DailyCopies { get; set; } = 7;
    public int WeeklyCopies { get; set; } = 4;
    // quota thủ công riêng, ngân sách dừng và dấu phục hồi bền qua restart host.
    public int ManualCopies { get; set; } = 7;
    public TimeSpan MaxStoppedDuration { get; set; } = TimeSpan.FromMinutes(30);
    // ba lượt I/O (copy + hai hash); dự toán trước khi dừng, không coi throughput là bảo đảm.
    public long MinThroughputBytesPerSecond { get; set; } = 10L * 1024 * 1024;
    public string StateDirectory { get; set; } = "data/questha-backup";
    public long MinimumFreeBytes { get; set; } = 1024L * 1024 * 1024;
    public Dictionary<string, string> NodeBackupRoots { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, BackupNodeControlOptions> NodeControls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>IPC cùng máy/cùng tài khoản tới host đang sở hữu replicator; rỗng thì tắt.</summary>
    public string AdminPipeName { get; set; } = "";

    /// <summary>Root backup riêng của node hoặc root mặc định.</summary>
    public string RootFor(string node) => NodeBackupRoots.TryGetValue(node, out var root) ? root : BackupRoot;

    /// <summary>Kiểm cấu hình trước mọi thao tác backup.</summary>
    public void Validate()
    {
        // dấu phục hồi phải sống qua thay đổi CWD/deploy của Windows Service.
        if ((Enabled || !string.IsNullOrWhiteSpace(AdminPipeName)) && !Path.IsPathFullyQualified(StateDirectory))
            throw new ArgumentException("QuestHa:Backup:StateDirectory phải là đường dẫn tuyệt đối khi bật backup hoặc IPC.");
        if ((!string.IsNullOrWhiteSpace(Schedule) && !TimeOnly.TryParseExact(Schedule, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) ||
            !TimeOnly.TryParseExact(WindowStart, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) || !TimeOnly.TryParseExact(WindowEnd, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            WindowStart == WindowEnd || NodeOffset <= TimeSpan.Zero || NodeOffset >= TimeSpan.FromDays(1) ||
            PollInterval <= TimeSpan.Zero || PollInterval > TimeSpan.FromDays(1) || RetryDelay <= TimeSpan.Zero || RetryDelay > TimeSpan.FromDays(7) || MaxAttempts is < 1 or > 10 ||
            DailyCopies < 7 || WeeklyCopies < 4 || ManualCopies < 1 || MinimumFreeBytes < 0 ||
            string.IsNullOrWhiteSpace(StateDirectory) || MinThroughputBytesPerSecond <= 0 || MaxStoppedDuration <= TimeSpan.Zero || MaxStoppedDuration > TimeSpan.FromDays(1) ||
            NodeStopTimeout <= TimeSpan.Zero || NodeStopTimeout > TimeSpan.FromDays(1) ||
            NodeStartTimeout <= TimeSpan.Zero || NodeStartTimeout > TimeSpan.FromDays(1) || !Enum.IsDefined(WeeklyDay))
            throw new ArgumentException("QuestHa:Backup có lịch hoặc giới hạn không hợp lệ (giữ ít nhất 7 daily + 4 weekly).");
    }
    /// <summary>
    /// Thư mục chứa bản backup nguội. Mỗi bản: <c>{BackupRoot}\{Node}\{yyyyMMdd-HHmmss}-{guidN}</c>.
    /// Bắt buộc cấu hình khi dùng backup/restore (rỗng ⇒ backup báo lỗi).
    /// </summary>
    public string BackupRoot { get; set; } = "";

    public TimeSpan NodeStopTimeout { get; set; } = TimeSpan.FromSeconds(60);

    public TimeSpan NodeStartTimeout { get; set; } = TimeSpan.FromSeconds(90);

    /// <summary>Bản sao nông để dựng snapshot tham số vận hành mới; không sửa bản đang được đọc.</summary>
    internal BackupOptions Clone() => (BackupOptions)MemberwiseClone();
}
