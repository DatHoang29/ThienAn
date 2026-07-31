namespace ITS.Sync.Core.Enums;

/// <summary>
/// Trạng thái của MỘT lần chạy đồng bộ (một strategy / một bảng).
/// </summary>
public class SyncStatus
{
        public const string Pending = "Pending";
        public const string Running = "Running";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";
        public const string Success = "Success";
}
